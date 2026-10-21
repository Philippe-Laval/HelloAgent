using System.Collections.Concurrent;
using System.Text.Json;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hosting;
using Microsoft.Agents.AI.Hosting.OpenAI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Extensions.AI;

// This sample shows how an application can own its own ASP.NET Core route and expose a workflow over the
// OpenAI Responses protocol. It uses the OpenAIResponses conversion helpers for the wire protocol and
// HostedWorkflowState for per-session checkpoint resume. The application keeps control of routing, auth,
// and checkpoint storage.
//
// This server demonstrates previous_response_id continuation ONLY. It rejects conversation-id continuity
// with HTTP 400. Because previous_response_id rotates every turn, the app owns a cursor store that maps each
// response id to the stable workflow session id, so the whole rotating chain resumes the same checkpointed
// run.

namespace LocalResponsesWorkflowServer;

public partial class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // This local sample does not authenticate callers. Multi-user hosts must authenticate and authorize
        // requests and partition response mappings, sessions, and checkpoints by trusted caller identity.
        // An isolation provider does not automatically scope these application-owned stores.
        // See the shared hosting guide and this sample's README.

        // Configuration via environment variables (never hardcode secrets).
        string endpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT")
            ?? throw new InvalidOperationException("FOUNDRY_PROJECT_ENDPOINT is not set.");
        string model = Environment.GetEnvironmentVariable("FOUNDRY_MODEL") ?? "gpt-6-luna";

        // WARNING: DefaultAzureCredential is convenient for development but requires careful consideration in production.
        // In production, consider using a specific credential (e.g., ManagedIdentityCredential) to avoid
        // latency issues, unintended credential probing, and potential security risks from fallback mechanisms.
        var projectClient = new AIProjectClient(new Uri(endpoint), new AzureCliCredential());
        AIAgent writer = projectClient.AsAIAgent(
            model: model,
            instructions: "You are an excellent slogan writer. Create one short slogan from the given brief.",
            name: "writer");

        // Workflow shape: a brief adapter turns the Responses input into the writer's
        // prompt and drives the agent turn, then a formatter renders the writer's output as a single slogan line.
        // A factory builds a fresh workflow instance per run so independent sessions can run concurrently.
        static Workflow BuildWorkflow(AIAgent writer)
        {
            var briefExecutor = new BriefExecutor();
            var formatterExecutor = new SloganFormatterExecutor();

            return new WorkflowBuilder(briefExecutor)
                .AddEdge(briefExecutor, writer)
                .AddEdge(writer, formatterExecutor)
                .WithOutputFrom(formatterExecutor)
                .Build();
        }

        // Optional shared execution state: the factory constructor builds a fresh workflow instance per run (the
        // default, cacheWorkflow: false, shown explicitly here), so independent sessions run in parallel — a single
        // shared instance cannot run concurrent turns. Pass cacheWorkflow: true instead to build the workflow once,
        // lazily on first use, and reuse it (a deferred, cached target that, like a shared instance, cannot run
        // concurrent turns). It is paired with an in-memory CheckpointManager and a per-session
        // sessionId -> CheckpointInfo head cursor so a session can resume from its last checkpoint; a resume rehydrates
        // a fresh instance from that shared checkpoint store.
        var state = new HostedWorkflowState(_ => new ValueTask<Workflow>(BuildWorkflow(writer)), cacheWorkflow: false);

        // The app keeps a response-id -> workflow-session-id cursor. previous_response_id rotates each turn, so every id in
        // a conversation's chain maps to the same workflow session, and resuming any of them restores that session's
        // latest checkpoint. In-memory for this local sample; a real app persists this per tenant/user.
        var responseToSession = new ConcurrentDictionary<string, string>(StringComparer.Ordinal);

        var app = builder.Build();

        // The application owns this route. Binding the body as JsonElement lets ASP.NET Core deserialize the JSON
        // request body directly, so there is no JsonDocument to own or dispose.
        app.MapPost("/responses", async (JsonElement body, CancellationToken cancellationToken) =>
        {
            OpenAIResponsesRunRequest run;
            try
            {
                run = OpenAIResponses.ToAgentRunRequest(body);
            }
            catch (ArgumentException)
            {
                return Results.BadRequest();
            }

            // This sample supports previous_response_id continuation only, read off the already-parsed request.
            // A conversation id is not implemented here, so reject it. The candidate is untrusted: a real app
            // authenticates the caller and authorizes/binds it before use.
            if (run.ConversationId is not null)
            {
                return Results.Problem(
                    detail: "This server supports previous_response_id continuation only; conversation is not implemented.",
                    statusCode: StatusCodes.Status400BadRequest);
            }

            string? previousResponseId = run.PreviousResponseId;
            string responseId = OpenAIResponses.CreateResponseId();

            // Resolve the workflow session: continue the chain's session when previous_response_id is known, otherwise
            // start a fresh workflow continuation.
            string sessionStoreId = previousResponseId is not null && responseToSession.TryGetValue(previousResponseId, out string? existing)
                ? existing
                : Guid.NewGuid().ToString("N");

            // Runs the workflow forward on the first call for this session, or restores the session's latest checkpoint
            // and runs forward with this turn's brief thereafter, then records the new head checkpoint.
            string brief = ExtractBrief(run.Messages);
            HostedWorkflowRunResult result = await state.RunOrResumeAsync(sessionStoreId, brief, cancellationToken).ConfigureAwait(false);

            // Map this response id onto the workflow session so the next previous_response_id continues the same run.
            responseToSession[responseId] = sessionStoreId;

            AgentResponse response = BuildWorkflowResponse(result);
            return Results.Json(OpenAIResponses.WriteResponse(response, responseId, previousResponseId));
        });

        // Bind to a fixed local URL so the paired client sample has a deterministic default.
        // Override with the ASPNETCORE_URLS environment variable when needed.
        app.Run("http://localhost:5001");
    }

    // Flattens the Responses input messages into a single brief string for the workflow's start executor.
    static string ExtractBrief(IEnumerable<ChatMessage> messages)
        => string.Join("\n", messages.Select(m => m.Text).Where(t => !string.IsNullOrWhiteSpace(t))).Trim();

    // Extracts the workflow's final string output (the formatted slogan) from its output events, falling back to a
    // short run summary when the workflow emitted no string output this turn.
    static AgentResponse BuildWorkflowResponse(HostedWorkflowRunResult result)
    {
        string? slogan = null;
        foreach (WorkflowEvent evt in result.Events)
        {
            if (evt is WorkflowOutputEvent output && output.Data is string text && !string.IsNullOrWhiteSpace(text))
            {
                slogan = text;
            }
        }

        return new AgentResponse(new ChatMessage(
            ChatRole.Assistant,
            slogan ?? $"{result.Events.Count} workflow event(s) processed."));
    }
}

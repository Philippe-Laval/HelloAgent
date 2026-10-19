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


/// <summary>
/// Formats the writer agent's output as the workflow's final response: one terminal-friendly slogan line.
/// </summary>
internal sealed class SloganFormatterExecutor() : Executor<List<ChatMessage>, string>("terminal_formatter")
{
    public override ValueTask<string> HandleAsync(List<ChatMessage> message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        string slogan = string.Join("\n", message.Select(m => m.Text ?? string.Empty)).Trim().Trim('"');
        return ValueTask.FromResult($"Slogan: \"{slogan}\"");
    }
}


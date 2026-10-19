using System.Text.Json;
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
/// Adapts the Responses brief into the writer agent's turn. It builds the writer prompt from the brief (a plain
/// topic string or a JSON object with topic/style/audience), sends it as a user message, and emits the
/// <see cref="TurnToken"/> that drives the downstream agent. This keeps the workflow non-chat-protocol (its
/// output is a plain string) while still driving the agent.
/// </summary>
[SendsMessage(typeof(ChatMessage))]
[SendsMessage(typeof(TurnToken))]
internal sealed class BriefExecutor() : Executor<string>("brief")
{
    public override async ValueTask HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
    {
        string topic = message.Trim();
        string style = "modern";
        string audience = "general";

        if (topic.StartsWith('{'))
        {
            try
            {
                using JsonDocument doc = JsonDocument.Parse(topic);
                JsonElement root = doc.RootElement;
                if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("topic", out JsonElement topicElement))
                {
                    topic = topicElement.GetString() ?? topic;
                    style = root.TryGetProperty("style", out JsonElement styleElement) ? styleElement.GetString() ?? style : style;
                    audience = root.TryGetProperty("audience", out JsonElement audienceElement) ? audienceElement.GetString() ?? audience : audience;
                }
            }
            catch (JsonException)
            {
                // Not a JSON brief; treat the whole text as the topic.
            }
        }

        if (string.IsNullOrWhiteSpace(topic))
        {
            topic = "a generic product";
        }

        string prompt =
            $"Topic: {topic}\n" +
            $"Style: {style}\n" +
            $"Audience: {audience}\n\n" +
            "Write a single short slogan that fits the topic, style, and audience.";

        await context.SendMessageAsync(new ChatMessage(ChatRole.User, prompt), cancellationToken: cancellationToken).ConfigureAwait(false);
        await context.SendMessageAsync(new TurnToken(emitEvents: true), cancellationToken: cancellationToken).ConfigureAwait(false);
    }
}


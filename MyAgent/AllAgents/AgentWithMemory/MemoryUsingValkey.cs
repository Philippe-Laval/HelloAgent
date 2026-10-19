using Azure.AI.Extensions.OpenAI;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Valkey;
using Microsoft.Extensions.AI;
using Valkey.Glide;

// wsl
// docker pull valkey/valkey-bundle
// docker run --name my-valkey-bundle -d -p 6379:6379 valkey/valkey-bundle
// docker exec -it my-valkey-bundle valkey-cli -h localhost -p 6379 -3
// MODULE LIST
// bf, json, search, ldap, lua
// exit


namespace MyAgent.AllAgents.AgentWithMemory
{
    internal class MemoryUsingValkey
    {
        public async Task Test()
        {
            var endpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT") ?? throw new InvalidOperationException("FOUNDRY_PROJECT_ENDPOINT is not set.");
            var deploymentName = Environment.GetEnvironmentVariable("FOUNDRY_MODEL") ?? "gpt-5.4-mini";
            var valkeyConnection = Environment.GetEnvironmentVariable("VALKEY_CONNECTION") ?? "localhost:6379";

            var connection = await ConnectionMultiplexer.ConnectAsync(valkeyConnection);

            Console.WriteLine("=== ValkeyChatHistoryProvider — Persistent Chat History ===\n");

            var historyProvider = new ValkeyChatHistoryProvider(
                connection,
                _ => new ValkeyChatHistoryProvider.State($"sample-{Guid.NewGuid():N}"),
                new ValkeyChatHistoryProviderOptions
                {
                    KeyPrefix = "sample_chat",
                    MaxMessages = 20
                });

            // Disable service-side stored output: ChatHistoryProvider is the single source of
            // truth for this conversation and must not be combined with a ConversationId.
#pragma warning disable MAAI001, OPENAI001
            AIAgent historyAgent = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
                .GetProjectOpenAIClient()
                .GetProjectResponsesClient()
                .AsIChatClientWithStoredOutputDisabled(deploymentName)
                .AsAIAgent(new ChatClientAgentOptions()
                {
                    ChatOptions = new() {
                        ModelId = deploymentName, 
                        Instructions = "You are a helpful assistant that remembers our conversation." 
                    },
                    ChatHistoryProvider = historyProvider
                });
#pragma warning restore MAAI001, OPENAI001

            AgentSession session1 = await historyAgent.CreateSessionAsync();
            Console.WriteLine(await historyAgent.RunAsync("Hello! My name is Alex and I'm a software engineer.", session1));
            Console.WriteLine(await historyAgent.RunAsync("I'm working on a project using Valkey for caching.", session1));
            Console.WriteLine(await historyAgent.RunAsync("What do you remember about me?", session1));

            var messageCount = await historyProvider.GetMessageCountAsync(session1);
            Console.WriteLine($"\n  Stored {messageCount} messages in Valkey.\n");

            // Clean up
            connection.Dispose();

            Console.WriteLine("Done!");
        }
    }
}

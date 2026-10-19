using System.Text;
using System.Text.Json;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;

// This sample shows how to add a basic custom memory component to an agent.
// The memory component subscribes to all messages added to the conversation and
// extracts the user's name and age if provided.
// The component adds a prompt to ask for this information if it is not already known
// and provides it to the model before each invocation if known.

namespace MyAgent.GetStarted
{
    public class HelloMemory
    {
        public async Task Test()
        {
            var endpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT") ?? throw new InvalidOperationException("FOUNDRY_PROJECT_ENDPOINT is not set.");
            var model = Environment.GetEnvironmentVariable("FOUNDRY_MODEL") ?? "gpt-6-luna";

            // WARNING: DefaultAzureCredential is convenient for development but requires careful consideration in production.
            // In production, consider using a specific credential (e.g., ManagedIdentityCredential) to avoid
            // latency issues, unintended credential probing, and potential security risks from fallback mechanisms.
            var projectClient = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential());

# pragma warning disable OPENAI001
            // Create a separate IChatClient for the memory component to use for structured extraction.
            // The memory component calls the model with a ResponseFormat (JSON schema) to extract user info.
            // Using a dedicated client here avoids mixing side-channel extraction calls with the agent's
            // conversation history, and avoids the chicken-and-egg problem of needing an IChatClient
            // before the main agent is constructed.
            IChatClient extractionClient =
                new AIProjectClient(
                    new Uri(endpoint),
                    new DefaultAzureCredential())
                .GetProjectOpenAIClient()
                .GetResponsesClient()
                .AsIChatClient(model);
# pragma warning restore OPENAI001

            // Create the agent with instructions and the custom memory context provider.
            // The memory component is attached to all sessions created by the agent. Here each new memory
            // component will have its own user info object, so each session will have its own memory.
            // In real world applications/services, where the user info would be persisted in a database,
            // and preferably shared between multiple sessions used by the same user, ensure that the
            // factory reads the user id from the current context and scopes the memory component
            // and its storage to that user id.
            AIAgent agent = projectClient.AsAIAgent(new ChatClientAgentOptions
            {
                ChatOptions = new ChatOptions
                {
                    ModelId = model,
                    Instructions = "You are a friendly assistant. Always address the user by their name.",
                },
                AIContextProviders = [new UserInfoMemory(extractionClient)]
            });

            // Create a new session for the conversation.
            AgentSession session = await agent.CreateSessionAsync();

            Console.WriteLine(">> Use session with blank memory\n");

            // Invoke the agent and output the text result.
            Console.WriteLine(await agent.RunAsync("Hello, what is the square root of 9?", session));
            Console.WriteLine(await agent.RunAsync("My name is Ruaidhrí", session));
            Console.WriteLine(await agent.RunAsync("I am 20 years old", session));

            // We can serialize the session. The serialized state will include the state of the memory component.
            JsonElement sessionElement = await agent.SerializeSessionAsync(session);

            Console.WriteLine("\n>> Use deserialized session with previously created memories\n");

            // Later we can deserialize the session and continue the conversation with the previous memory component state.
            var deserializedSession = await agent.DeserializeSessionAsync(sessionElement);
            Console.WriteLine(await agent.RunAsync("What is my name and age?", deserializedSession));

            Console.WriteLine("\n>> Read memories using memory component\n");

            // It's possible to access the memory component via the agent's GetService method.
            var userInfo = agent.GetService<UserInfoMemory>()?.GetUserInfo(deserializedSession);

            // Output the user info that was captured by the memory component.
            Console.WriteLine($"MEMORY - User Name: {userInfo?.UserName}");
            Console.WriteLine($"MEMORY - User Age: {userInfo?.UserAge}");

            Console.WriteLine("\n>> Use new session with previously created memories\n");

            // It is also possible to set the memories using a memory component on an individual session.
            // This is useful if we want to start a new session, but have it share the same memories as a previous session.
            var newSession = await agent.CreateSessionAsync();
            if (userInfo is not null && agent.GetService<UserInfoMemory>() is UserInfoMemory newSessionMemory)
            {
                newSessionMemory.SetUserInfo(newSession, userInfo);
            }

            // Invoke the agent and output the text result.
            // This time the agent should remember the user's name and use it in the response.
            Console.WriteLine(await agent.RunAsync("What is my name and age?", newSession));
        }

    }
}

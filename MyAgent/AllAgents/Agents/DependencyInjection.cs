using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

// Dependency Injection — Register and resolve agents via DI
//
// This sample shows how to use dependency injection to register an
// AIAgent and consume it from a hosted service with a chat loop.

namespace MyAgent.AllAgents.Agents
{
    internal class DependencyInjection
    {
        public async Task Test(string[] args)
        {
            var endpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT") ?? throw new InvalidOperationException("FOUNDRY_PROJECT_ENDPOINT is not set.");
            var deploymentName = Environment.GetEnvironmentVariable("FOUNDRY_MODEL") ?? "gpt-6-luna";

            // Create a host builder that we will register services with and then run.
            HostApplicationBuilder builder = Host.CreateApplicationBuilder(args);

            // Create the AI agent from the Microsoft Foundry project client.
            // WARNING: DefaultAzureCredential is convenient for development but requires careful consideration in production.
            // In production, consider using a specific credential (e.g., ManagedIdentityCredential) to avoid
            // latency issues, unintended credential probing, and potential security risks from fallback mechanisms.
            AIProjectClient aiProjectClient = new(new Uri(endpoint), new DefaultAzureCredential());
            AIAgent agent = aiProjectClient.AsAIAgent(model: deploymentName, name: "Joker", instructions: "You are good at telling jokes.");
            builder.Services.AddSingleton(agent);

            // Add a sample service that will use the agent to respond to user input.
            builder.Services.AddHostedService<SampleService>();

            // Build and run the host.
            using IHost host = builder.Build();
            await host.RunAsync().ConfigureAwait(false);
        }
    }
}

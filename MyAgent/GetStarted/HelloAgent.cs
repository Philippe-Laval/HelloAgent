using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using System;
using System.Collections.Generic;
using System.Text;

// This sample shows how to create and use a simple AI agent with AIProjectClient as the backend.

namespace MyAgent.GetStarted
{
    public class HelloAgent
    {
        public async Task Test()
        {
            var endpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT") ?? throw new InvalidOperationException("FOUNDRY_PROJECT_ENDPOINT is not set.");
            var model = Environment.GetEnvironmentVariable("FOUNDRY_MODEL") ?? "gpt-6-luna";

            // WARNING: DefaultAzureCredential is convenient for development but requires careful consideration in production.
            // In production, consider using a specific credential (e.g., ManagedIdentityCredential) to avoid
            // latency issues, unintended credential probing, and potential security risks from fallback mechanisms.
            AIAgent agent = new AIProjectClient(new Uri(endpoint), new DefaultAzureCredential())
                .AsAIAgent(model: model, instructions: "You are good at telling jokes.", name: "Joker");

            // Invoke the agent and output the text result.
            Console.WriteLine(await agent.RunAsync("Tell me a joke about a pirate."));

            // Invoke the agent with streaming support.
            await foreach (var update in agent.RunStreamingAsync("Tell me a joke about a pirate."))
            {
                Console.WriteLine(update);
            }
        }

    }
}

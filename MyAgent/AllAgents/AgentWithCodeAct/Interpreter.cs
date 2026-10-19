using Azure.AI.Projects;
using Azure.Identity;
using HyperlightSandbox.Guest.Python;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hyperlight;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyAgent.AllAgents.AgentWithCodeAct
{
    internal class Interpreter
    {
        public async Task Test()
        {
            var endpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT") ?? throw new InvalidOperationException("FOUNDRY_PROJECT_ENDPOINT is not set.");
            var deploymentName = Environment.GetEnvironmentVariable("FOUNDRY_MODEL") ?? "gpt-5.4-mini";
            //var guestPath = Environment.GetEnvironmentVariable("HYPERLIGHT_PYTHON_GUEST_PATH") ?? throw new InvalidOperationException("HYPERLIGHT_PYTHON_GUEST_PATH is not set.");

            // Python generation happens here: PythonGuestModule supplies a WASM-compiled Python
            // interpreter that runs inside the Hyperlight micro-VM. When the model emits an
            // execute_code call, this sandbox receives the generated Python program and runs it.
            var codeActOptions =
                HyperlightCodeActProviderOptions.CreateForWasm(PythonGuestModule.GetModulePath());

            //codeActOptions.Tools =
            //[
            //    getApplications,
            //    getIncidents,
            //    getCosts
            //];

            //codeActOptions.FileMounts =
            //[
            //    new FileMount("/host/data", "/input/data"),
            //    new FileMount("/host/models", "/sandbox/models"),
            //];

            //codeActOptions.AllowedDomains =
            //[
            //    new AllowedDomain("https://api.github.com"),
            //    new AllowedDomain("https://internal.api.example.com", ["GET"]),
            //];

            using var codeAct = new HyperlightCodeActProvider(codeActOptions);

            // WARNING: DefaultAzureCredential is convenient for development but requires careful consideration in production.
            // In production, consider using a specific credential (e.g., ManagedIdentityCredential) to avoid
            // latency issues, unintended credential probing, and potential security risks from fallback mechanisms.
            AIAgent agent = new AIProjectClient(
                new Uri(endpoint),
                new DefaultAzureCredential())
                .AsAIAgent(new ChatClientAgentOptions()
                {
                    ChatOptions = new() { 
                        ModelId = deploymentName, 
                        Instructions = "You are a helpful assistant. When the user asks something quantitative, write Python and call `execute_code` instead of guessing." 
                    },
                    AIContextProviders = [codeAct],
                });

            Console.WriteLine(await agent.RunAsync("What is the sum of 2 and 3?"));
            Console.WriteLine(await agent.RunAsync("What is the 20th Fibonacci number?"));
            Console.WriteLine(await agent.RunAsync("Compute the mean and standard deviation of [1, 4, 9, 16, 25, 36]."));
        }
    }
}

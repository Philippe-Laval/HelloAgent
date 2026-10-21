using Azure.AI.Projects;
using Azure.Identity;
using HyperlightSandbox.Guest.Python;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hyperlight;
using Microsoft.Extensions.AI;

// This sample shows how to wire up CodeAct manually using
// HyperlightExecuteCodeFunction rather than the AIContextProvider. Use this
// when you want a fixed tool surface for the agent's lifetime and don't need
// the per-run snapshot/registry semantics of HyperlightCodeActProvider.

namespace MyAgent.AllAgents.AgentWithCodeAct
{
    internal class ManualWiring
    {
        public async Task Test()
        {
            var endpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT") ?? throw new InvalidOperationException("FOUNDRY_PROJECT_ENDPOINT is not set.");
            var deploymentName = Environment.GetEnvironmentVariable("FOUNDRY_MODEL") ?? "gpt-6-luna";

            AIFunction calculate = AIFunctionFactory.Create(
                (double a, double b) => a * b,
                name: "multiply",
                description: "Multiply two numbers.");

            var options = HyperlightCodeActProviderOptions.CreateForWasm(PythonGuestModule.GetModulePath());
            options.Tools = [calculate];

            using var executeCode = new HyperlightExecuteCodeFunction(options);

            var instructions =
                "You are a helpful assistant. When math is involved, solve it by writing Python "
                + "and calling `execute_code` instead of computing values yourself.\n\n"
                + executeCode.BuildInstructions(toolsVisibleToModel: false);

            // WARNING: DefaultAzureCredential is convenient for development but requires careful consideration in production.
            // In production, consider using a specific credential (e.g., ManagedIdentityCredential) to avoid
            // latency issues, unintended credential probing, and potential security risks from fallback mechanisms.
            AIAgent agent = new AIProjectClient(
                new Uri(endpoint),
                new DefaultAzureCredential())
                .AsAIAgent(model: deploymentName, instructions: instructions, tools: [executeCode]);

            Console.WriteLine(await agent.RunAsync("What is 12.3 * 4.5? Use the multiply tool from within `execute_code`."));

        }
    }
}

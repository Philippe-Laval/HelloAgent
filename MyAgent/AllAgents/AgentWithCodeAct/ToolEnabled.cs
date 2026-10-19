using Azure.AI.Projects;
using Azure.Identity;
using HyperlightSandbox.Guest.Python;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Hyperlight;
using Microsoft.Extensions.AI;


// This sample shows how to use HyperlightCodeActProvider with provider-owned
// tools (exposed inside the sandbox via `call_tool(...)`). The model can
// orchestrate those tools in a single Python block, reducing round-trips. A
// sensitive tool (`send_email`) is additionally wrapped in
// ApprovalRequiredAIFunction so any code that reaches it requires user approval
// for the entire execute_code invocation.

namespace MyAgent.AllAgents.AgentWithCodeAct
{
    internal class ToolEnabled
    {
        public async Task Test()
        {
            var endpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT") ?? throw new InvalidOperationException("FOUNDRY_PROJECT_ENDPOINT is not set.");
            var deploymentName = Environment.GetEnvironmentVariable("FOUNDRY_MODEL") ?? "gpt-5.4-mini";

            AIFunction fetchDocs = AIFunctionFactory.Create(
                (string topic) => $"Docs for {topic}: (...)",
                name: "fetch_docs",
                description: "Fetch documentation for a given topic.");

            AIFunction queryData = AIFunctionFactory.Create(
                (string query) => $"Rows for `{query}`: []",
                name: "query_data",
                description: "Run a read-only SQL-like query against the sample store.");

            AIFunction sendEmail = new ApprovalRequiredAIFunction(
                AIFunctionFactory.Create(
                    (string to, string subject) => $"Sent '{subject}' to {to}.",
                    name: "send_email",
                    description: "Send an email on behalf of the user."));

            AIFunction responseToQuestions = AIFunctionFactory.Create(
                (string question) => $"The answer to '{question}' is not known.",
                   name: "response_questions",
                   description: "Respond to questions.");

            var options = HyperlightCodeActProviderOptions.CreateForWasm(PythonGuestModule.GetModulePath());
            options.Tools = [fetchDocs, queryData, sendEmail, responseToQuestions];

            using var executeCode = new HyperlightCodeActProvider(options);

            var instructions =
                "You are a helpful assistant. " +
                "When the user asks something quantitative, write Python and call `execute_code` instead of guessing. " +
                "Prefer orchestrating your work in a single `execute_code` block using `call_tool(...)` over issuing many direct tool calls.";

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
                        Instructions = instructions
                    },
                    AIContextProviders = [executeCode],
                });


            Console.WriteLine(await agent.RunAsync("Respond to question : whar is the sense of life ?"));
            Console.WriteLine(await agent.RunAsync("What is the sum of 2 and 3 ? Write Python and call `execute_code` instead of guessing."));
            //Console.WriteLine(await agent.RunAsync("Look up docs on 'retries' and query the 'orders' table, then summarize."));
            Console.WriteLine(await agent.RunAsync("Calculate 2 + 3. " +
                "Send an email to phil@gmail.com with the subject 'cool' with the send_email tool from within `execute_code`."));
        }
    }
}

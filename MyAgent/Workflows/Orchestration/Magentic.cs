using System;
using System.Collections.Generic;
using System.Text;
using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using Microsoft.Agents.AI.Workflows;
using Microsoft.Agents.AI.Workflows.Specialized.Magentic;
using Microsoft.Extensions.AI;

namespace MyAgent.Workflows.Orchestration;

/// <summary>
/// Demonstrates Magentic orchestration with a researcher, a coder, and an LLM manager.
/// </summary>
/// <remarks>
/// Pre-requisites:
/// - A Microsoft Foundry project endpoint and model deployment must be configured.
/// - Run <c>az login</c> before executing the sample.
/// </remarks>
public class Magentic
{
    private const string TaskPrompt =
        "I am preparing a report on the energy efficiency of different machine learning model architectures. " +
        "Compare the estimated training and inference energy consumption of ResNet-50, BERT-base, and GPT-2 " +
        "on standard datasets (e.g., ImageNet for ResNet, GLUE for BERT, WebText for GPT-2). " +
        "Then, estimate the CO2 emissions associated with each, assuming training on an Azure Standard_NC6s_v3 " +
        "VM for 24 hours. Provide tables for clarity, and recommend the most energy-efficient model " +
        "per task type (image classification, text classification, and text generation).";

    public async Task Test()
    {
        string endpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT")
            ?? throw new InvalidOperationException("FOUNDRY_PROJECT_ENDPOINT is not set.");
        string deploymentName = Environment.GetEnvironmentVariable("FOUNDRY_MODEL") ?? "gpt-5.4-mini";

        // WARNING: DefaultAzureCredential is convenient for development but requires careful consideration in production.
        // In production, consider using a specific credential (e.g., ManagedIdentityCredential) to avoid
        // latency issues, unintended credential probing, and potential security risks from fallback mechanisms.
        AIProjectClient projectClient = new(new Uri(endpoint), new DefaultAzureCredential());

        AIAgent researcherAgent = projectClient.AsAIAgent(
            deploymentName,
            name: "ResearcherAgent",
            description: "Specialist in research and information gathering.",
            instructions: "You are a researcher. Find relevant information without doing additional computation or quantitative analysis.");

        AIAgent coderAgent = projectClient.AsAIAgent(
            deploymentName,
            name: "CoderAgent",
            description: "A helpful assistant that writes and executes code to analyze data.",
            instructions: "You solve quantitative questions by writing and running code. Show the analysis and the computation process clearly.",
            tools: [new HostedCodeInterpreterTool()]);

        AIAgent managerAgent = projectClient.AsAIAgent(
            deploymentName,
            name: "MagenticManager",
            description: "Orchestrator that coordinates the research and coding workflow.",
            instructions: "You coordinate the team to complete complex tasks efficiently.");

        Workflow workflow = new MagenticWorkflowBuilder(managerAgent)
            .AddParticipants([researcherAgent, coderAgent])
            .WithName("Magentic Orchestration Workflow")
            .WithDescription("Coordinates a researcher and coder to solve a complex analytical task.")
            // By default the manager's internally generated messages (task ledger, progress ledger, final answer)
            // use the built-in English prompts. To have them written in another language, pin a concrete language:
            //     .WithResponseLanguage("French")
            // For full control you can also override any of the internal prompt templates (placeholders such as
            // {task}, {team}, and - for the progress ledger - {schema} are substituted by the framework):
            //     .WithPromptOverrides(new MagenticPromptOverrides
            //     {
            //         FinalAnswerPrompt = "Rédige la réponse finale à la demande suivante en français :\n{task}",
            //     })
            // The built-in English templates you can copy and translate are published on MagenticDefaultPrompts
            // (e.g. MagenticDefaultPrompts.ProgressLedgerPrompt) - use them as a starting point for your overrides.
            .RequirePlanSignoff(false)
            .WithMaxRounds(10)
            .WithMaxStalls(3)
            .WithMaxResets(2)
            .Build();

        Console.WriteLine("Building Magentic workflow...");
        Console.WriteLine();
        Console.WriteLine($"Task: {TaskPrompt}");
        Console.WriteLine();
        Console.WriteLine("Starting workflow execution...");

        await using StreamingRun run = await InProcessExecution.RunStreamingAsync(
            workflow,
            new List<ChatMessage> { new(ChatRole.User, TaskPrompt) });

        await run.TrySendMessageAsync(new TurnToken(emitEvents: true));

        string? lastResponseId = null;
        WorkflowOutputEvent? finalOutput = null;

        await foreach (WorkflowEvent workflowEvent in run.WatchStreamAsync())
        {
            switch (workflowEvent)
            {
                case AgentResponseUpdateEvent updateEvent:
                    WriteStreamingUpdate(updateEvent, ref lastResponseId);
                    break;

                case MagenticPlanCreatedEvent planCreated:
                    WriteMagenticMessage("Initial Plan", planCreated.FullTaskLedger.Text);
                    PauseIfInteractive();
                    break;

                case MagenticReplannedEvent replanned:
                    WriteMagenticMessage("Replanned", replanned.FullTaskLedger.Text);
                    PauseIfInteractive();
                    break;

                case MagenticProgressLedgerUpdatedEvent progressUpdated:
                    WriteMagenticMessage("Progress Ledger", FormatProgressLedger(progressUpdated.ProgressLedger));
                    PauseIfInteractive();
                    break;

                case WorkflowOutputEvent outputEvent when outputEvent.Is<List<ChatMessage>>():
                    finalOutput = outputEvent;
                    break;

                case WorkflowErrorEvent workflowError:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine(workflowError.Exception?.ToString() ?? "Unknown workflow error occurred.");
                    Console.ResetColor();
                    break;

                case ExecutorFailedEvent executorFailed:
                    Console.ForegroundColor = ConsoleColor.Red;
                    Console.Error.WriteLine($"Executor '{executorFailed.ExecutorId}' failed with {(executorFailed.Data is null ? "unknown error" : $"exception {executorFailed.Data}")}.");
                    Console.ResetColor();
                    break;
            }
        }

        if (finalOutput?.As<List<ChatMessage>>() is { } transcript)
        {
            Console.WriteLine();
            Console.WriteLine(new string('=', 80));
            Console.WriteLine();
            Console.WriteLine("Final Conversation Transcript:");
            Console.WriteLine();

            foreach (ChatMessage message in transcript)
            {
                Console.WriteLine($"{message.AuthorName ?? message.Role.ToString()}: {message.Text}");
                Console.WriteLine();
            }
        }
    }

    /// <summary>
    /// Écrit une mise à jour de streaming dans la console en regroupant la sortie par identifiant de réponse.
    /// </summary>
    /// <remarks>Ajoute une séparation visuelle lors d’un changement d’identifiant de réponse, puis écrit le
    /// texte uniquement lorsqu’il est non vide.</remarks>
    /// <param name="updateEvent">Événement contenant l’exécuteur, les identifiants de message/réponse et le fragment de texte à afficher.</param>
    /// <param name="lastResponseId">Identifiant de la dernière réponse affichée, mis à jour pour détecter un changement de réponse entre deux
    /// événements.</param>
    private void WriteStreamingUpdate(AgentResponseUpdateEvent updateEvent, ref string? lastResponseId)
    {
        string responseId = updateEvent.Update.ResponseId ?? updateEvent.Update.MessageId ?? updateEvent.ExecutorId;
        if (!string.Equals(responseId, lastResponseId, StringComparison.Ordinal))
        {
            if (lastResponseId is not null)
            {
                Console.WriteLine();
                Console.WriteLine();
            }

            Console.Write($"- {updateEvent.ExecutorId}: ");
            lastResponseId = responseId;
        }

        if (!string.IsNullOrEmpty(updateEvent.Update.Text))
        {
            Console.Write(updateEvent.Update.Text);
        }
    }

    /// <summary>
    /// Écrit dans la console un en-tête de message Magentic et son contenu.
    /// </summary>
    /// <param name="title">Titre affiché dans l’en-tête du message Magentic.</param>
    /// <param name="content">Contenu du message à afficher ; peut être null.</param>
    private void WriteMagenticMessage(string title, string? content)
    {
        Console.WriteLine();
        Console.WriteLine($"[Magentic {title}]");
        Console.WriteLine(content);
    }

    /// <summary>
    /// Formats a progress ledger as a multi-line, human-readable status report.
    /// </summary>
    /// <param name="ledger">The progress ledger containing the current request status, loop state, progress state, next speaker, and
    /// instruction or question.</param>
    /// <returns>A string with one line per ledger field, including request satisfaction, loop status, progress status, next
    /// speaker, and instruction.</returns>
    private string FormatProgressLedger(MagenticProgressLedger ledger) =>
        string.Join(Environment.NewLine,
            $"Request satisfied: {ledger.IsRequestSatisfied}",
            $"In loop: {ledger.IsInLoop}",
            $"Making progress: {ledger.IsProgressBeingMade}",
            $"Next speaker: {ledger.NextSpeaker}",
            $"Instruction: {ledger.InstructionOrQuestion}");

    /// <summary>
    /// Met l’exécution en pause jusqu’à l’appui sur Entrée 
    /// lorsque l’entrée et la sortie de la console ne sont pas redirigées.
    /// </summary>
    /// <remarks>N’affiche l’invite et n’attend la saisie que dans un contexte interactif.</remarks>
    private void PauseIfInteractive()
    {
        if (Console.IsInputRedirected || Console.IsOutputRedirected)
        {
            return;
        }

        Console.Write("Press Enter to continue...");
        Console.ReadLine();
        Console.WriteLine();
    }
}

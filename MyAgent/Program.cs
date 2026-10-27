// Copyright (c) Microsoft. All rights reserved.

// This sample shows how to create and use a simple AI agent with AIProjectClient as the backend.



using Azure.AI.Projects;
using Azure.Identity;
using Microsoft.Agents.AI;
using MyAgent.AllAgents.Agents;
using MyAgent.AllAgents.AgentWithCodeAct;
using MyAgent.AllAgents.AgentWithMemory;
using MyAgent.AllAgents.Evaluation;
using MyAgent.AllAgents.RAG;
using MyAgent.GetStarted;
using MyAgent.Workflows;
using MyAgent.Workflows.Concurrent;
using MyAgent.Workflows.GetStarted;
using MyAgent.Workflows.HumanInTheLoop;
using MyAgent.Workflows.Loop;
using MyAgent.Workflows.Orchestration;

namespace MyAgent;

public class Program
{
    public static async Task Main(string[] args)
    {
        var endpoint = Environment.GetEnvironmentVariable("FOUNDRY_PROJECT_ENDPOINT") ?? throw new InvalidOperationException("FOUNDRY_PROJECT_ENDPOINT is not set.");
        var model = Environment.GetEnvironmentVariable("FOUNDRY_MODEL") ?? "gpt-6-luna";

        // Get started
        if (false)
        {
            var hello = new HelloAgent();
            await hello.Test();

            var tool = new HelloTool();
            await tool.Test();

            var turn = new HelloTurn();
            await turn.Test();

            var memory = new HelloMemory();
            await memory.Test();

            var workflow = new HelloWorkflow();
            await workflow.Test();
        }

        // Agents
        if (false)
        {
            var functionToolsWithApprovals = new FunctionToolsWithApprovals();
            await functionToolsWithApprovals.Test();

            StructuredOutput structuredOutput = new StructuredOutput();
            await structuredOutput.Test();

            PersistedConversations persistedConversations = new PersistedConversations();
            await persistedConversations.Test();

            ThirdPartyChatHistoryStorage thirdPartyChatHistoryStorage = new ThirdPartyChatHistoryStorage();
            await thirdPartyChatHistoryStorage.Test();

            Observability o = new Observability();
            await o.Test();

            DependencyInjection d = new DependencyInjection();
            await d.Test(args);

            AsMcpTool asMcpTool = new AsMcpTool();
            await asMcpTool.Test(args);

            UsingImages usingImages = new UsingImages();
            await usingImages.Test(args);

            AsFunctionTool asFunctionTool = new AsFunctionTool();
            await asFunctionTool.Test(args);

            BackgroundResponsesWithToolsAndPersistence backgroundResponsesWithToolsAndPersistence = new BackgroundResponsesWithToolsAndPersistence();
            await backgroundResponsesWithToolsAndPersistence.Test();

            Middleware middleware = new Middleware();
            await middleware.Test();

            Plugins plugins = new Plugins();
            await plugins.Test(args);

            AdditionalAIContext additionalAIContext = new AdditionalAIContext();
            await additionalAIContext.Test();

            CompactionPipeline compactionPipeline = new CompactionPipeline();
            await compactionPipeline.Test();

            InFunctionLoopCheckpointing inFunctionLoopCheckpointing = new InFunctionLoopCheckpointing();
            await inFunctionLoopCheckpointing.Test(args);

            DynamicFunctionTools dynamicFunctionTools = new DynamicFunctionTools();
            await dynamicFunctionTools.Test();

            ShellWithEnvironment shellWithEnvironment = new ShellWithEnvironment();
            await shellWithEnvironment.Test();

            AgentMode agentMode = new AgentMode();
            await agentMode.Test();

            TodoList todoList = new TodoList();
            await todoList.Test();

            MultiModelRouting multiModelRouting = new MultiModelRouting();
            await multiModelRouting.Test();
        }

        // Agent With Code Act
        if (false)
        {
            Interpreter interpreter = new Interpreter();
            await interpreter.Test();

            ToolEnabled toolEnabled = new ToolEnabled();
            await toolEnabled.Test();

            ManualWiring manualWiring = new ManualWiring();
            await manualWiring.Test();
        }

        // Agent With Memory
        if (false)
        {
            ChatHistoryMemory chatHistoryMemory = new ChatHistoryMemory();
            await chatHistoryMemory.Test();

            FileMemoryProviderTest fileMemoryProviderTest = new FileMemoryProviderTest();
            await fileMemoryProviderTest.Test();

            MemoryUsingValkey memoryUsingValkey = new MemoryUsingValkey();
            await memoryUsingValkey.Test();
        }

        // Evaluation
        if (false)
        {
            Evaluation_SimpleEval evaluation_SimpleEval = new Evaluation_SimpleEval();
            await evaluation_SimpleEval.Test();

            Evaluation_CustomEvals evaluation_CustomEvals = new Evaluation_CustomEvals();
            await evaluation_CustomEvals.Test();

            Evaluation_Multimodal evaluation_Multimodal = new Evaluation_Multimodal();
            await evaluation_Multimodal.Test();

            Evaluation_ExpectedOutputs evaluation_ExpectedOutputs = new Evaluation_ExpectedOutputs();
            await evaluation_ExpectedOutputs.Test();
        }

        // RAG
        if (false)
        {
            BasicTextRAG basicTextRAG = new BasicTextRAG();
            await basicTextRAG.Test();

            CustomRAGDataSource customRAGDataSource = new CustomRAGDataSource();
            await customRAGDataSource.Test();
        }

        // Workflows
        if (false)
        {
            Workflow_01_Streaming workflow_01_Streaming = new Workflow_01_Streaming();
            await workflow_01_Streaming.Test();

            Workflow_02_AgentsInWorkflows workflow_02_AgentsInWorkflows = new Workflow_02_AgentsInWorkflows();
            await workflow_02_AgentsInWorkflows.Test();

            Workflow_03_AgentWorkflowPatterns workflow_03_AgentWorkflowPatterns = new Workflow_03_AgentWorkflowPatterns();
            await workflow_03_AgentWorkflowPatterns.Test();

            Workflow_05_SubWorkflows workflow_05_SubWorkflows = new Workflow_05_SubWorkflows();
            await workflow_05_SubWorkflows.Test();

            Workflow_06_MixedWorkflowAgentsAndExecutors workflow_06_MixedWorkflowAgentsAndExecutors = new Workflow_06_MixedWorkflowAgentsAndExecutors();
            await workflow_06_MixedWorkflowAgentsAndExecutors.Test();

            Workflow_07_WriterCriticWorkflow workflow_07_WriterCriticWorkflow = new Workflow_07_WriterCriticWorkflow();
            await workflow_07_WriterCriticWorkflow.Test();
        }

        // Loop
        if (false)
        {
            WorkflowLoopSample workflowLoopSample = new WorkflowLoopSample();
            await workflowLoopSample.Test();
        }

        // Orchestration
        if (false)
        {
            // Function tools with reasoning_effort are not supported for gpt-6-luna in /v1/chat/completions.
            // To use function tools, use /v1/responses or set reasoning_effort to 'none'.

            Handoff handoff = new Handoff();
            await handoff.Test();

            Magentic magentic = new Magentic();
            await magentic.Test();
        }

        // HumanInTheLoop
        if (false)
        {
            HumanInTheLoopBasic humanInTheLoopBasic = new HumanInTheLoopBasic();
            await humanInTheLoopBasic.Test();
        }

        // Concurrent
        if (false)
        {
        }

        WorkflowConcurrentSample workflowConcurrentSample = new WorkflowConcurrentSample();
        await workflowConcurrentSample.Test();
    }
}
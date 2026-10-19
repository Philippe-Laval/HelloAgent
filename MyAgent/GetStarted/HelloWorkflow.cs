using Microsoft.Agents.AI.Workflows;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyAgent.GetStarted
{
    /// <summary>
    /// This sample introduces the concepts of executors and edges in a workflow.
    ///
    /// Workflows are built from executors (processing units) connected by edges (data flow paths).
    /// In this example, we create a simple text processing pipeline that:
    /// 1. Takes input text and converts it to uppercase using an UppercaseExecutor
    /// 2. Takes the uppercase text and reverses it using a ReverseTextExecutor
    ///
    /// The executors are connected sequentially, so data flows from one to the next in order.
    /// For input "Hello, World!", the workflow produces "!DLROW ,OLLEH".
    /// </summary>
    public class HelloWorkflow
    {
        public async Task Test()
        {
            // Create the executors
            Func<string, string> uppercaseFunc = s => s.ToUpperInvariant();
            var uppercase = uppercaseFunc.BindAsExecutor("UppercaseExecutor");

            ReverseTextExecutor reverse = new();

            // Build the workflow by connecting executors sequentially
            WorkflowBuilder builder = new(uppercase);
            builder.AddEdge(uppercase, reverse).WithOutputFrom(reverse);
            var workflow = builder.Build();

            // Execute the workflow with input data
            await using Run run = await InProcessExecution.RunAsync(workflow, "Hello, World!");
            foreach (WorkflowEvent evt in run.NewEvents)
            {
                if (evt is ExecutorCompletedEvent executorComplete)
                {
                    Console.WriteLine($"{executorComplete.ExecutorId}: {executorComplete.Data}");
                }
            }
        }
    }
}

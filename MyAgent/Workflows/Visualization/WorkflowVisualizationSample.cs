using Microsoft.Agents.AI.Workflows;
using MyAgent.Workflows.Concurrent;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Runtime.Serialization;
using System.Text;

namespace MyAgent.Workflows.Visualization
{
    /// <summary>
    /// Sample demonstrating workflow visualization using Mermaid and DOT (Graphviz) formats.
    /// </summary>
    /// <remarks>
    /// This sample shows how to use the ToMermaidString() and ToDotString() extension methods
    /// to generate visual representations of workflow graphs. The visualizations can be used
    /// for documentation, debugging, and understanding complex workflow structures.
    /// </remarks>
    public partial class WorkflowVisualizationSample
    {
        /// <summary>
        /// Entry point that generates and displays workflow visualizations in Mermaid and DOT formats.
        /// </summary>
        /// <returns></returns>
        public async Task Test()
        {
            // Step 1: Build the workflow you want to visualize
            Workflow workflow = BuildWorkflow();

            // Step 2: Generate and display workflow visualization
            Console.WriteLine("Generating workflow visualization...");

            // Mermaid
            Console.WriteLine("Mermaid string: \n=======");
            var mermaid = workflow.ToMermaidString();
            Console.WriteLine(mermaid);
            Console.WriteLine("=======");

            // DOT
            Console.WriteLine("DiGraph string: *** Tip: To export DOT as an image, install Graphviz and pipe the DOT output to 'dot -Tsvg', 'dot -Tpng', etc. *** \n=======");
            var dotString = workflow.ToDotString();
            Console.WriteLine(dotString);
            Console.WriteLine("=======");
        }

        private Workflow BuildWorkflow()
        {
            var processor = new DataProcessor();
            var validator = new Validator();
            var formatter = new Formatter();

            // Build workflow
            WorkflowBuilder builder = new(processor); // Set starting executor
            builder.AddEdge(processor, validator);
            builder.AddEdge(validator, formatter);

            var workflow = builder.Build();
            return workflow;
        }


        internal sealed partial class DataProcessor() : Executor("DataProcessor")
        {
            [MessageHandler]
            private ValueTask<string> HandleAsync(string message, IWorkflowContext context)
            {
                string result = message.ToUpperInvariant();
                return ValueTask.FromResult(result); // Return value is automatically sent to connected executors
            }
        }

        internal sealed partial class Validator() : Executor("Validator")
        {
            [MessageHandler]
            private ValueTask<string> HandleAsync(string message, IWorkflowContext context)
            {
                string result = message.ToUpperInvariant();
                return ValueTask.FromResult(result); // Return value is automatically sent to connected executors
            }
        }

        internal sealed partial class Formatter() : Executor("Formatter")
        {
            [MessageHandler]
            private ValueTask<string> HandleAsync(string message, IWorkflowContext context)
            {
                string result = message.ToUpperInvariant();
                return ValueTask.FromResult(result); // Return value is automatically sent to connected executors
            }
        }

    }
}

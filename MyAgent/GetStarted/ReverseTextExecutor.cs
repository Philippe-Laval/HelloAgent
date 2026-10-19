using Microsoft.Agents.AI.Workflows;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyAgent.GetStarted
{
    /// <summary>
    /// Second executor: reverses the input text and completes the workflow.
    /// </summary>
    internal sealed class ReverseTextExecutor() : Executor<string, string>("ReverseTextExecutor")
    {
        /// <summary>
        /// Processes the input message by reversing the text.
        /// </summary>
        /// <param name="message">The input text to reverse</param>
        /// <param name="context">Workflow context for accessing workflow services and adding events</param>
        /// <param name="cancellationToken">The <see cref="CancellationToken"/> to monitor for cancellation requests.
        /// The default is <see cref="CancellationToken.None"/>.</param>
        /// <returns>The input text reversed</returns>
        public override ValueTask<string> HandleAsync(string message, IWorkflowContext context, CancellationToken cancellationToken = default)
        {
            // Because we do not suppress it, the returned result will be yielded as an output from this executor.
            return ValueTask.FromResult(string.Concat(message.Reverse()));
        }
    }
}

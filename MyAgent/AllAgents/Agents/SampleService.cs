using Microsoft.Agents.AI;
using Microsoft.Extensions.Hosting;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyAgent.AllAgents.Agents
{
    /// <summary>
    /// A sample service that uses an AI agent to respond to user input.
    /// </summary>
    internal sealed class SampleService(AIAgent agent, IHostApplicationLifetime appLifetime) : IHostedService
    {
        private AgentSession? _session;

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            // Create a session that will be used for the entirety of the service lifetime so that the user can ask follow up questions.
            this._session = await agent.CreateSessionAsync(cancellationToken);
            _ = this.RunAsync(appLifetime.ApplicationStopping);
        }

        public async Task RunAsync(CancellationToken cancellationToken)
        {
            // Delay a little to allow the service to finish starting.
            await Task.Delay(100, cancellationToken);

            while (!cancellationToken.IsCancellationRequested)
            {
                Console.WriteLine("\nAgent: Ask me to tell you a joke about a specific topic. To exit just press Ctrl+C or enter without any input.\n");
                Console.Write("> ");
                var input = Console.ReadLine();

                // If the user enters no input, signal the application to shut down.
                if (string.IsNullOrWhiteSpace(input))
                {
                    appLifetime.StopApplication();
                    break;
                }

                // Stream the output to the console as it is generated.
                await foreach (var update in agent.RunStreamingAsync(input, this._session, cancellationToken: cancellationToken))
                {
                    Console.Write(update);
                }

                Console.WriteLine();
            }
        }

        public Task StopAsync(CancellationToken cancellationToken) => Task.CompletedTask;
    }
}

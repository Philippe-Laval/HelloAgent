using Microsoft.Agents.AI;
using Microsoft.Extensions.AI;
using System;
using System.Collections.Generic;
using System.Text;

namespace MyAgent.GetStarted
{
    /// <summary>
    /// Sample memory component that can remember a user's name and age.
    /// </summary>
    internal sealed class UserInfoMemory : AIContextProvider
    {
        private readonly ProviderSessionState<UserInfo> _sessionState;
        private IReadOnlyList<string>? _stateKeys;
        private readonly IChatClient _chatClient;

        public UserInfoMemory(IChatClient chatClient, Func<AgentSession?, UserInfo>? stateInitializer = null)
        {
            this._sessionState = new ProviderSessionState<UserInfo>(
                stateInitializer ?? (_ => new UserInfo()),
                this.GetType().Name);
            this._chatClient = chatClient;
        }

        public override IReadOnlyList<string> StateKeys => this._stateKeys ??= [this._sessionState.StateKey];

        /// <summary>
        /// Gets the user info for the given session. 
        /// If no user info exists, a new instance will be created and returned.
        /// </summary>
        /// <param name="session"></param>
        /// <returns></returns>
        public UserInfo GetUserInfo(AgentSession session)
            => this._sessionState.GetOrInitializeState(session);

        /// <summary>
        /// Sets the user info for the given session.
        /// </summary>
        /// <param name="session"></param>
        /// <param name="userInfo"></param>
        public void SetUserInfo(AgentSession session, UserInfo userInfo)
            => this._sessionState.SaveState(session, userInfo);

        /// <summary>
        /// Stores the user info in the session state. 
        /// If the user info is not already known, it will attempt to 
        /// extract it from the user's messages using the chat client.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        /// <exception cref="InvalidOperationException"></exception>
        protected override async ValueTask StoreAIContextAsync(InvokedContext context, CancellationToken cancellationToken = default)
        {
            var userInfo = this._sessionState.GetOrInitializeState(context.Session);

            // Try and extract the user name and age from the message if we don't have it already and it's a user message.
            if ((userInfo.UserName is null || userInfo.UserAge is null) && context.RequestMessages.Any(x => x.Role == ChatRole.User))
            {
                // The Foundry Responses API requires the model name in the request body.
                // Retrieve it from the client's metadata so callers don't need to pass it separately.
                var modelId = this._chatClient.GetService<ChatClientMetadata>()?.DefaultModelId
                    ?? throw new InvalidOperationException(
                        "Could not retrieve DefaultModelId from the extraction IChatClient. " +
                        "Ensure the client was created with a model ID (e.g., via projectClient.AsAIAgent(...)).");
                var result = await this._chatClient.GetResponseAsync<UserInfo>(
                    context.RequestMessages,
                    new ChatOptions()
                    {
                        ModelId = modelId,
                        Instructions = "Extract the user's name and age from the message if present. If not present return nulls."
                    },
                    cancellationToken: cancellationToken);

                userInfo.UserName ??= result.Result.UserName;
                userInfo.UserAge ??= result.Result.UserAge;
            }

            this._sessionState.SaveState(context.Session, userInfo);
        }

        /// <summary>
        /// Provides the user info in the AI context. 
        /// If the user info is not already known, it will instruct the agent to ask for it.
        /// </summary>
        /// <param name="context"></param>
        /// <param name="cancellationToken"></param>
        /// <returns></returns>
        protected override ValueTask<AIContext> ProvideAIContextAsync(InvokingContext context, CancellationToken cancellationToken = default)
        {
            var userInfo = this._sessionState.GetOrInitializeState(context.Session);

            StringBuilder instructions = new();

            // If we don't already know the user's name and age, add instructions to ask for them, otherwise just provide what we have to the context.
            instructions
                .AppendLine(
                    userInfo.UserName is null ?
                        "Ask the user for their name and politely decline to answer any questions until they provide it." :
                        $"The user's name is {userInfo.UserName}.")
                .AppendLine(
                    userInfo.UserAge is null ?
                        "Ask the user for their age and politely decline to answer any questions until they provide it." :
                        $"The user's age is {userInfo.UserAge}.");

            return new ValueTask<AIContext>(new AIContext
            {
                Instructions = instructions.ToString()
            });
        }
    }
}

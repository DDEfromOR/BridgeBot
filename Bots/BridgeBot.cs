// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Schema;
using Microsoft.Bot.StreamingExtensions;
using Microsoft.Bot.StreamingExtensions.Transport.NamedPipes;
using Microsoft.Bot.StreamingExtensions.Transport.WebSockets;

namespace Microsoft.Bot.Builder.BridgeBot
{
    public class BridgeBot : ActivityHandler
    {
        private static WebSocketClient _webSocketClient;
        private static NamedPipeClient _namedPipeClient;
        private static Dictionary<string, ITurnContext> _turnContexts = new Dictionary<string, ITurnContext>();
        private BridgeRequestHandler _handler;
        private bool _useNamedPipes;
        private bool _useWebSockets;
        private readonly string _TargetBotPipeName = "bfv4.pipes";
        private readonly string _TargetBotEndPoint;
        private readonly string _TargetBotId;
        private readonly string _TargetBotPassword;

        public BridgeBot()
        {
            _handler = new BridgeRequestHandler((activity) =>
            {
                if (_turnContexts.TryGetValue(activity.Conversation.Id, out var value))
                {
                    value.SendActivityAsync(activity).Wait();
                }
            });

            // Named Pipe bots do not require authentication, as a named pipe connection must originate from the same 
            // machine/instance as the bot itself.
            // This version of BridgeBot can only support one bot connection at at a time, so one of these options must always be false.
            this._useNamedPipes = false; // Set to true to connect to a named pipe bot.
            this._useWebSockets = true; // Set to true to connect to WebSocket bot.

            this._TargetBotEndPoint = "ws://localhost:3978/api/messages"; // IF USING WEBSOCKETS REPLACE THIS WITH YOUR TARGET BOT'S ENDPOINT
            this._TargetBotPipeName = "bfv4.pipes"; // IF USING NAMED PIPES REPLACE THIS WITH THE NAMED PIPE YOUR TARGET BOT LISTENS ON

            // If using WebSockets replace the below with the target bot's MicrosoftAppId and MicrosoftAppPassword, which can be found in it's appsettings.json
            // BridgeBot requires these fields not be blank, but if your target bot has authentication disabled the values will not matter.
            this._TargetBotId = "REPLACE WITH BOT ID";
            this._TargetBotPassword = "REPLACE WITH BOT PASSWORD";

            if (!this._useWebSockets && !this._useNamedPipes)
            {
                // No bot has been set to bridge to, so there is nothing for BridgeBot to do.

                return;
            }

            if (this._useWebSockets && this._useNamedPipes)
            {
                // This version of BridgeBot can only bridge to one bot at this point, so one of the above should be false;

                return;
            }

            if (this._useNamedPipes)
            {
                SetupNamedPipeConnection();
            }
            if (this._useWebSockets)
            {
                SetupWebSocketConnection(this._TargetBotId, this._TargetBotPassword);
            }
        }

        private void SetupWebSocketConnection(string botId, string botPassword)
        {
            if (!_useWebSockets)
            {
                return;
            }
            Connector.Authentication.MicrosoftAppCredentials appCredentials = new Connector.Authentication.MicrosoftAppCredentials(botId, botPassword);
            var authHeaders = new Dictionary<string, string>() { { "authorization", $"Bearer {appCredentials.GetTokenAsync().Result}" }, { "channelid", "emulator" } };

            // Bots hosted on Azure will require the authHeaders be passed in as the third argument below. For local testing they are optional and may cause undesired behavior.
            // Setting the channelId to emulator causes the BotFrameworkAdapter in the target bot to treat activities differently. Search for the string 'emulator' in
            // the BotBuilder repo to learn more.
            _webSocketClient = new WebSocketClient(this._TargetBotEndPoint, _handler, null);
            _webSocketClient.ConnectAsync(authHeaders).Wait();
        }

        private void SetupNamedPipeConnection()
        {
            if (!_useNamedPipes)
            {
                return;
            }
            _namedPipeClient = new NamedPipeClient(this._TargetBotPipeName, _handler);
            _namedPipeClient.ConnectAsync();
        }

        protected override async Task OnMessageActivityAsync(ITurnContext<IMessageActivity> turnContext, CancellationToken cancellationToken)
        {
            //proxy the message over to the streaming bot
            var request = new StreamingRequest()
            {
                Verb = "POST",
                Path = "/api/messages"
            };
            turnContext.Activity.ServiceUrl = "urn:BridgeBot:ws://localhost";
            request.SetBody(turnContext.Activity);

            if (_useNamedPipes)
            {
                await _namedPipeClient.SendAsync(request).ConfigureAwait(false);
            }

            if (_useWebSockets)
            {
                await _webSocketClient.SendAsync(request).ConfigureAwait(false);
            }
        }

        protected override async Task OnMembersAddedAsync(IList<ChannelAccount> membersAdded, ITurnContext<IConversationUpdateActivity> turnContext, CancellationToken cancellationToken)
        {
            foreach (var member in membersAdded)
            {
                if (member.Id != turnContext.Activity.Recipient.Id)
                {
                    if (!_turnContexts.ContainsKey(turnContext.Activity.Conversation.Id))
                    {
                        _turnContexts.TryAdd(turnContext.Activity.Conversation.Id, turnContext);
                    }
                    var request = new StreamingRequest()
                    {
                        Verb = "POST",
                        Path = "/api/messages"
                    };
                    turnContext.Activity.ServiceUrl = "urn:BridgeBot:ws://localhost";
                    request.SetBody(turnContext.Activity);

                    if (_useNamedPipes)
                    {
                        await _namedPipeClient.SendAsync(request).ConfigureAwait(false);
                    }

                    if (_useWebSockets)
                    {
                        await _webSocketClient.SendAsync(request).ConfigureAwait(false);
                    }
                }
            }
        }
    }
}

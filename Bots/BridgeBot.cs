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
        private bool _useNamedPipes = false;
        private bool _useWebSockets = true;

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
            this._useNamedPipes = false; // Set to true to connect to a named pipe bot.
            this._useWebSockets = true; // Set to true to connect to WebSocket bot.

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
                SetupWebSocketConnection("REPLACE WITH BOT ID", "REPLACE WITH BOT PASSWORD");
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
            _webSocketClient = new WebSocketClient("ws://localhost:3978/api/messages", _handler, null);
            _webSocketClient.ConnectAsync(authHeaders).Wait();
        }

        private void SetupNamedPipeConnection()
        {
            if (!_useNamedPipes)
            {
                return;
            }
            _namedPipeClient = new NamedPipeClient("bfv4.pipes", _handler);
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

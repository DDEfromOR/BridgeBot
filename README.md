# BridgeBot
BridgeBot is a bot troubleshooting and testing tool thought up by John Taylor, built by Swagat Misrha, and iterated on by Daniel Evans.

Currently BridgeBot is most useful in testing bots making use of the new Streaming Extensions libraries. 

To set BridgeBot up
- Open the BridgeBot.cs file
- On lines 40-41 set the appropriate flag for the connection type you are using.
- On lines 43-44 set the Endpoint or PipeName where BridgeBot will find the bot you are testing.
- On lines 47-48 set the ID and Password of the bot you want BridgeBot to connect to.

To use BridgeBot
- Launch the bot you want to test.
- Launch BridgeBot.
- Launch the awesome [BotFramework Emulator](https://github.com/microsoft/botframework-emulator) 
- Configure the Emulator with BridgeBot's endpoint (this can be changed in the bridge.bot file).
- Use the Emulator as normal, BridgeBot will forward messages to your target bot and then forward the responses back to the Emulator.

# Further reading
- [Bot Framework Documentation][20]
- [Bot Basics][32]
- [Azure Bot Service Introduction][21]
- [Azure Bot Service Documentation][22]
- [.NET Core CLI tools][23]
- [Azure CLI][7]
- [msbot CLI][9]
- [Azure Portal][10]
- [Language Understanding using LUIS][11]


[1]: https://dev.botframework.com
[4]: https://dotnet.microsoft.com/download
[5]: https://github.com/microsoft/botframework-emulator
[6]: https://github.com/Microsoft/BotFramework-Emulator/releases
[7]: https://docs.microsoft.com/en-us/cli/azure/?view=azure-cli-latest
[8]: https://docs.microsoft.com/en-us/cli/azure/install-azure-cli?view=azure-cli-latest
[9]: https://github.com/Microsoft/botbuilder-tools/tree/master/packages/MSBot
[10]: https://portal.azure.com
[11]: https://www.luis.ai
[20]: https://docs.botframework.com
[21]: https://docs.microsoft.com/en-us/azure/bot-service/bot-service-overview-introduction?view=azure-bot-service-4.0
[22]: https://docs.microsoft.com/en-us/azure/bot-service/?view=azure-bot-service-4.0
[23]: https://docs.microsoft.com/en-us/dotnet/core/tools/?tabs=netcore2x
[32]: https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-basics?view=azure-bot-service-4.0
[40]: https://aka.ms/azuredeployment


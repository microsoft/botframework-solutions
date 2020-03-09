## Bot Project
Bot project is the launcher project for the bots written in declarative form (JSON), using the Composer, for the Bot Framework SDK.

## Instructions for setting up the Bot Project runtime
The Bot Project is a regular Bot Framework SDK V4 project. Before you can launch it from the emulator, you need to make sure you can run the bot. 

### Prerequisite:
* Install .Netcore 2

### Commands:

* from root folder 
* cd BotProject
* cd CSharp
* dotnet restore // for the package updates
* dotnet build // build
* dotnet run // start the bot
* It will start a web server and listening at http://localhost:3979.

### Test bot
* You can set you emulator to connect to http://localhost:3979/api/messages.

### config your bot
This setup is required for local testing of your Bot Runtime. 
* The only thing you need to config is appsetting.json, which has a bot setting to launch the bot:

```
appsettings.json：
"bot": {
  "provider": "localDisk",
  "path": "../../Bots/SampleBot3/bot3.botproj"
}
```

## .botproj folder structure
```
bot.botproj, bot project got the rootDialog from "entry"
{
    "services": [{
        "type": "luis",
        "id": "1",
        "name": "TodoBotLuis",
        "lufile": "todo.lu",
        "applicationId": "TodoBotLuis.applicationId",
        "endpointKey": "TodoBotLuis.endpointKey",
        "endpoint": "TodoBotLuis.endpoint"
    }],
    "files": [
        "*.dialog",
        "*.lg"
    ],
    "entry": "main.dialog"
}
```
* Please refer to [Samples](https://github.com/Microsoft/BotFramework-Composer/tree/master/SampleBots) for more samples.

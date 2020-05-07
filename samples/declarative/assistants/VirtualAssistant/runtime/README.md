## Bot Runtime
Bot project is the launcher project for the bots written in declarative form (JSON), using the Composer, for the Bot Framework SDK.
This same code is used by Composer to start the bot locally for testing.

## Instructions for using and customizing the bot runtime

Composer can be configured to use a customized copy of this runtime.
A copy of it can be added to your project automatically by using the "runtime settings" page in Composer.

The Bot Project is a regular Bot Framework SDK V4 project. You can modify the code of this project
and continue to use it with Composer.

* Add additional middleware
* Customize the state storage system
* Add custom dialog classes

### Prerequisite:
* Install .Netcore 3.1

### Build:

* cd [my bot folder]/runtime
* dotnet user-secrets init // init the user secret id
* dotnet build // build


### Run from Command line:
* cd [my bot folder]/runtime
* dotnet run // start the bot
* It will start a web server and listening at http://localhost:3979.

### Run with Composer

Open your bot project in Composer. Navigate to the runtime settings tab. 

Set the path to runtime to the full path to your runtime code. Customize the start command as necessary.

The "Start Bot" button will now use your customized runtime.

Note: the application code must be built and ready to run before Composer can manage it.

### Test bot
* You can set you emulator to connect to http://localhost:3979/api/messages.


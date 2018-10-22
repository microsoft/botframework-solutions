# Virtual Assistant Known Issues

# Known Issues

We are tracking the following known issues on our backlog:

- The Virtual Assistant Visual Studio Project needs to have all Nuget Packages/Assemblies used by downstream skills added to the parent project.
- The Bot Framework Emulator doesn't provide the ability to control the UserId blocking testing of authentication scenarios within the emulator. The Web Test Harness provides a workaround for this.
- Skill registration only enables one Authentication Connection Name which doesn't enable the use of multiple credentials for a given skills (e.g. Microsoft and Google providers for the Email Skill)
- The LUIS model for ToDo is a placeholder, a more comprehensive language model is to follow.
# LUIS Model Generation

## Overview

We have different LUIS language models for skills and assistant. We need the representation in code for those language models. 

Currently the language models we have in our assistant are:

[Email.cs](https://github.com/Microsoft/AI/blob/master/solutions/Virtual-Assistant/src/csharp/skills/emailskill/Dialogs/Shared/Resources/Email.cs)
[Calendar.cs](https://github.com/Microsoft/AI/blob/master/solutions/Virtual-Assistant/src/csharp/skills/calendarskill/Dialogs/Shared/Resources/Calendar.cs)
[PointOfInterest.cs](https://github.com/Microsoft/AI/blob/master/solutions/Virtual-Assistant/src/csharp/skills/pointofinterestskill/Dialogs/Shared/Resources/PointOfInterest.cs)
[ToDo](https://github.com/Microsoft/AI/blob/master/solutions/Virtual-Assistant/src/csharp/skills/todoskill/Dialogs/Shared/Resources/ToDo.cs)
[Dispatch](https://github.com/Microsoft/AI/blob/master/solutions/Virtual-Assistant/src/csharp/assistant/Dialogs/Shared/Resources/Dispatch.cs)
[General](https://github.com/Microsoft/AI/blob/master/solutions/Virtual-Assistant/src/csharp/microsoft.bot.solutions/Resources/General.cs)

To generate the language model class, please use LuisGen tool: https://github.com/Microsoft/botbuilder-tools/tree/master/packages/LUISGen

### Generation

We're in the process of consolidating the LUIS model deployment steps so the instruction about LUIS language model generation will come after that's done.

### Note

After generation of the *.cs class, be sure to make this change:

change

`public _Entities Entities { get; set; }`

to

`public virtual _Entities Entities { get; set; }`

change

`public (Intent intent, double score) TopIntent()`

to 

`public virtual (Intent intent, double score) TopIntent()`

This change is to make sure we have the ability to override the `Entities` property and `TopIntent` function in the Mock luis models for test purposes. Example of a Mock luis model: [MockEmailIntent.cs](https://github.com/Microsoft/AI/blob/master/solutions/Virtual-Assistant/src/csharp/skills/tests/emailskilltest/Flow/Fakes/MockEmailIntent.cs)
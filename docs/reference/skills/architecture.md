## Skill Invocation Flow

All communication between a Virtual Assistant and a Skill is performed through a custom `SkillDialog`, started when the dispatcher identifies a Skill that maps to a users utterances. Skills are invoked through a lightweight `InProcAdapter`, maintaining the communication protocol and ensuring Skills can be developed using the standard Bot Framework toolkit.

`SkillDialog` bootstraps the `InProcAdapter` and processes appropriate middleware before invoking the `OnTurn` method on the Bot for each Activity. A `skillBegin` event is sent at the beginning of each Skill Dialog and the end of a Skill Dialog is marked by the sending of a `endOfConversation` event.

![Skill Invocation Flow](../../media/virtualassistant-SkillFlow.png)

## Dispatching Skills
When a user tries to trigger a Skill, the Virtual Assistant needs to know how to process that and correctly map to a registered Skill.
The [Dispatch](https://docs.microsoft.com/en-us/azure/bot-service/bot-builder-tutorial-dispatch?view=azure-bot-service-4.0) model identifies all registered Skill LUIS models and how it should be processed locally (through LUIS and code, QnA Maker, or by invoking a Skill with `SKillDialog`).

## Using the SkillDialog
Each Skill uses a `SkillDialog` class to manage it's invocation.
The Virtual Assistant identifies a Skill to use and creates a new `SkillDialog` instance with configuration properties as a parameter. 
Through reflection, the dialog instantiates the Skill and invokes the `OnTurn` handler to begin the Skill. 
Skills require a new state container, configured in your Virtual Assistant's configured state store, to ensure state is maintained at the highest level. 
This dialog is active on the Virtual Assistant's `DialogStack`, ensuring that subsequent utterances are routed to your Skill. 
When an `EndOfConversation` event is sent from the Skill, it tears down the `SkillDialog` and returns control back to the user.

## Interrupting Active Skills
Skills can be interrupted through a top-level interruption (e.g. "cancel"). The user is prompted to confirm before tearing down the active Skill.
using System.Threading;
using System.Threading.Tasks;
using AdaptiveCalendarSkill.Services;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Builder.LanguageGeneration.Generators;
using Microsoft.Bot.Connector;
using Microsoft.Bot.Schema;

/// <summary>
/// This dialog is the lowest level of all dialogs. 
/// </summary>
namespace AdaptiveCalendarSkill.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        public MainDialog(
            BotServices services,
            CreateDialog createDialog,
            ViewDialog viewDialog)
            : base(nameof(MainDialog))
        {
            // Create instance of adaptive dialog. 
            var adaptiveDialog = new AdaptiveDialog("root")
            {
                // Create a LUIS recognizer.
                Recognizer = services.CognitiveModelSets["en"].LuisRecognizers["Calendar"],
                Generator = new ResourceMultiLanguageGenerator("MainDialog.lg"),
                Triggers =
                {
                    new OnConversationUpdateActivity()
                    {
                        Actions = { new SendActivity("[Help-Root-Dialog]") }
                    },

                    /******************************************************************************/
                    // place to add new dialog
                    new OnIntent("CreateCalendarEntry") { Actions = { new ReplaceDialog(createDialog.Id) } },
                    new OnIntent("FindCalendarEntry")
                    {
                        Actions =
                        {
                            new SetProperty()
                            {
                                Property = "conversation.meetingListPage",// 0-based
                                Value = "0"
                            },
                            new BeginDialog(viewDialog.Id)
                        }
                    },

                    /******************************************************************************/
                    // Come back with LG template based readback for global help
                    new OnIntent(GeneralLuis.Intent.Help.ToString())
                    {
                        Actions = { new SendActivity("[Help-Root-Dialog]") },
                        Condition = "turn.dialogevent.value.intents.Help.score > 0.5"
                    },
                    new OnIntent(GeneralLuis.Intent.Cancel.ToString())
                    {
                        Actions =
                        {
                            new SendActivity("[Welcome-Actions]"),
                            new CancelAllDialogs(),
                        },
                        Condition = "turn.dialogevent.value.intents.Cancel.score > 0.5"
                    },
                    new OnDialogEvent()
                    {
                        Event = DialogEvents.RepromptDialog,
                        Actions = { new SendActivity("[Help-Root-Dialog]") }
                    }
                }
            };

            /******************************************************************************/
            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(adaptiveDialog);
            AddDialog(createDialog);
            AddDialog(viewDialog);

            /******************************************************************************/
            // The initial child Dialog to run.
            InitialDialogId = adaptiveDialog.Id;
        }

        protected override async Task<DialogTurnResult> EndComponentAsync(DialogContext outerDc, object result, CancellationToken cancellationToken)
        {
            // Send 'handoff' activity
            var response = outerDc.Context.Activity.CreateReply();
            response.Type = ActivityTypes.Handoff;
            await outerDc.Context.SendActivityAsync(response);

            return await base.EndComponentAsync(outerDc, result, cancellationToken);
        }
    }
}

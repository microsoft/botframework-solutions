using AdaptiveCalendarSkill.Services;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Builder.LanguageGeneration.Generators;

/// <summary>
/// This dialog is the lowest level of all dialogs. 
/// </summary>
namespace AdaptiveCalendarSkill.Dialogs
{
    public class MainDialog : ComponentDialog
    {
        public MainDialog(
            BotSettings settings,
            BotServices services,
            OAuthPromptDialog oauthDialog,
            CreateEntryDialog createDialog)
            : base(nameof(MainDialog))
        {
            // Create instance of adaptive dialog. 
            var adaptiveDialog = new AdaptiveDialog("root")
            {
                // Create a LUIS recognizer.
                Recognizer = services.CognitiveModelSets["en"].LuisServices["Calendar"],
                Generator = new ResourceMultiLanguageGenerator("RootDialog.lg"),
                Triggers =
                {
                    new OnConversationUpdateActivity()
                    {
                        Actions = { new SendActivity("[Help-Root-Dialog]") }
                    },

                    /******************************************************************************/
                    // place to add new dialog
                    new OnIntent("ScheduleMeeting")
                    {
                        Actions =
                        {
                            new BeginDialog(createDialog.Id)
                        },
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
                    }
                }
            };

            /******************************************************************************/
            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(adaptiveDialog);
            AddDialog(oauthDialog);
            AddDialog(createDialog);

            /******************************************************************************/
            // The initial child Dialog to run.
            InitialDialogId = adaptiveDialog.Id;
        }
    }
}

using AdaptiveCalendarSkill.Services;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Events;
using Microsoft.Bot.Builder.LanguageGeneration;

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
            CreateCalendarEntry createDialog,
            FindCalendarEntry findDialog,
            DeleteCalendarEntry deleteDialog,
            FindCalendarWho findWhoDialog,
            AcceptCalendarEntry acceptDialog,
            ShowNextCalendar showNextDialog,
            ChangeCalendarEntry changeDialog)
            : base(nameof(MainDialog))
        {
            // Create instance of adaptive dialog. 
            var adaptiveDialog = new AdaptiveDialog("root")
            {
                // Create a LUIS recognizer.
                Recognizer = services.CognitiveModelSets["en"].LuisServices["Calendar"],
                Generator = new ResourceMultiLanguageGenerator("RootDialog.lg"),
                Events =
                {
                    new OnConversationUpdateActivity()
                    {
                        Actions = { new SendActivity("[Help-Root-Dialog]") }
                    },

                    /******************************************************************************/
                    // place to add new dialog
                    new OnIntent(CalendarLuis.Intent.CreateCalendarEntry.ToString())
                    {
                        Actions =
                        {
                            new SetProperty()
                            {
                                Property = "user.CreateCalendarEntry_pageIndex", // 0-based
                                Value = "0"
                            },
                            new BeginDialog(createDialog.Id)
                        },
                        Constraint = "turn.dialogevent.value.intents.CreateCalendarEntry.score > 0.5"
                    },
                    new OnIntent(CalendarLuis.Intent.FindCalendarEntry.ToString())
                    {
                        Actions =
                        {
                            new SetProperty()
                            {
                                Property = "user.ShowAllMeetingDialog_pageIndex",// 0-based
                                Value = "0"
                            },
                            new BeginDialog(findDialog.Id)
                        },
                        Constraint = "turn.dialogevent.value.intents.FindCalendarEntry.score > 0.5"
                    },
                    new OnIntent(CalendarLuis.Intent.DeleteCalendarEntry.ToString())
                    {
                        Actions = { new BeginDialog(deleteDialog.Id) },
                        Constraint = "turn.dialogevent.value.intents.DeleteCalendarEntry.score > 0.5"
                    },
                    new OnIntent(CalendarLuis.Intent.FindCalendarWho.ToString())
                    {
                        Actions =
                        {
                             new SetProperty()
                             {
                                Property = "user.FindCalendarWho_pageIndex",// 0-based
                                Value = "0"
                             },
                            new BeginDialog(findWhoDialog.Id)
                        },
                        Constraint = "turn.dialogevent.value.intents.FindCalendarWho.score > 0.5"
                    },
                    new OnIntent(CalendarLuis.Intent.AcceptEventEntry.ToString())
                    {
                        Actions = { new BeginDialog(acceptDialog.Id) },
                        Constraint = "turn.dialogevent.value.intents.AcceptCalendarEntry.score > 0.5"
                    },
                    new OnIntent(CalendarLuis.Intent.ShowNextCalendar.ToString()){
                        Actions = { new BeginDialog(showNextDialog.Id) },
                        Constraint = "turn.dialogevent.value.intents.ShowNextCalendar.score > 0.5"
                    },
                    new OnIntent(CalendarLuis.Intent.ChangeCalendarEntry.ToString())
                    {
                        Actions = { new BeginDialog(changeDialog.Id) },
                        Constraint = "turn.dialogevent.value.intents.ChangeCalendarEntry.score > 0.5"
                    },

                    /******************************************************************************/
                    // Come back with LG template based readback for global help
                    new OnIntent(GeneralLuis.Intent.Help.ToString())
                    {
                        Actions = { new SendActivity("[Help-Root-Dialog]") },
                        Constraint = "turn.dialogevent.value.intents.Help.score > 0.5"
                    },
                    new OnIntent(GeneralLuis.Intent.Cancel.ToString())
                    {
                        Actions =
                        {
                            new SendActivity("[Welcome-Actions]"),
                            new CancelAllDialogs(),
                        },
                        Constraint = "turn.dialogevent.value.intents.Cancel.score > 0.5"
                    }
                }
            };

            /******************************************************************************/
            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(adaptiveDialog);
            AddDialog(oauthDialog);
            AddDialog(createDialog);
            AddDialog(findDialog);
            AddDialog(deleteDialog);
            AddDialog(findWhoDialog);
            AddDialog(acceptDialog);
            AddDialog(showNextDialog);
            AddDialog(changeDialog);

            /******************************************************************************/
            // The initial child Dialog to run.
            InitialDialogId = adaptiveDialog.Id;
        }
    }
}

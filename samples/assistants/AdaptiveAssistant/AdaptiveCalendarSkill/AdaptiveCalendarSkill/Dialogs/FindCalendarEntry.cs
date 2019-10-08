using AdaptiveCalendarSkill.Services;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Events;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.LanguageGeneration;

/// <summary>
/// This dialog will show all the calendar entries.
/// </summary>
namespace AdaptiveCalendarSkill.Dialogs
{
    public class FindCalendarEntry : ComponentDialog
    {
        public FindCalendarEntry(
            BotSettings settings,
            BotServices services,
            ShowAllMeetingsDialog showAllMeetingsDialog)
            : base(nameof(FindCalendarEntry))
        {
            var adaptiveDialog = new AdaptiveDialog("find")
            {
                Recognizer = services.CognitiveModelSets["en"].LuisServices["Calendar"],
                Generator = new ResourceMultiLanguageGenerator("FindCalendarEntry.lg"),
                Events =
                {
                    new OnBeginDialog()
                    {
                        Actions =
                        {
                            new BeginDialog(showAllMeetingsDialog.Id),
                            new ConfirmInput(){
                                Property = "turn.FindCalendarEntry_ConfirmChoice",
                                Prompt = new ActivityTemplate("[OverviewAgain]"),
                                InvalidPrompt = new ActivityTemplate("[YesOrNo]"),
                            },
                            new IfCondition()
                            {
                                Condition = "turn.FindCalendarEntry_ConfirmChoice",
                                Actions =
                                {
                                    new RepeatDialog()
                                },
                                ElseActions =
                                {
                                    new EndDialog()
                                }
                            }
                        }
                    },
                    new OnIntent(GeneralLuis.Intent.Help.ToString())
                    {
                        Actions = {  new SendActivity("[HelpViewMeeting]") }
                    },
                    new OnIntent(GeneralLuis.Intent.Cancel.ToString())
                    {
                        Actions =
                        {
                            new SendActivity("[CancelViewMeeting]"),
                            new CancelAllDialogs()
                        }
                    }
                }
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(adaptiveDialog);
            AddDialog(showAllMeetingsDialog);

            // The initial child Dialog to run.
            InitialDialogId = adaptiveDialog.Id;
        }
    }
}

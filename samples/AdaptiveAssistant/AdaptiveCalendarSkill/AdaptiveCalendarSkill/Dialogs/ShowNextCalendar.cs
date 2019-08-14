using System.Collections.Generic;
using AdaptiveCalendarSkill.Services;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Events;
using Microsoft.Bot.Builder.LanguageGeneration;

/// <summary>
/// This dialog will prompt user to log into calendar account
/// </summary>
namespace AdaptiveCalendarSkill.Dialogs
{
    public class ShowNextCalendar : ComponentDialog
    {
        public ShowNextCalendar(
            BotSettings settings,
            BotServices services,
            OAuthPromptDialog oauthDialog)
            : base(nameof(ShowNextCalendar))
        {
            var adaptiveDialog = new AdaptiveDialog("next")
            {
                Recognizer = services.CognitiveModelSets["en"].LuisServices["Calendar"],
                Generator = new ResourceMultiLanguageGenerator("ShowNextCalendar.lg"),
                Events =
                {
                    new OnBeginDialog()
                    {
                        Actions =
                        {
                            new BeginDialog(oauthDialog.Id),
                            new HttpRequest(){
                                Url = "https://graph.microsoft.com/v1.0/me/calendarview?startdatetime={utcNow()}&enddatetime={addDays(utcNow(),7)}", // next 7 days
                                Method = HttpRequest.HttpMethod.GET,
                                Headers =  new Dictionary<string, string>()
                                {
                                    ["Authorization"] = "Bearer {user.token.token}",
                                },
                                Property = "dialog.ShowNextCalendar_graphAll" // not sorted by start time already. :(
                            },
                            new IfCondition()
                            {
                                Condition = "dialog.ShowNextCalendar_graphAll != null", // to make sure that we have the next
                                Actions =
                                {
                                    new SetProperty()
                                    {
                                        Value = "dialog.ShowNextCalendar_graphAll.value[0]",
                                        Property = "dialog.nextFound"
                                    }
                                },
                                ElseActions =
                                {
                                    new SendActivity("[NoNext]"),
                                    new SendActivity("[Welcome-Actions]"),
                                    new EndDialog()
                                }
                            },
                            new SendActivity("[NextEventPre]"),
                            new SendActivity("[detailedEntryTemplate(dialog.nextFound)]"), // simple template now
                            new SendActivity("[Welcome-Actions]"),
                            new EndDialog()
                        }
                    }
                }
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(adaptiveDialog);
            AddDialog(oauthDialog);

            // The initial child Dialog to run.
            InitialDialogId = adaptiveDialog.Id;
        }
    }
}

using AdaptiveCalendarSkill.Services;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Events;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.LanguageGeneration;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;

/// <summary>
/// This dialog will accept all the calendar entris if they have the same subject
/// </summary>
namespace AdaptiveCalendarSkill.Dialogs
{
    public class AcceptCalendarEntry : ComponentDialog
    {
        public AcceptCalendarEntry(
            BotSettings settings,
            BotServices services,
            ShowAllMeetingsDialog showMeetingDialog)
            : base(nameof(AcceptCalendarEntry))
        {
            // Create instance of adaptive dialog. 
            var adaptiveDialog = new AdaptiveDialog("accept")
            {
                Recognizer = services.CognitiveModelSets["en"].LuisServices["Calendar"],
                Generator = new ResourceMultiLanguageGenerator("AcceptCalendarEntry.lg"),
                Events = {
                    new OnBeginDialog()
                    {
                        Actions =
                        {
                            new SendActivity("[EmptyFocusedMeeting]"),
                            new SetProperty()
                            {
                                Property = "user.ShowAllMeetingDialog_pageIndex",// index must be set to zero
                                Value = "0" // in case we have not entered FindCalendarEntry from RootDialog
                            },
                            new BeginDialog(showMeetingDialog.Id),
                            new IfCondition()
                            {
                                Condition = "user.focusedMeeting == null",
                                Actions = {
                                    new SendActivity("[EmptyCalendar]"),
                                    new EndDialog()
                                }
                            },
                            new ConfirmInput()
                            {
                                Property = "turn.AcceptCalendarEntry_ConfirmChoice",
                                Prompt = new ActivityTemplate("[ConfirmPrompt]"),
                                InvalidPrompt = new ActivityTemplate("[YesOrNo]"),
                            },
                            new IfCondition()
                            {
                                Condition = "turn.AcceptCalendarEntry_ConfirmChoice",
                                Actions =
                                {
                                    new IfCondition() // we cannot accept a entry if we are the origanizer
                                    {
                                        Condition = "user.focusedMeeting.isOrganizer != true",
                                        Actions =
                                        {
                                            new HttpRequest()
                                            {
                                                Property = "user.acceptResponse",
                                                Method = HttpRequest.HttpMethod.POST,
                                                Url = "https://graph.microsoft.com/v1.0/me/events/{user.focusedMeeting.id}/accept",
                                                Headers = new Dictionary<string, string>()
                                                {
                                                    ["Authorization"] = "Bearer {user.token.token}",
                                                },
                                                Body = JObject.Parse(@"{
                                                  '1': '1'
                                                }") // this is a place holder issue
                                            },
                                            new SendActivity("[AcceptReadBack]")
                                        },
                                        ElseActions = {
                                            new SendActivity("[CannotAcceptOrganizer]")
                                        }
                                    }
                                }
                            },
                            new SendActivity("[Welcome-Actions]"),
                            new EndDialog()
                        }
                    },
                    new OnIntent(GeneralLuis.Intent.Help.ToString())
                    {
                        Actions = { new SendActivity("[HelpAcceptMeeting]") }
                    },
                    new OnIntent(GeneralLuis.Intent.Cancel.ToString())
                    {
                        Actions =
                        {
                            new SendActivity("[CancelAcceptMeeting]"),
                            new CancelAllDialogs()
                        }
                    }
                }
            };

            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(adaptiveDialog);
            adaptiveDialog.AddDialog(showMeetingDialog);

            // The initial child Dialog to run.
            InitialDialogId = adaptiveDialog.Id;
        }
    }
}

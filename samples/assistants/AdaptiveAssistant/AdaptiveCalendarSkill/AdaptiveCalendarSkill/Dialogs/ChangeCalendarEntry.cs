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
    public class ChangeCalendarEntry : ComponentDialog
    {
        public ChangeCalendarEntry(
            BotSettings settings,
            BotServices services,
            ShowAllMeetingsDialog showAllMeetingsDialog)
            : base(nameof(ChangeCalendarEntry))
        {
            // Create instance of adaptive dialog. 
            var adaptiveDialog = new AdaptiveDialog("change")
            {
                Recognizer = services.CognitiveModelSets["en"].LuisServices["Calendar"],
                Generator = new ResourceMultiLanguageGenerator("ChangeCalendarEntry.lg"),
                Events =
                {
                    new OnBeginDialog()
                    {
                        Actions =
                        {
                            new SendActivity("[emptyFocusedMeeting]"),
                            new SetProperty()
                            {
                                Property = "user.ShowAllMeetingDialog_pageIndex",// index must be set to zero
                                Value = "0" // in case we have not entered FindCalendarEntry from RootDialog
                            },
                            new BeginDialog(showAllMeetingsDialog.Id),
                            new IfCondition()
                            {
                                Condition = "user.focusedMeeting == null",
                                Actions =
                                {
                                    new SendActivity("[EmptyCalendar]"),
                                    new EndDialog()
                                }
                            },
                            new ConfirmInput()
                            {
                                Property = "turn.ChangeCalendarEntry_ConfirmChoice",
                                Prompt = new ActivityTemplate("[UpdateConfirm]"),
                                InvalidPrompt = new ActivityTemplate("[YesOrNo]"),
                            },
                            new IfCondition()
                            {
                                Condition = "turn.ChangeCalendarEntry_ConfirmChoice",
                                Actions =
                                {
                                    new DateTimeInput()
                                    {
                                        Property = "dialog.ChangeCalendarEntry_startTime",
                                        Prompt = new ActivityTemplate("[GetStartTime]")
                                    },
                                    new HttpRequest()
                                    {
                                        Property = "user.updateResponse",
                                        Method = HttpRequest.HttpMethod.PATCH,
                                        Url = "https://graph.microsoft.com/v1.0/me/events/{user.focusedMeeting.id}",
                                        Headers =  new Dictionary<string, string>()
                                        {
                                            ["Authorization"] = "Bearer {user.token.token}",
                                        },
                                        Body = JObject.Parse(@"{
                                            'start': {
                                                'dateTime': '{formatDateTime(dialog.ChangeCalendarEntry_startTime[0].value, \'yyyy-MM-ddTHH:mm:ss\')}', 
                                                'timeZone': 'UTC'
                                            }
                                        }")
                                    },
                                    new IfCondition()
                                    {
                                        Condition = "user.updateResponse.error == null",
                                        Actions = new List<IDialog>
                                        {
                                            new SendActivity("[UpdateCalendarEntryReadBack]")
                                        },
                                        ElseActions = new List<IDialog>
                                    {
                                        new SendActivity("[UpdateCalendarEntryFailed]")
                                    }
                                },
                            }
                        },

                        // we cannot accept a entry if we are the organizer
                    
                        new SendActivity("[Welcome-Actions]"),
                        new EndDialog()
                    }
                    },
                    new OnIntent(GeneralLuis.Intent.Help.ToString())
                    {
                        Actions = { new SendActivity("[HelpUpdateMeeting]") }
                    },
                    new OnIntent(GeneralLuis.Intent.Cancel.ToString())
                    {
                        Actions =
                        {
                            new SendActivity("[CancelUpdateMeeting]"),
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

using AdaptiveCalendarSkill.Services;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Events;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

/// <summary>
/// Delete calendar entry is not functioning now because we could not use http.delete
/// </summary>
namespace AdaptiveCalendarSkill.Dialogs
{
    public class DeleteCalendarEntry : ComponentDialog
    {
        public DeleteCalendarEntry(
            BotSettings settings,
            BotServices services,
            AddContactDialog addContactDialog,
            ShowAllMeetingsDialog showAllMeetingsDialog)
            : base(nameof(DeleteCalendarEntry))
        {
            var adaptiveDialog = new AdaptiveDialog("delete")
            {
                Recognizer = services.CognitiveModelSets["en"].LuisServices["Calendar"],
                Generator = new ResourceMultiLanguageGenerator("DeleteCalendarEntry.lg"),
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
                                Actions ={
                                    new SendActivity("[EmptyCalendar]"),
                                    new EndDialog()
                                }
                            },
                            new ConfirmInput()
                            {
                                Property = "turn.DeleteCalendarEntry_ConfirmChoice",
                                Prompt = new ActivityTemplate("[DeclineConfirm]"),
                                InvalidPrompt = new ActivityTemplate("[]"),
                            },
                            new IfCondition()
                            {
                                Condition = "turn.DeleteCalendarEntry_ConfirmChoice",
                                Actions =
                                {
                                    new IfCondition()
                                    {
                                        Condition = "user.focusedMeeting.isOrganizer != true",// we cannot decline a entry if we are the origanizer
                                        Actions =
                                        {
                                            new HttpRequest()
                                            {
                                                Property = "user.declineResponse",
                                                Method = HttpRequest.HttpMethod.POST,
                                                Url = "https://graph.microsoft.com/v1.0/me/events/{user.focusedMeeting.id}/decline",
                                                Headers =  new Dictionary<string, string>()
                                                {
                                                    ["Authorization"] = "Bearer {user.token.token}",
                                                }
                                            },
                                            new SendActivity("[DeclineReadBack]")
                                        },
                                        ElseActions ={
                                            new SendActivity("[CannotDeclineOrganizer]"),
                                            new HttpRequest()
                                            {
                                                Property = "user.declineResponse",
                                                //Method = HttpRequest.HttpMethod.DELETE, // CANNOT DELETE NOT BECAUSE IT IS NOT USABLE NOW
                                                Url = "https://graph.microsoft.com/v1.0/me/events/{user.focusedMeeting.id}/delete",
                                                Headers =  new Dictionary<string, string>()
                                                {
                                                    ["Authorization"] = "Bearer {user.token.token}",
                                                }
                                            },
                                            new SendActivity("[DeleteReadBack]"),
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
                        Actions = { new SendActivity("[HelpDeleteMeeting]") }
                    },
                    new OnIntent(GeneralLuis.Intent.Cancel.ToString())
                    {
                        Actions =
                        {
                            new SendActivity("[CancelDeleteMeeting]"),
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

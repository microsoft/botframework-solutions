using AdaptiveCalendarSkill.Services;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Events;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.LanguageGeneration;
using System.Collections.Generic;

/// <summary>
/// This dialog will show all the calendar entries.
/// </summary>
namespace AdaptiveCalendarSkill.Dialogs
{
    public class ShowAllMeetingsDialog : ComponentDialog
    {
        public ShowAllMeetingsDialog(
            BotSettings settings,
            BotServices services,
            OAuthPromptDialog oauthDialog)
            : base(nameof(ShowAllMeetingsDialog))
        {
            var adaptiveDialog = new AdaptiveDialog("find")
            {
                Recognizer = services.CognitiveModelSets["en"].LuisServices["Calendar"],
                Generator = new ResourceMultiLanguageGenerator("ShowAllMeetingsDialog.lg"),
                Events =
                {
                    new OnBeginDialog()
                    {
                        Actions =
                        {
                            new BeginDialog(oauthDialog.Id),
                            new HttpRequest() {
                                Url = "https://graph.microsoft.com/v1.0/me/calendarView?startdatetime={utcNow()}&enddatetime={addDays(utcNow(), 1)}",
                                Method = HttpRequest.HttpMethod.GET,
                                Headers = new Dictionary<string, string>()
                                {
                                    ["Authorization"] = "Bearer {user.token.token}",
                                },
                                Property = "dialog.ShowAllMeetingDialog_GraphAll"
                            },
                            // to avoid shoing an empty calendar & access denied
                            new IfCondition()
                            {
                                Condition = "dialog.ShowAllMeetingDialog_GraphAll.value != null && count(dialog.ShowAllMeetingDialog_GraphAll.value) > 0",
                                Actions =
                                {
                                    new IfCondition()
                                    {
                                        Condition = "(user.ShowAllMeetingDialog_pageIndex*3+2) < count(dialog.ShowAllMeetingDialog_GraphAll.value)",
                                        Actions =
                                        {
                                            new SendActivity("[stitchedEntryTemplate(dialog.ShowAllMeetingDialog_GraphAll.value, user.ShowAllMeetingDialog_pageIndex*3, user.ShowAllMeetingDialog_pageIndex*3+3)]"),
                                        },
                                        ElseActions =
                                        {
                                            new SendActivity("[stitchedEntryTemplate(dialog.ShowAllMeetingDialog_GraphAll.value, user.ShowAllMeetingDialog_pageIndex*3, count(dialog.ShowAllMeetingDialog_GraphAll.value))]"),
                                        }
                                    },// TODO only simple card right now, will use fancy card then
                                    new TextInput(){
                                        Property = "turn.ShowAllMeetingDialog_Choice",
                                        Prompt = new ActivityTemplate("[ChoicePrompt]")
                                    }
                                },
                                ElseActions = new List<IDialog>
                                {
                                    new SendActivity("[NoEntries]"),
                                    new EndDialog()
                                }
                            }
                        }
                    },
                    new OnIntent(GeneralLuis.Intent.Help.ToString())
                    {
                        Actions = { new SendActivity("[HelpViewMeeting]") }
                    },
                    new OnIntent(GeneralLuis.Intent.Cancel.ToString())
                    {
                        Actions =
                        {
                            new SendActivity("[CancelViewMeeting]"),
                            new CancelAllDialogs()
                        }
                    },
                    new OnIntent(GeneralLuis.Intent.ShowPrevious.ToString())
                    {
                        Actions =
                        {
                            new IfCondition()
                            {
                                Condition = " 0 < user.ShowAllMeetingDialog_pageIndex",
                                Actions =
                                {
                                    new SetProperty()
                                    {
                                        Property = "user.ShowAllMeetingDialog_pageIndex",
                                        Value = "user.ShowAllMeetingDialog_pageIndex - 1"
                                    },
                                    new RepeatDialog()
                                },
                                ElseActions =
                                {
                                    new SendActivity("[FirstPage]"),
                                    new RepeatDialog()
                                }
                            }
                        }
                    },
                    new OnIntent(GeneralLuis.Intent.ShowNext.ToString())
                    {
                        Actions =
                        {
                            new IfCondition()
                            {
                                Condition = " user.ShowAllMeetingDialog_pageIndex*3+3<count(dialog.ShowAllMeetingDialog_GraphAll.value)",
                                Actions =
                                {
                                    new SetProperty()
                                    {
                                        Property = "user.ShowAllMeetingDialog_pageIndex",
                                        Value = "user.ShowAllMeetingDialog_pageIndex+1"
                                    },
                                    new RepeatDialog()
                                },
                                ElseActions =
                                {
                                    new SendActivity("[LastPage]"),
                                    new RepeatDialog()
                                }
                            }
                        }
                    },
                    new OnIntent(GeneralLuis.Intent.SelectItem.ToString())
                    {
                        Actions =
                        {
                            new SetProperty()
                            {
                                Value = "@ordinal",
                                Property = "turn.ShowAllMeetingDialog_ordinal"
                            },
                            new SetProperty()
                            {
                                Value = "@number",
                                Property = "turn.ShowAllMeetingDialog_number"
                            },
                            new IfCondition()
                            {
                                Condition = "turn.ShowAllMeetingDialog_ordinal != null",
                                Actions =
                                {
                                    new SwitchCondition()
                                    {
                                        Condition = "turn.ShowAllMeetingDialog_ordinal",
                                        Cases = new List<Case>()
                                        {
                                            new Case("1", new List<IDialog>()
                                                {
                                                    new IfCondition(){
                                                        Condition = "dialog.ShowAllMeetingDialog_GraphAll.value[user.ShowAllMeetingDialog_pageIndex * 3] != null",
                                                        Actions ={
                                                            new SendActivity("[detailedEntryTemplate(dialog.ShowAllMeetingDialog_GraphAll.value[user.ShowAllMeetingDialog_pageIndex * 3])]"),
                                                            new SetProperty()
                                                            {
                                                                Property = "user.focusedMeeting",
                                                                Value = "dialog.ShowAllMeetingDialog_GraphAll.value[user.ShowAllMeetingDialog_pageIndex * 3]"
                                                            }
                                                        },
                                                        ElseActions ={
                                                            new SendActivity("[ViewEmptyEntry]"),
                                                        }
                                                    }
                                                }),
                                            new Case("2", new List<IDialog>()
                                                {
                                                    new IfCondition(){
                                                        Condition = "dialog.ShowAllMeetingDialog_GraphAll.value[user.ShowAllMeetingDialog_pageIndex * 3 + 1] != null",
                                                        Actions ={
                                                            new SendActivity("[detailedEntryTemplate(dialog.ShowAllMeetingDialog_GraphAll.value[user.ShowAllMeetingDialog_pageIndex * 3 + 1])]"),
                                                            new SetProperty()
                                                            {
                                                                Property = "user.focusedMeeting",
                                                                Value = "dialog.ShowAllMeetingDialog_GraphAll.value[user.ShowAllMeetingDialog_pageIndex * 3 + 1]"
                                                            }
                                                        },
                                                        ElseActions ={
                                                            new SendActivity("[ViewEmptyEntry]")
                                                        }
                                                    }
                                                }),
                                            new Case("3", new List<IDialog>()
                                                {
                                                    new IfCondition(){
                                                        Condition = "dialog.ShowAllMeetingDialog_GraphAll.value[user.ShowAllMeetingDialog_pageIndex * 3 + 2] != null",
                                                        Actions ={
                                                            new SendActivity("[detailedEntryTemplate(dialog.ShowAllMeetingDialog_GraphAll.value[user.ShowAllMeetingDialog_pageIndex * 3 + 2])]"),
                                                            new SetProperty()
                                                            {
                                                                Property = "user.focusedMeeting",
                                                                Value = "dialog.ShowAllMeetingDialog_GraphAll.value[user.ShowAllMeetingDialog_pageIndex * 3 + 2]"
                                                            }
                                                        },
                                                        ElseActions ={
                                                            new SendActivity("[ViewEmptyEntry]")
                                                        }
                                                    }
                                                })
                                        },
                                        Default =
                                        {
                                            new SendActivity("[CannotUnderstand]"),
                                            new EndDialog()
                                        }
                                    },
                                    new EndDialog()
                                }
                            },
                            new IfCondition()
                            {
                                // prevent "select the third one"
                                Condition = "turn.ShowAllMeetingDialog_number != null && turn.ShowAllMeetingDialog_ordinal == null",
                                Actions =
                                {
                                    new SwitchCondition()
                                    {
                                        Condition = "turn.ShowAllMeetingDialog_number",
                                        Cases = new List<Case>()
                                        {
                                            new Case("1", new List<IDialog>()
                                                {
                                                    new IfCondition(){
                                                        Condition = "dialog.ShowAllMeetingDialog_GraphAll.value[user.ShowAllMeetingDialog_pageIndex * 3] != null",
                                                        Actions ={
                                                            new SendActivity("[detailedEntryTemplate(dialog.ShowAllMeetingDialog_GraphAll.value[user.ShowAllMeetingDialog_pageIndex * 3])]"),
                                                            new SetProperty()
                                                            {
                                                                Property = "user.focusedMeeting",
                                                                Value = "dialog.ShowAllMeetingDialog_GraphAll.value[user.ShowAllMeetingDialog_pageIndex * 3]"
                                                            }
                                                        },
                                                        ElseActions ={
                                                            new SendActivity("[ViewEmptyEntry]"),
                                                        }
                                                    }
                                                }),
                                            new Case("2", new List<IDialog>()
                                                {
                                                    new IfCondition(){
                                                        Condition = "dialog.ShowAllMeetingDialog_GraphAll.value[user.ShowAllMeetingDialog_pageIndex * 3 + 1] != null",
                                                        Actions ={
                                                            new SendActivity("[detailedEntryTemplate(dialog.ShowAllMeetingDialog_GraphAll.value[user.ShowAllMeetingDialog_pageIndex * 3 + 1])]"),
                                                            new SetProperty()
                                                            {
                                                                Property = "user.focusedMeeting",
                                                                Value = "dialog.ShowAllMeetingDialog_GraphAll.value[user.ShowAllMeetingDialog_pageIndex * 3 + 1]"
                                                            }
                                                        },
                                                        ElseActions ={
                                                            new SendActivity("[ViewEmptyEntry]")
                                                        }
                                                    }
                                                }),
                                            new Case("3", new List<IDialog>()
                                                {
                                                    new IfCondition(){
                                                        Condition = "dialog.ShowAllMeetingDialog_GraphAll.value[user.ShowAllMeetingDialog_pageIndex * 3 + 2] != null",
                                                        Actions ={
                                                            new SendActivity("[detailedEntryTemplate(dialog.ShowAllMeetingDialog_GraphAll.value[user.ShowAllMeetingDialog_pageIndex * 3 + 2])]"),
                                                            new SetProperty()
                                                            {
                                                                Property = "user.focusedMeeting",
                                                                Value = "dialog.ShowAllMeetingDialog_GraphAll.value[user.ShowAllMeetingDialog_pageIndex * 3 + 2]"
                                                            }
                                                        },
                                                        ElseActions ={
                                                            new SendActivity("[ViewEmptyEntry]")
                                                        }
                                                    }
                                                })
                                        },
                                        Default =
                                        {
                                            new SendActivity("[CannotUnderstand]"),
                                            new EndDialog()
                                        }
                                    },
                                    new EndDialog()
                                }
                            },
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

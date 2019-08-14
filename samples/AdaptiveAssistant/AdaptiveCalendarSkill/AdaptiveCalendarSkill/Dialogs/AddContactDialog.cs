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
/// This dialog will find all matched email given the contact name. It may be repeatedly call itself to add more contact.
/// Finally, this dialog will fulfill a desired contact list.
/// </summary>
namespace AdaptiveCalendarSkill.Dialogs
{
    public class AddContactDialog : ComponentDialog
    {
        public AddContactDialog(
            BotSettings settings,
            BotServices services)
            : base(nameof(AddContactDialog))
        {
            var adaptiveDialog = new AdaptiveDialog("addContact")
            {
                Recognizer = services.CognitiveModelSets["en"].LuisServices["Calendar"],
                Generator = new ResourceMultiLanguageGenerator("AddContactDialog.lg"),
                Events =
                {
                    new OnBeginDialog()
                    {
                        Actions =
                        {
                            new TextInput()
                            {
                                Property = "user.CreateCalendarEntry_PersonName",
                                Prompt = new ActivityTemplate("[GetPersonName]")
                            },
                            new TraceActivity(),
                            new HttpRequest()
                            {
                                Property = "dialog.AddContactDialog_UserAll",
                                Method = HttpRequest.HttpMethod.GET,
                                Url = "https://graph.microsoft.com/v1.0/me/contacts",
                                Headers = new Dictionary<string, string>()
                                {
                                    ["Authorization"] = "Bearer {user.token.token}",
                                }
                            },
                            new IfCondition()
                            {
                                Condition = "dialog.AddContactDialog_UserAll.value != null && count(dialog.AddContactDialog_UserAll.value) > 0",
                                Actions ={
                                    new Foreach()
                                    {
                                        ListProperty = "dialog.AddContactDialog_UserAll.value",
                                        Actions =
                                        {
                                            new IfCondition()
                                            {
                                                Condition = "contains(dialog.AddContactDialog_UserAll.value[dialog.index].displayName, user.CreateCalendarEntry_PersonName) == true ||" +
                                                    "contains(dialog.AddContactDialog_UserAll.value[dialog.index].emailAddresses[0].address, user.CreateCalendarEntry_PersonName) == true",
                                                Actions =
                                                {
                                                    new EditArray()
                                                    {
                                                        ArrayProperty = "dialog.matchedEmails",
                                                        ChangeType = EditArray.ArrayChangeType.Push,
                                                        Value = "dialog.AddContactDialog_UserAll.value[dialog.index].emailAddresses[0].address"
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            },
                            new IfCondition()
                            {
                                Condition = "dialog.matchedEmails != null && count(dialog.matchedEmails) > 0",
                                Actions =
                                {
                                    new IfCondition()
                                    {
                                        Condition = "user.CreateCalendarEntry_pageIndex * 3 + 2 < count(dialog.matchedEmails)",
                                        Actions = new List<IDialog>
                                        {
                                            new SendActivity("[StitchEmailTemplate(dialog.matchedEmails, user.CreateCalendarEntry_pageIndex * 3, user.CreateCalendarEntry_pageIndex * 3 + 3)]"),
                                        },
                                        ElseActions = new List<IDialog>
                                        {
                                            new SendActivity("[StitchEmailTemplate(dialog.matchedEmails, user.CreateCalendarEntry_pageIndex * 3, count(dialog.matchedEmails))]")
                                        }
                                    }, // TODO only simple card right now, will use fancy card then
                                    new TextInput()
                                    {
                                        Property = "turn.AddContactDialog_userChoice",
                                        Prompt = new ActivityTemplate("[ChoicePrompt]")
                                    }
                                },
                                ElseActions =
                                {
                                    new SendActivity("[NoMatches]"),
                                    new SetProperty()
                                    {
                                        Property = "user.finalContact",
                                        Value = "user.CreateCalendarEntry_PersonName",
                                        //Value = "concat(user.finalContact, '{\"emailAddress\":{ \"address\":\"', user.CreateCalendarEntry_PersonName, '\"}},')"
                                    }
                                },
                            },
                            // For multiple contacts use only 
                            //new IfCondition()
                            //{
                            //    Condition = "user.repeatFlag == true",
                            //    Actions ={
                            //        new ConfirmInput()
                            //        {
                            //            Property = "turn.AddContactDialog_ConfirmChoice",
                            //            Prompt = new ActivityTemplate("Do you want to add another contact?"),
                            //            InvalidPrompt = new ActivityTemplate("Please Say Yes/No."),
                            //        },
                            //        new IfCondition()
                            //        {
                            //            Condition = "turn.AddContactDialog_ConfirmChoice",
                            //            Actions =
                            //            {
                            //                new DeleteProperty()
                            //                {
                            //                    Property = "user.CreateCalendarEntry_PersonName"
                            //                },
                            //                new RepeatDialog()
                            //            },
                            //            ElseActions ={
                            //                new SendActivity("{user.finalContact}"),
                            //                new EndDialog()
                            //            }
                            //        }
                            //    }
                            //}
                        }
                    },
                    new OnIntent(GeneralLuis.Intent.Help.ToString())
                    {
                        Actions =
                        {
                            new SendActivity("[HelpCreateMeeting]")
                        }
                    },
                    new OnIntent(GeneralLuis.Intent.Cancel.ToString())
                    {
                        Actions =
                        {
                                new SendActivity("[CancelCreateMeeting]"),
                                new CancelAllDialogs()
                        }
                    },
                    new OnIntent(GeneralLuis.Intent.ShowPrevious.ToString())
                    {
                        Actions =
                        {
                            new IfCondition()
                            {
                                Condition = " 0 < user.AddContactDialog_pageIndex",
                                Actions =
                                {
                                    new SetProperty()
                                    {
                                        Property = "user.AddContactDialog_pageIndex",
                                        Value = "user.AddContactDialog_pageIndex - 1"
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
                                Condition = "user.AddContactDialog_pageIndex * 3 + 3 < count(dialog.matchedEmails) ",
                                Actions =
                                {
                                    new SetProperty()
                                    {
                                        Property = "user.AddContactDialog_pageIndex",
                                        Value = "user.AddContactDialog_pageIndex + 1"
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
                                Property = "turn.AddContactDialog_ordinal"
                            },
                            new SetProperty()
                            {
                                Value = "@number",
                                Property = "turn.AddContactDialog_number"
                            },
                            new IfCondition()
                            {
                                Condition = "turn.AddContactDialog_ordinal != null",
                                Actions =
                                {
                                    new SwitchCondition()
                                    {
                                        Condition = "turn.AddContactDialog_ordinal",
                                        Cases = new List<Case>()
                                        {
                                            new Case("1", new List<IDialog>()
                                                {
                                                    new IfCondition()
                                                    {
                                                        Condition = "dialog.matchedEmails[user.CreateCalendarEntry_pageIndex * 3] != null",
                                                        Actions =
                                                        {
                                                            new SetProperty()
                                                            {
                                                                Property = "user.finalContact",
                                                                Value = "dialog.matchedEmails[user.CreateCalendarEntry_pageIndex * 3]",
                                                                //Value = "concat(user.finalContact, '{\"emailAddress\":{ \"address\":\"', dialog.matchedEmails[user.CreateCalendarEntry_pageIndex * 3], '\"}},')"
                                                            },
                                                            new EndDialog()
                                                        },
                                                        ElseActions =
                                                        {
                                                            new SendActivity("[ViewEmptyEntry]"),
                                                            new RepeatDialog()
                                                        }
                                                    }
                                                }),
                                            new Case("2", new List<IDialog>()
                                                {
                                                    new IfCondition()
                                                    {
                                                        Condition = "dialog.matchedEmails[user.CreateCalendarEntry_pageIndex * 3 + 1] != null",
                                                        Actions =
                                                        {
                                                            new SetProperty()
                                                            {
                                                                Property = "user.finalContact",
                                                                 Value = "dialog.matchedEmails[user.CreateCalendarEntry_pageIndex * 3 + 1]",
                                                                 //Value = "concat(user.finalContact, '{\"emailAddress\":{ \"address\":\"', dialog.matchedEmails[user.CreateCalendarEntry_pageIndex * 3 + 1], '\"}},')"
                                                            },
                                                            new EndDialog()
                                                        },
                                                        ElseActions =
                                                        {
                                                            new SendActivity("[ViewEmptyEntry]"),
                                                            new RepeatDialog()
                                                        }
                                                    }
                                                }),
                                            new Case("3", new List<IDialog>()
                                                {
                                                    new IfCondition()
                                                    {
                                                        Condition = "dialog.matchedEmails[user.CreateCalendarEntry_pageIndex * 3 + 2] != null",
                                                        Actions =
                                                        {
                                                            new SetProperty()
                                                            {
                                                                Property = "user.finalContact",
                                                                Value = "dialog.matchedEmails[user.CreateCalendarEntry_pageIndex * 3 + 2]",
                                                                //Value = "concat(user.finalContact, '{\"emailAddress\":{ \"address\":\"', dialog.matchedEmails[user.CreateCalendarEntry_pageIndex * 3 + 2], '\"}},')"

                                                            },
                                                            new EndDialog()
                                                        },
                                                        ElseActions =
                                                        {
                                                            new SendActivity("[ViewEmptyEntry]"),
                                                            new RepeatDialog()
                                                        }
                                                    }
                                                })
                                        },
                                        Default =
                                        {
                                            new SendActivity("[CannotUnderstand]"),
                                            new EndDialog()
                                        }
                                    }
                                }
                            },
                            new IfCondition()
                            {
                                Condition = "turn.AddContactDialog_number != null && turn.AddContactDialog_ordinal == null",
                                Actions =
                                {
                                    new SwitchCondition()
                                    {
                                        Condition = "turn.AddContactDialog_number",
                                        Cases = new List<Case>()
                                        {
                                            new Case("1", new List<IDialog>()
                                                {
                                                    new IfCondition()
                                                    {
                                                        Condition = "dialog.matchedEmails[user.CreateCalendarEntry_pageIndex * 3] != null",
                                                        Actions =
                                                        {
                                                            new SetProperty()
                                                            {
                                                                Property = "user.finalContact",
                                                                Value = "dialog.matchedEmails[user.CreateCalendarEntry_pageIndex * 3]",
                                                                //Value = "concat(user.finalContact, '{\"emailAddress\":{ \"address\":\"', dialog.matchedEmails[user.CreateCalendarEntry_pageIndex * 3], '\"}},')"
                                                            },
                                                        },
                                                        ElseActions =
                                                        {
                                                            new SendActivity("[ViewEmptyEntry]"),
                                                            new RepeatDialog()
                                                        }
                                                    }
                                                }),
                                            new Case("2", new List<IDialog>()
                                                {
                                                    new IfCondition()
                                                    {
                                                        Condition = "dialog.matchedEmails[user.CreateCalendarEntry_pageIndex * 3 + 1] != null",
                                                        Actions =
                                                        {
                                                            new SetProperty()
                                                            {
                                                                Property = "user.finalContact",
                                                                 Value = "dialog.matchedEmails[user.CreateCalendarEntry_pageIndex * 3 + 1]",
                                                                 //Value = "concat(user.finalContact, '{\"emailAddress\":{ \"address\":\"', dialog.matchedEmails[user.CreateCalendarEntry_pageIndex * 3 + 1], '\"}},')"
                                                            }
                                                        },
                                                        ElseActions =
                                                        {
                                                            new SendActivity("[ViewEmptyEntry]"),
                                                            new RepeatDialog()
                                                        }
                                                    }
                                                }),
                                            new Case("3", new List<IDialog>()
                                                {
                                                    new IfCondition()
                                                    {
                                                        Condition = "dialog.matchedEmails[user.CreateCalendarEntry_pageIndex * 3 + 2] != null",
                                                        Actions =
                                                        {
                                                            new SetProperty()
                                                            {
                                                                Property = "user.finalContact",
                                                                Value = "dialog.matchedEmails[user.CreateCalendarEntry_pageIndex * 3 + 2]",
                                                                //Value = "concat(user.finalContact, '{\"emailAddress\":{ \"address\":\"', dialog.matchedEmails[user.CreateCalendarEntry_pageIndex * 3 + 2], '\"}},')"

                                                            }
                                                        },
                                                        ElseActions =
                                                        {
                                                            new SendActivity("[ViewEmptyEntry]"),
                                                            new RepeatDialog()
                                                        }
                                                    }
                                                })
                                        },
                                        Default =
                                        {
                                            new SendActivity("[CannotUnderstand]"),
                                            new EndDialog()
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            };
            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(adaptiveDialog);

            // The initial child Dialog to run.
            InitialDialogId = adaptiveDialog.Id;
        }
    }
}

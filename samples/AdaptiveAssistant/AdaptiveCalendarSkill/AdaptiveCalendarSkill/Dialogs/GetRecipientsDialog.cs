using AdaptiveAssistant.Actions;
using AdaptiveCalendarSkill.Services;
using Luis;
using Microsoft.Azure.KeyVault.Models;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Events;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.Expressions.Parser;
using Microsoft.Bot.Builder.LanguageGeneration;
using Microsoft.CodeAnalysis.CodeStyle;
using Microsoft.Graph;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace AdaptiveCalendarSkill.Dialogs
{
    public class GetRecipientsDialog : ComponentDialog
    {
        public GetRecipientsDialog(
            BotSettings settings,
            BotServices services)
            : base(nameof(GetRecipientsDialog))
        {
            var getAttendees = new AdaptiveDialog("adaptive")
            {
                Recognizer = services.CognitiveModelSets["en"].LuisServices["Calendar"],
                Generator = new ResourceMultiLanguageGenerator("AddContactDialog.lg"),
                Events =
                {
                    new OnBeginDialog()
                    {
                        Actions =
                        {
                            // Prompts for name or email
                            new ContactInput()
                            {
                                Property = "dialog.contactName",
                                EmailProperty = "dialog.emailAddress",
                                Prompt = new ActivityTemplate("[ContactPrompt]"),
                                Value = "@PersonName"
                            },
                            new IfCondition()
                            {
                                Condition = "dialog.emailAddress != null",
                                Actions =
                                {
                                    // add email to attendeeEmailList
                                    new EditArray()
                                    {
                                        ArrayProperty = "dialog.attendeeEmailList",
                                        ChangeType = EditArray.ArrayChangeType.Push,
                                        Value = "dialog.contactName"
                                    },
                                    new SendActivity("I've added {dialog.emailAddress} to the recipients list."),
                                },
                                ElseActions =
                                {
                                    // look up current user's contacts
                                    new HttpRequest()
                                    {
                                        Property = "dialog.contactsList",
                                        Method = HttpRequest.HttpMethod.GET,
                                        Url = "https://graph.microsoft.com/v1.0/me/contacts?$search=\"{dialog.contactName}\"",
                                        Headers = new Dictionary<string, string>()
                                        {
                                            ["Authorization"] = "Bearer {user.token.token}",
                                        }
                                    },
                                    // remove any contacts without an email address
                                    new CodeAction((dc, options) =>
                                    {
                                        // get dialog.contactsList
                                        var exp = new ExpressionEngine().Parse("dialog.contactsList");
                                        (dynamic contactsList, var error) = exp.TryEvaluate(dc.State);

                                        var cleanList = new List<object>();

                                        foreach (var contact in contactsList.value)
                                        {
                                            if (contact.emailAddresses[0]?.address != null)
                                            {
                                                cleanList.Add(contact);
                                            }
                                        }

                                        dc.State.SetValue("dialog.contactsList", cleanList);
                                        return dc.EndDialogAsync();
                                    }),
                                    // if there are any contacts
                                    new IfCondition()
                                    {
                                        Condition = "dialog.contactsList != null && count(dialog.contactsList) > 0",
                                        Actions =
                                        {
                                            new IfCondition()
                                            {
                                                // If there was only one matching contact, add it to the email list
                                                Condition = "count(dialog.contactsList) == 1",
                                                Actions =
                                                {
                                                    new SetProperty()
                                                    {
                                                        Property = "dialog.emailAddress",
                                                        Value = "dialog.contactsList[0].emailAddresses[0].address"
                                                    },
                                                    new EditArray()
                                                    {
                                                        ArrayProperty = "dialog.attendeeEmailList",
                                                        ChangeType = EditArray.ArrayChangeType.Push,
                                                        Value = "dialog.emailAddress"
                                                    },
                                                    new SendActivity("I've added {dialog.contactsList[0].displayName} ({dialog.emailAddress}) to the recipients list."),
                                                },
                                                // else disambiguate and add selectedContact.email to attendees list
                                                ElseActions =
                                                {
                                                    new SendActivity("Found multiple contacts with the name \"{dialog.contactName}\"."),
                                                    new TraceActivity(),
                                                    new SetProperty()
                                                    {
                                                        Property = "dialog.emailChoiceList",
                                                        Value = "foreach(dialog.contactsList, item, item.emailAddresses[0].address)"
                                                    },
                                                    new ChoiceInput()
                                                    {
                                                        Prompt = new ActivityTemplate("Please select an email from this list:"),
                                                        ChoicesProperty = "dialog.emailChoiceList",
                                                        Property = "dialog.emailAddress"
                                                    },
                                                    new EditArray()
                                                    {
                                                        ArrayProperty = "dialog.attendeeEmailList",
                                                        ChangeType = EditArray.ArrayChangeType.Push,
                                                        Value = "dialog.emailAddress"
                                                    },
                                                    new SendActivity("I've added {dialog.emailAddress} to the recipients list."),
                                                }
                                            }
                                        },
                                        ElseActions =
                                        {
                                            new SendActivity("Sorry, I couldn't find any contacts with the name: {dialog.contactName}. Please try again."),
                                            new RepeatDialog(),
                                        }
                                    }
                                }
                            },                           
                            // Prompt for more people
                            new ConfirmInput()
                            {
                                Property = "dialog.addAnotherContact",
                                Prompt = new ActivityTemplate("Do you want to add another contact?")
                            },
                            new SetProperty()
                            {
                                Property = "dialog.attendeeListString",
                                Value = "join(dialog.attendeeEmailList, ',')"
                            },
                            new IfCondition()
                            {
                                Condition = "dialog.addAnotherContact == true",
                                Actions = { new RepeatDialog() },
                                ElseActions = { new EndDialog("dialog.attendeeListString") }
                            }
                        }
                    },
                }
            };

            AddDialog(getAttendees);
        }
    }
}

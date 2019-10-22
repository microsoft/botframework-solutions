using System.Collections.Generic;
using AdaptiveAssistant.Input;
using AdaptiveCalendarSkill.Services;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.Expressions.Parser;
using Microsoft.Bot.Builder.LanguageGeneration.Generators;
using Microsoft.Bot.Builder.LanguageGeneration.Templates;

namespace AdaptiveCalendarSkill.Dialogs
{
    public class InviteDialog : ComponentDialog
    {
        public InviteDialog(BotServices services)
            : base(nameof(InviteDialog))
        {
            var getAttendees = new AdaptiveDialog("adaptive")
            {
                Recognizer = services.CognitiveModelSets["en"].LuisRecognizers["Calendar"],
                Generator = new ResourceMultiLanguageGenerator("InviteDialog.lg"),
                Triggers =
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
                            },
                            new IfCondition()
                            {
                                Condition = "dialog.emailAddress != null",
                                Actions =
                                {
                                    // add email to attendeeEmailList
                                    new EditArray()
                                    {
                                        ItemsProperty = "dialog.recipients",
                                        ChangeType = EditArray.ArrayChangeType.Push,
                                        Value = "dialog.contactName"
                                    },
                                },
                                ElseActions =
                                {
                                    // look up current user's contacts
                                    new HttpRequest()
                                    {
                                        ResultProperty = "dialog.contactsList",
                                        ResponseType = HttpRequest.ResponseTypes.Json,
                                        Method = HttpRequest.HttpMethod.GET,
                                        Url = "https://graph.microsoft.com/v1.0/me/contacts?$search=\"{dialog.contactName}\"",
                                        Headers = new Dictionary<string, string>()
                                        {
                                            ["Authorization"] = "Bearer {user.token.tokenResponse.token}",
                                        }
                                    },
                                    // remove any contacts without an email address
                                    new CodeAction((dc, options) =>
                                    {
                                        // get dialog.contactsList
                                        var exp = new ExpressionEngine().Parse("dialog.contactsList.content");
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
                                                        ItemsProperty = "dialog.recipients",
                                                        ChangeType = EditArray.ArrayChangeType.Push,
                                                        Value = "dialog.emailAddress"
                                                    },
                                                    new SendActivity("[AddedContactMessage]"),
                                                },
                                                // else disambiguate and add selectedContact.email to attendees list
                                                ElseActions =
                                                {
                                                    new SendActivity("[MultipleContactsFound]"),
                                                    new TraceActivity(),
                                                    new SetProperty()
                                                    {
                                                        Property = "dialog.emailChoiceList",
                                                        Value = "foreach(dialog.contactsList, item, item.emailAddresses[0].address)"
                                                    },
                                                    new ChoiceInput()
                                                    {
                                                        Prompt = new ActivityTemplate("[ContactChoicePrompt]"),
                                                        Choices = new ChoiceSet("dialog.emailChoiceList"),
                                                        Property = "dialog.emailAddress"
                                                    },
                                                    new EditArray()
                                                    {
                                                        ItemsProperty = "dialog.recipients",
                                                        ChangeType = EditArray.ArrayChangeType.Push,
                                                        Value = "dialog.emailAddress"
                                                    },
                                                    new SendActivity("[AddedContactMessage]"),
                                                }
                                            }
                                        },
                                        ElseActions =
                                        {
                                            new SendActivity("[CouldNotFindContactMessage]"),
                                            new RepeatDialog(),
                                        }
                                    }
                                }
                            },                           
                            // Prompt for more people
                            new ConfirmInput()
                            {
                                Property = "dialog.addAnotherContact",
                                Prompt = new ActivityTemplate("[AddContactPrompt]"),
                            },
                            new IfCondition()
                            {
                                Condition = "dialog.addAnotherContact == true",
                                Actions = { new RepeatDialog() },
                                ElseActions = { new EndDialog(value: "dialog.recipients") }
                            }
                        }
                    },
                }
            };

            AddDialog(getAttendees);
        }
    }
}

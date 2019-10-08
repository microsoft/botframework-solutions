using AdaptiveCalendarSkill.Services;
using AdaptiveCards.Rendering;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.Expressions.Parser;
using Microsoft.Bot.Builder.LanguageGeneration.Generators;
using Microsoft.Bot.Builder.LanguageGeneration.Templates;
using Microsoft.Graph;
using Newtonsoft.Json.Linq;
using System.Collections.Generic;
using System.ComponentModel;
using System.Diagnostics;

namespace AdaptiveCalendarSkill.Dialogs
{
    public class CreateEntryDialog : ComponentDialog
    {
        public CreateEntryDialog(
            BotSettings settings,
            BotServices services,
            OAuthPromptDialog oauthDialog,
            GetRecipientsDialog getRecipientsDialog)
            : base(nameof(CreateEntryDialog))
        {
            var createDialog = new AdaptiveDialog("adaptive")
            {
                Recognizer = services.CognitiveModelSets["en"].LuisServices["Calendar"],
                Generator = new ResourceMultiLanguageGenerator("CreateCalendarEntry.lg"),
                Triggers =
                {
                    new OnBeginDialog()
                    {
                        Actions =
                        {
                            new BeginDialog(oauthDialog.Id),
                            // Title prompt
                            new TextInput()
                            {
                                Property = "dialog.title",
                                Prompt = new ActivityTemplate("[SubjectPrompt]"),
                                AllowInterruptions = AllowInterruptions.Never
                            },
                            new TextInput()
                            {
                                Property = "dialog.content",
                                Prompt = new ActivityTemplate("What's the meeting content?"),
                                AllowInterruptions = AllowInterruptions.Never
                            },
                            // Get attendees
                            new BeginDialog(getRecipientsDialog.Id)
                            {
                                ResultProperty = "dialog.recipients"
                            },
                            new TraceActivity(),
                            new SendActivity("create: {dialog.recipients}"),
                            new CodeAction((dc, options) =>
                            {
                                var exp = new ExpressionEngine().Parse("dialog.recipients");
                                (dynamic recipientsList, var error) = exp.TryEvaluate(dc.State);

                                var emailList = new List<object>();

                                foreach (var email in recipientsList)
                                {
                                    emailList.Add(new { emailAddress = new { address = email } });
                                }

                                dc.State.SetValue("dialog.emailList", emailList);
                                return dc.EndDialogAsync();
                            }),
                            new TextInput()
                            {
                                Property = "dialog.startTime",
                                Prompt = new ActivityTemplate("[StartTimePrompt]"),
                                Value = "@datetimev2",
                                AllowInterruptions = AllowInterruptions.Always
                            },
                            new TraceActivity(),
                            new TextInput()
                            {
                                Property = "dialog.location",
                                Prompt = new ActivityTemplate("[LocationPrompt]"),
                            },
                            new TextInput()
                            {
                                Property = "dialog.content",
                                Prompt = new ActivityTemplate("[ContentPrompt]"),
                            },
                            new ConfirmInput()
                            {
                                Property = "dialog.confirmEntry",
                                Prompt = new ActivityTemplate("[ConfirmDetailsPrompt]"),
                                InvalidPrompt = new ActivityTemplate("[YesOrNo]"),
                            },
                            new IfCondition()
                            {
                                Condition = "dialog.confirmEntry",
                                Actions =
                                {
                                    new HttpRequest()
                                    {
                                        ResultProperty = "dialog.createResponse",
                                        Method = HttpRequest.HttpMethod.POST,
                                        Url = "https://graph.microsoft.com/v1.0/me/events",
                                        Headers =  new Dictionary<string, string>(){
                                            ["Authorization"] = "Bearer {user.token.token}",
                                        },
                                        Body = JObject.FromObject(new {
                                            subject = "{dialog.title}",
                                            attendees = "{dialog.emailList}",
                                            location = new
                                            {
                                                displayName = "{dialog.location}"
                                            },
                                            start = new
                                            {
                                                dateTime = "{formatDateTime(dialog.startTime[0].value, \'yyyy-MM-ddTHH:mm:ss\')}",
                                                timeZone = "UTC"
                                            },
                                            end = new {
                                                dateTime = "{formatDateTime(dialog.endTime[0].value, \'yyyy-MM-ddTHH:mm:ss\')}",
                                                timeZone = "UTC"
                                            }
                                        })
                                    },
                                    new SendActivity("[SuccessMessage]")
                                },
                                ElseActions =
                                {
                                    // If they rejected the entry, start over
                                    new SendActivity("[StartOver]"),
                                    new RepeatDialog()
                                }
                            },
                            new EndDialog()
                        }
                    },
                    new OnDialogEvent()
                    {
                        // If we recognized an intent, try to pull out entities
                        Event = AdaptiveEvents.RecognizedIntent,
                        Actions =
                        {
                            new TraceActivity(),
                        }
                    },
                    new OnIntent(CalendarLuis.Intent.Help.ToString())
                    {
                        Actions = { new SendActivity("[Help-Root-Dialog]") },
                    }
                },
            };

            AddDialog(createDialog);
            AddDialog(oauthDialog);
            AddDialog(getRecipientsDialog);
        }
    }
}

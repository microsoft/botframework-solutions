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
                Events =
                {
                    new OnBeginDialog()
                    {
                        Actions =
                        {
                            new BeginDialog(oauthDialog.Id),
                            // Title prompt
                            new TextInput()
                            {
                                Property = "dialog.Title",
                                Prompt = new ActivityTemplate("[SubjectPrompt]"),
                                Value = "@Subject"
                            },
                            // Get attendees
                            new BeginDialog(getRecipientsDialog.Id)
                            {
                                Property = "dialog.attendeeString"
                            },
                            new SetProperty()
                            {
                                Property = "dialog.attendeeEmailList",
                                Value = "split(dialog.attendeeString, ',')"
                            },
                            new SendActivity("[RecipientsListMessage]"),
                            // Start time prompt
                            new DateTimeInput()
                            {
                                Property = "dialog.StartTime",
                                Prompt = new ActivityTemplate("[StartTimePrompt]"),
                                Value = "@FromTime"
                            },
                            // End time prompt
                            new DateTimeInput()
                            {
                                Property = "dialog.EndTime",
                                Prompt = new ActivityTemplate("[EndTimePrompt]"),
                                Value = "@ToTime"
                            },
                            // Location prompt
                            new TextInput()
                            {
                                Property = "dialog.Location",
                                Prompt = new ActivityTemplate("[LocationPrompt]"),
                                Value = "@Location"
                            },
                            // Content prompt
                            new TextInput()
                            {
                                Property = "dialog.Content",
                                Prompt = new ActivityTemplate("[ContentPrompt]"),
                            },
                            // Confirm
                            new ConfirmInput()
                            {
                                Property = "dialog.ConfirmEntry",
                                Prompt = new ActivityTemplate("[ConfirmDetailsPrompt]"),
                                InvalidPrompt = new ActivityTemplate("[YesOrNo]"),
                            },
                            new IfCondition()
                            {
                                Condition = "dialog.ConfirmEntry",
                                Actions =
                                {
                                    new HttpRequest()
                                    {
                                        Property = "dialog.createResponse",
                                        Method = HttpRequest.HttpMethod.POST,
                                        Url = "https://graph.microsoft.com/v1.0/me/events",
                                        Headers =  new Dictionary<string, string>(){
                                            ["Authorization"] = "Bearer {user.token.token}",
                                        },
                                        Body = JObject.Parse(@"{
                                            'subject': '{dialog.Subject}',
                                            'attendees': [
                                                {
                                                    'emailAddress':
                                                    {
                                                        'address':'test@test.com'
                                                    }
                                                }
                                            ],
                                            'location': {
                                                'displayName': '{dialog.Location}',
                                            },
                                            'start': {
                                                'dateTime': '{formatDateTime(dialog.StartTime[0].value, \'yyyy-MM-ddTHH:mm:ss\')}',
                                                'timeZone': 'UTC'
                                            },
                                            'end': {
                                                'dateTime': '{formatDateTime(dialog.EndTime[0].value, \'yyyy-MM-ddTHH:mm:ss\')}',
                                                'timeZone': 'UTC'
                                            }
                                        }")
                                    }
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
                },
            };

            AddDialog(createDialog);
            AddDialog(oauthDialog);
            AddDialog(getRecipientsDialog);
        }
    }
}

using System;
using System.Collections.Generic;
using AdaptiveCalendarSkill.Input;
using AdaptiveCalendarSkill.Services;
using Luis;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Adaptive;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Actions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Conditions;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Generators;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Input;
using Microsoft.Bot.Builder.Dialogs.Adaptive.Templates;
using Microsoft.Bot.Expressions;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Newtonsoft.Json.Linq;

namespace AdaptiveCalendarSkill.Dialogs
{
    public class CreateDialog : ComponentDialog
    {
        private int _defaultMeetingLength = 30;

        public CreateDialog(
            BotServices services,
            OAuthDialog oauthDialog,
            InviteDialog inviteDialog)
            : base(nameof(CreateDialog))
        {
            var createDialog = new AdaptiveDialog("adaptive")
            {
                Recognizer = services.CognitiveModelSets["en"].LuisServices["Calendar"],
                Generator = new ResourceMultiLanguageGenerator("CreateDialog.lg"),
                Triggers =
                {
                    new OnBeginDialog()
                    {
                        Actions =
                        {
                            new BeginDialog(oauthDialog.Id),
                            new BeginDialog(inviteDialog.Id)
                            {
                                ResultProperty = "dialog.recipients",
                            },
                            new CodeAction((dc, options) =>
                            {
                                var exp = new ExpressionEngine().Parse("dialog.recipients");
                                (dynamic recipientsList, var error) = exp.TryEvaluate(dc.State);

                                var emailList = new List<object>();

                                foreach (var email in recipientsList)
                                {
                                    emailList.Add(new { emailAddress = new { address = email } });
                                }

                                dc.State.SetValue("dialog.emailList", emailList.ToArray());
                                return dc.EndDialogAsync();
                            }),
                            new TextInput()
                            {
                                Property = "dialog.title",
                                Prompt = new ActivityTemplate("[TitlePrompt]"),
                                Value = "@Subject",
                                AllowInterruptions = "false"
                            },
                            new SetProperty()
                            {
                                Property = "dialog.title",
                                Value = "concat(toUpper(substring(dialog.title, 0, 1)), substring(dialog.title, 1))"
                            },
                            new TimexInput()
                            {
                                Property = "dialog.datetime",
                                Prompt = new ActivityTemplate("[DateTimePrompt]"),
                                Value = "@datetimev2",
                                AllowInterruptions = "true"
                            },
                            new IfCondition()
                            {
                                // if we have a date time range
                                Condition = "dialog.datetime.datetimerange != null",
                                Actions =
                                {
                                    new SetProperty()
                                    {
                                        Property = "dialog.startDateTime",
                                        Value = "formatDateTime(dialog.datetime.datetimerange[0].start)"
                                    },
                                    new SetProperty()
                                    {
                                        Property = "dialog.endDateTime",
                                        Value = "formatDateTime(dialog.datetime.datetimerange[0].end)"
                                    },
                                    new TraceActivity()
                                },
                                ElseActions =
                                {
                                    new IfCondition()
                                    {
                                        // if we have a timerange -- set date to today
                                        Condition = "dialog.datetime.timerange != null",
                                        Actions =
                                        {
                                            new SetProperty()
                                            {
                                                // probably won't work
                                                Property = "dialog.startDateTime",
                                                Value = $"formatDateTime(\'{DateTime.Today.ToString("yyyy-MM-ddTHH:mm:ss")}\')"
                                            },
                                            new SetProperty()
                                            {
                                                Property = "dialog.startDateTime",
                                                Value = "addSeconds(addMinutes(addHours(formatDateTime(dialog.startDateTime), dialog.datetime.timerange[0].start.Hour), dialog.datetime.timerange[0].start.Minutes), dialog.datetime.timerange[0].start.Seconds)"
                                            },
                                            new SetProperty()
                                            {
                                                Property = "dialog.endDateTime",
                                                Value = $"formatDateTime(\'{DateTime.Today.ToString("yyyy-MM-ddTHH:mm:ss")}\')"
                                            },
                                            new SetProperty()
                                            {
                                                Property = "dialog.endDateTime",
                                                Value = "addSeconds(addMinutes(addHours(formatDateTime(dialog.endDateTime), dialog.datetime.timerange[0].end.Hour), dialog.datetime.timerange[0].end.Minutes), dialog.datetime.timerange[0].end.Seconds)"
                                            }
                                        },
                                        ElseActions =
                                        {
                                            new IfCondition()
                                            {
                                                // if we have datetime
                                                Condition = "dialog.datetime.datetime != null",
                                                Actions =
                                                {
                                                    new SetProperty()
                                                    {
                                                        Property = "dialog.startDateTime",
                                                        Value = "formatDateTime(dialog.datetime.datetime[0])"
                                                    },
                                                    new SetProperty()
                                                    {
                                                        // Set end to default 30 min
                                                        Property = "dialog.endDateTime",
                                                        Value = $"addMinutes(formatDateTime(dialog.startDateTime), {_defaultMeetingLength})"
                                                    }
                                                },
                                                ElseActions =
                                                {
                                                    new IfCondition()
                                                    {
                                                        Condition = "dialog.datetime.daterange != null",
                                                        Actions =
                                                        {
                                                            new SetProperty()
                                                            {
                                                                Property = "dialog.startDateTime",
                                                                Value = "formatDateTime(dialog.datetime.daterange[0].start)"
                                                            },
                                                            new SetProperty()
                                                            {
                                                                Property = "dialog.endDateTime",
                                                                Value = "formatDateTime(dialog.datetime.daterange[0].end)"
                                                            },
                                                            new SetProperty()
                                                            {
                                                                // probably won't work
                                                                Property = "dialog.endDateTime",
                                                                Value = $"addMinutes(addHours(formatDateTime(dialog.endDateTime), 23), 59)"
                                                            }
                                                        },
                                                        ElseActions =
                                                        {
                                                            new IfCondition()
                                                            {
                                                                Condition = "dialog.datetime.time != null",
                                                                Actions =
                                                                {
                                                                    new SetProperty()
                                                                    {
                                                                        Property = "dialog.startDateTime",
                                                                        Value = $"formatDateTime(\'{DateTime.Today.ToString("yyyy-MM-ddTHH:mm:ss")}\')"
                                                                    },
                                                                    new SetProperty()
                                                                    {
                                                                        Property = "dialog.startDateTime",
                                                                        Value = "addSeconds(addMinutes(addHours(formatDateTime(dialog.startDateTime), dialog.datetime.time[0].Hour), dialog.datetime.time[0].Minutes), dialog.datetime.time[0].Seconds)"
                                                                    },
                                                                    new SetProperty()
                                                                    {
                                                                        // Set to default duration
                                                                        Property = "dialog.endDateTime",
                                                                        Value = $"addMinutes(formatDateTime(dialog.startDateTime), {_defaultMeetingLength})"
                                                                    }
                                                                },
                                                                ElseActions =
                                                                {
                                                                    new IfCondition()
                                                                    {
                                                                        Condition = "dialog.datetime.date != null",
                                                                        Actions =
                                                                        {
                                                                            new SetProperty()
                                                                            {
                                                                                Property = "dialog.startDateTime",
                                                                                Value = "formatDateTime(dialog.datetime.date[0])"
                                                                            },
                                                                            new TimexInput()
                                                                            {
                                                                                Property = "dialog.datetime",
                                                                                TimexType = Constants.TimexTypes.Time,
                                                                                Prompt = new ActivityTemplate("What time?"),
                                                                            },
                                                                            new SetProperty()
                                                                            {
                                                                                Property = "dialog.startDateTime",
                                                                                Value = "addSeconds(addMinutes(addHours(formatDateTime(dialog.startDateTime), dialog.datetime.time[0].Hour), dialog.datetime.time[0].Minutes), dialog.datetime.time[0].Seconds)"
                                                                            },
                                                                            new SetProperty()
                                                                            {
                                                                                // Set end to default 30 min
                                                                                Property = "dialog.endDateTime",
                                                                                Value = $"addMinutes(formatDateTime(dialog.startDateTime), {_defaultMeetingLength})"
                                                                            }
                                                                        },
                                                                        ElseActions =
                                                                        {
                                                                            new IfCondition()
                                                                            {
                                                                                Condition = "dialog.datetime.duration != null",
                                                                                Actions =
                                                                                {
                                                                                    new TimexInput()
                                                                                    {
                                                                                        Property = "dialog.datetime",
                                                                                        TimexType = Constants.TimexTypes.Date,
                                                                                        Prompt = new ActivityTemplate("What date?"),
                                                                                    },
                                                                                    new SetProperty()
                                                                                    {
                                                                                        Property = "dialog.startDateTime",
                                                                                        Value = "formatDateTime(dialog.datetime.date[0])"
                                                                                    },
                                                                                    new TimexInput()
                                                                                    {
                                                                                        Property = "dialog.datetime",
                                                                                        TimexType = Constants.TimexTypes.Time,
                                                                                        Prompt = new ActivityTemplate("What time?"),
                                                                                    },
                                                                                    new SetProperty()
                                                                                    {
                                                                                        Property = "dialog.startDateTime",
                                                                                        Value = "addSeconds(addMinutes(addHours(formatDateTime(dialog.startDateTime), dialog.datetime.time[0].Hour), dialog.datetime.time[0].Minutes), dialog.datetime.time[0].Seconds)"
                                                                                    },
                                                                                    new SetProperty()
                                                                                    {
                                                                                        // Set end to default 30 min
                                                                                        Property = "dialog.endDateTime",
                                                                                        Value = $"addMinutes(formatDateTime(dialog.startDateTime), dialog.datetime.duration)"
                                                                                    }
                                                                                }
                                                                            }
                                                                        }
                                                                    }
                                                                }
                                                            }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    },
                                    new TraceActivity()
                                }
                            },
                            new TextInput()
                            {
                                Property = "dialog.location",
                                Prompt = new ActivityTemplate("[LocationPrompt]"),
                                AllowInterruptions = "true"
                            },
                            new TextInput()
                            {
                                Property = "dialog.description",
                                Prompt = new ActivityTemplate("[DescriptionPrompt]"),
                                AllowInterruptions = "true"
                            },
                            new ConfirmInput()
                            {
                                Property = "dialog.confirmEntry",
                                Prompt = new ActivityTemplate("[ConfirmEventPrompt]"),
                                AllowInterruptions = "true"
                            },
                            new TraceActivity(),
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
                                            body = new
                                            {
                                                contentType = "HTML",
                                                content = "{dialog.description}"
                                            },
                                            start = new
                                            {
                                                dateTime = "{formatDateTime(dialog.startDateTime, \'yyyy-MM-ddTHH:mm:ss\')}",
                                                timeZone = "UTC"
                                            },
                                            end = new {
                                                dateTime = "{formatDateTime(dialog.endDateTime, \'yyyy-MM-ddTHH:mm:ss\')}",
                                                timeZone = "UTC"
                                            },
                                            location = new
                                            {
                                                displayName = "{dialog.location}"
                                            },
                                            attendees = "{dialog.emailList}",
                                        })
                                    },
                                    new SendActivity("[CreatedEventMessage]")
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
                    new OnIntent(CalendarLuis.Intent.Invite.ToString())
                    {
                        Condition = "turn.dialogevent.value.intents.SetTitle.score > 0.5",
                        Actions =
                        {
                            new BeginDialog(inviteDialog.Id)
                            {
                                ResultProperty = "dialog.addedRecipients"
                            },
                            new Foreach()
                            {
                                ItemsProperty = "dialog.addedRecipients",
                                Actions =
                                {
                                    new EditArray()
                                    {
                                        ChangeType = EditArray.ArrayChangeType.Push,
                                        ItemsProperty = "dialog.recipients",
                                        Value = "$foreach.value"
                                    },
                                }
                            },                            
                            new CodeAction((dc, options) =>
                            {
                                var exp = new ExpressionEngine().Parse("dialog.recipients");
                                (dynamic recipientsList, var error) = exp.TryEvaluate(dc.State);

                                var emailList = new List<object>();

                                foreach (var email in recipientsList)
                                {
                                    emailList.Add(new { emailAddress = new { address = email } });
                                }

                                dc.State.SetValue("dialog.emailList", emailList.ToArray());
                                return dc.EndDialogAsync();
                            }),
                            new TraceActivity()
                        }
                    },
                    new OnIntent(CalendarLuis.Intent.SetTitle.ToString())
                    {
                        Condition = "turn.dialogevent.value.intents.SetTitle.score > 0.5",
                        Actions =
                        {
                            new SetProperty()
                            {
                                Property = "dialog.title",
                                Value = "@Subject"
                            }
                        }
                    },
                    new OnIntent(CalendarLuis.Intent.SetDateTime.ToString())
                    {
                        // How to flexibly get the full datetime logic here
                        Condition = "turn.dialogevent.value.intents.SetDateTime.score > 0.5",
                        Actions =
                        {
                            new SetProperty()
                            {
                                Property = "dialog.datetime",
                                Value = "@datetimev2"
                            }
                        }
                    },
                    new OnIntent(CalendarLuis.Intent.SetLocation.ToString())
                    {
                        Condition = "turn.dialogevent.value.intents.SetLocation.score > 0.5",
                        Actions =
                        {
                            new SetProperty()
                            {
                                Property = "dialog.location",
                                Value = "@Location"
                            }
                        }
                    },
                    new OnIntent(CalendarLuis.Intent.Cancel.ToString())
                    {
                        Condition = "turn.dialogevent.value.intents.Cancel.score > 0.8",
                        Actions =
                        {
                            new SendActivity("Sure, I've cancelled the meeting."),
                            new EndDialog()
                        }
                    },
                },
            };

            AddDialog(createDialog);
            AddDialog(oauthDialog);
            AddDialog(inviteDialog);
        }
    }
}

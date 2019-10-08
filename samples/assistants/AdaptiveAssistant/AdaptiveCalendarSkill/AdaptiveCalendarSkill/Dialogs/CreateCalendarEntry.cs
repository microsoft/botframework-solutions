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
/// This dialog will create a calendar entry by propting user to enter the relevant information,
/// including subject, starting time, ending time, and the email address of the attendee
/// The user can enter only one attendee
/// </summary>
namespace AdaptiveCalendarSkill.Dialogs
{
    public class CreateCalendarEntry : ComponentDialog
    {
        public CreateCalendarEntry(
            BotSettings settings,
            BotServices services,
            OAuthPromptDialog oauthDialog,
            AddContactDialog addContactDialog)
            : base(nameof(CreateCalendarEntry))
        {
            var adaptiveDialog = new AdaptiveDialog("create")
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
                            new SetProperty()
                            {
                                Value = "@FromTime",
                                Property = "dialog.CreateCalendarEntry_FromTime"
                            },
                            new SetProperty(){
                                Value = "@ToTime",
                                Property = "dialog.CreateCalendarEntry_ToTime"
                            },
                            new SetProperty(){
                                Value = "@Location",
                                Property = "dialog.CreateCalendarEntry_Location"
                            },
                            new SetProperty(){
                                Value = "@Subject",
                                Property = "dialog.CreateCalendarEntry_Subject"
                            },
                            new DeleteProperty(){
                                Property = "user.CreateCalendarEntry_PersonName" // otherwise, it will remember the personName from last time
                            },
                            new SetProperty(){ // if not null, then will not ask add another one until no
                                Value = "@personName",
                                Property = "user.CreateCalendarEntry_PersonName"
                            },
                            // add contact flow
                            new SetProperty()
                            {
                                Property = "user.AddContactDialog_pageIndex",// 0-based
                                Value = "0"
                            },
                            new SetProperty()
                            {
                                Property = "user.finalContact",
                                Value = "''"
                            },
                            // for multiple contacts use only
                            //new IfCondition(){
                            //    Condition = "user.CreateCalendarEntry_PersonName == null",
                            //    Actions =
                            //    {
                            //        // add contact flow
                            //        new SetProperty()
                            //        {
                            //            Property = "user.repeatFlag",
                            //            Value = "true"
                            //        },
                            //    },
                            //    ElseActions =
                            //    {
                            //         new SetProperty()
                            //        {
                            //            Property = "user.repeatFlag",
                            //            Value = "true"
                            //        },
                            //    }
                            //},
                            new BeginDialog(addContactDialog.Id),
                            // only for multiple contacts user
                            //new SetProperty()
                            //{
                            //    Property = "user.finalContact",
                            //    Value = "substring(user.finalContact, 0, length(user.finalContact) - 1)"
                            //},
                            //new SetProperty()
                            //{
                            //     Property = "user.finalContact",
                            //     Value = "concat('[', user.finalContact, ']')"
                            //},
                            new TextInput()
                            {
                                Property = "dialog.CreateCalendarEntry_Subject",
                                Prompt = new ActivityTemplate("[GetSubject]")
                            },
                            new DateTimeInput()
                            {
                                Property = "dialog.CreateCalendarEntry_FromTime",
                                Prompt = new ActivityTemplate("[GetFromTime]")
                            },
                            new DateTimeInput()
                            {
                                Property = "dialog.CreateCalendarEntry_ToTime",
                                Prompt = new ActivityTemplate("[GetToTime]")
                            },
                            new TextInput()
                            {
                                Property = "dialog.CreateCalendarEntry_Location",
                                Prompt = new ActivityTemplate("[GetLocation]")
                            },
                            new SendActivity("[CreateCalendarDetailedEntryReadBack]"),
                            new ConfirmInput(){
                                Property = "turn.CreateCalendarEntry_ConfirmChoice",
                                Prompt = new ActivityTemplate("[InformationConfirm]"),
                                InvalidPrompt = new ActivityTemplate("[YesOrNo]"),
                            },
                            // to post our latest update to our calendar
                            new IfCondition()
                            {
                                Condition = "turn.CreateCalendarEntry_ConfirmChoice",
                                Actions ={
                                    new HttpRequest()
                                    {
                                        Property = "dialog.createResponse",
                                        Method = HttpRequest.HttpMethod.POST,
                                        Url = "https://graph.microsoft.com/v1.0/me/events",
                                        Headers =  new Dictionary<string, string>(){
                                            ["Authorization"] = "Bearer {user.token.token}",
                                        },
                                        Body = JObject.Parse(@"{
                                            'subject': '{dialog.CreateCalendarEntry_Subject}',
                                            'attendees': [
                                                {
                                                    'emailAddress':
                                                    {
                                                        'address':'{user.finalContact}'
                                                    }
                                                }
                                            ],
                                            'location': {
                                                'displayName': '{dialog.CreateCalendarEntry_Location}',
                                            },
                                            'start': {
                                                'dateTime': '{formatDateTime(dialog.CreateCalendarEntry_FromTime[0].value, \'yyyy-MM-ddTHH:mm:ss\')}',
                                                'timeZone': 'UTC'
                                            },
                                            'end': {
                                                'dateTime': '{formatDateTime(dialog.CreateCalendarEntry_ToTime[0].value, \'yyyy-MM-ddTHH:mm:ss\')}',
                                                'timeZone': 'UTC'
                                            }
                                        }")
                                        //Body = JObject.Parse(@"{
                                        //    'subject': '{dialog.CreateCalendarEntry_Subject}',
                                        //    'attendees': '{user.finalContact}', //DEBUG exists here, because user.finalContact cannot be correctly placed and replaced
                                        //    'location': {
                                        //        'displayName': '{dialog.CreateCalendarEntry_Location}',
                                        //    },
                                        //    'start': {
                                        //        'dateTime': '{formatDateTime(dialog.CreateCalendarEntry_FromTime[0].value, \'yyyy-MM-ddTHH:mm:ss\')}',
                                        //        'timeZone': 'UTC'
                                        //    },
                                        //    'end': {
                                        //        'dateTime': '{formatDateTime(dialog.CreateCalendarEntry_ToTime[0].value, \'yyyy-MM-ddTHH:mm:ss\')}',
                                        //        'timeZone': 'UTC'
                                        //    }
                                        //}")
                                    }
                                },
                                ElseActions ={
                                    new SendActivity("[StartOver]"),
                                    new RepeatDialog()
                                }
                            },
                            new IfCondition
                            {
                                Condition = "dialog.createResponse.error == null",
                                Actions = new List<IDialog>
                                {
                                    new SendActivity("[CreateCalendarEntryReadBack]")
                                },
                                ElseActions = new List<IDialog>
                                {
                                    new SendActivity("{dialog.createResponse}"),
                                    new SendActivity("[CreateCalendarEntryFailed]")
                                },
                            },
                            new SendActivity("[Welcome-Actions]"),
                            new EndDialog()
                        }
                    },
                    new OnIntent(GeneralLuis.Intent.Help.ToString())
                    {
                        Actions = { new SendActivity("[HelpCreateMeeting]") }
                    },
                    new OnIntent(GeneralLuis.Intent.Cancel.ToString())
                    {
                        Actions =
                        {
                            new SendActivity("[CancelCreateMeeting]"),
                            new CancelAllDialogs()
                        }
                    }
                }
            };
            // Add named dialogs to the DialogSet. These names are saved in the dialog state.
            AddDialog(adaptiveDialog);
            AddDialog(addContactDialog);
            AddDialog(oauthDialog);

            // The initial child Dialog to run.
            InitialDialogId = adaptiveDialog.Id;
        }
    }
}

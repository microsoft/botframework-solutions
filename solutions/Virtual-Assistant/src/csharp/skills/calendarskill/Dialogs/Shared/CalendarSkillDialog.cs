using CalendarSkill.Dialogs.Main.Resources;
using CalendarSkill.Dialogs.Shared.Resources;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Dialogs.Choices;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Extensions;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Recognizers.Text;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CalendarSkill
{
    public class CalendarSkillDialog : ComponentDialog
    {
        // Constants
        public const string SkillModeAuth = "SkillAuth";
        public const string LocalModeAuth = "LocalAuth";

        // Fields
        protected SkillConfiguration _services;
        protected IStatePropertyAccessor<CalendarSkillState> _accessor;
        protected IServiceManager _serviceManager;
        protected CalendarSkillResponseBuilder _responseBuilder = new CalendarSkillResponseBuilder();

        public CalendarSkillDialog(
            string dialogId,
            SkillConfiguration services,
            IStatePropertyAccessor<CalendarSkillState> accessor,
            IServiceManager serviceManager)
            : base(dialogId)
        {
            _services = services;
            _accessor = accessor;
            _serviceManager = serviceManager;

            var oauthSettings = new OAuthPromptSettings()
            {
                ConnectionName = _services.AuthConnectionName,
                Text = $"Authentication",
                Title = "Signin",
                Timeout = 300000, // User has 5 minutes to login
            };

            AddDialog(new EventPrompt(SkillModeAuth, "tokens/response", TokenResponseValidator));
            AddDialog(new OAuthPrompt(LocalModeAuth, oauthSettings, AuthPromptValidator));
            AddDialog(new TextPrompt(Actions.Prompt));
            AddDialog(new ConfirmPrompt(Actions.TakeFurtherAction, null, Culture.English) { Style = ListStyle.SuggestedAction });
            AddDialog(new DateTimePrompt(Actions.DateTimePrompt, null, Culture.English));
            AddDialog(new DateTimePrompt(Actions.DateTimePromptForUpdateDelete, DateTimePromptValidator, Culture.English));
            AddDialog(new ChoicePrompt(Actions.Choice, ChoiceValidator, Culture.English) { Style = ListStyle.None, });
            AddDialog(new ChoicePrompt(Actions.EventChoice, null, Culture.English) { Style = ListStyle.Inline, ChoiceOptions = new ChoiceFactoryOptions { InlineSeparator = string.Empty, InlineOr = string.Empty, InlineOrMore = string.Empty, IncludeNumbers = false } });
        }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _accessor.GetAsync(dc.Context);
            await DigestCalendarLuisResult(dc, state.LuisResult);
            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await _accessor.GetAsync(dc.Context);
            await DigestCalendarLuisResult(dc, state.LuisResult);
            return await base.OnContinueDialogAsync(dc, cancellationToken);
        }

        // Shared steps
        public async Task<DialogTurnResult> GetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken)
        {
            try
            {
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;

                // If in Skill mode we ask the calling Bot for the token
                if (skillOptions != null && skillOptions.SkillMode)
                {
                    // We trigger a Token Request from the Parent Bot by sending a "TokenRequest" event back and then waiting for a "TokenResponse"
                    // TODO Error handling - if we get a new activity that isn't an event
                    var response = sc.Context.Activity.CreateReply();
                    response.Type = ActivityTypes.Event;
                    response.Name = "tokens/request";

                    // Send the tokens/request Event
                    await sc.Context.SendActivityAsync(response);

                    // Wait for the tokens/response event
                    return await sc.PromptAsync(SkillModeAuth, new PromptOptions());
                }
                else
                {
                    return await sc.PromptAsync(LocalModeAuth, new PromptOptions());
                }
            }
            catch
            {
                // await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(SharedResponses.AuthFailed));
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        public async Task<DialogTurnResult> AfterGetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // When the user authenticates interactively we pass on the tokens/Response event which surfaces as a JObject
                // When the token is cached we get a TokenResponse object.
                var skillOptions = (CalendarSkillDialogOptions)sc.Options;
                TokenResponse tokenResponse;
                if (skillOptions.SkillMode)
                {
                    var resultType = sc.Context.Activity.Value.GetType();
                    if (resultType == typeof(TokenResponse))
                    {
                        tokenResponse = sc.Context.Activity.Value as TokenResponse;
                    }
                    else
                    {
                        var tokenResponseObject = sc.Context.Activity.Value as JObject;
                        tokenResponse = tokenResponseObject?.ToObject<TokenResponse>();
                    }
                }
                else
                {
                    tokenResponse = sc.Result as TokenResponse;
                }

                if (tokenResponse != null)
                {
                    var state = await _accessor.GetAsync(sc.Context);
                    state.APIToken = tokenResponse.Token;
                }

                return await sc.NextAsync();
            }
            catch
            {
                await HandleDialogExceptions(sc);
                throw;
            }
        }

        // Validators
        public Task<bool> TokenResponseValidator(PromptValidatorContext<Activity> pc, CancellationToken cancellationToken)
        {
            var activity = pc.Recognized.Value;
            if (activity != null && activity.Type == ActivityTypes.Event)
            {
                return Task.FromResult(true);
            }
            else
            {
                return Task.FromResult(false);
            }
        }

        public Task<bool> AuthPromptValidator(PromptValidatorContext<TokenResponse> promptContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        public async Task<bool> ChoiceValidator(PromptValidatorContext<FoundChoice> pc, CancellationToken cancellationToken)
        {

            var state = await _accessor.GetAsync(pc.Context);
            var luisResult = state.GeneralLuisResult;
            var topIntent = luisResult?.TopIntent().intent;

            // TODO: The signature for validators has changed to return bool -- Need new way to handle this logic
            // If user want to show more recipient end current choice dialog and return the intent to next step.
            if (topIntent == Luis.General.Intent.Next || topIntent == Luis.General.Intent.Previous)
            {
                // pc.End(topIntent);
                return true;
            }
            else
            {
                if (!pc.Recognized.Succeeded || pc.Recognized == null)
                {
                    // do nothing when not recognized.
                }
                else
                {
                    // pc.End(pc.Recognized.Value);
                    return true;
                }
            }

            return false;
        }

        public Task<bool> DateTimePromptValidator(PromptValidatorContext<IList<DateTimeResolution>> promptContext, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }

        // Helpers
        public async Task ShowMeetingList(DialogContext dc, List<EventModel> events, bool showDate = true)
        {
            var replyToConversation = dc.Context.Activity.CreateReply();
            replyToConversation.AttachmentLayout = AttachmentLayoutTypes.Carousel;
            replyToConversation.Attachments = new List<Microsoft.Bot.Schema.Attachment>();

            var cardsData = new List<CalendarCardData>();
            foreach (var item in events)
            {
                var meetingCard = item.ToAdaptiveCardData(showDate);
                var replyTemp = dc.Context.Activity.CreateAdaptiveCardReply(CalendarMainResponses.GreetingMessage, item.OnlineMeetingUrl == null ? "Dialogs/Shared/Resources/Cards/CalendarCardNoJoinButton.json" : "Dialogs/Shared/Resources/Cards/CalendarCard.json", meetingCard);
                replyToConversation.Attachments.Add(replyTemp.Attachments[0]);
            }

            await dc.Context.SendActivityAsync(replyToConversation);
        }

        public static bool IsRelativeTime(string userInput, string resolverResult, string timex)
        {
            if (userInput.Contains("ago") ||
                userInput.Contains("before") ||
                userInput.Contains("later") ||
                userInput.Contains("next"))
            {
                return true;
            }

            if (userInput.Contains("today") ||
                userInput.Contains("now") ||
                userInput.Contains("yesterday") ||
                userInput.Contains("tomorrow"))
            {
                return true;
            }

            if (timex == "PRESENT_REF")
            {
                return true;
            }

            return false;
        }

        private Task DigestCalendarLuisResult(DialogContext dc, Calendar luisResult)
        {
            // add luis entities to state
            return Task.CompletedTask;
        }

        public async Task HandleDialogExceptions(WaterfallStepContext sc)
        {
            var state = await _accessor.GetAsync(sc.Context);
            state.Clear();
            await sc.CancelAllDialogsAsync();
        }
    }
}

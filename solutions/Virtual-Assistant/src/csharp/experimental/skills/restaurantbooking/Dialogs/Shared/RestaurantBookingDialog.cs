using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Responses;
using Microsoft.Bot.Solutions.Skills;
using Microsoft.Recognizers.Text.DataTypes.TimexExpression;
using Microsoft.Recognizers.Text.DateTime;
using Newtonsoft.Json.Linq;
using RestaurantBooking.Dialogs.Shared.Resources;
using Constants = Microsoft.Recognizers.Text.DataTypes.TimexExpression.Constants;

namespace RestaurantBooking
{
    public class RestaurantBookingDialog : ComponentDialog
    {
        // Constants
        public const string SkillModeAuth = "SkillAuth";
        public const string LocalModeAuth = "LocalAuth";

        public RestaurantBookingDialog(
           string dialogId,
           SkillConfigurationBase services,
           ResponseManager responseManager,
           IStatePropertyAccessor<RestaurantBookingState> accessor,
           IStatePropertyAccessor<DialogState> dialogStateAccessor,
           IServiceManager serviceManager,
           IBotTelemetryClient telemetryClient)
           : base(dialogId)
        {
            Services = services;
            ResponseManager = responseManager;
            Accessor = accessor;
            DialogStateAccessor = dialogStateAccessor;
            ServiceManager = serviceManager;
            TelemetryClient = telemetryClient;
        }

        // Fields
        protected SkillConfigurationBase Services { get; set; }

        protected IStatePropertyAccessor<RestaurantBookingState> Accessor { get; set; }

        protected IStatePropertyAccessor<DialogState> DialogStateAccessor { get; set; }

        protected IServiceManager ServiceManager { get; set; }

        protected ResponseManager ResponseManager { get; set; }

        // Shared steps
        public async Task<DialogTurnResult> GetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var skillOptions = (RestaurantBookingDialogOptions)sc.Options;

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
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
            }
        }

        public async Task<DialogTurnResult> AfterGetAuthToken(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                // When the user authenticates interactively we pass on the tokens/Response event which surfaces as a JObject
                // When the token is cached we get a TokenResponse object.
                var skillOptions = (RestaurantBookingDialogOptions)sc.Options;
                TokenResponse tokenResponse;
                if (skillOptions != null && skillOptions.SkillMode)
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
                    var state = await Accessor.GetAsync(sc.Context);
                }

                return await sc.NextAsync();
            }
            catch (Exception ex)
            {
                throw await HandleDialogExceptions(sc, ex);
            }
        }

        // Helpers
        public async Task DigestLuisResult(DialogContext dc, Reservation luisResult)
        {
            try
            {
                var state = await Accessor.GetAsync(dc.Context);

                // Extract entities and store in state here.
                if (luisResult != null)
                {
                    var entities = luisResult.Entities;

                    // Extract the cuisines out (already normalized to canonical form) and put in State thus slot-filling for the dialog.
                    if (entities.cuisine != null)
                    {
                        foreach (var cuisine in entities.cuisine)
                        {
                            var type = cuisine.First<string>();
                            state.Booking.Category = type;
                        }
                    }

                    if (entities.datetime != null)
                    {
                        var results = DateTimeRecognizer.RecognizeDateTime(dc.Context.Activity.Text, CultureInfo.CurrentUICulture.ToString());
                        if (results.Count > 0)
                        {
                            // We only care about presence of one DateTime
                            var result = results.First();

                            // The resolution could include two example values: one for AM and one for PM.
                            var distinctTimexExpressions = new HashSet<string>();
                            var values = (List<Dictionary<string, string>>)result.Resolution["values"];
                            foreach (var value in values)
                            {
                                // Each result includes a TIMEX expression that captures the inherent date but not time ambiguity.
                                // We are interested in the distinct set of TIMEX expressions.
                                if (value.TryGetValue("timex", out var timex))
                                {
                                    distinctTimexExpressions.Add(timex);
                                }
                            }

                            // Now we have the timex properties let's see if we have a definite date and time
                            // If so we slot-fill this and move on, if we don't we'll ignore for now meaning the user will be prompted
                            TimexProperty timexProperty = new TimexProperty(distinctTimexExpressions.First());

                            if (timexProperty.Types.Contains(Constants.TimexTypes.Date) && timexProperty.Types.Contains(Constants.TimexTypes.Definite))
                            {
                                // We have definite date (no ambiguity)
                                state.Booking.ReservationTime = new DateTime(timexProperty.Year.Value, timexProperty.Month.Value, timexProperty.DayOfMonth.Value);

                                // Timex doesn't capture time ambiguity (e.g. 4 rather than 4pm)
                                if (timexProperty.Types.Contains(Constants.TimexTypes.Time))
                                {
                                    // If we have multiple TimeX
                                    if (distinctTimexExpressions.Count == 1)
                                    {
                                        // We have definite time (no ambiguity)
                                        state.Booking.ReservationDate = new DateTime(timexProperty.Year.Value, timexProperty.Month.Value, timexProperty.DayOfMonth.Value);
                                        state.Booking.ReservationTime = DateTime.Parse($"{timexProperty.Hour.Value}:{timexProperty.Minute.Value}:{timexProperty.Second.Value}");
                                    }
                                    else
                                    {
                                        // We don't have a distinct time so add the TimeEx expressions to enable disambiguation later
                                        state.AmbiguousTimexExpressions = distinctTimexExpressions;
                                    }
                                }
                            }
                            else if (timexProperty.Types.Contains(Constants.TimexTypes.Time))
                            {
                                // We might have a time but no date (e.g. book a table for 4pm)
                                // If we have multiple timex (and time) this means we have a AM and PM component (e.g. ambiguous - book a table at 9)
                                if (distinctTimexExpressions.Count == 1)
                                {
                                    state.Booking.ReservationTime = DateTime.Parse($"{timexProperty.Hour.Value}:{timexProperty.Minute.Value}:{timexProperty.Second.Value}");
                                }
                            }
                            else
                            {
                                // We don't have a distinct date so add the TimeEx expressions to enable disambiguation later
                                state.AmbiguousTimexExpressions = distinctTimexExpressions;
                            }
                        }
                    }

                    if (entities.geographyV2_City != null)
                    {
                        state.Booking.Location = entities.geographyV2_City.First<string>();
                    }

                    // Establishing attendee count can be problematic as the number entity can be picked up for poorly qualified
                    // times, e.g. book a restaurant tomorrow at 2 for 4 people so we rely on a composite entity
                    if (entities.attendees != null)
                    {
                        var attendeesComposite = entities.attendees.First();
                        if (attendeesComposite != null)
                        {
                            int.TryParse(attendeesComposite.number.First().ToString(), out int attendeeCount);
                            if (attendeeCount > 0)
                            {
                                state.Booking.AttendeeCount = attendeeCount;
                            }
                        }
                    }
                }
            }
            catch
            {
                // put log here
            }
        }

        // This method is called by any waterfall step that throws an exception to ensure consistency
        public async Task<Exception> HandleDialogExceptions(WaterfallStepContext sc, Exception ex)
        {
            await sc.Context.SendActivityAsync(sc.Context.Activity.CreateReply(RestaurantBookingSharedResponses.ErrorMessage));
            await sc.CancelAllDialogsAsync();
            return ex;
        }

        protected override async Task<DialogTurnResult> OnBeginDialogAsync(DialogContext dc, object options, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(dc.Context);

            await DigestLuisResult(dc, state.LuisResult);
            return await base.OnBeginDialogAsync(dc, options, cancellationToken);
        }

        protected override async Task<DialogTurnResult> OnContinueDialogAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await Accessor.GetAsync(dc.Context);

            // await DigestLuisResult(dc, state.LuisResult);
            return await base.OnContinueDialogAsync(dc, cancellationToken);
        }

        // Validators
        private Task<bool> TokenResponseValidator(PromptValidatorContext<Activity> pc, CancellationToken cancellationToken)
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

        private Task<bool> AuthPromptValidator(PromptValidatorContext<TokenResponse> pc, CancellationToken cancellationToken)
        {
            return Task.FromResult(true);
        }
    }
}
// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Configuration;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Skills;

namespace VirtualAssistant
{
    public class MainDialog : RouterDialog
    {
        // Fields
        private BotServices _services;
        private BotConfiguration _botConfig;
        private UserState _userState;
        private ConversationState _conversationState;
        private IStatePropertyAccessor<OnboardingState> _onboardingState;
        private IStatePropertyAccessor<Dictionary<string, object>> _parametersAccessor;
        private MainResponses _responder = new MainResponses();
        private SkillRouter _skillRouter;

        public MainDialog(BotServices services, BotConfiguration botConfig, ConversationState conversationState, UserState userState)
            : base(nameof(MainDialog))
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _botConfig = botConfig;
            _conversationState = conversationState;
            _userState = userState;
            _onboardingState = _userState.CreateProperty<OnboardingState>(nameof(OnboardingState));
            _parametersAccessor = _userState.CreateProperty<Dictionary<string, object>>("userInfo");
            var dialogState = _conversationState.CreateProperty<DialogState>(nameof(DialogState));

            AddDialog(new OnboardingDialog(_services, _onboardingState));
            AddDialog(new EscalateDialog(_services));
            AddDialog(new CustomSkillDialog(_services.SkillConfigurations, dialogState));

            // Initialize skill dispatcher
            _skillRouter = new SkillRouter(_services.SkillDefinitions);
        }

        protected override async Task OnStartAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var onboardingState = await _onboardingState.GetAsync(dc.Context, () => new OnboardingState());

            var view = new MainResponses();
            await view.ReplyWith(dc.Context, MainResponses.Intro);

            if (string.IsNullOrEmpty(onboardingState.Name))
            {
                // This is the first time the user is interacting with the bot, so gather onboarding information.
                await dc.BeginDialogAsync(nameof(OnboardingDialog));
            }
        }

        protected override async Task RouteAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var parameters = await _parametersAccessor.GetAsync(dc.Context, () => new Dictionary<string, object>());

            // No dialog is currently on the stack and we haven't responded to the user
            // Check dispatch result
            var dispatchResult = await _services.DispatchRecognizer.RecognizeAsync<Dispatch>(dc.Context, CancellationToken.None);
            var intent = dispatchResult.TopIntent().intent;

            switch (intent)
            {
                case Dispatch.Intent.l_General:
                    {
                        // If dispatch result is general luis model
                        var luisService = _services.LuisServices["general"];
                        var luisResult = await luisService.RecognizeAsync<General>(dc.Context, CancellationToken.None);
                        var luisIntent = luisResult?.TopIntent().intent;

                        // switch on general intents
                        switch (luisIntent)
                        {
                            case General.Intent.Greeting:
                                {
                                    // send greeting response
                                    await _responder.ReplyWith(dc.Context, MainResponses.Greeting);
                                    break;
                                }

                            case General.Intent.Help:
                                {
                                    // send help response
                                    await _responder.ReplyWith(dc.Context, MainResponses.Help);
                                    break;
                                }

                            case General.Intent.Cancel:
                                {
                                    // send cancelled response
                                    await _responder.ReplyWith(dc.Context, MainResponses.Cancelled);

                                    // Cancel any active dialogs on the stack
                                    await dc.CancelAllDialogsAsync();
                                    break;
                                }

                            case General.Intent.Escalate:
                                {
                                    // start escalate dialog
                                    await dc.BeginDialogAsync(nameof(EscalateDialog));
                                    break;
                                }

                            case General.Intent.None:
                            default:
                                {
                                    // No intent was identified, send confused message
                                    await _responder.ReplyWith(dc.Context, MainResponses.Confused);
                                    break;
                                }
                        }

                        break;
                    }

                case Dispatch.Intent.l_Calendar:
                    {
                        var luisService = _services.LuisServices["calendar"];
                        var luisResult = await luisService.RecognizeAsync<Calendar>(dc.Context, CancellationToken.None);
                        var matchedSkill = _skillRouter.IdentifyRegisteredSkill(intent.ToString());

                        await RouteToSkillAsync(dc, new SkillDialogOptions()
                        {
                            SkillDefinition = matchedSkill,
                            Parameters = parameters,
                        });

                        break;
                    }

                case Dispatch.Intent.l_Email:
                    {
                        var luisService = _services.LuisServices["email"];
                        var luisResult = await luisService.RecognizeAsync<Email>(dc.Context, CancellationToken.None);
                        var matchedSkill = _skillRouter.IdentifyRegisteredSkill(intent.ToString());

                        await RouteToSkillAsync(dc, new SkillDialogOptions()
                        {
                            SkillDefinition = matchedSkill,
                            Parameters = parameters,
                        });

                        break;
                    }

                case Dispatch.Intent.l_ToDo:
                    {
                        var luisService = _services.LuisServices["todo"];
                        var luisResult = await luisService.RecognizeAsync<ToDo>(dc.Context, CancellationToken.None);
                        var matchedSkill = _skillRouter.IdentifyRegisteredSkill(intent.ToString());

                        await RouteToSkillAsync(dc, new SkillDialogOptions()
                        {
                            SkillDefinition = matchedSkill,
                            Parameters = parameters,
                        });

                        break;
                    }

                case Dispatch.Intent.l_PointOfInterest:
                    {
                        var luisService = _services.LuisServices["pointofinterest"];
                        var luisResult = await luisService.RecognizeAsync<PointOfInterest>(dc.Context, CancellationToken.None);
                        var matchedSkill = _skillRouter.IdentifyRegisteredSkill(intent.ToString());

                        await RouteToSkillAsync(dc, new SkillDialogOptions()
                        {
                            SkillDefinition = matchedSkill,
                            Parameters = parameters,
                        });

                        break;
                    }

                case Dispatch.Intent.q_FAQ:
                    {
                        var qnaService = _services.QnAServices["faq"];
                        var answers = await qnaService.GetAnswersAsync(dc.Context);
                        if (answers != null && answers.Count() > 0)
                        {
                            await dc.Context.SendActivityAsync(answers[0].Answer);
                        }

                        break;
                    }
            }
        }

        private async Task RouteToSkillAsync(DialogContext dc, SkillDialogOptions options)
        {
            // If we can't handle this within the local Bot it's a skill (prefix of s will make this clearer)
            if (options.SkillDefinition != null)
            {
                // We have matched to a Skill
                await dc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"-->Forwarding your utterance to the {options.SkillDefinition.Name} skill."));

                // Begin the SkillDialog and pass the arguments in
                await dc.BeginDialogAsync(nameof(CustomSkillDialog), options);

                // Pass the activity we have
                var result = await dc.ContinueDialogAsync();

                if (result.Status == DialogTurnStatus.Complete)
                {
                    await CompleteAsync(dc);
                }
            }
        }

        protected override async Task CompleteAsync(DialogContext dc, DialogTurnResult result = null, CancellationToken cancellationToken = default(CancellationToken))
        {
            await _responder.ReplyWith(dc.Context, MainResponses.Completed);

            // End active dialog
            await dc.EndDialogAsync(result);
        }

        protected override async Task OnEventAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            // Indicates whether the event activity should be sent to the active dialog on the stack
            var forward = true;
            var ev = dc.Context.Activity.AsEventActivity();
            var parameters = await _parametersAccessor.GetAsync(dc.Context, () => new Dictionary<string, object>());

            // Send trace to emulator
            var trace = new Activity(type: ActivityTypes.Trace, text: $"Received event: {ev.Name}");
            await dc.Context.SendActivityAsync(trace);

            switch (ev.Name)
            {
                case Events.TimezoneEvent:
                    {
                        try
                        {
                            var timezone = ev.Value.ToString();
                            var tz = TimeZoneInfo.FindSystemTimeZoneById(timezone);

                            parameters[ev.Name] = tz;
                        }
                        catch
                        {
                            await dc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Timezone passed could not be mapped to a valid Timezone. Property not set."));
                        }

                        forward = false;
                        break;
                    }

                case Events.LocationEvent:
                    {
                        parameters[ev.Name] = ev.Value;
                        forward = false;
                        break;
                    }

                case Events.TokenResponseEvent:
                    {
                        forward = true;
                        break;
                    }

                case Events.ActiveLocationUpdate:
                case Events.ActiveRouteUpdate:
                    {
                        var matchedSkill = _skillRouter.IdentifyRegisteredSkill(Dispatch.Intent.l_PointOfInterest.ToString());

                        await RouteToSkillAsync(dc, new SkillDialogOptions()
                        {
                            SkillDefinition = matchedSkill
                        });

                        forward = false;
                        break;
                    }
            }

            if (forward)
            {
                var result = await dc.ContinueDialogAsync();

                if (result.Status == DialogTurnStatus.Complete)
                {
                    await CompleteAsync(dc);
                }
            }
        }
    }

    public static class Events
    {
        public const string TokenResponseEvent = "tokens/response";
        public const string TimezoneEvent = "IPA.Timezone";
        public const string LocationEvent = "IPA.Location";
        public const string ActiveLocationUpdate = "POI.ActiveLocation";
        public const string ActiveRouteUpdate = "POI.ActiveRoute";
    }
}

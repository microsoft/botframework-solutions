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
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions;
using Microsoft.Bot.Solutions.Skills;

namespace CustomAssistant
{
    public class MainDialog : RouterDialog
    {
        // Constants
        public const string Name = "MainDialog";
        private const string LocationEvent = "IPA.Location";
        private const string TimezoneEvent = "IPA.Timezone";

        // Fields
        private BotServices _services;
        private UserState _userState;
        private ConversationState _conversationState;
        private SkillRouter _skillRouter;
        private MainResponses _responder = new MainResponses();

        public MainDialog(BotServices services, ConversationState conversationState, UserState userState)
            : base(nameof(MainDialog))
        {
            _services = services ?? throw new ArgumentNullException(nameof(services));
            _conversationState = conversationState;
            _userState = userState;

            AddDialog(new OnboardingDialog(_services, _userState.CreateProperty<OnboardingState>(nameof(OnboardingState))));
            AddDialog(new EscalateDialog(_services));
            AddDialog(new CustomSkillDialog(_services));

            // Initialize skill dispatcher
            _skillRouter = new SkillRouter(_services.RegisteredSkills);
        }

        protected override async Task OnStartAsync(DialogContext dc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var onboardingAccessor = _userState.CreateProperty<OnboardingState>(nameof(OnboardingState));
            var onboardingState = await onboardingAccessor.GetAsync(dc.Context, () => new OnboardingState());

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
            var userInfoAccessor = _userState.CreateProperty<Dictionary<string, object>>("userInfo");

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
                        var userInformation = await userInfoAccessor.GetAsync(dc.Context, () => new Dictionary<string, object>());

                        var luisService = _services.LuisServices["calendar"];
                        var luisResult = await luisService.RecognizeAsync<Calendar>(dc.Context, CancellationToken.None);
                        var matchedSkill = _skillRouter.IdentifyRegisteredSkill(intent.ToString());

                        await RouteToSkillAsync(dc, new SkillDialogOptions()
                        {
                            LuisResult = luisResult,
                            MatchedSkill = matchedSkill,
                            UserInfo = userInformation,
                        });

                        break;
                    }

                case Dispatch.Intent.l_Email:
                    {
                        var userInfo = await userInfoAccessor.GetAsync(dc.Context, () => new Dictionary<string, object>());

                        var luisService = _services.LuisServices["email"];
                        var luisResult = await luisService.RecognizeAsync<Email>(dc.Context, CancellationToken.None);
                        var matchedSkill = _skillRouter.IdentifyRegisteredSkill(intent.ToString());

                        await RouteToSkillAsync(dc, new SkillDialogOptions()
                        {
                            LuisResult = luisResult,
                            MatchedSkill = matchedSkill,
                            UserInfo = userInfo,
                        });

                        break;
                    }

                case Dispatch.Intent.l_ToDo:
                    {
                        var userInfo = await userInfoAccessor.GetAsync(dc.Context, () => new Dictionary<string, object>());

                        var luisService = _services.LuisServices["todo"];
                        var luisResult = await luisService.RecognizeAsync<ToDo>(dc.Context, CancellationToken.None);
                        var matchedSkill = _skillRouter.IdentifyRegisteredSkill(intent.ToString());

                        await RouteToSkillAsync(dc, new SkillDialogOptions()
                        {
                            LuisResult = luisResult,
                            MatchedSkill = matchedSkill,
                            UserInfo = userInfo,
                        });

                        break;
                    }

                case Dispatch.Intent.l_PointOfInterest:
                    {
                        var userInfo = await userInfoAccessor.GetAsync(dc.Context, () => new Dictionary<string, object>());

                        var luisService = _services.LuisServices["pointofinterest"];
                        var luisResult = await luisService.RecognizeAsync<PointOfInterest>(dc.Context, CancellationToken.None);
                        var matchedSkill = _skillRouter.IdentifyRegisteredSkill(intent.ToString());

                        await RouteToSkillAsync(dc, new SkillDialogOptions()
                        {
                            LuisResult = luisResult,
                            MatchedSkill = matchedSkill,
                            UserInfo = userInfo,
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
            if (options.MatchedSkill != null)
            {
                // We have matched to a Skill
                await dc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"-->Forwarding your utterance to the {options.MatchedSkill.Name} skill."));

                // Begin the SkillDialog and pass the arguments in
                await dc.BeginDialogAsync(nameof(CustomSkillDialog), options);

                // Pass the activity we have
                await dc.ContinueDialogAsync();
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
            var ev = dc.Context.Activity.AsEventActivity();
            await dc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Received event: {ev.Name}"));

            var userInfoAccessor = _userState.CreateProperty<Dictionary<string, object>>("userInfo");
            var userInfo = await userInfoAccessor.GetAsync(dc.Context, () => new System.Collections.Generic.Dictionary<string, object>());

            switch (ev.Name)
            {
                case TimezoneEvent:
                    {
                        try
                        {
                            var timezone = ev.Value.ToString();
                            var tz = TimeZoneInfo.FindSystemTimeZoneById(timezone);

                            userInfo[ev.Name] = tz;
                        }
                        catch
                        {
                            await dc.Context.SendActivityAsync(new Activity(type: ActivityTypes.Trace, text: $"Timezone passed could not be mapped to a valid Timezone. Property not set."));
                        }

                        break;
                    }

                case LocationEvent:
                    {
                        userInfo[ev.Name] = ev.Value;
                        break;
                    }

                case "tokens/response":
                    {
                        // Auth dialog completion
                        var result = await dc.ContinueDialogAsync();

                        if (result.Status == DialogTurnStatus.Complete)
                        {
                            await CompleteAsync(dc);
                        }

                        break;
                    }

                case "POI.ActiveLocation":
                case "POI.ActiveRoute":
                    {
                        var matchedSkill = _skillRouter.IdentifyRegisteredSkill(Dispatch.Intent.l_PointOfInterest.ToString());

                        await RouteToSkillAsync(dc, new SkillDialogOptions()
                        {
                            MatchedSkill = matchedSkill,
                            UserInfo = userInfo
                        });

                        break;
                    }

                default:
                    {
                        if (dc.ActiveDialog != null)
                        {
                            var result = await dc.ContinueDialogAsync();

                            if (result.Status == DialogTurnStatus.Complete)
                            {
                                await CompleteAsync(dc);
                            }
                        }
                        break;
                    }
            }
        }
    }
}

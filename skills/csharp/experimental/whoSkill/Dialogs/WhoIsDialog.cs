using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using Microsoft.Extensions.DependencyInjection;
using WhoSkill.Models;
using WhoSkill.Responses.Shared;
using WhoSkill.Responses.WhoIs;
using WhoSkill.Services;
using WhoSkill.Utilities;

namespace WhoSkill.Dialogs
{
    public class WhoIsDialog : WhoSkillDialogBase
    {
        public WhoIsDialog(
                BotSettings settings,
                ConversationState conversationState,
                MSGraphService msGraphService,
                LocaleTemplateEngineManager localeTemplateEngineManager,
                IBotTelemetryClient telemetryClient,
                MicrosoftAppCredentials appCredentials)
            : base(nameof(WhoIsDialog), settings, conversationState, msGraphService, localeTemplateEngineManager, telemetryClient, appCredentials)
        {
            AddDialog(new ManagerDialog(settings, conversationState, msGraphService, localeTemplateEngineManager, telemetryClient, appCredentials));
        }

        protected override async Task<DialogTurnResult> SearchKeyword(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await WhoStateAccessor.GetAsync(sc.Context);
            if (string.IsNullOrEmpty(state.Keyword))
            {
                var activity = TemplateEngine.GenerateActivityForLocale(WhoSharedResponses.NoKeyword);
                await sc.Context.SendActivityAsync(activity);
                return await sc.EndDialogAsync();
            }

            List<Candidate> candidates = null;
            candidates = await MSGraphService.GetUsers(state.Keyword);
            state.Candidates = candidates;

            return await sc.NextAsync();
        }

        protected override async Task<DialogTurnResult> DisplayResult(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await WhoStateAccessor.GetAsync(sc.Context);
            if (state.PickedPerson == null)
            {
                var activity = TemplateEngine.GenerateActivityForLocale(WhoSharedResponses.NoSearchResult);
                await sc.Context.SendActivityAsync(activity);
                return await sc.EndDialogAsync();
            }

            string templateName = string.Empty;
            switch (state.TriggerIntent)
            {
                case WhoLuis.Intent.WhoIs:
                    {
                        templateName = WhoIsResponses.WhoIs;
                        break;
                    }

                case WhoLuis.Intent.JobTitle:
                    {
                        templateName = WhoIsResponses.JobTitle;
                        break;
                    }

                case WhoLuis.Intent.Department:
                    {
                        templateName = WhoIsResponses.Department;
                        break;
                    }

                case WhoLuis.Intent.Location:
                    {
                        templateName = WhoIsResponses.Location;
                        break;
                    }

                case WhoLuis.Intent.PhoneNumber:
                    {
                        templateName = WhoIsResponses.PhoneNumber;
                        break;
                    }

                case WhoLuis.Intent.EmailAddress:
                    {
                        templateName = WhoIsResponses.EmailAddress;
                        break;
                    }

                default:
                    {
                        templateName = WhoIsResponses.WhoIs;
                        break;
                    }
            }

            var data = new
            {
                TargetName = state.Keyword,
                JobTitle = state.PickedPerson.JobTitle ?? string.Empty,
                Department = state.PickedPerson.Department ?? string.Empty,
                OfficeLocation = state.PickedPerson.OfficeLocation ?? string.Empty,
                MobilePhone = state.PickedPerson.MobilePhone ?? string.Empty,
                EmailAddress = state.PickedPerson.Mail ?? string.Empty,
            };
            var reply = TemplateEngine.GenerateActivityForLocale(templateName, new { Person = data });
            await sc.Context.SendActivityAsync(reply);

            var card = await GetCardForDetail(state.PickedPerson);
            return await sc.PromptAsync(Actions.Prompt, new PromptOptions() { Prompt = card });
        }

        protected override async Task<DialogTurnResult> CollectUserChoiceAfterResult(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            var state = await WhoStateAccessor.GetAsync(sc.Context);
            var luisResult = sc.Context.TurnState.Get<WhoLuis>(StateProperties.WhoLuisResultKey);
            var topIntent = luisResult.TopIntent().intent;

            switch (topIntent)
            {
                case WhoLuis.Intent.Manager:
                    {
                        var keyword = state.PickedPerson.Mail;
                        state.Init();
                        state.Keyword = keyword;
                        state.TriggerIntent = WhoLuis.Intent.Manager;
                        return await sc.BeginDialogAsync(nameof(ManagerDialog));
                    }

                default:
                    {
                        var didntUnderstandActivity = TemplateEngine.GenerateActivityForLocale(WhoSharedResponses.DidntUnderstandMessage);
                        await sc.Context.SendActivityAsync(didntUnderstandActivity);
                        return await sc.EndDialogAsync();
                    }
            }
        }
    }
}
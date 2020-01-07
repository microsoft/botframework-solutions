using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Connector.Authentication;
using WhoSkill.Responses.WhoIs;
using WhoSkill.Services;

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
        }

        protected override async Task SendResultReply(WaterfallStepContext sc)
        {
            try
            {
                var state = await WhoStateAccessor.GetAsync(sc.Context);
                var data = new
                {
                    TargetName = state.TargetName,
                    JobTitle = state.Candidates[0].JobTitle ?? string.Empty,
                    Department = state.Candidates[0].Department ?? string.Empty,
                    OfficeLocation = state.Candidates[0].OfficeLocation ?? string.Empty,
                    MobilePhone = state.Candidates[0].MobilePhone ?? string.Empty,
                    EmailAddress = state.Candidates[0].Mail ?? string.Empty,
                };
                var templateName = state.ReplyTemplateName ?? WhoIsResponses.WhoIs;
                var reply = TemplateEngine.GenerateActivityForLocale(templateName, new { Person = data });
                await sc.Context.SendActivityAsync(reply);
                var cardReply = await GetCardForDetail(state.Candidates[0]);
                await sc.Context.SendActivityAsync(cardReply);
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
            }
        }
    }
}
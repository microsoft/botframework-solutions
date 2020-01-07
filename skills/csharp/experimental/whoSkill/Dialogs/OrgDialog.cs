using System;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Connector.Authentication;
using WhoSkill.Responses.Org;
using WhoSkill.Services;

namespace WhoSkill.Dialogs
{
    public class OrgDialog : WhoSkillDialogBase
    {
        public OrgDialog(
              BotSettings settings,
              ConversationState conversationState,
              MSGraphService msGraphService,
              LocaleTemplateEngineManager localeTemplateEngineManager,
              IBotTelemetryClient telemetryClient,
              MicrosoftAppCredentials appCredentials)
          : base(nameof(OrgDialog), settings, conversationState, msGraphService, localeTemplateEngineManager, telemetryClient, appCredentials)
        {
        }

        protected override async Task SendResultReply(WaterfallStepContext sc)
        {
            try
            {
                var state = await WhoStateAccessor.GetAsync(sc.Context);
                var id = state.Candidates[0].Id;
                var candidate = await MSGraphService.GetManager(id);
                var data = new
                {
                    TargetName = state.TargetName,
                    JobTitle = candidate.JobTitle ?? string.Empty,
                    Department = candidate.Department ?? string.Empty,
                    OfficeLocation = candidate.OfficeLocation ?? string.Empty,
                    MobilePhone = candidate.MobilePhone ?? string.Empty,
                    EmailAddress = candidate.Mail ?? string.Empty,
                };

                var templateName = state.ReplyTemplateName;
                if (templateName == OrgResponses.Manager)
                {
                    var reply = TemplateEngine.GenerateActivityForLocale(templateName, new { Person = data });
                    await sc.Context.SendActivityAsync(reply);
                    var cardReply = await GetCardForDetail(candidate);
                    await sc.Context.SendActivityAsync(cardReply);
                }
                else
                {
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
            }
        }
    }
}

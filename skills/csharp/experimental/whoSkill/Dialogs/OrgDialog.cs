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

                var templateName = state.ReplyTemplateName;
                if (templateName == OrgResponses.Manager)
                {
                    var data = new
                    {
                        TargetName = state.TargetName,
                    };
                    var candidate = await MSGraphService.GetManager(id);
                    if (candidate == null)
                    {
                        var reply = TemplateEngine.GenerateActivityForLocale(OrgResponses.NoManager, new { Person = data });
                        await sc.Context.SendActivityAsync(reply);
                    }
                    else
                    {
                        var reply = TemplateEngine.GenerateActivityForLocale(templateName, new { Person = data });
                        await sc.Context.SendActivityAsync(reply);
                        var cardReply = await GetCardForDetail(candidate);
                        await sc.Context.SendActivityAsync(cardReply);
                    }
                }
                else if (templateName == OrgResponses.DirectReports)
                {
                    var data = new
                    {
                        TargetName = state.TargetName,
                    };
                    var candidates = await MSGraphService.GetDirectReports(id);
                    if (candidates == null || candidates.Count == 0)
                    {
                        var reply = TemplateEngine.GenerateActivityForLocale(OrgResponses.NoDirectReports, new { Person = data });
                        await sc.Context.SendActivityAsync(reply);
                    }
                    else
                    {
                        var reply = TemplateEngine.GenerateActivityForLocale(templateName, new { Person = data, Number = candidates.Count });
                        await sc.Context.SendActivityAsync(reply);
                        var cardReply = await GetCardForPage(candidates);
                        await sc.Context.SendActivityAsync(cardReply);
                    }
                }
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
            }
        }
    }
}

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
    public class EmailAboutDialog : WhoSkillDialogBase
    {
        public EmailAboutDialog(
                BotSettings settings,
                ConversationState conversationState,
                MSGraphService msGraphService,
                LocaleTemplateEngineManager localeTemplateEngineManager,
                IBotTelemetryClient telemetryClient,
                MicrosoftAppCredentials appCredentials)
            : base(nameof(EmailAboutDialog), settings, conversationState, msGraphService, localeTemplateEngineManager, telemetryClient, appCredentials)
        {

        }

    }
}
using Microsoft.Bot.Schema;

namespace ITSMSkill.Services
{
    public class ServiceManager : IServiceManager
    {
        public IITServiceManagement CreateManagement(BotSettings botSettings, TokenResponse tokenResponse)
        {
            if (!string.IsNullOrEmpty(botSettings.ServiceNowUrl) && tokenResponse.ConnectionName == "ServiceNow")
            {
                return new ServiceNow.Management(botSettings.ServiceNowUrl, tokenResponse.Token, botSettings.LimitSize);
            }
            else
            {
                return null;
            }
        }
    }
}

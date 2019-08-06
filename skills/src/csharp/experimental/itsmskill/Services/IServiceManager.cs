using Microsoft.Bot.Schema;

namespace ITSMSkill.Services
{
    public interface IServiceManager
    {
        IITServiceManagement CreateManagement(BotSettings botSettings, TokenResponse tokenResponse);
    }
}

using System.Threading.Tasks;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Skills
{
    public interface ISkillHandoffResponseHandler
    {
        void HandleHandoffResponse(Activity activity);
    }
}
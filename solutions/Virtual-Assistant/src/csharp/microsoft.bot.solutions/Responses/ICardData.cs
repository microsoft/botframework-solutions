using System.Threading.Tasks;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Solutions.Resources
{
    public interface ICardData
    {
        Attachment BuildCardAttachment(string json);
    }
}
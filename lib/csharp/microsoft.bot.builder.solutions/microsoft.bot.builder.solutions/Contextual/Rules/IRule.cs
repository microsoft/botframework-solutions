using System.Threading.Tasks;
using Microsoft.Bot.Builder.Solutions.Contextual.Models;

namespace Microsoft.Bot.Builder.Solutions.Contextual.Rules
{
    public interface IRule
    {
        Task<string> GetAnaphoraResolutionResultAsync(AnaphoraResolutionState state);
    }
}

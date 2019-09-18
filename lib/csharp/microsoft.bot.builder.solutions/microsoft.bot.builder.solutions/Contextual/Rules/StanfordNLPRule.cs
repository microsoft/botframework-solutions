using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Solutions.Contextual.Models;
using Microsoft.Bot.Builder.Solutions.Contextual.Services;

namespace Microsoft.Bot.Builder.Solutions.Contextual.Rules
{
    public class StanfordNLPRule : IRule
    {
        public async Task<string> GetAnaphoraResolutionResultAsync(AnaphoraResolutionState state)
        {
            var content = await StanfordNLPService.PostToStanfordNLPAsync(state.Text);
            return ResolveContent(content);
        }

        private string ResolveContent(string content)
        {
            try
            {
                var lastRelation = content.Split(new string[] { "\"," }, StringSplitOptions.None).Last();
                int startIndex = lastRelation.IndexOf("\\\"");
                lastRelation = lastRelation.Substring(startIndex + 2);
                int endIndex = lastRelation.IndexOf("\\\"");
                return lastRelation.Substring(0, endIndex);
            }
            catch
            {
                return null;
            }
        }
    }
}

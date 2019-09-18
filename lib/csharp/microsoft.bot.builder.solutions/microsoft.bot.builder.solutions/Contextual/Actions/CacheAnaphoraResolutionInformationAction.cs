using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Solutions.Contextual.Actions
{
    public class CacheAnaphoraResolutionInformationAction : SkillContextualActionBase
    {
        public CacheAnaphoraResolutionInformationAction(
            ConversationstateAbstractor conversationstateAbstractor,
            UserContextManager userContextManager)
        {
            ConversationstateAbstractor = conversationstateAbstractor;
            UserContextManager = userContextManager;

            BeforeTurnAction = turnContext =>
            {
                CacheText(turnContext);
            };

            AfterTurnAction = async turnContext =>
            {
                await CacheLatestContact(turnContext);
            };
        }

        private ConversationstateAbstractor ConversationstateAbstractor { get; set; }

        private UserContextManager UserContextManager { get; set; }

        private void CacheText(ITurnContext turnContext)
        {
            UserContextManager.AnaphoraResolutionState.Text = turnContext.Activity.Text;
        }

        private async Task CacheLatestContact(ITurnContext turnContext)
        {
            string latestContact = await AbstractLatestContactAsync(turnContext);
            if (latestContact != null)
            {
                if (UserContextManager.AnaphoraResolutionState.PreviousContacts.Contains(latestContact))
                {
                    UserContextManager.AnaphoraResolutionState.PreviousContacts.Remove(latestContact);
                }

                UserContextManager.AnaphoraResolutionState.PreviousContacts.Add(latestContact);
            }
        }

        private async Task<string> AbstractLatestContactAsync(ITurnContext turnContext)
        {
            try
            {
                var properties = await ConversationstateAbstractor.AbstractTargetPropertiesAsync(turnContext);
                return properties[0];
            }
            catch
            {
                return null;
            }
        }
    }
}
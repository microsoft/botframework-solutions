using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Microsoft.Bot.Builder.Solutions.Contextual.Actions
{
    public class CacheCoreferenceResolutionInformationAction : SkillContextualActionBase
    {
        public CacheCoreferenceResolutionInformationAction(
            ConversationstateAbstractor conversationstateAbstractor,
            UserContextManager userContextManager)
        {
            ConversationstateAbstractor = conversationstateAbstractor;
            UserContextManager = userContextManager;

            AfterTurnAction = async turnContext =>
            {
                await CacheCoreferenceResolutionInformation(turnContext);
            };
        }

        private ConversationstateAbstractor ConversationstateAbstractor { get; set; }

        private UserContextManager UserContextManager { get; set; }

        private async Task CacheCoreferenceResolutionInformation(ITurnContext turnContext)
        {
            string latestContact = await AbstractLatestContactAsync(turnContext);
            if (latestContact != null)
            {
                if (UserContextManager.PreviousContacts.Contains(latestContact))
                {
                    UserContextManager.PreviousContacts.Remove(latestContact);
                }

                UserContextManager.PreviousContacts.Add(latestContact);
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
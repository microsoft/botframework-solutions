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
            ConversationState convState,
            UserContextManager userContextManager,
            string skillName)
        {
            ConversationState = convState;
            UserContextManager = userContextManager;
            SkillName = skillName;

            AfterTurnAction = async turnContext =>
            {
                await CacheCoreferenceResolutionInformation(turnContext);
            };
        }

        private ConversationState ConversationState { get; set; }

        private UserContextManager UserContextManager { get; set; }

        private string SkillName { get; set; }

        private string Contact;

        private async Task CacheCoreferenceResolutionInformation(ITurnContext turnContext)
        {
            Contact = null;
            await AbstractPreviousContactAsync(turnContext);
            if (Contact != null)
            {
                if (UserContextManager.PreviousContacts.Contains(Contact))
                {
                    UserContextManager.PreviousContacts.Remove(Contact);
                }

                UserContextManager.PreviousContacts.Add(Contact);
            }
        }

        private async Task AbstractPreviousContactAsync(ITurnContext turnContext)
        {
            try
            {
                var skillStateAccessor = ConversationState.CreateProperty<object>(string.Format("{0}State", SkillName));
                var skillState = await skillStateAccessor.GetAsync(turnContext);
                ScanPropertiesRecursively(skillState);
            }
            catch
            {
            }
        }

        private void ScanPropertiesRecursively(object propValue, int depth = 0)
        {
            // Because many models exist circular reference, this limit can prevent infinite loop.
            if (propValue == null || depth > 7)
            {
                return;
            }

            var childProps = propValue.GetType().GetProperties().Where(x => !x.GetIndexParameters().Any());
            foreach (var prop in childProps)
            {
                var name = prop.Name;
                var value = prop.GetValue(propValue);
                if (name == "Contacts")
                {
                    try
                    {
                        var lastContact = ((IEnumerable<dynamic>)value).Last();
                        if (lastContact.DisplayName != null)
                        {
                            Contact = lastContact.DisplayName;
                        }
                    }
                    catch
                    {
                    }

                    try
                    {
                        var lastContact = ((IEnumerable<dynamic>)value).Last();
                        if (lastContact.EmailAddress.Name != null)
                        {
                            Contact = lastContact.EmailAddress.Name;
                        }
                    }
                    catch
                    {
                    }
                }

                ScanPropertiesRecursively(value, depth + 1);
            }
        }
    }
}

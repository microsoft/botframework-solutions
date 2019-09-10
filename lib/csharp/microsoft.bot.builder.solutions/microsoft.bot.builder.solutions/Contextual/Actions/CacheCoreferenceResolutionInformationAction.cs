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

        private async Task CacheCoreferenceResolutionInformation(ITurnContext turnContext)
        {
            string contact = null;
            switch (SkillName)
            {
                case "EmailSkill":
                    contact = await AbstractEmailPreviousContactAsync(turnContext);
                    break;
                case "CalendarSkill":
                    contact = await AbstractCalendarPreviousContactAsync(turnContext);
                    break;
            }

            if (contact != null)
            {
                if (UserContextManager.PreviousContacts.Contains(contact))
                {
                    UserContextManager.PreviousContacts.Remove(contact);
                }

                UserContextManager.PreviousContacts.Add(contact);
            }
        }

        private async Task<string> AbstractEmailPreviousContactAsync(ITurnContext turnContext)
        {
            try
            {
                var skillStateAccessor = ConversationState.CreateProperty<dynamic>(string.Format("{0}State", SkillName));
                var skillState = await skillStateAccessor.GetAsync(turnContext);
                var contacts = skillState.FindContactInfor.Contacts;
                return ((IEnumerable<dynamic>)contacts).Last().EmailAddress.Name;
            }
            catch
            {
                return null;
            }
        }

        private async Task<string> AbstractCalendarPreviousContactAsync(ITurnContext turnContext)
        {
            try
            {
                var skillStateAccessor = ConversationState.CreateProperty<dynamic>(string.Format("{0}State", SkillName));
                var skillState = await skillStateAccessor.GetAsync(turnContext);
                var contacts = skillState.MeetingInfor.ContactInfor.Contacts;
                return ((IEnumerable<dynamic>)contacts).Last().DisplayName;
            }
            catch
            {
                return null;
            }
        }
    }
}

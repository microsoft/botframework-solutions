using EmailSkill.Models;
using EmailSkill.Services;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Contextual;
using Microsoft.Bot.Builder.Solutions.Contextual.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EmailSkill.Contextual
{
    public class EmailContextResolver : IContextResolver
    {
        private EmailSkillState _state;
        private WaterfallStepContext _sc;
        private IServiceManager _serviceManager;

        public EmailContextResolver(EmailSkillState state, WaterfallStepContext sc, IServiceManager serviceManager)
        {
            _state = state;
            _sc = sc;
            _serviceManager = serviceManager;
        }

        public async Task<IList<string>> GetResolvedContactAsync(RelatedEntityInfo relatedEntityInfo)
        {
            // Todo
            if (relatedEntityInfo.PronounType == PossessivePronoun.FirstPerson)
            {
                // To do
                if (Regex.IsMatch(relatedEntityInfo.RelationshipName, "manager", RegexOptions.IgnoreCase))
                {
                    var person = await GetMyManager();
                    if (person != null)
                    {
                        var firstPersonManager = new List<string>();
                        firstPersonManager.Add(person.DisplayName);
                        return firstPersonManager;
                    }
                }
            }
            else if (relatedEntityInfo.PronounType == PossessivePronoun.ThirdPerson && _state.FindContactInfor.Contacts.Count > 0)
            {
                int count = _state.FindContactInfor.Contacts.Count;
                string prename = _state.FindContactInfor.Contacts[count - 1].EmailAddress.Address;

                // To do
                if (Regex.IsMatch(relatedEntityInfo.RelationshipName, "manager", RegexOptions.IgnoreCase))
                {
                    var person = await GetManager(prename);
                    if (person != null)
                    {
                        var thirdPersonManager = new List<string>();
                        thirdPersonManager.Add(person.DisplayName);
                        return thirdPersonManager;
                    }
                }
            }

            return null;
        }

        protected async Task<PersonModel> GetMyManager()
        {
            var token = _state.Token;
            var service = _serviceManager.InitUserService(token, _state.GetUserTimeZone(), _state.MailSourceType);
            return await service.GetMyManagerAsync();
        }

        protected async Task<PersonModel> GetManager(string name)
        {
            var token = _state.Token;
            var service = _serviceManager.InitUserService(token, _state.GetUserTimeZone(), _state.MailSourceType);
            return await service.GetManagerAsync(name);
        }
    }
}

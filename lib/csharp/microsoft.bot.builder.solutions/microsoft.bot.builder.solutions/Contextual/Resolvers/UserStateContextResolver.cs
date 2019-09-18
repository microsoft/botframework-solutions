using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Solutions.Contextual;
using Microsoft.Bot.Builder.Solutions.Contextual.Models;
using Microsoft.Bot.Builder.Solutions.Contextual.Rules;

namespace Microsoft.Bot.Builder.Solutions.Contextual
{
    public class UserStateContextResolver : IContextResolver
    {
        private UserInfoState _userInfoState;
        private List<IRule> _rules = new List<IRule>();

        public UserStateContextResolver(UserInfoState userInfo)
        {
            _userInfoState = userInfo;
        }

        public async Task<IList<string>> GetResolvedContactAsync(RelatedEntityInfo relatedEntityInfo)
        {
            if (relatedEntityInfo.PronounType == PossessivePronoun.FirstPerson)
            {
                var relationshipType = UserRelationship.GetRelationshipType(relatedEntityInfo.RelationshipName);

                IList<string> relationshipContactName;
                if (_userInfoState.RelativeContacts.TryGetValue(relationshipType.RelationshipName, out relationshipContactName))
                {
                    return relationshipContactName;
                }
            }
            else
            {
            }

            return null;
        }

        // ToDo: Put these two parts in base class?
        public async Task<string> ExcuteARRules(AnaphoraResolutionState anaphoraResolutionState)
        {
            foreach (var rule in _rules)
            {
                var name = await rule.GetAnaphoraResolutionResultAsync(anaphoraResolutionState);
                if (!string.IsNullOrEmpty(name))
                {
                    return name;
                }
            }

            return null;
        }

        public void RegisterARRule(IRule rule)
        {
            _rules.Add(rule);
        }

        public List<string> GetPreviousTriggerIntents(List<PreviousTriggerIntent> previousTriggerIntents)
        {
            return previousTriggerIntents.Select(x => x.Utterance).ToList();
        }
    }
}

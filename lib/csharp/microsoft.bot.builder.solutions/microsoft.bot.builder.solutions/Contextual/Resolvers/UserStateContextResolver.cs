using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Solutions.Contextual;
using Microsoft.Bot.Builder.Solutions.Contextual.Models;

namespace Microsoft.Bot.Builder.Solutions.Contextual
{
    public class UserStateContextResolver : IContextResolver
    {
        private UserInfoState _userInfoState;
        private List<string> _previousContact;

        public UserStateContextResolver(UserInfoState userInfo, List<string> previousContact)
        {
            _userInfoState = userInfo;
            _previousContact = previousContact;
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
                GetAnaphoraResolution(PossessivePronoun.ThirdPerson);
            }

            return null;
        }

        public async Task<string> GetAnaphoraResolution(string pron)
        {
            if (pron == PossessivePronoun.ThirdPerson)
            {
                return _previousContact.Last();
            }

            return null;
        }
    }
}

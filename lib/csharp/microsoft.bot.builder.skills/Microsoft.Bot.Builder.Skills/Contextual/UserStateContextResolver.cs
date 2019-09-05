using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills.Contextual.Models;
using Microsoft.Bot.Builder.Skills.Contextual.Resources;

namespace Microsoft.Bot.Builder.Skills.Contextual
{
    public class UserStateContextResolver : IContextResolver
    {
        private UserInfoState _userInfoState;

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

            return null;
        }
    }
}

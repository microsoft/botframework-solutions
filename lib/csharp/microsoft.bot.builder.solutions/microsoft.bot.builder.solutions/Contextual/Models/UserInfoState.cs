namespace Microsoft.Bot.Builder.Solutions.Contextual.Models
{
    using Microsoft.Bot.Builder.Solutions.Contextual.Resources;
    using Microsoft.Bot.Builder.Solutions.Resources;
    using System.Collections.Generic;
    using System.Linq;

    public class UserInfoState
    {
        public string Address { get; set; }

        public IDictionary<string, IList<string>> RelativeContacts;

        public UserInfoState()
        {
            RelativeContacts = new Dictionary<string, IList<string>>();
        }

        public void Clear()
        {
            RelativeContacts.Clear();
        }

        public void SaveRelationshipContact(RelatedEntityInfo relatedEntityInfo, IList<string> result)
        {
            var relationshipType = UserRelationship.GetRelationshipType(relatedEntityInfo.RelationshipName);

            if (RelativeContacts.ContainsKey(relationshipType.RelationshipName))
            {
                RelativeContacts[relationshipType.RelationshipName] = result;
            }
            else
            {
                RelativeContacts.Add(relationshipType.RelationshipName, result);
            }
        }
    }
}

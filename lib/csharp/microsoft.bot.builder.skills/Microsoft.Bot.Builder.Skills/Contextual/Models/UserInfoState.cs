namespace Microsoft.Bot.Builder.Skills.Contextual.Models
{
    using System.Collections.Generic;

    public class UserInfoState
    {
        public string Address { get; set; }

        public IDictionary<UserRelationshipType, string> RelativeContacts;

        public IDictionary<string, string> OtherContacts;

        public UserInfoState()
        {
            RelativeContacts = new Dictionary<UserRelationshipType, string>();
            OtherContacts = new Dictionary<string, string>();
        }

        public void Clear()
        {
            RelativeContacts.Clear();
            OtherContacts.Clear();
        }

        public string GetRelationshipContact(RelatedEntityInfo relatedEntityInfo)
        {
            if (relatedEntityInfo.PronounType == PossessivePronoun.FirstPerson)
            {
                var relationshipType = GetRelationshipType(relatedEntityInfo.RelationshipName);

                var relationshipContactName = string.Empty;
                if (RelativeContacts.TryGetValue(relationshipType, out relationshipContactName))
                {
                    return relationshipContactName;
                }
            }

            return string.Empty;
        }

        public void SaveRelationshipContact(RelatedEntityInfo relatedEntityInfo, string result)
        {
            var relationshipType = GetRelationshipType(relatedEntityInfo.RelationshipName);

            if (RelativeContacts.ContainsKey(relationshipType))
            {
                RelativeContacts[relationshipType] = result;
            }
            else
            {
                RelativeContacts.Add(relationshipType, result);
            }
        }

        private UserRelationshipType GetRelationshipType(string relationship)
        {
            // Todo
            return UserRelationshipType.Wife;
        }
    }
}

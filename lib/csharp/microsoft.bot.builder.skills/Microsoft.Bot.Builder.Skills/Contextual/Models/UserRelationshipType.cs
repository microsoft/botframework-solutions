using Microsoft.Bot.Builder.Skills.Contextual.Resources;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Microsoft.Bot.Builder.Skills.Contextual.Models
{
    public class UserRelationship
    {
        public UserRelationshipType RelationshipType { get; set; }

        public string RelationshipName { get; set; }

        public UserRelationship(UserRelationshipType relationshipType)
        {
            this.RelationshipType = relationshipType;
            this.RelationshipName = relationshipType.ToString();
        }

        public UserRelationship(string relationshipType)
        {
            this.RelationshipType = UserRelationshipType.Unknown;
            this.RelationshipName = relationshipType;
        }

        public static UserRelationship GetRelationshipType(string relationship)
        {
            var result = new UserRelationship(relationship);

            if (GetIsRelationship(RelationshipStrings.Aunt, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Aunt);
            }
            else if (GetIsRelationship(RelationshipStrings.Brother, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Brother);
            }
            else if (GetIsRelationship(RelationshipStrings.Brother_In_Law, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Brother_In_Law);
            }
            else if (GetIsRelationship(RelationshipStrings.Child, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Child);
            }
            else if (GetIsRelationship(RelationshipStrings.Colleague, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Colleague);
            }
            else if (GetIsRelationship(RelationshipStrings.Cousin, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Cousin);
            }
            else if (GetIsRelationship(RelationshipStrings.Daughter, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Daughter);
            }
            else if (GetIsRelationship(RelationshipStrings.Daughter_In_Law, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Daughter_In_Law);
            }
            else if (GetIsRelationship(RelationshipStrings.Family, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Family);
            }
            else if (GetIsRelationship(RelationshipStrings.Father, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Father);
            }
            else if (GetIsRelationship(RelationshipStrings.Father_In_Law, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Father_In_Law);
            }
            else if (GetIsRelationship(RelationshipStrings.Friend, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Friend);
            }
            else if (GetIsRelationship(RelationshipStrings.Grandchild, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Grandchild);
            }
            else if (GetIsRelationship(RelationshipStrings.Granddaughter, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Granddaughter);
            }
            else if (GetIsRelationship(RelationshipStrings.Grandfather, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Grandfather);
            }
            else if (GetIsRelationship(RelationshipStrings.Grandmother, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Grandmother);
            }
            else if (GetIsRelationship(RelationshipStrings.Grandparent, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Grandparent);
            }
            else if (GetIsRelationship(RelationshipStrings.Grandson, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Grandson);
            }
            else if (GetIsRelationship(RelationshipStrings.Husband, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Husband);
            }
            else if (GetIsRelationship(RelationshipStrings.Manager, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Manager);
            }
            else if (GetIsRelationship(RelationshipStrings.Mother, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Mother);
            }
            else if (GetIsRelationship(RelationshipStrings.Mother_in_law, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Mother_in_law);
            }
            else if (GetIsRelationship(RelationshipStrings.Neighbor, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Neighbor);
            }
            else if (GetIsRelationship(RelationshipStrings.Nephew, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Nephew);
            }
            else if (GetIsRelationship(RelationshipStrings.Niece, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Niece);
            }
            else if (GetIsRelationship(RelationshipStrings.Parent, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Parent);
            }
            else if (GetIsRelationship(RelationshipStrings.Partner, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Partner);
            }
            else if (GetIsRelationship(RelationshipStrings.Sibling, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Sibling);
            }
            else if (GetIsRelationship(RelationshipStrings.Sister, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Sister);
            }
            else if (GetIsRelationship(RelationshipStrings.Sister_In_Law, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Sister_In_Law);
            }
            else if (GetIsRelationship(RelationshipStrings.Son, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Son);
            }
            else if (GetIsRelationship(RelationshipStrings.Son_In_Law, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Son_In_Law);
            }
            else if (GetIsRelationship(RelationshipStrings.Step_Daughter, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Step_Daughter);
            }
            else if (GetIsRelationship(RelationshipStrings.Stepfather, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Stepfather);
            }
            else if (GetIsRelationship(RelationshipStrings.Stepmother, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Stepmother);
            }
            else if (GetIsRelationship(RelationshipStrings.Stepsister, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Stepsister);
            }
            else if (GetIsRelationship(RelationshipStrings.Stepson, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Stepson);
            }
            else if (GetIsRelationship(RelationshipStrings.Student, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Student);
            }
            else if (GetIsRelationship(RelationshipStrings.Teacher, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Teacher);
            }
            else if (GetIsRelationship(RelationshipStrings.Uncle, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Uncle);
            }
            else if (GetIsRelationship(RelationshipStrings.Wife, relationship))
            {
                result = new UserRelationship(UserRelationshipType.Wife);
            }

            return result;
        }

        private static bool GetIsRelationship(string relationshipTypeInfo, string relationship)
        {
            var skipItems = relationshipTypeInfo.Split('|');

            for (int i = 0; i < skipItems.Count(); i++)
            {
                skipItems[i] = skipItems[i].Trim();
            }

            var isRelationship = false;
            if (skipItems.Contains<string>(relationship.ToLowerInvariant()))
            {
                isRelationship = true;
            }

            return isRelationship;
        }
    }

    public enum UserRelationshipType
    {
        Unknown,
        Aunt,
        Brother,
        Brother_In_Law,
        Child,
        Colleague,
        Cousin,
        Daughter,
        Daughter_In_Law,
        Family,
        Father,
        Father_In_Law,
        Friend,
        Grandchild,
        Granddaughter,
        Grandfather,
        Grandmother,
        Grandparent,
        Grandson,
        Husband,
        Manager,
        Mother,
        Mother_in_law,
        Neighbor,
        Nephew,
        Niece,
        Parent,
        Partner,
        Sibling,
        Sister,
        Sister_In_Law,
        Son,
        Son_In_Law,
        Step_Daughter,
        Stepfather,
        Stepmother,
        Stepsister,
        Stepson,
        Student,
        Teacher,
        Uncle,
        Wife
    }
}

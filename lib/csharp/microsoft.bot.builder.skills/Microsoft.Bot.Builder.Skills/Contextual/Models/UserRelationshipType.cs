using System;
using System.Collections.Generic;
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

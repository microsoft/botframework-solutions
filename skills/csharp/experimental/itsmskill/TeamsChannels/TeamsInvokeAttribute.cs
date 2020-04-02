namespace ITSMSkill.TeamsChannels
{
    using System;

    [AttributeUsage(AttributeTargets.Class)]
    public class TeamsInvokeAttribute : Attribute
    {
        public string FlowType { get; set; }
    }
}

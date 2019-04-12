using System.Collections.Generic;
using System.Text.RegularExpressions;
using CalendarSkill.Models;
using CalendarSkill.Responses.Shared;

namespace CalendarSkill.Utilities
{
    public static class AskParameterTemplate
    {
        private static Dictionary<AskParameterType, string> templateMapping;

        static AskParameterTemplate()
        {
            // Read the regexs from data file.
            templateMapping = new Dictionary<AskParameterType, string>
            {
                { AskParameterType.AskForDetail, CalendarCommonStrings.AskForDetail },
                { AskParameterType.AskForStartTime, CalendarCommonStrings.AskForStartTime },
                { AskParameterType.AskForEndTime, CalendarCommonStrings.AskForEndTime },
                { AskParameterType.AskForTime, CalendarCommonStrings.AskForTime },
                { AskParameterType.AskForDuration, CalendarCommonStrings.AskForDuration },
                { AskParameterType.AskForLocation, CalendarCommonStrings.AskForLocation },
                { AskParameterType.AskForAttendee, CalendarCommonStrings.AskForAttendee },
                { AskParameterType.AskForTitle, CalendarCommonStrings.AskForTitle },
                { AskParameterType.AskForContent, CalendarCommonStrings.AskForContent }
            };
        }

        public static List<AskParameterType> GetAskParameterTypes(string content)
        {
            // return all parameter types that matches this content
            // for example, when and where will match ask location and ask time
            var types = new List<AskParameterType>();
            if (string.IsNullOrEmpty(content))
            {
                return types;
            }

            foreach (AskParameterType type in templateMapping.Keys)
            {
                var regex = new Regex(templateMapping[type]);
                if (regex.IsMatch(content))
                {
                    types.Add(type);
                }
            }

            return types;
        }
    }
}

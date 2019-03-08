using CalendarSkill.Dialogs.Shared.Resources.Strings;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace CalendarSkill.Models.Resources
{
    public static class AskParameterTemplate
    {
        private static Dictionary<AskParameterType, string> templateMapping;

        static AskParameterTemplate()
        {
            // Read the regexs from data file.
            templateMapping = new Dictionary<AskParameterType, string>();

            templateMapping.Add(AskParameterType.AskForDetail, CalendarCommonStrings.AskForDetail);
            templateMapping.Add(AskParameterType.AskForStartTime, CalendarCommonStrings.AskForStartTime);
            templateMapping.Add(AskParameterType.AskForEndTime, CalendarCommonStrings.AskForEndTime);
            templateMapping.Add(AskParameterType.AskForTime, CalendarCommonStrings.AskForTime);
            templateMapping.Add(AskParameterType.AskForDuration, CalendarCommonStrings.AskForDuration);
            templateMapping.Add(AskParameterType.AskForLocation, CalendarCommonStrings.AskForLocation);
            templateMapping.Add(AskParameterType.AskForAttendee, CalendarCommonStrings.AskForAttendee);
            templateMapping.Add(AskParameterType.AskForTitle, CalendarCommonStrings.AskForTitle);
            templateMapping.Add(AskParameterType.AskForContent, CalendarCommonStrings.AskForContent);
        }

        public static List<AskParameterType> GetAskParameterTypes(string content)
        {
            // return all parameter types that matches this content
            // for example, when and where will match ask location and ask time
            List<AskParameterType> types = new List<AskParameterType>();
            if (string.IsNullOrEmpty(content))
            {
                return types;
            }

            foreach (AskParameterType type in templateMapping.Keys)
            {
                Regex regex = new Regex(templateMapping[type]);
                if (regex.IsMatch(content))
                {
                    types.Add(type);
                }
            }

            return types;
        }
    }
}

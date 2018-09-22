using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;

namespace CalendarSkill
{
    public class CalendarSkillHelper
    {
        public static async Task<Calendar> GetLuisResult(ITurnContext context, CalendarSkillAccessors accessors, CalendarSkillServices services, CancellationToken cancellationToken)
        {
            var state = await accessors.CalendarSkillState.GetAsync(context);

            Calendar luisResult = null;

            if (state.LuisResultPassedFromSkill != null)
            {
                luisResult = (Calendar)state.LuisResultPassedFromSkill;
            }
            else
            {
                luisResult = await services.LuisRecognizer.RecognizeAsync<Calendar>(context, cancellationToken);
            }

            await DigestCalendarLuisResult(context, accessors, luisResult);
            return luisResult;
        }

        /// <summary>
        /// set luis result to conversation state.
        /// </summary>
        /// <param name="context">context.</param>
        /// <param name="accessors">accessors.</param>
        /// <param name="luisResult">LUIS result.</param>
        /// <returns>representing the asynchronous operation.</returns>
        public static async Task DigestCalendarLuisResult(ITurnContext context, CalendarSkillAccessors accessors, Calendar luisResult)
        {
            try
            {
                var state = await accessors.CalendarSkillState.GetAsync(context);

                var entity = luisResult.Entities;

                if (entity.Subject != null)
                {
                    state.Title = entity.Subject[0];
                }

                if (entity.ContactName != null)
                {
                    foreach (var name in entity.ContactName)
                    {
                        if (!state.AttendeesNameList.Contains(name))
                        {
                            state.AttendeesNameList.Add(name);
                        }
                    }
                }

                if (entity.ordinal != null)
                {
                    try
                    {
                        var eventList = state.SummaryEvents;
                        var value = entity.ordinal[0];
                        var num = int.Parse(value.ToString());
                        if (eventList != null && num > 0)
                        {
                            var currentList = eventList.GetRange(0, Math.Min(CalendarSkillState.PageSize, eventList.Count));
                            if (num <= currentList.Count)
                            {
                                state.ReadOutEvents.Clear();
                                state.ReadOutEvents.Add(currentList[num - 1]);
                            }
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }

                if (entity.number != null && entity.ordinal.Length == 0)
                {
                    try
                    {
                        var eventList = state.SummaryEvents;
                        var value = entity.ordinal[0];
                        var num = int.Parse(value.ToString());
                        if (eventList != null && num > 0)
                        {
                            var currentList = eventList.GetRange(0, Math.Min(CalendarSkillState.PageSize, eventList.Count));
                            if (num <= currentList.Count)
                            {
                                state.ReadOutEvents.Clear();
                                state.ReadOutEvents.Add(currentList[num - 1]);
                            }
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
            catch
            {
                // put log here
            }
        }

        // Workaround as DateTimePrompt only return a local time.
        public static bool IsRelativeTime(string userInput, string resolverResult, string timex)
        {
            if (userInput.Contains("ago") ||
                userInput.Contains("before") ||
                userInput.Contains("later") ||
                userInput.Contains("next"))
            {
                return true;
            }

            if (userInput.Contains("today") ||
                userInput.Contains("now") ||
                userInput.Contains("yesterday") ||
                userInput.Contains("tomorrow"))
            {
                return true;
            }

            if (timex == "PRESENT_REF")
            {
                return true;
            }

            return false;
        }

        public static class TimeZoneConverter
        {
            private static IDictionary<string, string> ianaToWindowsMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            private static IDictionary<string, string> windowsToIanaMap = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

            public static string IanaToWindows(string ianaTimeZoneId)
            {
                LoadData();
                if (ianaToWindowsMap.ContainsKey(ianaTimeZoneId))
                {
                    return ianaToWindowsMap[ianaTimeZoneId];
                }

                throw new InvalidTimeZoneException();
            }

            public static string WindowsToIana(string windowsTimeZoneId)
            {
                LoadData();
                if (windowsToIanaMap.ContainsKey($"001|{windowsTimeZoneId}"))
                {
                    return windowsToIanaMap[$"001|{windowsTimeZoneId}"];
                }

                throw new InvalidTimeZoneException();
            }

            private static void LoadData()
            {
                using (var mappingFile = new FileStream("Dialogs/Shared/Resources/WindowsIanaMapping", FileMode.Open))
                using (var sr = new StreamReader(mappingFile))
                {
                    string line;
                    while ((line = sr.ReadLine()) != null)
                    {
                        var table = line.Split(",");
                        var windowsId = table[0];
                        var territory = table[1];
                        var ianaIdList = table[2].Split(" ");
                        if (!windowsToIanaMap.ContainsKey($"{territory}|{windowsId}"))
                        {
                            windowsToIanaMap.Add($"{territory}|{windowsId}", ianaIdList[0]);
                        }

                        foreach (var ianaId in ianaIdList)
                        {
                            if (!ianaToWindowsMap.ContainsKey(ianaId))
                            {
                                ianaToWindowsMap.Add(ianaId, windowsId);
                            }
                        }
                    }
                }
            }
        }
    }
}

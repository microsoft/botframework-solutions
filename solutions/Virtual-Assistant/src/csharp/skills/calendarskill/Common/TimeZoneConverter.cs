using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace CalendarSkill.Common
{
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
            var dir = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            var resDir = Path.Combine(dir, @"Common\WindowsIanaMapping");

            using (var mappingFile = new FileStream(resDir, FileMode.Open))
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

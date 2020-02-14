using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

namespace JsonConverter
{
    partial class Program
    {
        public static Dictionary<string, List<string>> ConvertedTextsFiles { get; set; } = new Dictionary<string, List<string>>();

        // eg: xx.zh-cn.json. Its locale is zh-cn.
        // xx.lg. Its locale is en-us.
        public static string GetLocale(string file)
        {
            var localeDic = new Dictionary<string, string>
            {
                { "en","en-us" },
                { "de","de-de" },
                { "fr","fr-fr" },
                { "it","it-it" },
                { "es","es-es" },
                { "zh","zh-cn" },
            };

            var fileName = Path.GetFileName(file);
            var nameAndExts = fileName.Split(".");
            if (localeDic.ContainsKey(nameAndExts[nameAndExts.Length - 2]))
            {
                return localeDic[nameAndExts[1]];
            }
            else
            {
                return localeDic["en"];
            }
        }

        // eg: POISharedResponses.es. Its dialog name is POIShared.
        public static string GetDialogName(string file)
        {
            var fileName = Path.GetFileName(file).Split(".")[0];
            return fileName.Substring(0, fileName.Length - "Responses".Length);
        }
    }
}

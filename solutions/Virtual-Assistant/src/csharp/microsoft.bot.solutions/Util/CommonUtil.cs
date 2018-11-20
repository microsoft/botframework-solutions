using System;
using System.Collections.Generic;
using System.Text;

namespace Microsoft.Bot.Solutions.Util
{
    public class CommonUtil
    {
        public static readonly double ScoreThreshold = 0.5f;

        public static readonly int MaxReadSize = 3;

        public static readonly int MaxDisplaySize = 6;

        private const string _readMore = "more";

        public static bool IsReadMoreIntent(string userInput)
        {
            return userInput.ToLowerInvariant().Contains(_readMore);
        }
    }
}

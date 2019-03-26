using System;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.RegularExpressions;
using AdaptiveCards;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Schema;
using Newtonsoft.Json;

namespace EmailSkill.Util
{
    public class AdaptiveCardHelper
    {
        public static readonly string DefaultAvatarIconPathFormat = "https://ui-avatars.com/api/?name={0}";

        public static readonly string DefaultMe = "Me";

        public static readonly int MaxDisplayRecipientNum = 5;

        public static readonly string DefaultIcon = "data:image/gif;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQImWNgYGBgAAAABQABh6FO1AAAAABJRU5ErkJggg==";
    }
}

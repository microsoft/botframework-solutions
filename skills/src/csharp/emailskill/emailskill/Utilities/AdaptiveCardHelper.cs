﻿using System;
using EmailSkill.Models;

namespace EmailSkill.Utilities
{
    public class AdaptiveCardHelper
    {
        public static readonly string DefaultAvatarIconPathFormat = "https://ui-avatars.com/api/?name={0}";

        public static readonly string DefaultMe = "Me";

        public static readonly int MaxDisplayRecipientNum = 5;

        public static readonly string DefaultIcon = "data:image/gif;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQImWNgYGBgAAAABQABh6FO1AAAAABJRU5ErkJggg==";

        public static readonly string ImportantIcon = "data:image/svg+xml;base64,PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0idXRmLTgiPz4KPCEtLSBHZW5lcmF0b3I6IEFk%0D%0Ab2JlIElsbHVzdHJhdG9yIDIzLjAuMiwgU1ZHIEV4cG9ydCBQbHVnLUluIC4gU1ZHIFZlcnNpb246%0D%0AIDYuMDAgQnVpbGQgMCkgIC0tPgo8c3ZnIHZlcnNpb249IjEuMSIgYmFzZVByb2ZpbGU9InRpbnki%0D%0AIGlkPSJMYXllcl8xIiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHhtbG5zOnhs%0D%0AaW5rPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5L3hsaW5rIgoJIHg9IjBweCIgeT0iMHB4IiB2aWV3%0D%0AQm94PSIwIDAgMjQgMjQiIHhtbDpzcGFjZT0icHJlc2VydmUiPgo8cGF0aCBmaWxsPSIjQzUwRjFG%0D%0AIiBkPSJNMTAuNywxNi41di0xNWgzdjE1SDEwLjd6IE0xMC43LDIyLjV2LTNoM3YzSDEwLjd6Ii8+%0D%0ACjwvc3ZnPgo=";

        public static readonly string AttachmentIcon = "data:image/svg+xml;base64,PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0idXRmLTgiPz4KPCEtLSBHZW5lcmF0b3I6IEFk%0D%0Ab2JlIElsbHVzdHJhdG9yIDIzLjAuMiwgU1ZHIEV4cG9ydCBQbHVnLUluIC4gU1ZHIFZlcnNpb246%0D%0AIDYuMDAgQnVpbGQgMCkgIC0tPgo8c3ZnIHZlcnNpb249IjEuMSIgYmFzZVByb2ZpbGU9InRpbnki%0D%0AIGlkPSJMYXllcl8xIiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHhtbG5zOnhs%0D%0AaW5rPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5L3hsaW5rIgoJIHg9IjBweCIgeT0iMHB4IiB2aWV3%0D%0AQm94PSIwIDAgMjQgMjQiIHhtbDpzcGFjZT0icHJlc2VydmUiPgo8cGF0aCBmaWxsPSIjNzY3Njc2%0D%0AIiBkPSJNMTguMSw0LjV2MTQuMmMwLDAuNy0wLjEsMS40LTAuNCwyUzE3LDIyLDE2LjUsMjIuNXMt%0D%0AMSwwLjgtMS43LDEuMXMtMS4zLDAuNC0yLDAuNHMtMS40LTAuMS0yLTAuNAoJUzkuNiwyMyw5LjEs%0D%0AMjIuNXMtMC44LTEtMS4xLTEuN3MtMC40LTEuMy0wLjQtMnYtMTVjMC0wLjUsMC4xLTEsMC4zLTEu%0D%0ANXMwLjUtMC44LDAuOC0xLjJzMC43LTAuNiwxLjItMC44UzEwLjgsMCwxMS4zLDAKCXMxLDAuMSwx%0D%0ALjUsMC4zczAuOCwwLjUsMS4yLDAuOHMwLjYsMC43LDAuOCwxLjJzMC4zLDAuOSwwLjMsMS41djE1%0D%0AYzAsMC4zLTAuMSwwLjYtMC4yLDAuOXMtMC4zLDAuNS0wLjUsMC43cy0wLjQsMC40LTAuNywwLjUK%0D%0ACVMxMy4yLDIxLDEyLjgsMjFTMTIuMiwyMSwxMiwyMC45cy0wLjUtMC4zLTAuNy0wLjVzLTAuNC0w%0D%0ALjQtMC41LTAuN3MtMC4yLTAuNi0wLjItMC45VjZoMS41djEyLjdjMCwwLjIsMC4xLDAuNCwwLjIs%0D%0AMC41CglzMC4zLDAuMiwwLjUsMC4yczAuNC0wLjEsMC41LTAuMnMwLjItMC4zLDAuMi0wLjV2LTE1%0D%0AYzAtMC4zLTAuMS0wLjYtMC4yLTAuOXMtMC4zLTAuNS0wLjUtMC43cy0wLjQtMC40LTAuNy0wLjVz%0D%0ALTAuNi0wLjItMC45LTAuMgoJcy0wLjYsMC4xLTAuOSwwLjJTOS45LDIsOS43LDIuMlM5LjQsMi42%0D%0ALDkuMywyLjlTOS4xLDMuNSw5LjEsMy44djE1YzAsMC41LDAuMSwxLDAuMywxLjVzMC41LDAuOCww%0D%0ALjgsMS4yczAuNywwLjYsMS4yLDAuOAoJczAuOSwwLjMsMS41LDAuM3MxLTAuMSwxLjUtMC4zczAu%0D%0AOC0wLjUsMS4yLTAuOHMwLjYtMC43LDAuOC0xLjJzMC4zLTAuOSwwLjMtMS41VjQuNUgxOC4xeiIv%0D%0APgo8L3N2Zz4K";

        public static string GetSourceType(MailSource mailSource)
        {
            var result = mailSource.ToString();
            switch (mailSource)
            {
                case MailSource.Microsoft:
                    result = "Microsoft Graph";
                    break;
                case MailSource.Google:
                    result = "Google";
                    break;
                default:
                    throw new Exception("Source Type not Defined.");
            }

            return result;
        }
    }
}

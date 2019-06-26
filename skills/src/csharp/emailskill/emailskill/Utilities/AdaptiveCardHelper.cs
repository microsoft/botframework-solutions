using System;
using EmailSkill.Models;

namespace EmailSkill.Utilities
{
    public class AdaptiveCardHelper
    {
        public static readonly string DefaultAvatarIconPathFormat = "https://ui-avatars.com/api/?name={0}";

        public static readonly string DefaultMe = "Me";

        public static readonly int MaxDisplayRecipientNum = 5;

        public static readonly string DefaultIcon = "data:image/gif;base64,iVBORw0KGgoAAAANSUhEUgAAAAEAAAABCAYAAAAfFcSJAAAADUlEQVQImWNgYGBgAAAABQABh6FO1AAAAABJRU5ErkJggg==";

        public static readonly string ImportantIcon = "data:image/svg+xml;base64,PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0idXRmLTgiPz4KPCEtLSBHZW5lcmF0b3I6IEFkb2JlIElsbHVzdHJhdG9yIDIzLjAuMiwgU1ZHIEV4cG9ydCBQbHVnLUluIC4gU1ZHIFZlcnNpb246IDYuMDAgQnVpbGQgMCkgIC0tPgo8c3ZnIHZlcnNpb249IjEuMSIgYmFzZVByb2ZpbGU9InRpbnkiIGlkPSJMYXllcl8xIiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHhtbG5zOnhsaW5rPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5L3hsaW5rIgoJIHg9IjBweCIgeT0iMHB4IiB2aWV3Qm94PSIwIDAgMjQgMjQiIHhtbDpzcGFjZT0icHJlc2VydmUiPgo8cGF0aCBmaWxsPSIjQzUwRjFGIiBkPSJNMTAuNywxNi41di0xNWgzdjE1SDEwLjd6IE0xMC43LDIyLjV2LTNoM3YzSDEwLjd6Ii8+Cjwvc3ZnPgo=";

        public static readonly string AttachmentIcon = "data:image/svg+xml;base64,PD94bWwgdmVyc2lvbj0iMS4wIiBlbmNvZGluZz0idXRmLTgiPz4KPCEtLSBHZW5lcmF0b3I6IEFkb2JlIElsbHVzdHJhdG9yIDIzLjAuMiwgU1ZHIEV4cG9ydCBQbHVnLUluIC4gU1ZHIFZlcnNpb246IDYuMDAgQnVpbGQgMCkgIC0tPgo8c3ZnIHZlcnNpb249IjEuMSIgYmFzZVByb2ZpbGU9InRpbnkiIGlkPSJMYXllcl8xIiB4bWxucz0iaHR0cDovL3d3dy53My5vcmcvMjAwMC9zdmciIHhtbG5zOnhsaW5rPSJodHRwOi8vd3d3LnczLm9yZy8xOTk5L3hsaW5rIgoJIHg9IjBweCIgeT0iMHB4IiB2aWV3Qm94PSIwIDAgMjQgMjQiIHhtbDpzcGFjZT0icHJlc2VydmUiPgo8cGF0aCBmaWxsPSIjNzY3Njc2IiBkPSJNMTguMSw0LjV2MTQuMmMwLDAuNy0wLjEsMS40LTAuNCwyUzE3LDIyLDE2LjUsMjIuNXMtMSwwLjgtMS43LDEuMXMtMS4zLDAuNC0yLDAuNHMtMS40LTAuMS0yLTAuNAoJUzkuNiwyMyw5LjEsMjIuNXMtMC44LTEtMS4xLTEuN3MtMC40LTEuMy0wLjQtMnYtMTVjMC0wLjUsMC4xLTEsMC4zLTEuNXMwLjUtMC44LDAuOC0xLjJzMC43LTAuNiwxLjItMC44UzEwLjgsMCwxMS4zLDAKCXMxLDAuMSwxLjUsMC4zczAuOCwwLjUsMS4yLDAuOHMwLjYsMC43LDAuOCwxLjJzMC4zLDAuOSwwLjMsMS41djE1YzAsMC4zLTAuMSwwLjYtMC4yLDAuOXMtMC4zLDAuNS0wLjUsMC43cy0wLjQsMC40LTAuNywwLjUKCVMxMy4yLDIxLDEyLjgsMjFTMTIuMiwyMSwxMiwyMC45cy0wLjUtMC4zLTAuNy0wLjVzLTAuNC0wLjQtMC41LTAuN3MtMC4yLTAuNi0wLjItMC45VjZoMS41djEyLjdjMCwwLjIsMC4xLDAuNCwwLjIsMC41CglzMC4zLDAuMiwwLjUsMC4yczAuNC0wLjEsMC41LTAuMnMwLjItMC4zLDAuMi0wLjV2LTE1YzAtMC4zLTAuMS0wLjYtMC4yLTAuOXMtMC4zLTAuNS0wLjUtMC43cy0wLjQtMC40LTAuNy0wLjVzLTAuNi0wLjItMC45LTAuMgoJcy0wLjYsMC4xLTAuOSwwLjJTOS45LDIsOS43LDIuMlM5LjQsMi42LDkuMywyLjlTOS4xLDMuNSw5LjEsMy44djE1YzAsMC41LDAuMSwxLDAuMywxLjVzMC41LDAuOCwwLjgsMS4yczAuNywwLjYsMS4yLDAuOAoJczAuOSwwLjMsMS41LDAuM3MxLTAuMSwxLjUtMC4zczAuOC0wLjUsMS4yLTAuOHMwLjYtMC43LDAuOC0xLjJzMC4zLTAuOSwwLjMtMS41VjQuNUgxOC4xeiIvPgo8L3N2Zz4K";

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

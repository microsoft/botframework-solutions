using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace EmailSkill.Util
{
    public class Util
    {
        public static bool IsEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
            {
                return false;
            }

            // Match email address, e.g. a@b.com
            return Regex.IsMatch(email, @"^\w+([-+.]\w+)*@\w+([-.]\w+)*\.\w+([-.]\w+)*$");
        }

        public static bool IsTargetAttachment(string attachmentType, string attachmentFileName)
        {
            if (attachmentType.Contains("doc") || attachmentType.Contains("word"))
            {
                if (attachmentFileName.Contains("doc"))
                {
                    return true;
                }

                return false;
            }
            else if (attachmentType.Contains("ppt") || attachmentType.Contains("power point") || attachmentType.Contains("powerpoint"))
            {
                if (attachmentFileName.Contains("ppt"))
                {
                    return true;
                }

                return false;
            }
            else if (attachmentType.Contains("text"))
            {
                if (attachmentFileName.Contains("txt"))
                {
                    return true;
                }

                return false;
            }

            return true;
        }
    }
}

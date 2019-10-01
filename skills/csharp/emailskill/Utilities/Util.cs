using System.Text.RegularExpressions;

namespace EmailSkill.Utilities
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
    }
}

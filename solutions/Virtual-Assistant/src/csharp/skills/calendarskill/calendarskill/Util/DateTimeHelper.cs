using CalendarSkill.Dialogs.Shared.Resources.Strings;

namespace CalendarSkill.Util
{
    public class DateTimeHelper
    {
        public static string ConvertNumberToDateTimeString(string numberString, bool convertToDate)
        {
            // convert exact number to date or time
            // if need convert to date, add "st", "rd", or "th" after the number
            // if need convert to time, add ":00" after the number
            if (int.TryParse(numberString, out var number))
            {
                if (convertToDate)
                {
                    if (number > 0 && number <= 31)
                    {
                        if (number % 10 == 1 && number != 11)
                        {
                            return string.Format(CalendarCommonStrings.OrdinalSuffixSt, numberString);
                        }
                        else if (number % 10 == 2 && number != 12)
                        {
                            return string.Format(CalendarCommonStrings.OrdinalSuffixNd, numberString);
                        }
                        else if (number % 10 == 3 && number != 13)
                        {
                            return string.Format(CalendarCommonStrings.OrdinalSuffixRd, numberString);
                        }
                        else
                        {
                            return string.Format(CalendarCommonStrings.OrdinalSuffixTh, numberString);
                        }
                    }
                }
                else
                {
                    if (number >= 0 && number <= 24)
                    {
                        return numberString + ":00";
                    }
                }
            }

            return numberString;
        }
    }
}

namespace RestaurantBooking.Helpers
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    public static class MatchName
    {
        private const string _propertyToMatch = "Name";

        public static string GetSoundEx(string word)
        {
            // The length of the returned code.
            var length = 4;

            // Value to return.
            var value = string.Empty;

            // The size of the word to process.
            var size = word.Length;

            // The word must be at least two characters in length.
            if (size <= 1)
            {
                return value;
            }

            // Convert the word to uppercase characters.
            word = word.ToUpper(CultureInfo.InvariantCulture);

            // Convert the word to a character array.
            var chars = word.ToCharArray();

            // Buffer to hold the character codes.
            var buffer = new StringBuilder { Length = 0 };

            // The current and previous character codes.
            var prevCode = 0;
            var currCode = 0;

            // Add the first character to the buffer.
            buffer.Append(chars[0]);

            // Loop through all the characters and convert them to the proper character code.
            for (var i = 1; i < size; i++)
            {
                switch (chars[i])
                {
                    case 'A':
                    case 'E':
                    case 'I':
                    case 'O':
                    case 'U':
                    case 'H':
                    case 'W':
                    case 'Y':
                        currCode = 0;
                        break;
                    case 'B':
                    case 'F':
                    case 'P':
                    case 'V':
                        currCode = 1;
                        break;
                    case 'C':
                    case 'G':
                    case 'J':
                    case 'K':
                    case 'Q':
                    case 'S':
                    case 'X':
                    case 'Z':
                        currCode = 2;
                        break;
                    case 'D':
                    case 'T':
                        currCode = 3;
                        break;
                    case 'L':
                        currCode = 4;
                        break;
                    case 'M':
                    case 'N':
                        currCode = 5;
                        break;
                    case 'R':
                        currCode = 6;
                        break;
                }

                // Check if the current code is the same as the previous code.
                if (currCode != prevCode)
                {
                    // Check to see if the current code is 0 (a vowel); do not process vowels.
                    if (currCode != 0)
                    {
                        buffer.Append(currCode);
                    }
                }

                // Set the previous character code.
                prevCode = currCode;

                // If the buffer size meets the length limit, exit the loop.
                if (buffer.Length == length)
                {
                    break;
                }
            }

            // Pad the buffer, if required.
            size = buffer.Length;
            if (size < length)
            {
                buffer.Append('0', length - size);
            }

            // Set the value to return.
            value = buffer.ToString();

            // Return the value.
            return value;
        }

        public static T GetMatchUsingSoundex<T>(string toMatch, List<T> inputList, List<string> ignoreList, Func<T, string> keyExpression = null)
        {
            var result = default(T);

            if (string.IsNullOrEmpty(toMatch))
            {
                return result;
            }

            if (keyExpression == null && typeof(T).GetProperty(_propertyToMatch) == null)
            {
                return result;
            }

            keyExpression = keyExpression ?? GetValueFromNameProperty;

            var wordsInUtterance = toMatch.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);
            var maxMatchedWords = 0;

            foreach (var input in inputList)
            {
                var name = keyExpression(input);

                if (string.IsNullOrEmpty(name))
                {
                    continue;
                }

                // input.Name.ToLower() == "call concierge")
                if (ignoreList.Exists(n => n.ToLower(CultureInfo.InvariantCulture) == name))
                {
                    continue;
                }

                var resNameArray = name.Split(new[] { " " }, StringSplitOptions.RemoveEmptyEntries);

                var numOfWordsMatched = 0;

                foreach (var word in wordsInUtterance)
                {
                    var usrStrSoundex = GetSoundEx(word);
                    numOfWordsMatched += resNameArray.Count(resStr => usrStrSoundex == GetSoundEx(resStr));
                }

                if (maxMatchedWords < numOfWordsMatched)
                {
                    maxMatchedWords = numOfWordsMatched;
                    result = input;
                }
            }

            return result;
        }

        private static string GetValueFromNameProperty<T>(T input)
        {
            var type = input.GetType();

            var prop = type.GetProperty(_propertyToMatch);

            var value = prop.GetValue(input);

            return value is string ? value.ToString() : null;
        }
    }
}

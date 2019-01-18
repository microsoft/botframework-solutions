// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill.Common
{
    using System.Collections.Generic;
    using System.Globalization;
    using System.Linq;
    using System.Text.RegularExpressions;

    /// <summary>
    /// English number normalizer.
    /// </summary>
    public class NumberNormalizer
    {
        private static readonly Regex DigitNumber = new Regex("[+-]?([.][0-9]+|[0-9]+([.][0-9]+)?)([Ee][+-]?[0-9]+)?", RegexOptions.Compiled);

        /// <summary>
        /// Split the given string into numbers (digits or spelled out) and non-number substrings in between.
        /// </summary>
        /// <param name="str">A string containing digits and/or spelled out numbers, possibly mixed with text, whitespace, or other non-numbers.</param>
        /// <returns>The numbers in the given string and the substrings between them.</returns>
        public IList<Chunk> SplitNumbers(string str)
        {
            IList<Chunk> chunks = new List<Chunk>();

            int pos = 0;
            int prev_number_end = 0;
            while (pos < str.Length)
            {
                var rest = str.Substring(pos);

                var digit_number_match = DigitNumber.Match(rest);
                if (digit_number_match.Success && digit_number_match.Index == 0)
                {
                    AddNonNumberChunk(chunks, str, prev_number_end, pos);

                    var value = digit_number_match.Value;
                    chunks.Add(new Chunk(value, double.Parse(value, CultureInfo.InvariantCulture)));

                    pos += digit_number_match.Length;
                    prev_number_end = pos;
                }
                else
                {
                    Parser parser = new Parser(rest);
                    var number = parser.Parse();
                    if (number != null)
                    {
                        AddNonNumberChunk(chunks, str, prev_number_end, pos);

                        var original = str.Substring(pos, parser.GetPosition());
                        chunks.Add(new Chunk(original, number.Value));

                        pos += parser.GetPosition();
                        prev_number_end = pos;
                    }
                    else
                    {
                        // Not a number.
                        if (char.IsHighSurrogate(str.ElementAt(pos)))
                        {
                            ++pos;
                        }

                        ++pos;
                    }
                }
            }

            AddNonNumberChunk(chunks, str, prev_number_end, pos);

            return chunks;
        }

        private void AddNonNumberChunk(IList<Chunk> chunks, string str, int start, int end)
        {
            if (start < end)
            {
                chunks.Add(new Chunk(str.Substring(start, end - start)));
            }
        }
    }
}
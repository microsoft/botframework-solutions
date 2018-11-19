// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace AutomotiveSkill
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
        private static readonly Regex DIGIT_NUMBER = new Regex("[+-]?([.][0-9]+|[0-9]+([.][0-9]+)?)([Ee][+-]?[0-9]+)?", RegexOptions.Compiled);

        /// <summary>
        /// Split the given string into numbers (digits or spelled out) and non-number substrings in between.
        /// </summary>
        /// <param name="str">A string containing digits and/or spelled out numbers, possibly mixed with text, whitespace, or other non-numbers.</param>
        /// <returns>The numbers in the given string and the substrings between them.</returns>
        public IList<Number.Chunk> SplitNumbers(string str)
        {
            IList<Number.Chunk> chunks = new List<Number.Chunk>();

            int pos = 0;
            int prev_number_end = 0;
            while (pos < str.Length)
            {
                var rest = str.Substring(pos);

                var digit_number_match = DIGIT_NUMBER.Match(rest);
                if (digit_number_match.Success && digit_number_match.Index == 0)
                {
                    add_non_number_chunk(chunks, str, prev_number_end, pos);

                    var value = digit_number_match.Value;
                    chunks.Add(new Number.Chunk(value, double.Parse(value, CultureInfo.InvariantCulture)));

                    pos += digit_number_match.Length;
                    prev_number_end = pos;
                }
                else
                {
                    Number.Parser parser = new Number.Parser(rest);
                    var number = parser.parse();
                    if (number != null)
                    {
                        add_non_number_chunk(chunks, str, prev_number_end, pos);

                        var original = str.Substring(pos, parser.getPosition());
                        chunks.Add(new Number.Chunk(original, number.Value));

                        pos += parser.getPosition();
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

            add_non_number_chunk(chunks, str, prev_number_end, pos);

            return chunks;
        }

        private void add_non_number_chunk(IList<Number.Chunk> chunks, string str, int start, int end)
        {
            if (start < end)
            {
                chunks.Add(new Number.Chunk(str.Substring(start, end - start)));
            }
        }
    }

    namespace Number
    {
        /// <summary>
        /// A chunk of a string, which can be either a number or a non-number
        /// substring between two numbers. Iff it is a number, its numeric value is
        /// given in addition to the substring.
        /// </summary>
        public class Chunk
        {
            public string value { get; }
            public double? numeric_value { get; }

            public Chunk()
            { }

            public Chunk(string value)
            {
                this.value = value;
            }

            public Chunk(string value, double numeric_value)
            {
                this.value = value;
                this.numeric_value = numeric_value;
            }
        }

        public class Parser
        {
            private static readonly Regex WORD = new Regex("[\\w']+", RegexOptions.Compiled);

            private static readonly IDictionary<string, int> ones = new Dictionary<string, int>
            {
                { "zero",  0},
                { "one",   1},
                { "two",   2},
                { "three", 3},
                { "four",  4},
                { "five",  5},
                { "six",   6},
                { "seven", 7},
                { "eight", 8},
                { "nine",  9},
            };

            private static readonly IDictionary<string, int> teens = new Dictionary<string, int>
            {
                { "ten",       10},
                { "eleven",    11},
                { "twelve",    12},
                { "thirteen",  13},
                { "fourteen",  14},
                { "fifteen",   15},
                { "sixteen",   16},
                { "seventeen", 17},
                { "eighteen",  18},
                { "nineteen",  19},
            };

            private static readonly IDictionary<string, int> tens = new Dictionary<string, int>
            {
                { "twenty",  20},
                { "thirty",  30},
                { "forty",   40},
                { "fourty",  40},
                { "fifty",   50},
                { "sixty",   60},
                { "seventy", 70},
                { "eighty",  80},
                { "ninety",  90},
            };

            private readonly string phrase;
            private int phrasePosition;
            private LinkedList<string> queue;
            private LinkedList<int> positions;
            private int position;

            public Parser(string phrase)
            {
                this.phrase = phrase;
                this.phrasePosition = 0;
                this.queue = new LinkedList<string>();
                this.positions = new LinkedList<int>();
                this.position = 0;
            }

            /// <summary>
            /// Parse a number.
            /// </summary>
            /// <returns>The parsed number or null if the given string cannot be parsed.</returns>
            public int? parse()
            {
                // We skip non-word characters during parsing because we want to allow
                // non-word characters in between the tokens of the number phrase
                // (e.g., "twenty one"). But at the beginning of the whole number
                // phrase, we only allow word characters.
                var match = WORD.Match(this.phrase, this.phrasePosition);
                if (!match.Success || match.Index != this.phrasePosition)
                {
                    return null;
                }

                return parse_thousands(0);
            }

            /// <returns>The position of the end of the match.</returns>
            public int getPosition()
            {
                return this.position;
            }


            private bool enqueue(int size = 1)
            {
                while (this.queue.Count() < size)
                {
                    var match = WORD.Match(this.phrase, this.phrasePosition);
                    if (!match.Success)
                    {
                        return false;
                    }
                    this.queue.AddLast(match.Value);
                    this.positions.AddLast(match.Index + match.Length);
                }
                return true;
            }

            private string peek(int i = 0)
            {
                return this.queue.ElementAt(i);
            }

            private void pop()
            {
                this.queue.RemoveFirst();
                this.positions.RemoveFirst();
            }

            private void accept()
            {
                this.position = this.positions.First();
            }

            /// <summary>
            /// ones : "zero" | "one" | "two" | ...
            /// </summary>
            private int? parse_ones(int value)
            {
                if (!enqueue())
                {
                    return null;
                }
                var word = peek();

                if (ones.TryGetValue(word, out value))
                {
                    accept();
                    pop();
                    return value;
                }

                return null;
            }

            /// <summary>
            /// tens : "ten" | "eleven" | ...
            ///      | ("twenty" | "thirty" | ...) ones?
            ///      | ones
            /// </summary>
            private int? parse_tens(int value)
            {
                if (!enqueue())
                {
                    return null;
                }
                var word = peek();

                if (teens.TryGetValue(word, out var found))
                {
                    value = found;
                    accept();
                    pop();
                    return value;
                }

                if (tens.TryGetValue(word, out found))
                {
                    value = found;
                    accept();
                    pop();

                    var ones = parse_ones(0);
                    if (ones != null)
                    {
                        value += ones.Value;
                    }

                    return value;
                }

                return parse_ones(value);
            }

            /// <summary>
            /// hundreds : ("a" | tens)? "hundred" ("and"? tens)?
            ///          | tens
            /// </summary>
            private int? parse_hundreds(int value)
            {
                if (enqueue() && peek() == "hundred")
                {
                    value = 1;
                }
                else if (enqueue(2) && peek(0) == "a" && peek(1) == "hundred")
                {
                    value = 1;
                    pop();
                }
                else
                {
                    var tens = parse_tens(value);
                    if (tens != null)
                    {
                        value = tens.Value;
                    }
                    else
                    {
                        return null;
                    }
                }

                if (!enqueue())
                {
                    return value;
                }

                if (peek() == "hundred")
                {
                    value *= 100;
                    accept();
                    pop();

                    if (enqueue())
                    {
                        if (peek() == "and")
                        {
                            pop();
                        }

                        var tens = parse_tens(0);
                        if (tens != null)
                        {
                            value += tens.Value;
                        }
                    }
                }

                return value;
            }

            /// <summary>
            /// thousands : ("a" | hundreds)? "thousand" ("and"? hundreds)
            ///           | hundreds
            /// </summary>
            private int? parse_thousands(int value)
            {
                if (enqueue() && peek() == "thousand")
                {
                    value = 1;
                }
                else if (enqueue(2) && peek(0) == "a" && peek(1) == "thousand")
                {
                    value = 1;
                    pop();
                }
                else
                {
                    var hundreds = parse_hundreds(value);
                    if (hundreds != null)
                    {
                        value = hundreds.Value;
                    }
                    else
                    {
                        return null;
                    }
                }

                if (!enqueue())
                {
                    return value;
                }

                if (peek() == "thousand")
                {
                    value *= 1000;
                    accept();
                    pop();

                    if (enqueue())
                    {
                        if (peek() == "and")
                        {
                            pop();
                        }

                        var hundreds = parse_hundreds(0);
                        if (hundreds != null)
                        {
                            value += hundreds.Value;
                        }
                    }
                }

                return value;
            }
        }
    }
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace AutomotiveSkill.Common
{
    public class Parser
    {
        private static readonly Regex WORD = new Regex("[\\w']+", RegexOptions.Compiled);

        private static readonly IDictionary<string, int> Ones = new Dictionary<string, int>
            {
                { "zero",  0 },
                { "one",   1 },
                { "two",   2 },
                { "three", 3 },
                { "four",  4 },
                { "five",  5 },
                { "six",   6 },
                { "seven", 7 },
                { "eight", 8 },
                { "nine",  9 },
            };

        private static readonly IDictionary<string, int> Teens = new Dictionary<string, int>
            {
                { "ten",       10 },
                { "eleven",    11 },
                { "twelve",    12 },
                { "thirteen",  13 },
                { "fourteen",  14 },
                { "fifteen",   15 },
                { "sixteen",   16 },
                { "seventeen", 17 },
                { "eighteen",  18 },
                { "nineteen",  19 },
            };

        private static readonly IDictionary<string, int> Tens = new Dictionary<string, int>
            {
                { "twenty",  20 },
                { "thirty",  30 },
                { "forty",   40 },
                { "fourty",  40 },
                { "fifty",   50 },
                { "sixty",   60 },
                { "seventy", 70 },
                { "eighty",  80 },
                { "ninety",  90 },
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
        public int? Parse()
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

            return ParseThousands(0);
        }

        /// <summary>
        /// Position of end of match.
        /// </summary>
        /// <returns>The position of the end of the match.</returns>
        public int GetPosition()
        {
            return this.position;
        }

        private bool Enqueue(int size = 1)
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

        private string Peek(int i = 0)
        {
            return this.queue.ElementAt(i);
        }

        private void Pop()
        {
            this.queue.RemoveFirst();
            this.positions.RemoveFirst();
        }

        private void Accept()
        {
            this.position = this.positions.First();
        }

        /// <summary>
        /// ones : "zero" | "one" | "two" | ...
        /// </summary>
        private int? ParseOnes(int value)
        {
            if (!Enqueue())
            {
                return null;
            }

            var word = Peek();

            if (Ones.TryGetValue(word, out value))
            {
                Accept();
                Pop();
                return value;
            }

            return null;
        }

        /// <summary>
        /// tens : "ten" | "eleven" | ...
        ///      | ("twenty" | "thirty" | ...) ones?
        ///      | ones.
        /// </summary>
        private int? ParseTens(int value)
        {
            if (!Enqueue())
            {
                return null;
            }

            var word = Peek();

            if (Teens.TryGetValue(word, out var found))
            {
                value = found;
                Accept();
                Pop();
                return value;
            }

            if (Tens.TryGetValue(word, out found))
            {
                value = found;
                Accept();
                Pop();

                var ones = ParseOnes(0);
                if (ones != null)
                {
                    value += ones.Value;
                }

                return value;
            }

            return ParseOnes(value);
        }

        /// <summary>
        /// hundreds : ("a" | tens)? "hundred" ("and"? tens)?
        ///          | tens.
        /// </summary>
        private int? ParseHundreds(int value)
        {
            if (Enqueue() && Peek() == "hundred")
            {
                value = 1;
            }
            else if (Enqueue(2) && Peek(0) == "a" && Peek(1) == "hundred")
            {
                value = 1;
                Pop();
            }
            else
            {
                var tens = ParseTens(value);
                if (tens != null)
                {
                    value = tens.Value;
                }
                else
                {
                    return null;
                }
            }

            if (!Enqueue())
            {
                return value;
            }

            if (Peek() == "hundred")
            {
                value *= 100;
                Accept();
                Pop();

                if (Enqueue())
                {
                    if (Peek() == "and")
                    {
                        Pop();
                    }

                    var tens = ParseTens(0);
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
        ///           | hundreds.
        /// </summary>
        private int? ParseThousands(int value)
        {
            if (Enqueue() && Peek() == "thousand")
            {
                value = 1;
            }
            else if (Enqueue(2) && Peek(0) == "a" && Peek(1) == "thousand")
            {
                value = 1;
                Pop();
            }
            else
            {
                var hundreds = ParseHundreds(value);
                if (hundreds != null)
                {
                    value = hundreds.Value;
                }
                else
                {
                    return null;
                }
            }

            if (!Enqueue())
            {
                return value;
            }

            if (Peek() == "thousand")
            {
                value *= 1000;
                Accept();
                Pop();

                if (Enqueue())
                {
                    if (Peek() == "and")
                    {
                        Pop();
                    }

                    var hundreds = ParseHundreds(0);
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

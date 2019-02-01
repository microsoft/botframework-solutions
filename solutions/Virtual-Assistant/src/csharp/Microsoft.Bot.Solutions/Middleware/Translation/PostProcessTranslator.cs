using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;

namespace Microsoft.Bot.Solutions.Middleware.Translation
{
    /// <summary>
    /// PostProcessTranslator  is used to handle translation errors while translating numbers
    /// and to handle words that needs to be kept same as source language from provided template each line having a regex
    /// having first group matching the words that needs to be kept.
    /// </summary>
    internal class PostProcessTranslator
    {
        private readonly HashSet<string> _patterns;

        internal PostProcessTranslator(List<string> patterns)
            : this()
        {
            foreach (var pattern in patterns)
            {
                var processedLine = pattern.Trim();
                if (!pattern.Contains('('))
                {
                    processedLine = '(' + pattern + ')';
                }

                _patterns.Add(processedLine);
            }
        }

        internal PostProcessTranslator()
        {
            _patterns = new HashSet<string>();
        }

        /// <summary>
        /// Adds a no translate phrase to the pattern list.
        /// </summary>
        /// <param name="noTranslatePhrase">String containing no translate phrase.</param>
        public void AddNoTranslatePhrase(string noTranslatePhrase)
        {
            _patterns.Add("(" + noTranslatePhrase + ")");
        }

        /// <summary>
        /// Fixing translation
        /// used to handle numbers and no translate list.
        /// </summary>
        /// <param name="sourceMessage">Source Message.</param>
        /// <param name="alignment">String containing the Alignments.</param>
        /// <param name="targetMessage">Target Message.</param>
        /// <returns>Translated string.</returns>
        internal string FixTranslation(string sourceMessage, string alignment, string targetMessage)
        {
            var containsNum = Regex.IsMatch(sourceMessage, @"\d");

            if (_patterns.Count == 0 && !containsNum)
            {
                return targetMessage;
            }

            if (string.IsNullOrWhiteSpace(alignment))
            {
                return targetMessage;
            }

            var toBeReplaced = from result in _patterns
                               where Regex.IsMatch(sourceMessage, result, RegexOptions.Singleline | RegexOptions.IgnoreCase)
                               select result;
            var alignments = alignment.Trim().Split(' ');
            var srcWords = SplitSentence(sourceMessage, alignments);
            var trgWords = SplitSentence(targetMessage, alignments, false);
            var alignMap = WordAlignmentParse(alignments, srcWords, trgWords);
            if (toBeReplaced.Any())
            {
                foreach (var pattern in toBeReplaced)
                {
                    var matchNoTranslate = Regex.Match(sourceMessage, pattern, RegexOptions.Singleline | RegexOptions.IgnoreCase);
                    var noTranslateStartChrIndex = matchNoTranslate.Groups[1].Index;
                    var noTranslateMatchLength = matchNoTranslate.Groups[1].Length;
                    var wrdIndx = 0;
                    var chrIndx = 0;
                    var newChrLengthFromMatch = 0;
                    var srcIndex = -1;
                    var newNoTranslateArrayLength = 1;
                    foreach (var wrd in srcWords)
                    {
                        chrIndx += wrd.Length + 1;
                        wrdIndx++;
                        if (chrIndx == noTranslateStartChrIndex)
                        {
                            srcIndex = wrdIndx;
                        }

                        if (srcIndex != -1)
                        {
                            if (newChrLengthFromMatch + srcWords[wrdIndx].Length >= noTranslateMatchLength)
                            {
                                break;
                            }

                            newNoTranslateArrayLength += 1;
                            newChrLengthFromMatch += srcWords[wrdIndx].Length + 1;
                        }
                    }

                    if (srcIndex == -1)
                    {
                        continue;
                    }

                    var wrdNoTranslate = new string[newNoTranslateArrayLength];
                    Array.Copy(srcWords, srcIndex, wrdNoTranslate, 0, newNoTranslateArrayLength);
                    foreach (var srcWrd in wrdNoTranslate)
                    {
                        trgWords = KeepSrcWrdInTranslation(alignMap, srcWords, trgWords, srcIndex);
                        srcIndex++;
                    }
                }
            }

            var numericMatches = Regex.Matches(sourceMessage, @"\d+", RegexOptions.Singleline);
            foreach (Match numericMatch in numericMatches)
            {
                var srcIndex = Array.FindIndex(srcWords, row => row == numericMatch.Groups[0].Value);
                trgWords = KeepSrcWrdInTranslation(alignMap, srcWords, trgWords, srcIndex);
            }

            return Join(" ", trgWords);
        }

        /// <summary>
        /// Helper to Join words to sentence.
        /// </summary>
        /// <param name="delimiter">String delimiter used  to join words.</param>
        /// <param name="words">String Array of words to be joined.</param>
        /// <returns>string joined sentence.</returns>
        private string Join(string delimiter, string[] words)
        {
            var sentence = string.Join(delimiter, words);
            sentence = Regex.Replace(sentence, "[ ]?'[ ]?", "'");
            return sentence.Trim();
        }

        /// <summary>
        /// Helper to split sentence to words.
        /// </summary>
        /// <param name="sentence">String containing sentence to be splitted.</param>
        /// <returns>string array of words.</returns>
        private string[] SplitSentence(string sentence, string[] alignments = null, bool isSrcSentence = true)
        {
            var wrds = sentence.Split(' ');
            var alignSplitWrds = new string[0];
            if (alignments != null && alignments.Length > 0)
            {
                var outWrds = new List<string>();
                var wrdIndxInAlignment = 1;

                if (isSrcSentence)
                {
                    wrdIndxInAlignment = 0;
                }
                else
                {
                    // reorder alignments in case of target translated  message to get ordered output words.
                    Array.Sort(alignments, (x, y) => int.Parse(x.Split('-')[wrdIndxInAlignment].Split(':')[0]).CompareTo(int.Parse(y.Split('-')[wrdIndxInAlignment].Split(':')[0])));
                }

                var withoutSpaceSentence = sentence.Replace(" ", string.Empty);

                foreach (var alignData in alignments)
                {
                    alignSplitWrds = outWrds.ToArray();
                    var wordIndexes = alignData.Split('-')[wrdIndxInAlignment];
                    var startIndex = int.Parse(wordIndexes.Split(':')[0]);
                    var length = int.Parse(wordIndexes.Split(':')[1]) - startIndex + 1;
                    var wrd = sentence.Substring(startIndex, length);
                    var newWrds = new string[outWrds.Count + 1];
                    if (newWrds.Length > 1)
                    {
                        alignSplitWrds.CopyTo(newWrds, 0);
                    }

                    newWrds[outWrds.Count] = wrd;
                    var subSentence = Join(string.Empty, newWrds.ToArray());
                    if (withoutSpaceSentence.Contains(subSentence))
                    {
                        outWrds.Add(wrd);
                    }
                }

                alignSplitWrds = outWrds.ToArray();
            }

            var punctuationChars = new char[] { '.', ',', '?', '!' };
            if (Join(string.Empty, alignSplitWrds).TrimEnd(punctuationChars) == Join(string.Empty, wrds).TrimEnd(punctuationChars))
            {
                return alignSplitWrds;
            }

            return wrds;
        }

        private Dictionary<int, int> WordAlignmentParse(string[] alignments, string[] srcWords, string[] trgWords)
        {
            var alignMap = new Dictionary<int, int>();
            var sourceMessage = Join(" ", srcWords);
            var trgMessage = Join(" ", trgWords);
            foreach (var alignData in alignments)
            {
                var wordIndexes = alignData.Split('-');
                var srcStartIndex = int.Parse(wordIndexes[0].Split(':')[0]);
                var srcLength = int.Parse(wordIndexes[0].Split(':')[1]) - srcStartIndex + 1;
                if ((srcLength + srcStartIndex) > sourceMessage.Length)
                {
                    continue;
                }

                var srcWrd = sourceMessage.Substring(srcStartIndex, srcLength);
                var sourceWordIndex = Array.FindIndex(srcWords, row => row == srcWrd);

                var trgstartIndex = int.Parse(wordIndexes[1].Split(':')[0]);
                var trgLength = int.Parse(wordIndexes[1].Split(':')[1]) - trgstartIndex + 1;
                if ((trgLength + trgstartIndex) > trgMessage.Length)
                {
                    continue;
                }

                var trgWrd = trgMessage.Substring(trgstartIndex, trgLength);
                var targetWordIndex = Array.FindIndex(trgWords, row => row == trgWrd);

                if (sourceWordIndex >= 0 && targetWordIndex >= 0)
                {
                    alignMap[sourceWordIndex] = targetWordIndex;
                }
            }

            return alignMap;
        }

        private string[] KeepSrcWrdInTranslation(Dictionary<int, int> alignment, string[] sourceWords, string[] targetWords, int srcWrdIndx)
        {
            if (alignment.ContainsKey(srcWrdIndx))
            {
                targetWords[alignment[srcWrdIndx]] = sourceWords[srcWrdIndx];
            }

            return targetWords;
        }
    }
}

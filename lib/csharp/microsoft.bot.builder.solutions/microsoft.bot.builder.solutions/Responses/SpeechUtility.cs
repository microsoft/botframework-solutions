using System;
using System.Collections.Generic;
using AdaptiveCards;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Extensions;
using Microsoft.Bot.Builder.Solutions.Resources;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Builder.Solutions.Responses
{
    /// <summary>
    /// Read order of list items.
    /// </summary>
    public enum ReadPreference
    {
        /// <summary>First item, second item, third item, etc.</summary>
        Enumeration,

        /// <summary>Latest item, second item, third item, etc.</summary>
        Chronological
    }

    public class SpeechUtility
    {
        /// <summary>
        /// Concatenate PromptOption string properties into a formatted speech-ready string.
        /// </summary>
        /// <param name="selectOption">Prompt options.</param>
        /// <param name="readOrder">Read order of list items.</param>
        /// <param name="maxSize">The max read size, default is 4.</param>
        /// <returns>Formatted speech-ready string.</returns>
        public static string ListToSpeechReadyString(PromptOptions selectOption, ReadPreference readOrder = ReadPreference.Enumeration,  int maxSize = 4)
        {
            List<string> selectOptionSpeakStrings = new List<string>();

            for (int i = 0; i < selectOption.Choices.Count; ++i)
            {
                selectOptionSpeakStrings.Add(selectOption.Choices[i].Value);
            }

            return ListToSpeechReadyString(selectOption.Prompt.Text, selectOptionSpeakStrings, readOrder, maxSize);
        }

        /// <summary>
        /// Concatenate Activity string properties into a formatted speech-ready string.
        /// </summary>
        /// <param name="activityToProcess">Activity.</param>
        /// <param name="readOrder">Read order of list items.</param>
        /// <param name="maxSize">The max read size, default is 4.</param>
        /// <returns>Formatted speech-ready string.</returns>
        public static string ListToSpeechReadyString(Activity activityToProcess, ReadPreference readOrder = ReadPreference.Enumeration, int maxSize = 4)
        {
            List<string> selectOptionSpeakStrings = new List<string>();

            for (int i = 0; i < activityToProcess.Attachments.Count; ++i)
            {
                // Add try parse - if already an adaptive card good to go, if unknown object convert from string
                var cardContent = AdaptiveCard.FromJson(activityToProcess.Attachments[i].Content.ToString());
                if (!string.IsNullOrEmpty(cardContent?.Card?.Speak))
                {
                    selectOptionSpeakStrings.Add(cardContent.Card.Speak);
                }
            }

            return ListToSpeechReadyString(activityToProcess.Speak, selectOptionSpeakStrings, readOrder, maxSize);
        }

        /// <summary>
        /// Concatenate strings into a formatted speech-ready string.
        /// </summary>
        /// <param name="parentString">Itroduction string.</param>
        /// <param name="selectionStrings">List item strings.</param>
        /// <param name="readOrder">Read order of list items.</param>
        /// <param name="maxSize">The max read size.</param>
        /// <returns>Formatted speech-ready string.</returns>
        private static string ListToSpeechReadyString(string parentString, List<string> selectionStrings, ReadPreference readOrder, int maxSize)
        {
            var result = string.Empty;
            if (!string.IsNullOrEmpty(parentString))
            {
                result += parentString + "<break/>";
            }

            List<string> itemDetails = new List<string>();

            int readSize = Math.Min(selectionStrings.Count, maxSize);
            if (readSize == 1)
            {
                itemDetails.Add(selectionStrings[0]);
            }
            else
            {
                for (var i = 0; i < readSize; i++)
                {
                    var readFormat = string.Empty;

                    if (i == 0)
                    {
                        if (readOrder.Equals(ReadPreference.Chronological))
                        {
                            readFormat = CommonStrings.LatestItem;
                        }
                        else
                        {
                            readFormat = CommonStrings.FirstItem;
                        }
                    } else
                    {
                        if (i == readSize - 1)
                        {
                            readFormat = CommonStrings.LastItem;
                        }
                        else
                        {
                            if (i == 1)
                            {
                                readFormat = CommonStrings.SecondItem;
                            }
                            else if (i == 2)
                            {
                                readFormat = CommonStrings.ThirdItem;
                            }
                            else if (i == 3)
                            {
                                readFormat = CommonStrings.FourthItem;
                            }
                        }
                    }

                    var selectionDetail = string.Format(readFormat, selectionStrings[i]);
                    itemDetails.Add(selectionDetail);
                }
            }

            result += itemDetails.ToSpeechString(CommonStrings.And);
            return result;
        }
    }
}

// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Solutions.Responses
{
    using System;
    using System.Collections.Generic;
    using AdaptiveCards;
    using Microsoft.Bot.Builder.Dialogs;
    using Microsoft.Bot.Schema;
    using Microsoft.Bot.Solutions.Extensions;
    using Microsoft.Bot.Solutions.Resources;

    /// <summary>
    /// Read order of list items.
    /// </summary>
    public enum ReadPreference
    {
        /// <summary>First item, second item, third item, etc.</summary>
        Enumeration,

        /// <summary>Latest item, second item, third item, etc.</summary>
        Chronological,
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
        public static string ListToSpeechReadyString(PromptOptions selectOption, ReadPreference readOrder = ReadPreference.Enumeration, int maxSize = 4)
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
                // Card attachments may be formatted as card or generic objects
                if (activityToProcess.Attachments[i].Content is AdaptiveCard)
                {
                    dynamic cardContent = activityToProcess.Attachments[i].Content;
                    if (!string.IsNullOrEmpty(cardContent?.Speak))
                    {
                        selectOptionSpeakStrings.Add(cardContent.Speak);
                    }
                }
                else
                {
                    dynamic cardContent = activityToProcess.Attachments[i].Content;
                    if (cardContent?.speak != null)
                    {
                        selectOptionSpeakStrings.Add(cardContent.speak.ToString());
                    }
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
            var result = $"{parentString} " ?? string.Empty;

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
                    }
                    else
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
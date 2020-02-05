// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Recognizers.Text.Choice;

namespace Microsoft.Bot.Solutions.Util
{
    public static class ConfirmRecognizerHelper
    {
        public static PromptRecognizerResult<bool> ConfirmYesOrNo(string utterance, string locale = null)
        {
            var result = new PromptRecognizerResult<bool>();
            if (!string.IsNullOrEmpty(utterance))
            {
                // Recognize utterance
                var results = ChoiceRecognizer.RecognizeBoolean(utterance, locale);
                if (results.Count > 0)
                {
                    var first = results[0];
                    if (bool.TryParse(first.Resolution["value"].ToString(), out var value))
                    {
                        result.Succeeded = true;
                        result.Value = value;
                    }
                }
            }

            return result;
        }
    }
}

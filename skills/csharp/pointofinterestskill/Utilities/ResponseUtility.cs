using System.Collections.Generic;
using Microsoft.Bot.Schema;

namespace PointOfInterestSkill.Utilities
{
    public class ResponseUtility
    {
        /// <summary>
        /// Build a consolidated Speak response to prevent the client from having to crack each card open and build itself.
        /// </summary>
        /// <param name="activityToProcess">The Activity containing multiple cards.</param>
        /// <returns>A speech friendly consolidated response.</returns>
        public static string BuildSpeechFriendlyPoIResponse(Activity activityToProcess)
        {
            var speakStrings = new List<string>();
            if (!string.IsNullOrEmpty(activityToProcess.Speak))
            {
                speakStrings.Add(activityToProcess.Speak);
            }

            for (var i = 0; i < activityToProcess.Attachments.Count; ++i)
            {
                dynamic generatedCard = activityToProcess.Attachments[i].Content;
                if (generatedCard != null && generatedCard.speak != null)
                {
                    // The dash makes the voice take a short break, which is what a human would do when reading out a numbered list.
                    speakStrings.Add($"{(i + 1).ToString()} - {generatedCard.speak}.");
                }
            }

            return string.Join(',', speakStrings);
        }
    }
}

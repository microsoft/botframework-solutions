using System.Collections.Generic;
using Microsoft.Bot.Schema;

namespace PointOfInterestSkill.Dialogs.Shared
{
    public class SpeakHelper
    {
        /// <summary>
        /// Build a consolidated Speak response to prevent the client from having to crack each card open and build itself.
        /// </summary>
        /// <param name="activityToProcess">The Activity containing multiple cards.</param>
        /// <returns>A speech friendly consolidated response.</returns>
        public static string BuildSpeechFriendlyPoIResponse(Activity activityToProcess)
        {
            List<string> speakStrings = new List<string>();
            if (!string.IsNullOrEmpty(activityToProcess.Speak))
            {
                speakStrings.Add(activityToProcess.Speak);
            }

            foreach (Attachment attachment in activityToProcess.Attachments)
            {
                dynamic generatedCard = attachment.Content;
                if (generatedCard != null && generatedCard.speak != null)
                {
                    speakStrings.Add((string)generatedCard.speak);
                }
            }

            return string.Join(',', speakStrings);
        }
    }
}

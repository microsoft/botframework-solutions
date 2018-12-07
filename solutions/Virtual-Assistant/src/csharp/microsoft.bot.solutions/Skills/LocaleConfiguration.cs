using System.Collections.Generic;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.QnA;

namespace Microsoft.Bot.Solutions.Skills
{
    /// <summary>
    /// A set of service clients for a specific locale including Dispatch, LUIS, and QnA Maker models.
    /// </summary>
    public class LocaleConfiguration
    {
        public string Locale { get; set; }

        /// <summary>
        /// Gets or sets the Dispatch LUIS Recognizer used.
        /// </summary>
        /// <remarks>The Dispatch LUIS Recognizer should not be modified while the bot is running.</remarks>
        /// <value>
        /// A <see cref="LuisRecognizer"/> client instance created based on configuration in the .bot file.
        /// </value>
        public TelemetryLuisRecognizer DispatchRecognizer { get; set; }

        /// <summary>
        /// Gets or sets the LUIS Services used.
        /// Given there can be multiple <see cref="TelemetryLuisRecognizer"/> services used in a single bot,
        /// LuisServices is represented as a dictionary.  This is also modeled in the
        /// ".bot" file since the elements are named.
        /// </summary>
        /// <remarks>The LUIS services collection should not be modified while the bot is running.</remarks>
        /// <value>
        /// A <see cref="LuisRecognizer"/> client instance created based on configuration in the .bot file.
        /// </value>
        public Dictionary<string, IRecognizer> LuisServices { get; set; } = new Dictionary<string, IRecognizer>();

        /// <summary>
        /// Gets or sets the QnAMaker Services used.
        /// Given there can be multiple <see cref="TelemetryQnAMaker"/> services used in a single bot,
        /// QnAServices is represented as a dictionary.  This is also modeled in the
        /// ".bot" file since the elements are named.
        /// </summary>
        /// <remarks>The QnAMaker services collection should not be modified while the bot is running.</remarks>
        /// <value>
        /// A <see cref="TelemetryQnAMaker"/> client instance created based on configuration in the .bot file.
        /// </value>
        public Dictionary<string, QnAMaker> QnAServices { get; set; } = new Dictionary<string, QnAMaker>();
    }
}

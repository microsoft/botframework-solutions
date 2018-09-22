// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace EmailSkill
{
    using Microsoft.Bot.Solutions.Cards;

    /// <summary>
    /// Data used when create an email card.
    /// </summary>
    public class EmailWithOutButtonCardData : CardDataBase
    {
        /// <summary>
        /// Gets or sets subject.
        /// </summary>
        /// <value>
        /// Subject.
        /// </value>
        public string Subject { get; set; }

        /// <summary>
        /// Gets or sets sender.
        /// </summary>
        /// <value>
        /// Sender.
        /// </value>
        public string Sender { get; set; }

        /// <summary>
        /// Gets or sets NameList.
        /// </summary>
        /// <value>
        /// NameList.
        /// </value>
        public string NameList { get; set; }

        /// <summary>
        /// Gets or sets ReceivedDateTime.
        /// </summary>
        /// <value>
        /// ReceivedDateTime.
        /// </value>
        public string ReceivedDateTime { get; set; }

        /// <summary>
        /// Gets or sets EmailContent.
        /// </summary>
        /// <value>
        /// EmailContent.
        /// </value>
        public string EmailContent { get; set; }

        /// <summary>
        /// Gets or sets EmailLink.
        /// </summary>
        /// <value>
        /// EmailLink.
        /// </value>
        public string EmailLink { get; set; }

        /// <summary>
        /// Gets or sets speak text when card show.
        /// </summary>
        /// <value>
        /// Speak text when card show.
        /// </value>
        public string Speak { get; set; }
    }
}

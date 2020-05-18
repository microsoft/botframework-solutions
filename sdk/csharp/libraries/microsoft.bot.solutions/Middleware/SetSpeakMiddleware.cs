// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace Microsoft.Bot.Solutions.Middleware
{
    using System;
    using System.Collections.Generic;
    using System.Globalization;
    using System.Threading;
    using System.Threading.Tasks;
    using System.Xml;
    using System.Xml.Linq;
    using Microsoft.Bot.Builder;
    using Microsoft.Bot.Schema;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// Set Speech Synthesis Markup Language (SSML) on an Activity's Speak property with locale and voice input.
    /// </summary>
    public class SetSpeakMiddleware : IMiddleware
    {
        private const string DefaultLocale = "en-US";

        private static readonly IDictionary<string, string> DefaultVoiceFonts = new Dictionary<string, string>()
        {
            { "de-DE", "Microsoft Server Speech Text to Speech Voice (de-DE, Hedda)" },
            { "en-US", "Microsoft Server Speech Text to Speech Voice (en-US, Jessa24kRUS)" },
            { "es-ES", "Microsoft Server Speech Text to Speech Voice (es-ES, Laura, Apollo)" },
            { "fr-FR", "Microsoft Server Speech Text to Speech Voice (fr-FR, Julie, Apollo)" },
            { "it-IT", "Microsoft Server Speech Text to Speech Voice (it-IT, Cosimo, Apollo)" },
            { "zh-CN", "Microsoft Server Speech Text to Speech Voice (zh-CN, HuihuiRUS)" },
        };

        private static readonly ISet<string> DefaultChannels = new HashSet<string>()
        {
            Connector.Channels.DirectlineSpeech,
            Connector.Channels.Emulator,
        };

        private static string _locale;

        private static IDictionary<string, string> _voiceFonts;

        private static ISet<string> _channels;

        private static readonly XNamespace NamespaceURI = @"https://www.w3.org/2001/10/synthesis";

        /// <summary>
        /// Initializes a new instance of the <see cref="SetSpeakMiddleware"/> class.
        /// </summary>
        /// <param name="locale">If null, use en-US.</param>
        /// <param name="voiceFonts">Map voice font for locale like en-US to "Microsoft Server Speech Text to Speech Voice (en-US, Jessa24kRUS)".</param>
        /// <param name="channels">Set SSML for these channels. If null, use <see cref="Connector.Channels.DirectlineSpeech"/> and <see cref="Connector.Channels.Emulator"/>.</param>
        public SetSpeakMiddleware(string locale = null, IDictionary<string, string> voiceFonts = null, ISet<string> channels = null)
        {
            _locale = locale ?? DefaultLocale;
            _voiceFonts = voiceFonts ?? DefaultVoiceFonts;
            _channels = channels ?? DefaultChannels;
        }

        /// <summary>
        /// If outgoing Activities are messages and using one of the desired channels, decorate the Speak property with an SSML formatted string.
        /// </summary>
        /// <param name="context">The Bot Context object.</param>
        /// <param name="next">The next middleware component to run.</param>
        /// <param name="cancellationToken">The cancellation token for the task.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public Task OnTurnAsync(ITurnContext context, NextDelegate next, CancellationToken cancellationToken = default(CancellationToken))
        {
            context.OnSendActivities(async (ctx, activities, nextSend) =>
            {
                foreach (var activity in activities)
                {
                    switch (activity.Type)
                    {
                        case ActivityTypes.Message:
                            activity.Speak = GetActivitySpeakText(activity);

                            if (_channels.Contains(activity.ChannelId))
                            {
                                activity.Speak = DecorateSSML(activity);
                            }

                            break;
                    }
                }

                return await nextSend().ConfigureAwait(false);
            });

            return next(cancellationToken);
        }

        /// <summary>
        /// Gets the speak text for the activity.
        /// </summary>
        /// <param name="activity">Outgoing bot Activity.</param>
        /// <returns>speech text string value.</returns>
        private static string GetActivitySpeakText(Activity activity)
        {
            // return speak or text value if they already exist in the activity
            var result = activity.Speak ?? activity.Text;
            if (result != null)
            {
                return result;
            }

            // return speak value of first attachment if an attachment exists and has a speak value
            if (activity.Attachments.Count > 0)
            {
                var attachmentContent = activity.Attachments[0].Content;
                if (attachmentContent != null)
                {
                    var contentObj = attachmentContent as JObject;
                    return contentObj?["speak"]?.ToString();
                }
            }

            return null;
        }

            /// <summary>
            /// Formats an existing string to be formatted for Speech Synthesis Markup Language with a voice font.
            /// </summary>
            /// <param name="activity">Outgoing bot Activity.</param>
            /// <returns>SSML-formatted string to be used with synthetic speech.</returns>
            private static string DecorateSSML(Activity activity)
        {
            if (string.IsNullOrWhiteSpace(activity.Speak))
            {
                return activity.Speak?.Trim();
            }

            XElement rootElement = null;
            try
            {
                rootElement = XElement.Parse(activity.Speak);
            }
            catch (XmlException)
            {
                // Ignore any exceptions. This is effectively a "TryParse", except that XElement doesn't
                // have a TryParse method.
            }

            if (rootElement == null || rootElement.Name.LocalName != "speak")
            {
                // If the text is not valid XML, or if it's not a <speak> node, treat it as plain text.
                rootElement = new XElement(NamespaceURI + "speak", activity.Speak);
            }

            var locale = _locale;
            if (!string.IsNullOrEmpty(activity.Locale))
            {
                try
                {
                    var normalizedLocale = new CultureInfo(activity.Locale).Name;
                    if (_voiceFonts.ContainsKey(normalizedLocale))
                    {
                        locale = normalizedLocale;
                    }
                }
                catch (CultureNotFoundException)
                {
                }
            }

            AddAttributeIfMissing(rootElement, "version", "1.0");
            AddAttributeIfMissing(rootElement, XNamespace.Xml + "lang", locale);
            AddAttributeIfMissing(rootElement, XNamespace.Xmlns + "mstts", "https://www.w3.org/2001/mstts");

            var sayAsElements = rootElement.Elements("say-as");
            foreach (var element in sayAsElements)
            {
                UpdateAttributeIfPresent(element, "interpret-as", "digits", "number_digit");
            }

            // add voice element if absent
            AddVoiceElementIfMissing(rootElement, _voiceFonts[locale]);

            return rootElement.ToString(SaveOptions.DisableFormatting);
        }

        /// <summary>
        /// Add a new attribute to an XML element.
        /// </summary>
        /// <param name="element">The XML element to update.</param>
        /// <param name="attributeName">The XML attribute name to add.</param>
        /// <param name="attributeValue">The XML attribute value to add.</param>
        private static void AddAttributeIfMissing(XElement element, XName attributeName, string attributeValue)
        {
            var existingAttribute = element.Attribute(attributeName);
            if (existingAttribute == null)
            {
                element.Add(new XAttribute(attributeName, attributeValue));
            }
        }

        /// <summary>
        /// Add a new attribute with a voice property to the parent XML element.
        /// </summary>
        /// <param name="parentElement">The XML element to update.</param>
        /// <param name="attributeValue">The XML attribute to add.</param>
        private static void AddVoiceElementIfMissing(XElement parentElement, string attributeValue)
        {
            try
            {
                var existingVoiceElement = parentElement.Element("voice") ?? parentElement.Element(NamespaceURI + "voice");

                // If an existing voice element is null (absent), then add it. Otherwise, assume the author has set it correctly.
                if (existingVoiceElement == null)
                {
                    var existingNodes = parentElement.DescendantNodes();

                    XElement voiceElement = new XElement("voice", new XAttribute("name", attributeValue));
                    voiceElement.Add(existingNodes);
                    parentElement.RemoveNodes();
                    parentElement.Add(voiceElement);
                }
                else
                {
                    existingVoiceElement.SetAttributeValue("name", attributeValue);
                }
            }
            catch (Exception ex)
            {
                throw new Exception($"Could not add voice element to speak property {ex.Message}");
            }
        }

        /// <summary>
        /// Update an XML attribute if it already exists.
        /// </summary>
        /// <param name="element">The XML element to update.</param>
        /// <param name="attributeName">The XML attribute name to update.</param>
        /// <param name="currentAttributeValue">The current XML attribute's value.</param>
        /// <param name="newAttributeValue">The new XML attribute's value.</param>
        private static void UpdateAttributeIfPresent(XElement element, XName attributeName, string currentAttributeValue, string newAttributeValue)
        {
            var existingAttribute = element?.Attribute(attributeName);
            if (existingAttribute?.Value == currentAttributeValue)
            {
                existingAttribute?.SetValue(newAttributeValue);
            }
        }
    }
}
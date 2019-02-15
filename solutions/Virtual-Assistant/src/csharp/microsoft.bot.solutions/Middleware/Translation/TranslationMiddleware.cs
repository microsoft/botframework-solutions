// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Schema;

namespace Microsoft.Bot.Solutions.Middleware.Translation
{
    /// <summary>
    /// Middleware for translating text between the user and bot.
    /// Uses the Microsoft Translator Text API.
    /// </summary>
    public class TranslationMiddleware : IMiddleware
    {
        private readonly string[] _nativeLanguages;
        private readonly Translator _translator;
        private readonly Dictionary<string, List<string>> _patterns;
        private readonly Func<ITurnContext, string> _getUserLanguage;
        private readonly Func<ITurnContext, Task<bool>> _isUserLanguageChanged;
        private readonly bool _toUserLanguage;

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationMiddleware"/> class.
        /// </summary>
        /// <param name="nativeLanguages">The languages supported by your app.</param>
        /// <param name="translatorKey">Your subscription key for the Microsoft Translator Text API.</param>
        /// <param name="toUserLanguage">Indicates whether to transalte messages sent from the bot into the user's language.</param>
        public TranslationMiddleware(string[] nativeLanguages, string translatorKey, bool toUserLanguage = false)
        {
            AssertValidNativeLanguages(nativeLanguages);
            this._nativeLanguages = nativeLanguages;
            if (string.IsNullOrEmpty(translatorKey))
            {
                throw new ArgumentNullException(nameof(translatorKey));
            }

            this._translator = new Translator(translatorKey);
            _patterns = new Dictionary<string, List<string>>();
            _toUserLanguage = toUserLanguage;
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationMiddleware"/> class.
        /// </summary>
        /// <param name="nativeLanguages">The languages supported by your app.</param>
        /// <param name="translatorKey">Your subscription key for the Microsoft Translator Text API.</param>
        /// <param name="patterns">List of regex patterns, indexed by language identifier,
        /// that can be used to flag text that should not be translated.</param>
        /// <param name="toUserLanguage">Indicates whether to transalte messages sent from the bot into the user's language.</param>
        /// <remarks>Each pattern the <paramref name="patterns"/> describes an entity that should not be translated.
        /// For example, in French <c>je m’appelle ([a-z]+)</c>, which will avoid translation of anything coming after je m’appelle.</remarks>
        public TranslationMiddleware(string[] nativeLanguages, string translatorKey, Dictionary<string, List<string>> patterns, bool toUserLanguage = false)
            : this(nativeLanguages, translatorKey, toUserLanguage)
        {
            this._patterns = patterns ?? throw new ArgumentNullException(nameof(patterns));
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TranslationMiddleware"/> class.
        /// </summary>
        /// <param name="nativeLanguages">List of languages supported by your app.</param>
        /// <param name="translatorKey">Your subscription key for the Microsoft Translator Text API.</param>
        /// <param name="patterns">List of regex patterns, indexed by language identifier,
        /// that can be used to flag text that should not be translated.</param>
        /// <param name="getUserLanguage">A delegate for getting the user language,
        /// to use in place of the Detect method of the Microsoft Translator Text API.</param>
        /// <param name="isUserLanguageChanged">A delegate for checking whether the user requested to change their language.</param>
        /// <param name="toUserLanguage">Indicates whether to transalte messages sent from the bot into the user's language.</param>
        /// <remarks>Each pattern the <paramref name="patterns"/> describes an entity that should not be translated.
        /// For example, in French <c>je m’appelle ([a-z]+)</c>, which will avoid translation of anything coming after je m’appelle.</remarks>
        public TranslationMiddleware(string[] nativeLanguages, string translatorKey, Dictionary<string, List<string>> patterns, Func<ITurnContext, string> getUserLanguage, Func<ITurnContext, Task<bool>> isUserLanguageChanged, bool toUserLanguage = false)
            : this(nativeLanguages, translatorKey, patterns, toUserLanguage)
        {
            this._getUserLanguage = getUserLanguage ?? throw new ArgumentNullException(nameof(getUserLanguage));
            this._isUserLanguageChanged = isUserLanguageChanged ?? throw new ArgumentNullException(nameof(isUserLanguageChanged));
        }

        /// <summary>
        /// Processess an incoming activity.
        /// </summary>
        /// <param name="context">The context object for this turn.</param>
        /// <param name="nextTurn">The delegate to call to continue the bot middleware pipeline.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task OnTurnAsync(ITurnContext context, NextDelegate nextTurn, CancellationToken cancellationToken)
        {
            if (context.Activity.Type == ActivityTypes.Message)
            {
                var message = context.Activity.AsMessageActivity();
                if (message != null)
                {
                    if (!string.IsNullOrWhiteSpace(message.Text))
                    {
                        var languageChanged = false;

                        if (_isUserLanguageChanged != null)
                        {
                            languageChanged = await _isUserLanguageChanged(context);
                        }

                        if (!languageChanged)
                        {
                            // determine the language we are using for this conversation
                            var sourceLanguage = string.Empty;
                            var targetLanguage = string.Empty;
                            if (_getUserLanguage == null)
                            {
                                // awaiting user language detection using Microsoft Translator API.
                                sourceLanguage = await _translator.Detect(message.Text);
                            }
                            else
                            {
                                sourceLanguage = _getUserLanguage(context);
                            }

                            targetLanguage = _nativeLanguages.Contains(sourceLanguage) ? sourceLanguage : this._nativeLanguages.FirstOrDefault() ?? "en";
                            await TranslateMessageAsync(context, message, sourceLanguage, targetLanguage, _nativeLanguages.Contains(sourceLanguage)).ConfigureAwait(false);

                            if (_toUserLanguage)
                            {
                                context.OnSendActivities(async (newContext, activities, nextSend) =>
                                {
                                    // Translate messages sent to the user to user language
                                    var tasks = new List<Task>();
                                    foreach (var currentActivity in activities.Where(a => a.Type == ActivityTypes.Message))
                                    {
                                        tasks.Add(TranslateMessageAsync(newContext, currentActivity.AsMessageActivity(), targetLanguage, sourceLanguage, false));
                                    }

                                    if (tasks.Any())
                                    {
                                        await Task.WhenAll(tasks).ConfigureAwait(false);
                                    }

                                    return await nextSend();
                                });

                                context.OnUpdateActivity(async (newContext, activity, nextUpdate) =>
                                {
                                    // Translate messages sent to the user to user language
                                    if (activity.Type == ActivityTypes.Message)
                                    {
                                        await TranslateMessageAsync(newContext, activity.AsMessageActivity(), targetLanguage, sourceLanguage, false).ConfigureAwait(false);
                                    }

                                    return await nextUpdate();
                                });
                            }
                        }
                        else
                        {
                            // skip routing in case of user changed the language
                            return;
                        }
                    }
                }
            }

            if (nextTurn != null)
            {
                await nextTurn(cancellationToken).ConfigureAwait(false);
            }
        }

        private static void AssertValidNativeLanguages(string[] nativeLanguages)
        {
            if (nativeLanguages == null)
            {
                throw new ArgumentNullException(nameof(nativeLanguages));
            }
        }

        private async Task TranslateMessageAsync(ITurnContext context, IMessageActivity message, string sourceLanguage, string targetLanguage, bool inNativeLanguages)
        {
            if (!inNativeLanguages && sourceLanguage != targetLanguage)
            {
                // if we have text and a target language
                if (!string.IsNullOrWhiteSpace(message.Text) && !string.IsNullOrEmpty(targetLanguage))
                {
                    if (targetLanguage == sourceLanguage)
                    {
                        return;
                    }

                    // check if the developer has added pattern list for the input source language
                    if (_patterns.ContainsKey(sourceLanguage) && _patterns[sourceLanguage].Count > 0)
                    {
                        // if we have a list of patterns for the current user's language send it to the translator post processor.
                        _translator.SetPostProcessorTemplate(_patterns[sourceLanguage]);
                    }

                    var text = message.Text;
                    var lines = text.Split(new string[] { "\r\n", "\n" }, StringSplitOptions.None);
                    var translateResult = await this._translator.TranslateArray(lines, sourceLanguage, targetLanguage).ConfigureAwait(false);
                    text = string.Join("\n", translateResult);
                    message.Text = text;
                }
            }
        }
    }
}

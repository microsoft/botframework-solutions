﻿// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using static Luis.EmailLuis;

namespace EmailSkill.Tests.Flow.Utterances
{
    public class BaseTestUtterances : Dictionary<string, EmailLuis>
    {
        public BaseTestUtterances()
        {
            this.Add(FirstOne, CreateIntent(FirstOne, Intent.None, ordinal: new double[1] { 1 }));
            this.Add(SecondOne, CreateIntent(SecondOne, Intent.None, ordinal: new double[1] { 2 }));
            this.Add(ThirdOne, CreateIntent(ThirdOne, Intent.None, ordinal: new double[1] { 3 }));

            this.Add(NumberOne, CreateIntent(NumberOne, Intent.None, ordinal: new double[1] { 1 }));
            this.Add(NumberTwo, CreateIntent(NumberTwo, Intent.None, ordinal: new double[1] { 2 }));
        }

        public static double TopIntentScore { get; } = 0.9;

        public static string FirstOne { get; } = "The first one";

        public static string SecondOne { get; } = "The second one";

        public static string ThirdOne { get; } = "The third one";

        public static string NumberOne { get; } = "1";

        public static string NumberTwo { get; } = "2";

        public void AddManager(BaseTestUtterances utterances)
        {
            foreach (var item in utterances)
            {
                if (!this.ContainsKey(item.Key))
                {
                    this.Add(item.Key, item.Value);
                }
            }
        }

        public EmailLuis GetBaseNoneIntent()
        {
            var emailIntent = new EmailLuis
            {
                Intents = new Dictionary<Intent, IntentScore>()
            };
            emailIntent.Intents.Add(Intent.None, new IntentScore() { Score = TopIntentScore });

            return emailIntent;
        }

        protected EmailLuis CreateIntent(
            string userInput,
            Intent intent = Intent.None,
            double[] ordinal = null,
            string[] contactName = null,
            string[] senderName = null,
            string[] emailAdress = null,
            string[] subject = null,
            string[] message = null)
        {
            var emailIntent = new EmailLuis
            {
                Text = userInput,

                Intents = new Dictionary<Intent, IntentScore>()
            };
            emailIntent.Intents.Add(intent, new IntentScore() { Score = TopIntentScore });

            emailIntent.Entities = new _Entities
            {
                _instance = new _Entities._Instance(),

                ordinal = ordinal,
                ContactName = contactName,
                SenderName = senderName,
                EmailSubject = subject,
                Message = message
            };

            if (!string.IsNullOrEmpty(userInput))
            {
                emailIntent.Entities.email = emailAdress;
                if (emailAdress != null)
                {
                    emailIntent.Entities._instance.email = new InstanceData[emailAdress.Length];

                    for (int i = 0; i < emailAdress.Length; i++)
                    {
                        var email = emailAdress[i];
                        var startIndex = userInput.IndexOf(email);
                        var endIndex = userInput.IndexOf(email) + email.Length;

                        InstanceData originalEmailAdress = new InstanceData()
                        {
                            StartIndex = startIndex,
                            EndIndex = endIndex
                        };
                        emailIntent.Entities._instance.email[i] = originalEmailAdress;
                    }
                }
            }

            return emailIntent;
        }
    }
}

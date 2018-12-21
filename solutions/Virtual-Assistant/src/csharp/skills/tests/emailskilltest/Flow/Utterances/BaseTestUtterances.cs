using System.Collections.Generic;
using Luis;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.AI.Luis;
using static Luis.Email;

namespace EmailSkillTest.Flow.Utterances
{
    public class BaseTestUtterances : Dictionary<string, Email>
    {
        public BaseTestUtterances()
        {
            this.Add(FirstOne, CreateIntent(FirstOne, Intent.SelectItem, ordinal: new double[1] { 1 }));
            this.Add(SecondOne, CreateIntent(SecondOne, Intent.SelectItem, ordinal: new double[1] { 2 }));
            this.Add(ThirdOne, CreateIntent(ThirdOne, Intent.SelectItem, ordinal: new double[1] { 3 }));

            this.Add(NumberOne, CreateIntent(NumberOne, Intent.SelectItem, ordinal: new double[1] { 1 }));
            this.Add(NumberTwo, CreateIntent(NumberTwo, Intent.SelectItem, ordinal: new double[1] { 2 }));
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

        public Email GetBaseNoneIntent()
        {
            var emailIntent = new Email
            {
                Intents = new Dictionary<Intent, IntentScore>()
            };
            emailIntent.Intents.Add(Intent.None, new IntentScore() { Score = TopIntentScore });

            return emailIntent;
        }

        protected Email CreateIntent(
            string userInput,
            Intent intent = Intent.None,
            double[] ordinal = null,
            double[] number = null,
            string[] contactName = null,
            string[] senderName = null,
            string[] emailAdress = null,
            string[] subject = null,
            string[] message = null)
        {
            var emailIntent = new Email
            {
                Text = userInput,

                Intents = new Dictionary<Intent, IntentScore>()
            };
            emailIntent.Intents.Add(intent, new IntentScore() { Score = TopIntentScore });

            emailIntent.Entities = new _Entities
            {
                _instance = new _Entities._Instance(),

                ordinal = ordinal,
                number = number,
                ContactName = contactName,
                SenderName = senderName,
                EmailSubject = subject,
                Message = message
            };

            if (!string.IsNullOrEmpty(userInput))
            {
                emailIntent.Entities.EmailAddress = emailAdress;
                if (emailAdress != null)
                {
                    emailIntent.Entities._instance.EmailAddress = new InstanceData[emailAdress.Length];

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
                        emailIntent.Entities._instance.EmailAddress[i] = originalEmailAdress;
                    }
                }
            }

            return emailIntent;
        }
    }
}

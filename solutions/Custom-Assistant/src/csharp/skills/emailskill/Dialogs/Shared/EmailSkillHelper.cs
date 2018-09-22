// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Threading;
using System.Threading.Tasks;
using Luis;
using Microsoft.Bot.Builder;

namespace EmailSkill.Dialogs.Shared
{
    /// <summary>
    /// Helper class for email skill.
    /// </summary>
    public class EmailSkillHelper
    {
        /// <summary>
        /// Get luis result from user input or parent bot.
        /// </summary>
        /// <param name="context">The current turn context.</param>
        /// <param name="accessors">Email skill accessors.</param>
        /// <param name="services">Email skill services.</param>
        /// <param name="cancellationToken">The cancellation token.</param>
        /// <returns>The luis result.</returns>
        public static async Task<Email> GetLuisResult(ITurnContext context, EmailSkillAccessors accessors, EmailSkillServices services, CancellationToken cancellationToken)
        {
            var state = await accessors.EmailSkillState.GetAsync(context);

            Email luisResult = null;

            if (state.LuisResultPassedFromSkill != null)
            {
                luisResult = (Email)state.LuisResultPassedFromSkill;
            }
            else
            {
                luisResult = await services.LuisRecognizer.RecognizeAsync<Email>(context, cancellationToken);
            }

            await DigestEmailLuisResult(context, accessors, luisResult);
            return luisResult;
        }

        /// <summary>
        /// Set luis result to conversation state.
        /// </summary>
        /// <param name="context">Turn context.</param>
        /// <param name="accessors">Email skill accessors.</param>
        /// <param name="luisResult">The Luis result.</param>
        /// <returns>representing the asynchronous operation.</returns>
        public static async Task DigestEmailLuisResult(ITurnContext context, EmailSkillAccessors accessors, Email luisResult)
        {
            try
            {
                var state = await accessors.EmailSkillState.GetAsync(context);
                if (context.Activity.Text != null)
                {
                    var words = context.Activity.Text.Split(' ');
                    foreach (var word in words)
                    {
                        switch (word)
                        {
                            case "high":
                            case "important":
                                state.IsImportant = true;
                                break;
                            case "unread":
                                state.IsRead = true;
                                break;
                        }
                    }
                }

                var entity = luisResult.Entities;
                if (entity.ContactName != null)
                {
                    foreach (var name in entity.ContactName)
                    {
                        if (!state.NameList.Contains(name))
                        {
                            state.NameList.Add(name);
                        }
                    }
                }

                if (entity.EmailSubject != null)
                {
                    state.Subject = entity.EmailSubject[0];
                }

                if (entity.Message != null)
                {
                    state.Content = entity.Message[0];
                }

                if (entity.SenderName != null)
                {
                    state.SenderName = entity.SenderName[0];
                }

                if (entity.datetime != null)
                {
                    // todo: enable date time
                    // case "builtin.datetimeV2.date":
                    // case "builtin.datetimeV2.datetime":
                    // foreach (dynamic value in resolution["values"])
                    // {
                    //    var start = value["value"].ToString();
                    //    var dateTime = DateTime.Parse(start);
                    //    state.StartDateTime = dateTime;
                    //    state.EndDateTime = DateTime.UtcNow;
                    // }

                    // break;
                    // case "builtin.datetimeV2.datetimerange":
                    // foreach (dynamic value in resolution["values"])
                    // {
                    //    var start = value["start"].ToString();
                    //    var end = value["end"].ToString();
                    //    state.StartDateTime = DateTime.Parse(start);
                    //    state.EndDateTime = DateTime.Parse(end);
                    // }

                    // break;
                }

                if (entity.ordinal != null)
                {
                    try
                    {
                        var emailList = state.MessageList;
                        var value = entity.ordinal[0];
                        if (Math.Abs(value - (int)value) < double.Epsilon)
                        {
                            var num = (int)value;
                            if (emailList != null && num > 0 && num <= emailList.Count)
                            {
                                state.Message.Clear();
                                state.Message.Add(emailList[num - 1]);
                            }
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }

                if (entity.number != null && entity.ordinal != null)
                {
                    try
                    {
                        var emailList = state.MessageList;
                        var value = entity.ordinal[0];
                        if (Math.Abs(value - (int)value) < double.Epsilon)
                        {
                            var num = (int)value;
                            if (emailList != null && num > 0 && num <= emailList.Count)
                            {
                                state.Message.Clear();
                                state.Message.Add(emailList[num - 1]);
                            }
                        }
                    }
                    catch
                    {
                        // ignored
                    }
                }
            }
            catch
            {
                // put log here
            }
        }
    }
}

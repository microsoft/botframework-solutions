// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System.Net.Http.Headers;

namespace EmailSkill
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Threading.Tasks;
    using Microsoft.Graph;

    /// <summary>
    /// Mail service used to call real apis.
    /// </summary>
    public class MailService : IMailService
    {
        private GraphServiceClient _graphClient;
        private TimeZoneInfo _timeZoneInfo;

        /// <summary>
        /// Initializes a new instance of the <see cref="MailService"/> class.
        /// Init service use token.
        /// </summary>
        /// <param name="token">access token.</param>
        /// <param name="timeZoneInfo">timeZoneInfo.</param>
        /// <returns>User service itself.</returns>
        public MailService(string token, TimeZoneInfo timeZoneInfo)
        {
            this._graphClient = this.GetAuthenticatedClient(token);
            this._timeZoneInfo = timeZoneInfo;
        }

        /// <summary>
        /// Get an authenticated ms graph client use access token.
        /// </summary>
        /// <param name="accessToken">access token.</param>
        /// <returns>Authenticated graph service client.</returns>
        public GraphServiceClient GetAuthenticatedClient(string accessToken)
        {
            GraphServiceClient graphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    async (requestMessage) =>
                    {
                        // Append the access token to the request.
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);

                        // Get event times in the current time zone.
                        requestMessage.Headers.Add("Prefer", "outlook.timezone=\"" + _timeZoneInfo.Id + "\"");

                        await Task.CompletedTask;
                    }));
            return graphClient;
        }

        /// <summary>
        /// Get messages in all the current user's mail folders.
        /// </summary>
        /// <param name="fromTime">search condition, start time.</param>
        /// <param name="toTime">search condition, end time.</param>
        /// <param name="getUnRead">bool flag, if get unread email.</param>
        /// <param name="isImportant">bool flag, if get important email.</param>
        /// <param name="directlyToMe">bool flag, if filter email directly to me.</param>
        /// <param name="fromAddress">search condition, filter email from this address.</param>
        /// <param name="skip">number of skipped result.</param>
        /// <returns>A <see cref="Task{TResult}"/> representing the result of the asynchronous operation.</returns>
        public async Task<List<Message>> GetMyMessages(DateTime fromTime, DateTime toTime, bool getUnRead = false, bool isImportant = false, bool directlyToMe = false, string fromAddress = null, int skip = 0)
        {
            var optionList = new List<QueryOption>();
            var filterString = string.Empty;
            if (getUnRead)
            {
                filterString = this.AppendFilterString(filterString, "isread:false");
            }

            if (isImportant)
            {
                filterString = this.AppendFilterString(filterString, "importance:high");
            }

            if (directlyToMe)
            {
                User me = await this._graphClient.Me.Request().GetAsync();
                var address = me.Mail ?? me.UserPrincipalName;
                filterString = this.AppendFilterString(filterString, $"to:{address}");
            }

            if (!string.IsNullOrEmpty(fromAddress))
            {
                filterString = this.AppendFilterString(filterString, $"from:{fromAddress}");
            }

            if (!string.IsNullOrEmpty(filterString))
            {
                optionList.Add(new QueryOption("$search", $"\"{filterString}\""));
            }

            // skip can't be used with search
            // optionList.Add(new QueryOption("$skip", $"{page}"));

            // some message don't have receiveddatetime. use last modified datetime.
            // optionList.Add(new QueryOption(GraphQueryConstants.Orderby, "lastModifiedDateTime desc"));
            // only get emails from Inbox folder.
            IMailFolderMessagesCollectionPage messages = optionList.Count != 0 ? await this._graphClient.Me.MailFolders.Inbox.Messages.Request(optionList).GetAsync() : await this._graphClient.Me.MailFolders.Inbox.Messages.Request().GetAsync();
            List<Message> result = new List<Message>();

            var done = false;
            while (messages?.Count > 0 && !done)
            {
                foreach (Message message in messages)
                {
                    var receivedDateTime = message.ReceivedDateTime;
                    if (receivedDateTime > fromTime && receivedDateTime < toTime)
                    {
                        if (skip > 0)
                        {
                            skip--;
                        }
                        else
                        {
                            result.Add(message);
                        }
                    }
                    else
                    {
                        done = true;
                    }

                    // make size to 5 to save memory
                    // 5 is the page size
                    if (result.Count == 5)
                    {
                        return result.OrderByDescending(me => me.ReceivedDateTime).ToList();
                    }
                }

                if (messages.NextPageRequest != null)
                {
                    messages = await messages.NextPageRequest.GetAsync();
                }
                else
                {
                    done = true;
                }
            }

            return result.OrderByDescending(message => message.ReceivedDateTime).ToList();
        }

        /// <summary>
        /// Add a new filter condition of.
        /// </summary>
        /// <param name="old">before append a new condition.</param>
        /// <param name="filterString">new filter condition.</param>
        /// <returns>new filter string.</returns>
        public string AppendFilterString(string old, string filterString)
        {
            string result = old;
            if (string.IsNullOrEmpty(old))
            {
                result += filterString;
            }
            else
            {
                result += $" and {filterString}";
            }

            return result;
        }

        /// <summary>
        /// Send an email message.
        /// </summary>
        /// <param name="content">Email Body.</param>
        /// <param name="subject">Eamil Subject.</param>
        /// <param name="recipients">List of recipient.</param>
        /// <returns>Completed Task.</returns>
        public async Task SendMessage(string content, string subject, List<Recipient> recipients)
        {
            // Create the recipient list. This snippet uses the current user as the recipient.
            User me = await this._graphClient.Me.Request().Select("Mail, UserPrincipalName").GetAsync();
            string address = me.Mail ?? me.UserPrincipalName;

            // todo: I don't know why but recipient list need to be create again to avoid 400 error
            List<Recipient> re = new List<Recipient>();
            foreach (var recipient in recipients)
            {
                re.Add(new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = recipient.EmailAddress.Address,
                    },
                });
            }

            // Create the message.
            Message email = new Message
            {
                Body = new ItemBody
                {
                    Content = content,
                    ContentType = BodyType.Html,
                },
                Subject = subject,
                ToRecipients = re,
            };

            // Send the message.
            await this._graphClient.Me.SendMail(email, true).Request().PostAsync();

            // This operation doesn't return anything.
        }

        /// <summary>
        /// Reply to a specified message.
        /// </summary>
        /// <param name="id">The Id of message to reply.</param>
        /// <param name="text">The rely message body.</param>
        /// <returns>Completed Task.</returns>
        public async Task<List<Message>> ReplyToMessage(string id, string text)
        {
            List<Message> items = new List<Message>();

            // Reply to the message.
            await this._graphClient.Me.Messages[id].ReplyAll(text).Request().PostAsync();

            // This operation doesn't return anything.
            return items;
        }

        /// <summary>
        /// Update a specified message.
        /// </summary>
        /// <param name="updatedMessage">updatedMessage.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task<Message> UpdateMessage(Message updatedMessage)
        {
            // Update the message.
            var result = await this._graphClient.Me.Messages[updatedMessage.Id].Request().UpdateAsync(updatedMessage);

            return result;
        }

        /// <summary>
        /// Forward a specific message.
        /// </summary>
        /// <param name="id">id.</param>
        /// <param name="comment">comment.</param>
        /// <param name="recipients">recipients.</param>
        /// <returns>A <see cref="Task"/> representing the asynchronous operation.</returns>
        public async Task ForwardMessage(string id, string comment, List<Recipient> recipients)
        {
            // todo: I don't know why but recipient list need to be create again to avoid 400 error
            List<Recipient> re = new List<Recipient>();
            foreach (var recipient in recipients)
            {
                re.Add(new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = recipient.EmailAddress.Address,
                    },
                });
            }

            // This operation doesn't return anything.
            await this._graphClient.Me.Messages[id].Forward(comment, re).Request().PostAsync();
        }
    }
}

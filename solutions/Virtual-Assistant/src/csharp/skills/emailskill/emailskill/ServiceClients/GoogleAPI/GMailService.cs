// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Mail;
using System.Text;
using System.Threading.Tasks;
using EmailSkill.Dialogs.Shared.Resources.Strings;
using Google;
using Google.Apis.Auth.OAuth2;
using Google.Apis.Auth.OAuth2.Flows;
using Google.Apis.Auth.OAuth2.Responses;
using Google.Apis.Gmail.v1;
using Google.Apis.Gmail.v1.Data;
using Google.Apis.Requests;
using Google.Apis.Services;
using Google.Apis.Util.Store;
using Microsoft.Graph;
using MimeKit;
using GmailMessage = Google.Apis.Gmail.v1.Data.Message;
using MSMessage = Microsoft.Graph.Message;

namespace EmailSkill.ServiceClients.GoogleAPI
{
    /// <summary>
    /// The Google Email API service.
    /// </summary>
    public class GMailService : IMailService
    {
        private static GmailService service;
        private readonly int maxSize = 50;
        private string pageToken = string.Empty;

        /// <summary>
        /// Initializes a new instance of the <see cref="GMailService"/> class.
        /// </summary>
        /// <param name="baseClientService">baseClientService.</param>
        public GMailService(GmailService baseClientService)
        {
            service = baseClientService;
        }

        public static GmailService GetServiceClient(GoogleClient config, string token)
        {
            // Create Gmail API service.
            var flow = new GoogleAuthorizationCodeFlow(new GoogleAuthorizationCodeFlow.Initializer
            {
                ClientSecrets = new ClientSecrets
                {
                    ClientId = config.ClientId,
                    ClientSecret = config.ClientSecret,
                },
                Scopes = config.Scopes,
                DataStore = new FileDataStore("Store"),
            });

            var tokenRes = new TokenResponse
            {
                AccessToken = token,
                ExpiresInSeconds = 3600,
                IssuedUtc = DateTime.UtcNow,
            };

            var credential = new UserCredential(flow, Environment.UserName, tokenRes);
            var service = new GmailService(new BaseClientService.Initializer()
            {
                HttpClientInitializer = credential,
                ApplicationName = config.ApplicationName,
            });

            return service;
        }

        public static string Base64UrlEncode(string text)
        {
            var textBytes = Encoding.UTF8.GetBytes(text);

            var result = System.Convert.ToBase64String(textBytes);
            result = result.Split('=')[0]; // Remove any trailing '='s
            result = result.Replace('+', '-'); // 62nd char of encoding
            result = result.Replace('/', '_'); // 63rd char of encoding
            return result;
        }

        // decode from base64url to utf-8 bytes
        public static byte[] Base64UrlDecode(string text)
        {
            string result = text;
            result = result.Replace('-', '+'); // 62nd char of encoding
            result = result.Replace('_', '/'); // 63rd char of encoding

            // Pad with trailing '='s
            switch (result.Length % 4)
            {
                case 0: break; // No pad chars in this case
                case 2: result += "=="; break; // Two pad chars
                case 3: result += "="; break; // One pad char
                default:
                    throw new System.Exception(
             "Illegal base64url string!");
            }

            byte[] textBytes = Convert.FromBase64String(result);
            return textBytes;
        }

        // decode to mimeMessage
        public static MimeMessage DecodeToMessage(string text)
        {
            byte[] msg = Base64UrlDecode(text);
            MemoryStream mm = new MemoryStream(msg);
            MimeKit.MimeMessage mime = MimeKit.MimeMessage.Load(mm);
            return mime;
        }

        /// <inheritdoc/>
        public async Task ForwardMessageAsync(string id, string content, List<Recipient> recipients)
        {
            try
            {
                // getOriginalMessage
                var (originalMessage, threadId) = await this.GetMessageById(id);
                var forward = new MimeMessage();
                foreach (var recipient in recipients)
                {
                    forward.To.Add(new MailboxAddress(recipient.EmailAddress.Address));
                }

                // set the reply subject
                forward.Subject = string.Format(EmailCommonStrings.ForwardReplyFormat, originalMessage.Subject);

                // construct the References headers
                foreach (var mid in originalMessage.References)
                {
                    forward.References.Add(mid);
                }

                if (!string.IsNullOrEmpty(originalMessage.MessageId))
                {
                    forward.References.Add(originalMessage.MessageId);
                }

                // quote the original message text
                using (var quoted = new StringWriter())
                {
                    var sender = originalMessage.Sender ?? originalMessage.From.Mailboxes.FirstOrDefault();
                    quoted.WriteLine(content);
                    quoted.WriteLine();
                    quoted.WriteLine(EmailCommonStrings.ForwardMessage);
                    quoted.WriteLine(EmailCommonStrings.FromFormat, originalMessage.From);
                    quoted.WriteLine(EmailCommonStrings.DateFormat, originalMessage.Date);
                    quoted.WriteLine(EmailCommonStrings.SubjectFormat, originalMessage.Subject);
                    quoted.WriteLine(EmailCommonStrings.ToFormat, originalMessage.To);
                    if (originalMessage.Cc.Count > 0)
                    {
                        quoted.WriteLine(EmailCommonStrings.CCFormat, originalMessage.Cc);
                    }

                    using (var reader = new StringReader(originalMessage.TextBody))
                    {
                        string line;

                        while ((line = reader.ReadLine()) != null)
                        {
                            quoted.Write("> ");
                            quoted.WriteLine(line);
                        }
                    }

                    content = quoted.ToString();
                }

                var sendRequest = service.Users.Messages.Send(
                    new GmailMessage()
                    {
                        Raw = Base64UrlEncode(forward.ToString() + content),
                        ThreadId = threadId,
                    }, "me");
                await ((IClientServiceRequest<GmailMessage>)sendRequest).ExecuteAsync();
            }
            catch (GoogleApiException ex)
            {
                throw GoogleClient.HandleGoogleAPIException(ex);
            }
        }

        public async Task SendMessageAsync(string content, string subject, List<Recipient> recipients)
        {
            try
            {
                // get from address
                var profileRequest = service.Users.GetProfile("me");
                var user = ((IClientServiceRequest<Profile>)profileRequest).Execute();
                var mess = new MailMessage
                {
                    Subject = subject,
                    From = new MailAddress(user.EmailAddress)
                };

                foreach (var re in recipients)
                {
                    mess.To.Add(new MailAddress(re.EmailAddress.Address));
                }

                mess.ReplyToList.Add(new MailAddress(user.EmailAddress));
                var adds = AlternateView.CreateAlternateViewFromString(content, new System.Net.Mime.ContentType("text/plain"));
                adds.ContentType.CharSet = Encoding.UTF8.WebName;
                mess.AlternateViews.Add(adds);

                var mime = MimeMessage.CreateFromMailMessage(mess);
                var sendRequest = service.Users.Messages.Send(
                    new GmailMessage()
                    {
                        Raw = Base64UrlEncode(mime.ToString()),
                    }, "me");
                await ((IClientServiceRequest<GmailMessage>)sendRequest).ExecuteAsync();
            }
            catch (GoogleApiException ex)
            {
                throw GoogleClient.HandleGoogleAPIException(ex);
            }
        }

        public async Task<List<MSMessage>> ReplyToMessageAsync(string id, string content)
        {
            try
            {
                var (originMessage, threadId) = await this.GetMessageById(id);
                var reply = new MimeMessage();

                // reply to the sender of the message
                if (originMessage.ReplyTo.Count > 0)
                {
                    reply.To.AddRange(originMessage.ReplyTo);
                }
                else if (originMessage.From.Count > 0)
                {
                    reply.To.AddRange(originMessage.From);
                }
                else if (originMessage.Sender != null)
                {
                    reply.To.Add(originMessage.Sender);
                }

                // set the reply subject
                if (!originMessage.Subject.StartsWith(EmailCommonStrings.Reply, StringComparison.OrdinalIgnoreCase))
                {
                    reply.Subject = string.Format(EmailCommonStrings.ReplyReplyFormat, originMessage.Subject);
                }
                else
                {
                    reply.Subject = originMessage.Subject;
                }

                // construct the In-Reply-To and References headers
                if (!string.IsNullOrEmpty(originMessage.MessageId))
                {
                    reply.InReplyTo = originMessage.MessageId;
                    foreach (var mid in originMessage.References)
                    {
                        reply.References.Add(mid);
                    }

                    reply.References.Add(originMessage.MessageId);
                }

                // quote the original message text
                using (var quoted = new StringWriter())
                {
                    var sender = originMessage.Sender ?? originMessage.From.Mailboxes.FirstOrDefault();
                    quoted.WriteLine(EmailCommonStrings.EmailInfoFormat, originMessage.Date.ToString("f"), !string.IsNullOrEmpty(sender.Name) ? sender.Name : sender.Address);
                    using (var reader = new StringReader(originMessage.TextBody))
                    {
                        string line;

                        while ((line = reader.ReadLine()) != null)
                        {
                            quoted.Write("> ");
                            quoted.WriteLine(line);
                        }
                    }

                    content = quoted.ToString();
                }

                var sendRequest = service.Users.Messages.Send(
                new GmailMessage()
                {
                    Raw = Base64UrlEncode(reply.ToString() + content),
                    ThreadId = threadId,
                }, "me");
                await ((IClientServiceRequest<GmailMessage>)sendRequest).ExecuteAsync();
                return null;
            }
            catch (GoogleApiException ex)
            {
                throw GoogleClient.HandleGoogleAPIException(ex);
            }
        }

        public async Task<List<MSMessage>> GetMyMessagesAsync(DateTime fromTime, DateTime toTime, bool getUnRead = false, bool isImportant = false, bool directlyToMe = false, string fromAddress = null)
        {
            try
            {
                var profileRequest = service.Users.GetProfile("me");
                var user = ((IClientServiceRequest<Profile>)profileRequest).Execute();
                var userAddress = user.EmailAddress;

                string searchOperation = string.Empty;
                searchOperation = this.AppendFilterString(searchOperation, "in:inbox");
                if (getUnRead)
                {
                    searchOperation = this.AppendFilterString(searchOperation, "is:unread");
                }

                if (isImportant)
                {
                    searchOperation = this.AppendFilterString(searchOperation, "is:important");
                }

                if (directlyToMe)
                {
                    searchOperation = this.AppendFilterString(searchOperation, $"deliveredto:{userAddress}");
                }

                if (fromAddress != null)
                {
                    searchOperation = this.AppendFilterString(searchOperation, $"from:{fromAddress}");
                }

                if (fromTime != null)
                {
                    searchOperation = this.AppendFilterString(searchOperation, $"after:{fromTime.Year}/{fromTime.Month}/{fromTime.Day}");
                }

                if (toTime != null)
                {
                    searchOperation = this.AppendFilterString(searchOperation, $"before:{toTime.Year}/{toTime.Month}/{toTime.Day}");
                }

                var request = service.Users.Messages.List("me");
                request.Q = searchOperation;
                request.MaxResults = this.maxSize;

                var response = await ((IClientServiceRequest<ListMessagesResponse>)request).ExecuteAsync();
                var result = new List<MSMessage>();

                // response.Messages only have id and threadID
                if (response.Messages != null)
                {
                    var messages = await Task.WhenAll(response.Messages.Select(temp =>
                    {
                        var req = service.Users.Messages.Get("me", temp.Id);
                        req.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Raw;
                        return ((IClientServiceRequest<GmailMessage>)req).ExecuteAsync();
                    }));
                    if (messages != null && messages.Length > 0)
                    {
                        foreach (var m in messages)
                        {
                            // map to msgraph email
                            var ms = this.MapMimeMessageToMSMessage(DecodeToMessage(m.Raw));
                            ms.BodyPreview = m.Snippet;
                            ms.Id = m.Id;
                            ms.WebLink = $"https://mail.google.com/mail/#inbox/{m.Id}";
                            result.Add(ms);
                        }
                    }
                }

                if (response.NextPageToken != null && response.NextPageToken != string.Empty)
                {
                    this.pageToken = response.NextPageToken;
                }
                else
                {
                    this.pageToken = string.Empty;
                }

                return result;
            }
            catch (GoogleApiException ex)
            {
                throw GoogleClient.HandleGoogleAPIException(ex);
            }
        }

        public Task DeleteMessageAsync(string id)
        {
            throw new NotImplementedException();
        }

        public string AppendFilterString(string old, string filterString)
        {
            string result = old;
            if (string.IsNullOrEmpty(old))
            {
                result += filterString;
            }
            else
            {
                result += $" {filterString}";
            }

            return result;
        }

        private async Task<(MimeMessage, string)> GetMessageById(string id)
        {
            var request = service.Users.Messages.Get("me", id);
            request.Format = UsersResource.MessagesResource.GetRequest.FormatEnum.Raw;
            var response = await ((IClientServiceRequest<GmailMessage>)request).ExecuteAsync();
            var mime = DecodeToMessage(response.Raw);
            return (mime, response.ThreadId);
        }

        private MSMessage MapMimeMessageToMSMessage(MimeMessage mime)
        {
            MSMessage message = new MSMessage
            {
                ReceivedDateTime = mime.Date
            };
            if (mime.To != null)
            {
                var to = new List<Recipient>();
                foreach (var address in mime.To)
                {
                    to.Add(new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = ((MailboxAddress)address).Address,
                            Name = address.Name == string.Empty ? ((MailboxAddress)address).Address : address.Name,
                        },
                    });
                }

                message.ToRecipients = to;
            }

            if (mime.From != null && mime.From.Count > 0)
            {
                message.From = new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = ((MailboxAddress)mime.From[0]).Address, // mime.From[0].ToString()
                        Name = mime.From[0].Name == string.Empty ? ((MailboxAddress)mime.From[0]).Address : mime.From[0].Name,
                    },
                };
                message.Sender = new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = ((MailboxAddress)mime.From[0]).Address,
                        Name = mime.From[0].Name == string.Empty ? ((MailboxAddress)mime.From[0]).Address : mime.From[0].Name,
                    },
                };
            }

            if (mime.Sender != null)
            {
                message.Sender = new Recipient
                {
                    EmailAddress = new EmailAddress
                    {
                        Address = mime.Sender.Address,
                        Name = mime.Sender.Name == string.Empty ? mime.Sender.Address : mime.Sender.Name,
                    },
                };
            }

            if (mime.Subject != null)
            {
                message.Subject = mime.Subject;
            }

            if (mime.Body != null)
            {
                var textBody = mime.BodyParts.OfType<TextPart>().FirstOrDefault();
                message.Body = new ItemBody
                {
                    Content = textBody.Text,
                    ContentType = BodyType.Text,
                };
            }

            if (mime.Cc != null)
            {
                var cc = new List<Recipient>();
                foreach (var address in mime.Cc)
                {
                    cc.Add(new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = ((MailboxAddress)address).Address,
                            Name = address.Name == string.Empty ? ((MailboxAddress)address).Address : address.Name,
                        },
                    });
                }

                message.CcRecipients = cc;
            }

            if (mime.ReplyTo != null)
            {
                var replyTo = new List<Recipient>();
                foreach (var address in mime.ReplyTo)
                {
                    replyTo.Add(new Recipient
                    {
                        EmailAddress = new EmailAddress
                        {
                            Address = ((MailboxAddress)address).Address,
                            Name = address.Name == string.Empty ? ((MailboxAddress)address).Address : address.Name,
                        },
                    });
                }

                message.ReplyTo = replyTo;
            }

            return message;
        }
    }
}
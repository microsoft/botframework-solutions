// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkill.ServiceClients
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Net.Http.Headers;
    using System.Text;
    using System.Text.RegularExpressions;
    using System.Threading.Tasks;
    using System.Xml;
    using global::ToDoSkill.Models;
    using Microsoft.Bot.Solutions.Dialogs.BotResponseFormatters;
    using Microsoft.Bot.Solutions.Skills;
    using Microsoft.Graph;
    using Newtonsoft.Json;

    /// <summary>
    /// To Do skill helper class.
    /// </summary>
    public class ServiceHelper
    {
        private const string APIErrorAccessDenied = "erroraccessdenied";

        private static readonly Regex ComplexTokensRegex = new Regex(@"\{[^{\}]+(?=})\}", RegexOptions.Compiled);
        private static readonly List<IBotResponseFormatter> ResponseFormatters = new List<IBotResponseFormatter>();
        private static readonly IBotResponseFormatter DefaultFormatter = new DefaultBotResponseFormatter();
        private static HttpClient httpClient = new HttpClient();

        /// <summary>
        /// Generate create notebook http request.
        /// </summary>
        /// <param name="makePageContent">page content.</param>
        /// <param name="sectionContentUrl">section content url.</param>
        /// <param name="notebookName">notebook name.</param>
        /// <returns>Generated http request message.</returns>
        public static HttpRequestMessage GenerateCreateNotebookHttpRequest(string makePageContent, string sectionContentUrl, string notebookName)
        {
            var pageCreateCommand = "{\"displayName\" : \"" + notebookName + "\"}";

            return new HttpRequestMessage(new HttpMethod("POST"), sectionContentUrl)
            { Content = new StringContent(pageCreateCommand, Encoding.UTF8, "application/json") };
        }

        /// <summary>
        /// Generate create section http request.
        /// </summary>
        /// <param name="makePageContent">page content.</param>
        /// <param name="sectionContentUrl">section content url.</param>
        /// <param name="sectionTitle">section title.</param>
        /// <returns>Generated http request message.</returns>
        public static HttpRequestMessage GenerateCreateSectionHttpRequest(string makePageContent, string sectionContentUrl, string sectionTitle)
        {
            string pageCreateCommand = "{\"displayName\" : \"" + sectionTitle + "\"}";

            return new HttpRequestMessage(new HttpMethod("POST"), sectionContentUrl)
            { Content = new StringContent(pageCreateCommand, Encoding.UTF8, "application/json") };
        }

        /// <summary>
        /// Generate create page http request.
        /// </summary>
        /// <param name="pageTitle">page title.</param>
        /// <param name="sectionUrl">section url.</param>
        /// <returns>Generated http request message.</returns>
        public static HttpRequestMessage GenerateCreatePageHttpRequest(string pageTitle, string sectionUrl)
        {
            var html = GeneratePageHtml(pageTitle);
            var content = new MultipartFormDataContent("myBoundary");

            content.Add(new StringContent(html, Encoding.UTF8, "text/html"), "\"Presentation\"");

            var httpRequestMessage = new HttpRequestMessage(
                HttpMethod.Post,
                sectionUrl);

            httpRequestMessage.Content = content;
            return httpRequestMessage;
        }

        /// <summary>
        /// Generate add To Do http request.
        /// </summary>
        /// <param name="todoText">To Do text.</param>
        /// <param name="todoPageContent">To Do page content.</param>
        /// <param name="pageContentUrl">page content url.</param>
        /// <returns>Generated http request message.</returns>
        public static HttpRequestMessage GenerateAddToDoHttpRequest(string todoText, string todoPageContent, string pageContentUrl)
        {
            XmlDocument doc = new XmlDocument();
            doc.LoadXml(todoPageContent);
            var root = doc.DocumentElement;
            var firstToDoElement = root.SelectSingleNode("body")
                ?.SelectSingleNode("div")
                ?.SelectNodes("p")
                ?.Cast<XmlNode>()
                ?.FirstOrDefault(node => node.Attributes["data-tag"] != null && node.Attributes["data-tag"].Value.StartsWith("to-do"));

            var todoDivId = string.Empty;
            if (firstToDoElement != null)
            {
                todoDivId = firstToDoElement.ParentNode.Attributes["id"].Value;
            }

            object patchCommand;
            if (!string.IsNullOrWhiteSpace(todoDivId))
            {
                patchCommand = new
                {
                    target = todoDivId,
                    action = "append",
                    position = "before",
                    content = $"<p data-tag='to-do' style='margin-top:0pt;margin-bottom:0pt'>{todoText}</p>",
                };
            }
            else
            {
                patchCommand = new
                {
                    target = "body",
                    action = "append",
                    position = "before",
                    content = $"<div><p data-tag='to-do' style='margin-top:0pt;margin-bottom:0pt'>{todoText}</p></div>",
                };
            }

            return new HttpRequestMessage(new HttpMethod("PATCH"), pageContentUrl)
            {
                Content = new StringContent(JsonConvert.SerializeObject(new[] { patchCommand }), Encoding.UTF8, "application/json"),
            };
        }

        /// <summary>
        /// Generate mark all To Dos http request.
        /// </summary>
        /// <param name="taskItems">Task items.</param>
        /// <param name="pageContentUrl">page content url.</param>
        /// <returns>Generated http request message.</returns>
        public static HttpRequestMessage GenerateMarkToDosHttpRequest(List<TaskItem> taskItems, string pageContentUrl)
        {
            var commands = new List<object>();
            foreach (var toDoTaskActivity in taskItems)
            {
                var patchCommand = new
                {
                    target = toDoTaskActivity.Id,
                    action = "replace",
                    content = $"<p data-tag='to-do:completed' style='margin-top:0pt;margin-bottom:0pt'>{toDoTaskActivity.Topic}</p>",
                };

                commands.Add(patchCommand);
            }

            return new HttpRequestMessage(new HttpMethod("PATCH"), pageContentUrl)
            {
                Content = new StringContent(JsonConvert.SerializeObject(commands), Encoding.UTF8, "application/json"),
            };
        }

        /// <summary>
        /// Generate delete all To Dos http request.
        /// </summary>
        /// <param name="taskItems">Task items.</param>
        /// <param name="pageContentUrl">page content url.</param>
        /// <returns>Generated http request message.</returns>
        public static HttpRequestMessage GenerateDeleteToDosHttpRequest(List<TaskItem> taskItems, string pageContentUrl)
        {
            var commands = new List<object>();
            foreach (var toDoTaskActivity in taskItems)
            {
                var patchCommand = new
                {
                    target = toDoTaskActivity.Id,
                    action = "replace",
                    content = "<p></p>",
                };

                commands.Add(patchCommand);
            }

            return new HttpRequestMessage(new HttpMethod("PATCH"), pageContentUrl)
            {
                Content = new StringContent(JsonConvert.SerializeObject(commands), Encoding.UTF8, "application/json"),
            };
        }

        /// <summary>
        /// Generate onenote page content html.
        /// </summary>
        /// <param name="pageTitle">page title.</param>
        /// <returns>Generated page html.</returns>
        public static string GeneratePageHtml(string pageTitle)
        {
            var timeStamp = DateTime.Now.ToString("yyyy-MM-ddTHH:mm:00.0000000");
            var htmlTemplate = new StringBuilder("<!DOCTYPE html><html>");
            htmlTemplate.Append($"<head><title>{pageTitle}</title><meta name='created' content='{timeStamp}' /></head><body>");
            htmlTemplate.Append("</body></html>");
            return htmlTemplate.ToString();
        }

        /// <summary>
        /// Generate add Outlook task http request.
        /// </summary>
        /// <param name="url">url.</param>
        /// <param name="taskSubject">page title.</param>
        /// <returns>Generated page html.</returns>
        public static HttpRequestMessage GenerateAddOutlookTaskHttpRequest(string url, string taskSubject)
        {
            var taskObject = new
            {
                subject = taskSubject,
            };

            return new HttpRequestMessage(new HttpMethod("POST"), url)
            {
                Content = new StringContent(JsonConvert.SerializeObject(taskObject), Encoding.UTF8, "application/json"),
            };
        }

        /// <summary>
        /// Generate add Outlook task http request.
        /// </summary>
        /// <param name="url">url.</param>
        /// <param name="taskItem">Task item.</param>
        /// <returns>Generated page html.</returns>
        public static HttpRequestMessage GenerateDeleteOutlookTaskHttpRequest(string url, TaskItem taskItem)
        {
            return new HttpRequestMessage(new HttpMethod("DELETE"), url + "/" + taskItem.Id);
        }

        /// <summary>
        /// Generate add Outlook task http request.
        /// </summary>
        /// <param name="url">url.</param>
        /// <param name="taskItem">Task item.</param>
        /// <returns>Generated page html.</returns>
        public static HttpRequestMessage GenerateMarkOutlookTaskCompletedHttpRequest(string url, TaskItem taskItem)
        {
            return new HttpRequestMessage(new HttpMethod("POST"), url + "/" + taskItem.Id + "/complete");
        }

        /// <summary>
        /// Generate add Outlook task http request.
        /// </summary>
        /// <param name="url">url.</param>
        /// <param name="taskFolderName">Task folder name.</param>
        /// <returns>Generated page html.</returns>
        public static HttpRequestMessage GenerateCreateTaskFolderHttpRequest(string url, string taskFolderName)
        {
            var taskFolderObject = new
            {
                name = taskFolderName,
            };

            return new HttpRequestMessage(new HttpMethod("POST"), url)
            {
                Content = new StringContent(JsonConvert.SerializeObject(taskFolderObject), Encoding.UTF8, "application/json"),
            };
        }

        /// <summary>
        /// Generate httpClient.
        /// </summary>
        /// <param name="accessToken">API access token.</param>
        /// <returns>Generated httpClient.</returns>
        public static HttpClient GetHttpClient(string accessToken)
        {
            httpClient.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
            httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", accessToken);
            return httpClient;
        }

        public static SkillException HandleGraphAPIException(ServiceException ex)
        {
            var skillExceptionType = SkillExceptionType.Other;
            if (ex.Error.Code.Equals(APIErrorAccessDenied, StringComparison.InvariantCultureIgnoreCase))
            {
                skillExceptionType = SkillExceptionType.APIAccessDenied;
            }

            return new SkillException(skillExceptionType, ex.Message, ex);
        }

        public static ServiceException GenerateServiceException(dynamic errorResponse)
        {
            var errorObject = errorResponse.error;
            Error error = new Error();
            error.Code = errorObject.code.ToString();
            error.Message = errorObject.message.ToString();
            return new ServiceException(error);
        }

        /// <summary>
        /// Get an authenticated ms graph client use access token.
        /// </summary>
        /// <param name="accessToken">access token.</param>
        /// <returns>Authenticated graph service client.</returns>
        public static IGraphServiceClient GetAuthenticatedClient(string accessToken)
        {
            GraphServiceClient graphClient = new GraphServiceClient(
                new DelegateAuthenticationProvider(
                    async (requestMessage) =>
                    {
                        // Append the access token to the request.
                        requestMessage.Headers.Authorization = new AuthenticationHeaderValue("bearer", accessToken);
                        await Task.CompletedTask;
                    }));
            return graphClient;
        }
    }
}
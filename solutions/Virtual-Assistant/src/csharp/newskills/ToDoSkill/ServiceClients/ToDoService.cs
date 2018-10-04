// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkill
{
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Xml;
    using Microsoft.Graph;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// To Do bot service.
    /// </summary>
    public class ToDoService : IToDoService
    {
        private const string OneNoteNotebookName = "ToDoNotebook";
        private const string OneNoteSectionName = "ToDoSection";
        private const string OneNoteToPageName = "To Do";
        private readonly string graphBaseUrl = "https://graph.microsoft.com/v1.0/me";
        private HttpClient httpClient;
        private string pageId;

        /// <summary>
        /// Initializes ToDoService using token.
        /// </summary>
        /// <param name="token">the token used for msgraph API call.</param>
        /// <param name="pageId">the page id.</param>
        /// <returns>To Do service itself.</returns>
        public async Task<IToDoService> Init(string token, string pageId)
        {
            try
            {
                httpClient = ServiceHelper.GetHttpClient(token);
                if (string.IsNullOrEmpty(pageId))
                {
                    var notebookId = await GetOrCreateNotebookAsync(OneNoteNotebookName);
                    var sectionId = await GetOrCreateSectionAsync(notebookId, OneNoteSectionName);
                    this.pageId = await GetOrCreatePageAsync(sectionId, OneNoteToPageName);
                }
                else
                {
                    this.pageId = pageId;
                }

                return this;
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Get To Do list.
        /// </summary>
        /// <returns>Tuple of To Do task activities and onenote page id.</returns>
        public async Task<Tuple<List<ToDoItem>, string>> GetMyToDoList()
        {
            try
            {
                var pages = await GetOneNotePageById(pageId);

                var retryCount = 2;
                while ((pages == null || pages.Count == 0) && retryCount > 0)
                {
                    pages = await GetOneNotePageById(pageId);
                    retryCount--;
                }

                if (pages != null && pages.Count > 0)
                {
                    var todos = await GetToDos(pages.First().ContentUrl);
                    return new Tuple<List<ToDoItem>, string>(todos, pageId);
                }
                else
                {
                    throw new Exception("Can not get the To Do OneNote pages.");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Get default To Do page.
        /// </summary>
        /// <returns>Default onenote page.</returns>
        public async Task<OnenotePage> GetDefaultToDoPage()
        {
            try
            {
                var pages = await GetOneNotePageById(pageId);

                var retryCount = 2;
                while ((pages == null || pages.Count == 0) && retryCount > 0)
                {
                    pages = await GetOneNotePageById(pageId);
                    retryCount--;
                }

                if (pages != null && pages.Count > 0)
                {
                    return pages.First();
                }
                else
                {
                    throw new Exception("Can not get the To Do OneNote pages.");
                }
            }
            catch (Exception ex)
            {
                throw ex;
            }
        }

        /// <summary>
        /// Add To Do to onenote page.
        /// </summary>
        /// <param name="todoText">To Do text.</param>
        /// <param name="pageContentUrl">page content url.</param>
        /// <returns>Ture if succeed.</returns>
        public async Task<bool> AddToDoToOneNote(string todoText, string pageContentUrl)
        {
            var todoContent = await httpClient.GetStringAsync(pageContentUrl + "/?includeIDs=true");
            var httpRequestMessage = ServiceHelper.GenerateAddToDoHttpRequest(todoText, todoContent, pageContentUrl);
            var result = await httpClient.SendAsync(httpRequestMessage);
            return result.IsSuccessStatusCode;
        }

        /// <summary>
        /// Mark to do item as completed.
        /// </summary>
        /// <param name="toDoActivity">To Do activity.</param>
        /// <param name="pageContentUrl">page content url.</param>
        /// <returns>True if succeed.</returns>
        public async Task<bool> MarkToDoItemCompleted(ToDoItem toDoActivity, string pageContentUrl)
        {
            var httpRequestMessage = ServiceHelper.GenerateMarkToDoHttpRequest(toDoActivity, pageContentUrl);
            var result = await httpClient.SendAsync(httpRequestMessage);
            return result.IsSuccessStatusCode;
        }

        /// <summary>
        /// Mark all to do items as completed.
        /// </summary>
        /// <param name="toDoActivities">To Do activities.</param>
        /// <param name="pageContentUrl">page content url.</param>
        /// <returns>True if succeed.</returns>
        public async Task<bool> MarkAllToDoItemsCompleted(List<ToDoItem> toDoActivities, string pageContentUrl)
        {
            var httpRequestMessage = ServiceHelper.GenerateMarkToDosHttpRequest(toDoActivities, pageContentUrl);
            var result = await httpClient.SendAsync(httpRequestMessage);
            return result.IsSuccessStatusCode;
        }

        /// <summary>
        /// Delete To Do.
        /// </summary>
        /// <param name="toDoActivity">To Do activity.</param>
        /// <param name="pageContentUrl">page content url.</param>
        /// <returns>True if succeed.</returns>
        public async Task<bool> DeleteToDo(ToDoItem toDoActivity, string pageContentUrl)
        {
            var httpRequestMessage = ServiceHelper.GenerateDeleteToDoHttpRequest(toDoActivity, pageContentUrl);
            var result = await httpClient.SendAsync(httpRequestMessage);
            return result.IsSuccessStatusCode;
        }

        /// <summary>
        /// Delete all To Dos.
        /// </summary>
        /// <param name="toDoActivities">To Do activities.</param>
        /// <param name="pageContentUrl">page content url.</param>
        /// <returns>True if succeed.</returns>
        public async Task<bool> DeleteAllToDos(List<ToDoItem> toDoActivities, string pageContentUrl)
        {
            var httpRequestMessage = ServiceHelper.GenerateDeleteToDosHttpRequest(toDoActivities, pageContentUrl);
            var result = await httpClient.SendAsync(httpRequestMessage);
            return result.IsSuccessStatusCode;
        }

        private async Task<string> CreateOneNoteNotebook(string createNotebookUrl, string notebookName)
        {
            var makeSectionContent = await httpClient.GetStringAsync(createNotebookUrl);
            var httpRequestMessage = ServiceHelper.GenerateCreateNotebookHttpRequest(makeSectionContent, createNotebookUrl, notebookName);
            var result = await httpClient.SendAsync(httpRequestMessage);
            dynamic responseContent = JObject.Parse(await result.Content.ReadAsStringAsync());
            return (string)responseContent.id;
        }

        private async Task<string> GetOrCreateNotebookAsync(string notebookName)
        {
            var notebooksUrl = $"{graphBaseUrl}/onenote/notebooks";
            var onenoteNotebook = await GetOneNoteNotebook($"{notebooksUrl}?filter=name eq '{notebookName}'");
            if (onenoteNotebook.Count == 0)
            {
                return await CreateOneNoteNotebook(notebooksUrl, notebookName);
            }

            return onenoteNotebook[0].Id;
        }

        private async Task<List<Notebook>> GetOneNoteNotebook(string url)
        {
            return JsonConvert.DeserializeObject<List<Notebook>>(await ExecuteGraphFetch(url));
        }

        private async Task<string> CreateOneNoteSection(string sectionContentUrl, string sectionTitle)
        {
            var makeSectionContent = await httpClient.GetStringAsync(sectionContentUrl);
            var httpRequestMessage = ServiceHelper.GenerateCreateSectionHttpRequest(makeSectionContent, sectionContentUrl, sectionTitle);
            var result = await httpClient.SendAsync(httpRequestMessage);
            dynamic responseContent = JObject.Parse(await result.Content.ReadAsStringAsync());
            return (string)responseContent.id;
        }

        private async Task<string> GetOrCreateSectionAsync(string notebookId, string sectionTitle)
        {
            var sectionsUrl = $"{graphBaseUrl}/onenote/notebooks/{notebookId}/sections";
            var onenoteSection = await GetOneNoteSection($"{sectionsUrl}?filter=name eq '{sectionTitle}'");
            if (onenoteSection.Count == 0)
            {
                return await CreateOneNoteSection(sectionsUrl, sectionTitle);
            }

            return onenoteSection[0].Id;
        }

        private async Task<List<OnenoteSection>> GetOneNoteSection(string url)
        {
            return JsonConvert.DeserializeObject<List<OnenoteSection>>(await ExecuteGraphFetch(url));
        }

        private async Task<bool> CreateOneNotePage(string sectionUrl, string pageTitle)
        {
            var httpRequestMessage = ServiceHelper.GenerateCreatePageHttpRequest(pageTitle, sectionUrl);
            var result = await httpClient.SendAsync(httpRequestMessage);
            return result.IsSuccessStatusCode;
        }

        private async Task<string> GetOrCreatePageAsync(string sectionId, string pageTitle)
        {
            var pagesUrl = $"{graphBaseUrl}/onenote/sections/{sectionId}/pages";
            var onenotePage = await GetOneNotePage($"{pagesUrl}?filter=title eq '{pageTitle}'");
            if (onenotePage == null || onenotePage.Count == 0)
            {
                var successFlag = await CreateOneNotePage(pagesUrl, pageTitle);
                if (successFlag)
                {
                    var retryCount = 3;
                    while ((onenotePage == null || onenotePage.Count == 0) && retryCount > 0)
                    {
                        onenotePage = await GetOneNotePage($"{pagesUrl}?filter=title eq '{pageTitle}'");
                        retryCount--;
                    }
                }
                else
                {
                    throw new Exception("Can not create the To Do OneNote page.");
                }
            }

            if (onenotePage == null || onenotePage.Count == 0)
            {
                throw new Exception("Can not get the To Do OneNote page.");
            }
            else
            {
                return onenotePage[0].Id;
            }
        }

        private async Task<List<OnenotePage>> GetOneNotePage(string url)
        {
            return JsonConvert.DeserializeObject<List<OnenotePage>>(await ExecuteGraphFetch(url));

        }

        private async Task<List<OnenotePage>> GetOneNotePageById(string pageId)
        {
            var pageByIdUrl = $"{graphBaseUrl}/onenote/pages?filter=id eq '{pageId}'";
            return await GetOneNotePage(pageByIdUrl);
        }

        private async Task<List<ToDoItem>> GetToDos(string pageContentUrl)
        {
            var todoContent = await httpClient.GetStringAsync(pageContentUrl + "?includeIDs=true");
            var doc = new XmlDocument();
            doc.LoadXml(todoContent);
            XmlNode root = doc.DocumentElement;

            var todosList = root.SelectSingleNode("body")
                ?.SelectSingleNode("div")
                ?.SelectNodes("p")
                ?.Cast<XmlNode>()
                ?.Where(node => node.Attributes["data-tag"] != null && node.Attributes["data-tag"].Value.StartsWith("to-do"))
                ?.Select(node => new ToDoItem() { Topic = node.InnerText, Id = node.Attributes["id"].Value, IsCompleted = node.Attributes["data-tag"].Value == "to-do" ? false : true })
                ?.ToList();

            if (todosList == null)
            {
                todosList = new List<ToDoItem>();
            }

            return todosList;
        }

        private async Task<string> ExecuteGraphFetch(string url)
        {
            var result = await httpClient.GetStringAsync(url);
            dynamic content = JObject.Parse(result);
            return JsonConvert.SerializeObject((object)content.value);
        }
    }
}

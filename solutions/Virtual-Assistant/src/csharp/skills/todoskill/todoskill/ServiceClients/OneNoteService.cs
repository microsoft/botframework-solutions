// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License.

namespace ToDoSkill.ServiceClients
{
    using System.Collections.Generic;
    using System.Linq;
    using System.Net.Http;
    using System.Threading.Tasks;
    using System.Xml;
    using global::ToDoSkill.Dialogs.Shared.Resources;
    using global::ToDoSkill.Models;
    using Microsoft.Graph;
    using Newtonsoft.Json;
    using Newtonsoft.Json.Linq;

    /// <summary>
    /// To Do bot service.
    /// </summary>
    public class OneNoteService : ITaskService
    {
        private readonly string graphBaseUrl = "https://graph.microsoft.com/v1.0/me";
        private HttpClient httpClient;
        private Dictionary<string, string> pageIds;

        /// <summary>
        /// Init task service.
        /// </summary>
        /// <param name="token">Task service token.</param>
        /// <param name="pageIds">OneNote page name and id dictionary.</param>
        /// <param name="client">the httpclient for making the API request.</param>
        /// <returns>Task service itself.</returns>
        public async Task<ITaskService> InitAsync(string token, Dictionary<string, string> pageIds, HttpClient client = null)
        {
            try
            {
                if (client == null)
                {
                    httpClient = ServiceHelper.GetHttpClient(token);
                }
                else
                {
                    httpClient = client;
                }

                if (!pageIds.ContainsKey(ToDoStrings.ToDo)
                    || !pageIds.ContainsKey(ToDoStrings.Grocery)
                    || !pageIds.ContainsKey(ToDoStrings.Shopping))
                {
                    var notebookId = await GetOrCreateNotebookAsync(ToDoStrings.OneNoteBookName);
                    var sectionId = await GetOrCreateSectionAsync(notebookId, ToDoStrings.OneNoteSectionName);

                    if (!pageIds.ContainsKey(ToDoStrings.ToDo))
                    {
                        var toDoPageId = await GetOrCreatePageAsync(sectionId, ToDoStrings.ToDo);
                        pageIds.Add(ToDoStrings.ToDo, toDoPageId);
                    }

                    if (!pageIds.ContainsKey(ToDoStrings.Grocery))
                    {
                        var groceryPageId = await GetOrCreatePageAsync(sectionId, ToDoStrings.Grocery);
                        pageIds.Add(ToDoStrings.Grocery, groceryPageId);
                    }

                    if (!pageIds.ContainsKey(ToDoStrings.Shopping))
                    {
                        var shoppingPageId = await GetOrCreatePageAsync(sectionId, ToDoStrings.Shopping);
                        pageIds.Add(ToDoStrings.Shopping, shoppingPageId);
                    }
                }

                this.pageIds = pageIds;
                return this;
            }
            catch (ServiceException ex)
            {
                throw ServiceHelper.HandleGraphAPIException(ex);
            }
        }

        /// <summary>
        /// Get To Do tasks.
        /// </summary>
        /// <param name="listType">Task list type.</param>
        /// <returns>List of task items.</returns>
        public async Task<List<TaskItem>> GetTasksAsync(string listType)
        {
            try
            {
                var pages = await GetOneNotePageByIdAsync(pageIds[listType]);

                var retryCount = 2;
                while ((pages == null || pages.Count == 0) && retryCount > 0)
                {
                    pages = await GetOneNotePageByIdAsync(pageIds[listType]);
                    retryCount--;
                }

                var todos = await GetToDoContentAsync(pages.First().ContentUrl);
                return todos;
            }
            catch (ServiceException ex)
            {
                throw ServiceHelper.HandleGraphAPIException(ex);
            }
        }

        /// <summary>
        /// Add a task.
        /// </summary>
        /// <param name="listType">Task list type.</param>
        /// <param name="taskText">The task text.</param>
        /// <returns>Ture if succeed.</returns>
        public async Task<bool> AddTaskAsync(string listType, string taskText)
        {
            try
            {
                var pageContentUrl = await this.GetDefaultToDoPageAsync(listType);
                var todoContent = await ExecuteContentFetchAsync(pageContentUrl.ContentUrl + "/?includeIDs=true");
                var httpRequestMessage = ServiceHelper.GenerateAddToDoHttpRequest(taskText, todoContent, pageContentUrl.ContentUrl);
                var result = await ExecuteSendAsync(httpRequestMessage);
                return result;
            }
            catch (ServiceException ex)
            {
                throw ServiceHelper.HandleGraphAPIException(ex);
            }
        }

        /// <summary>
        /// Mark tasks as completed.
        /// </summary>
        /// <param name="listType">Task list type.</param>
        /// <param name="taskItems">Task items.</param>
        /// <returns>True if succeed.</returns>
        public async Task<bool> MarkTasksCompletedAsync(string listType, List<TaskItem> taskItems)
        {
            try
            {
                var pageContentUrl = await this.GetDefaultToDoPageAsync(listType);
                var httpRequestMessage = ServiceHelper.GenerateMarkToDosHttpRequest(taskItems, pageContentUrl.ContentUrl);
                var result = await ExecuteSendAsync(httpRequestMessage);
                return result;
            }
            catch (ServiceException ex)
            {
                throw ServiceHelper.HandleGraphAPIException(ex);
            }
        }

        /// <summary>
        /// Delete tasks.
        /// </summary>
        /// <param name="listType">Task list type.</param>
        /// <param name="taskItems">Task items.</param>
        /// <returns>True if succeed.</returns>
        public async Task<bool> DeleteTasksAsync(string listType, List<TaskItem> taskItems)
        {
            try
            {
                var pageContentUrl = await this.GetDefaultToDoPageAsync(listType);
                var httpRequestMessage = ServiceHelper.GenerateDeleteToDosHttpRequest(taskItems, pageContentUrl.ContentUrl);
                var result = await ExecuteSendAsync(httpRequestMessage);
                return result;
            }
            catch (ServiceException ex)
            {
                throw ServiceHelper.HandleGraphAPIException(ex);
            }
        }

        /// <summary>
        /// Get task web link.
        /// </summary>
        /// <returns>Task web link.</returns>
        public async Task<string> GetTaskWebLink()
        {
            var notebooksUrl = $"{graphBaseUrl}/onenote/notebooks";
            var onenoteNotebook = await GetOneNoteNotebookAsync($"{notebooksUrl}?filter=name eq '{ToDoStrings.OneNoteBookName}'");
            return onenoteNotebook?[0]?.Links?.OneNoteWebUrl?.Href;
        }

        private async Task<string> CreateOneNoteNotebookAsync(string createNotebookUrl, string notebookName)
        {
            var makeSectionContent = await ExecuteContentFetchAsync(createNotebookUrl);
            var httpRequestMessage = ServiceHelper.GenerateCreateNotebookHttpRequest(makeSectionContent, createNotebookUrl, notebookName);
            var result = await httpClient.SendAsync(httpRequestMessage);
            dynamic responseContent = JObject.Parse(await result.Content.ReadAsStringAsync());

            if (result.IsSuccessStatusCode)
            {
                return (string)responseContent.id;
            }
            else
            {
                ServiceException serviceException = ServiceHelper.GenerateServiceException(responseContent);
                throw serviceException;
            }
        }

        private async Task<string> GetOrCreateNotebookAsync(string notebookName)
        {
            var notebooksUrl = $"{graphBaseUrl}/onenote/notebooks";
            var onenoteNotebook = await GetOneNoteNotebookAsync($"{notebooksUrl}?filter=name eq '{notebookName}'");
            if (onenoteNotebook.Count == 0)
            {
                return await CreateOneNoteNotebookAsync(notebooksUrl, notebookName);
            }

            return onenoteNotebook[0].Id;
        }

        private async Task<List<Notebook>> GetOneNoteNotebookAsync(string url)
        {
            return JsonConvert.DeserializeObject<List<Notebook>>(await ExecuteGraphFetchAsync(url));
        }

        private async Task<string> CreateOneNoteSectionAsync(string sectionContentUrl, string sectionTitle)
        {
            var makeSectionContent = await ExecuteContentFetchAsync(sectionContentUrl);
            var httpRequestMessage = ServiceHelper.GenerateCreateSectionHttpRequest(makeSectionContent, sectionContentUrl, sectionTitle);
            var result = await httpClient.SendAsync(httpRequestMessage);
            dynamic responseContent = JObject.Parse(await result.Content.ReadAsStringAsync());

            if (result.IsSuccessStatusCode)
            {
                return (string)responseContent.id;
            }
            else
            {
                ServiceException serviceException = ServiceHelper.GenerateServiceException(responseContent);
                throw serviceException;
            }
        }

        private async Task<string> GetOrCreateSectionAsync(string notebookId, string sectionTitle)
        {
            var sectionsUrl = $"{graphBaseUrl}/onenote/notebooks/{notebookId}/sections";
            var onenoteSection = await GetOneNoteSectionAsync($"{sectionsUrl}?filter=name eq '{sectionTitle}'");
            if (onenoteSection.Count == 0)
            {
                return await CreateOneNoteSectionAsync(sectionsUrl, sectionTitle);
            }

            return onenoteSection[0].Id;
        }

        private async Task<List<OnenoteSection>> GetOneNoteSectionAsync(string url)
        {
            return JsonConvert.DeserializeObject<List<OnenoteSection>>(await ExecuteGraphFetchAsync(url));
        }

        private async Task<bool> CreateOneNotePageAsync(string sectionUrl, string pageTitle)
        {
            var httpRequestMessage = ServiceHelper.GenerateCreatePageHttpRequest(pageTitle, sectionUrl);
            var result = await ExecuteSendAsync(httpRequestMessage);
            return result;
        }

        private async Task<string> GetOrCreatePageAsync(string sectionId, string pageTitle)
        {
            var pagesUrl = $"{graphBaseUrl}/onenote/sections/{sectionId}/pages";
            var onenotePage = await GetOneNotePageAsync($"{pagesUrl}?filter=title eq '{pageTitle}'");
            if (onenotePage == null || onenotePage.Count == 0)
            {
                var successFlag = await CreateOneNotePageAsync(pagesUrl, pageTitle);
                if (successFlag)
                {
                    var retryCount = 3;
                    while ((onenotePage == null || onenotePage.Count == 0) && retryCount > 0)
                    {
                        onenotePage = await GetOneNotePageAsync($"{pagesUrl}?filter=title eq '{pageTitle}'");
                        retryCount--;
                    }
                }
            }

            return onenotePage[0].Id;
        }

        private async Task<List<OnenotePage>> GetOneNotePageAsync(string url)
        {
            return JsonConvert.DeserializeObject<List<OnenotePage>>(await ExecuteGraphFetchAsync(url));
        }

        private async Task<List<OnenotePage>> GetOneNotePageByIdAsync(string pageId)
        {
            var pageByIdUrl = $"{graphBaseUrl}/onenote/pages?filter=id eq '{pageId}'";
            return await GetOneNotePageAsync(pageByIdUrl);
        }

        private async Task<List<TaskItem>> GetToDoContentAsync(string pageContentUrl)
        {
            var todoContent = await ExecuteContentFetchAsync(pageContentUrl + "?includeIDs=true");
            var doc = new XmlDocument();
            doc.LoadXml(todoContent);
            XmlNode root = doc.DocumentElement;

            var todosList = root.SelectSingleNode("body")
                ?.SelectSingleNode("div")
                ?.SelectNodes("p")
                ?.Cast<XmlNode>()
                ?.Where(node => node.Attributes["data-tag"] != null && node.Attributes["data-tag"].Value.StartsWith("to-do"))
                ?.Select(node => new TaskItem() { Topic = node.InnerText, Id = node.Attributes["id"].Value, IsCompleted = node.Attributes["data-tag"].Value == "to-do" ? false : true })
                ?.ToList();

            if (todosList == null)
            {
                todosList = new List<TaskItem>();
            }
            else
            {
                todosList.RemoveAll(t => t.IsCompleted);
            }

            return todosList;
        }

        private async Task<OnenotePage> GetDefaultToDoPageAsync(string listType)
        {
            var pages = await GetOneNotePageByIdAsync(pageIds[listType]);
            var retryCount = 2;
            while ((pages == null || pages.Count == 0) && retryCount > 0)
            {
                pages = await GetOneNotePageByIdAsync(pageIds[listType]);
                retryCount--;
            }

            return pages.First();
        }

        private async Task<string> ExecuteGraphFetchAsync(string url)
        {
            var result = await this.httpClient.GetAsync(url);
            dynamic responseContent = JObject.Parse(await result.Content.ReadAsStringAsync());
            if (result.IsSuccessStatusCode)
            {
                return JsonConvert.SerializeObject((object)responseContent.value);
            }
            else
            {
                ServiceException serviceException = ServiceHelper.GenerateServiceException(responseContent);
                throw serviceException;
            }
        }

        private async Task<dynamic> ExecuteContentFetchAsync(string url)
        {
            var result = await httpClient.GetAsync(url);
            var responseContent = await result.Content.ReadAsStringAsync();
            if (result.IsSuccessStatusCode)
            {
                return responseContent;
            }
            else
            {
                ServiceException serviceException = ServiceHelper.GenerateServiceException(responseContent);
                throw serviceException;
            }
        }

        private async Task<bool> ExecuteSendAsync(HttpRequestMessage request)
        {
            var result = await httpClient.SendAsync(request);
            if (result.IsSuccessStatusCode)
            {
                return true;
            }
            else
            {
                dynamic responseContent = JObject.Parse(await result.Content.ReadAsStringAsync());
                ServiceException serviceException = ServiceHelper.GenerateServiceException(responseContent);
                throw serviceException;
            }
        }
    }
}
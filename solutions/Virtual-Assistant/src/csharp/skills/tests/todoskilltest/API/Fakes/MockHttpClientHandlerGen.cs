using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ToDoSkill;

namespace ToDoSkillTest.API.Fakes
{
    public class MockHttpClientHandlerGen
    {
        private readonly HttpClientHandler httpClientHandler;
        private List<TaskItem> todos;

        /// <summary>
        /// Initializes a new instance of the <see cref="MockHttpClientHandlerGen"/> class.
        /// </summary>
        public MockHttpClientHandlerGen()
        {
            this.httpClientHandler = this.GenerateMockHttpClientHandler();
            this.todos = new List<TaskItem>()
            {
                new TaskItem()
                {
                    Id = "1",
                    Topic = "Task 1",
                    IsCompleted = false,
                },
                new TaskItem()
                {
                    Id = "2",
                    Topic = "Task 2",
                    IsCompleted = false,
                },
                new TaskItem()
                {
                    Id = "3",
                    Topic = "Task 3",
                    IsCompleted = false,
                },
                new TaskItem()
                {
                    Id = "4",
                    Topic = "Task 4",
                    IsCompleted = false,
                },
            };
        }

        public HttpClientHandler GetMockHttpClientHandler()
        {
            return this.httpClientHandler;
        }

        private HttpClientHandler GenerateMockHttpClientHandler()
        {
            var mockClient = new Mock<HttpClientHandler>(MockBehavior.Strict);
            this.SetHttpMockBehavior(ref mockClient);
            return mockClient.Object;
        }

        private void SetHttpMockBehavior(ref Mock<HttpClientHandler> mockClient)
        {
            mockClient
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
               "SendAsync",
               ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://graph.microsoft.com/v1.0/me/onenote/pages?filter=id")),
               ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(() => new HttpResponseMessage()
               {
                   Content = new StringContent(this.GetPageDetails()),
               });

            mockClient
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://graph.microsoft.com/v1.0/users/bnoabotletdev@outlook.com/onenote/pages/") && r.Method != HttpMethod.Patch),
                ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() => new HttpResponseMessage()
                {
                    Content = new StringContent(this.GetPageContent()),
                });

            mockClient
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://graph.microsoft.com/v1.0/users/bnoabotletdev@outlook.com/onenote/pages/") && r.Method == HttpMethod.Patch),
                ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() => new HttpResponseMessage()
                {
                    Content = new StringContent(string.Empty),
                })
                .Callback<HttpRequestMessage, CancellationToken>(async (r, c) => await this.HandlePatch(r));

            mockClient
              .Protected()
              .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://graph.microsoft.com/v1.0/me/onenote/notebooks?filter=name")),
              ItExpr.IsAny<CancellationToken>())
              .ReturnsAsync(() => new HttpResponseMessage()
              {
                  Content = new StringContent(this.GetPageDetails()),
              });
        }

        private string GetTodoHtml(TaskItem task)
        {
            var taskStatus = task.IsCompleted ? ":completed" : string.Empty;
            return $"<p id=\"{task.Id}\" data-tag=\"to-do{taskStatus}\" style=\"margin-top:0pt;margin-bottom:0pt\">{task.Topic}</p>";
        }

        private string GetTodosHtml()
        {
            var res = "\r\n";
            this.todos.ForEach(todo =>
            {
                res += $"{this.GetTodoHtml(todo)}\r\n";
            });
            return res;
        }

        private async Task<bool> HandlePatch(HttpRequestMessage request)
        {
            var result = await request.Content.ReadAsStringAsync();
            var objects = JsonConvert.DeserializeObject<List<object>>(result);
            if (objects == null || objects.Count == 0)
            {
                return false;
            }

            var req = JObject.Parse(objects[0].ToString());
            if (req["action"].ToString() == "append")
            {
                this.AddTask(req["content"].ToString());
            }
            else if (req["action"].ToString() == "replace")
            {
                var doc = new XmlDocument();
                doc.LoadXml(req["content"].ToString());
                var targetId = req["target"].ToString();
                if (doc.InnerText.Length == 0)
                {
                    // Remove the current task
                    this.RemoveTask(targetId);
                }
                else
                {
                    // Mark the current task as complete
                    this.MarkTask(targetId);
                }
            }

            return true;
        }

        private void MarkTask(string id)
        {
            this.todos[this.todos.FindIndex(s => s.Id == id)].IsCompleted = true;
        }

        private void RemoveTask(string id)
        {
            this.todos.RemoveAt(this.todos.FindIndex(i => i.Id == id));
        }

        private void AddTask(string taskContent)
        {
            var doc = new XmlDocument();
            doc.LoadXml(taskContent);
            this.todos.Insert(0, new TaskItem()
            {
                Id = this.todos.Count.ToString(),
                Topic = doc.InnerText,
            });
        }

        private string GetPageContent()
        {
            return $"<html lang=\"en-US\">\r\n<head>\r\n<title>ToDo</title>\r\n<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\" />\r\n<meta name=\"created\" content=\"2018-11-07T14:52:00.0000000\" />\r\n</head>\r\n<body data-absolute-enabled=\"true\" style=\"font-family:Calibri;font-size:11pt\">\r\n<div id=\"div:TEST\" data-id=\"_default\" style=\"position:absolute;left:48px;top:120px;width:624px\">{GetTodosHtml()}</div>\r\n</body>\r\n</html>";
        }

        private string GetPageDetails()
        {
            return "{\"@odata.context\":\"https://graph.microsoft.com/v1.0/$metadata#users('bnoabotletdev%40outlook.com')/onenote/pages\",\"value\":[{\"id\":\"0-6683264ee61f47bc9e089900e12ed192!215-A2624DB91264DE33!152\",\"self\":\"https://graph.microsoft.com/v1.0/users/bnoabotletdev@outlook.com/onenote/pages/0-6683264ee61f47bc9e089900e12ed192!215-A2624DB91264DE33!152\",\"createdDateTime\":\"2018-11-07T14:52:00Z\",\"title\":\"ToDo\",\"createdByAppId\":\"WLID-00000000482345AD\",\"contentUrl\":\"https://graph.microsoft.com/v1.0/users/bnoabotletdev@outlook.com/onenote/pages/0-6683264ee61f47bc9e089900e12ed192!215-A2624DB91264DE33!152/content\",\"lastModifiedDateTime\":\"2018-11-07T14:52:00Z\",\"links\":{\"oneNoteClientUrl\":{\"href\":\"onenote:https://d.docs.live.net/a2624db91264de33/%e6%96%87%e6%a1%a3/ToDoNotebook/ToDoSection.one#ToDo&section-id=c2d2e1a9-1690-469e-bae9-e4a27c6c1aff&page-id=66fe82e8-596f-44e3-9cf7-d82a92a6158a&end\"},\"oneNoteWebUrl\":{\"href\":\"https://onedrive.live.com/redir.aspx?cid=a2624db91264de33&page=edit&resid=A2624DB91264DE33!150&parId=A2624DB91264DE33!106&wd=target%28ToDoSection.one%7Cc2d2e1a9-1690-469e-bae9-e4a27c6c1aff%2FToDo%7C66fe82e8-596f-44e3-9cf7-d82a92a6158a%2F%29\"}},\"parentSection@odata.context\":\"https://graph.microsoft.com/v1.0/$metadata#users('bnoabotletdev%40outlook.com')/onenote/pages('0-6683264ee61f47bc9e089900e12ed192%21215-A2624DB91264DE33%21152')/parentSection/$entity\",\"parentSection\":{\"id\":\"0-A2624DB91264DE33!152\",\"displayName\":\"ToDoSection\",\"self\":\"https://graph.microsoft.com/v1.0/users/bnoabotletdev@outlook.com/onenote/sections/0-A2624DB91264DE33!152\"}}]}";
        }
    }
}

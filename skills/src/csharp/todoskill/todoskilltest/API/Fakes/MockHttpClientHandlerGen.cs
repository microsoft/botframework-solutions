using System.Collections.Generic;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Moq;
using Moq.Protected;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using ToDoSkill.Models;

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
                    IsCompleted = true,
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
               MockData.SendAsync,
               ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://graph.microsoft.com/v1.0/me/onenote/pages?filter=id")),
               ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(() => new HttpResponseMessage()
               {
                   Content = new StringContent(this.GetPageDetails()),
               });

            mockClient
               .Protected()
               .Setup<Task<HttpResponseMessage>>(
               MockData.SendAsync,
               ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://graph.microsoft.com/v1.0/me/onenote/sections/testid/pages?filter=title")),
               ItExpr.IsAny<CancellationToken>())
               .ReturnsAsync(() => new HttpResponseMessage()
               {
                   Content = new StringContent(this.GetPageDetails()),
               });

            mockClient
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                MockData.SendAsync,
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://graph.microsoft.com/v1.0/users/test@outlook.com/onenote/pages/") && r.Method != HttpMethod.Patch),
                ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() => new HttpResponseMessage()
                {
                    Content = new StringContent(this.GetPageContent()),
                });

            mockClient
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                MockData.SendAsync,
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://graph.microsoft.com/v1.0/users/test@outlook.com/onenote/pages/") && r.Method == HttpMethod.Patch),
                ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() => new HttpResponseMessage()
                {
                    Content = new StringContent(string.Empty),
                })
                .Callback<HttpRequestMessage, CancellationToken>(async (r, c) => await this.HandlePatch(r));

            mockClient
              .Protected()
              .Setup<Task<HttpResponseMessage>>(
              MockData.SendAsync,
              ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://graph.microsoft.com/v1.0/me/onenote/notebooks?filter=name")),
              ItExpr.IsAny<CancellationToken>())
              .ReturnsAsync(() => new HttpResponseMessage()
              {
                  Content = new StringContent(this.GetPageDetails()),
              });

            mockClient
              .Protected()
              .Setup<Task<HttpResponseMessage>>(
              MockData.SendAsync,
              ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://graph.microsoft.com/v1.0/me/onenote/notebooks/testid/sections?filter=name")),
              ItExpr.IsAny<CancellationToken>())
              .ReturnsAsync(() => new HttpResponseMessage()
              {
                  Content = new StringContent(this.GetPageDetails()),
              });

            mockClient
              .Protected()
              .Setup<Task<HttpResponseMessage>>(
              MockData.SendAsync,
              ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://graph.microsoft.com/v1.0/me/onenote/notebooks/testid/sections?filter=title")),
              ItExpr.IsAny<CancellationToken>())
              .ReturnsAsync(() => new HttpResponseMessage()
              {
                  Content = new StringContent(this.GetPageDetails()),
              });

            mockClient
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                MockData.SendAsync,
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://graph.microsoft.com/beta/me/outlook/taskFolders/To Do/tasks") && r.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() => new HttpResponseMessage()
                {
                    Content = new StringContent(this.GetOutlookTasks()),
                });

            mockClient
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                MockData.SendAsync,
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().Equals("https://graph.microsoft.com/beta/me/outlook/taskFolders") && r.Method == HttpMethod.Get),
                ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() => new HttpResponseMessage()
                {
                    Content = new StringContent(this.GetOutlookTaskFolders()),
                });

            mockClient
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                MockData.SendAsync,
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://graph.microsoft.com/beta/me/outlook/taskFolders/To Do/tasks") && r.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() => new HttpResponseMessage()
                {
                    Content = new StringContent(string.Empty),
                })
                .Callback<HttpRequestMessage, CancellationToken>(async (r, c) => await this.AddOutlookTaskAsync(r));

            mockClient
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                MockData.SendAsync,
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://graph.microsoft.com/beta/me/outlook/tasks") && r.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() => new HttpResponseMessage()
                {
                    Content = new StringContent(string.Empty),
                })
                .Callback<HttpRequestMessage, CancellationToken>((r, c) => this.MarkOrDeleteOutlookTask(r));

            mockClient
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                MockData.SendAsync,
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://graph.microsoft.com/beta/me/outlook/tasks") && r.Method == HttpMethod.Delete),
                ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() => new HttpResponseMessage()
                {
                    Content = new StringContent(string.Empty),
                })
                .Callback<HttpRequestMessage, CancellationToken>((r, c) => this.MarkOrDeleteOutlookTask(r));

            mockClient
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                MockData.SendAsync,
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://graph.microsoft.com/beta/me/outlook/taskFolders/Shopping/tasks") && r.Method == HttpMethod.Post),
                ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() => new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.Unauthorized,
                    Content = new StringContent(this.GenerateServiceExceptionResponse()),
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
            return "{\"@odata.context\":\"https://graph.microsoft.com/v1.0/$metadata#users('test%40outlook.com')/onenote/pages\",\"value\":[{\"id\":\"testid\",\"self\":\"https://graph.microsoft.com/v1.0/users/test@outlook.com/onenote/pages/testpageid\",\"createdDateTime\":\"2018-11-07T14:52:00Z\",\"title\":\"ToDo\",\"createdByAppId\":\"testappid\",\"contentUrl\":\"https://graph.microsoft.com/v1.0/users/test@outlook.com/onenote/pages/testpageid/content\",\"lastModifiedDateTime\":\"2018-11-07T14:52:00Z\",\"links\":{\"oneNoteClientUrl\":{\"href\":\"onenote:https://d.docs.live.net/\"},\"oneNoteWebUrl\":{\"href\":\"https://onedrive.live.com/\"}},\"parentSection@odata.context\":\"https://graph.microsoft.com/v1.0/$metadata#users('test%40outlook.com')/onenote/pages('testid')/parentSection/$entity\",\"parentSection\":{\"id\":\"testid\",\"displayName\":\"TestSection\",\"self\":\"https://graph.microsoft.com/v1.0\"}}]}";
        }

        private string GetOutlookTasks()
        {
            var taskObjects = new List<object>();
            foreach (var task in this.todos)
            {
                taskObjects.Add(new
                {
                    subject = task.Topic,
                    id = task.Id,
                    status = task.IsCompleted ? "completed" : "uncompleted",
                });
            }

            var taskResponseDetails = new JObject();
            taskResponseDetails.Add("@odata.context", "https://graph.microsoft.com/beta/outlook/taskFolders/tasks");
            taskResponseDetails.Add("value", JToken.FromObject(taskObjects));

            return JsonConvert.SerializeObject(taskResponseDetails);
        }

        private string GetOutlookTaskFolders()
        {
            var taskObjects = new List<object>();
            taskObjects.Add(new
            {
                id = MockData.ToDo,
                name = MockData.ToDo
            });

            taskObjects.Add(new
            {
                id = MockData.Shopping,
                name = MockData.Shopping
            });

            taskObjects.Add(new
            {
                id = MockData.Grocery,
                name = MockData.Grocery
            });

            var taskResponseDetails = new JObject();
            taskResponseDetails.Add("@odata.context", "https://graph.microsoft.com/beta/outlook/taskFolders");
            taskResponseDetails.Add("value", JToken.FromObject(taskObjects));

            return JsonConvert.SerializeObject(taskResponseDetails);
        }

        private async Task<bool> AddOutlookTaskAsync(HttpRequestMessage request)
        {
            var result = await request.Content.ReadAsStringAsync();
            var reqObj = JsonConvert.DeserializeObject<object>(result);
            if (reqObj == null)
            {
                return false;
            }

            var req = JObject.Parse(reqObj.ToString());
            var taskContent = req["subject"].ToString();

            this.todos.Insert(0, new TaskItem()
            {
                Id = "0",
                Topic = taskContent,
                IsCompleted = false,
            });

            return true;
        }

        private bool MarkOrDeleteOutlookTask(HttpRequestMessage request)
        {
            var url = request.RequestUri.ToString();
            if (request.Method == HttpMethod.Post)
            {
                var subUrl = url.Remove(url.LastIndexOf('/'));
                var id = subUrl.Substring(subUrl.LastIndexOf('/') + 1);
                this.todos[this.todos.FindIndex(s => s.Id == id)].IsCompleted = true;
            }
            else
            {
                var id = url.Substring(url.LastIndexOf('/') + 1);
                this.todos.RemoveAt(this.todos.FindIndex(i => i.Id == id));
            }

            return true;
        }

        private string GenerateServiceExceptionResponse()
        {
            return "{ \"error\": { \"code\": \"ErrorAccessDenied\", \"message\": \"Access is denied. Check credentials and try again.\", \"innerError\": { \"request-id\": \"fcfed3fd-2c0a-4278-b55a-5b156af772dd\", \"date\": \"2018-12-14T06:34:54\" } } }";
        }
    }
}
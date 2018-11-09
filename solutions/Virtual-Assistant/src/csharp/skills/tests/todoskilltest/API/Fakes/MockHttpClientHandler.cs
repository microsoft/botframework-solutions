using System;
using System.Collections.Generic;
using System.Text;
using System.Net.Http;
using System.Threading.Tasks;
using Moq;
using Moq.Protected;
using System.Threading;
using ToDoSkill;
using Newtonsoft.Json.Linq;
using System.Xml;
using Newtonsoft.Json;
using System.Linq;

namespace ToDoSkillTest.API.Fakes
{
    class MockHttpClient
    {
        private static List<TaskItem> todos = new List<TaskItem>()
        {
            new TaskItem()
            {
                Id = "1",
                Topic = "Task 1",
                IsCompleted = false
            },
            new TaskItem()
            {
                Id = "2",
                Topic = "Task 2",
                IsCompleted = false
            },
            new TaskItem()
            {
                Id = "3",
                Topic = "Task 3",
                IsCompleted = false
            },
            new TaskItem()
            {
                Id = "4",
                Topic = "Task 4",
                IsCompleted = false
            }
        };

        public static string GetTodoHtml(TaskItem task)
        {
            if (!task.IsCompleted)
            {
                return $"<p id=\"{task.Id}\" data-tag=\"to-do\" style=\"margin-top:0pt;margin-bottom:0pt\">{task.Topic}</p>";
            }
            else
            {
                return $"<p id=\"{task.Id}\" data-tag=\"to-do:completed\" style=\"margin-top:0pt;margin-bottom:0pt\">{task.Topic}</p>";
            }
            
        }

        public static string GetTodosHtml()
        {
            var res = "\r\n";
            todos.ForEach(todo =>
            {
                res += $"{GetTodoHtml(todo)}\r\n";
            });
            return res;
        }

        public static HttpClientHandler getMockHttpClient()
        {
            var mockClient = new Mock<HttpClientHandler>(MockBehavior.Strict);

            mockClient
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://graph.microsoft.com/v1.0/me/onenote/pages?filter=id")),
                ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() => new HttpResponseMessage()
                {
                    Content = new StringContent(GetPageDetails())
                });

            mockClient
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://graph.microsoft.com/v1.0/users/bnoabotletdev@outlook.com/onenote/pages/") && r.Method != HttpMethod.Patch),
                ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() => new HttpResponseMessage()
                {
                    Content = new StringContent(GetPageContent())
                });

            mockClient
                .Protected()
                .Setup<Task<HttpResponseMessage>>(
                "SendAsync",
                ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://graph.microsoft.com/v1.0/users/bnoabotletdev@outlook.com/onenote/pages/") && r.Method == HttpMethod.Patch),
                ItExpr.IsAny<CancellationToken>())
                .ReturnsAsync(() => new HttpResponseMessage()
                {
                    StatusCode = System.Net.HttpStatusCode.OK,
                    Content = new StringContent("")
                })
                .Callback<HttpRequestMessage, CancellationToken>(async (r, c) => await HandlePatch(r));

            mockClient
              .Protected()
              .Setup<Task<HttpResponseMessage>>(
              "SendAsync",
              ItExpr.Is<HttpRequestMessage>(r => r.RequestUri.ToString().StartsWith("https://graph.microsoft.com/v1.0/me/onenote/notebooks?filter=name")),
              ItExpr.IsAny<CancellationToken>())
              .ReturnsAsync(() => new HttpResponseMessage()
              {
                  Content = new StringContent(GetPageDetails())
              });
            return mockClient.Object;
        }
        static async Task<bool> HandlePatch(HttpRequestMessage r)
        {
            var result = await r.Content.ReadAsStringAsync();
            var objects = JsonConvert.DeserializeObject<List<object>>(result);
            var req = JObject.Parse(objects[0].ToString());
            if (req["action"].ToString() == "append")
            {
                AddTask(req["content"].ToString());
            }
            else if (req["action"].ToString() == "replace")
            {
                var doc = new XmlDocument();
                doc.LoadXml(req["content"].ToString());
                var targetId = req["target"].ToString();
                if (doc.InnerText.Length == 0)
                {
                    // Remove the current task
                    RemoveTask(targetId);
                }
                else
                {
                    // Mark the current task as complete
                    MarkTask(targetId);
                }
            }
            
            return true;
        }
        static void MarkTask(string id)
        {
            todos[todos.FindIndex(s => s.Id == id)].IsCompleted = true;
        }
        static void RemoveTask(string id)
        {
            todos.RemoveAt(todos.FindIndex(i => i.Id == id));
        }
        static void AddTask(string taskContent)
        {
            var doc = new XmlDocument();
            doc.LoadXml(taskContent);
           
            todos.Insert(0, new TaskItem()
            {
                Id = todos.Count.ToString(),
                Topic = doc.InnerText
            });
        }

        static string GetPageContent()
        {
            return $"<html lang=\"en-US\">\r\n<head>\r\n<title>ToDo</title>\r\n<meta http-equiv=\"Content-Type\" content=\"text/html; charset=utf-8\" />\r\n<meta name=\"created\" content=\"2018-11-07T14:52:00.0000000\" />\r\n</head>\r\n<body data-absolute-enabled=\"true\" style=\"font-family:Calibri;font-size:11pt\">\r\n<div id=\"div:TEST\" data-id=\"_default\" style=\"position:absolute;left:48px;top:120px;width:624px\">{GetTodosHtml()}</div>\r\n</body>\r\n</html>";
        }
        static string GetPageDetails()
        {
            return "{\"@odata.context\":\"https://graph.microsoft.com/v1.0/$metadata#users('bnoabotletdev%40outlook.com')/onenote/pages\",\"value\":[{\"id\":\"0-6683264ee61f47bc9e089900e12ed192!215-A2624DB91264DE33!152\",\"self\":\"https://graph.microsoft.com/v1.0/users/bnoabotletdev@outlook.com/onenote/pages/0-6683264ee61f47bc9e089900e12ed192!215-A2624DB91264DE33!152\",\"createdDateTime\":\"2018-11-07T14:52:00Z\",\"title\":\"ToDo\",\"createdByAppId\":\"WLID-00000000482345AD\",\"contentUrl\":\"https://graph.microsoft.com/v1.0/users/bnoabotletdev@outlook.com/onenote/pages/0-6683264ee61f47bc9e089900e12ed192!215-A2624DB91264DE33!152/content\",\"lastModifiedDateTime\":\"2018-11-07T14:52:00Z\",\"links\":{\"oneNoteClientUrl\":{\"href\":\"onenote:https://d.docs.live.net/a2624db91264de33/%e6%96%87%e6%a1%a3/ToDoNotebook/ToDoSection.one#ToDo&section-id=c2d2e1a9-1690-469e-bae9-e4a27c6c1aff&page-id=66fe82e8-596f-44e3-9cf7-d82a92a6158a&end\"},\"oneNoteWebUrl\":{\"href\":\"https://onedrive.live.com/redir.aspx?cid=a2624db91264de33&page=edit&resid=A2624DB91264DE33!150&parId=A2624DB91264DE33!106&wd=target%28ToDoSection.one%7Cc2d2e1a9-1690-469e-bae9-e4a27c6c1aff%2FToDo%7C66fe82e8-596f-44e3-9cf7-d82a92a6158a%2F%29\"}},\"parentSection@odata.context\":\"https://graph.microsoft.com/v1.0/$metadata#users('bnoabotletdev%40outlook.com')/onenote/pages('0-6683264ee61f47bc9e089900e12ed192%21215-A2624DB91264DE33%21152')/parentSection/$entity\",\"parentSection\":{\"id\":\"0-A2624DB91264DE33!152\",\"displayName\":\"ToDoSection\",\"self\":\"https://graph.microsoft.com/v1.0/users/bnoabotletdev@outlook.com/onenote/sections/0-A2624DB91264DE33!152\"}}]}";
        }
        
        

        
    }
}

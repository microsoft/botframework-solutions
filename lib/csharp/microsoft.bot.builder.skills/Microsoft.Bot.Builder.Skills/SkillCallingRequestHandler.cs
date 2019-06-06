using System;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills.Protocol;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Protocol;
using Microsoft.Bot.Schema;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Skills
{
    public class SkillCallingRequestHandler : RequestHandler
    {
        private readonly Router _router;
        private readonly ITurnContext _turnContext;
        private readonly Action<Activity> _tokenRequestHandler;
        private readonly Action<Activity> _handoffActivityHandler;

        public SkillCallingRequestHandler(ITurnContext turnContext, Action<Activity> tokenRequestHandler = null, Action<Activity> handoffActivityHandler = null)
        {
            _turnContext = turnContext ?? throw new ArgumentNullException(nameof(turnContext));
            _tokenRequestHandler = tokenRequestHandler;
            _handoffActivityHandler = handoffActivityHandler;

            var routes = new RouteTemplate[]
            {
                new RouteTemplate()
                {
                    Method = "POST",
                    Path = "/activities/{activityId}",
                    Action = new RouteAction()
                    {
                        Action =
                            async (request, routeData) =>
                            {
                                var activity = await request.ReadBodyAsJson<Activity>().ConfigureAwait(false);
                                if (activity != null)
                                {
                                    if (activity.Type == ActivityTypes.Event && activity.Name == TokenEvents.TokenRequestEventName)
                                    {
                                        if (_tokenRequestHandler != null)
                                        {
                                            _tokenRequestHandler(activity);

                                            return new ResourceResponse();
                                        }
                                        else
                                        {
                                            throw new ArgumentNullException("TokenRequestHandler", "Skill is requesting for token but there's no handler on the calling side!");
                                        }
                                    }
                                    else if (activity.Type == ActivityTypes.EndOfConversation)
                                    {
                                        if (_handoffActivityHandler != null)
                                        {
                                            _handoffActivityHandler(activity);

                                            return new ResourceResponse();
                                        }
                                        else
                                        {
                                            throw new ArgumentNullException("HandoffActivityHandler", "Skill is sending handoff activity but there's no handler on the calling side!");
                                        }
                                    }
                                    else
                                    {
                                        var result = await _turnContext.SendActivityAsync(activity).ConfigureAwait(false);
                                        return result;
                                    }
                                }
                                else
                                {
                                    throw new Exception("Error deserializing activity response!");
                                }
                            },
                    },
                },
                new RouteTemplate()
                {
                    Method = "PUT",
                    Path = "/activities/{activityId}",
                    Action = new RouteAction()
                    {
                        Action =
                            async (request, routeData) =>
                            {
                                var activity = await request.ReadBodyAsJson<Activity>().ConfigureAwait(false);
                                var result = _turnContext.UpdateActivityAsync(activity).ConfigureAwait(false);
                                return result;
                            },
                    },
                },
                new RouteTemplate()
                {
                    Method = "DELETE",
                    Path = "/activities/{activityId}",
                    Action = new RouteAction()
                    {
                        Action =
                            async (request, routeData) =>
                            {
                                var result = await _turnContext.DeleteActivityAsync(routeData.activityId);
                                return result;
                            },
                    },
                },
            };

            _router = new Router(routes);
        }

        public override async Task<Response> ProcessRequestAsync(ReceiveRequest request, object context = null, ILogger<RequestHandler> logger = null)
        {
            var routeContext = _router.Route(request);
            if (routeContext != null)
            {
                try
                {
                    var responseBody = await routeContext.Action.Action(request, routeContext.RouteData).ConfigureAwait(false);
                    return Response.OK(new StringContent(JsonConvert.SerializeObject(responseBody, SerializationSettings.DefaultSerializationSettings), Encoding.UTF8, SerializationSettings.ApplicationJson));
                }
                catch
                {
                    return Response.InternalServerError();
                }
            }
            else
            {
                return Response.NotFound();
            }
        }
    }
}
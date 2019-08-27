using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills.Models;
using Microsoft.Bot.Builder.Skills.Protocol;
using Microsoft.Bot.Builder.Solutions;
using Microsoft.Bot.Schema;
using Microsoft.Bot.StreamingExtensions;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;

namespace Microsoft.Bot.Builder.Skills
{
    public class SkillCallingRequestHandler : RequestHandler
    {
        private readonly Router _router;
        private readonly ITurnContext _turnContext;
        private readonly IBotTelemetryClient _botTelemetryClient;
        private readonly ISkillProtocolHandler _skillProtocolHandler;

        public SkillCallingRequestHandler(
            ITurnContext turnContext,
            IBotTelemetryClient botTelemetryClient,
            ISkillProtocolHandler skillProtocolHandler)
        {
            _turnContext = turnContext ?? throw new ArgumentNullException(nameof(turnContext));
            _botTelemetryClient = botTelemetryClient;
            _skillProtocolHandler = skillProtocolHandler;

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
                                var activity = request.ReadBodyAsJson<Activity>();
                                if (activity != null)
                                {
                                    if (activity.Type == ActivityTypes.Event && activity.Name == TokenEvents.TokenRequestEventName)
                                    {
                                        if (_skillProtocolHandler != null)
                                        {
                                            await _skillProtocolHandler.HandleTokenRequest(activity);

                                            return new ResourceResponse();
                                        }
                                        else
                                        {
                                            throw new ArgumentNullException("TokenRequestHandler", "Skill is requesting for token but there's no handler on the calling side!");
                                        }
                                    }
                                    else if (activity.Type == ActivityTypes.Event && activity.Name == SkillEvents.FallbackEventName)
                                    {
                                        if (_skillProtocolHandler != null)
                                        {
                                            await _skillProtocolHandler.HandleFallback(activity);

                                            return new ResourceResponse();
                                        }
                                        else
                                        {
                                            throw new ArgumentNullException("FallbackRequestHandler", "Skill is asking for fallback but there is no handler on the calling side!");
                                        }
                                    }
                                    else if (activity.Type == ActivityTypes.EndOfConversation)
                                    {
                                        var result = await _turnContext.SendActivityAsync(activity).ConfigureAwait(false);
                                        if (_skillProtocolHandler != null)
                                        {
                                            await _skillProtocolHandler.HandleEndOfConversation(activity);

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
                                var activity = request.ReadBodyAsJson<Activity>();
                                var result = await _turnContext.UpdateActivityAsync(activity).ConfigureAwait(false);
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
                                var result = await _turnContext.DeleteActivityAsync(routeData.activityId).ConfigureAwait(false);
                                return result;
                            },
                    },
                },
            };

            _router = new Router(routes);
        }

        public override async Task<StreamingResponse> ProcessRequestAsync(ReceiveRequest request, ILogger<RequestHandler> logger, object context = null, CancellationToken cancellationToken = default)
        {
            var routeContext = _router.Route(request);
            if (routeContext != null)
            {
                try
                {
                    var responseBody = await routeContext.Action.Action(request, routeContext.RouteData).ConfigureAwait(false);
                    return StreamingResponse.OK(new StringContent(JsonConvert.SerializeObject(responseBody, SerializationSettings.DefaultSerializationSettings), Encoding.UTF8, SerializationSettings.ApplicationJson));
                }
                catch (Exception ex)
                {
                    _botTelemetryClient.TrackException(ex);
                    return StreamingResponse.InternalServerError();
                }
            }
            else
            {
                return StreamingResponse.NotFound();
            }
        }
    }
}
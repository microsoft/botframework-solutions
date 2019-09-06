using System;
using System.Net;
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
        private readonly IBotTelemetryClient _botTelemetryClient;

        public SkillCallingRequestHandler(
            ITurnContext turnContext,
            IBotTelemetryClient botTelemetryClient,
            Action<Activity> tokenRequestHandler = null,
            Action<Activity> fallbackRequestHandler = null,
            Action<Activity> handoffActivityHandler = null)
        {
            if (turnContext == null)
            {
                throw new ArgumentNullException(nameof(turnContext));
            }

            _botTelemetryClient = botTelemetryClient;

            var routes = new[]
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
                                        if (tokenRequestHandler != null)
                                        {
                                            tokenRequestHandler(activity);
                                            return new ResourceResponse();
                                        }

                                        throw new ArgumentNullException(nameof(tokenRequestHandler), "Skill is requesting for token but there's no handler on the calling side!");
                                    }

                                    if (activity.Type == ActivityTypes.Event && activity.Name == SkillEvents.FallbackEventName)
                                    {
                                        if (fallbackRequestHandler != null)
                                        {
                                            fallbackRequestHandler(activity);
                                            return new ResourceResponse();
                                        }

                                        throw new ArgumentNullException(nameof(fallbackRequestHandler), "Skill is asking for fallback but there is no handler on the calling side!");
                                    }

                                    if (activity.Type == ActivityTypes.Handoff)
                                    {
                                        await turnContext.SendActivityAsync(activity).ConfigureAwait(false);
                                        if (handoffActivityHandler != null)
                                        {
                                            handoffActivityHandler(activity);
                                            return new ResourceResponse();
                                        }

                                        throw new ArgumentNullException(nameof(handoffActivityHandler), "Skill is sending handoff activity but there's no handler on the calling side!");
                                    }

                                    var result = await turnContext.SendActivityAsync(activity).ConfigureAwait(false);
                                    return result;
                                }

                                throw new Exception("Error deserializing activity response!");
                            },
                    },
                },
                new RouteTemplate
                {
                    Method = "PUT",
                    Path = "/activities/{activityId}",
                    Action = new RouteAction()
                    {
                        Action =
                            async (request, routeData) =>
                            {
                                var activity = request.ReadBodyAsJson<Activity>();
                                var result = await turnContext.UpdateActivityAsync(activity).ConfigureAwait(false);
                                return result;
                            },
                    },
                },
                new RouteTemplate
                {
                    Method = "DELETE",
                    Path = "/activities/{activityId}",
                    Action = new RouteAction()
                    {
                        Action =
                            async (request, routeData) =>
                            {
                                var result = await turnContext.DeleteActivityAsync(routeData.activityId).ConfigureAwait(false);
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
                    var response = new StreamingResponse { StatusCode = (int)HttpStatusCode.OK };
                    string content = JsonConvert.SerializeObject(responseBody, SerializationSettings.DefaultSerializationSettings);
                    response.SetBody(content);
                    return response;
                }
#pragma warning disable CA1031 // Do not catch general exception types (disable, using exception data to populate the response.
                catch (Exception ex)
#pragma warning restore CA1031 // Do not catch general exception types
                {
                    _botTelemetryClient.TrackException(ex);
                    var response = new StreamingResponse { StatusCode = (int)HttpStatusCode.InternalServerError };
                    var content = JsonConvert.SerializeObject(ex, SerializationSettings.DefaultSerializationSettings);
                    response.SetBody(content);
                    return response;
                }
            }

            return StreamingResponse.NotFound();
        }
    }
}

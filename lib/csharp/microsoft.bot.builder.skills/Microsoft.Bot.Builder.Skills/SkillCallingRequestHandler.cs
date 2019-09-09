using System;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Builder.Skills.Protocol;
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
        private readonly ISkillHandoffResponseHandler _skillHandoffResponseHandler;
        private readonly ISkillResponseHandler _skillResponseHandler;

        public SkillCallingRequestHandler(
            IBotTelemetryClient botTelemetryClient,
            ISkillHandoffResponseHandler skillHandoffResponseHandler,
            ISkillResponseHandler skillResponseHandler)
        {
            _botTelemetryClient = botTelemetryClient;
            _skillResponseHandler = skillResponseHandler;
            _skillHandoffResponseHandler = skillHandoffResponseHandler ?? throw new ArgumentNullException(nameof(skillHandoffResponseHandler));

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
                                    if (activity.Type == ActivityTypes.Handoff)
                                    {
                                        _skillHandoffResponseHandler.HandleHandoffResponse(activity);
                                    }

                                    if (_skillResponseHandler != null)
                                    {
                                        return await _skillResponseHandler.SendActivityAsync(activity);
                                    }
                                    else
                                    {
                                        return new ResourceResponse();
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
                                if (_skillResponseHandler != null)
                                {
                                    return await _skillResponseHandler.UpdateActivityAsync(activity);
                                }
                                else
                                {
                                    return new ResourceResponse();
                                }
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
                                if (_skillResponseHandler != null)
                                {
                                    return await _skillResponseHandler.DeleteActivityAsync(routeData.activityId);
                                }
                                else
                                {
                                    return new ResourceResponse();
                                }
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
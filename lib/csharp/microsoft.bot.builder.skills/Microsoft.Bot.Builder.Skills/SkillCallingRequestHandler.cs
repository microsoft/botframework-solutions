using System;
using System.Net;
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
                                    if (activity.Type == ActivityTypes.Handoff)
                                    {
                                        _skillHandoffResponseHandler.HandleHandoffResponse(activity);
                                    }

                                    if (_skillResponseHandler != null)
                                    {
                                        return await _skillResponseHandler.SendActivityAsync(activity).ConfigureAwait(false);
                                    }
                                    else
                                    {
                                        return new ResourceResponse();
                                    }
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
                                if (_skillResponseHandler != null)
                                {
                                    return await _skillResponseHandler.UpdateActivityAsync(activity).ConfigureAwait(false);
                                }
                                else
                                {
                                    return new ResourceResponse();
                                }
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

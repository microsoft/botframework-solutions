using System;
using System.Collections.Specialized;
using System.Threading;
using System.Threading.Tasks;
using EmailSkill.Dialogs.DailyBrief.Resources;
using EmailSkill.Dialogs.Shared;
using EmailSkill.Model;
using EmailSkill.Proactive;
using EmailSkill.ServiceClients;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Dialogs;
using Microsoft.Bot.Builder.Solutions.Proactive;
using Microsoft.Bot.Builder.Solutions.Responses;
using Microsoft.Bot.Builder.Solutions.Skills;
using Microsoft.Bot.Builder.Solutions.TaskExtensions;
using Microsoft.Bot.Builder.Solutions.Util;
using Microsoft.Bot.Configuration;
using static EmailSkill.Proactive.DailyBriefEventHandler;

namespace EmailSkill.Dialogs.DailyBrief
{
    public class DailyBriefDialog : EmailSkillDialog
    {
        private IBackgroundTaskQueue _backgroundTaskQueue;
        private IStatePropertyAccessor<ProactiveModel> _proactiveStateAccessor;
        private EndpointService _endpointService;
        private ResponseManager _responseManager;
        private ScheduledTask _scheduledTask;

        public DailyBriefDialog(
            SkillConfigurationBase services,
            EndpointService endpointService,
            ResponseManager responseManager,
            IStatePropertyAccessor<EmailSkillState> emailStateAccessor,
            IStatePropertyAccessor<DialogState> dialogStateAccessor,
            IStatePropertyAccessor<ProactiveModel> proactiveStateAccessor,
            IServiceManager serviceManager,
            IBotTelemetryClient telemetryClient,
            IBackgroundTaskQueue backgroundTaskQueue,
            ScheduledTask scheduledTask)
            : base(nameof(DailyBriefDialog), services, responseManager, emailStateAccessor, dialogStateAccessor, serviceManager, telemetryClient)
        {
            TelemetryClient = telemetryClient;

            _backgroundTaskQueue = backgroundTaskQueue;
            _proactiveStateAccessor = proactiveStateAccessor;
            _endpointService = endpointService;
            _responseManager = responseManager;
            _scheduledTask = scheduledTask;

            var emaildailyBrief = new WaterfallStep[]
            {
                GetAuthToken,
                AfterGetAuthToken,
                QueueDailyBriefWorker
            };

            // Define the conversation flow using a waterfall model.
            AddDialog(new WaterfallDialog(Actions.DailyBrief, emaildailyBrief));

            // Set starting dialog for component
            InitialDialogId = Actions.DailyBrief;
        }

        public async Task<DialogTurnResult> QueueDailyBriefWorker(WaterfallStepContext sc, CancellationToken cancellationToken = default(CancellationToken))
        {
            try
            {
                var state = await EmailStateAccessor.GetAsync(sc.Context);

                if (!string.IsNullOrWhiteSpace(state.Token))
                {
                    var activity = sc.Context.Activity;
                    var userId = activity.From.Id;

                    var proactiveState = await _proactiveStateAccessor.GetAsync(sc.Context, () => new ProactiveModel());
                    var emailService = ServiceManager.InitMailService(state.Token, state.GetUserTimeZone(), state.MailSourceType);

                    //_backgroundTaskQueue.QueueBackgroundWorkItem(async (token) =>
                    //{
                    //    var handler = new DailyBriefEventHandler
                    //    {
                    //        EmailService = emailService
                    //    };
                    //    await handler.Handle(DailyBriefEventCallback(userId, sc, proactiveState));
                    //});
                    //var scheduledTask = new ScheduledTask(_backgroundTaskQueue);

                    if (_scheduledTask != null)
                    {
                        var scheduledTaskModel = new ScheduledTaskModel()
                        {
                            Name = "EmailDailyBrief",
                            ScheduleExpression = "*/2 17 * * *",
                            Task = async (token) =>
                            {
                                var handler = new DailyBriefEventHandler
                                {
                                    EmailService = emailService
                                };
                                await handler.Handle(DailyBriefEventCallback(userId, sc, proactiveState));
                            },
                            CancellationToken = cancellationToken
                        };
                        _scheduledTask.AddScheduledTask(scheduledTaskModel);
                    }
                }

                return EndOfTurn;
            }
            catch (Exception ex)
            {
                await HandleDialogExceptions(sc, ex);
                throw;
            }
        }

        private DailyBriefEventCallback DailyBriefEventCallback(string userId, WaterfallStepContext sc, ProactiveModel proactiveModel)
        {
            return async (overview, cancellationToken) =>
            {
                await sc.Context.Adapter.ContinueConversationAsync(_endpointService.AppId, proactiveModel[MD5Util.ComputeHash(userId)].Conversation, DailyBriefContinueConversationCallback(overview, sc), cancellationToken);
            };
        }

        // Creates the turn logic to use for the proactive message.
        private BotCallbackHandler DailyBriefContinueConversationCallback(EmailOverview overview, WaterfallStepContext sc)
        {
            sc.EndDialogAsync(); // ?

            return async (turnContext, token) =>
            {
                var responseString = DailyBriefResponses.EmailDailyBriefMessage;
                var responseParams = new StringDictionary()
                {
                    { "EmailTotalCount", overview.TotalEmailCount.ToString() },
                };

                var activity = turnContext.Activity.CreateReply();
                var response = _responseManager.GetResponse(responseString, responseParams);
                activity.Text = response.Text;
                activity.Speak = response.Speak;
                activity.InputHint = response.InputHint;
                activity.SuggestedActions = response.SuggestedActions;
                activity.DeliveryMode = CommonUtil.DeliveryModeProactive;
                await turnContext.SendActivityAsync(activity);
            };
        }
    }
}

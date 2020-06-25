using System.Threading;
using System.Threading.Tasks;
using System.Xml.Linq;
using Microsoft.Bot.Builder;
using Microsoft.Bot.Builder.Adapters;
using Microsoft.Bot.Schema;
using Microsoft.Bot.Solutions.Feedback;
using Microsoft.Bot.Solutions.Middleware;
using Microsoft.Bot.Solutions.Proactive;
using Microsoft.Bot.Solutions.TaskExtensions;
using Microsoft.Bot.Solutions.Util;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Solutions.Tests.TaskExtensionTests
{
    [TestClass]
    [TestCategory("UnitTests")]
    public class QueueHostedServiceTests
    {
        [TestMethod]
        public async Task Verify_Hosted_Service_Executes_Task()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddHostedService<QueuedHostedService>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            var serviceProvider = services.BuildServiceProvider();

            var service = serviceProvider.GetService<IHostedService>() as QueuedHostedService;

            var backgroundQueue = serviceProvider.GetService<IBackgroundTaskQueue>();

            await service.StartAsync(CancellationToken.None);

            var isExecuted = false;
            backgroundQueue.QueueBackgroundWorkItem(async cancellationToken =>
            {
                await Task.FromResult<bool>(isExecuted = true);
            });

            await Task.Delay(10000);
            Assert.IsTrue(isExecuted);

            await service.StopAsync(CancellationToken.None);
        }
    }
}

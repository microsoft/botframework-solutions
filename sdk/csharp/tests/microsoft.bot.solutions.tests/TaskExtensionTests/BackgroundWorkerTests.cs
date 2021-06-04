using System;
using System.Diagnostics.CodeAnalysis;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Bot.Solutions.TaskExtensions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.Bot.Solutions.Tests.TaskExtensionTests
{
    [TestClass]
    [TestCategory("UnitTests")]
    [ExcludeFromCodeCoverageAttribute]
    public class BackgroundWorkerTests
    {
        [TestMethod]
        public async Task Verify_Hosted_Service_Executes_Test()
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

        [TestMethod]
        public void Scheduled_Task_Null_Test()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddHostedService<QueuedHostedService>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            var serviceProvider = services.BuildServiceProvider();

            var service = serviceProvider.GetService<IHostedService>() as QueuedHostedService;

            var backgroundQueue = serviceProvider.GetService<IBackgroundTaskQueue>();

            ScheduledTask testScheduledTask = new ScheduledTask(backgroundQueue);

            var ex = Assert.ThrowsException<ArgumentNullException>(() => testScheduledTask.AddScheduledTask(null));

            Assert.IsTrue(ex.Message.Contains("ScheduledTask cannot be null!"));
        }

        [TestMethod]
        public void Scheduled_Task_Null_Expression_Test()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddHostedService<QueuedHostedService>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            var serviceProvider = services.BuildServiceProvider();

            var service = serviceProvider.GetService<IHostedService>() as QueuedHostedService;

            var backgroundQueue = serviceProvider.GetService<IBackgroundTaskQueue>();

            var taskModel = new ScheduledTaskModel();

            ScheduledTask testScheduledTask = new ScheduledTask(backgroundQueue);

            var ex = Assert.ThrowsException<ArgumentException>(() => testScheduledTask.AddScheduledTask(taskModel));

            Assert.IsTrue(ex.Message.Contains("ScheduledTask has to have a schedule expression!"));
        }

        [TestMethod]
        public void Scheduled_Task_Null_TaskModel_Task_Test()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddHostedService<QueuedHostedService>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            var serviceProvider = services.BuildServiceProvider();

            var service = serviceProvider.GetService<IHostedService>() as QueuedHostedService;

            var backgroundQueue = serviceProvider.GetService<IBackgroundTaskQueue>();

            var taskModel = new ScheduledTaskModel();
            taskModel.ScheduleExpression = "0 5 31 2 *";

            ScheduledTask testScheduledTask = new ScheduledTask(backgroundQueue);

            var ex = Assert.ThrowsException<ArgumentException>(() => testScheduledTask.AddScheduledTask(taskModel));

            Assert.IsTrue(ex.Message.Contains("ScheduledTask has to have a task"));
        }

        [TestMethod]
        public void Scheduled_Task_Test()
        {
            IServiceCollection services = new ServiceCollection();

            services.AddHostedService<QueuedHostedService>();
            services.AddSingleton<IBackgroundTaskQueue, BackgroundTaskQueue>();
            var serviceProvider = services.BuildServiceProvider();

            var service = serviceProvider.GetService<IHostedService>() as QueuedHostedService;

            var backgroundQueue = serviceProvider.GetService<IBackgroundTaskQueue>();

            var taskModel = new ScheduledTaskModel();
            taskModel.ScheduleExpression = "0 5 31 2 *";

            ScheduledTask testScheduledTask = new ScheduledTask(backgroundQueue);

            var ex = Assert.ThrowsException<ArgumentException>(() => testScheduledTask.AddScheduledTask(taskModel));

            Assert.IsTrue(ex.Message.Contains("ScheduledTask has to have a task"));
        }
    }
}

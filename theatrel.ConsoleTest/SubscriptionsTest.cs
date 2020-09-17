using System.Threading;
using System.Threading.Tasks;
using Autofac;
using Moq;
using theatrel.Interfaces.Subscriptions;
using theatrel.Subscriptions;
using theatrel.TLBot.Interfaces;

namespace theatrel.ConsoleTest
{
    internal class SubscriptionsTest
    {
        public static async Task Test()
        {
            await using var scope = Setup();
            var processor = scope.Resolve<ISubscriptionProcessor>();
            await processor.ProcessSubscriptions();
        }

        private static ILifetimeScope Setup()
        {
            Mock<ITgBotService> telegramService = new Mock<ITgBotService>();

            telegramService.Setup(x => x.SendMessageAsync(It.IsAny<long>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .Returns(() => Task.FromResult(true));

            return Bootstrapper.RootScope.BeginLifetimeScope(builder =>
            {
                builder.RegisterInstance(telegramService.Object).As<ITgBotService>().AsImplementedInterfaces().SingleInstance();
                builder.RegisterModule<SubscriptionModule>();
            });
        }
    }
}

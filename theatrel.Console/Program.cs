using System;
using System.Linq;
using theatrel.Console;
using theatrel.Interfaces;
using System.Diagnostics;
using Telegram.Bot;
using System.Collections.Generic;
using System.Collections.Concurrent;
using Telegram.Bot.Args;
using System.Threading.Tasks;
using System.Threading;
using theatrel.TLBot.Interfaces;

namespace TheatrelConsole
{
    class Program
    {
        private static IPlayBillDataResolver _playBillResolver = Bootstrapper.Resolve<IPlayBillDataResolver>();

        private IDictionary<long, string> _chatsInfo = new ConcurrentDictionary<long, string>();
        private static ITLBotProcessor _tLBotProcessor;

        static void Main(string[] args)
        {
            Bootstrapper.Start();
            _tLBotProcessor = Bootstrapper.Resolve<ITLBotProcessor>();

            var tsk = Task.Run(() => { while (true) { } });
            Task.WaitAll(tsk);

            Bootstrapper.Stop();
        }

        private static IPerformanceFilter GetFilter()
        {
            IPerformanceFilter filter = Bootstrapper.Resolve<IPerformanceFilter>();

            //filter.Locations = new[] { "Мариинский театр", "Мариинский театр 2" };
            filter.DaysOfWeek = new[] { DayOfWeek.Saturday, DayOfWeek.Sunday };
            filter.PerfomanceTypes = new[] { "Опера", "Балет" };

            return filter;
        }
    }
}

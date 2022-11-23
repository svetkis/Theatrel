using System;
using System.Collections.Generic;
using System.Text;
using theatrel.DataAccess.DbService;
using theatrel.Interfaces.TgBot;

namespace theatrel.TLBot.Commands
{
    internal class SubscriptionEntry
    {
        public int PlaybillEntryId;
        public DateTime When;
        public string Name;
    }

    internal static class SubscriptionsHelper
    {
        public static SubscriptionEntry[] ParseSubscriptionsCommandLine(IChatDataInfo chatInfo, string commandLine, IDbService dbService, StringBuilder sb = null)
        {
            if (string.IsNullOrEmpty(chatInfo.Info))
                return Array.Empty<SubscriptionEntry>();

            string[] performanceIds = chatInfo.Info.Split(',');

            List<SubscriptionEntry> entriesList = new List<SubscriptionEntry>();
            string[] parsedIndexes = commandLine
                .Split(new char[] { ' ', ',' }, StringSplitOptions.RemoveEmptyEntries);

            using var playbillRepository = dbService.GetPlaybillRepository();

            foreach (string indexString in parsedIndexes)
            {
                if (!int.TryParse(indexString, out int index) || performanceIds.Length < index || index < 1)
                {
                    sb?.AppendLine($"Ошибка парсинга {indexString}");
                    continue;
                }

                var entityId = int.Parse(performanceIds[index - 1]);
                var pbEntity = playbillRepository.GetPlaybillEntryWithPerformanceData(entityId);

                if (string.IsNullOrEmpty(pbEntity.Performance.Name))
                {
                    sb?.AppendLine("Не найден спектакль");
                    continue;
                }

                entriesList.Add(new SubscriptionEntry
                {
                    PlaybillEntryId = entityId,
                    Name = pbEntity?.Performance.Name,
                    When = pbEntity?.When ?? DateTime.UtcNow
                });
            }

            return entriesList.ToArray();
        }
    }
}

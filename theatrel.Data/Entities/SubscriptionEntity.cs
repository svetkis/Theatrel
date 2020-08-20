using System;

namespace theatrel.DataAccess.Entities
{
    public class SubscriptionEntity
    {
        public int Id { get; set; }

        public long TelegramUserId { get; set; }
        public TelegramUserEntity TelegramUser { get; set; }

        public DateTime LastUpdate { get; set; }

        public int TrackingChanges { get; set; }

        public int PerformanceFilterId { get; set; }
        public PerformanceFilterEntity PerformanceFilter { get; set; }
    }
}

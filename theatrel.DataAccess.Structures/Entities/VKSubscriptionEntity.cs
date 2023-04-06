using System;

namespace theatrel.DataAccess.Structures.Entities;

public class VkSubscriptionEntity
{
    public int Id { get; set; }

    public int SubscriptionType { get; set; }

    public DateTime LastUpdate { get; set; }

    public int TrackingChanges { get; set; }

    public int PerformanceFilterId { get; set; }

    public PerformanceFilterEntity PerformanceFilter { get; set; }

    public long VkId { get; set; }
}
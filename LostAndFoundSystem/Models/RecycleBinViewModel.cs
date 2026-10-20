namespace LostAndFoundSystem.Models
{
    // One row in the Recycle Bin (either a deleted item or a deleted notification)
    public class RecycleBinEntry
    {
        public string Kind { get; set; } = "Item";          // "Item" or "Notification"
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string Details { get; set; } = string.Empty;
        public string Badge { get; set; } = string.Empty;    // Lost / Found / Claimed, or notification type
        public string? ImageUrl { get; set; }
        public DateTime DeletedAt { get; set; }
        public string DeletedBy { get; set; } = "—";
        public int DaysLeft { get; set; }
    }

    public class RecycleBinViewModel
    {
        public List<RecycleBinEntry> Entries { get; set; } = new();

        // Current filters
        public string Type { get; set; } = "All";            // All, Items, Notifications
        public string? Search { get; set; }
        public string Range { get; set; } = "Any";           // Any, Today, 7, 30

        // Counts for the filter tabs (after search + date range are applied)
        public int ItemsCount { get; set; }
        public int NotificationsCount { get; set; }
        public int TotalCount => ItemsCount + NotificationsCount;

        public int RetentionDays { get; set; } = 30;
    }
}
namespace roadwork_portal_service.Model
{
    public class SessionDto
    {
        public DateTime plannedDate { get; set; }
        public long sksNo { get; set; }
        public string acceptance1 { get; set; } = "";
        public string attachments { get; set; } = "";
        public string miscItems { get; set; } = "";
        public string? presentUserIds { get; set; }
        public string? distributionUserIds { get; set; }
        public string errorMessage { get; set; } = "";
    }

    public sealed class UpdateSessionUsersDto
    {
        // CSV strings, e.g. "1,2,3" or emails
        public string? presentUserIds { get; set; }
        public string? distributionUserIds { get; set; }
    }

    public sealed class UpdateSessionDetailsDto
    {
        public string? attachments { get; set; }
        public string? acceptance1 { get; set; }
        public string? miscItems { get; set; }
    }

}

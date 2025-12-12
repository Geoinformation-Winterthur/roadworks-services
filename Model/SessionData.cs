namespace roadwork_portal_service.Model
{
    public class SessionDto
    {
        public DateTime plannedDate { get; set; }
        public long sksNo { get; set; }
        public string reportType { get; set; } = "";
        public string dateType { get; set; } = "";
        public string acceptance1 { get; set; } = "";
        public string attachments { get; set; } = "";
        public string miscItems { get; set; } = "";
        public string? presentUserIds { get; set; }
        public string? distributionUserIds { get; set; }
        public string errorMessage { get; set; } = "";
    }

    public sealed class UpdateSessionUsersDto
    {        
        public string? presentUserIds { get; set; }
        public string? distributionUserIds { get; set; }
    }

    public sealed class UpdateSessionDetailsDto
    {        
        public long sksNo { get; set; }
        public string? plannedDateForBackend { get; set; }
        public string? reportType { get; set; }
        public string? attachments { get; set; }
        public string? acceptance1 { get; set; }
        public string? miscItems { get; set; }
    }

    public sealed class CreateSessionDto
    {
        public long sksNo { get; set; }
        public string? plannedDateForBackend { get; set; }      
        public string? acceptance1 { get; set; }
        public string? attachments { get; set; }
        public string? miscItems { get; set; }
        public string? presentUserIds { get; set; }    
        public string? distributionUserIds { get; set; }
    }

}

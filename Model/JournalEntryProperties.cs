namespace roadwork_portal_service.Model
{
    public class JournalEntryProperties
    {
        public string uuid { get; set; } = "";
        public string uuidRoadworkActivity { get; set; } = "";
        public string content { get; set; } = "";
        public DateTime? created { get; set; }
        public DateTime? lastModified { get; set; }
        internal string createdBy { get; set; } = ""; // internal >> exclude in API response
        public string createdByName { get; set; } = "";
        public bool isEditingAllowed { get; set; } = false;
    }
}

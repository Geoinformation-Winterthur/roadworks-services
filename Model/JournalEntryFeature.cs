namespace roadwork_portal_service.Model
{
    public class JournalEntryFeature
    {
        public string type { get; set; } = "JournalEntryFeature";
        public JournalEntryProperties properties { get; set; } = new JournalEntryProperties();
        public string errorMessage { get; set; } = "";
    }
}

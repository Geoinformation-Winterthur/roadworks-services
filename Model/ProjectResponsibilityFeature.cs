namespace roadwork_portal_service.Model
{
    public class ActivityResponsibilityFeature
    {
        public string type { get; set; } = "ActivityResponsibilityFeature";
        public ActivityResponsibilityProperties properties { get; set; } = new ActivityResponsibilityProperties();
        public string errorMessage { get; set; } = "";
    }
}

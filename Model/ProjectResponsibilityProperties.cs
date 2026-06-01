using System.Text.Json.Serialization;

namespace roadwork_portal_service.Model
{
    public class ActivityResponsibilityProperties
    {
        public string uuid { get; set; } = "";
        public string uuidRoadworkActivity { get; set; } = "";
        public string uuidOrganisationalUnit { get; set; } = "";
        public string uuidUser { get; set; } = "";
        public ResponsibilityType? responsibilityType { get; set; }
        public string phase { get; set; } = "";
        public short sortOrder { get; set; } = 0;
    }

    [JsonConverter(typeof(JsonStringEnumConverter))]
    public enum ResponsibilityType
    {
        ProjectLead,
        PhaseLead
    }
}

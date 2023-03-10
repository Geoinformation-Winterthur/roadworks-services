namespace roadwork_portal_service.Model;

public class ManagementAreaFeature
{
    public string type { get; set; } = "Feature";
    public string uuid { get; set; } = "";
    public string managername { get; set; } = "";
    public Geometry geometry { get; set; } = new Geometry();
}


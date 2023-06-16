// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model;

public class ManagementAreaFeature
{
    public string type { get; set; } = "";
    public ManagementAreaProperties properties { get; set; }
    public RoadworkPolygon geometry { get; set; }
    public string errorMessage { get; set; } = "";

    public ManagementAreaFeature()
    {
        this.type = "ManagementAreaFeature";
        this.properties = new ManagementAreaProperties();
        this.geometry = new RoadworkPolygon();
    }
}


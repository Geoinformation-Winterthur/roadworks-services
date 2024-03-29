// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model;

public class RoadWorkNeedFeature
{
    public string type { get; set; }
    public RoadWorkNeedProperties properties { get; set; }
    public RoadworkPolygon geometry { get; set; }
    public string errorMessage { get; set; }

    public RoadWorkNeedFeature()
    {
        this.type = "RoadWorkNeedFeature";
        this.properties = new RoadWorkNeedProperties();
        this.geometry = new RoadworkPolygon();
        this.errorMessage = "";
    }
}


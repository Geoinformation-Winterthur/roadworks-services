// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model;

public class RoadWorkActivityFeature
{
    public string type { get; set; }
    public RoadWorkActivityProperties properties { get; set; }
    public RoadworkPolygon geometry { get; set; }
    public string errorMessage { get; set; } = "";

    public RoadWorkActivityFeature()
    {
        this.type = "RoadWorkActivityFeature";
        this.properties = new RoadWorkActivityProperties();
        this.geometry = new RoadworkPolygon();
    }
}


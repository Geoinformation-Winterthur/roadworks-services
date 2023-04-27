// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model;

public class EventFeature
{
    public string type { get; set; }
    public EventProperties properties { get; set; }
    public RoadworkPolygon geometry { get; set; }
    public string errorMessage { get; set; }

    public EventFeature()
    {
        this.type = "EventFeature";
        this.properties = new EventProperties();
        this.geometry = new RoadworkPolygon();
        this.errorMessage = "";
    }
}


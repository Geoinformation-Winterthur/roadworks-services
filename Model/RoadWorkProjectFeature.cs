// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model;

using NetTopologySuite.Geometries;

public class RoadWorkProjectFeature
{
    public string type { get; set; }
    public RoadWorkProjectProperties properties { get; set; }
    public Polygon geometry { get; set; }

    public RoadWorkProjectFeature()
    {
        this.type = "RoadWorkProjectFeature";
        this.properties = new RoadWorkProjectProperties();
        this.geometry = Polygon.Empty;
    }
}


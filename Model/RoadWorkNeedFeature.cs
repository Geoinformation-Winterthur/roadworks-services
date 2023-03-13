// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model;

using NetTopologySuite.Geometries;

public class RoadWorkNeedFeature
{
    public string type { get; set; }
    public RoadWorkNeedProperties properties { get; set; }
    public Polygon geometry { get; set; }

    public RoadWorkNeedFeature()
    {
        this.type = "RoadWorkProjectFeature";
        this.properties = new RoadWorkNeedProperties();
        this.geometry = Polygon.Empty;
    }
}


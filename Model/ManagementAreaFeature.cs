// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model;

using NetTopologySuite.Geometries;

public class ManagementAreaFeature
{
    public string type { get; set; } = "";
    public ManagementAreaProperties properties { get; set; }
    public Polygon geometry { get; set; }

    public ManagementAreaFeature()
    {
        this.type = "ManagementAreaFeature";
        this.properties = new ManagementAreaProperties();
        this.geometry = Polygon.Empty;
    }
}


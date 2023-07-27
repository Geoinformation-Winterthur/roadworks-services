// <copyright company="Geoinformation Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Geoinformation Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model
{
    public class ConstructionSiteFeature
    {
        public string type { get; set; }
        public ConstructionSiteProperties properties { get; set; }
        public RoadworkPolygon geometry { get; set; } = new RoadworkPolygon();
        public string errorMessage { get; set; } = "";

        public ConstructionSiteFeature()
        {
            this.type = "Feature";
            this.properties = new ConstructionSiteProperties();
        }
    }
}

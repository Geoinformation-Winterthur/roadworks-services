using System;

namespace roadwork_portal_service.Model
{
    public class RoadWorkProjectFeature
    {
        public string type { get; set; } = "";
        public RoadWorkProjectProperties properties { get; set; }
        public Geometry geometry { get; set; } = new Geometry();

        public RoadWorkProjectFeature()
        {
            this.type = "Feature";
            this.properties = new RoadWorkProjectProperties();
        }
    }
}

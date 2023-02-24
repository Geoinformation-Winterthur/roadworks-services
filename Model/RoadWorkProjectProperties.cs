
using System;

namespace roadwork_portal_service.Model
{
    public class RoadWorkProjectProperties
    {
        public int id { get; set; } = -1;
        public string place { get; set; } = "";
        public string area { get; set; } = "";
        public string project { get; set; } = "";
        public int projectNo { get; set; } = -1;
        public string status { get; set; } = "not_coordinated";
        public string priority { get; set; } = "high";
        public DateTime realizationUntil { get; set; } = DateTime.Now;
        public bool active { get; set; } = false;
        public string trafficObstructionType { get; set; } = "";
        public RoadWorkProjectPart[] roadWorkProjectParts { get; set; } = new RoadWorkProjectPart[0];
    }
}

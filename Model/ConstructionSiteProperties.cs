// <copyright company="Geoinformation Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Geoinformation Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model
{
    public class ConstructionSiteProperties
    {
        public string uuid { get; set; } = "";
        public string name { get; set; } = "";
        public string description { get; set; } = "";
        public DateTime created { get; set; } = DateTime.MinValue;
        public DateTime lastModified { get; set; } = DateTime.MinValue;
        public DateTime dateFrom { get; set; } = DateTime.MinValue;
        public DateTime dateTo { get; set; } = DateTime.MinValue;
    }
}

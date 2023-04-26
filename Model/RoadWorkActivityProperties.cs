// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model;

public class RoadWorkActivityProperties
{
    public string uuid { get; set; } = "";
    public string name { get; set; } = "";
    public ManagementAreaFeature managementarea
                { get; set; } = new ManagementAreaFeature();
    public User projectManager { get; set; } = new User();
    public User trafficAgent { get; set; } = new User();
    public string comment { get; set; } = "";
    public DateTime created { get; set; } = DateTime.MinValue;
    public DateTime lastModified { get; set; } = DateTime.MinValue;
    public DateTime finishFrom { get; set; } = DateTime.MinValue;
    public DateTime finishTo { get; set; } = DateTime.MinValue;
    public decimal costs { get; set; } = 0m;
    public CostTypes costsType { get; set; } = new CostTypes();

}


// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model;

public class RoadWorkNeedProperties
{
    public string uuid { get; set; } = "";
    public string name { get; set; } = "";
    public string kind { get; set; } = "";
    public User orderer { get; set; } = new User();
    public DateTime finishEarlyFrom { get; set; } = DateTime.MinValue;
    public DateTime finishEarlyTo { get; set; } = DateTime.MinValue;
    public DateTime finishOptimumFrom { get; set; } = DateTime.MinValue;
    public DateTime finishOptimumTo { get; set; } = DateTime.MinValue;
    public DateTime finishLateFrom { get; set; } = DateTime.MinValue;
    public DateTime finishLateTo { get; set; } = DateTime.MinValue;
    public Priority priority { get; set; } = new Priority();
    public Status status { get; set; } = new Status();
    public string comment { get; set; } = "";
    public ManagementAreaFeature managementarea
                { get; set; } = new ManagementAreaFeature();
}

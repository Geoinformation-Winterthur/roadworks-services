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
    public string ordererUuid { get; set; } = "";
    public DateTime finishEarlyFrom { get; set; } = DateTime.MinValue;
    public DateTime finishEarlyTo { get; set; } = DateTime.MinValue;
    public DateTime finishOptimumFrom { get; set; } = DateTime.MinValue;
    public DateTime finishOptimumTo { get; set; } = DateTime.MinValue;
    public DateTime finishLateFrom { get; set; } = DateTime.MinValue;
    public DateTime finishLateTo { get; set; } = DateTime.MinValue;
    public string priorityUuid { get; set; } = "";
    public string statusUuid { get; set; } = "";
    public string comment { get; set; } = "";
    public string managementareaUuid { get; set; } = "";
}


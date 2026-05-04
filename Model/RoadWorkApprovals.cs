// <copyright company="Stadt Winterthur">
//      Author: Simon Meyer (GEOBOX AG)
//      Copyright (c) Stadt Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model;

public class RoadWorkApprovals
{
    public string? uuid { get; set; } = "";
    public string? uuidRoadworkActivity { get; set; } = "";
    public string? uuidRoadworkNeed { get; set; } = "";
    public bool? approvalRequired { get; set; } = false;
    public bool? strgApprovalRequired { get; set; } = false;
    public bool? bafuApprovalRequired { get; set; } = false;
    public bool? lsvApprovalRequired { get; set; } = false;
    public bool? ssvApprovalRequired { get; set; } = false;
    public bool? wwgApprovalRequired { get; set; } = false;
    public bool? eriApprovalRequired { get; set; } = false;
    public bool? pbgApprovalRequired { get; set; } = false;
    public bool? ebgApprovalRequired { get; set; } = false;
    public bool? awelApprovalRequired { get; set; } = false;
    public bool? estiApprovalRequired { get; set; } = false;
    public bool? otherApprovalRequired { get; set; } = false;
    public string? otherApprovalDetails { get; set; } = "";

    public string? NeedName { get; set; } = "";
    public string? NeedOrganisation { get; set; } = "";
}

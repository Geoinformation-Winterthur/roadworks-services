// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model;

public class RoadWorkActivityProperties
{
    public string uuid { get; set; } = "";
    public string name { get; set; } = "";
    public User projectManager { get; set; } = new User();
    public User trafficAgent { get; set; } = new User();
    public string description { get; set; } = "";
    public DateTime created { get; set; } = DateTime.MinValue;
    public DateTime lastModified { get; set; } = DateTime.MinValue;
    public DateTime finishFrom { get; set; } = DateTime.MinValue;
    public DateTime finishTo { get; set; } = DateTime.MinValue;
    public decimal costs { get; set; } = 0m;
    public CostType costsType { get; set; } = new CostType();

    public string[] roadWorkNeedsUuids { get; set; } = new string[0];
    public Status status { get; set; } = new Status();
    public bool isEditingAllowed { get; set; } = false;
    public bool isInInternet {get; set; } = false;
    public string billingAddress1 { get; set; } = "";
    public string billingAddress2 { get; set; } = "";
    public int investmentNo { get; set; } = 0;
    public int pdbFid { get; set; } = 0;
    public string strabakoNo { get; set; } = "";
}

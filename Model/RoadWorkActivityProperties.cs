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
    public string projectNo { get; set; } = "";
    public string comment { get; set; } = "";
    public string section { get; set; } = "";
    public string type { get; set; } = "";
    public string projectType { get; set; } = "";
    public bool overarchingMeasure { get; set; } = false;
    public int desiredYearFrom { get; set; } = -1;
    public int desiredYearTo { get; set; } = -1;
    public bool prestudy { get; set; } = false;
    public DateTime created { get; set; } = DateTime.MinValue;
    public DateTime lastModified { get; set; } = DateTime.MinValue;
    public DateTime finishFrom { get; set; } = DateTime.MinValue;
    public DateTime finishTo { get; set; } = DateTime.MinValue;
    public DateTime startOfConstruction { get; set; } = DateTime.MinValue;
    public DateTime endOfConstruction { get; set; } = DateTime.MinValue;
    public DateTime consultDue { get; set; } = DateTime.MinValue;
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
    public string dateSks { get; set; } = "";
    public string dateKap { get; set; } = "";
    public string dateOks { get; set; } = "";
    public DateTime dateGlTba { get; set; } = DateTime.MinValue;
    public ActivityHistoryItem[] activityHistory { get; set; } = new ActivityHistoryItem[0];
}

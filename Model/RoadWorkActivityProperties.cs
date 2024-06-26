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
    public DateTime finishEarlyTo { get; set; } = DateTime.MinValue;
    public DateTime finishOptimumTo { get; set; } = DateTime.MinValue;
    public DateTime finishLateTo { get; set; } = DateTime.MinValue;
    public DateTime? startOfConstruction { get; set; }
    public DateTime? endOfConstruction { get; set; }
    public DateTime? dateOfAcceptance { get; set; }
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
    public string dateSksReal { get; set; } = "";
    public string dateKap { get; set; } = "";
    public string dateKapReal { get; set; } = "";
    public string dateOks { get; set; } = "";
    public string dateOksReal { get; set; } = "";
    public DateTime dateGlTba { get; set; } = DateTime.MinValue;
    public DateTime? dateGlTbaReal { get; set; }
    public ActivityHistoryItem[] activityHistory { get; set; } = new ActivityHistoryItem[0];
    public bool isPrivate { get; set; } = false;
    public User[] involvedUsers { get; set; } = new User[0];
    public DateTime? datePlanned { get; set; }
    public DateTime? dateAccept { get; set; }
    public DateTime? dateGuarantee { get; set; }
    public bool isStudy { get; set; } = false;
    public DateTime? dateStudyStart { get; set; }
    public DateTime? dateStudyEnd { get; set; }
    public bool isDesire { get; set; } = false;
    public DateTime? dateDesireStart { get; set; }
    public DateTime? dateDesireEnd { get; set; }
    public bool isParticip { get; set; } = false;
    public DateTime? dateParticipStart { get; set; }
    public DateTime? dateParticipEnd { get; set; }
    public bool isPlanCirc { get; set; } = false;
    public DateTime? datePlanCircStart { get; set; }
    public DateTime? datePlanCircEnd { get; set; }
    public DateTime? dateConsultStart { get; set; }
    public DateTime? dateConsultEnd { get; set; }
    public DateTime? dateConsultClose { get; set; }
    public DateTime? dateReportStart { get; set; }
    public DateTime? dateReportEnd { get; set; }
    public DateTime? dateReportClose { get; set; }
    public DateTime? dateInfoStart { get; set; }
    public DateTime? dateInfoEnd { get; set; }
    public DateTime? dateInfoClose { get; set; }
    public bool isAggloprog { get; set; } = false;
    public DateTime? dateStartInconsult { get; set; }
    public DateTime? dateStartVerified { get; set; }
    public DateTime? dateStartReporting { get; set; }
    public DateTime? dateStartSuspended { get; set; }
    public DateTime? dateStartCoordinated { get; set; }
}

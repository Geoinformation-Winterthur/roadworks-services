// <copyright company="Vermessungsamt Winterthur">
//      Author: Edgar Butwilowski
//      Copyright (c) Vermessungsamt Winterthur. All rights reserved.
// </copyright>
namespace roadwork_portal_service.Model;

public class RoadWorkActivityProperties
{
    public string? uuid { get; set; } = "";
    public string? name { get; set; } = "";
    public User? projectManager { get; set; } = new User();
    public User? trafficAgent { get; set; } = new User();
    public string? description { get; set; } = "";
    public string? projectNo { get; set; } = "";
    public string? roadWorkActivityNo { get; set; } = "";
    public string? comment { get; set; } = "";
    public string? sessionComment1 { get; set; } = "";
    public string? sessionComment2 { get; set; } = "";
    public string? sessionComment3 { get; set; } = "";
    public string? section { get; set; } = "";
    public string? type { get; set; } = "";
    public string projectType { get; set; } = "";
    public string projectKind { get; set; } = "";
    public bool? overarchingMeasure { get; set; } = false;
    public int? desiredYearFrom { get; set; } = -1;
    public int? desiredYearTo { get; set; } = -1;
    public bool? prestudy { get; set; } = false;
    public DateTime? created { get; set; } = DateTime.MinValue;
    public DateTime? lastModified { get; set; } = DateTime.MinValue;
    public DateTime? finishEarlyTo { get; set; }
    public DateTime? finishOptimumTo { get; set; }
    public DateTime? finishLateTo { get; set; }
    public DateTime? startOfConstruction { get; set; }
    public DateTime? endOfConstruction { get; set; }
    public DateTime? dateOfAcceptance { get; set; }
    public DateTime? consultDue { get; set; } = DateTime.MinValue;
    public decimal? costs { get; set; } = 0m;
    public string costsType { get; set; } = "";

    public string[]? roadWorkNeedsUuids { get; set; } = new string[0];
    public string? status { get; set; }
    public bool? isEditingAllowed { get; set; } = false;
    public bool? isInInternet {get; set; } = false;
    public string? billingAddress1 { get; set; } = "";
    public string? billingAddress2 { get; set; } = "";
    public int? investmentNo { get; set; } = 0;
    public int? pdbFid { get; set; } = 0;
    public string? strabakoNo { get; set; } = "";
    public DateTime? dateSks { get; set; }
    public DateTime? dateSksReal { get; set; }
    public DateTime? dateSksPlanned { get; set; }
    public DateTime? dateKap { get; set; }
    public DateTime? dateKapReal { get; set; }
    public DateTime? dateOks { get; set; }
    public DateTime? dateOksReal { get; set; }
    public DateTime? dateGlTba { get; set; }
    public DateTime? dateGlTbaReal { get; set; }
    public ActivityHistoryItem[]? activityHistory { get; set; } = new ActivityHistoryItem[0];
    public bool? isPrivate { get; set; } = false;
    public User[]? involvedUsers { get; set; } = new User[0];
    public DateTime? datePlanned { get; set; }
    public DateTime? dateAccept { get; set; }
    public DateTime? dateGuarantee { get; set; }
    public bool? isStudy { get; set; } = false;
    public DateTime? dateStudyStart { get; set; }
    public DateTime? dateStudyEnd { get; set; }
    public DateTime? projectStudyApproved { get; set; }
    public DateTime? studyApproved { get; set; }
    public bool? isDesire { get; set; } = false;
    public DateTime? dateDesireStart { get; set; }
    public DateTime? dateDesireEnd { get; set; }
    public bool? isParticip { get; set; } = false;
    public DateTime? dateParticipStart { get; set; }
    public DateTime? dateParticipEnd { get; set; }
    public bool? isPlanCirc { get; set; } = false;
    public DateTime? datePlanCircStart { get; set; }
    public DateTime? datePlanCircEnd { get; set; }
    public DateTime? dateConsultStart1 { get; set; }
    public DateTime? dateConsultEnd1 { get; set; }
    public DateTime? dateConsultStart2 { get; set; }
    public DateTime? dateConsultEnd2 { get; set; }
    public DateTime? dateConsultClose { get; set; }
    public DateTime? dateReportStart { get; set; }
    public DateTime? dateReportEnd { get; set; }
    public DateTime? dateReportClose { get; set; }
    public DateTime? dateInfoStart { get; set; }
    public DateTime? dateInfoEnd { get; set; }
    public DateTime? dateInfoClose { get; set; }
    public bool? isAggloprog { get; set; } = false;
    public DateTime? dateStartInconsult1 { get; set; }
    public DateTime? dateStartInconsult2 { get; set; }
    public DateTime? dateStartVerified1 { get; set; }
    public DateTime? dateStartVerified2 { get; set; }
    public DateTime? dateStartReporting { get; set; }
    public DateTime? dateStartSuspended { get; set; }
    public DateTime? dateStartCoordinated { get; set; }
    public string? url { get; set; }
    public DocumentAttributes[]? documentAtts { get; set; }
    public bool? isSksRelevant { get; set; }
    public DateTime? costLastModified { get; set; }
    public User? costLastModifiedBy { get; set; } = new User();
}

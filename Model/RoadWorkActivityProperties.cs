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
    public User? trafficAgent { get; set; } = new User(); // unused
    public string? description { get; set; } = "";
    public string? projectNo { get; set; } = "";
    public string? roadWorkActivityNo { get; set; } = "";
    public string? comment { get; set; } = "";
    public string? sessionComment1 { get; set; } = "";
    public string? sessionComment2 { get; set; } = "";
    public string? section { get; set; } = "";
    public string? type { get; set; } = "";
    public string projectType { get; set; } = "";
    public string projectKind { get; set; } = "";
    public string? workingTitle { get; set; } = "";
    public bool? implementationByThird { get; set; } = false;
    public bool? overarchingMeasure { get; set; } = false;
    public int? desiredYearFrom { get; set; } = -1;
    public int? desiredYearTo { get; set; } = -1;
    public DateTime? created { get; set; } = DateTime.MinValue; // System, Modul Termine, Phase1: "Bauvorhaben erfasst"
    public DateTime? lastModified { get; set; } = DateTime.MinValue; // System
    public DateTime? finishEarlyTo { get; set; } // unused (never a value assigned), Termine Alt: "Frühester Baubeginn"
    public DateTime? finishOptimumTo { get; set; } // unused (never a value assigned), Termine Alt: "Wunsch Baubeginn"
    public DateTime? finishLateTo { get; set; } // unused (never a value assigned), Termine Alt: "Späteste Inbetriebnahme"
    public DateTime? startOfConstruction { get; set; } // Modul Termine: "Baubeginn (Voraussichtlich)"
    public DateTime? endOfConstruction { get; set; } // Modul Termine: "Bauende (Voraussichtlich)"
    // public DateTime? dateOfAcceptance { get; set; }  // removed in #650
    public DateTime? consultDue { get; set; } = DateTime.MinValue; // unused
    public decimal? costs { get; set; } = 0m;
    public string costsType { get; set; } = "";

    public string[]? roadWorkNeedsUuids { get; set; } = new string[0];
    public string? status { get; set; }
    public bool? isEditingAllowed { get; set; } = false;
    public bool? isInInternet {get; set; } = false; // unused
    public string? billingAddress1 { get; set; } = ""; // unused
    public string? billingAddress2 { get; set; } = ""; // unused
    public int? investmentNo { get; set; } = 0;
    public int? pdbFid { get; set; } = 0; // unused
    public string? strabakoNo { get; set; } = "";
    public DateTime? dateSks { get; set; } // SKS Berechnet
    public DateTime? dateSksReal { get; set; } // Modul Sitzungen, gehnehmigt: "SKS"
    public DateTime? dateSksPlanned { get; set; } // Modul Sitzungen, terminiert: "SKS"
    public long? sksNo { get; set; } 
    public DateTime? dateKap { get; set; } // Modul Sitzungen, berechnet: "KAP"
    public DateTime? dateKapReal { get; set; } // Modul Sitzungen, gehnehmigt: "KAP"
    public DateTime? dateOks { get; set; } // OKS Berechnet
    public DateTime? dateOksReal { get; set; } // Modul Sitzungen, terminiert: "OKS"
    public DateTime? dateGlTba { get; set; } // Modul Übersicht
    public DateTime? dateGlTbaReal { get; set; } // Modul Termine, Phase1: "Genehmigter Projektierungsauftrag (GL)"
    public ActivityHistoryItem[]? activityHistory { get; set; } = new ActivityHistoryItem[0];
    public bool? isPrivate { get; set; } = false;
    public User[]? involvedUsers { get; set; } = new User[0];
    public DateTime? datePlanned { get; set; } // unused
    public DateTime? dateAccept { get; set; } // unused
    public DateTime? dateGuarantee { get; set; } // Modul Termine, Phase5: "Abnahme/Garantie"
    public bool? isStudy { get; set; } = false;
    public DateTime? dateStudyStart { get; set; } // Modul Termine, Phase2: "Auftrag für Vorstudie erarbeiten"
    public DateTime? dateStudyEnd { get; set; } // Modul Termine, Phase2: "Genehmigter Auftrag Vorstudie (GL)"
    public DateTime? projectStudyApproved { get; set; } // Modul Termine, Phase2: "Vorstudie erarbeiten"
    public DateTime? studyApproved { get; set; } // Modul Termine, Phase2: "Genehmigte Vorstudie (GL)"
    //public bool? isDesire { get; set; } = false; // removed in #650
    //public DateTime? dateDesireStart { get; set; } // removed in #650
    //public DateTime? dateDesireEnd { get; set; } // removed in #650
    public bool? isParticip { get; set; } = false;
    public DateTime? dateParticipStart { get; set; } // Modul Termine, Phase3: "Planauflage §13" >> Start
    public DateTime? dateParticipEnd { get; set; } // Modul Termine, Phase3: "Planauflage §13" >> End
    public bool? isPlanCirc { get; set; } = false;
    public DateTime? datePlanCircStart { get; set; } // Modul Termine, Phase3: "Planauflage §16" >> Start
    public DateTime? datePlanCircEnd { get; set; } // Modul Termine, Phase3: "Planauflage §16" >> End
    public DateTime? dateConsultStart1 { get; set; } // Modul Termine, Phase2: "Bedarfsklärung - 1. Iteration" >> Start
    public DateTime? dateConsultEnd1 { get; set; } // Modul Termine, Phase2: "Bedarfsklärung - 1. Iteration" >> End
    public DateTime? dateConsultStart2 { get; set; } // Modul Termine, Phase2: "Bedarfsklärung - 2. Iteration" >> Start
    public DateTime? dateConsultEnd2 { get; set; } // Modul Termine, Phase2: "Bedarfsklärung - 2. Iteration" >> End
    public DateTime? dateConsultClose { get; set; } // Modul Vernehmlassung: "Bedarfsklärung Abschluss"
    public DateTime? dateReportStart { get; set; } // Modul Termine, Phase2: "Stellungnahme" >> Start
    public DateTime? dateReportEnd { get; set; } // Modul Termine, Phase2: "Stellungnahme" >> End
    public DateTime? dateReportClose { get; set; } // Modul Vernehmlassung: "Stellungnahme Abschluss"
    //public DateTime? dateInfoStart { get; set; } // removed in #650
    //public DateTime? dateInfoEnd { get; set; } // removed in #650
    //public DateTime? dateInfoClose { get; set; } // removed in #650
    public bool? isAggloprog { get; set; } = false;
    public bool? isTrafficRegulationRequired { get; set; } = false;
    public DateTime? dateStartInconsult1 { get; set; } // unused, Phase: in Bedarfsklärung - 1.Iteration (Phase 12)
    public DateTime? dateStartInconsult2 { get; set; } // unused, Phase: in Bedarfsklärung - 2.Iteration (Phase 12)
    public DateTime? dateStartVerified1 { get; set; } // Phase: verifiziert-1 (Phase 12)
    public DateTime? dateStartVerified2 { get; set; } // unused, Phase: verifiziert-2 (Phase 12)
    public DateTime? dateStartReporting { get; set; } // Phase: Stellungnahme (Phase 12)
    public DateTime? dateStartSuspended { get; set; } // unused, Phase: sistiert
    public DateTime? dateStartCoordinated { get; set; } // Phase: koordiniert (Phase 12)
    public string? url { get; set; }
    public DocumentAttributes[]? documentAtts { get; set; }
    public bool? isOksActive { get; set; }
    public DateTime? isOksActiveLastModified { get; set; } // System & Export only
    public DateTime? costLastModified { get; set; } // System & Export only
    public User? costLastModifiedBy { get; set; } = new User();

    // Additional attributes for journal (#616, 2026.4)
    public string? plannedTasks { get; set; } = "";
    public string? constraintsDependencies { get; set; } = "";
    public string? acquisitionPlanned { get; set; } = "NO"; // Valid values: YES, NO, MAYBE

    // Aggloprogramm (#617, 2026.4)
    public bool? partOfAggloprogram { get; set; } = false;
    public string aggloprogramLink { get; set; } = "";
    public int? aggloprogramGeneration { get; set; }
    public string? aggloprogramAreCode { get; set; } = "";
    public string? aggloprogramAreDescription { get; set; } = "";
    public DateTime? aggloprogramDueDate { get; set; } // Modul Journal, Agglo: "Umzusetzen bis"
    public decimal? aggloprogramCostTotal { get; set; }
    public decimal? aggloprogramCostCanton { get; set; }

    // Prestudy
    public bool? prestudy { get; set; } = false;
    // Prestudy additional (#663, 2026.9)
    public bool? prestudySks { get; set; } = false;
    // Prestudy additional (#621, 2026.4)
    public string? prestudyDuration { get; set; } = "";
    public string? prestudyContractor { get; set; } = "";
    public string? prestudyDetail { get; set; } = "";
    public DateTime? prestudyVkErConfirmed { get; set; } // Modul Journal: "Finanzielle Ressourcen für Phase 2 (VK ER) abgesprochen.."
    public long? prestudyVkErNumber { get; set; }

    // Affected entities (#622, 2026.4)
    public bool? busStopsSheltersAffected { get; set; } = false;
    public bool? structuresAffected { get; set; } = false;
    public bool? roadDrainageAffected { get; set; } = false;
    public bool? houseConnectionsAffected { get; set; } = false;
    public bool? wasteFacilitiesAffected { get; set; } = false;
    public bool? technicalInstallationsAffected { get; set; } = false;
    public bool? treesAffected { get; set; } = false;
    public bool? streetFurnitureAffected { get; set; } = false;
    public bool? urbanClimateAffected { get; set; } = false;
    public bool? subjectToDepaving { get; set; } = false;
    public bool? pedestriansCyclingAffected { get; set; } = false;
    public bool? disabilityEqualityAffected { get; set; } = false;

    // Private entities (#623, 2026.4)
    public bool? privateEntityAffected { get; set; } = false;
    public string? privateEntityExtent { get; set; } = "";
    public string? privateEntityRequirements { get; set; } = "";
    public bool? privateEntityAcquisition { get; set; } = false;
    public bool? privateEntityIsInitiator { get; set; } = false;

    // Provis (Abacus) (#624, 2026.4)
    public long? erpNumber { get; set; }

    // Ressources (#625, 2026.4)
    public DateTime? staffResourcesAprConfirmed { get; set; } // Modul Journal: "Personelle Ressourcen APR (ab Phase 3) abgesprochen"
    public DateTime? costEstimateAprConfirmed { get; set; } // Modul Journal: "Journal >> "Kostenschätzung mit APR (Phase 3 bis 5) abgesprochen"

    // Engineering contract (#626, 2026.4)
    public bool? coreDrillingContracted { get; set; } = false;
    public bool? quotesRequested { get; set; } = false;
    public bool? quotesReviewed { get; set; } = false;
    public bool? aprChecked { get; set; } = false;
    public bool? afmChecked { get; set; } = false;

    // Approval and filing (#626, 2026.4)
    public bool? cfDone { get; set; } = false;
    public bool? rdDone { get; set; } = false;
    public bool? approved { get; set; } = false;
    public bool? fabasoftDone { get; set; } = false;
    public bool? gisUpdated { get; set; } = false;

    public RoadWorkApprovals approvals { get; set; } = new RoadWorkApprovals();
}

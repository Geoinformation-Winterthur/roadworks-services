using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Geometries;
using Npgsql;
using roadwork_portal_service.Configuration;
using roadwork_portal_service.Helper;
using roadwork_portal_service.Model;

namespace roadwork_portal_service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RoadWorkActivityController : ControllerBase
    {
        private readonly ILogger<RoadWorkActivityController> _logger;
        private IConfiguration Configuration { get; }

        public RoadWorkActivityController(ILogger<RoadWorkActivityController> logger,
                        IConfiguration configuration)
        {
            _logger = logger;
            this.Configuration = configuration;
        }

        // GET roadworkactivity/
        [HttpGet]
        [Authorize(Roles = "view,orderer,trafficmanager,territorymanager,administrator")]
        public async Task<IEnumerable<RoadWorkActivityFeature>> GetActivities
                                (string? uuid = "", string? status = "", bool summary = false)
        {
            List<RoadWorkActivityFeature> projectsFromDb = new List<RoadWorkActivityFeature>();
            // get data of current user from database:
            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand selectComm = pgConn.CreateCommand();
                selectComm.CommandText = @"SELECT r.uuid, r.name, 
                        r.projectmanager, pm.first_name AS pm_first_name, pm.last_name AS pm_last_name, r.traffic_agent,
                        ta.first_name AS ta_first_name, ta.last_name AS ta_last_name, description, created, last_modified, r.date_from, r.date_to,
                        r.costs, r.costs_type, r.status, r.in_internet,
                        r.billing_address1, r.billing_address2, r.investment_no, r.pdb_fid,
                        r.strabako_no, r.date_sks, r.date_kap, r.date_oks, r.date_gl_tba,
                        r.comment, r.session_comment_1, r.session_comment_2, r.section, r.type, r.projecttype, r.projectkind, r.overarching_measure,
                        r.desired_year_from, r.desired_year_to, r.prestudy, r.start_of_construction,
                        r.end_of_construction, r.consult_due, r.project_no, 
                        r.roadworkactivity_no, 
                        r.private, r.date_accept,
                        r.date_guarantee, r.is_study, r.date_study_start, r.date_study_end,
                        r.is_desire, r.date_desire_start, r.date_desire_end, r.is_particip,
                        r.date_particip_start, r.date_particip_end, r.is_plan_circ,
                        r.date_plan_circ_start, r.date_plan_circ_end, r.date_consult_start1, r.date_consult_end1,
                        r.date_consult_start2, r.date_consult_end2, r.date_consult_close, r.date_report_start,
                        r.date_report_end, r.date_report_close, r.date_info_start,
                        r.date_info_end, r.date_info_close, r.is_aggloprog, r.is_traffic_regulation_required, r.date_optimum,
                        r.date_of_acceptance, r.url,
                        r.project_study_approved, r.study_approved, r.date_sks_real,
                        r.date_kap_real, r.date_oks_real, r.date_gl_tba_real,
                        r.date_start_inconsult1, r.date_start_verified1, r.date_start_inconsult2, r.date_start_verified2, r.date_start_reporting,
                        r.date_start_suspended, r.date_start_coordinated, r.sks_relevant,
                        r.costs_last_modified, r.costs_last_modified_by,
                        cm.first_name AS cm_first_name, cm.last_name AS cm_last_name,
                        r.date_sks_planned, r.geom
                    FROM ""wtb_ssp_roadworkactivities"" r
                    LEFT JOIN ""wtb_ssp_users"" pm ON r.projectmanager = pm.uuid
                    LEFT JOIN ""wtb_ssp_users"" ta ON r.traffic_agent = ta.uuid
                    LEFT JOIN ""wtb_ssp_users"" cm ON r.costs_last_modified_by = cm.uuid";

                if (uuid != null)
                {
                    uuid = uuid.Trim().ToLower();
                    if (uuid != "")
                    {
                        selectComm.CommandText += " WHERE r.uuid=@uuid";
                        selectComm.Parameters.AddWithValue("uuid", new Guid(uuid));
                    }
                }

                if (status == null)
                    status = "";
                status = status.Trim();

                if ((uuid == null || uuid == "") && status != "")
                {
                    string[] statusArray = status.Split(",");

                    int i = 0;
                    while (i < statusArray.Length)
                    {
                        statusArray[i] = statusArray[i].Trim().ToLower();
                        i++;
                    }

                    selectComm.CommandText += " WHERE r.status = ANY (:status)";
                    selectComm.Parameters.AddWithValue("status", statusArray);
                }

                using (NpgsqlDataReader reader = await selectComm.ExecuteReaderAsync())
                {
                    RoadWorkActivityFeature projectFeatureFromDb;
                    while (reader.Read())
                    {
                        projectFeatureFromDb = new RoadWorkActivityFeature();
                        projectFeatureFromDb.properties.uuid = reader.IsDBNull(reader.GetOrdinal("uuid"))
                            ? ""
                            : reader.GetGuid(reader.GetOrdinal("uuid")).ToString();

                        projectFeatureFromDb.properties.name = reader.IsDBNull(reader.GetOrdinal("name"))
                            ? ""
                            : reader.GetString(reader.GetOrdinal("name"));

                        User projectManager = new User();
                        projectManager.uuid = reader.IsDBNull(reader.GetOrdinal("projectmanager"))
                            ? ""
                            : reader.GetGuid(reader.GetOrdinal("projectmanager")).ToString();
                        projectManager.firstName = reader.IsDBNull(reader.GetOrdinal("pm_first_name"))
                            ? ""
                            : reader.GetString(reader.GetOrdinal("pm_first_name"));
                        projectManager.lastName = reader.IsDBNull(reader.GetOrdinal("pm_last_name"))
                            ? ""
                            : reader.GetString(reader.GetOrdinal("pm_last_name"));
                        projectFeatureFromDb.properties.projectManager = projectManager;

                        User trafficAgent = new User();
                        trafficAgent.uuid = reader.IsDBNull(reader.GetOrdinal("traffic_agent"))
                            ? ""
                            : reader.GetGuid(reader.GetOrdinal("traffic_agent")).ToString();
                        trafficAgent.firstName = reader.IsDBNull(reader.GetOrdinal("ta_first_name"))
                            ? ""
                            : reader.GetString(reader.GetOrdinal("ta_first_name"));
                        trafficAgent.lastName = reader.IsDBNull(reader.GetOrdinal("ta_last_name"))
                            ? ""
                            : reader.GetString(reader.GetOrdinal("ta_last_name"));
                        projectFeatureFromDb.properties.trafficAgent = trafficAgent;

                        User costLastModifiedBy = new User();
                        costLastModifiedBy.uuid = reader.IsDBNull(reader.GetOrdinal("costs_last_modified_by"))
                            ? ""
                            : reader.GetGuid(reader.GetOrdinal("costs_last_modified_by")).ToString();
                        costLastModifiedBy.firstName = reader.IsDBNull(reader.GetOrdinal("cm_first_name"))
                            ? ""
                            : reader.GetString(reader.GetOrdinal("cm_first_name"));
                        costLastModifiedBy.lastName = reader.IsDBNull(reader.GetOrdinal("cm_last_name"))
                            ? ""
                            : reader.GetString(reader.GetOrdinal("cm_last_name"));
                        projectFeatureFromDb.properties.costLastModifiedBy = costLastModifiedBy;

                        projectFeatureFromDb.properties.description = reader.IsDBNull(reader.GetOrdinal("description"))
                            ? ""
                            : reader.GetString(reader.GetOrdinal("description"));
                        projectFeatureFromDb.properties.created = reader.IsDBNull(reader.GetOrdinal("created"))
                            ? DateTime.MinValue
                            : reader.GetDateTime(reader.GetOrdinal("created"));
                        projectFeatureFromDb.properties.lastModified = reader.IsDBNull(reader.GetOrdinal("last_modified"))
                            ? DateTime.MinValue
                            : reader.GetDateTime(reader.GetOrdinal("last_modified"));
                        if (!reader.IsDBNull(reader.GetOrdinal("date_from")))
                            projectFeatureFromDb.properties.finishEarlyTo = reader.GetDateTime(reader.GetOrdinal("date_from"));
                        if (!reader.IsDBNull(reader.GetOrdinal("date_to")))
                            projectFeatureFromDb.properties.finishLateTo = reader.GetDateTime(reader.GetOrdinal("date_to"));
                        if (!reader.IsDBNull(reader.GetOrdinal("date_optimum")))
                            projectFeatureFromDb.properties.finishEarlyTo = reader.GetDateTime(reader.GetOrdinal("date_optimum"));
                        else
                            projectFeatureFromDb.properties.finishEarlyTo = null;
                        projectFeatureFromDb.properties.costs = reader.IsDBNull(reader.GetOrdinal("costs"))
                            ? 0m
                            : reader.GetDecimal(reader.GetOrdinal("costs"));

                        projectFeatureFromDb.properties.costsType = reader.IsDBNull(reader.GetOrdinal("costs_type"))
                            ? ""
                            : reader.GetString(reader.GetOrdinal("costs_type"));

                        if (User.IsInRole("administrator") || User.IsInRole("territorymanager"))
                        {
                            projectFeatureFromDb.properties.isEditingAllowed = true;
                        }

                        projectFeatureFromDb.properties.status = reader.IsDBNull(reader.GetOrdinal("status"))
                            ? ""
                            : reader.GetString(reader.GetOrdinal("status"));

                        projectFeatureFromDb.properties.isInInternet = reader.IsDBNull(reader.GetOrdinal("in_internet"))
                            ? false
                            : reader.GetBoolean(reader.GetOrdinal("in_internet"));
                        projectFeatureFromDb.properties.billingAddress1 = reader.IsDBNull(reader.GetOrdinal("billing_address1"))
                            ? ""
                            : reader.GetString(reader.GetOrdinal("billing_address1"));
                        projectFeatureFromDb.properties.billingAddress2 = reader.IsDBNull(reader.GetOrdinal("billing_address2"))
                            ? ""
                            : reader.GetString(reader.GetOrdinal("billing_address2"));
                        projectFeatureFromDb.properties.investmentNo = reader.IsDBNull(reader.GetOrdinal("investment_no"))
                            ? 0
                            : reader.GetInt32(reader.GetOrdinal("investment_no"));
                        projectFeatureFromDb.properties.pdbFid = reader.IsDBNull(reader.GetOrdinal("pdb_fid"))
                            ? 0
                            : reader.GetInt32(reader.GetOrdinal("pdb_fid"));
                        projectFeatureFromDb.properties.strabakoNo = reader.IsDBNull(reader.GetOrdinal("strabako_no"))
                            ? ""
                            : reader.GetString(reader.GetOrdinal("strabako_no"));

                        projectFeatureFromDb.properties.dateSks = reader.IsDBNull(reader.GetOrdinal("date_sks"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("date_sks"));
                        projectFeatureFromDb.properties.dateSksPlanned = reader.IsDBNull(reader.GetOrdinal("date_sks_planned"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("date_sks_planned"));
                        projectFeatureFromDb.properties.dateKap = reader.IsDBNull(reader.GetOrdinal("date_kap"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("date_kap"));
                        projectFeatureFromDb.properties.dateOks = reader.IsDBNull(reader.GetOrdinal("date_oks"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("date_oks"));
                        projectFeatureFromDb.properties.dateGlTba = reader.IsDBNull(reader.GetOrdinal("date_gl_tba"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("date_gl_tba"));

                        projectFeatureFromDb.properties.comment = reader.IsDBNull(reader.GetOrdinal("comment"))
                            ? ""
                            : reader.GetString(reader.GetOrdinal("comment"));
                        projectFeatureFromDb.properties.sessionComment1 = reader.IsDBNull(reader.GetOrdinal("session_comment_1"))
                            ? ""
                            : reader.GetString(reader.GetOrdinal("session_comment_1"));
                        projectFeatureFromDb.properties.sessionComment2 = reader.IsDBNull(reader.GetOrdinal("session_comment_2"))
                            ? ""
                            : reader.GetString(reader.GetOrdinal("session_comment_2"));                        
                        projectFeatureFromDb.properties.section = reader.IsDBNull(reader.GetOrdinal("section"))
                            ? ""
                            : reader.GetString(reader.GetOrdinal("section"));
                        projectFeatureFromDb.properties.type = reader.IsDBNull(reader.GetOrdinal("type"))
                            ? ""
                            : reader.GetString(reader.GetOrdinal("type"));
                        projectFeatureFromDb.properties.projectType = reader.IsDBNull(reader.GetOrdinal("projecttype"))
                            ? ""
                            : reader.GetString(reader.GetOrdinal("projecttype"));
                        projectFeatureFromDb.properties.projectKind = reader.IsDBNull(reader.GetOrdinal("projectkind"))
                            ? ""
                            : reader.GetString(reader.GetOrdinal("projectkind"));
                        projectFeatureFromDb.properties.overarchingMeasure = reader.IsDBNull(reader.GetOrdinal("overarching_measure"))
                            ? false
                            : reader.GetBoolean(reader.GetOrdinal("overarching_measure"));
                        projectFeatureFromDb.properties.desiredYearFrom = reader.IsDBNull(reader.GetOrdinal("desired_year_from"))
                            ? -1
                            : reader.GetInt32(reader.GetOrdinal("desired_year_from"));
                        projectFeatureFromDb.properties.desiredYearTo = reader.IsDBNull(reader.GetOrdinal("desired_year_to"))
                            ? -1
                            : reader.GetInt32(reader.GetOrdinal("desired_year_to"));
                        projectFeatureFromDb.properties.prestudy = reader.IsDBNull(reader.GetOrdinal("prestudy"))
                            ? false
                            : reader.GetBoolean(reader.GetOrdinal("prestudy"));
                        if (!reader.IsDBNull(reader.GetOrdinal("start_of_construction")))
                            projectFeatureFromDb.properties.startOfConstruction =
                                reader.GetDateTime(reader.GetOrdinal("start_of_construction"));
                        if (!reader.IsDBNull(reader.GetOrdinal("end_of_construction")))
                            projectFeatureFromDb.properties.endOfConstruction =
                                reader.GetDateTime(reader.GetOrdinal("end_of_construction"));
                        projectFeatureFromDb.properties.consultDue = reader.IsDBNull(reader.GetOrdinal("consult_due"))
                            ? DateTime.MinValue
                            : reader.GetDateTime(reader.GetOrdinal("consult_due"));
                        projectFeatureFromDb.properties.projectNo = reader.IsDBNull(reader.GetOrdinal("project_no"))
                            ? ""
                            : reader.GetString(reader.GetOrdinal("project_no"));
                        projectFeatureFromDb.properties.roadWorkActivityNo = reader.IsDBNull(reader.GetOrdinal("roadworkactivity_no"))
                            ? ""
                            : reader.GetString(reader.GetOrdinal("roadworkactivity_no"));                                                        
                        projectFeatureFromDb.properties.isPrivate = reader.IsDBNull(reader.GetOrdinal("private"))
                            ? false
                            : reader.GetBoolean(reader.GetOrdinal("private"));
                        if (!reader.IsDBNull(reader.GetOrdinal("date_accept")))
                            projectFeatureFromDb.properties.dateAccept = reader.GetDateTime(reader.GetOrdinal("date_accept"));
                        if (!reader.IsDBNull(reader.GetOrdinal("date_guarantee")))
                            projectFeatureFromDb.properties.dateGuarantee = reader.GetDateTime(reader.GetOrdinal("date_guarantee"));
                        if (!reader.IsDBNull(reader.GetOrdinal("is_study")))
                            projectFeatureFromDb.properties.isStudy = reader.GetBoolean(reader.GetOrdinal("is_study"));
                        if (!reader.IsDBNull(reader.GetOrdinal("date_study_start")))
                            projectFeatureFromDb.properties.dateStudyStart =
                                reader.GetDateTime(reader.GetOrdinal("date_study_start"));
                        if (!reader.IsDBNull(reader.GetOrdinal("date_study_end")))
                            projectFeatureFromDb.properties.dateStudyEnd = reader.GetDateTime(reader.GetOrdinal("date_study_end"));
                        if (!reader.IsDBNull(reader.GetOrdinal("is_desire")))
                            projectFeatureFromDb.properties.isDesire = reader.GetBoolean(reader.GetOrdinal("is_desire"));
                        if (!reader.IsDBNull(reader.GetOrdinal("date_desire_start")))
                            projectFeatureFromDb.properties.dateDesireStart =
                                reader.GetDateTime(reader.GetOrdinal("date_desire_start"));
                        if (!reader.IsDBNull(reader.GetOrdinal("date_desire_end")))
                            projectFeatureFromDb.properties.dateDesireEnd =
                                reader.GetDateTime(reader.GetOrdinal("date_desire_end"));
                        if (!reader.IsDBNull(reader.GetOrdinal("is_particip")))
                            projectFeatureFromDb.properties.isParticip = reader.GetBoolean(reader.GetOrdinal("is_particip"));
                        if (!reader.IsDBNull(reader.GetOrdinal("date_particip_start")))
                            projectFeatureFromDb.properties.dateParticipStart =
                                reader.GetDateTime(reader.GetOrdinal("date_particip_start"));
                        if (!reader.IsDBNull(reader.GetOrdinal("date_particip_end")))
                            projectFeatureFromDb.properties.dateParticipEnd =
                                reader.GetDateTime(reader.GetOrdinal("date_particip_end"));
                        if (!reader.IsDBNull(reader.GetOrdinal("is_plan_circ")))
                            projectFeatureFromDb.properties.isPlanCirc = reader.GetBoolean(reader.GetOrdinal("is_plan_circ"));
                        if (!reader.IsDBNull(reader.GetOrdinal("date_plan_circ_start")))
                            projectFeatureFromDb.properties.datePlanCircStart =
                                reader.GetDateTime(reader.GetOrdinal("date_plan_circ_start"));
                        if (!reader.IsDBNull(reader.GetOrdinal("date_plan_circ_end")))
                            projectFeatureFromDb.properties.datePlanCircEnd =
                                reader.GetDateTime(reader.GetOrdinal("date_plan_circ_end"));
                        if (!reader.IsDBNull(reader.GetOrdinal("date_consult_start1")))
                            projectFeatureFromDb.properties.dateConsultStart1 =
                                reader.GetDateTime(reader.GetOrdinal("date_consult_start1"));
                        if (!reader.IsDBNull(reader.GetOrdinal("date_consult_end1")))
                            projectFeatureFromDb.properties.dateConsultEnd1 =
                                reader.GetDateTime(reader.GetOrdinal("date_consult_end1"));
                        if (!reader.IsDBNull(reader.GetOrdinal("date_consult_start2")))
                            projectFeatureFromDb.properties.dateConsultStart2 =
                                reader.GetDateTime(reader.GetOrdinal("date_consult_start2"));
                        if (!reader.IsDBNull(reader.GetOrdinal("date_consult_end2")))
                            projectFeatureFromDb.properties.dateConsultEnd2 =
                                reader.GetDateTime(reader.GetOrdinal("date_consult_end2"));
                        if (!reader.IsDBNull(reader.GetOrdinal("date_consult_close")))
                            projectFeatureFromDb.properties.dateConsultClose =
                                reader.GetDateTime(reader.GetOrdinal("date_consult_close"));
                        if (!reader.IsDBNull(reader.GetOrdinal("date_report_start")))
                            projectFeatureFromDb.properties.dateReportStart =
                                reader.GetDateTime(reader.GetOrdinal("date_report_start"));
                        if (!reader.IsDBNull(reader.GetOrdinal("date_report_end")))
                            projectFeatureFromDb.properties.dateReportEnd =
                                reader.GetDateTime(reader.GetOrdinal("date_report_end"));
                        if (!reader.IsDBNull(reader.GetOrdinal("date_report_close")))
                            projectFeatureFromDb.properties.dateReportClose =
                                reader.GetDateTime(reader.GetOrdinal("date_report_close"));
                        if (!reader.IsDBNull(reader.GetOrdinal("date_info_start")))
                            projectFeatureFromDb.properties.dateInfoStart =
                                reader.GetDateTime(reader.GetOrdinal("date_info_start"));
                        if (!reader.IsDBNull(reader.GetOrdinal("date_info_end")))
                            projectFeatureFromDb.properties.dateInfoEnd =
                                reader.GetDateTime(reader.GetOrdinal("date_info_end"));
                        if (!reader.IsDBNull(reader.GetOrdinal("date_info_close")))
                            projectFeatureFromDb.properties.dateInfoClose =
                                reader.GetDateTime(reader.GetOrdinal("date_info_close"));
                        if (!reader.IsDBNull(reader.GetOrdinal("is_aggloprog")))
                            projectFeatureFromDb.properties.isAggloprog = reader.GetBoolean(reader.GetOrdinal("is_aggloprog"));
                        if (!reader.IsDBNull(reader.GetOrdinal("is_traffic_regulation_required")))
                            projectFeatureFromDb.properties.isTrafficRegulationRequired = reader.GetBoolean(reader.GetOrdinal("is_traffic_regulation_required"));                            
                        if (!reader.IsDBNull(reader.GetOrdinal("date_of_acceptance")))
                            projectFeatureFromDb.properties.dateOfAcceptance =
                                reader.GetDateTime(reader.GetOrdinal("date_of_acceptance"));
                        if (!reader.IsDBNull(reader.GetOrdinal("url")))
                            projectFeatureFromDb.properties.url = reader.GetString(reader.GetOrdinal("url"));

                        /*  intentionally mirrors original logic, assigning both properties from "study_approved" */
                        if (!reader.IsDBNull(reader.GetOrdinal("project_study_approved")))
                            projectFeatureFromDb.properties.projectStudyApproved =
                                reader.GetDateTime(reader.GetOrdinal("study_approved"));
                        if (!reader.IsDBNull(reader.GetOrdinal("study_approved")))
                            projectFeatureFromDb.properties.studyApproved =
                                reader.GetDateTime(reader.GetOrdinal("study_approved"));

                        projectFeatureFromDb.properties.dateSksReal = reader.IsDBNull(reader.GetOrdinal("date_sks_real"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("date_sks_real"));
                        projectFeatureFromDb.properties.dateKapReal = reader.IsDBNull(reader.GetOrdinal("date_kap_real"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("date_kap_real"));
                        projectFeatureFromDb.properties.dateOksReal = reader.IsDBNull(reader.GetOrdinal("date_oks_real"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("date_oks_real"));
                        projectFeatureFromDb.properties.dateGlTbaReal = reader.IsDBNull(reader.GetOrdinal("date_gl_tba_real"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("date_gl_tba_real"));
                        projectFeatureFromDb.properties.dateStartInconsult1 = reader.IsDBNull(reader.GetOrdinal("date_start_inconsult1"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("date_start_inconsult1"));
                        projectFeatureFromDb.properties.dateStartVerified1 = reader.IsDBNull(reader.GetOrdinal("date_start_verified1"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("date_start_verified1"));
                        projectFeatureFromDb.properties.dateStartInconsult2 = reader.IsDBNull(reader.GetOrdinal("date_start_inconsult2"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("date_start_inconsult2"));                            
                        projectFeatureFromDb.properties.dateStartVerified2 = reader.IsDBNull(reader.GetOrdinal("date_start_verified2"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("date_start_verified2"));
                        projectFeatureFromDb.properties.dateStartReporting = reader.IsDBNull(reader.GetOrdinal("date_start_reporting"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("date_start_reporting"));
                        projectFeatureFromDb.properties.dateStartSuspended = reader.IsDBNull(reader.GetOrdinal("date_start_suspended"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("date_start_suspended"));
                        projectFeatureFromDb.properties.dateStartCoordinated =
                            reader.IsDBNull(reader.GetOrdinal("date_start_coordinated"))
                                ? null
                                : reader.GetDateTime(reader.GetOrdinal("date_start_coordinated"));
                        projectFeatureFromDb.properties.isSksRelevant = reader.IsDBNull(reader.GetOrdinal("sks_relevant"))
                            ? false
                            : reader.GetBoolean(reader.GetOrdinal("sks_relevant"));
                        projectFeatureFromDb.properties.costLastModified = reader.IsDBNull(reader.GetOrdinal("costs_last_modified"))
                            ? null
                            : reader.GetDateTime(reader.GetOrdinal("costs_last_modified"));

                        Polygon ntsPoly = reader.IsDBNull(reader.GetOrdinal("geom"))
                            ? Polygon.Empty
                            : reader.GetValue(reader.GetOrdinal("geom")) as Polygon;
                        projectFeatureFromDb.geometry = new RoadworkPolygon(ntsPoly);

                        projectsFromDb.Add(projectFeatureFromDb);
                    }
                }

                foreach (RoadWorkActivityFeature projectFeatureFromDb in projectsFromDb)
                {

                    NpgsqlCommand selectDocAttsComm = pgConn.CreateCommand();
                    selectDocAttsComm.CommandText = "SELECT uuid, filename FROM \"wtb_ssp_documents\" " +
                        "WHERE roadworkactivity=@roadworkactivity";
                    selectDocAttsComm.Parameters.AddWithValue("roadworkactivity", new Guid(projectFeatureFromDb.properties.uuid));

                    List<DocumentAttributes> multipleDocumentsAttributes = new List<DocumentAttributes>();
                    using (NpgsqlDataReader docsReader = selectDocAttsComm.ExecuteReader())
                    {
                        DocumentAttributes documentAttributes;
                        while (docsReader.Read())
                        {
                            if (!docsReader.IsDBNull(0))
                            {
                                documentAttributes = new DocumentAttributes();
                                documentAttributes.uuid = docsReader.GetGuid(0).ToString();
                                documentAttributes.filename = "";
                                if (!docsReader.IsDBNull(1))
                                {
                                    documentAttributes.filename = docsReader.GetString(1);
                                }
                                multipleDocumentsAttributes.Add(documentAttributes);
                            }
                        }
                    }
                    projectFeatureFromDb.properties.documentAtts = multipleDocumentsAttributes.ToArray();
                }


                foreach (RoadWorkActivityFeature activityFromDb in projectsFromDb)
                {
                    NpgsqlCommand selectRoadWorkNeedsComm = pgConn.CreateCommand();
                    selectRoadWorkNeedsComm.CommandText = @"SELECT uuid_roadwork_need
                            FROM ""wtb_ssp_activities_to_needs""
                            WHERE uuid_roadwork_activity = @uuid_roadwork_activity";
                    selectRoadWorkNeedsComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(activityFromDb.properties.uuid));

                    using (NpgsqlDataReader roadWorkNeedReader = await selectRoadWorkNeedsComm.ExecuteReaderAsync())
                    {
                        List<string> roadWorkNeedsUuids = new List<string>();
                        while (roadWorkNeedReader.Read())
                        {
                            if (!roadWorkNeedReader.IsDBNull(0))
                                roadWorkNeedsUuids.Add(roadWorkNeedReader.GetGuid(0).ToString());
                        }
                        activityFromDb.properties.roadWorkNeedsUuids = roadWorkNeedsUuids.ToArray();
                    }

                    if (!summary)
                    {
                        NpgsqlCommand selectHistoryComm = pgConn.CreateCommand();
                        selectHistoryComm.CommandText = @"SELECT uuid, changedate, who, what, usercomment
                            FROM ""wtb_ssp_activities_history"" WHERE uuid_roadwork_activity = :uuid_roadwork_activity
                            ORDER BY changedate DESC";
                        selectHistoryComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(activityFromDb.properties.uuid));

                        using (NpgsqlDataReader activityHistoryReader = await selectHistoryComm.ExecuteReaderAsync())
                        {
                            List<ActivityHistoryItem> activityHistoryItems = new List<ActivityHistoryItem>();
                            while (activityHistoryReader.Read())
                            {
                                ActivityHistoryItem activityHistoryItem = new ActivityHistoryItem();
                                activityHistoryItem.uuid = activityHistoryReader.IsDBNull(0) ? "" : activityHistoryReader.GetGuid(0).ToString();
                                activityHistoryItem.changeDate = activityHistoryReader.IsDBNull(1) ? DateTime.MinValue : activityHistoryReader.GetDateTime(1);
                                activityHistoryItem.who = activityHistoryReader.IsDBNull(2) ? "" : activityHistoryReader.GetString(2);
                                activityHistoryItem.what = activityHistoryReader.IsDBNull(3) ? "" : activityHistoryReader.GetString(3);
                                activityHistoryItem.userComment = activityHistoryReader.IsDBNull(4) ? "" : activityHistoryReader.GetString(4);

                                activityHistoryItems.Add(activityHistoryItem);
                            }
                            activityFromDb.properties.activityHistory = activityHistoryItems.ToArray();
                        }
                    }

                    NpgsqlCommand selectInvolvedUsersComm = pgConn.CreateCommand();
                    selectInvolvedUsersComm.CommandText = @"SELECT users.uuid, users.last_name,
                                        users.first_name, users.e_mail, org.uuid,
                                        org.name, org.abbreviation
                                    FROM ""wtb_ssp_act_partic"" partic
                                    LEFT JOIN ""wtb_ssp_users"" users
                                    ON partic.participant = users.uuid
                                    LEFT JOIN ""wtb_ssp_organisationalunits"" org
                                    ON users.org_unit = org.uuid
                                    WHERE partic.road_act=@uuid";
                    selectInvolvedUsersComm.Parameters.AddWithValue("uuid", new Guid(activityFromDb.properties.uuid));

                    using (NpgsqlDataReader involvedUsersReader = await selectInvolvedUsersComm.ExecuteReaderAsync())
                    {
                        List<User> involvedUsers = new List<User>();
                        while (involvedUsersReader.Read())
                        {
                            if (!involvedUsersReader.IsDBNull(0))
                            {
                                User involvedUser = new User();
                                involvedUser.uuid = involvedUsersReader.GetGuid(0).ToString();
                                involvedUser.lastName = involvedUsersReader.GetString(1);
                                involvedUser.firstName = involvedUsersReader.GetString(2);
                                involvedUser.mailAddress = involvedUsersReader.GetString(3);
                                involvedUser.organisationalUnit.uuid = involvedUsersReader.GetGuid(4).ToString();
                                involvedUser.organisationalUnit.name = involvedUsersReader.GetString(5);
                                involvedUser.organisationalUnit.abbreviation = involvedUsersReader.GetString(6);

                                involvedUsers.Add(involvedUser);
                            }
                        }
                        activityFromDb.properties.involvedUsers = involvedUsers.ToArray();
                    }
                }

                pgConn.Close();
            }

            return projectsFromDb.ToArray();
        }

        // GET roadworkactivity/projecttypes/
        [HttpGet]
        [Route("/Roadworkactivity/Projecttypes/")]
        [Authorize]
        public IEnumerable<EnumType> GetProjectTypes()
        {
            List<EnumType> result = new List<EnumType>();
            // get data of current user from database:
            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand selectComm = pgConn.CreateCommand();
                selectComm.CommandText = "SELECT code, name FROM \"wtb_ssp_projecttypes\"";

                using (NpgsqlDataReader reader = selectComm.ExecuteReader())
                {
                    EnumType projectType;
                    while (reader.Read())
                    {
                        projectType = new EnumType();
                        projectType.code = reader.IsDBNull(0) ? "" : reader.GetString(0);
                        projectType.name = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        result.Add(projectType);
                    }
                }
                pgConn.Close();
            }
            return result.ToArray();
        }

        // POST roadworkactivity/
        [HttpPost]
        [Authorize(Roles = "territorymanager,administrator")]
        public ActionResult<RoadWorkActivityFeature> AddActivity([FromBody] RoadWorkActivityFeature roadWorkActivityFeature)
        {
            try
            {
                Polygon roadWorkActivityPoly = roadWorkActivityFeature.geometry.getNtsPolygon();
                Coordinate[] coordinates = roadWorkActivityPoly.Coordinates;

                if (!(bool)roadWorkActivityFeature.properties.isPrivate)
                {
                    _logger.LogError("User tried to add instead of update a public roadwork activity, " +
                        "which is not allowed");
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "SSP-3";
                    return Ok(roadWorkActivityFeature);
                }

                if (coordinates.Length < 3)
                {
                    _logger.LogWarning("Roadwork activity polygon has less than 3 coordinates");
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "SSP-7";
                    return Ok(roadWorkActivityFeature);
                }

                ConfigurationData configData = AppConfigController.getConfigurationFromDb(false);

                // only if project area is greater than min area size:
                if (roadWorkActivityPoly.Area <= configData.minAreaSize)
                {
                    _logger.LogWarning("Roadwork need area is less than or equal " + configData.minAreaSize + "qm");
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "SSP-8";
                    return Ok(roadWorkActivityFeature);
                }

                // only if project area is smaller than max area size:
                if (roadWorkActivityPoly.Area > configData.maxAreaSize)
                {
                    _logger.LogWarning("Roadwork need area is greater than " + configData.maxAreaSize + "qm");
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "SSP-16";
                    return Ok(roadWorkActivityFeature);
                }

                if (roadWorkActivityFeature.properties.finishEarlyTo! > roadWorkActivityFeature.properties.finishLateTo)
                {
                    _logger.LogWarning("The finish from date of a roadwork activity cannot be higher than its finish to date");
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "SSP-19";
                    return Ok(roadWorkActivityFeature);
                }

                if (roadWorkActivityFeature.properties.isAggloprog != null &&
                        (bool)roadWorkActivityFeature.properties.isAggloprog)
                    roadWorkActivityFeature.properties.isSksRelevant = true;

                if (roadWorkActivityFeature.properties.projectNo == null)
                    roadWorkActivityFeature.properties.projectNo = "";
                else roadWorkActivityFeature.properties.projectNo = roadWorkActivityFeature.properties.projectNo.Trim();

                User userFromDb = LoginController.getAuthorizedUserFromDb(this.User, false);

                ConfigurationData configurationData = AppConfigController.getConfigurationFromDb(false);

                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {

                    pgConn.Open();                    

                    if (roadWorkActivityFeature.properties.name == null || roadWorkActivityFeature.properties.name == "")
                    {
                        roadWorkActivityFeature.properties.name = HelperFunctions.getAddressNames(roadWorkActivityPoly, pgConn);
                    }

                    roadWorkActivityFeature.properties.uuid = Guid.NewGuid().ToString();

                    using NpgsqlTransaction trans = pgConn.BeginTransaction();

                    NpgsqlCommand insertComm = pgConn.CreateCommand();
                    insertComm.CommandText = @"INSERT INTO ""wtb_ssp_roadworkactivities""
                                    (uuid, name, projectmanager, traffic_agent, description,
                                    project_no, roadworkactivity_no, comment, session_comment_1, session_comment_2, section, type, projecttype, projectkind,
                                    overarching_measure, desired_year_from, desired_year_to, prestudy, 
                                    start_of_construction, end_of_construction, consult_due,
                                    created, last_modified, date_from, date_optimum, date_to,
                                    costs, costs_type, status, in_internet, billing_address1,
                                    billing_address2, investment_no, date_sks,
                                    date_kap, private, date_consult_start1, date_consult_end1,
                                    date_consult_start2, date_consult_end2, date_report_start, date_report_end,
                                    url, sks_relevant, strabako_no, date_sks_planned, geom)
                                    VALUES (@uuid, @name, @projectmanager, @traffic_agent,
                                    @description, @project_no, 
                                    (
                                        SELECT
                                            to_char(current_timestamp, 'YYYY')  -- year prefix
                                            || '_' ||
                                            (
                                                COALESCE(
                                                    MAX(split_part(r.roadworkactivity_no, '_', 2)::int),
                                                    0
                                                ) + 1
                                            )::text
                                        FROM wtb_ssp_roadworkactivities r
                                        WHERE split_part(r.roadworkactivity_no, '_', 1)
                                            = to_char(current_timestamp, 'YYYY')
                                    ),
                                    @comment, @session_comment_1, @session_comment_2, @section, @type, @projecttype, @projectkind,
                                    @overarching_measure, @desired_year_from, @desired_year_to, @prestudy, 
                                    @start_of_construction, @end_of_construction, @consult_due,
                                    current_timestamp, current_timestamp, @date_from, @date_optimum,
                                    @date_to, @costs, @costs_type, @status, @in_internet, @billing_address1,
                                    @billing_address2, @investment_no, @date_sks, @date_kap,
                                    @private, @date_consult_start1, @date_consult_end1, @date_consult_start2, @date_consult_end2,
                                    @date_report_start, @date_report_end, @url, @sks_relevant,
                                    @strabako_no, @date_sks_planned, @geom)";
                    insertComm.Parameters.AddWithValue("uuid", new Guid(roadWorkActivityFeature.properties.uuid));
                    if (roadWorkActivityFeature.properties.projectManager.uuid != "")
                    {
                        insertComm.Parameters.AddWithValue("projectmanager", new Guid(roadWorkActivityFeature.properties.projectManager.uuid));
                    }
                    else
                    {
                        insertComm.Parameters.AddWithValue("projectmanager", DBNull.Value);
                    }
                    if (roadWorkActivityFeature.properties.trafficAgent.uuid != "")
                    {
                        insertComm.Parameters.AddWithValue("traffic_agent", new Guid(roadWorkActivityFeature.properties.trafficAgent.uuid));
                    }
                    else
                    {
                        insertComm.Parameters.AddWithValue("traffic_agent", DBNull.Value);
                    }
                    insertComm.Parameters.AddWithValue("name", roadWorkActivityFeature.properties.name);
                    insertComm.Parameters.AddWithValue("description", roadWorkActivityFeature.properties.description);
                    insertComm.Parameters.AddWithValue("date_from", roadWorkActivityFeature.properties.finishEarlyTo != null ? roadWorkActivityFeature.properties.finishEarlyTo : DBNull.Value);
                    insertComm.Parameters.AddWithValue("date_optimum", roadWorkActivityFeature.properties.finishOptimumTo != null ? roadWorkActivityFeature.properties.finishOptimumTo : DBNull.Value);
                    insertComm.Parameters.AddWithValue("date_to", roadWorkActivityFeature.properties.finishLateTo != null ? roadWorkActivityFeature.properties.finishLateTo : DBNull.Value);
                    insertComm.Parameters.AddWithValue("costs", roadWorkActivityFeature.properties.costs <= 0 ? DBNull.Value : roadWorkActivityFeature.properties.costs);
                    insertComm.Parameters.AddWithValue("costs_type", roadWorkActivityFeature.properties.costsType);
                    insertComm.Parameters.AddWithValue("status", "review");
                    insertComm.Parameters.AddWithValue("in_internet", roadWorkActivityFeature.properties.isInInternet);
                    insertComm.Parameters.AddWithValue("billing_address1", roadWorkActivityFeature.properties.billingAddress1);
                    insertComm.Parameters.AddWithValue("billing_address2", roadWorkActivityFeature.properties.billingAddress2);
                    insertComm.Parameters.AddWithValue("investment_no", roadWorkActivityFeature.properties.investmentNo == 0 ? DBNull.Value : roadWorkActivityFeature.properties.investmentNo);
                    insertComm.Parameters.AddWithValue("comment", roadWorkActivityFeature.properties.comment);
                    insertComm.Parameters.AddWithValue("session_comment_1", roadWorkActivityFeature.properties.sessionComment1);
                    insertComm.Parameters.AddWithValue("session_comment_2", roadWorkActivityFeature.properties.sessionComment2);                    
                    insertComm.Parameters.AddWithValue("section", roadWorkActivityFeature.properties.section);
                    insertComm.Parameters.AddWithValue("type", roadWorkActivityFeature.properties.type);
                    insertComm.Parameters.AddWithValue("projecttype", roadWorkActivityFeature.properties.projectType == "" ?
                                        DBNull.Value : roadWorkActivityFeature.properties.projectType);
                    insertComm.Parameters.AddWithValue("projectkind", roadWorkActivityFeature.properties.projectKind == "" ?
                                        DBNull.Value : roadWorkActivityFeature.properties.projectKind);                                        
                    insertComm.Parameters.AddWithValue("overarching_measure", roadWorkActivityFeature.properties.overarchingMeasure);
                    insertComm.Parameters.AddWithValue("desired_year_from", roadWorkActivityFeature.properties.desiredYearFrom);
                    insertComm.Parameters.AddWithValue("desired_year_to", roadWorkActivityFeature.properties.desiredYearTo);
                    insertComm.Parameters.AddWithValue("prestudy", roadWorkActivityFeature.properties.prestudy);
                    insertComm.Parameters.AddWithValue("start_of_construction", roadWorkActivityFeature.properties.startOfConstruction != null ? roadWorkActivityFeature.properties.startOfConstruction : DBNull.Value);
                    insertComm.Parameters.AddWithValue("end_of_construction", roadWorkActivityFeature.properties.endOfConstruction != null ? roadWorkActivityFeature.properties.endOfConstruction : DBNull.Value);
                    insertComm.Parameters.AddWithValue("date_of_acceptance", roadWorkActivityFeature.properties.dateOfAcceptance != null ? roadWorkActivityFeature.properties.dateOfAcceptance : DBNull.Value);
                    insertComm.Parameters.AddWithValue("project_no", roadWorkActivityFeature.properties.projectNo != null ? roadWorkActivityFeature.properties.projectNo : DBNull.Value);
                    insertComm.Parameters.AddWithValue("roadworkactivity_no", roadWorkActivityFeature.properties.roadWorkActivityNo != null ? roadWorkActivityFeature.properties.roadWorkActivityNo : DBNull.Value);
                    insertComm.Parameters.AddWithValue("private", roadWorkActivityFeature.properties.isPrivate);
                    insertComm.Parameters.AddWithValue("date_sks_planned", roadWorkActivityFeature.properties.dateSksPlanned != null ? roadWorkActivityFeature.properties.dateSksPlanned : DBNull.Value);
                    insertComm.Parameters.AddWithValue("date_consult_start1", DateTime.Now.AddDays(7));
                    insertComm.Parameters.AddWithValue("date_consult_end1", DateTime.Now.AddDays(28));
                    insertComm.Parameters.AddWithValue("date_consult_start2", DateTime.Now.AddDays(35));
                    insertComm.Parameters.AddWithValue("date_consult_end2", DateTime.Now.AddDays(56));
                    insertComm.Parameters.AddWithValue("date_report_start", DateTime.Now.AddDays(63));
                    insertComm.Parameters.AddWithValue("date_report_end", DateTime.Now.AddDays(73));
                    insertComm.Parameters.AddWithValue("url", roadWorkActivityFeature.properties.url != null ? roadWorkActivityFeature.properties.url : DBNull.Value);
                    insertComm.Parameters.AddWithValue("sks_relevant", roadWorkActivityFeature.properties.isSksRelevant != null ? roadWorkActivityFeature.properties.isSksRelevant : DBNull.Value);
                    insertComm.Parameters.AddWithValue("strabako_no", roadWorkActivityFeature.properties.strabakoNo != null ? roadWorkActivityFeature.properties.strabakoNo : DBNull.Value);
                    insertComm.Parameters.AddWithValue("geom", roadWorkActivityPoly);

                    DateTime? nextKap = null;
                    DateTime nextKapTemp = DateTime.Now;
                    foreach (DateTime plannedDateKap in configurationData.plannedDatesKap)
                    {
                        if (plannedDateKap > nextKapTemp)
                        {
                            nextKap = plannedDateKap;
                            break;
                        }
                    }
                    insertComm.Parameters.AddWithValue("date_kap", nextKap == null ? DBNull.Value : nextKap);

                    DateTime? nextSks = null;
                    DateTime nextSksTemp = DateTime.Now.AddDays(66);
                    foreach (DateTime plannedDateSks in configurationData.plannedDatesSks)
                    {
                        if (plannedDateSks > nextSksTemp)
                        {
                            nextSks = plannedDateSks;
                            break;
                        }
                    }
                    insertComm.Parameters.AddWithValue("consult_due", nextSks == null ? DBNull.Value : nextSks);
                    insertComm.Parameters.AddWithValue("date_sks", nextSks == null ? DBNull.Value : nextSks);

                    insertComm.ExecuteNonQuery();

                    NpgsqlCommand insertInvolvedUsersComm;
                    foreach (User involvedUser in roadWorkActivityFeature.properties.involvedUsers)
                    {
                        insertInvolvedUsersComm = pgConn.CreateCommand();
                        insertInvolvedUsersComm.CommandText = @"INSERT INTO ""wtb_ssp_act_partic""
                                    VALUES(@uuid, @road_act, @participant)";
                        insertInvolvedUsersComm.Parameters.AddWithValue("uuid", Guid.NewGuid());
                        insertInvolvedUsersComm.Parameters.AddWithValue("road_act", new Guid(roadWorkActivityFeature.properties.uuid));
                        insertInvolvedUsersComm.Parameters.AddWithValue("participant", new Guid(involvedUser.uuid));
                        insertInvolvedUsersComm.ExecuteNonQuery();
                    }

                    ActivityHistoryItem activityHistoryItem = new ActivityHistoryItem();
                    activityHistoryItem.uuid = Guid.NewGuid().ToString();
                    activityHistoryItem.changeDate = DateTime.Now;
                    activityHistoryItem.who = userFromDb.firstName + " " + userFromDb.lastName;
                    if (roadWorkActivityFeature.properties.isPrivate != null &&
                                (bool)roadWorkActivityFeature.properties.isPrivate)
                        activityHistoryItem.what = "Bauvorhaben als Entwurf erstellt";
                    else activityHistoryItem.what = "Bauvorhaben erstellt und publiziert";
                    activityHistoryItem.userComment = "";

                    roadWorkActivityFeature.properties.activityHistory = new ActivityHistoryItem[1];
                    roadWorkActivityFeature.properties.activityHistory[0] = activityHistoryItem;

                    NpgsqlCommand insertHistoryComm = pgConn.CreateCommand();
                    insertHistoryComm.CommandText = @"INSERT INTO ""wtb_ssp_activities_history""
                            (uuid, uuid_roadwork_activity, changedate, who, what)
                            VALUES
                            (@uuid, @uuid_roadwork_activity, @changedate, @who, @what)";

                    insertHistoryComm.Parameters.AddWithValue("uuid", new Guid(activityHistoryItem.uuid));
                    insertHistoryComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadWorkActivityFeature.properties.uuid));
                    insertHistoryComm.Parameters.AddWithValue("changedate", activityHistoryItem.changeDate);
                    insertHistoryComm.Parameters.AddWithValue("who", activityHistoryItem.who);
                    insertHistoryComm.Parameters.AddWithValue("what", activityHistoryItem.what);

                    insertHistoryComm.ExecuteNonQuery();

                    if (roadWorkActivityFeature.properties.roadWorkNeedsUuids != null &&
                            roadWorkActivityFeature.properties.roadWorkNeedsUuids.Length != 0)
                    {
                        NpgsqlCommand insertRelationComm = pgConn.CreateCommand();
                        insertRelationComm.CommandText = @"INSERT INTO ""wtb_ssp_activities_to_needs""
                                        (uuid, uuid_roadwork_need, uuid_roadwork_activity, activityrelationtype)
                                        VALUES(@uuid, @uuid_roadwork_need, @uuid_roadwork_activity, 'assignedneed')";
                        insertRelationComm.Parameters.AddWithValue("uuid", Guid.NewGuid());
                        insertRelationComm.Parameters.AddWithValue("uuid_roadwork_need", new Guid(roadWorkActivityFeature.properties.roadWorkNeedsUuids[0]));
                        insertRelationComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadWorkActivityFeature.properties.uuid));
                        insertRelationComm.ExecuteNonQuery();

                        NpgsqlCommand updateNeedComm = pgConn.CreateCommand();
                        updateNeedComm.CommandText = @"UPDATE ""wtb_ssp_roadworkneeds""
                                    SET status='review'
                                    WHERE uuid=@uuid";
                        updateNeedComm.Parameters.AddWithValue("uuid", new Guid(roadWorkActivityFeature.properties.roadWorkNeedsUuids[0]));
                        updateNeedComm.ExecuteNonQuery();

                        insertHistoryComm = pgConn.CreateCommand();
                        insertHistoryComm.CommandText = @"INSERT INTO ""wtb_ssp_activities_history""
                                    (uuid, uuid_roadwork_activity, changedate, who, what)
                                    VALUES
                                    (@uuid, @uuid_roadwork_activity, @changedate, @who, @what)";

                        insertHistoryComm.Parameters.AddWithValue("uuid", Guid.NewGuid());
                        insertHistoryComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadWorkActivityFeature.properties.uuid));
                        insertHistoryComm.Parameters.AddWithValue("changedate", DateTime.Now);
                        insertHistoryComm.Parameters.AddWithValue("who", userFromDb.firstName + " " + userFromDb.lastName);
                        string whatText = "Der Bedarf '" + roadWorkActivityFeature.properties.roadWorkNeedsUuids[0] +
                                            "' wurde neu verknüpft. Die neue Verknüpfung ist: Zugewiesener Bedarf";
                        insertHistoryComm.Parameters.AddWithValue("what", whatText);

                        insertHistoryComm.ExecuteNonQuery();
                    }

                    trans.Commit();
                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                roadWorkActivityFeature = new RoadWorkActivityFeature();
                roadWorkActivityFeature.errorMessage = "SSP-3";
                return Ok(roadWorkActivityFeature);
            }

            return Ok(roadWorkActivityFeature);
        }

        // PUT roadworkactivity/
        [HttpPut]
        [Authorize(Roles = "territorymanager,administrator")]
        public ActionResult<RoadWorkActivityFeature> UpdateActivity([FromBody] RoadWorkActivityFeature roadWorkActivityFeature)
        {
            try
            {
                if (roadWorkActivityFeature == null)
                {
                    _logger.LogWarning("No roadworkactivity received in update activity method.");
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "SSP-3";
                    return Ok(roadWorkActivityFeature);
                }

                if (roadWorkActivityFeature.geometry == null ||
                        roadWorkActivityFeature.geometry.coordinates == null)
                {
                    _logger.LogWarning("Roadwork activity " + roadWorkActivityFeature.properties.uuid +
                            " has no geometry.");
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "SSP-3";
                    return Ok(roadWorkActivityFeature);
                }


                if (roadWorkActivityFeature.geometry.coordinates.Length < 3)
                {
                    _logger.LogWarning("Roadwork activity " + roadWorkActivityFeature.properties.uuid +
                            " has less than three coordinates.");
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "SSP-3";
                    return Ok(roadWorkActivityFeature);
                }

                Polygon roadWorkActivityPoly = roadWorkActivityFeature.geometry.getNtsPolygon();

                if (!roadWorkActivityPoly.IsSimple)
                {
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "SSP-10";
                    return Ok(roadWorkActivityFeature);
                }

                if (!roadWorkActivityPoly.IsValid)
                {
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "SSP-11";
                    return Ok(roadWorkActivityFeature);
                }

                ConfigurationData configData = AppConfigController.getConfigurationFromDb(false);
                // only if project area is greater than min area size:
                if (roadWorkActivityPoly.Area <= configData.minAreaSize)
                {
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "SSP-8";
                    return Ok(roadWorkActivityFeature);
                }

                // only if project area is smaller than max area size:
                if (roadWorkActivityPoly.Area > configData.maxAreaSize)
                {
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "SSP-16";
                    return Ok(roadWorkActivityFeature);
                }

                if (roadWorkActivityFeature.properties.finishEarlyTo! > roadWorkActivityFeature.properties.finishLateTo)
                {
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "SSP-19";
                    return Ok(roadWorkActivityFeature);
                }

                if ((bool)roadWorkActivityFeature.properties.isStudy &&
                        (roadWorkActivityFeature.properties.dateStudyStart == null ||
                        roadWorkActivityFeature.properties.dateStudyEnd == null))
                {
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "SSP-35";
                    return Ok(roadWorkActivityFeature);
                }

                if (roadWorkActivityFeature.properties.dateStudyStart >
                        roadWorkActivityFeature.properties.dateStudyEnd)
                {
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "SSP-36";
                    return Ok(roadWorkActivityFeature);
                }

                if (roadWorkActivityFeature.properties.isAggloprog != null &&
                        (bool)roadWorkActivityFeature.properties.isAggloprog)
                    roadWorkActivityFeature.properties.isSksRelevant = true;

                User userFromDb = LoginController.getAuthorizedUserFromDb(this.User, false);

                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {

                    pgConn.Open();

                    using NpgsqlTransaction trans = pgConn.BeginTransaction();

                    if (!User.IsInRole("administrator") && !User.IsInRole("territorymanager"))
                    {
                        NpgsqlCommand selectManagerOfActivityComm = pgConn.CreateCommand();
                        selectManagerOfActivityComm.CommandText = @"SELECT u.e_mail
                                    FROM ""wtb_ssp_roadworkactivities"" r
                                    LEFT JOIN ""wtb_ssp_users"" u ON r.projectmanager = u.uuid
                                    WHERE r.uuid=@uuid";
                        selectManagerOfActivityComm.Parameters.AddWithValue("uuid", new Guid(roadWorkActivityFeature.properties.uuid));

                        string eMailOfProjectManager = "";

                        using (NpgsqlDataReader reader = selectManagerOfActivityComm.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                eMailOfProjectManager = reader.IsDBNull(0) ? "" : reader.GetString(0);
                            }
                        }

                        string mailOfLoggedInUser = User.FindFirstValue(ClaimTypes.Email);

                        if (mailOfLoggedInUser != eMailOfProjectManager)
                        {
                            _logger.LogWarning("User " + mailOfLoggedInUser + " has no right to edit " +
                                "roadwork activity " + roadWorkActivityFeature.properties.uuid + " but tried " +
                                "to edit it.");
                            roadWorkActivityFeature.errorMessage = "SSP-31";
                            return Ok(roadWorkActivityFeature);
                        }

                    }

                    if (!(bool)roadWorkActivityFeature.properties.isPrivate)
                    {
                        int assignedNeedsCount = _countAssingedNeedsOfActivity(pgConn, roadWorkActivityFeature.properties.uuid);
                        if (assignedNeedsCount == 0)
                        {
                            _logger.LogWarning("The roadwork activity does not relate to at least one roadwork need");
                            roadWorkActivityFeature = new RoadWorkActivityFeature();
                            roadWorkActivityFeature.errorMessage = "SSP-28";
                            return Ok(roadWorkActivityFeature);
                        }
                    }

                    NpgsqlCommand selectStatusComm = pgConn.CreateCommand();
                    selectStatusComm.CommandText = @"SELECT status, project_study_approved,
                                                        study_approved, private FROM ""wtb_ssp_roadworkactivities""
                                                        WHERE uuid=@uuid";
                    selectStatusComm.Parameters.AddWithValue("uuid", new Guid(roadWorkActivityFeature.properties.uuid));

                    string statusOfActivityInDb = "";
                    DateTime? projectStudyApprovedInDb = null;
                    DateTime? studyApprovedInDb = null;
                    bool isPrivateInDb = false;
                    using (NpgsqlDataReader reader = selectStatusComm.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            statusOfActivityInDb = reader.IsDBNull(0) ? "" : reader.GetString(0);
                            projectStudyApprovedInDb = reader.IsDBNull(1) ? null : reader.GetDateTime(1);
                            studyApprovedInDb = reader.IsDBNull(2) ? null : reader.GetDateTime(2);
                            isPrivateInDb = reader.IsDBNull(3) ? false : reader.GetBoolean(3);
                        }
                    }

                    bool firstTimePublished = false;
                    if (isPrivateInDb)
                        if (roadWorkActivityFeature.properties.isPrivate == null ||
                            !(bool)roadWorkActivityFeature.properties.isPrivate)
                            firstTimePublished = true;

                    if (firstTimePublished)
                    {
                        ActivityHistoryItem activityHistoryItem = new ActivityHistoryItem();
                        activityHistoryItem.uuid = Guid.NewGuid().ToString();
                        activityHistoryItem.changeDate = DateTime.Now;
                        activityHistoryItem.who = userFromDb.firstName + " " + userFromDb.lastName;
                        activityHistoryItem.what = "Bauvorhaben publiziert";
                        activityHistoryItem.userComment = "";

                        NpgsqlCommand insertHistoryComm = pgConn.CreateCommand();
                        insertHistoryComm.CommandText = @"INSERT INTO ""wtb_ssp_activities_history""
                            (uuid, uuid_roadwork_activity, changedate, who, what)
                            VALUES
                            (@uuid, @uuid_roadwork_activity, @changedate, @who, @what)";

                        insertHistoryComm.Parameters.AddWithValue("uuid", new Guid(activityHistoryItem.uuid));
                        insertHistoryComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadWorkActivityFeature.properties.uuid));
                        insertHistoryComm.Parameters.AddWithValue("changedate", activityHistoryItem.changeDate);
                        insertHistoryComm.Parameters.AddWithValue("who", activityHistoryItem.who);
                        insertHistoryComm.Parameters.AddWithValue("what", activityHistoryItem.what);

                        insertHistoryComm.ExecuteNonQuery();
                    }

                    bool hasStatusChanged = false;
                    if (statusOfActivityInDb != null && statusOfActivityInDb.Length != 0)
                    {
                        hasStatusChanged = statusOfActivityInDb != roadWorkActivityFeature.properties.status;
                    }

                    if (projectStudyApprovedInDb == null
                                && roadWorkActivityFeature.properties.projectStudyApproved != null)
                    {
                        if (hasStatusChanged)
                        {
                            roadWorkActivityFeature.properties.projectStudyApproved = null;
                        }
                        else
                        {
                            if (roadWorkActivityFeature.properties.status != "coordinated")
                            {
                                roadWorkActivityFeature = new RoadWorkActivityFeature();
                                roadWorkActivityFeature.errorMessage = "SSP-37";
                                return Ok(roadWorkActivityFeature);
                            }
                            roadWorkActivityFeature.properties.status = "prestudy";
                            hasStatusChanged = true;
                        }
                    }

                    if (studyApprovedInDb == null
                                && roadWorkActivityFeature.properties.studyApproved != null)
                    {
                        if (hasStatusChanged)
                        {
                            roadWorkActivityFeature.properties.studyApproved = null;
                        }
                        else
                        {
                            roadWorkActivityFeature.properties.status = "coordinated";
                            hasStatusChanged = true;
                        }
                    }

                    if (hasStatusChanged && (bool)roadWorkActivityFeature.properties.isPrivate)
                    {
                        _logger.LogWarning("The process status of a private (draft) roadwork activity has been tried to change. " +
                                    "This is not allowed");
                        roadWorkActivityFeature = new RoadWorkActivityFeature();
                        roadWorkActivityFeature.errorMessage = "SSP-32";
                        return Ok(roadWorkActivityFeature);
                    }

                    if (hasStatusChanged &&
                        !_isStatusChangeAllowed(statusOfActivityInDb, roadWorkActivityFeature.properties.status))
                    {
                        _logger.LogWarning("User tried to change the status of a roadwork activity" +
                                    " in a way that is not allowed");
                        roadWorkActivityFeature = new RoadWorkActivityFeature();
                        roadWorkActivityFeature.errorMessage = "SSP-34";
                        return Ok(roadWorkActivityFeature);
                    }

                    NpgsqlCommand selectMainAttributeValues = pgConn.CreateCommand();
                    selectMainAttributeValues.CommandText = @"SELECT projectmanager,
                                    projecttype, name, section, costs
                                    FROM ""wtb_ssp_roadworkactivities""
                                    WHERE uuid=@uuid";
                    selectMainAttributeValues.Parameters.AddWithValue("uuid", new Guid(roadWorkActivityFeature.properties.uuid));

                    string projectManagerInDb = "";
                    string projectTypeInDb = "";
                    string nameInDb = "";
                    string sectionInDb = "";
                    decimal? costsInDb = null;
                    using (NpgsqlDataReader reader = selectMainAttributeValues.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if (!reader.IsDBNull(0)) projectManagerInDb = reader.GetGuid(0).ToString();
                            if (!reader.IsDBNull(1)) projectTypeInDb = reader.GetString(1);
                            if (!reader.IsDBNull(2)) nameInDb = reader.GetString(2);
                            if (!reader.IsDBNull(3)) sectionInDb = reader.GetString(3);
                            if (!reader.IsDBNull(4)) costsInDb = reader.GetDecimal(4);
                        }
                    }

                    if (roadWorkActivityFeature.properties.projectManager.uuid != projectManagerInDb)
                    {
                        NpgsqlCommand insertHistoryComm = pgConn.CreateCommand();
                        insertHistoryComm.CommandText = @"INSERT INTO ""wtb_ssp_activities_history""
                                    (uuid, uuid_roadwork_activity, changedate, who, what)
                                    VALUES
                                    (@uuid, @uuid_roadwork_activity, @changedate, @who, @what)";

                        insertHistoryComm.Parameters.AddWithValue("uuid", Guid.NewGuid());
                        insertHistoryComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadWorkActivityFeature.properties.uuid));
                        insertHistoryComm.Parameters.AddWithValue("changedate", DateTime.Now);
                        insertHistoryComm.Parameters.AddWithValue("who", userFromDb.firstName + " " + userFromDb.lastName);
                        string whatText = "Die Projektleitung des Bauvorhabens wurde geändert.";
                        insertHistoryComm.Parameters.AddWithValue("what", whatText);
                        insertHistoryComm.ExecuteNonQuery();
                    }

                    if (roadWorkActivityFeature.properties.projectType != projectTypeInDb)
                    {
                        NpgsqlCommand insertHistoryComm = pgConn.CreateCommand();
                        insertHistoryComm.CommandText = @"INSERT INTO ""wtb_ssp_activities_history""
                                    (uuid, uuid_roadwork_activity, changedate, who, what)
                                    VALUES
                                    (@uuid, @uuid_roadwork_activity, @changedate, @who, @what)";

                        insertHistoryComm.Parameters.AddWithValue("uuid", Guid.NewGuid());
                        insertHistoryComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadWorkActivityFeature.properties.uuid));
                        insertHistoryComm.Parameters.AddWithValue("changedate", DateTime.Now);
                        insertHistoryComm.Parameters.AddWithValue("who", userFromDb.firstName + " " + userFromDb.lastName);
                        string whatText = "Der Projekttyp des Bauvorhabens wurde geändert.";
                        insertHistoryComm.Parameters.AddWithValue("what", whatText);
                        insertHistoryComm.ExecuteNonQuery();
                    }

                    if (roadWorkActivityFeature.properties.name != nameInDb)
                    {
                        NpgsqlCommand insertHistoryComm = pgConn.CreateCommand();
                        insertHistoryComm.CommandText = @"INSERT INTO ""wtb_ssp_activities_history""
                                    (uuid, uuid_roadwork_activity, changedate, who, what)
                                    VALUES
                                    (@uuid, @uuid_roadwork_activity, @changedate, @who, @what)";

                        insertHistoryComm.Parameters.AddWithValue("uuid", Guid.NewGuid());
                        insertHistoryComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadWorkActivityFeature.properties.uuid));
                        insertHistoryComm.Parameters.AddWithValue("changedate", DateTime.Now);
                        insertHistoryComm.Parameters.AddWithValue("who", userFromDb.firstName + " " + userFromDb.lastName);
                        string whatText = "Titel/Strasse des Bauvorhabens wurde geändert.";
                        insertHistoryComm.Parameters.AddWithValue("what", whatText);
                        insertHistoryComm.ExecuteNonQuery();
                    }

                    if (roadWorkActivityFeature.properties.section != sectionInDb)
                    {
                        NpgsqlCommand insertHistoryComm = pgConn.CreateCommand();
                        insertHistoryComm.CommandText = @"INSERT INTO ""wtb_ssp_activities_history""
                                    (uuid, uuid_roadwork_activity, changedate, who, what)
                                    VALUES
                                    (@uuid, @uuid_roadwork_activity, @changedate, @who, @what)";

                        insertHistoryComm.Parameters.AddWithValue("uuid", Guid.NewGuid());
                        insertHistoryComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadWorkActivityFeature.properties.uuid));
                        insertHistoryComm.Parameters.AddWithValue("changedate", DateTime.Now);
                        insertHistoryComm.Parameters.AddWithValue("who", userFromDb.firstName + " " + userFromDb.lastName);
                        string whatText = "Abschnitt des Bauvorhabens wurde geändert.";
                        insertHistoryComm.Parameters.AddWithValue("what", whatText);
                        insertHistoryComm.ExecuteNonQuery();
                    }

                    NpgsqlCommand selectInvolvedUsers = pgConn.CreateCommand();
                    selectInvolvedUsers.CommandText = @"SELECT participant
                                    FROM ""wtb_ssp_act_partic""
                                    WHERE road_act=@road_act_uuid";
                    selectInvolvedUsers.Parameters.AddWithValue("road_act_uuid", new Guid(roadWorkActivityFeature.properties.uuid));

                    List<string> involvedUsersUuidsFromDb = new List<string>();
                    using (NpgsqlDataReader reader = selectInvolvedUsers.ExecuteReader())
                        while (reader.Read())
                            if (!reader.IsDBNull(0)) involvedUsersUuidsFromDb.Add(reader.GetGuid(0).ToString());

                    bool involvedUsersHaveChanged = false;
                    if (involvedUsersUuidsFromDb.Count != roadWorkActivityFeature.properties.involvedUsers.Length)
                        involvedUsersHaveChanged = true;
                    else
                    {
                        foreach (User involvedUser in roadWorkActivityFeature.properties.involvedUsers)
                        {
                            if (!involvedUsersUuidsFromDb.Contains(involvedUser.uuid))
                            {
                                involvedUsersHaveChanged = true;
                                break;
                            }
                        }
                    }

                    if (involvedUsersHaveChanged)
                    {
                        NpgsqlCommand insertHistoryComm = pgConn.CreateCommand();
                        insertHistoryComm.CommandText = @"INSERT INTO ""wtb_ssp_activities_history""
                                    (uuid, uuid_roadwork_activity, changedate, who, what)
                                    VALUES
                                    (@uuid, @uuid_roadwork_activity, @changedate, @who, @what)";

                        insertHistoryComm.Parameters.AddWithValue("uuid", Guid.NewGuid());
                        insertHistoryComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadWorkActivityFeature.properties.uuid));
                        insertHistoryComm.Parameters.AddWithValue("changedate", DateTime.Now);
                        insertHistoryComm.Parameters.AddWithValue("who", userFromDb.firstName + " " + userFromDb.lastName);
                        string whatText = "Die Beteiligten des Bauvorhabens wurden geändert.";
                        insertHistoryComm.Parameters.AddWithValue("what", whatText);
                        insertHistoryComm.ExecuteNonQuery();
                    }

                    NpgsqlCommand updateComm = pgConn.CreateCommand();
                    updateComm.CommandText = @"UPDATE ""wtb_ssp_roadworkactivities""
                                    SET name=@name, projectmanager=@projectmanager,
                                    traffic_agent=@traffic_agent, description=@description,
                                    comment=@comment, session_comment_1=@session_comment_1, session_comment_2=@session_comment_2, section=@section, type=@type,
                                    projecttype=@projecttype, projectkind=@projectkind, overarching_measure=@overarching_measure,
                                    desired_year_from=@desired_year_from, desired_year_to=@desired_year_to, prestudy=@prestudy, 
                                    start_of_construction=@start_of_construction, date_of_acceptance=@date_of_acceptance,
                                    end_of_construction=@end_of_construction, consult_due=@consult_due,
                                    last_modified=@last_modified, project_no=@project_no, roadworkactivity_no=@roadworkactivity_no,
                                    date_from=@date_from, date_optimum=@date_optimum, date_to=@date_to,
                                    costs=@costs, costs_type=@costs_type, status=@status,
                                    billing_address1=@billing_address1,
                                    billing_address2=@billing_address2, url=@url,
                                    in_internet=@in_internet, investment_no=@investment_no,
                                    date_sks_real=@date_sks_real, date_kap_real=@date_kap_real, date_oks_real=@date_oks_real,
                                    date_gl_tba=@date_gl_tba, date_gl_tba_real=@date_gl_tba_real, private=@private,
                                    date_accept=@date_accept, date_guarantee=@date_guarantee,
                                    is_study=@is_study, date_study_start=@date_study_start,
                                    date_study_end=@date_study_end, is_desire=@is_desire,
                                    date_desire_start=@date_desire_start, date_desire_end=@date_desire_end,
                                    is_particip=@is_particip, date_particip_start=@date_particip_start,
                                    date_particip_end=@date_particip_end, is_plan_circ=@is_plan_circ,
                                    date_plan_circ_start=@date_plan_circ_start,
                                    date_plan_circ_end=@date_plan_circ_end,
                                    date_consult_start1=@date_consult_start1, date_consult_end1=@date_consult_end1,
                                    date_consult_start2=@date_consult_start2, date_consult_end2=@date_consult_end2,
                                    date_consult_close=@date_consult_close,
                                    date_report_start=@date_report_start,
                                    date_report_end=@date_report_end, date_report_close=@date_report_close,
                                    date_info_start=@date_info_start, date_info_end=@date_info_end,
                                    date_info_close=@date_info_close, is_aggloprog=@is_aggloprog, is_traffic_regulation_required=@is_traffic_regulation_required,
                                    project_study_approved=@project_study_approved, study_approved=@study_approved,
                                    sks_relevant=@sks_relevant, strabako_no=@strabako_no, date_sks_planned=@date_sks_planned,";

                    if (costsInDb != roadWorkActivityFeature.properties.costs){
                        updateComm.CommandText += "costs_last_modified=@costs_last_modified, ";
                        updateComm.CommandText += "costs_last_modified_by=@costs_last_modified_by, ";
                    }

                    if (hasStatusChanged)
                    {
                        if (roadWorkActivityFeature.properties.status == "inconsult1")
                            updateComm.CommandText += "date_start_inconsult1=@date_start_inconsult1, ";
                        else if (roadWorkActivityFeature.properties.status == "inconsult2")
                            updateComm.CommandText += "date_start_inconsult2=@date_start_inconsult2, ";
                        else if (roadWorkActivityFeature.properties.status == "verified1")
                            updateComm.CommandText += "date_start_verified1=@date_start_verified1, ";
                        else if (roadWorkActivityFeature.properties.status == "verified2")
                            updateComm.CommandText += "date_start_verified2=@date_start_verified2, ";
                        else if (roadWorkActivityFeature.properties.status == "reporting")
                            updateComm.CommandText += "date_start_reporting=@date_start_reporting, ";
                        else if (roadWorkActivityFeature.properties.status == "suspended")
                            updateComm.CommandText += "date_start_suspended=@date_start_suspended, ";
                        else if (roadWorkActivityFeature.properties.status == "coordinated")
                            updateComm.CommandText += "date_start_coordinated=@date_start_coordinated, ";
                    }

                    // if we are going one step back (from status "verified1" to status "inconsult1")
                    // than delete timestamp:
                    if (statusOfActivityInDb == "verified1" &&
                            roadWorkActivityFeature.properties.status == "inconsult1")
                    {
                        updateComm.CommandText += "date_start_verified1=NULL, ";
                    }

                    // if we are going one step back (from status "verified2" to status "inconsult2")
                    // than delete timestamp:
                    if (statusOfActivityInDb == "verified2" &&
                            roadWorkActivityFeature.properties.status == "inconsult2")
                    {
                        updateComm.CommandText += "date_start_verified2=NULL, ";
                    }

                    // if we are going one step back (from status "coordinated" to status "reporting")
                    // than delete timestamp:
                    if (statusOfActivityInDb == "coordinated" &&
                            roadWorkActivityFeature.properties.status == "reporting")
                    {
                        updateComm.CommandText += "date_start_coordinated=NULL, ";
                    }

                    updateComm.CommandText += "geom=@geom WHERE uuid=@uuid";

                    updateComm.Parameters.AddWithValue("name", roadWorkActivityFeature.properties.name);
                    if (roadWorkActivityFeature.properties.projectManager.uuid != "")
                    {
                        updateComm.Parameters.AddWithValue("projectmanager",
                                new Guid(roadWorkActivityFeature.properties.projectManager.uuid));
                    }
                    else
                    {
                        updateComm.Parameters.AddWithValue("projectmanager", DBNull.Value);
                    }
                    if (roadWorkActivityFeature.properties.trafficAgent.uuid != "")
                    {
                        updateComm.Parameters.AddWithValue("traffic_agent",
                                new Guid(roadWorkActivityFeature.properties.trafficAgent.uuid));
                    }
                    else
                    {
                        updateComm.Parameters.AddWithValue("traffic_agent", DBNull.Value);
                    }
                    updateComm.Parameters.AddWithValue("description", roadWorkActivityFeature.properties.description);
                    roadWorkActivityFeature.properties.lastModified = DateTime.Now;
                    updateComm.Parameters.AddWithValue("last_modified", roadWorkActivityFeature.properties.lastModified);
                    updateComm.Parameters.AddWithValue("date_from", roadWorkActivityFeature.properties.finishEarlyTo != null ? roadWorkActivityFeature.properties.finishEarlyTo : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_optimum", roadWorkActivityFeature.properties.finishOptimumTo != null ? roadWorkActivityFeature.properties.finishOptimumTo : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_to", roadWorkActivityFeature.properties.finishLateTo != null ? roadWorkActivityFeature.properties.finishLateTo : DBNull.Value);
                    updateComm.Parameters.AddWithValue("costs", roadWorkActivityFeature.properties.costs);
                    updateComm.Parameters.AddWithValue("costs_type", roadWorkActivityFeature.properties.costsType);
                    updateComm.Parameters.AddWithValue("status", roadWorkActivityFeature.properties.status);
                    updateComm.Parameters.AddWithValue("in_internet", roadWorkActivityFeature.properties.isInInternet);
                    updateComm.Parameters.AddWithValue("billing_address1", roadWorkActivityFeature.properties.billingAddress1);
                    updateComm.Parameters.AddWithValue("billing_address2", roadWorkActivityFeature.properties.billingAddress2);
                    updateComm.Parameters.AddWithValue("url", roadWorkActivityFeature.properties.url != null ? roadWorkActivityFeature.properties.url : DBNull.Value);
                    updateComm.Parameters.AddWithValue("investment_no", roadWorkActivityFeature.properties.investmentNo);
                    updateComm.Parameters.AddWithValue("date_sks_real", roadWorkActivityFeature.properties.dateSksReal != null ? roadWorkActivityFeature.properties.dateSksReal : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_sks_planned", roadWorkActivityFeature.properties.dateSksPlanned != null ? roadWorkActivityFeature.properties.dateSksPlanned : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_kap_real", roadWorkActivityFeature.properties.dateKapReal != null ? roadWorkActivityFeature.properties.dateKapReal : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_oks_real", roadWorkActivityFeature.properties.dateOksReal != null ? roadWorkActivityFeature.properties.dateOksReal : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_gl_tba", roadWorkActivityFeature.properties.dateGlTba != null ? roadWorkActivityFeature.properties.dateGlTba : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_gl_tba_real", roadWorkActivityFeature.properties.dateGlTbaReal != null ? roadWorkActivityFeature.properties.dateGlTbaReal : DBNull.Value);
                    updateComm.Parameters.AddWithValue("comment", roadWorkActivityFeature.properties.comment);
                    updateComm.Parameters.AddWithValue("session_comment_1", roadWorkActivityFeature.properties.sessionComment1);
                    updateComm.Parameters.AddWithValue("session_comment_2", roadWorkActivityFeature.properties.sessionComment2);                    
                    updateComm.Parameters.AddWithValue("section", roadWorkActivityFeature.properties.section);
                    updateComm.Parameters.AddWithValue("type", roadWorkActivityFeature.properties.type);
                    updateComm.Parameters.AddWithValue("projecttype", roadWorkActivityFeature.properties.projectType == "" ?
                                            DBNull.Value : roadWorkActivityFeature.properties.projectType);
                    updateComm.Parameters.AddWithValue("projectkind", roadWorkActivityFeature.properties.projectKind == "" ?
                                            DBNull.Value : roadWorkActivityFeature.properties.projectKind);
                    updateComm.Parameters.AddWithValue("overarching_measure", roadWorkActivityFeature.properties.overarchingMeasure);
                    updateComm.Parameters.AddWithValue("desired_year_from", roadWorkActivityFeature.properties.desiredYearFrom);
                    updateComm.Parameters.AddWithValue("desired_year_to", roadWorkActivityFeature.properties.desiredYearTo);
                    updateComm.Parameters.AddWithValue("prestudy", roadWorkActivityFeature.properties.prestudy);
                    updateComm.Parameters.AddWithValue("start_of_construction", roadWorkActivityFeature.properties.startOfConstruction != null ? roadWorkActivityFeature.properties.startOfConstruction : DBNull.Value);
                    updateComm.Parameters.AddWithValue("end_of_construction", roadWorkActivityFeature.properties.endOfConstruction != null ? roadWorkActivityFeature.properties.endOfConstruction : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_of_acceptance", roadWorkActivityFeature.properties.dateOfAcceptance != null ? roadWorkActivityFeature.properties.dateOfAcceptance : DBNull.Value);
                    updateComm.Parameters.AddWithValue("consult_due", roadWorkActivityFeature.properties.consultDue);
                    updateComm.Parameters.AddWithValue("project_no", roadWorkActivityFeature.properties.projectNo != null ? roadWorkActivityFeature.properties.projectNo : DBNull.Value);
                    updateComm.Parameters.AddWithValue("roadworkactivity_no", roadWorkActivityFeature.properties.roadWorkActivityNo != null ? roadWorkActivityFeature.properties.roadWorkActivityNo : DBNull.Value);
                    updateComm.Parameters.AddWithValue("private", roadWorkActivityFeature.properties.isPrivate);
                    updateComm.Parameters.AddWithValue("date_accept", roadWorkActivityFeature.properties.dateAccept != null ? roadWorkActivityFeature.properties.dateAccept : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_guarantee", roadWorkActivityFeature.properties.dateGuarantee != null ? roadWorkActivityFeature.properties.dateGuarantee : DBNull.Value);
                    updateComm.Parameters.AddWithValue("is_study", roadWorkActivityFeature.properties.isStudy);
                    updateComm.Parameters.AddWithValue("date_study_start", roadWorkActivityFeature.properties.dateStudyStart != null ? roadWorkActivityFeature.properties.dateStudyStart : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_study_end", roadWorkActivityFeature.properties.dateStudyEnd != null ? roadWorkActivityFeature.properties.dateStudyEnd : DBNull.Value);
                    updateComm.Parameters.AddWithValue("project_study_approved", roadWorkActivityFeature.properties.projectStudyApproved != null ? roadWorkActivityFeature.properties.projectStudyApproved : DBNull.Value);
                    updateComm.Parameters.AddWithValue("study_approved", roadWorkActivityFeature.properties.studyApproved != null ? roadWorkActivityFeature.properties.studyApproved : DBNull.Value);
                    updateComm.Parameters.AddWithValue("is_desire", roadWorkActivityFeature.properties.isDesire);
                    updateComm.Parameters.AddWithValue("date_desire_start", roadWorkActivityFeature.properties.dateDesireStart != null ? roadWorkActivityFeature.properties.dateDesireStart : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_desire_end", roadWorkActivityFeature.properties.dateDesireEnd != null ? roadWorkActivityFeature.properties.dateDesireEnd : DBNull.Value);
                    updateComm.Parameters.AddWithValue("is_particip", roadWorkActivityFeature.properties.isParticip);
                    updateComm.Parameters.AddWithValue("date_particip_start", roadWorkActivityFeature.properties.dateParticipStart != null ? roadWorkActivityFeature.properties.dateParticipStart : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_particip_end", roadWorkActivityFeature.properties.dateParticipEnd != null ? roadWorkActivityFeature.properties.dateParticipEnd : DBNull.Value);
                    updateComm.Parameters.AddWithValue("is_plan_circ", roadWorkActivityFeature.properties.isPlanCirc);
                    updateComm.Parameters.AddWithValue("date_plan_circ_start", roadWorkActivityFeature.properties.datePlanCircStart != null ? roadWorkActivityFeature.properties.datePlanCircStart : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_plan_circ_end", roadWorkActivityFeature.properties.datePlanCircEnd != null ? roadWorkActivityFeature.properties.datePlanCircEnd : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_consult_start1", roadWorkActivityFeature.properties.dateConsultStart1 != null ? roadWorkActivityFeature.properties.dateConsultStart1 : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_consult_end1", roadWorkActivityFeature.properties.dateConsultEnd1 != null ? roadWorkActivityFeature.properties.dateConsultEnd1 : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_consult_start2", roadWorkActivityFeature.properties.dateConsultStart2 != null ? roadWorkActivityFeature.properties.dateConsultStart2 : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_consult_end2", roadWorkActivityFeature.properties.dateConsultEnd2 != null ? roadWorkActivityFeature.properties.dateConsultEnd2 : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_consult_close", roadWorkActivityFeature.properties.dateConsultClose != null ? roadWorkActivityFeature.properties.dateConsultClose : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_report_start", roadWorkActivityFeature.properties.dateReportStart != null ? roadWorkActivityFeature.properties.dateReportStart : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_report_end", roadWorkActivityFeature.properties.dateReportEnd != null ? roadWorkActivityFeature.properties.dateReportEnd : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_report_close", roadWorkActivityFeature.properties.dateReportClose != null ? roadWorkActivityFeature.properties.dateReportClose : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_info_start", roadWorkActivityFeature.properties.dateInfoStart != null ? roadWorkActivityFeature.properties.dateInfoStart : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_info_end", roadWorkActivityFeature.properties.dateInfoEnd != null ? roadWorkActivityFeature.properties.dateInfoEnd : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_info_close", roadWorkActivityFeature.properties.dateInfoClose != null ? roadWorkActivityFeature.properties.dateInfoClose : DBNull.Value);
                    updateComm.Parameters.AddWithValue("is_aggloprog", roadWorkActivityFeature.properties.isAggloprog != null ? roadWorkActivityFeature.properties.isAggloprog : DBNull.Value);
                    updateComm.Parameters.AddWithValue("is_traffic_regulation_required", roadWorkActivityFeature.properties.isTrafficRegulationRequired != null ? roadWorkActivityFeature.properties.isTrafficRegulationRequired : DBNull.Value);
                    updateComm.Parameters.AddWithValue("sks_relevant", roadWorkActivityFeature.properties.isSksRelevant != null ? roadWorkActivityFeature.properties.isSksRelevant : DBNull.Value);
                    updateComm.Parameters.AddWithValue("strabako_no", roadWorkActivityFeature.properties.strabakoNo != null ? roadWorkActivityFeature.properties.strabakoNo : DBNull.Value);
                    if (costsInDb != roadWorkActivityFeature.properties.costs)
                    {
                        updateComm.Parameters.AddWithValue("costs_last_modified", DateTime.Now);
                        updateComm.Parameters.AddWithValue("costs_last_modified_by", new Guid(userFromDb.uuid));
                    }
                    updateComm.Parameters.AddWithValue("geom", roadWorkActivityPoly);
                    updateComm.Parameters.AddWithValue("uuid", new Guid(roadWorkActivityFeature.properties.uuid));

                    if (hasStatusChanged)
                    {
                        if (roadWorkActivityFeature.properties.status == "inconsult1")
                            updateComm.Parameters.AddWithValue("date_start_inconsult1", DateTime.Now);
                        else if (roadWorkActivityFeature.properties.status == "inconsult2")
                            updateComm.Parameters.AddWithValue("date_start_inconsult2", DateTime.Now);
                        else if (roadWorkActivityFeature.properties.status == "verified1")
                            updateComm.Parameters.AddWithValue("date_start_verified1", DateTime.Now);
                        else if (roadWorkActivityFeature.properties.status == "verified2")
                            updateComm.Parameters.AddWithValue("date_start_verified2", DateTime.Now);
                        else if (roadWorkActivityFeature.properties.status == "reporting")
                            updateComm.Parameters.AddWithValue("date_start_reporting", DateTime.Now);
                        else if (roadWorkActivityFeature.properties.status == "suspended")
                            updateComm.Parameters.AddWithValue("date_start_suspended", DateTime.Now);
                        else if (roadWorkActivityFeature.properties.status == "coordinated")
                            updateComm.Parameters.AddWithValue("date_start_coordinated", DateTime.Now);
                    }

                    updateComm.ExecuteNonQuery();

                    NpgsqlCommand deleteInvolvedUsersComm = pgConn.CreateCommand();
                    deleteInvolvedUsersComm.CommandText = @"DELETE FROM ""wtb_ssp_act_partic""
                                    WHERE road_act=@uuid";
                    deleteInvolvedUsersComm.Parameters.AddWithValue("uuid", new Guid(roadWorkActivityFeature.properties.uuid));
                    deleteInvolvedUsersComm.ExecuteNonQuery();

                    NpgsqlCommand insertInvolvedUsersComm;
                    foreach (User involvedUser in roadWorkActivityFeature.properties.involvedUsers)
                    {
                        insertInvolvedUsersComm = pgConn.CreateCommand();
                        insertInvolvedUsersComm.CommandText = @"INSERT INTO ""wtb_ssp_act_partic""
                                    VALUES(@uuid, @road_act, @participant)";
                        insertInvolvedUsersComm.Parameters.AddWithValue("uuid", Guid.NewGuid());
                        insertInvolvedUsersComm.Parameters.AddWithValue("road_act", new Guid(roadWorkActivityFeature.properties.uuid));
                        insertInvolvedUsersComm.Parameters.AddWithValue("participant", new Guid(involvedUser.uuid));
                        insertInvolvedUsersComm.ExecuteNonQuery();
                    }

                    if (hasStatusChanged)
                    {
                        NpgsqlCommand insertHistoryComm = pgConn.CreateCommand();
                        insertHistoryComm.CommandText = @"INSERT INTO ""wtb_ssp_activities_history""
                                    (uuid, uuid_roadwork_activity, changedate, who, what)
                                    VALUES
                                    (@uuid, @uuid_roadwork_activity, @changedate, @who, @what)";

                        insertHistoryComm.Parameters.AddWithValue("uuid", Guid.NewGuid());
                        insertHistoryComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadWorkActivityFeature.properties.uuid));
                        insertHistoryComm.Parameters.AddWithValue("changedate", DateTime.Now);
                        insertHistoryComm.Parameters.AddWithValue("who", userFromDb.firstName + " " + userFromDb.lastName);
                        string whatText = "Status des Bauvorhabens wurde geändert zu: ";
                        if (roadWorkActivityFeature.properties.status == "review")
                            whatText += "in Prüfung";
                        else if (roadWorkActivityFeature.properties.status == "inconsult1")
                            whatText += "in Bedarfsklärung-1";
                        else if (roadWorkActivityFeature.properties.status == "inconsult2")
                            whatText += "in Bedarfsklärung-2";
                        else if (roadWorkActivityFeature.properties.status == "verified1")
                            whatText += "verifiziert-1";
                        else if (roadWorkActivityFeature.properties.status == "verified2")
                            whatText += "verifiziert-2";
                        else if (roadWorkActivityFeature.properties.status == "reporting")
                            whatText += "in Stellungnahme";
                        else if (roadWorkActivityFeature.properties.status == "coordinated")
                            whatText += "koordiniert";
                        insertHistoryComm.Parameters.AddWithValue("what", whatText);
                        insertHistoryComm.ExecuteNonQuery();

                        NpgsqlCommand updateActivityStatusComm = pgConn.CreateCommand();
                        updateActivityStatusComm.CommandText = @"UPDATE wtb_ssp_roadworkactivities
                                                    SET status=@status WHERE uuid=@uuid";
                        updateActivityStatusComm.Parameters.AddWithValue("status", roadWorkActivityFeature.properties.status);
                        updateActivityStatusComm.Parameters.AddWithValue("uuid", new Guid(roadWorkActivityFeature.properties.uuid));
                        updateActivityStatusComm.ExecuteNonQuery();

                        if (roadWorkActivityFeature.properties.status == "inconsult1" ||
                            roadWorkActivityFeature.properties.status == "inconsult2" ||
                            roadWorkActivityFeature.properties.status == "reporting")
                        {
                            foreach (String involvedNeedUuid in roadWorkActivityFeature.properties.roadWorkNeedsUuids)
                            {
                                NpgsqlCommand insertEmptyCommentsComm = pgConn.CreateCommand();
                                insertEmptyCommentsComm.CommandText = @"INSERT INTO ""wtb_ssp_activity_consult""
                                    (uuid, uuid_roadwork_activity, input_by, feedback_phase, feedback_given, uuid_roadwork_need, last_edit_by)
                                    VALUES
                                    (@uuid, @uuid_roadwork_activity, (SELECT orderer FROM wtb_ssp_roadworkneeds WHERE uuid = @uuid_roadwork_need), @feedback_phase, @feedback_given, @uuid_roadwork_need, @last_edit_by)";

                                insertEmptyCommentsComm.Parameters.AddWithValue("uuid", Guid.NewGuid());
                                insertEmptyCommentsComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadWorkActivityFeature.properties.uuid));                                
                                insertEmptyCommentsComm.Parameters.AddWithValue("feedback_phase", roadWorkActivityFeature.properties.status);
                                insertEmptyCommentsComm.Parameters.AddWithValue("feedback_given", false);

                                insertEmptyCommentsComm.Parameters.AddWithValue("uuid_roadwork_need", new Guid(involvedNeedUuid));
                                insertEmptyCommentsComm.Parameters.AddWithValue("last_edit_by", new Guid(userFromDb.uuid));
                                

                                insertEmptyCommentsComm.ExecuteNonQuery();
                            }
                        }

                        if (roadWorkActivityFeature.properties.status == "verified1")
                        {
                            NpgsqlCommand updateNeedsStatusComm = pgConn.CreateCommand();
                            updateNeedsStatusComm.CommandText = @"UPDATE ""wtb_ssp_roadworkneeds""
                                                    SET status='verified1'
                                                    WHERE uuid IN
                                                    (SELECT n.uuid
                                                        FROM ""wtb_ssp_roadworkneeds"" n
                                                        LEFT JOIN ""wtb_ssp_activities_to_needs"" an ON an.uuid_roadwork_need = n.uuid
                                                        WHERE an.activityrelationtype='assignedneed'
                                                        AND an.uuid_roadwork_activity=@uuid_roadwork_activity)";
                            updateNeedsStatusComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadWorkActivityFeature.properties.uuid));
                            updateNeedsStatusComm.ExecuteNonQuery();
                        }
                        else if (roadWorkActivityFeature.properties.status == "verified2")
                        {
                            NpgsqlCommand updateNeedsStatusComm = pgConn.CreateCommand();
                            updateNeedsStatusComm.CommandText = @"UPDATE ""wtb_ssp_roadworkneeds""
                                                    SET status='verified2'
                                                    WHERE uuid IN
                                                    (SELECT n.uuid
                                                        FROM ""wtb_ssp_roadworkneeds"" n
                                                        LEFT JOIN ""wtb_ssp_activities_to_needs"" an ON an.uuid_roadwork_need = n.uuid
                                                        WHERE an.activityrelationtype='assignedneed'
                                                        AND an.uuid_roadwork_activity=@uuid_roadwork_activity)";
                            updateNeedsStatusComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadWorkActivityFeature.properties.uuid));
                            updateNeedsStatusComm.ExecuteNonQuery();
                        }
                        else
                        {
                            NpgsqlCommand updateNeedsStatusComm = pgConn.CreateCommand();
                            updateNeedsStatusComm.CommandText = @"UPDATE ""wtb_ssp_roadworkneeds""
                                                    SET status=@status
                                                    WHERE uuid IN
                                                    (SELECT n.uuid
                                                        FROM ""wtb_ssp_roadworkneeds"" n
                                                        LEFT JOIN ""wtb_ssp_activities_to_needs"" an ON an.uuid_roadwork_need = n.uuid
                                                        WHERE an.activityrelationtype='assignedneed'
                                                            AND an.uuid_roadwork_activity=@uuid_roadwork_activity)";
                            updateNeedsStatusComm.Parameters.AddWithValue("status", roadWorkActivityFeature.properties.status);
                            updateNeedsStatusComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadWorkActivityFeature.properties.uuid));
                            updateNeedsStatusComm.ExecuteNonQuery();
                        }

                    }

                    trans.Commit();

                }

            }
            catch (Exception ex)
            {
                _logger.LogError(GetType().Name + ": " + ex.Message);
                roadWorkActivityFeature = new RoadWorkActivityFeature();
                roadWorkActivityFeature.errorMessage = "SSP-3";
                return Ok(roadWorkActivityFeature);
            }

            return Ok(roadWorkActivityFeature);
        }

        // PUT /roadworkactivity/registertrafficmanager/8b5286bc-f2b9...
        /// <summary>
        /// Sets the current user as the traffic manager of the
        /// rodwork activity with the given UUID. If the current user
        /// has not the role of a traffic manager, nothing is changed.
        /// </summary>
        /// <response code="200">
        /// Error message object. It has an errorMessage if there
        /// was an error. Else, the errorMessage part is empty.
        /// </response>
        [HttpPut]
        [Route("/Roadworkactivity/RegisterTrafficManager/")]
        [ProducesResponseType(typeof(ConstructionSiteFeature[]), 200)]
        public RoadWorkActivityFeature RegisterTrafficManager([FromBody] RoadWorkActivityFeature roadWorkActivity)
        {
            try
            {
                if (roadWorkActivity == null || roadWorkActivity.properties.uuid == null
                            || roadWorkActivity.properties.uuid.Trim() == "")
                {
                    _logger.LogWarning("No roadworkactivity received in register traffic manager operation.");
                    roadWorkActivity = new RoadWorkActivityFeature();
                    roadWorkActivity.errorMessage = "SSP-15";
                    return roadWorkActivity;
                }

                User userFromDb = LoginController.getAuthorizedUserFromDb(this.User, false);
                if (userFromDb == null || userFromDb.uuid == null || userFromDb.uuid.Trim() == "")
                {
                    _logger.LogWarning("User not found in register traffic manager operation.");
                    roadWorkActivity.errorMessage = "SSP-0";
                    return roadWorkActivity;
                }

                if (User.IsInRole("trafficmanager") || User.IsInRole("administrator"))
                {
                    using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                    {
                        pgConn.Open();
                        NpgsqlCommand updateComm = pgConn.CreateCommand();
                        updateComm.CommandText = @"UPDATE ""wtb_ssp_roadworkactivities""
                                        SET traffic_agent = @traffic_agent
                                        WHERE uuid = @uuid";
                        updateComm.Parameters.AddWithValue("traffic_agent", new Guid(userFromDb.uuid));
                        updateComm.Parameters.AddWithValue("uuid", new Guid(roadWorkActivity.properties.uuid));
                        updateComm.ExecuteNonQuery();
                        roadWorkActivity.properties.trafficAgent.uuid = userFromDb.uuid;
                        roadWorkActivity.properties.trafficAgent.firstName = userFromDb.firstName;
                        roadWorkActivity.properties.trafficAgent.lastName = userFromDb.lastName;
                        return roadWorkActivity;
                    }

                }
                return roadWorkActivity;
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
                roadWorkActivity.errorMessage = "SSP-3";
                return roadWorkActivity;
            }
        }


        // DELETE /roadworkactivity?uuid=...&deletereason=...
        [HttpDelete]
        [Authorize(Roles = "territorymanager,administrator")]
        public ActionResult<ErrorMessage> DeleteActivity(string uuid, string? deleteReason)
        {
            ErrorMessage errorResult = new ErrorMessage();

            try
            {
                if (uuid == null)
                    uuid = "";

                if (deleteReason == null)
                    deleteReason = "";

                uuid = uuid.ToLower().Trim();

                if (uuid == "")
                {
                    _logger.LogWarning("No uuid provided by user in delete roadwork activity process. " +
                                "Thus process is canceled, no roadwork activity is deleted.");
                    errorResult.errorMessage = "SSP-15";
                    return Ok(errorResult);
                }

                int countAffectedRows = 0;
                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    pgConn.Open();


                    NpgsqlCommand selectPrivateStatusComm = pgConn.CreateCommand();
                    selectPrivateStatusComm.CommandText = @"SELECT private
                                    FROM ""wtb_ssp_roadworkactivities""
                                    WHERE uuid=@uuid";
                    selectPrivateStatusComm.Parameters.AddWithValue("uuid", new Guid(uuid));

                    bool isPrivate = false;

                    using (NpgsqlDataReader reader = selectPrivateStatusComm.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            isPrivate = reader.IsDBNull(0) ? false : reader.GetBoolean(0);
                        }
                    }

                    if (!isPrivate && deleteReason.Length < 1)
                    {
                        _logger.LogWarning("No reason for deletion provided by user in delete roadwork activity process. " +
                                    "Thus process is canceled, no roadwork activity is deleted.");
                        errorResult.errorMessage = "SSP-39";
                        return Ok(errorResult);
                    }

                    using (NpgsqlTransaction deleteTransAction = pgConn.BeginTransaction())
                    {
                        string revertNeedStatus = isPrivate ? "requirement" : "edited";

                        NpgsqlCommand updateComm = pgConn.CreateCommand();
                        updateComm.CommandText = @$"UPDATE ""wtb_ssp_roadworkneeds""
                                SET status='{revertNeedStatus}', delete_reason=@delete_reason
                                WHERE uuid IN
                                (SELECT n.uuid
                                    FROM ""wtb_ssp_roadworkneeds"" n
                                    LEFT JOIN ""wtb_ssp_activities_to_needs"" an ON an.uuid_roadwork_need = n.uuid
                                    WHERE an.uuid_roadwork_activity=@uuid_roadwork_activity)";
                        updateComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(uuid));
                        updateComm.Parameters.AddWithValue("delete_reason", deleteReason);
                        updateComm.ExecuteNonQuery();

                        NpgsqlCommand deleteDocumentsComm = pgConn.CreateCommand();
                        deleteDocumentsComm = pgConn.CreateCommand();
                        deleteDocumentsComm.CommandText = @"DELETE FROM ""wtb_ssp_documents""
                                WHERE roadworkactivity=@uuid";
                        deleteDocumentsComm.Parameters.AddWithValue("uuid", new Guid(uuid));
                        countAffectedRows = deleteDocumentsComm.ExecuteNonQuery();

                        NpgsqlCommand deleteActivityComm = pgConn.CreateCommand();
                        deleteActivityComm = pgConn.CreateCommand();
                        deleteActivityComm.CommandText = @"DELETE FROM ""wtb_ssp_roadworkactivities""
                                WHERE uuid=@uuid";
                        deleteActivityComm.Parameters.AddWithValue("uuid", new Guid(uuid));
                        countAffectedRows = deleteActivityComm.ExecuteNonQuery();

                        deleteTransAction.Commit();
                    }

                    pgConn.Close();
                }

                if (countAffectedRows == 1)
                {
                    return Ok();
                }

                _logger.LogError("Unknown error.");
                errorResult.errorMessage = "SSP-3";
                return Ok(errorResult);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                errorResult.errorMessage = "SSP-3";
                return Ok(errorResult);
            }

        }

        // GET /collections/constructionsites/items/
        /// <summary>
        /// Retrieves a collection of all official road construction-sites
        /// of the City of Winterthur.
        /// </summary>
        /// <response code="200">
        /// The data is returned in an array of feature objects.
        /// </response>
        [HttpGet]
        [Route("/Collections/Constructionsites/Items/")]
        [ProducesResponseType(typeof(ConstructionSiteFeature[]), 200)]
        public async Task<ConstructionSiteFeature[]> GetConstructionSiteFeatures()
        {
            List<ConstructionSiteFeature> result = new List<ConstructionSiteFeature>();

            try
            {
                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    await pgConn.OpenAsync();
                    NpgsqlCommand selectComm = pgConn.CreateCommand();
                    selectComm.CommandText = @"SELECT uuid, name, description, created, last_modified,
                                    date_from, date_to, geom
                                    FROM ""wtb_ssp_roadworkactivities""
                                    WHERE in_internet = true AND date_to > current_timestamp";

                    using (NpgsqlDataReader reader = await selectComm.ExecuteReaderAsync())
                    {
                        ConstructionSiteFeature constructionSite;
                        while (await reader.ReadAsync())
                        {
                            constructionSite = new ConstructionSiteFeature();
                            constructionSite.properties.uuid = reader.IsDBNull(0) ? "" : reader.GetGuid(0).ToString();
                            constructionSite.properties.name = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            constructionSite.properties.description = reader.IsDBNull(2) ? "" : reader.GetString(2);
                            constructionSite.properties.created = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3);
                            constructionSite.properties.lastModified = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4);
                            constructionSite.properties.dateFrom = reader.IsDBNull(5) ? DateTime.MinValue : reader.GetDateTime(5);
                            constructionSite.properties.dateTo = reader.IsDBNull(6) ? DateTime.MinValue : reader.GetDateTime(6);
                            Polygon ntsPoly = reader.IsDBNull(7) ? Polygon.Empty : reader.GetValue(7) as Polygon;
                            constructionSite.geometry = new RoadworkPolygon(ntsPoly);
                            result.Add(constructionSite);
                        }
                        return result.ToArray();
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
                ConstructionSiteFeature errObj = new ConstructionSiteFeature();
                errObj.errorMessage = "Unknown critical error.";
                return new ConstructionSiteFeature[] { errObj };
            }
        }

        // GET /collections/constructionsites/items/638364
        /// <summary>
        /// Retrieves the official road construction-site of the City of Winterthur
        /// for the given UUID.
        /// </summary>
        /// <response code="200">
        /// The data is returned as a feature objects.
        /// </response>
        [HttpGet]
        [Route("/Collections/Constructionsites/Items/{uuid}")]
        [ProducesResponseType(typeof(ConstructionSiteFeature), 200)]
        public async Task<ConstructionSiteFeature> GetConstructionSiteFeature(string uuid)
        {
            ConstructionSiteFeature result = new ConstructionSiteFeature();

            if (uuid == null || uuid.Trim() == "")
            {
                result.errorMessage = "No UUID given.";
                return result;
            }

            try
            {
                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    await pgConn.OpenAsync();
                    NpgsqlCommand selectComm = pgConn.CreateCommand();
                    selectComm.CommandText = @"SELECT uuid, name, description, created, last_modified,
                                    date_from, date_to, geom
                                    FROM ""wtb_ssp_roadworkactivities""
                                    WHERE uuid=@uuid AND in_internet = true
                                            AND date_to > current_timestampe";
                    selectComm.Parameters.AddWithValue("uuid", new Guid(uuid));

                    using (NpgsqlDataReader reader = await selectComm.ExecuteReaderAsync())
                    {
                        if (await reader.ReadAsync())
                        {
                            result = new ConstructionSiteFeature();
                            result.properties.uuid = reader.IsDBNull(0) ? "" : reader.GetGuid(0).ToString();
                            result.properties.name = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            result.properties.description = reader.IsDBNull(2) ? "" : reader.GetString(2);
                            result.properties.created = reader.IsDBNull(3) ? DateTime.MinValue : reader.GetDateTime(3);
                            result.properties.lastModified = reader.IsDBNull(4) ? DateTime.MinValue : reader.GetDateTime(4);
                            result.properties.dateFrom = reader.IsDBNull(5) ? DateTime.MinValue : reader.GetDateTime(5);
                            result.properties.dateTo = reader.IsDBNull(6) ? DateTime.MinValue : reader.GetDateTime(6);
                            Polygon ntsPoly = reader.IsDBNull(7) ? Polygon.Empty : reader.GetValue(7) as Polygon;
                            result.geometry = new RoadworkPolygon(ntsPoly);
                            return result;
                        }
                        else
                        {
                            result.errorMessage = "No construction-site found for given uuid.";
                            return result;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex.Message);
                result.errorMessage = "Unknown critical error.";
                return result;
            }
        }


        private int _countAssingedNeedsOfActivity(NpgsqlConnection pgConn, string roadWorkActivityUuid)
        {
            int result = 0;
            NpgsqlCommand selectCountAssignedNeedsComm = pgConn.CreateCommand();
            selectCountAssignedNeedsComm.CommandText = @"SELECT count(*) FROM ""wtb_ssp_activities_to_needs""
                                                        WHERE uuid_roadwork_activity=@uuid_roadwork_activity
                                                        AND activityrelationtype='assignedneed'";
            selectCountAssignedNeedsComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadWorkActivityUuid));

            using (NpgsqlDataReader reader = selectCountAssignedNeedsComm.ExecuteReader())
                if (reader.Read())
                    result = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);

            return result;
        }


        private string _generateProjectNo(NpgsqlConnection pgConn)
        {
            string projectNo = "";
            int countProjectNo = 1;
            int noOfRetries = 0;

            while (countProjectNo != 0 && noOfRetries < 10)
            {
                string projectNoTemp = Guid.NewGuid().ToString("N").Substring(0, 15);

                NpgsqlCommand selectCountAssignedNeedsComm = pgConn.CreateCommand();
                selectCountAssignedNeedsComm.CommandText = @"SELECT count(*) FROM ""wtb_ssp_roadworkactivities""
                                                            WHERE project_no=@project_no";
                selectCountAssignedNeedsComm.Parameters.AddWithValue("project_no", projectNoTemp);
                using (NpgsqlDataReader reader = selectCountAssignedNeedsComm.ExecuteReader())
                    if (reader.Read())
                        countProjectNo = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                if(countProjectNo == 0)
                    projectNo = projectNoTemp;
                noOfRetries++;
            }
            return projectNo;
        }

        private bool _isStatusChangeAllowed(string oldStatus, string newStatus)
        {
            if (oldStatus == "review")
            {
                if (newStatus == "review")
                    return false;
            }
            else if (oldStatus == "inconsult1")
            {
                if (newStatus == "inconsult1" ||
                    newStatus == "review")
                    return false;
            }
            else if (oldStatus == "inconsult2")
            {
                if (newStatus == "inconsult2" ||
                    newStatus == "review")
                    return false;
            }
            else if (oldStatus == "verified1")
            {
                if (newStatus == "review")
                    return false;
            }
            else if (oldStatus == "verified2")
            {
                if (newStatus == "review")
                    return false;
            }
            else if (oldStatus == "reporting")
            {
                if (newStatus != "coordinated" &&
                    newStatus != "suspended")
                    return false;
            }
            else if (oldStatus == "coordinated")
            {
                if (newStatus != "reporting" &&
                    newStatus != "suspended" &&
                    newStatus != "prestudy")
                    return false;
            }
            else if (oldStatus == "prestudy")
            {
                return false;
            }
            else if (oldStatus == "suspended")
            {
                return false;
            }
            return true;
        }

    }
}
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
        [Authorize(Roles = "orderer,trafficmanager,territorymanager,administrator")]
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
                            r.projectmanager, pm.first_name, pm.last_name, r.traffic_agent,
                            ta.first_name, ta.last_name, description, created, last_modified, r.date_from, r.date_to,
                            r.costs, r.costs_type, r.status, r.in_internet,
                            r.billing_address1, r.billing_address2, r.investment_no, r.pdb_fid,
                            r.strabako_no, r.date_sks, r.date_kap, r.date_oks, r.date_gl_tba,
                            r.comment, r.section, r.type, r.projecttype, r.overarching_measure,
                            r.desired_year_from, r.desired_year_to, r.prestudy, r.start_of_construction,
                            r.end_of_construction, r.consult_due, r.project_no, r.private, r.date_accept,
                            r.date_guarantee, r.is_study, r.date_study_start, r.date_study_end,
                            r.is_desire, r.date_desire_start, r.date_desire_end, r.is_particip,
                            r.date_particip_start, r.date_particip_end, r.is_plan_circ,
                            r.date_plan_circ_start, r.date_plan_circ_end, r.date_consult_start,
                            r.date_consult_end, r.date_consult_close, r.date_report_start,
                            r.date_report_end, r.date_report_close, r.date_info_start,
                            r.date_info_end, r.date_info_close, r.is_aggloprog, r.date_optimum,
                            r.date_of_acceptance, r.url,
                            r.project_study_approved, r.study_approved, r.date_sks_real,
                            r.date_kap_real, r.date_oks_real, r.date_gl_tba_real, r.geom
                        FROM ""wtb_ssp_roadworkactivities"" r
                        LEFT JOIN ""wtb_ssp_users"" pm ON r.projectmanager = pm.uuid
                        LEFT JOIN ""wtb_ssp_users"" ta ON r.traffic_agent = ta.uuid";

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
                        projectFeatureFromDb.properties.uuid = reader.IsDBNull(0) ? "" : reader.GetGuid(0).ToString();
                        projectFeatureFromDb.properties.name = reader.IsDBNull(1) ? "" : reader.GetString(1);

                        User projectManager = new User();
                        projectManager.uuid = reader.IsDBNull(2) ? "" : reader.GetGuid(2).ToString();
                        projectManager.firstName = reader.IsDBNull(3) ? "" : reader.GetString(3);
                        projectManager.lastName = reader.IsDBNull(4) ? "" : reader.GetString(4);
                        projectFeatureFromDb.properties.projectManager = projectManager;

                        User trafficAgent = new User();
                        trafficAgent.uuid = reader.IsDBNull(5) ? "" : reader.GetGuid(5).ToString();
                        trafficAgent.firstName = reader.IsDBNull(6) ? "" : reader.GetString(6);
                        trafficAgent.lastName = reader.IsDBNull(7) ? "" : reader.GetString(7);
                        projectFeatureFromDb.properties.trafficAgent = trafficAgent;

                        projectFeatureFromDb.properties.description = reader.IsDBNull(8) ? "" : reader.GetString(8);
                        projectFeatureFromDb.properties.created = reader.IsDBNull(9) ? DateTime.MinValue : reader.GetDateTime(9);
                        projectFeatureFromDb.properties.lastModified = reader.IsDBNull(10) ? DateTime.MinValue : reader.GetDateTime(10);
                        projectFeatureFromDb.properties.finishEarlyTo = reader.IsDBNull(11) ? DateTime.MinValue : reader.GetDateTime(11);
                        projectFeatureFromDb.properties.finishLateTo = reader.IsDBNull(12) ? DateTime.MinValue : reader.GetDateTime(12);
                        projectFeatureFromDb.properties.costs = reader.IsDBNull(13) ? 0m : reader.GetDecimal(13);

                        projectFeatureFromDb.properties.costsType = reader.IsDBNull(14) ? "" : reader.GetString(14); ;

                        if (User.IsInRole("administrator") || User.IsInRole("territorymanager"))
                        {
                            projectFeatureFromDb.properties.isEditingAllowed = true;
                        }

                        projectFeatureFromDb.properties.status = reader.IsDBNull(15) ? "" : reader.GetString(15);

                        projectFeatureFromDb.properties.isInInternet = reader.IsDBNull(16) ? false : reader.GetBoolean(16);
                        projectFeatureFromDb.properties.billingAddress1 = reader.IsDBNull(17) ? "" : reader.GetString(17);
                        projectFeatureFromDb.properties.billingAddress2 = reader.IsDBNull(18) ? "" : reader.GetString(18);
                        projectFeatureFromDb.properties.investmentNo = reader.IsDBNull(19) ? 0 : reader.GetInt32(19);
                        projectFeatureFromDb.properties.pdbFid = reader.IsDBNull(20) ? 0 : reader.GetInt32(20);
                        projectFeatureFromDb.properties.strabakoNo = reader.IsDBNull(21) ? "" : reader.GetString(21);

                        projectFeatureFromDb.properties.dateSks = reader.IsDBNull(22) ? null : reader.GetDateTime(22);
                        projectFeatureFromDb.properties.dateKap = reader.IsDBNull(23) ? null : reader.GetDateTime(23);
                        projectFeatureFromDb.properties.dateOks = reader.IsDBNull(24) ? null : reader.GetDateTime(24);
                        projectFeatureFromDb.properties.dateGlTba = reader.IsDBNull(25) ? null : reader.GetDateTime(25);

                        projectFeatureFromDb.properties.comment = reader.IsDBNull(26) ? "" : reader.GetString(26);
                        projectFeatureFromDb.properties.section = reader.IsDBNull(27) ? "" : reader.GetString(27);
                        projectFeatureFromDb.properties.type = reader.IsDBNull(28) ? "" : reader.GetString(28);
                        projectFeatureFromDb.properties.projectType = reader.IsDBNull(29) ? "" : reader.GetString(29);
                        projectFeatureFromDb.properties.overarchingMeasure = reader.IsDBNull(30) ? false : reader.GetBoolean(30);
                        projectFeatureFromDb.properties.desiredYearFrom = reader.IsDBNull(31) ? -1 : reader.GetInt32(31);
                        projectFeatureFromDb.properties.desiredYearTo = reader.IsDBNull(32) ? -1 : reader.GetInt32(32);
                        projectFeatureFromDb.properties.prestudy = reader.IsDBNull(33) ? false : reader.GetBoolean(33);
                        if (!reader.IsDBNull(34)) projectFeatureFromDb.properties.startOfConstruction = reader.GetDateTime(34);
                        if (!reader.IsDBNull(35)) projectFeatureFromDb.properties.endOfConstruction = reader.GetDateTime(35);
                        projectFeatureFromDb.properties.consultDue = reader.IsDBNull(36) ? DateTime.MinValue : reader.GetDateTime(36);
                        projectFeatureFromDb.properties.projectNo = reader.IsDBNull(37) ? "" : reader.GetString(37);
                        projectFeatureFromDb.properties.isPrivate = reader.IsDBNull(38) ? false : reader.GetBoolean(38);
                        if (!reader.IsDBNull(39)) projectFeatureFromDb.properties.dateAccept = reader.GetDateTime(39);
                        if (!reader.IsDBNull(40)) projectFeatureFromDb.properties.dateGuarantee = reader.GetDateTime(40);
                        if (!reader.IsDBNull(41)) projectFeatureFromDb.properties.isStudy = reader.GetBoolean(41);
                        if (!reader.IsDBNull(42)) projectFeatureFromDb.properties.dateStudyStart = reader.GetDateTime(42);
                        if (!reader.IsDBNull(43)) projectFeatureFromDb.properties.dateStudyEnd = reader.GetDateTime(43);
                        if (!reader.IsDBNull(44)) projectFeatureFromDb.properties.isDesire = reader.GetBoolean(44);
                        if (!reader.IsDBNull(45)) projectFeatureFromDb.properties.dateDesireStart = reader.GetDateTime(45);
                        if (!reader.IsDBNull(46)) projectFeatureFromDb.properties.dateDesireEnd = reader.GetDateTime(46);
                        if (!reader.IsDBNull(47)) projectFeatureFromDb.properties.isParticip = reader.GetBoolean(47);
                        if (!reader.IsDBNull(48)) projectFeatureFromDb.properties.dateParticipStart = reader.GetDateTime(48);
                        if (!reader.IsDBNull(49)) projectFeatureFromDb.properties.dateParticipEnd = reader.GetDateTime(49);
                        if (!reader.IsDBNull(50)) projectFeatureFromDb.properties.isPlanCirc = reader.GetBoolean(50);
                        if (!reader.IsDBNull(51)) projectFeatureFromDb.properties.datePlanCircStart = reader.GetDateTime(51);
                        if (!reader.IsDBNull(52)) projectFeatureFromDb.properties.datePlanCircEnd = reader.GetDateTime(52);
                        if (!reader.IsDBNull(53)) projectFeatureFromDb.properties.dateConsultStart = reader.GetDateTime(53);
                        if (!reader.IsDBNull(54)) projectFeatureFromDb.properties.dateConsultEnd = reader.GetDateTime(54);
                        if (!reader.IsDBNull(55)) projectFeatureFromDb.properties.dateConsultClose = reader.GetDateTime(55);
                        if (!reader.IsDBNull(56)) projectFeatureFromDb.properties.dateReportStart = reader.GetDateTime(56);
                        if (!reader.IsDBNull(57)) projectFeatureFromDb.properties.dateReportEnd = reader.GetDateTime(57);
                        if (!reader.IsDBNull(58)) projectFeatureFromDb.properties.dateReportClose = reader.GetDateTime(58);
                        if (!reader.IsDBNull(59)) projectFeatureFromDb.properties.dateInfoStart = reader.GetDateTime(59);
                        if (!reader.IsDBNull(60)) projectFeatureFromDb.properties.dateInfoEnd = reader.GetDateTime(60);
                        if (!reader.IsDBNull(61)) projectFeatureFromDb.properties.dateInfoClose = reader.GetDateTime(61);
                        if (!reader.IsDBNull(62)) projectFeatureFromDb.properties.isAggloprog = reader.GetBoolean(62);
                        projectFeatureFromDb.properties.finishOptimumTo = reader.IsDBNull(63) ? DateTime.MinValue : reader.GetDateTime(63);
                        if (!reader.IsDBNull(64)) projectFeatureFromDb.properties.dateOfAcceptance = reader.GetDateTime(64);
                        if (!reader.IsDBNull(65)) projectFeatureFromDb.properties.url = reader.GetString(65);
                        if (!reader.IsDBNull(66)) projectFeatureFromDb.properties.projectStudyApproved = reader.GetDateTime(67);
                        if (!reader.IsDBNull(67)) projectFeatureFromDb.properties.studyApproved = reader.GetDateTime(67);
                        projectFeatureFromDb.properties.dateSksReal = reader.IsDBNull(68) ? null : reader.GetDateTime(68);
                        projectFeatureFromDb.properties.dateKapReal = reader.IsDBNull(69) ? null : reader.GetDateTime(69);
                        projectFeatureFromDb.properties.dateOksReal = reader.IsDBNull(70) ? null : reader.GetDateTime(70);
                        projectFeatureFromDb.properties.dateGlTbaReal = reader.IsDBNull(71) ? null : reader.GetDateTime(71);

                        Polygon ntsPoly = reader.IsDBNull(72) ? Polygon.Empty : reader.GetValue(72) as Polygon;
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

        // GET roadworkactivity/costtypes/
        [HttpGet]
        [Route("/Roadworkactivity/Costtypes/")]
        [Authorize]
        public IEnumerable<EnumType> GetCostTypes()
        {
            List<EnumType> result = new List<EnumType>();
            // get data of current user from database:
            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand selectComm = pgConn.CreateCommand();
                selectComm.CommandText = "SELECT code, name FROM \"wtb_ssp_costtypes\"";

                using (NpgsqlDataReader reader = selectComm.ExecuteReader())
                {
                    EnumType costType;
                    while (reader.Read())
                    {
                        costType = new EnumType();
                        costType.code = reader.IsDBNull(0) ? "" : reader.GetString(0);
                        costType.name = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        result.Add(costType);
                    }
                }
                pgConn.Close();
            }
            return result.ToArray();
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

                if (roadWorkActivityFeature.properties.finishEarlyTo > roadWorkActivityFeature.properties.finishLateTo)
                {
                    _logger.LogWarning("The finish from date of a roadwork activity cannot be higher than its finish to date");
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "SSP-19";
                    return Ok(roadWorkActivityFeature);
                }

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
                                    project_no, comment, section, type, projecttype,
                                    overarching_measure, desired_year_from, desired_year_to, prestudy, 
                                    start_of_construction, end_of_construction, consult_due,
                                    created, last_modified, date_from, date_optimum, date_to,
                                    costs, costs_type, status, in_internet, billing_address1,
                                    billing_address2, investment_no, date_sks,
                                    date_kap, private, date_consult_start,
                                    date_consult_end, date_report_start, date_report_end,
                                    url, geom)
                                    VALUES (@uuid, @name, @projectmanager, @traffic_agent,
                                    @description, @project_no, @comment, @section, @type, @projecttype,
                                    @overarching_measure, @desired_year_from, @desired_year_to, @prestudy, 
                                    @start_of_construction, @end_of_construction, @consult_due,
                                    current_timestamp, current_timestamp, @date_from, @date_optimum,
                                    @date_to, @costs, @costs_type, @status, @in_internet, @billing_address1,
                                    @billing_address2, @investment_no, @date_sks, @date_kap,
                                    @private, @date_consult_start, @date_consult_end,
                                    @date_report_start, @date_report_end, @url, @geom)";
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
                    insertComm.Parameters.AddWithValue("date_from", roadWorkActivityFeature.properties.finishEarlyTo);
                    insertComm.Parameters.AddWithValue("date_optimum", roadWorkActivityFeature.properties.finishOptimumTo);
                    insertComm.Parameters.AddWithValue("date_to", roadWorkActivityFeature.properties.finishLateTo);
                    insertComm.Parameters.AddWithValue("costs", roadWorkActivityFeature.properties.costs <= 0 ? DBNull.Value : roadWorkActivityFeature.properties.costs);
                    insertComm.Parameters.AddWithValue("costs_type", roadWorkActivityFeature.properties.costsType);
                    insertComm.Parameters.AddWithValue("status", "review");
                    insertComm.Parameters.AddWithValue("in_internet", roadWorkActivityFeature.properties.isInInternet);
                    insertComm.Parameters.AddWithValue("billing_address1", roadWorkActivityFeature.properties.billingAddress1);
                    insertComm.Parameters.AddWithValue("billing_address2", roadWorkActivityFeature.properties.billingAddress2);
                    insertComm.Parameters.AddWithValue("investment_no", roadWorkActivityFeature.properties.investmentNo == 0 ? DBNull.Value : roadWorkActivityFeature.properties.investmentNo);
                    insertComm.Parameters.AddWithValue("comment", roadWorkActivityFeature.properties.comment);
                    insertComm.Parameters.AddWithValue("section", roadWorkActivityFeature.properties.section);
                    insertComm.Parameters.AddWithValue("type", roadWorkActivityFeature.properties.type);
                    insertComm.Parameters.AddWithValue("projecttype", roadWorkActivityFeature.properties.projectType == "" ?
                                        DBNull.Value : roadWorkActivityFeature.properties.projectType);
                    insertComm.Parameters.AddWithValue("overarching_measure", roadWorkActivityFeature.properties.overarchingMeasure);
                    insertComm.Parameters.AddWithValue("desired_year_from", roadWorkActivityFeature.properties.desiredYearFrom);
                    insertComm.Parameters.AddWithValue("desired_year_to", roadWorkActivityFeature.properties.desiredYearTo);
                    insertComm.Parameters.AddWithValue("prestudy", roadWorkActivityFeature.properties.prestudy);
                    insertComm.Parameters.AddWithValue("start_of_construction", roadWorkActivityFeature.properties.startOfConstruction != null ? roadWorkActivityFeature.properties.startOfConstruction : DBNull.Value);
                    insertComm.Parameters.AddWithValue("end_of_construction", roadWorkActivityFeature.properties.endOfConstruction != null ? roadWorkActivityFeature.properties.endOfConstruction : DBNull.Value);
                    insertComm.Parameters.AddWithValue("date_of_acceptance", roadWorkActivityFeature.properties.dateOfAcceptance != null ? roadWorkActivityFeature.properties.dateOfAcceptance : DBNull.Value);
                    insertComm.Parameters.AddWithValue("project_no", roadWorkActivityFeature.properties.projectNo);
                    insertComm.Parameters.AddWithValue("private", roadWorkActivityFeature.properties.isPrivate);
                    insertComm.Parameters.AddWithValue("date_consult_start", DateTime.Now.AddDays(7));
                    insertComm.Parameters.AddWithValue("date_consult_end", DateTime.Now.AddDays(28));
                    insertComm.Parameters.AddWithValue("date_report_start", DateTime.Now.AddDays(35));
                    insertComm.Parameters.AddWithValue("date_report_end", DateTime.Now.AddDays(56));
                    insertComm.Parameters.AddWithValue("url", roadWorkActivityFeature.properties.url != null ? roadWorkActivityFeature.properties.url : DBNull.Value);
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

                if (roadWorkActivityFeature.properties.finishEarlyTo > roadWorkActivityFeature.properties.finishLateTo)
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
                                    projecttype, name, section
                                    FROM ""wtb_ssp_roadworkactivities""
                                    WHERE uuid=@uuid";
                    selectMainAttributeValues.Parameters.AddWithValue("uuid", new Guid(roadWorkActivityFeature.properties.uuid));

                    string projectManagerInDb = "";
                    string projectTypeInDb = "";
                    string nameInDb = "";
                    string sectionInDb = "";
                    using (NpgsqlDataReader reader = selectMainAttributeValues.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            if (!reader.IsDBNull(0)) projectManagerInDb = reader.GetGuid(0).ToString();
                            if (!reader.IsDBNull(1)) projectTypeInDb = reader.GetString(1);
                            if (!reader.IsDBNull(2)) nameInDb = reader.GetString(2);
                            if (!reader.IsDBNull(3)) sectionInDb = reader.GetString(3);
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
                                    comment=@comment, section=@section, type=@type,
                                    projecttype=@projecttype, overarching_measure=@overarching_measure,
                                    desired_year_from=@desired_year_from, desired_year_to=@desired_year_to, prestudy=@prestudy, 
                                    start_of_construction=@start_of_construction, date_of_acceptance=@date_of_acceptance,
                                    end_of_construction=@end_of_construction, consult_due=@consult_due,
                                    last_modified=@last_modified, project_no=@project_no,
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
                                    date_consult_start=@date_consult_start, date_consult_end=@date_consult_end,
                                    date_consult_close=@date_consult_close,
                                    date_report_start=@date_report_start,
                                    date_report_end=@date_report_end, date_report_close=@date_report_close,
                                    date_info_start=@date_info_start, date_info_end=@date_info_end,
                                    date_info_close=@date_info_close, is_aggloprog=@is_aggloprog,
                                    project_study_approved=@project_study_approved, study_approved=@study_approved,";

                    if (hasStatusChanged)
                    {
                        if (roadWorkActivityFeature.properties.status == "inconsult")
                            updateComm.CommandText += "date_start_inconsult=@date_start_inconsult, ";
                        else if (roadWorkActivityFeature.properties.status == "verified")
                            updateComm.CommandText += "date_start_verified=@date_start_verified, ";
                        else if (roadWorkActivityFeature.properties.status == "reporting")
                            updateComm.CommandText += "date_start_reporting=@date_start_reporting, ";
                        else if (roadWorkActivityFeature.properties.status == "suspended")
                            updateComm.CommandText += "date_start_suspended=@date_start_suspended, ";
                        else if (roadWorkActivityFeature.properties.status == "coordinated")
                            updateComm.CommandText += "date_start_coordinated=@date_start_coordinated, ";
                    }

                    // if we are going one step back (from status "verified" to status "inconsult")
                    // than delete timestamp:
                    if (statusOfActivityInDb == "verified" &&
                            roadWorkActivityFeature.properties.status == "inconsult")
                    {
                        updateComm.CommandText += "date_start_verified=NULL, ";
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
                    updateComm.Parameters.AddWithValue("date_from", roadWorkActivityFeature.properties.finishEarlyTo);
                    updateComm.Parameters.AddWithValue("date_optimum", roadWorkActivityFeature.properties.finishOptimumTo);
                    updateComm.Parameters.AddWithValue("date_to", roadWorkActivityFeature.properties.finishLateTo);
                    updateComm.Parameters.AddWithValue("costs", roadWorkActivityFeature.properties.costs);
                    updateComm.Parameters.AddWithValue("costs_type", roadWorkActivityFeature.properties.costsType);
                    updateComm.Parameters.AddWithValue("status", roadWorkActivityFeature.properties.status);
                    updateComm.Parameters.AddWithValue("in_internet", roadWorkActivityFeature.properties.isInInternet);
                    updateComm.Parameters.AddWithValue("billing_address1", roadWorkActivityFeature.properties.billingAddress1);
                    updateComm.Parameters.AddWithValue("billing_address2", roadWorkActivityFeature.properties.billingAddress2);
                    updateComm.Parameters.AddWithValue("url", roadWorkActivityFeature.properties.url != null ? roadWorkActivityFeature.properties.url : DBNull.Value);
                    updateComm.Parameters.AddWithValue("investment_no", roadWorkActivityFeature.properties.investmentNo);
                    updateComm.Parameters.AddWithValue("date_sks_real", roadWorkActivityFeature.properties.dateSksReal != null ? roadWorkActivityFeature.properties.dateSksReal : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_kap_real", roadWorkActivityFeature.properties.dateKapReal != null ? roadWorkActivityFeature.properties.dateKapReal : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_oks_real", roadWorkActivityFeature.properties.dateOksReal != null ? roadWorkActivityFeature.properties.dateOksReal : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_gl_tba", roadWorkActivityFeature.properties.dateGlTba != null ? roadWorkActivityFeature.properties.dateGlTba : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_gl_tba_real", roadWorkActivityFeature.properties.dateGlTbaReal != null ? roadWorkActivityFeature.properties.dateGlTbaReal : DBNull.Value);
                    updateComm.Parameters.AddWithValue("comment", roadWorkActivityFeature.properties.comment);
                    updateComm.Parameters.AddWithValue("section", roadWorkActivityFeature.properties.section);
                    updateComm.Parameters.AddWithValue("type", roadWorkActivityFeature.properties.type);
                    updateComm.Parameters.AddWithValue("projecttype", roadWorkActivityFeature.properties.projectType == "" ?
                                            DBNull.Value : roadWorkActivityFeature.properties.projectType);
                    updateComm.Parameters.AddWithValue("overarching_measure", roadWorkActivityFeature.properties.overarchingMeasure);
                    updateComm.Parameters.AddWithValue("desired_year_from", roadWorkActivityFeature.properties.desiredYearFrom);
                    updateComm.Parameters.AddWithValue("desired_year_to", roadWorkActivityFeature.properties.desiredYearTo);
                    updateComm.Parameters.AddWithValue("prestudy", roadWorkActivityFeature.properties.prestudy);
                    updateComm.Parameters.AddWithValue("start_of_construction", roadWorkActivityFeature.properties.startOfConstruction != null ? roadWorkActivityFeature.properties.startOfConstruction : DBNull.Value);
                    updateComm.Parameters.AddWithValue("end_of_construction", roadWorkActivityFeature.properties.endOfConstruction != null ? roadWorkActivityFeature.properties.endOfConstruction : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_of_acceptance", roadWorkActivityFeature.properties.dateOfAcceptance != null ? roadWorkActivityFeature.properties.dateOfAcceptance : DBNull.Value);
                    updateComm.Parameters.AddWithValue("consult_due", roadWorkActivityFeature.properties.consultDue);
                    updateComm.Parameters.AddWithValue("project_no", roadWorkActivityFeature.properties.projectNo);
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
                    updateComm.Parameters.AddWithValue("date_consult_start", roadWorkActivityFeature.properties.dateConsultStart != null ? roadWorkActivityFeature.properties.dateConsultStart : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_consult_end", roadWorkActivityFeature.properties.dateConsultEnd != null ? roadWorkActivityFeature.properties.dateConsultEnd : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_consult_close", roadWorkActivityFeature.properties.dateConsultClose != null ? roadWorkActivityFeature.properties.dateConsultClose : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_report_start", roadWorkActivityFeature.properties.dateReportStart != null ? roadWorkActivityFeature.properties.dateReportStart : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_report_end", roadWorkActivityFeature.properties.dateReportEnd != null ? roadWorkActivityFeature.properties.dateReportEnd : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_report_close", roadWorkActivityFeature.properties.dateReportClose != null ? roadWorkActivityFeature.properties.dateReportClose : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_info_start", roadWorkActivityFeature.properties.dateInfoStart != null ? roadWorkActivityFeature.properties.dateInfoStart : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_info_end", roadWorkActivityFeature.properties.dateInfoEnd != null ? roadWorkActivityFeature.properties.dateInfoEnd : DBNull.Value);
                    updateComm.Parameters.AddWithValue("date_info_close", roadWorkActivityFeature.properties.dateInfoClose != null ? roadWorkActivityFeature.properties.dateInfoClose : DBNull.Value);
                    updateComm.Parameters.AddWithValue("is_aggloprog", roadWorkActivityFeature.properties.isAggloprog);
                    updateComm.Parameters.AddWithValue("geom", roadWorkActivityPoly);
                    updateComm.Parameters.AddWithValue("uuid", new Guid(roadWorkActivityFeature.properties.uuid));

                    if (hasStatusChanged)
                    {
                        if (roadWorkActivityFeature.properties.status == "inconsult")
                            updateComm.Parameters.AddWithValue("date_start_inconsult", DateTime.Now);
                        else if (roadWorkActivityFeature.properties.status == "verified")
                            updateComm.Parameters.AddWithValue("date_start_verified", DateTime.Now);
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
                        else if (roadWorkActivityFeature.properties.status == "inconsult")
                            whatText += "in Bedarfsklärung";
                        else if (roadWorkActivityFeature.properties.status == "verified")
                            whatText += "verifiziert";
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

                        if (roadWorkActivityFeature.properties.status == "inconsult" ||
                            roadWorkActivityFeature.properties.status == "reporting")
                        {
                            foreach (User involvedUser in roadWorkActivityFeature.properties.involvedUsers)
                            {
                                NpgsqlCommand insertEmptyCommentsComm = pgConn.CreateCommand();
                                insertEmptyCommentsComm.CommandText = @"INSERT INTO ""wtb_ssp_activity_consult""
                                    (uuid, uuid_roadwork_activity, input_by, feedback_phase, feedback_given)
                                    VALUES
                                    (@uuid, @uuid_roadwork_activity, @input_by, @feedback_phase, @feedback_given)";

                                insertEmptyCommentsComm.Parameters.AddWithValue("uuid", Guid.NewGuid());
                                insertEmptyCommentsComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadWorkActivityFeature.properties.uuid));
                                insertEmptyCommentsComm.Parameters.AddWithValue("input_by", new Guid(involvedUser.uuid));
                                insertEmptyCommentsComm.Parameters.AddWithValue("feedback_phase", roadWorkActivityFeature.properties.status);
                                insertEmptyCommentsComm.Parameters.AddWithValue("feedback_given", false);
                                insertEmptyCommentsComm.ExecuteNonQuery();
                            }
                        }

                        if (roadWorkActivityFeature.properties.status == "verified")
                        {
                            NpgsqlCommand updateNeedsStatusComm = pgConn.CreateCommand();
                            updateNeedsStatusComm.CommandText = @"UPDATE ""wtb_ssp_roadworkneeds""
                                                    SET status='verified'
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
                        string revertNeedStatus = isPrivate ? "requirement" : "suspended";

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

        private bool _isStatusChangeAllowed(string oldStatus, string newStatus)
        {
            if (oldStatus == "review")
            {
                if (newStatus == "review")
                    return false;
            }
            else if (oldStatus == "inconsult")
            {
                if (newStatus == "inconsult" ||
                    newStatus == "review")
                    return false;
            }
            else if (oldStatus == "verified")
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
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
                            r.costs, c.code, c.name, s.code, s.name, r.in_internet,
                            r.billing_address1, r.billing_address2, r.investment_no, r.pdb_fid,
                            r.strabako_no, r.date_sks, r.date_kap, r.date_oks, r.date_gl_tba,
                            r.comment, r.section, r.type, r.projecttype, r.overarching_measure,
                            r.desired_year, r.prestudy, r.start_of_construction, r.end_of_construction,
                            r.consult_due, r.project_no, r.geom
                        FROM ""wtb_ssp_roadworkactivities"" r
                        LEFT JOIN ""wtb_ssp_users"" pm ON r.projectmanager = pm.uuid
                        LEFT JOIN ""wtb_ssp_users"" ta ON r.traffic_agent = ta.uuid
                        LEFT JOIN ""wtb_ssp_status"" s ON r.status = s.code
                        LEFT JOIN ""wtb_ssp_costtypes"" c ON r.costs_type = c.code";

                if (uuid != null)
                {
                    uuid = uuid.Trim().ToLower();
                    if (uuid != "")
                    {
                        selectComm.CommandText += " WHERE r.uuid=@uuid";
                        selectComm.Parameters.AddWithValue("uuid", new Guid(uuid));
                    }
                }

                if ((uuid == null || uuid == "") && status != null)
                {
                    status = status.Trim().ToLower();
                    if (status != "")
                    {
                        selectComm.CommandText += " WHERE r.status=@status";
                        selectComm.Parameters.AddWithValue("status", status);
                    }
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
                        projectFeatureFromDb.properties.finishFrom = reader.IsDBNull(11) ? DateTime.MinValue : reader.GetDateTime(11);
                        projectFeatureFromDb.properties.finishTo = reader.IsDBNull(12) ? DateTime.MinValue : reader.GetDateTime(12);
                        projectFeatureFromDb.properties.costs = reader.IsDBNull(13) ? 0m : reader.GetDecimal(13);

                        CostType ct = new CostType();
                        ct.code = reader.IsDBNull(14) ? "" : reader.GetString(14);
                        ct.name = reader.IsDBNull(15) ? "" : reader.GetString(15);
                        projectFeatureFromDb.properties.costsType = ct;

                        if (User.IsInRole("administrator") || User.IsInRole("territorymanager"))
                        {
                            projectFeatureFromDb.properties.isEditingAllowed = true;
                        }

                        Status statusFromDb = new Status();
                        statusFromDb.code = reader.IsDBNull(16) ? "" : reader.GetString(16);
                        statusFromDb.name = reader.IsDBNull(17) ? "" : reader.GetString(17);
                        projectFeatureFromDb.properties.status = statusFromDb;

                        projectFeatureFromDb.properties.isInInternet = reader.IsDBNull(18) ? false : reader.GetBoolean(18);
                        projectFeatureFromDb.properties.billingAddress1 = reader.IsDBNull(19) ? "" : reader.GetString(19);
                        projectFeatureFromDb.properties.billingAddress2 = reader.IsDBNull(20) ? "" : reader.GetString(20);
                        projectFeatureFromDb.properties.investmentNo = reader.IsDBNull(21) ? 0 : reader.GetInt32(21);
                        projectFeatureFromDb.properties.pdbFid = reader.IsDBNull(22) ? 0 : reader.GetInt32(22);
                        projectFeatureFromDb.properties.strabakoNo = reader.IsDBNull(23) ? "" : reader.GetString(23);

                        projectFeatureFromDb.properties.dateSks = reader.IsDBNull(24) ? "" : reader.GetString(24);
                        projectFeatureFromDb.properties.dateKap = reader.IsDBNull(25) ? "" : reader.GetString(25);
                        projectFeatureFromDb.properties.dateOks = reader.IsDBNull(26) ? "" : reader.GetString(26);

                        projectFeatureFromDb.properties.dateGlTba = reader.IsDBNull(27) ? DateTime.MinValue : reader.GetDateTime(27);

                        projectFeatureFromDb.properties.comment = reader.IsDBNull(28) ? "" : reader.GetString(28);
                        projectFeatureFromDb.properties.section = reader.IsDBNull(29) ? "" : reader.GetString(29);
                        projectFeatureFromDb.properties.type = reader.IsDBNull(30) ? "" : reader.GetString(30);
                        projectFeatureFromDb.properties.projectType = reader.IsDBNull(31) ? "" : reader.GetString(31);
                        projectFeatureFromDb.properties.overarchingMeasure = reader.IsDBNull(32) ? false : reader.GetBoolean(32);
                        projectFeatureFromDb.properties.desiredYear = reader.IsDBNull(33) ? -1 : reader.GetInt32(33);
                        projectFeatureFromDb.properties.prestudy = reader.IsDBNull(34) ? false : reader.GetBoolean(34);
                        projectFeatureFromDb.properties.startOfConstruction = reader.IsDBNull(35) ? DateTime.MinValue : reader.GetDateTime(35);
                        projectFeatureFromDb.properties.endOfConstruction = reader.IsDBNull(36) ? DateTime.MinValue : reader.GetDateTime(36);
                        projectFeatureFromDb.properties.consultDue = reader.IsDBNull(37) ? DateTime.MinValue : reader.GetDateTime(37);
                        projectFeatureFromDb.properties.projectNo = reader.IsDBNull(38) ? "" : reader.GetString(38);

                        Polygon ntsPoly = reader.IsDBNull(39) ? Polygon.Empty : reader.GetValue(39) as Polygon;
                        projectFeatureFromDb.geometry = new RoadworkPolygon(ntsPoly);

                        projectsFromDb.Add(projectFeatureFromDb);
                    }
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
                            FROM ""wtb_ssp_activities_history"" WHERE uuid_roadwork_activity = :uuid_roadwork_activity";
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
                }

                pgConn.Close();
            }

            return projectsFromDb.ToArray();
        }

        // GET roadworkactivity/costtypes/
        [HttpGet]
        [Route("/Roadworkactivity/Costtypes/")]
        [Authorize]
        public IEnumerable<CostType> GetCostTypes()
        {
            List<CostType> result = new List<CostType>();
            // get data of current user from database:
            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand selectComm = pgConn.CreateCommand();
                selectComm.CommandText = "SELECT code, name FROM \"wtb_ssp_costtypes\"";

                using (NpgsqlDataReader reader = selectComm.ExecuteReader())
                {
                    CostType costType;
                    while (reader.Read())
                    {
                        costType = new CostType();
                        costType.code = reader.IsDBNull(0) ? "" : reader.GetString(0);
                        costType.name = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        result.Add(costType);
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

                if (coordinates.Length < 3)
                {
                    _logger.LogWarning("Roadwork activity polygon has less than 3 coordinates.");
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "SSP-7";
                    return Ok(roadWorkActivityFeature);
                }

                ConfigurationData configData = AppConfigController.getConfigurationFromDb();

                // only if project area is greater than min area size:
                if (roadWorkActivityPoly.Area <= configData.minAreaSize)
                {
                    _logger.LogWarning("Roadworkneed area is less than or equal " + configData.minAreaSize + "qm.");
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "SSP-8";
                    return Ok(roadWorkActivityFeature);
                }

                // only if project area is smaller than max area size:
                if (roadWorkActivityPoly.Area > configData.maxAreaSize)
                {
                    _logger.LogWarning("Roadworkneed area is greater than " + configData.maxAreaSize + "qm.");
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "SSP-16";
                    return Ok(roadWorkActivityFeature);
                }

                if (roadWorkActivityFeature.properties.finishFrom > roadWorkActivityFeature.properties.finishTo)
                {
                    _logger.LogWarning("The finish from date of a roadworkactivity cannot be higher than its finish to date.");
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "SSP-19";
                    return Ok(roadWorkActivityFeature);
                }

                User userFromDb = LoginController.getAuthorizedUserFromDb(this.User, false);

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
                                    overarching_measure, desired_year, prestudy, 
                                    start_of_construction, end_of_construction, consult_due,
                                    created, last_modified, date_from, date_to,
                                    costs, costs_type, status, in_internet, billing_address1,
                                    billing_address2, investment_no, date_sks, date_kap,
                                    date_oks, date_gl_tba, geom)
                                    VALUES (@uuid, @name, @projectmanager, @traffic_agent,
                                    @description, @project_no, @comment, @section, @type, @projecttype,
                                    @overarching_measure, @desired_year, @prestudy, 
                                    @start_of_construction, @end_of_construction, @consult_due,
                                    current_timestamp, current_timestamp, @date_from,
                                    @date_to, @costs, @costs_type, @status, @in_internet, @billing_address1,
                                    @billing_address2, @investment_no, @date_sks, @date_kap, @date_oks,
                                    @date_gl_tba, @geom)";
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
                    insertComm.Parameters.AddWithValue("date_from", roadWorkActivityFeature.properties.finishFrom);
                    insertComm.Parameters.AddWithValue("date_to", roadWorkActivityFeature.properties.finishTo);
                    insertComm.Parameters.AddWithValue("costs", roadWorkActivityFeature.properties.costs == 0 ? DBNull.Value : roadWorkActivityFeature.properties.costs);
                    insertComm.Parameters.AddWithValue("costs_type", roadWorkActivityFeature.properties.costsType.code);
                    insertComm.Parameters.AddWithValue("status", "review");
                    insertComm.Parameters.AddWithValue("in_internet", roadWorkActivityFeature.properties.isInInternet);
                    insertComm.Parameters.AddWithValue("billing_address1", roadWorkActivityFeature.properties.billingAddress1);
                    insertComm.Parameters.AddWithValue("billing_address2", roadWorkActivityFeature.properties.billingAddress2);
                    insertComm.Parameters.AddWithValue("investment_no", roadWorkActivityFeature.properties.investmentNo == 0 ? DBNull.Value : roadWorkActivityFeature.properties.investmentNo);
                    insertComm.Parameters.AddWithValue("date_sks", roadWorkActivityFeature.properties.dateSks);
                    insertComm.Parameters.AddWithValue("date_kap", roadWorkActivityFeature.properties.dateKap);
                    insertComm.Parameters.AddWithValue("date_oks", roadWorkActivityFeature.properties.dateOks);
                    insertComm.Parameters.AddWithValue("date_gl_tba", roadWorkActivityFeature.properties.dateGlTba);
                    insertComm.Parameters.AddWithValue("comment", roadWorkActivityFeature.properties.comment);
                    insertComm.Parameters.AddWithValue("section", roadWorkActivityFeature.properties.section);
                    insertComm.Parameters.AddWithValue("type", roadWorkActivityFeature.properties.type);
                    insertComm.Parameters.AddWithValue("projecttype", roadWorkActivityFeature.properties.projectType);
                    insertComm.Parameters.AddWithValue("overarching_measure", roadWorkActivityFeature.properties.overarchingMeasure);
                    insertComm.Parameters.AddWithValue("desired_year", roadWorkActivityFeature.properties.desiredYear);
                    insertComm.Parameters.AddWithValue("prestudy", roadWorkActivityFeature.properties.prestudy);
                    insertComm.Parameters.AddWithValue("start_of_construction", roadWorkActivityFeature.properties.startOfConstruction);
                    insertComm.Parameters.AddWithValue("end_of_construction", roadWorkActivityFeature.properties.endOfConstruction);
                    insertComm.Parameters.AddWithValue("consult_due", roadWorkActivityFeature.properties.consultDue);
                    insertComm.Parameters.AddWithValue("project_no", roadWorkActivityFeature.properties.projectNo);
                    insertComm.Parameters.AddWithValue("geom", roadWorkActivityPoly);                    

                    insertComm.ExecuteNonQuery();

                    ActivityHistoryItem activityHistoryItem = new ActivityHistoryItem();
                    activityHistoryItem.uuid = Guid.NewGuid().ToString();
                    activityHistoryItem.changeDate = DateTime.Now;
                    activityHistoryItem.who = userFromDb.firstName + " " + userFromDb.lastName;
                    activityHistoryItem.what = "Massnahme erstellt";
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
                        string whatText = "Das Baubedürfnis '" + roadWorkActivityFeature.properties.roadWorkNeedsUuids[0] +
                                            "' wurde neu zugewiesen. Die neue Zuweisung ist: Zugewiesenes Bedürfnis";
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
                        roadWorkActivityFeature.geometry.coordinates == null ||
                        roadWorkActivityFeature.geometry.coordinates.Length < 3)
                {
                    _logger.LogWarning("Roadworkactivity has a geometry error.");
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "SSP-3";
                    return Ok(roadWorkActivityFeature);
                }

                Polygon roadWorkActivityPoly = roadWorkActivityFeature.geometry.getNtsPolygon();

                if (!roadWorkActivityPoly.IsSimple)
                {
                    _logger.LogWarning("Geometry of roadworkactivity " + roadWorkActivityFeature.properties.uuid +
                            " does not fulfill the criteria of geometrical simplicity.");
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "SSP-10";
                    return Ok(roadWorkActivityFeature);
                }

                if (!roadWorkActivityPoly.IsValid)
                {
                    _logger.LogWarning("Geometry of roadworkactivity " + roadWorkActivityFeature.properties.uuid +
                            " does not fulfill the criteria of geometrical validity.");
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "SSP-11";
                    return Ok(roadWorkActivityFeature);
                }

                ConfigurationData configData = AppConfigController.getConfigurationFromDb();
                // only if project area is greater than min area size:
                if (roadWorkActivityPoly.Area <= configData.minAreaSize)
                {
                    _logger.LogWarning("Roadworkneed area is less than or equal " + configData.minAreaSize + "qm.");
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "SSP-8";
                    return Ok(roadWorkActivityFeature);
                }

                // only if project area is smaller than max area size:
                if (roadWorkActivityPoly.Area > configData.maxAreaSize)
                {
                    _logger.LogWarning("Roadworkneed area is greater than " + configData.maxAreaSize + "qm.");
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "SSP-16";
                    return Ok(roadWorkActivityFeature);
                }

                if (roadWorkActivityFeature.properties.finishFrom > roadWorkActivityFeature.properties.finishTo)
                {
                    _logger.LogWarning("The finish from date of a roadworkactivity cannot be higher than its finish to date.");
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "SSP-19";
                    return Ok(roadWorkActivityFeature);
                }

                User userFromDb = LoginController.getAuthorizedUserFromDb(this.User, false);

                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {

                    pgConn.Open();

                    using NpgsqlTransaction trans = pgConn.BeginTransaction();

                    if (!User.IsInRole("administrator"))
                    {
                        NpgsqlCommand selectManagerOfActivityComm = pgConn.CreateCommand();
                        selectManagerOfActivityComm.CommandText = @"SELECT u.e_mail
                                    FROM ""wtb_ssp_roadworkactivities"" r
                                    LEFT JOIN ""wtb_ssp_users"" u ON r.projectmanager = u.uuid
                                    WHERE r.uuid=@uuid";
                        selectManagerOfActivityComm.Parameters.AddWithValue("uuid", new Guid(roadWorkActivityFeature.properties.uuid));

                        string eMailOfManager = "";

                        using (NpgsqlDataReader reader = selectManagerOfActivityComm.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                eMailOfManager = reader.IsDBNull(0) ? "" : reader.GetString(0);
                            }
                        }

                        string mailOfLoggedInUser = User.FindFirstValue(ClaimTypes.Email);

                        if (mailOfLoggedInUser != eMailOfManager)
                        {
                            _logger.LogWarning("User " + mailOfLoggedInUser + " has no right to edit " +
                                "roadwork activity " + roadWorkActivityFeature.properties.uuid + " but tried " +
                                "to edit it.");
                            roadWorkActivityFeature.errorMessage = "SSP-14";
                            return Ok(roadWorkActivityFeature);
                        }

                    }

                    NpgsqlCommand selectStatusComm = pgConn.CreateCommand();
                    selectStatusComm.CommandText = @"SELECT status FROM ""wtb_ssp_roadworkactivities""
                                                        WHERE uuid=@uuid";
                    selectStatusComm.Parameters.AddWithValue("uuid", new Guid(roadWorkActivityFeature.properties.uuid));

                    string statusOfActivityInDb = "";
                    using (NpgsqlDataReader reader = selectStatusComm.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            statusOfActivityInDb = reader.IsDBNull(0) ? "" : reader.GetString(0);
                        }
                    }

                    NpgsqlCommand updateComm = pgConn.CreateCommand();
                    updateComm.CommandText = @"UPDATE ""wtb_ssp_roadworkactivities""
                                    SET name=@name, projectmanager=@projectmanager,
                                    traffic_agent=@traffic_agent, description=@description,
                                    comment=@comment, section=@section, type=@type,
                                    projecttype=@projecttype, overarching_measure=@overarching_measure,
                                    desired_year=@desired_year, prestudy=@prestudy, 
                                    start_of_construction=@start_of_construction,
                                    end_of_construction=@end_of_construction, consult_due=@consult_due,
                                    last_modified=@last_modified, project_no=@project_no,
                                    date_from=@date_from, date_to=@date_to,
                                    costs=@costs, costs_type=@costs_type, status=@status,
                                    billing_address1=@billing_address1,
                                    billing_address2=@billing_address2,
                                    in_internet=@in_internet, investment_no=@investment_no,
                                    date_sks=@date_sks, date_kap=@date_kap, date_oks=@date_oks,
                                    date_gl_tba=@date_gl_tba,
                                    geom=@geom
                                    WHERE uuid=@uuid";

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
                    updateComm.Parameters.AddWithValue("date_from", roadWorkActivityFeature.properties.finishFrom);
                    updateComm.Parameters.AddWithValue("date_to", roadWorkActivityFeature.properties.finishTo);
                    updateComm.Parameters.AddWithValue("costs", roadWorkActivityFeature.properties.costs);
                    updateComm.Parameters.AddWithValue("costs_type", roadWorkActivityFeature.properties.costsType.code);
                    updateComm.Parameters.AddWithValue("status", roadWorkActivityFeature.properties.status.code);
                    updateComm.Parameters.AddWithValue("in_internet", roadWorkActivityFeature.properties.isInInternet);
                    updateComm.Parameters.AddWithValue("billing_address1", roadWorkActivityFeature.properties.billingAddress1);
                    updateComm.Parameters.AddWithValue("billing_address2", roadWorkActivityFeature.properties.billingAddress2);
                    updateComm.Parameters.AddWithValue("investment_no", roadWorkActivityFeature.properties.investmentNo);
                    updateComm.Parameters.AddWithValue("date_sks", roadWorkActivityFeature.properties.dateSks);
                    updateComm.Parameters.AddWithValue("date_kap", roadWorkActivityFeature.properties.dateKap);
                    updateComm.Parameters.AddWithValue("date_oks", roadWorkActivityFeature.properties.dateOks);
                    updateComm.Parameters.AddWithValue("date_gl_tba", roadWorkActivityFeature.properties.dateGlTba);
                    updateComm.Parameters.AddWithValue("comment", roadWorkActivityFeature.properties.comment);
                    updateComm.Parameters.AddWithValue("section", roadWorkActivityFeature.properties.section);
                    updateComm.Parameters.AddWithValue("type", roadWorkActivityFeature.properties.type);
                    updateComm.Parameters.AddWithValue("projecttype", roadWorkActivityFeature.properties.projectType);
                    updateComm.Parameters.AddWithValue("overarching_measure", roadWorkActivityFeature.properties.overarchingMeasure);
                    updateComm.Parameters.AddWithValue("desired_year", roadWorkActivityFeature.properties.desiredYear);
                    updateComm.Parameters.AddWithValue("prestudy", roadWorkActivityFeature.properties.prestudy);
                    updateComm.Parameters.AddWithValue("start_of_construction", roadWorkActivityFeature.properties.startOfConstruction);
                    updateComm.Parameters.AddWithValue("end_of_construction", roadWorkActivityFeature.properties.endOfConstruction);
                    updateComm.Parameters.AddWithValue("consult_due", roadWorkActivityFeature.properties.consultDue);
                    updateComm.Parameters.AddWithValue("project_no", roadWorkActivityFeature.properties.projectNo);
                    updateComm.Parameters.AddWithValue("geom", roadWorkActivityPoly);
                    updateComm.Parameters.AddWithValue("uuid", new Guid(roadWorkActivityFeature.properties.uuid));

                    updateComm.ExecuteNonQuery();

                    if (statusOfActivityInDb != null && statusOfActivityInDb.Length != 0)
                    {
                        if (statusOfActivityInDb != roadWorkActivityFeature.properties.status.code)
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
                            if (roadWorkActivityFeature.properties.status.code == "review")
                                whatText += "in Prüfung";
                            else if (roadWorkActivityFeature.properties.status.code == "inconsult")
                                whatText += "in Bedarfsklärung";
                            else if (roadWorkActivityFeature.properties.status.code == "verified")
                                whatText += "verifiziert";
                            else if (roadWorkActivityFeature.properties.status.code == "reporting")
                                whatText += "in Stellungnahme";
                            else if (roadWorkActivityFeature.properties.status.code == "coordinated")
                                whatText += "koordiniert";
                            insertHistoryComm.Parameters.AddWithValue("what", whatText);
                            insertHistoryComm.ExecuteNonQuery();

                            NpgsqlCommand updateActivityStatusComm = pgConn.CreateCommand();
                            updateActivityStatusComm.CommandText = @"UPDATE wtb_ssp_roadworkactivities
                                                    SET status=@status WHERE uuid=@uuid";
                            updateActivityStatusComm.Parameters.AddWithValue("status", roadWorkActivityFeature.properties.status.code);
                            updateActivityStatusComm.Parameters.AddWithValue("uuid", new Guid(roadWorkActivityFeature.properties.uuid));
                            updateActivityStatusComm.ExecuteNonQuery();

                            if (roadWorkActivityFeature.properties.status.code == "verified")
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
                                updateNeedsStatusComm.Parameters.AddWithValue("status", roadWorkActivityFeature.properties.status.code);
                                updateNeedsStatusComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadWorkActivityFeature.properties.uuid));
                                updateNeedsStatusComm.ExecuteNonQuery();
                            }
                        }
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

                if (userFromDb.role.code == "trafficmanager" || userFromDb.role.code == "administrator")
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


        // DELETE /roadworkactivity?uuid=...
        [HttpDelete]
        [Authorize(Roles = "administrator")]
        public ActionResult<ErrorMessage> DeleteActivity(string uuid)
        {
            ErrorMessage errorResult = new ErrorMessage();

            try
            {

                if (uuid == null)
                {
                    _logger.LogWarning("No uuid provided by user in delete roadwork activity process. " +
                                "Thus process is canceled, no roadwork activity is deleted.");
                    errorResult.errorMessage = "SSP-15";
                    return Ok(errorResult);
                }

                uuid = uuid.ToLower().Trim();

                if (uuid == "")
                {
                    _logger.LogWarning("No uuid provided by user in delete roadwork activity process. " +
                                "Thus process is canceled, no roadwork activity is deleted.");
                    errorResult.errorMessage = "SSP-15";
                    return Ok(errorResult);
                }

                int noAffectedRows = 0;
                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    pgConn.Open();
                    using (NpgsqlTransaction deleteTransAction = pgConn.BeginTransaction())
                    {
                        NpgsqlCommand updateComm = pgConn.CreateCommand();
                        updateComm.CommandText = @"UPDATE ""wtb_ssp_roadworkneeds""
                                SET status='requirement'
                                WHERE uuid IN
                                (SELECT n.uuid
                                    FROM ""wtb_ssp_roadworkneeds"" n
                                    LEFT JOIN ""wtb_ssp_activities_to_needs"" an ON an.uuid_roadwork_need = n.uuid
                                    WHERE an.uuid_roadwork_activity=@uuid_roadwork_activity)";
                        updateComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(uuid));
                        updateComm.ExecuteNonQuery();

                        updateComm.CommandText = @"DELETE FROM ""wtb_ssp_activities_to_needs""
                                WHERE uuid_roadwork_activity=@uuid_roadwork_activity";
                        updateComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(uuid));
                        updateComm.ExecuteNonQuery();

                        NpgsqlCommand deleteComm = pgConn.CreateCommand();
                        deleteComm = pgConn.CreateCommand();
                        deleteComm.CommandText = @"DELETE FROM ""wtb_ssp_roadworkactivities""
                                WHERE uuid=@uuid";
                        deleteComm.Parameters.AddWithValue("uuid", new Guid(uuid));
                        noAffectedRows = deleteComm.ExecuteNonQuery();
                        deleteTransAction.Commit();
                    }

                    pgConn.Close();
                }

                if (noAffectedRows == 1)
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

    }
}
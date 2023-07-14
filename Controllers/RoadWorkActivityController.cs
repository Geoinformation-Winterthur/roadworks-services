using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Geometries;
using Npgsql;
using roadwork_portal_service.Configuration;
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
        [Authorize(Roles = "orderer,territorymanager,administrator")]
        public async Task<IEnumerable<RoadWorkActivityFeature>> GetActivities
                                (string? uuid = "", string? status = "", bool summary = false)
        {
            List<RoadWorkActivityFeature> projectsFromDb = new List<RoadWorkActivityFeature>();
            // get data of current user from database:
            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand selectComm = pgConn.CreateCommand();
                selectComm.CommandText = @"SELECT r.uuid, r.name, r.managementarea, m.manager, am.first_name, am.last_name,
                            m.substitute_manager, sam.first_name, sam.last_name, r.projectmanager, pm.first_name, pm.last_name, r.traffic_agent,
                            ta.first_name, ta.last_name, description, created, last_modified, r.finish_from, r.finish_to,
                            r.costs, c.code, c.name, am.e_mail, s.code, s.name, r.geom
                        FROM ""roadworkactivities"" r
                        LEFT JOIN ""managementareas"" m ON r.managementarea = m.uuid
                        LEFT JOIN ""users"" am ON m.manager = am.uuid
                        LEFT JOIN ""users"" sam ON m.substitute_manager = sam.uuid
                        LEFT JOIN ""users"" pm ON r.projectmanager = pm.uuid
                        LEFT JOIN ""users"" ta ON r.traffic_agent = ta.uuid
                        LEFT JOIN ""status"" s ON r.status = s.code
                        LEFT JOIN ""costtypes"" c ON r.costs_type = c.code";

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

                        ManagementAreaFeature managementAreaFeature = new ManagementAreaFeature();
                        managementAreaFeature.properties.uuid = reader.IsDBNull(2) ? "" : reader.GetGuid(2).ToString();

                        User areaManager = new User();
                        areaManager.uuid = reader.IsDBNull(3) ? "" : reader.GetGuid(3).ToString();
                        areaManager.firstName = reader.IsDBNull(4) ? "" : reader.GetString(4);
                        areaManager.lastName = reader.IsDBNull(5) ? "" : reader.GetString(5);
                        managementAreaFeature.properties.manager = areaManager;

                        User areaSubstituteManager = new User();
                        areaSubstituteManager.uuid = reader.IsDBNull(6) ? "" : reader.GetGuid(6).ToString();
                        areaSubstituteManager.firstName = reader.IsDBNull(7) ? "" : reader.GetString(7);
                        areaSubstituteManager.lastName = reader.IsDBNull(8) ? "" : reader.GetString(8);
                        managementAreaFeature.properties.substituteManager = areaSubstituteManager;

                        projectFeatureFromDb.properties.managementarea = managementAreaFeature;

                        User projectManager = new User();
                        projectManager.uuid = reader.IsDBNull(9) ? "" : reader.GetGuid(9).ToString();
                        projectManager.firstName = reader.IsDBNull(10) ? "" : reader.GetString(10);
                        projectManager.lastName = reader.IsDBNull(11) ? "" : reader.GetString(11);
                        projectFeatureFromDb.properties.projectManager = projectManager;

                        User trafficAgent = new User();
                        trafficAgent.uuid = reader.IsDBNull(12) ? "" : reader.GetGuid(12).ToString();
                        trafficAgent.firstName = reader.IsDBNull(13) ? "" : reader.GetString(13);
                        trafficAgent.lastName = reader.IsDBNull(14) ? "" : reader.GetString(14);
                        projectFeatureFromDb.properties.trafficAgent = trafficAgent;

                        projectFeatureFromDb.properties.description = reader.IsDBNull(15) ? "" : reader.GetString(15);
                        projectFeatureFromDb.properties.created = reader.IsDBNull(16) ? DateTime.MinValue : reader.GetDateTime(16);
                        projectFeatureFromDb.properties.lastModified = reader.IsDBNull(17) ? DateTime.MinValue : reader.GetDateTime(17);
                        projectFeatureFromDb.properties.finishFrom = reader.IsDBNull(18) ? DateTime.MinValue : reader.GetDateTime(18);
                        projectFeatureFromDb.properties.finishTo = reader.IsDBNull(19) ? DateTime.MinValue : reader.GetDateTime(19);
                        projectFeatureFromDb.properties.costs = reader.IsDBNull(20) ? 0m : reader.GetDecimal(20);

                        CostTypes ct = new CostTypes();
                        ct.code = reader.IsDBNull(21) ? "" : reader.GetString(21);
                        ct.name = reader.IsDBNull(22) ? "" : reader.GetString(22);
                        projectFeatureFromDb.properties.costsType = ct;

                        string managerMailAddress = reader.IsDBNull(23) ? "" : reader.GetString(23);
                        string mailOfLoggedInUser = User.FindFirstValue(ClaimTypes.Email);
                        if (User.IsInRole("administrator") || managerMailAddress == mailOfLoggedInUser)
                        {
                            projectFeatureFromDb.properties.isEditingAllowed = true;
                        }

                        Status statusFromDb = new Status();
                        statusFromDb.code = reader.IsDBNull(24) ? "" : reader.GetString(24);
                        statusFromDb.name = reader.IsDBNull(25) ? "" : reader.GetString(25);
                        projectFeatureFromDb.properties.status = statusFromDb;

                        Polygon ntsPoly = reader.IsDBNull(26) ? Polygon.Empty : reader.GetValue(26) as Polygon;
                        projectFeatureFromDb.geometry = new RoadworkPolygon(ntsPoly);

                        projectsFromDb.Add(projectFeatureFromDb);
                    }
                }

                foreach (RoadWorkActivityFeature activityFromDb in projectsFromDb)
                {
                    NpgsqlCommand selectRoadWorkNeedComm = pgConn.CreateCommand();
                    selectRoadWorkNeedComm.CommandText = @"SELECT uuid
                            FROM ""roadworkneeds""
                            WHERE roadworkactivity = @roadworkactivity_uuid";
                    selectRoadWorkNeedComm.Parameters.AddWithValue("roadworkactivity_uuid", new Guid(activityFromDb.properties.uuid));

                    using (NpgsqlDataReader roadWorkNeedReader = await selectRoadWorkNeedComm.ExecuteReaderAsync())
                    {
                        List<string> roadWorkNeedsUuids = new List<string>();
                        while (roadWorkNeedReader.Read())
                        {
                            if (!roadWorkNeedReader.IsDBNull(0))
                                roadWorkNeedsUuids.Add(roadWorkNeedReader.GetGuid(0).ToString());
                        }
                        activityFromDb.properties.roadWorkNeedsUuids = roadWorkNeedsUuids.ToArray();
                    }
                }

                pgConn.Close();
            }

            return projectsFromDb.ToArray();
        }

        // POST roadworkactivity/
        [HttpPost]
        [Authorize(Roles = "territorymanager,administrator")]
        public async Task<ActionResult<RoadWorkActivityFeature>> AddActivity([FromBody] RoadWorkActivityFeature roadWorkActivityFeature)
        {
            try
            {
                Polygon roadWorkActivityPoly = roadWorkActivityFeature.geometry.getNtsPolygon();
                Coordinate[] coordinates = roadWorkActivityPoly.Coordinates;

                if (coordinates.Length < 3)
                {
                    _logger.LogWarning("Roadwork activity polygon has less than 3 coordinates.");
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "KOPAL-7";
                    return Ok(roadWorkActivityFeature);
                }

                ConfigurationData configData = AppConfigController.getConfigurationFromDb();

                // only if project area is greater than min area size:
                if (roadWorkActivityPoly.Area <= configData.minAreaSize)
                {
                    _logger.LogWarning("Roadworkneed area is less than or equal " + configData.minAreaSize + "qm.");
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "KOPAL-8";
                    return Ok(roadWorkActivityFeature);
                }

                // only if project area is smaller than max area size:
                if (roadWorkActivityPoly.Area > configData.maxAreaSize)
                {
                    _logger.LogWarning("Roadworkneed area is greater than " + configData.maxAreaSize + "qm.");
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "KOPAL-16";
                    return Ok(roadWorkActivityFeature);
                }

                if (roadWorkActivityFeature.properties.finishFrom > roadWorkActivityFeature.properties.finishTo)
                {
                    _logger.LogWarning("The finish from date of a roadworkactivity cannot be higher than its finish to date.");
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "KOPAL-19";
                    return Ok(roadWorkActivityFeature);
                }

                User userFromDb = LoginController.getAuthorizedUserFromDb(this.User);
                roadWorkActivityFeature.properties.projectManager = userFromDb;

                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {

                    pgConn.Open();

                    List<Guid> roadWorkNeedsUuidsList = new List<Guid>();
                    NpgsqlCommand selectIntersectingNeedsComm = pgConn.CreateCommand();
                    selectIntersectingNeedsComm.CommandText = @"SELECT uuid
                                            FROM ""roadworkneeds""
                                            WHERE ST_Intersects(@geom, geom)";
                    selectIntersectingNeedsComm.Parameters.AddWithValue("geom", roadWorkActivityPoly);

                    using (NpgsqlDataReader reader = await selectIntersectingNeedsComm.ExecuteReaderAsync())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                roadWorkNeedsUuidsList.Add(reader.GetGuid(0));
                            }
                        }
                    }

                    DateTime roadWorkActivityFinishFrom = DateTime.MaxValue;
                    DateTime roadWorkActivityFinishTo = DateTime.MinValue;
                    if (roadWorkNeedsUuidsList.Count() != 0)
                    {
                        NpgsqlCommand selectPeriodComm = pgConn.CreateCommand();
                        selectPeriodComm.CommandText = @"SELECT finish_optimum_from, finish_optimum_to
                                                    FROM ""roadworkneeds""
                                                    WHERE uuid = ANY (@uuids)";
                        selectPeriodComm.Parameters.AddWithValue("uuids", roadWorkNeedsUuidsList);
                        using (NpgsqlDataReader reader = await selectPeriodComm.ExecuteReaderAsync())
                        {
                            DateTime roadWorkActivityFinishFromTemp;
                            DateTime roadWorkActivityFinishToTemp;
                            while (reader.Read())
                            {
                                roadWorkActivityFinishFromTemp = reader.IsDBNull(0) ? DateTime.MaxValue : reader.GetDateTime(0);
                                if (roadWorkActivityFinishFromTemp < roadWorkActivityFinishFrom)
                                {
                                    roadWorkActivityFinishFrom = roadWorkActivityFinishFromTemp;
                                }
                                roadWorkActivityFinishToTemp = reader.IsDBNull(1) ? DateTime.MinValue : reader.GetDateTime(1);
                                if (roadWorkActivityFinishToTemp > roadWorkActivityFinishTo)
                                {
                                    roadWorkActivityFinishTo = roadWorkActivityFinishToTemp;
                                }
                            }
                        }
                    }

                    NpgsqlCommand selectMgmtAreaComm = pgConn.CreateCommand();
                    selectMgmtAreaComm.CommandText = @"SELECT a.uuid,
                                        m.first_name, m.last_name,
                                        s.first_name, s.last_name
                                    FROM ""managementareas"" a
                                    LEFT JOIN ""users"" m ON a.manager = m.uuid
                                    LEFT JOIN ""users"" s ON a.substitute_manager = s.uuid
                                    WHERE ST_Area(ST_Intersection(@geom, geom)) > 0
                                    ORDER BY ST_Area(ST_Intersection(@geom, geom)) DESC
                                    LIMIT 1";

                    selectMgmtAreaComm.Parameters.AddWithValue("geom", roadWorkActivityPoly);

                    roadWorkActivityFeature.properties.managementarea = new ManagementAreaFeature();

                    using (NpgsqlDataReader reader = await selectMgmtAreaComm.ExecuteReaderAsync())
                    {
                        if (reader.Read())
                        {
                            roadWorkActivityFeature.properties.managementarea.properties.uuid = reader.IsDBNull(0) ? "" : reader.GetGuid(0).ToString();
                            roadWorkActivityFeature.properties.managementarea.properties.manager.firstName = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            roadWorkActivityFeature.properties.managementarea.properties.manager.lastName = reader.IsDBNull(2) ? "" : reader.GetString(2);
                            roadWorkActivityFeature.properties.managementarea.properties.substituteManager.firstName = reader.IsDBNull(3) ? "" : reader.GetString(3);
                            roadWorkActivityFeature.properties.managementarea.properties.substituteManager.lastName = reader.IsDBNull(4) ? "" : reader.GetString(4);
                        }
                    }

                    if (roadWorkActivityFeature.properties.managementarea.properties.uuid == "")
                    {
                        _logger.LogWarning("New roadworkneed does not lie in any management area.");
                        roadWorkActivityFeature.errorMessage = "KOPAL-9";
                        return Ok(roadWorkActivityFeature);
                    }

                    roadWorkActivityFeature.properties.uuid = Guid.NewGuid().ToString();

                    await using NpgsqlTransaction trans = await pgConn.BeginTransactionAsync();

                    NpgsqlCommand insertComm = pgConn.CreateCommand();
                    insertComm.CommandText = @"INSERT INTO ""roadworkactivities""
                                    (uuid, name, managementarea, projectmanager, traffic_agent, description,
                                    created, last_modified, finish_from, finish_to,
                                    costs, costs_type, status, geom)
                                    VALUES (@uuid, @name, @managementarea, @projectmanager, @traffic_agent,
                                    @description, current_timestamp, @last_modified, @finish_from,
                                    @finish_to, @costs, @costs_type, @status, @geom)";
                    insertComm.Parameters.AddWithValue("uuid", new Guid(roadWorkActivityFeature.properties.uuid));
                    if (roadWorkActivityFeature.properties.managementarea.properties.uuid != "")
                    {
                        insertComm.Parameters.AddWithValue("managementarea", new Guid(roadWorkActivityFeature.properties.managementarea.properties.uuid));
                    }
                    else
                    {
                        insertComm.Parameters.AddWithValue("managementarea", DBNull.Value);
                    }
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
                    insertComm.Parameters.AddWithValue("last_modified", roadWorkActivityFeature.properties.lastModified);
                    insertComm.Parameters.AddWithValue("finish_from", roadWorkActivityFinishFrom);
                    insertComm.Parameters.AddWithValue("finish_to", roadWorkActivityFinishTo);
                    insertComm.Parameters.AddWithValue("costs", roadWorkActivityFeature.properties.costs);
                    insertComm.Parameters.AddWithValue("costs_type", "fullcost"); // TODO make this dynamic 
                    insertComm.Parameters.AddWithValue("status", "inwork");
                    insertComm.Parameters.AddWithValue("geom", roadWorkActivityPoly);

                    await insertComm.ExecuteNonQueryAsync();

                    if (roadWorkNeedsUuidsList.Count() != 0)
                    {
                        NpgsqlCommand updateComm = pgConn.CreateCommand();
                        updateComm.CommandText = @"UPDATE ""roadworkneeds"" SET
                                    roadworkactivity = @roadworkactivity,
                                    activityrelationtype = 'nonassignedneed'
                                    WHERE uuid = ANY (@uuids)";
                        updateComm.Parameters.AddWithValue("roadworkactivity", new Guid(roadWorkActivityFeature.properties.uuid));
                        updateComm.Parameters.AddWithValue("uuids", roadWorkNeedsUuidsList);
                        await updateComm.ExecuteNonQueryAsync();

                    }
                    await trans.CommitAsync();

                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                roadWorkActivityFeature = new RoadWorkActivityFeature();
                roadWorkActivityFeature.errorMessage = "KOPAL-3";
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
                    roadWorkActivityFeature.errorMessage = "KOPAL-3";
                    return Ok(roadWorkActivityFeature);
                }

                if (roadWorkActivityFeature.geometry == null ||
                        roadWorkActivityFeature.geometry.coordinates == null ||
                        roadWorkActivityFeature.geometry.coordinates.Length < 3)
                {
                    _logger.LogWarning("Roadworkactivity has a geometry error.");
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "KOPAL-3";
                    return Ok(roadWorkActivityFeature);
                }

                Polygon roadWorkActivityPoly = roadWorkActivityFeature.geometry.getNtsPolygon();

                if (!roadWorkActivityPoly.IsSimple)
                {
                    _logger.LogWarning("Geometry of roadworkactivity " + roadWorkActivityFeature.properties.uuid +
                            " does not fulfill the criteria of geometrical simplicity.");
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "KOPAL-10";
                    return Ok(roadWorkActivityFeature);
                }

                if (!roadWorkActivityPoly.IsValid)
                {
                    _logger.LogWarning("Geometry of roadworkactivity " + roadWorkActivityFeature.properties.uuid +
                            " does not fulfill the criteria of geometrical validity.");
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "KOPAL-11";
                    return Ok(roadWorkActivityFeature);
                }

                ConfigurationData configData = AppConfigController.getConfigurationFromDb();
                // only if project area is greater than min area size:
                if (roadWorkActivityPoly.Area <= configData.minAreaSize)
                {
                    _logger.LogWarning("Roadworkneed area is less than or equal " + configData.minAreaSize + "qm.");
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "KOPAL-8";
                    return Ok(roadWorkActivityFeature);
                }

                // only if project area is smaller than max area size:
                if (roadWorkActivityPoly.Area > configData.maxAreaSize)
                {
                    _logger.LogWarning("Roadworkneed area is greater than " + configData.maxAreaSize + "qm.");
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "KOPAL-16";
                    return Ok(roadWorkActivityFeature);
                }

                if (roadWorkActivityFeature.properties.finishFrom > roadWorkActivityFeature.properties.finishTo)
                {
                    _logger.LogWarning("The finish from date of a roadworkactivity cannot be higher than its finish to date.");
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "KOPAL-19";
                    return Ok(roadWorkActivityFeature);
                }

                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {

                    pgConn.Open();

                    using NpgsqlTransaction trans = pgConn.BeginTransaction();

                    if (!User.IsInRole("administrator"))
                    {
                        NpgsqlCommand selectManagerOfActivityComm = pgConn.CreateCommand();
                        selectManagerOfActivityComm.CommandText = @"SELECT u.e_mail
                                    FROM ""roadworkactivities"" r
                                    LEFT JOIN ""users"" u ON r.projectmanager = u.uuid
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
                            roadWorkActivityFeature.errorMessage = "KOPAL-14";
                            return Ok(roadWorkActivityFeature);
                        }

                    }

                    NpgsqlCommand selectStatusComm = pgConn.CreateCommand();
                    selectStatusComm.CommandText = @"SELECT status FROM ""roadworkactivities""
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
                    updateComm.CommandText = @"UPDATE ""roadworkactivities""
                                    SET name=@name, managementarea=@managementarea, projectmanager=@projectmanager,
                                    traffic_agent=@traffic_agent, description=@description,
                                    last_modified=current_timestamp,
                                    finish_from=@finish_from, finish_to=@finish_to,
                                    costs=@costs, costs_type=@costs_type, status=@status, geom=@geom
                                    WHERE uuid=@uuid";

                    updateComm.Parameters.AddWithValue("name", roadWorkActivityFeature.properties.name);
                    if (roadWorkActivityFeature.properties.managementarea.properties.uuid != "")
                    {
                        updateComm.Parameters.AddWithValue("managementarea",
                                new Guid(roadWorkActivityFeature.properties.managementarea.properties.uuid));
                    }
                    else
                    {
                        updateComm.Parameters.AddWithValue("managementarea", DBNull.Value);
                    }
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
                    updateComm.Parameters.AddWithValue("finish_from", roadWorkActivityFeature.properties.finishFrom);
                    updateComm.Parameters.AddWithValue("finish_to", roadWorkActivityFeature.properties.finishTo);
                    updateComm.Parameters.AddWithValue("costs", roadWorkActivityFeature.properties.costs);
                    updateComm.Parameters.AddWithValue("costs_type", roadWorkActivityFeature.properties.costsType.code);
                    updateComm.Parameters.AddWithValue("status", roadWorkActivityFeature.properties.status.code);
                    updateComm.Parameters.AddWithValue("geom", roadWorkActivityPoly);
                    updateComm.Parameters.AddWithValue("uuid", new Guid(roadWorkActivityFeature.properties.uuid));

                    updateComm.ExecuteNonQuery();

                    NpgsqlCommand selectIntersectingNeeds = pgConn.CreateCommand();
                    selectIntersectingNeeds.CommandText = @"SELECT uuid FROM ""roadworkneeds"" WHERE ST_Intersects(@geom, geom)";
                    selectIntersectingNeeds.Parameters.AddWithValue("geom", roadWorkActivityPoly);

                    List<Guid> intersectingNeedsUuids = new List<Guid>();
                    using (NpgsqlDataReader reader = selectIntersectingNeeds.ExecuteReader())
                    {
                        while (reader.Read())
                        {
                            if (!reader.IsDBNull(0))
                            {
                                intersectingNeedsUuids.Add(reader.GetGuid(0));
                            }
                        }
                    }

                    if (intersectingNeedsUuids.Count != 0)
                    {
                        NpgsqlCommand cleanActivityOfNeedsComm = pgConn.CreateCommand();
                        cleanActivityOfNeedsComm.CommandText = @"UPDATE roadworkneeds
                                        SET roadworkactivity = NULL, activityrelationtype = NULL
                                        WHERE activityrelationtype = 'nonassignedneed'
                                            AND roadworkactivity = @activity_uuid";
                        cleanActivityOfNeedsComm.Parameters.AddWithValue("activity_uuid", new Guid(roadWorkActivityFeature.properties.uuid));
                        cleanActivityOfNeedsComm.ExecuteNonQuery();


                        NpgsqlCommand updateActivityOfNeedsComm = pgConn.CreateCommand();
                        updateActivityOfNeedsComm.CommandText = @"UPDATE roadworkneeds
                                        SET roadworkactivity = @activity_uuid,
                                            activityrelationtype = 'nonassignedneed'
                                        WHERE activityrelationtype <> 'assignedneed'
                                            AND uuid = ANY (:needs_uuids)";
                        updateActivityOfNeedsComm.Parameters.AddWithValue("activity_uuid", new Guid(roadWorkActivityFeature.properties.uuid));
                        updateActivityOfNeedsComm.Parameters.AddWithValue("needs_uuids", intersectingNeedsUuids);
                        updateActivityOfNeedsComm.ExecuteNonQuery();

                        roadWorkActivityFeature.properties.roadWorkNeedsUuids = new string[intersectingNeedsUuids.Count];
                        int i = 0;
                        foreach (Guid intersectingNeedUuid in intersectingNeedsUuids)
                        {
                            roadWorkActivityFeature.properties.roadWorkNeedsUuids[i] = intersectingNeedUuid.ToString();
                            i++;
                        }

                    }

                    if (statusOfActivityInDb != null && statusOfActivityInDb.Length != 0)
                    {
                        if (statusOfActivityInDb != roadWorkActivityFeature.properties.status.code)
                        {
                            NpgsqlCommand updateActivityStatusComm = pgConn.CreateCommand();
                            updateActivityStatusComm.CommandText = @"UPDATE roadworkactivities
                                                    SET status=@status WHERE uuid=@uuid";
                            updateActivityStatusComm.Parameters.AddWithValue("status", roadWorkActivityFeature.properties.status.code);
                            updateActivityStatusComm.Parameters.AddWithValue("uuid", new Guid(roadWorkActivityFeature.properties.uuid));
                            updateActivityStatusComm.ExecuteNonQuery();

                            if (roadWorkActivityFeature.properties.status.code == "inwork")
                            {
                                NpgsqlCommand updateNeedsStatusComm = pgConn.CreateCommand();
                                updateNeedsStatusComm.CommandText = @"UPDATE roadworkneeds
                                                    SET status='coordinated'
                                                    WHERE activityrelationtype='assignedneed'
                                                        AND roadworkactivity=@activity_uuid";
                                updateNeedsStatusComm.Parameters.AddWithValue("activity_uuid", new Guid(roadWorkActivityFeature.properties.uuid));
                                updateNeedsStatusComm.ExecuteNonQuery();
                            }
                            else
                            {
                                NpgsqlCommand updateNeedsStatusComm = pgConn.CreateCommand();
                                updateNeedsStatusComm.CommandText = @"UPDATE roadworkneeds
                                                    SET status=@status
                                                    WHERE activityrelationtype='assignedneed'
                                                        AND roadworkactivity=@activity_uuid";
                                updateNeedsStatusComm.Parameters.AddWithValue("status", roadWorkActivityFeature.properties.status.code);
                                updateNeedsStatusComm.Parameters.AddWithValue("activity_uuid", new Guid(roadWorkActivityFeature.properties.uuid));
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
                roadWorkActivityFeature.errorMessage = "KOPAL-3";
                return Ok(roadWorkActivityFeature);
            }

            return Ok(roadWorkActivityFeature);
        }

        // DELETE /roadworkactivity?uuid=...
        [HttpDelete]
        [Authorize(Roles = "territorymanager,administrator")]
        public ActionResult<ErrorMessage> DeleteActivity(string uuid)
        {
            ErrorMessage errorResult = new ErrorMessage();

            try
            {

                if (uuid == null)
                {
                    _logger.LogWarning("No uuid provided by user in delete roadwork activity process. " +
                                "Thus process is canceled, no roadwork activity is deleted.");
                    errorResult.errorMessage = "KOPAL-15";
                    return Ok(errorResult);
                }

                uuid = uuid.ToLower().Trim();

                if (uuid == "")
                {
                    _logger.LogWarning("No uuid provided by user in delete roadwork activity process. " +
                                "Thus process is canceled, no roadwork activity is deleted.");
                    errorResult.errorMessage = "KOPAL-15";
                    return Ok(errorResult);
                }

                int noAffectedRows = 0;
                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    pgConn.Open();
                    using (NpgsqlTransaction deleteTransAction = pgConn.BeginTransaction())
                    {
                        NpgsqlCommand updateComm = pgConn.CreateCommand();
                        updateComm.CommandText = @"UPDATE ""roadworkneeds""
                                SET roadworkactivity=NULL, activityrelationtype=NULL,
                                    status='notcoord'
                                WHERE roadworkactivity=@roadworkactivity";
                        updateComm.Parameters.AddWithValue("roadworkactivity", new Guid(uuid));
                        updateComm.ExecuteNonQuery();

                        NpgsqlCommand deleteComm = pgConn.CreateCommand();
                        deleteComm = pgConn.CreateCommand();
                        deleteComm.CommandText = @"DELETE FROM ""roadworkactivities""
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
                errorResult.errorMessage = "KOPAL-3";
                return Ok(errorResult);

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                errorResult.errorMessage = "KOPAL-3";
                return Ok(errorResult);
            }

        }

    }
}
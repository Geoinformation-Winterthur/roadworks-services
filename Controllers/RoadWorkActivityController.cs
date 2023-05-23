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
        public async Task<IEnumerable<RoadWorkActivityFeature>> GetActivities(string? uuid = "", bool summary = false)
        {
            List<RoadWorkActivityFeature> projectsFromDb = new List<RoadWorkActivityFeature>();
            // get data of current user from database:
            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand selectComm = pgConn.CreateCommand();
                selectComm.CommandText = @"SELECT r.uuid, r.name, r.managementarea, m.manager, am.first_name, am.last_name,
                            r.projectmanager, pm.first_name, pm.last_name, r.traffic_agent,
                            ta.first_name, ta.last_name, description, created, last_modified, r.finish_from, r.finish_to,
                            r.costs, c.code, c.name, r.geom
                        FROM ""roadworkactivities"" r
                        LEFT JOIN ""managementareas"" m ON r.managementarea = m.uuid
                        LEFT JOIN ""users"" am ON m.manager = am.uuid
                        LEFT JOIN ""users"" pm ON r.projectmanager = pm.uuid
                        LEFT JOIN ""users"" ta ON r.traffic_agent = ta.uuid
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
                        projectFeatureFromDb.properties.managementarea = managementAreaFeature;

                        User projectManager = new User();
                        projectManager.uuid = reader.IsDBNull(6) ? "" : reader.GetGuid(6).ToString();
                        projectManager.firstName = reader.IsDBNull(7) ? "" : reader.GetString(7);
                        projectManager.lastName = reader.IsDBNull(8) ? "" : reader.GetString(8);
                        projectFeatureFromDb.properties.projectManager = projectManager;

                        User trafficAgent = new User();
                        trafficAgent.uuid = reader.IsDBNull(9) ? "" : reader.GetGuid(9).ToString();
                        trafficAgent.firstName = reader.IsDBNull(10) ? "" : reader.GetString(10);
                        trafficAgent.lastName = reader.IsDBNull(11) ? "" : reader.GetString(11);
                        projectFeatureFromDb.properties.trafficAgent = trafficAgent;

                        projectFeatureFromDb.properties.description = reader.IsDBNull(12) ? "" : reader.GetString(12);
                        projectFeatureFromDb.properties.created = reader.IsDBNull(13) ? DateTime.MinValue : reader.GetDateTime(13);
                        projectFeatureFromDb.properties.lastModified = reader.IsDBNull(14) ? DateTime.MinValue : reader.GetDateTime(14);
                        projectFeatureFromDb.properties.finishFrom = reader.IsDBNull(15) ? DateTime.MinValue : reader.GetDateTime(15);
                        projectFeatureFromDb.properties.finishTo = reader.IsDBNull(16) ? DateTime.MinValue : reader.GetDateTime(16);
                        projectFeatureFromDb.properties.costs = reader.IsDBNull(17) ? 0m : reader.GetDecimal(17);

                        CostTypes ct = new CostTypes();
                        ct.code = reader.IsDBNull(18) ? "" : reader.GetString(18);
                        ct.name = reader.IsDBNull(19) ? "" : reader.GetString(19);
                        projectFeatureFromDb.properties.costsType = ct;

                        Polygon ntsPoly = reader.IsDBNull(20) ? Polygon.Empty : reader.GetValue(20) as Polygon;
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
            Polygon roadWorkActivityPoly = roadWorkActivityFeature.geometry.getNtsPolygon();
            Coordinate[] coordinates = roadWorkActivityPoly.Coordinates;

            if (coordinates.Length < 3)
            {
                _logger.LogWarning("Roadwork activity polygon has less than 3 coordinates.");
                roadWorkActivityFeature.errorMessage = "KOPAL-7";
                return Ok(roadWorkActivityFeature);
            }

            // only if project area is greater than 10qm:
            if (roadWorkActivityPoly.Area <= 10.0)
            {
                _logger.LogWarning("Roadworkneed area is less than or equal 10qm.");
                roadWorkActivityFeature.errorMessage = "KOPAL-8";
                return Ok(roadWorkActivityFeature);
            }

            User userFromDb = LoginController.getAuthorizedUserFromDb(this.User);
            roadWorkActivityFeature.properties.projectManager = userFromDb;

            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                try
                {
                    pgConn.Open();

                    NpgsqlCommand selectMgmtAreaComm = pgConn.CreateCommand();
                    selectMgmtAreaComm.CommandText = @"SELECT m.uuid,
                                        u.first_name, u.last_name
                                    FROM ""managementareas"" m
                                    LEFT JOIN ""users"" u ON m.manager = u.uuid
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
                                    costs, costs_type, geom)
                                    VALUES (@uuid, @name, @managementarea, @projectmanager, @traffic_agent,
                                    @description, current_timestamp, @last_modified, @finish_from,
                                    @finish_to, @costs, @costs_type, @geom)";
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
                    insertComm.Parameters.AddWithValue("finish_from", roadWorkActivityFeature.properties.finishFrom);
                    insertComm.Parameters.AddWithValue("finish_to", roadWorkActivityFeature.properties.finishTo);
                    insertComm.Parameters.AddWithValue("costs", roadWorkActivityFeature.properties.costs);
                    insertComm.Parameters.AddWithValue("costs_type", "fullcost"); // TODO make this dynamic 
                    insertComm.Parameters.AddWithValue("geom", roadWorkActivityPoly);

                    await insertComm.ExecuteNonQueryAsync();

                    if (roadWorkActivityFeature.properties.roadWorkNeedsUuids != null &&
                            roadWorkActivityFeature.properties.roadWorkNeedsUuids.Length != 0)
                    {


                        List<Guid> roadWorkNeedsUuidsList = new List<Guid>();
                        foreach (string roadWorkNeedUuid in roadWorkActivityFeature.properties.roadWorkNeedsUuids)
                        {
                            roadWorkNeedsUuidsList.Add(new Guid(roadWorkNeedUuid));
                        }
                        NpgsqlCommand updateComm = pgConn.CreateCommand();
                        updateComm.CommandText = @"UPDATE ""roadworkneeds"" SET
                                    roadworkactivity = @roadworkactivity,
                                    status = 'coordinated'
                                    WHERE uuid = ANY (@uuids)";
                        updateComm.Parameters.AddWithValue("roadworkactivity", new Guid(roadWorkActivityFeature.properties.uuid));
                        updateComm.Parameters.AddWithValue("uuids", roadWorkNeedsUuidsList);
                        await updateComm.ExecuteNonQueryAsync();

                    }
                    await trans.CommitAsync();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    roadWorkActivityFeature.errorMessage = "KOPAL-3";
                    return Ok(roadWorkActivityFeature);
                }
                finally
                {
                    pgConn.Close();
                }
            }

            return Ok(roadWorkActivityFeature);
        }

        // PUT roadworkactivity/
        [HttpPut]
        [Authorize(Roles = "territorymanager,administrator")]
        public ActionResult<RoadWorkActivityFeature> UpdateActivity([FromBody] RoadWorkActivityFeature roadWorkActivityFeature)
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

            // error if activity area is less equal 10qm:
            if (roadWorkActivityPoly.Area <= 10.0)
            {
                _logger.LogWarning("Roadworkactivity area is less than or equal 10qm.");
                roadWorkActivityFeature = new RoadWorkActivityFeature();
                roadWorkActivityFeature.errorMessage = "KOPAL-8";
                return Ok(roadWorkActivityFeature);
            }

            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                try
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

                    NpgsqlCommand updateComm = pgConn.CreateCommand();
                    updateComm.CommandText = @"UPDATE ""roadworkactivities""
                                    SET name=@name, managementarea=@managementarea, projectmanager=@projectmanager,
                                    traffic_agent=@traffic_agent, description=@description,
                                    created=current_timestamp, last_modified=current_timestamp,
                                    finish_from=current_timestamp, finish_to=current_timestamp,
                                    costs=@costs, costs_type=@costs_type, geom=@geom
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
                    updateComm.Parameters.AddWithValue("created", roadWorkActivityFeature.properties.created);
                    updateComm.Parameters.AddWithValue("last_modified", roadWorkActivityFeature.properties.lastModified);
                    updateComm.Parameters.AddWithValue("finish_from", roadWorkActivityFeature.properties.finishFrom);
                    updateComm.Parameters.AddWithValue("finish_to", roadWorkActivityFeature.properties.finishTo);
                    updateComm.Parameters.AddWithValue("costs", roadWorkActivityFeature.properties.costs);
                    updateComm.Parameters.AddWithValue("costs_type", roadWorkActivityFeature.properties.costsType.code);
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
                                        SET roadworkactivity=NULL
                                        WHERE roadworkactivity = @activity_uuid";
                        cleanActivityOfNeedsComm.Parameters.AddWithValue("activity_uuid", new Guid(roadWorkActivityFeature.properties.uuid));
                        cleanActivityOfNeedsComm.ExecuteNonQuery();


                        NpgsqlCommand updateActivityOfNeedsComm = pgConn.CreateCommand();
                        updateActivityOfNeedsComm.CommandText = @"UPDATE roadworkneeds
                                        SET roadworkactivity=@activity_uuid
                                        WHERE uuid = ANY (:needs_uuids)";
                        updateActivityOfNeedsComm.Parameters.AddWithValue("activity_uuid", new Guid(roadWorkActivityFeature.properties.uuid));
                        updateActivityOfNeedsComm.Parameters.AddWithValue("needs_uuids", intersectingNeedsUuids);
                        updateActivityOfNeedsComm.ExecuteNonQuery();

                        roadWorkActivityFeature.properties.roadWorkNeedsUuids = new string[intersectingNeedsUuids.Count];
                        int i = 0;
                        foreach(Guid intersectingNeedUuid in intersectingNeedsUuids)                        
                        {
                            roadWorkActivityFeature.properties.roadWorkNeedsUuids[i] = intersectingNeedUuid.ToString();
                            i++;
                        }

                    }

                    trans.Commit();
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex.Message);
                    roadWorkActivityFeature = new RoadWorkActivityFeature();
                    roadWorkActivityFeature.errorMessage = "KOPAL-3";
                    return Ok(roadWorkActivityFeature);
                }
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
                                SET uuid=NULL
                                WHERE uuid=@uuid";
                        updateComm.Parameters.AddWithValue("uuid", new Guid(uuid));
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
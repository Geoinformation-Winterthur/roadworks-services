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
        public IEnumerable<RoadWorkActivityFeature> GetActivities(string? uuid = "", bool summary = false)
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

                using (NpgsqlDataReader reader = selectComm.ExecuteReader())
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
                pgConn.Close();
            }

            return projectsFromDb.ToArray();
        }

        // POST roadworkactivity/
        [HttpPost]
        [Authorize(Roles = "territorymanager,administrator")]
        public ActionResult<RoadWorkActivityFeature> AddActivity([FromBody] RoadWorkActivityFeature roadWorkActivityFeature)
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

                    using (NpgsqlDataReader reader = selectMgmtAreaComm.ExecuteReader())
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

                    Guid resultUuid = Guid.NewGuid();
                    roadWorkActivityFeature.properties.uuid = resultUuid.ToString();

                    NpgsqlCommand insertComm = pgConn.CreateCommand();
                    insertComm.CommandText = @"INSERT INTO ""roadworkactivities""
                                    (uuid, managementarea, projectmanager, traffic_agent, description,
                                    created, last_modified, finish_from, finish_to,
                                    costs, costs_type, geom)
                                    VALUES (@uuid, @managementarea, @projectmanager, @traffic_agent,
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
                    insertComm.Parameters.AddWithValue("description", roadWorkActivityFeature.properties.description);
                    insertComm.Parameters.AddWithValue("last_modified", roadWorkActivityFeature.properties.lastModified);
                    insertComm.Parameters.AddWithValue("finish_from", roadWorkActivityFeature.properties.finishFrom);
                    insertComm.Parameters.AddWithValue("finish_to", roadWorkActivityFeature.properties.finishTo);
                    insertComm.Parameters.AddWithValue("costs", roadWorkActivityFeature.properties.costs);
                    insertComm.Parameters.AddWithValue("costs_type", "fullcost"); // TODO make this dynamic 
                    insertComm.Parameters.AddWithValue("geom", roadWorkActivityPoly);

                    insertComm.ExecuteNonQuery();

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

    }
}
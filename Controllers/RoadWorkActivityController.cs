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
        [Authorize]
        public IEnumerable<RoadWorkActivityFeature> GetProjects(string? uuid = "", bool summary = false)
        {
            List<RoadWorkActivityFeature> projectsFromDb = new List<RoadWorkActivityFeature>();
            // get data of current user from database:
            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand selectComm = pgConn.CreateCommand();
                selectComm.CommandText = @"SELECT r.uuid, r.managementarea, m.manager, am.first_name, am.last_name,
                            r.projectmanager, pm.first_name, pm.last_name, r.traffic_agent,
                            ta.first_name, ta.last_name, comment, created, last_modified, r.finish_from, r.finish_to,
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

                        ManagementAreaFeature managementAreaFeature = new ManagementAreaFeature();
                        managementAreaFeature.properties.uuid = reader.IsDBNull(1) ? "" : reader.GetGuid(1).ToString();
                        User areaManager = new User();
                        areaManager.uuid = reader.IsDBNull(2) ? "" : reader.GetGuid(2).ToString();
                        areaManager.firstName = reader.IsDBNull(3) ? "" : reader.GetString(3);
                        areaManager.lastName = reader.IsDBNull(4) ? "" : reader.GetString(4);
                        managementAreaFeature.properties.manager = areaManager;
                        projectFeatureFromDb.properties.managementarea = managementAreaFeature;

                        User projectManager = new User();
                        projectManager.uuid = reader.IsDBNull(5) ? "" : reader.GetGuid(5).ToString();
                        projectManager.firstName = reader.IsDBNull(6) ? "" : reader.GetString(6);
                        projectManager.lastName = reader.IsDBNull(7) ? "" : reader.GetString(7);
                        projectFeatureFromDb.properties.projectManager = projectManager;

                        User trafficAgent = new User();
                        trafficAgent.uuid = reader.IsDBNull(8) ? "" : reader.GetGuid(8).ToString();
                        trafficAgent.firstName = reader.IsDBNull(9) ? "" : reader.GetString(9);
                        trafficAgent.lastName = reader.IsDBNull(10) ? "" : reader.GetString(10);
                        projectFeatureFromDb.properties.trafficAgent = trafficAgent;

                        projectFeatureFromDb.properties.comment = reader.IsDBNull(11) ? "" : reader.GetString(11);
                        projectFeatureFromDb.properties.created = reader.IsDBNull(12) ? DateTime.MinValue : reader.GetDateTime(12);
                        projectFeatureFromDb.properties.lastModified = reader.IsDBNull(13) ? DateTime.MinValue : reader.GetDateTime(13);
                        projectFeatureFromDb.properties.finishFrom = reader.IsDBNull(14) ? DateTime.MinValue : reader.GetDateTime(14);
                        projectFeatureFromDb.properties.finishTo = reader.IsDBNull(15) ? DateTime.MinValue : reader.GetDateTime(15);
                        projectFeatureFromDb.properties.costs = reader.IsDBNull(16) ? 0m : reader.GetDecimal(16);

                        CostTypes ct = new CostTypes();
                        ct.code = reader.IsDBNull(17) ? "" : reader.GetString(17);
                        ct.name = reader.IsDBNull(18) ? "" : reader.GetString(18);
                        projectFeatureFromDb.properties.costsType = ct;

                        Polygon ntsPoly = reader.IsDBNull(19) ? Polygon.Empty : reader.GetValue(19) as Polygon;
                        projectFeatureFromDb.geometry = new RoadworkPolygon(ntsPoly);

                        projectsFromDb.Add(projectFeatureFromDb);
                    }
                }
                pgConn.Close();
            }

            return projectsFromDb.ToArray();
        }

    }
}
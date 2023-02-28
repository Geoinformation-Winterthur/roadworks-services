using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Geometries;
using NetTopologySuite.IO;
using Npgsql;
using roadwork_portal_service.Configuration;
using roadwork_portal_service.Model;

namespace roadwork_portal_service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class RoadWorkProjectController : ControllerBase
    {
        // ONLY FOR TEST PURPOSES:
        public static List<RoadWorkProjectFeature> roadworkProjects = new List<RoadWorkProjectFeature>();
        private readonly ILogger<RoadWorkProjectController> _logger;
        private IConfiguration Configuration { get; }

        public RoadWorkProjectController(ILogger<RoadWorkProjectController> logger,
                        IConfiguration configuration)
        {
            _logger = logger;
            this.Configuration = configuration;
        }

        // GET roadworkproject/
        [HttpGet]
        [Authorize]
        public IEnumerable<RoadWorkProjectFeature> GetProjects(int uuid = -1, bool summary = false)
        {
            List<RoadWorkProjectFeature> projectsFromDb = new List<RoadWorkProjectFeature>();
            // get data of current user from database:
            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand selectComm = pgConn.CreateCommand();
                selectComm.CommandText = @"SELECT uuid, place, area, project, project_no,
                        status, priority, realization_until, active, traffic_obstruction_type,
                        ST_AsText(geom) FROM ""roadworkprojects""";

                if (uuid > 0)
                {
                        selectComm.CommandText += " WHERE uuid=@uuid";
                        selectComm.Parameters.AddWithValue("uuid", uuid);
                }

                using (NpgsqlDataReader reader = selectComm.ExecuteReader())
                {
                    RoadWorkProjectFeature projectFeatureFromDb;
                    while (reader.Read())
                    {
                        projectFeatureFromDb = new RoadWorkProjectFeature();
                        projectFeatureFromDb.properties.uuid = reader.IsDBNull(0) ? "" : reader.GetInt64(0).ToString(); // TODO read 128 bit, not only 64 bit
                        projectFeatureFromDb.properties.place = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        projectFeatureFromDb.properties.area = reader.IsDBNull(2) ? "" : reader.GetString(2);
                        projectFeatureFromDb.properties.project = reader.IsDBNull(3) ? "" : reader.GetString(3);
                        projectFeatureFromDb.properties.projectNo = reader.IsDBNull(4) ? -1 : reader.GetInt32(4);
                        projectFeatureFromDb.properties.status = reader.IsDBNull(5) ? "" : reader.GetString(5);
                        projectFeatureFromDb.properties.priority = reader.IsDBNull(6) ? "" : reader.GetString(6);
                        projectFeatureFromDb.properties.realizationUntil = reader.IsDBNull(7) ? DateTime.Now : reader.GetDateTime(7);
                        projectFeatureFromDb.properties.active = reader.IsDBNull(8) ? false : reader.GetBoolean(8);
                        projectFeatureFromDb.properties.trafficObstructionType = reader.IsDBNull(9) ? "" : reader.GetString(9);
                        string geomWkt = reader.IsDBNull(10) ? "" : reader.GetString(10);

                        Polygon polyFromDb = new WKTReader().Read(geomWkt) as Polygon;

                        List<double> polyCoordsList = new List<double>();
                        foreach (Coordinate polyCoord in polyFromDb.ExteriorRing.Coordinates)
                        {
                            polyCoordsList.Add(polyCoord.X);
                            polyCoordsList.Add(polyCoord.Y);
                        }

                        roadwork_portal_service.Model.Geometry geometry
                                = new roadwork_portal_service.Model.Geometry(
                                    roadwork_portal_service.Model.Geometry.GeometryType.Polygon,
                                    polyCoordsList.ToArray<double>());

                        projectFeatureFromDb.geometry = geometry;


                        projectsFromDb.Add(projectFeatureFromDb);
                    }
                }
                pgConn.Close();
            }

            return projectsFromDb.ToArray<RoadWorkProjectFeature>();
        }

        // POST roadworkproject/?projectid=...
        [HttpPost]
        [Authorize]
        public ActionResult PostProject([FromBody] RoadWorkProjectFeature roadWorkProjectFeature)
        {
            RoadWorkProjectController.roadworkProjects.Add(roadWorkProjectFeature);
            return Ok();
        }

        // PUT roadworkproject/?projectid=...
        [HttpPut]
        [Authorize]
        public ActionResult PutProject([FromBody] double[] coordinates, int projectId = -1)
        {
            if (coordinates.Length > 2)
            {
                string polyWkt = "POLYGON((";
                int count = 0;
                foreach (double coord in coordinates)
                {
                    polyWkt += coord;
                    count++;
                    if (count % 2 == 0)
                    {
                        polyWkt += ",";
                    }
                    else
                    {
                        polyWkt += " ";
                    }
                }
                polyWkt = polyWkt.Substring(0, polyWkt.Length - 1);
                polyWkt += "))";
                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    pgConn.Open();
                    NpgsqlCommand updateComm = pgConn.CreateCommand();
                    updateComm.CommandText = @"UPDATE ""roadworkprojects""
                        SET geom=ST_PolygonFromText(@geom, 2056)
                        WHERE uuid=@projectId";
                    updateComm.Parameters.AddWithValue("geom", polyWkt);
                    updateComm.Parameters.AddWithValue("projectId", projectId);

                    updateComm.ExecuteNonQuery();
                    pgConn.Close();
                }
            }

            return Ok();
        }

    }
}
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite;
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
        public IEnumerable<RoadWorkProjectFeature> GetProjects(string? uuid = "", bool summary = false)
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


                if (uuid != null)
                {
                    uuid = uuid.Trim();
                    if (uuid != "")
                    {
                        selectComm.CommandText += " WHERE uuid=@uuid";
                        selectComm.Parameters.AddWithValue("uuid", uuid);
                    }
                }

                using (NpgsqlDataReader reader = selectComm.ExecuteReader())
                {
                    RoadWorkProjectFeature projectFeatureFromDb;
                    while (reader.Read())
                    {
                        projectFeatureFromDb = new RoadWorkProjectFeature();
                        projectFeatureFromDb.properties.uuid = reader.IsDBNull(0) ? "" : reader.GetString(0);
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
        public ActionResult<RoadWorkProjectFeature> PostProject([FromBody] RoadWorkProjectFeature roadWorkProjectFeature)
        {
            string resultUuid = "";
            double[] coordinates = roadWorkProjectFeature.geometry.coordinates;
            if (coordinates.Length > 2)
            {
                Polygon polygon = coordinatesToPolygon(coordinates);
                string polyWkt = new WKTWriter().Write(polygon);

                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    try
                    {
                        pgConn.Open();

                        // only if project area is greater than 10qm:
                        if (polygon.Area > 10.0)
                        {
                            resultUuid = Guid.NewGuid().ToString();
                            NpgsqlCommand insertComm = pgConn.CreateCommand();
                            insertComm.CommandText = @"INSERT INTO ""roadworkprojects""
                                    (uuid, place, area, project, project_no, status, priority,
                                    realization_until, active, traffic_obstruction_type, geom)
                                    VALUES (@uuid, @place, @area, @project, @project_no, @status, @priority,
                                    @realization_until, @active, @traffic_obstruction_type, ST_PolygonFromText(@geom, 2056))";
                            insertComm.Parameters.AddWithValue("uuid", resultUuid);
                            insertComm.Parameters.AddWithValue("place", roadWorkProjectFeature.properties.place);
                            insertComm.Parameters.AddWithValue("area", roadWorkProjectFeature.properties.area);
                            insertComm.Parameters.AddWithValue("project", roadWorkProjectFeature.properties.project);
                            insertComm.Parameters.AddWithValue("project_no", roadWorkProjectFeature.properties.projectNo);
                            insertComm.Parameters.AddWithValue("status", roadWorkProjectFeature.properties.status);
                            insertComm.Parameters.AddWithValue("priority", roadWorkProjectFeature.properties.priority);
                            insertComm.Parameters.AddWithValue("realization_until", roadWorkProjectFeature.properties.realizationUntil);
                            insertComm.Parameters.AddWithValue("active", roadWorkProjectFeature.properties.active);
                            insertComm.Parameters.AddWithValue("traffic_obstruction_type", roadWorkProjectFeature.properties.trafficObstructionType);
                            insertComm.Parameters.AddWithValue("geom", polyWkt);

                            insertComm.ExecuteNonQuery();
                        }
                        else
                        {
                            return Ok(new WebAppException("Roadwork project area is less than or equal 10qm."));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                        return Ok(new WebAppException(ex.Message));
                    }
                    finally
                    {
                        pgConn.Close();
                    }
                }
            }
            RoadWorkProjectFeature result = new RoadWorkProjectFeature();
            result.properties.uuid = resultUuid;
            return Ok(result);
        }

        // PUT roadworkproject/?uuid=...
        [HttpPut]
        [Authorize]
        public ActionResult<WebAppException> PutProject([FromBody] double[] coordinates, string uuid = "")
        {
            if (coordinates.Length > 2)
            {
                Polygon polygon = coordinatesToPolygon(coordinates);
                string polyWkt = new WKTWriter().Write(polygon);

                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    try
                    {
                        pgConn.Open();

                        // only if project area is greater than 10qm:
                        if (polygon.Area > 10.0)
                        {
                            NpgsqlCommand updateComm = pgConn.CreateCommand();
                            updateComm.CommandText = @"UPDATE ""roadworkprojects""
                                    SET geom=ST_PolygonFromText(@geom, 2056)
                                    WHERE uuid=@uuid";
                            updateComm.Parameters.AddWithValue("geom", polyWkt);
                            updateComm.Parameters.AddWithValue("uuid", uuid);

                            updateComm.ExecuteNonQuery();
                        }
                        else
                        {
                            return Ok(new WebAppException("Roadwork project area is less than or equal 10qm."));
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                        return Ok(new WebAppException(ex.Message));
                    }
                    finally
                    {
                        pgConn.Close();
                    }
                }
            }
            return Ok();
        }

        private static Polygon coordinatesToPolygon(double[] coordinates)
        {
            List<Coordinate> ntsCoordinatesList = new List<Coordinate>();
            for(int i = 0; i < coordinates.Length; i = i + 2)
            {
                Coordinate ntsCoord = new Coordinate(coordinates[i], coordinates[i+1]);
                ntsCoordinatesList.Add(ntsCoord);
            }
            GeometryFactory geomFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid:2056);            
            return geomFactory.CreatePolygon(ntsCoordinatesList.ToArray());
        }

    }
}
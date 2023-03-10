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
    public class ManagementAreaController : ControllerBase
    {
        private readonly ILogger<ManagementAreaController> _logger;
        private IConfiguration Configuration { get; }

        public ManagementAreaController(ILogger<ManagementAreaController> logger,
                        IConfiguration configuration)
        {
            _logger = logger;
            this.Configuration = configuration;
        }

        // GET managementarea/
        [HttpGet]
        [Authorize(Roles = "administrator")]
        public IEnumerable<ManagementAreaFeature> GetManagementAreas()
        {
            List<ManagementAreaFeature> managementAreasFromDb = new List<ManagementAreaFeature>();
            // get data of current user from database:
            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand selectComm = pgConn.CreateCommand();
                selectComm.CommandText = @"SELECT uuid, managername, geom
                            FROM ""managementareas""";

                using (NpgsqlDataReader reader = selectComm.ExecuteReader())
                {
                    ManagementAreaFeature managementAreaFeatureFromDb;
                    while (reader.Read())
                    {
                        managementAreaFeatureFromDb = new ManagementAreaFeature();
                        managementAreaFeatureFromDb.uuid = reader.IsDBNull(0) ? "" : reader.GetString(0);
                        managementAreaFeatureFromDb.managername = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        Polygon polyFromDb = reader.IsDBNull(2) ? Polygon.Empty : reader.GetValue(2) as Polygon;

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

                        managementAreaFeatureFromDb.geometry = geometry;

                        managementAreasFromDb.Add(managementAreaFeatureFromDb);
                    }
                }
                pgConn.Close();
            }

            return managementAreasFromDb.ToArray();
        }

    }
}
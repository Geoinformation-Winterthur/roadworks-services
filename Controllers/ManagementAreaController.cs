using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite;
using NetTopologySuite.Geometries;
using NetTopologySuite.Features;
using Npgsql;
using roadwork_portal_service.Configuration;
using System.Numerics;
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
        [Authorize(Roles = "territorymanager,administrator")]
        public ActionResult<FeatureCollection> GetManagementAreas()
        {
            FeatureCollection managementAreas = new FeatureCollection();
            // get data of current user from database:
            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand selectComm = pgConn.CreateCommand();
                selectComm.CommandText = @"SELECT m.uuid, m.name, am.uuid, am.first_name || ' ' || am.last_name,
                            sam.uuid, sam.first_name || ' ' || sam.last_name, m.color_fill, m.color_stroke, m.geom
                            FROM ""managementareas"" m
                            LEFT JOIN ""users"" am ON m.manager = am.uuid
                            LEFT JOIN ""users"" sam ON m.substitute_manager = sam.uuid";

                using (NpgsqlDataReader reader = selectComm.ExecuteReader())
                {
                    GeometryFactory geomFactory = NtsGeometryServices.Instance.CreateGeometryFactory(srid: 2056);
                    Feature managementAreaFeatureFromDb;
                    while (reader.Read())
                    {
                        managementAreaFeatureFromDb = new Feature();
                        managementAreaFeatureFromDb.Attributes = new AttributesTable();
                        managementAreaFeatureFromDb.Attributes.Add("uuid", reader.IsDBNull(0) ? "" : reader.GetGuid(0).ToString());
                        managementAreaFeatureFromDb.Attributes.Add("name", reader.IsDBNull(1) ? "" : reader.GetString(1));
                        managementAreaFeatureFromDb.Attributes.Add("manager_uuid", reader.IsDBNull(2) ? "" : reader.GetGuid(2).ToString());
                        managementAreaFeatureFromDb.Attributes.Add("managername", reader.IsDBNull(3) ? "" : reader.GetString(3));
                        managementAreaFeatureFromDb.Attributes.Add("substitutemanager_uuid", reader.IsDBNull(4) ? "" : reader.GetGuid(4).ToString());
                        managementAreaFeatureFromDb.Attributes.Add("substitutemanager_name", reader.IsDBNull(5) ? "" : reader.GetString(5));
                        managementAreaFeatureFromDb.Attributes.Add("color_fill", reader.IsDBNull(6) ? "" : reader.GetString(6));
                        managementAreaFeatureFromDb.Attributes.Add("color_stroke", reader.IsDBNull(7) ? "" : reader.GetString(7));
                        managementAreaFeatureFromDb.Geometry = reader.IsDBNull(8) ? Polygon.Empty : reader.GetValue(8) as Polygon;

                        managementAreas.Add(managementAreaFeatureFromDb);
                    }
                }
                pgConn.Close();
            }

            return Ok(managementAreas);
        }

        // PUT managementarea/
        [HttpPut]
        [Authorize(Roles = "administrator")]
        public ActionResult<ManagementAreaFeature>
                    UpdateManagementArea([FromBody] ManagementAreaFeature managementAreaFeature)
        {

            try
            {
                if (managementAreaFeature == null || managementAreaFeature.properties == null ||
                        managementAreaFeature.properties.uuid == null)
                {
                    _logger.LogWarning("No management area feature provided in area feature update process.");
                    managementAreaFeature = new ManagementAreaFeature();
                    managementAreaFeature.errorMessage = "KOPAL-15";
                    return Ok(managementAreaFeature);
                }

                string managementAreaUuid = managementAreaFeature.properties.uuid.Trim().ToLower();
                if (managementAreaUuid == "")
                {
                    _logger.LogWarning("No management area feature provided in area feature update process.");
                    managementAreaFeature = new ManagementAreaFeature();
                    managementAreaFeature.errorMessage = "KOPAL-15";
                    return Ok(managementAreaFeature);
                }

                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {

                    pgConn.Open();

                    string managerUuid = "";

                    if (managementAreaFeature.properties.manager != null
                        && managementAreaFeature.properties.manager.uuid != null)
                    {
                        managerUuid = managementAreaFeature.properties.manager.uuid.Trim().ToLower();
                    }

                    if (managerUuid != "")
                    {

                        NpgsqlCommand selectAreaManagerRole = pgConn.CreateCommand();
                        selectAreaManagerRole.CommandText = @"SELECT role,
                                        first_name, last_name
                                    FROM ""users""
                                    WHERE uuid=@uuid";
                        selectAreaManagerRole.Parameters.AddWithValue("uuid", new Guid(managerUuid));

                        using (NpgsqlDataReader reader = selectAreaManagerRole.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                managementAreaFeature.properties.manager.role.code
                                            = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                managementAreaFeature.properties.manager.firstName
                                            = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                managementAreaFeature.properties.manager.lastName
                                            = reader.IsDBNull(2) ? "" : reader.GetString(2);
                            }
                        }

                        if (managementAreaFeature.properties.manager.role.code != "territorymanager")
                        {
                            _logger.LogWarning("Administrator tried to set the user with UUID "
                                + managerUuid + " as a manager of an area, though the user " +
                                "has not the role of a territory manager. Operation forbidden " +
                                "and canceled.");
                            managementAreaFeature = new ManagementAreaFeature();
                            managementAreaFeature.errorMessage = "KOPAL-17";
                            return Ok(managementAreaFeature);
                        }

                        NpgsqlCommand updateComm = pgConn.CreateCommand();
                        updateComm.CommandText = @"UPDATE ""managementareas""
                                    SET manager=@manager_uuid
                                    WHERE uuid=@uuid";

                        updateComm.Parameters.AddWithValue("manager_uuid", new Guid(managerUuid));
                        updateComm.Parameters.AddWithValue("uuid", new Guid(managementAreaFeature.properties.uuid));

                        updateComm.ExecuteNonQuery();
                    }

                    string substituteManagerUuid = "";

                    if (managementAreaFeature.properties.substituteManager != null
                        && managementAreaFeature.properties.substituteManager.uuid != null)
                    {
                        substituteManagerUuid = managementAreaFeature.properties.substituteManager.uuid.Trim().ToLower();
                    }

                    if (substituteManagerUuid != "")
                    {

                        NpgsqlCommand selectAreaManagerRole = pgConn.CreateCommand();
                        selectAreaManagerRole.CommandText = @"SELECT role,
                                        first_name, last_name
                                    FROM ""users""
                                    WHERE uuid=@uuid";
                        selectAreaManagerRole.Parameters.AddWithValue("uuid", new Guid(substituteManagerUuid));

                        using (NpgsqlDataReader reader = selectAreaManagerRole.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                managementAreaFeature.properties.substituteManager.role.code
                                            = reader.IsDBNull(0) ? "" : reader.GetString(0);
                                managementAreaFeature.properties.substituteManager.firstName
                                            = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                managementAreaFeature.properties.substituteManager.lastName
                                            = reader.IsDBNull(2) ? "" : reader.GetString(2);
                            }
                        }

                        if (managementAreaFeature.properties.substituteManager.role.code != "territorymanager")
                        {
                            _logger.LogWarning("Administrator tried to set the user with UUID "
                                + managerUuid + " as a manager of an area, though the user " +
                                "has not the role of a territory manager. Operation forbidden " +
                                "and canceled.");
                            managementAreaFeature = new ManagementAreaFeature();
                            managementAreaFeature.errorMessage = "KOPAL-17";
                            return Ok(managementAreaFeature);
                        }

                        NpgsqlCommand updateComm = pgConn.CreateCommand();
                        updateComm.CommandText = @"UPDATE ""managementareas""
                                    SET substitute_manager=@substitute_manager_uuid
                                    WHERE uuid=@uuid";

                        updateComm.Parameters.AddWithValue("substitute_manager_uuid", new Guid(substituteManagerUuid));
                        updateComm.Parameters.AddWithValue("uuid", new Guid(managementAreaFeature.properties.uuid));

                        updateComm.ExecuteNonQuery();
                    }

                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                managementAreaFeature = new ManagementAreaFeature();
                managementAreaFeature.errorMessage = "KOPAL-3";
                return Ok(managementAreaFeature);
            }
            return managementAreaFeature;
        }

    }
}
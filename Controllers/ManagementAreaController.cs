using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Geometries;
using NetTopologySuite.Features;
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
        [Authorize]
        public ActionResult<FeatureCollection> GetManagementAreas()
        {
            FeatureCollection managementAreas = new FeatureCollection();

            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand selectComm = pgConn.CreateCommand();
                selectComm.CommandText = @"SELECT m.uuid, m.name, am.uuid, am.first_name || ' ' || am.last_name,
                            sam.uuid, sam.first_name || ' ' || sam.last_name, m.color_fill, m.color_stroke, m.geom
                            FROM ""wtb_ssp_managementareas"" m
                            LEFT JOIN ""wtb_ssp_users"" am ON m.manager = am.uuid
                            LEFT JOIN ""wtb_ssp_users"" sam ON m.substitute_manager = sam.uuid";

                using (NpgsqlDataReader reader = selectComm.ExecuteReader())
                {
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

        // POST managementarea/
        [HttpPost]
        [Authorize]
        public ActionResult<ManagementArea> GetIntersectingManagementAreas([FromBody] RoadworkPolygon polygon)
        {
            ManagementArea result = new ManagementArea();

            try
            {

                Polygon roadWorkPoly = polygon.getNtsPolygon();
                Coordinate[] coordinates = roadWorkPoly.Coordinates;

                if (coordinates.Length < 3)
                {
                    _logger.LogWarning("Roadwork polygon has less than 3 coordinates.");
                    result.errorMessage = "SSP-3";
                    return Ok(result);
                }

                ConfigurationData configData = AppConfigController.getConfigurationFromDb();
                // only if project area is greater than min area size:
                if (roadWorkPoly.Area <= configData.minAreaSize)
                {
                    _logger.LogWarning("Roadwork area is less than or equal " + configData.minAreaSize + "qm.");
                    result.errorMessage = "SSP-3";
                    return Ok(result);
                }

                // only if project area is smaller than max area size:
                if (roadWorkPoly.Area > configData.maxAreaSize)
                {
                    _logger.LogWarning("Roadwork area is greater than " + configData.maxAreaSize + "qm.");
                    result.errorMessage = "SSP-3";
                    return Ok(result);
                }

                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    pgConn.Open();
                    NpgsqlCommand selectComm = pgConn.CreateCommand();
                    selectComm.CommandText = @"SELECT m.uuid, am.uuid, am.first_name, am.last_name,
                            sam.uuid, sam.first_name, sam.last_name
                            FROM ""wtb_ssp_managementareas"" m
                            LEFT JOIN ""wtb_ssp_users"" am ON m.manager = am.uuid
                            LEFT JOIN ""wtb_ssp_users"" sam ON m.substitute_manager = sam.uuid
                            WHERE ST_Area(ST_Intersection(@geom, m.geom)) > 0
                                    ORDER BY ST_Area(ST_Intersection(@geom, m.geom)) DESC
                                    LIMIT 1";

                    selectComm.Parameters.AddWithValue("geom", roadWorkPoly);


                    using (NpgsqlDataReader reader = selectComm.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            result.uuid = reader.IsDBNull(0) ? "" : reader.GetGuid(0).ToString();

                            User manager = new User();
                            manager.uuid = reader.IsDBNull(1) ? "" : reader.GetGuid(1).ToString();
                            manager.firstName = reader.IsDBNull(2) ? "" : reader.GetString(2);
                            manager.lastName = reader.IsDBNull(3) ? "" : reader.GetString(3);
                            result.manager = manager;

                            User substituteManager = new User();
                            substituteManager.uuid = reader.IsDBNull(4) ? "" : reader.GetGuid(4).ToString();
                            substituteManager.firstName = reader.IsDBNull(5) ? "" : reader.GetString(5);
                            substituteManager.lastName = reader.IsDBNull(6) ? "" : reader.GetString(6);
                            result.substituteManager = substituteManager;
                        }
                    }
                    pgConn.Close();
                }

            }
            catch (Exception ex)
            {
                this._logger.LogError(ex.Message);
                result.errorMessage = "SSP-3";
            }

            return Ok(result);

        }

        // PUT managementarea/
        [HttpPut]
        [Authorize(Roles = "administrator")]
        public ActionResult<ManagementArea>
                    UpdateManagementArea([FromBody] ManagementArea managementArea)
        {

            try
            {
                if (managementArea == null || managementArea.uuid == null)
                {
                    _logger.LogWarning("No management area feature provided in area feature update process.");
                    managementArea = new ManagementArea();
                    managementArea.errorMessage = "SSP-15";
                    return Ok(managementArea);
                }

                string managementAreaUuid = managementArea.uuid.Trim().ToLower();
                if (managementAreaUuid == "")
                {
                    _logger.LogWarning("No management area feature provided in area feature update process.");
                    managementArea = new ManagementArea();
                    managementArea.errorMessage = "SSP-15";
                    return Ok(managementArea);
                }

                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {

                    pgConn.Open();

                    string managerUuid = "";

                    if (managementArea.manager != null
                        && managementArea.manager.uuid != null)
                    {
                        managerUuid = managementArea.manager.uuid.Trim().ToLower();
                    }

                    if (managerUuid != "")
                    {

                        NpgsqlCommand selectAreaManagerRole = pgConn.CreateCommand();
                        selectAreaManagerRole.CommandText = @"SELECT role,
                                        first_name, last_name
                                    FROM ""wtb_ssp_users""
                                    WHERE uuid=@uuid";
                        selectAreaManagerRole.Parameters.AddWithValue("uuid", new Guid(managerUuid));

                        using (NpgsqlDataReader reader = selectAreaManagerRole.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                managementArea.manager.setRole(reader.IsDBNull(0) ? "" : reader.GetString(0));
                                managementArea.manager.firstName
                                            = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                managementArea.manager.lastName
                                            = reader.IsDBNull(2) ? "" : reader.GetString(2);
                            }
                        }

                        if (managementArea.manager.hasRole("territorymanager"))
                        {
                            _logger.LogWarning("Administrator tried to set the user with UUID "
                                + managerUuid + " as a manager of an area, though the user " +
                                "has not the role of a territory manager. Operation forbidden " +
                                "and canceled.");
                            managementArea = new ManagementArea();
                            managementArea.errorMessage = "SSP-17";
                            return Ok(managementArea);
                        }

                        NpgsqlCommand updateComm = pgConn.CreateCommand();
                        updateComm.CommandText = @"UPDATE ""wtb_ssp_managementareas""
                                    SET manager=@manager_uuid
                                    WHERE uuid=@uuid";

                        updateComm.Parameters.AddWithValue("manager_uuid", new Guid(managerUuid));
                        updateComm.Parameters.AddWithValue("uuid", new Guid(managementArea.uuid));

                        updateComm.ExecuteNonQuery();
                    }

                    string substituteManagerUuid = "";

                    if (managementArea.substituteManager != null
                        && managementArea.substituteManager.uuid != null)
                    {
                        substituteManagerUuid = managementArea.substituteManager.uuid.Trim().ToLower();
                    }

                    if (substituteManagerUuid != "")
                    {

                        NpgsqlCommand selectAreaManagerRole = pgConn.CreateCommand();
                        selectAreaManagerRole.CommandText = @"SELECT role,
                                        first_name, last_name
                                    FROM ""wtb_ssp_users""
                                    WHERE uuid=@uuid";
                        selectAreaManagerRole.Parameters.AddWithValue("uuid", new Guid(substituteManagerUuid));

                        using (NpgsqlDataReader reader = selectAreaManagerRole.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                managementArea.substituteManager.setRole(reader.IsDBNull(0) ? "" : reader.GetString(0));
                                managementArea.substituteManager.firstName
                                            = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                managementArea.substituteManager.lastName
                                            = reader.IsDBNull(2) ? "" : reader.GetString(2);
                            }
                        }

                        if (managementArea.substituteManager.hasRole("territorymanager"))
                        {
                            _logger.LogWarning("Administrator tried to set the user with UUID "
                                + managerUuid + " as a manager of an area, though the user " +
                                "has not the role of a territory manager. Operation forbidden " +
                                "and canceled.");
                            managementArea = new ManagementArea();
                            managementArea.errorMessage = "SSP-17";
                            return Ok(managementArea);
                        }

                        NpgsqlCommand updateComm = pgConn.CreateCommand();
                        updateComm.CommandText = @"UPDATE ""wtb_ssp_managementareas""
                                    SET substitute_manager=@substitute_manager_uuid
                                    WHERE uuid=@uuid";

                        updateComm.Parameters.AddWithValue("substitute_manager_uuid", new Guid(substituteManagerUuid));
                        updateComm.Parameters.AddWithValue("uuid", new Guid(managementArea.uuid));

                        updateComm.ExecuteNonQuery();
                    }

                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                managementArea = new ManagementArea();
                managementArea.errorMessage = "SSP-3";
                return Ok(managementArea);
            }
            return managementArea;
        }

    }
}
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
    public class RoadWorkNeedController : ControllerBase
    {
        private readonly ILogger<RoadWorkNeedController> _logger;
        private IConfiguration Configuration { get; }

        public RoadWorkNeedController(ILogger<RoadWorkNeedController> logger,
                        IConfiguration configuration)
        {
            _logger = logger;
            this.Configuration = configuration;
        }

        // GET roadworkneed/
        [HttpGet]
        [Authorize(Roles = "orderer,territorymanager,administrator")]
        public IEnumerable<RoadWorkNeedFeature> GetNeeds(string? uuids = "", string? roadWorkActivityUuid = "",
                bool summary = false)
        {
            List<RoadWorkNeedFeature> projectsFromDb = new List<RoadWorkNeedFeature>();

            // get data of current user from database:
            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand selectComm = pgConn.CreateCommand();
                selectComm.CommandText = @"SELECT r.uuid, r.name, rwt.code, rwt.name, r.orderer,
                            u.first_name, u.last_name, o.name, r.finish_early_from, r.finish_early_to,
                            r.finish_optimum_from, r.finish_optimum_to, r.finish_late_from,
                            r.finish_late_to, p.code, s.code, s.name,
                            r.description, r.managementarea, m.manager, am.first_name, am.last_name,
                            m.substitute_manager, sam.first_name, sam.last_name,
                            r.created, r.last_modified, r.roadworkactivity, u.e_mail,
                            r.longer_six_months, r.relevance, r.geom
                        FROM ""roadworkneeds"" r
                        LEFT JOIN ""users"" u ON r.orderer = u.uuid
                        LEFT JOIN ""organisationalunits"" o ON u.org_unit = o.uuid
                        LEFT JOIN ""priorities"" p ON r.priority = p.code
                        LEFT JOIN ""status"" s ON r.status = s.code
                        LEFT JOIN ""managementareas"" m ON r.managementarea = m.uuid                        
                        LEFT JOIN ""users"" am ON m.manager = am.uuid
                        LEFT JOIN ""users"" sam ON m.substitute_manager = sam.uuid
                        LEFT JOIN ""roadworkneedtypes"" rwt ON r.kind = rwt.code";

                if (uuids != null)
                {
                    uuids = uuids.Trim().ToLower();
                    if (uuids != "")
                    {
                        List<Guid> uuidsList = new List<Guid>();

                        foreach (string uuid in uuids.Split(","))
                        {
                            if (uuid != null && uuid != "")
                            {
                                uuidsList.Add(new Guid(uuid));
                            }
                        }
                        selectComm.CommandText += " WHERE r.uuid = ANY (:uuids)";
                        selectComm.Parameters.AddWithValue("uuids", uuidsList);
                    }
                }

                using (NpgsqlDataReader reader = selectComm.ExecuteReader())
                {
                    RoadWorkNeedFeature needFeatureFromDb;
                    while (reader.Read())
                    {
                        needFeatureFromDb = new RoadWorkNeedFeature();
                        needFeatureFromDb.properties.uuid = reader.IsDBNull(0) ? "" : reader.GetGuid(0).ToString();
                        needFeatureFromDb.properties.name = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        needFeatureFromDb.properties.kind = new RoadWorkNeedEnum();
                        needFeatureFromDb.properties.kind.code = reader.IsDBNull(2) ? "" : reader.GetString(2);
                        needFeatureFromDb.properties.kind.name = reader.IsDBNull(3) ? "" : reader.GetString(3);
                        User orderer = new User();
                        orderer.uuid = reader.IsDBNull(4) ? "" : reader.GetGuid(4).ToString();
                        orderer.firstName = reader.IsDBNull(5) ? "" : reader.GetString(5);
                        orderer.lastName = reader.IsDBNull(6) ? "" : reader.GetString(6);
                        OrganisationalUnit orgUnit = new OrganisationalUnit();
                        orgUnit.name = reader.IsDBNull(7) ? "" : reader.GetString(7);
                        orderer.organisationalUnit = orgUnit;
                        needFeatureFromDb.properties.orderer = orderer;
                        needFeatureFromDb.properties.finishEarlyFrom = reader.IsDBNull(8) ? DateTime.MinValue : reader.GetDateTime(8);
                        needFeatureFromDb.properties.finishEarlyTo = reader.IsDBNull(9) ? DateTime.MinValue : reader.GetDateTime(9);
                        needFeatureFromDb.properties.finishOptimumFrom = reader.IsDBNull(10) ? DateTime.MinValue : reader.GetDateTime(10);
                        needFeatureFromDb.properties.finishOptimumTo = reader.IsDBNull(11) ? DateTime.MinValue : reader.GetDateTime(11);
                        needFeatureFromDb.properties.finishLateFrom = reader.IsDBNull(12) ? DateTime.MinValue : reader.GetDateTime(12);
                        needFeatureFromDb.properties.finishLateTo = reader.IsDBNull(13) ? DateTime.MinValue : reader.GetDateTime(13);
                        Priority priority = new Priority();
                        priority.code = reader.IsDBNull(14) ? "" : reader.GetString(14);
                        needFeatureFromDb.properties.priority = priority;
                        Status status = new Status();
                        status.code = reader.IsDBNull(15) ? "" : reader.GetString(15);
                        status.name = reader.IsDBNull(16) ? "" : reader.GetString(16);
                        needFeatureFromDb.properties.status = status;
                        needFeatureFromDb.properties.description = reader.IsDBNull(17) ? "" : reader.GetString(17);
                        ManagementAreaFeature managementAreaFeature = new ManagementAreaFeature();
                        managementAreaFeature.properties.uuid = reader.IsDBNull(18) ? "" : reader.GetGuid(18).ToString();

                        User manager = new User();
                        manager.uuid = reader.IsDBNull(19) ? "" : reader.GetGuid(19).ToString();
                        manager.firstName = reader.IsDBNull(20) ? "" : reader.GetString(20);
                        manager.lastName = reader.IsDBNull(21) ? "" : reader.GetString(21);
                        managementAreaFeature.properties.manager = manager;

                        User substituteManager = new User();
                        substituteManager.uuid = reader.IsDBNull(22) ? "" : reader.GetGuid(22).ToString();
                        substituteManager.firstName = reader.IsDBNull(23) ? "" : reader.GetString(23);
                        substituteManager.lastName = reader.IsDBNull(24) ? "" : reader.GetString(24);
                        managementAreaFeature.properties.substituteManager = substituteManager;

                        needFeatureFromDb.properties.managementarea = managementAreaFeature;

                        needFeatureFromDb.properties.created = reader.IsDBNull(25) ? DateTime.MinValue : reader.GetDateTime(25);
                        needFeatureFromDb.properties.lastModified = reader.IsDBNull(26) ? DateTime.MinValue : reader.GetDateTime(26);
                        needFeatureFromDb.properties.roadWorkActivityUuid = reader.IsDBNull(27) ? "" : reader.GetGuid(27).ToString();

                        string ordererMailAddress = reader.IsDBNull(28) ? "" : reader.GetString(28);
                        string mailOfLoggedInUser = User.FindFirstValue(ClaimTypes.Email);
                        if (User.IsInRole("administrator") || ordererMailAddress == mailOfLoggedInUser)
                        {
                            needFeatureFromDb.properties.isEditingAllowed = true;
                        }

                        needFeatureFromDb.properties.longer6Month = reader.IsDBNull(29) ? false : reader.GetBoolean(29);

                        needFeatureFromDb.properties.relevance = reader.IsDBNull(30) ? 0 : reader.GetInt32(30);
                        Polygon ntsPoly = reader.IsDBNull(31) ? Polygon.Empty : reader.GetValue(31) as Polygon;
                        needFeatureFromDb.geometry = new RoadworkPolygon(ntsPoly);

                        projectsFromDb.Add(needFeatureFromDb);
                    }
                }
                pgConn.Close();
            }

            return projectsFromDb.ToArray();
        }

        // POST roadworkneed/
        [HttpPost]
        [Authorize(Roles = "orderer,administrator")]
        public ActionResult<RoadWorkNeedFeature> AddNeed([FromBody] RoadWorkNeedFeature roadWorkNeedFeature)
        {
            try
            {
                Polygon roadWorkNeedPoly = roadWorkNeedFeature.geometry.getNtsPolygon();
                Coordinate[] coordinates = roadWorkNeedPoly.Coordinates;

                if (coordinates.Length < 3)
                {
                    _logger.LogWarning("Roadworkneed Polygon has less than 3 coordinates.");
                    roadWorkNeedFeature.errorMessage = "KOPAL-7";
                    return Ok(roadWorkNeedFeature);
                }

                ConfigurationData configData = AppConfigController.getConfigurationFromDb();
                // only if project area is greater than min area size:
                if (roadWorkNeedPoly.Area <= configData.minAreaSize)
                {
                    _logger.LogWarning("Roadworkneed area is less than or equal " + configData.minAreaSize + "qm.");
                    roadWorkNeedFeature = new RoadWorkNeedFeature();
                    roadWorkNeedFeature.errorMessage = "KOPAL-8";
                    return Ok(roadWorkNeedFeature);
                }

                // only if project area is smaller than max area size:
                if (roadWorkNeedPoly.Area > configData.maxAreaSize)
                {
                    _logger.LogWarning("Roadworkneed area is greater than " + configData.maxAreaSize + "qm.");
                    roadWorkNeedFeature = new RoadWorkNeedFeature();
                    roadWorkNeedFeature.errorMessage = "KOPAL-16";
                    return Ok(roadWorkNeedFeature);
                }

                User userFromDb = LoginController.getAuthorizedUserFromDb(this.User);
                roadWorkNeedFeature.properties.orderer = userFromDb;

                if (roadWorkNeedFeature.properties.name != null)
                {
                    roadWorkNeedFeature.properties.name = roadWorkNeedFeature.properties.name.Trim();
                }

                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {

                    pgConn.Open();

                    if (roadWorkNeedFeature.properties.name == null || roadWorkNeedFeature.properties.name == "")
                    {
                        roadWorkNeedFeature.properties.name = "";
                        int bufferSize = 0;
                        List<(string, Point)> fromToNamesList;
                        (string, Point)[] greatestDistanceTuple;
                        Polygon bufferedPoly = roadWorkNeedPoly;

                        do
                        {
                            if (bufferSize > 0)
                            {
                                bufferedPoly = bufferedPoly.Buffer(bufferSize) as Polygon;
                            }

                            fromToNamesList = _getFromToListFromDb(bufferedPoly, pgConn);

                            greatestDistanceTuple = _calcGreatestDistanceTuple(fromToNamesList);

                            if (greatestDistanceTuple[0].Item1 != null && greatestDistanceTuple[1].Item1 != null)
                            {
                                roadWorkNeedFeature.properties.name =
                                        greatestDistanceTuple[0].Item1 + " bis " + greatestDistanceTuple[1].Item1;
                            }
                            bufferSize += 10;
                        } while (bufferSize < 100 && roadWorkNeedFeature.properties.name == "");

                    }

                    NpgsqlCommand selectMgmtAreaComm = pgConn.CreateCommand();
                    selectMgmtAreaComm.CommandText = @"SELECT m.uuid,
                                        am.first_name, am.last_name,
                                        sam.first_name, sam.last_name
                                    FROM ""managementareas"" m
                                    LEFT JOIN ""users"" am ON m.manager = am.uuid
                                    LEFT JOIN ""users"" sam ON m.substitute_manager = sam.uuid
                                    WHERE ST_Area(ST_Intersection(@geom, geom)) > 0
                                    ORDER BY ST_Area(ST_Intersection(@geom, geom)) DESC
                                    LIMIT 1";

                    selectMgmtAreaComm.Parameters.AddWithValue("geom", roadWorkNeedPoly);

                    roadWorkNeedFeature.properties.managementarea = new ManagementAreaFeature();

                    using (NpgsqlDataReader reader = selectMgmtAreaComm.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            roadWorkNeedFeature.properties.managementarea.properties.uuid = reader.IsDBNull(0) ? "" : reader.GetGuid(0).ToString();
                            roadWorkNeedFeature.properties.managementarea.properties.manager.firstName = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            roadWorkNeedFeature.properties.managementarea.properties.manager.lastName = reader.IsDBNull(2) ? "" : reader.GetString(2);
                            roadWorkNeedFeature.properties.managementarea.properties.substituteManager.firstName = reader.IsDBNull(3) ? "" : reader.GetString(3);
                            roadWorkNeedFeature.properties.managementarea.properties.substituteManager.lastName = reader.IsDBNull(4) ? "" : reader.GetString(4);
                        }
                    }

                    if (roadWorkNeedFeature.properties.managementarea.properties.uuid == "")
                    {
                        _logger.LogWarning("New roadworkneed does not lie in any management area.");
                        roadWorkNeedFeature.errorMessage = "KOPAL-9";
                        return Ok(roadWorkNeedFeature);
                    }

                    Guid resultUuid = Guid.NewGuid();
                    roadWorkNeedFeature.properties.uuid = resultUuid.ToString();

                    NpgsqlCommand insertComm = pgConn.CreateCommand();
                    insertComm.CommandText = @"INSERT INTO ""roadworkneeds""
                                    (uuid, name, kind, orderer, created, last_modified, finish_early_from, finish_early_to,
                                    finish_optimum_from, finish_optimum_to, finish_late_from,
                                    finish_late_to, priority, status, description, managementarea, longer_six_months, relevance,
                                    geom)
                                    VALUES (@uuid, @name, @kind, @orderer, current_timestamp, current_timestamp,
                                    @finish_early_from, @finish_early_to, @finish_optimum_from, @finish_optimum_to, @finish_late_from,
                                    @finish_late_to, @priority, @status, @description, @managementarea, @longer_six_months, @relevance,
                                    @geom)";
                    insertComm.Parameters.AddWithValue("uuid", new Guid(roadWorkNeedFeature.properties.uuid));
                    insertComm.Parameters.AddWithValue("name", roadWorkNeedFeature.properties.name);
                    insertComm.Parameters.AddWithValue("kind", roadWorkNeedFeature.properties.kind.code);
                    if (roadWorkNeedFeature.properties.orderer.uuid != "")
                    {
                        insertComm.Parameters.AddWithValue("orderer", new Guid(roadWorkNeedFeature.properties.orderer.uuid));
                    }
                    else
                    {
                        insertComm.Parameters.AddWithValue("orderer", DBNull.Value);
                    }
                    insertComm.Parameters.AddWithValue("finish_early_from", roadWorkNeedFeature.properties.finishEarlyFrom);
                    insertComm.Parameters.AddWithValue("finish_early_to", roadWorkNeedFeature.properties.finishEarlyTo);
                    insertComm.Parameters.AddWithValue("finish_optimum_from", roadWorkNeedFeature.properties.finishOptimumFrom);
                    insertComm.Parameters.AddWithValue("finish_optimum_to", roadWorkNeedFeature.properties.finishOptimumTo);
                    insertComm.Parameters.AddWithValue("finish_late_from", roadWorkNeedFeature.properties.finishLateFrom);
                    insertComm.Parameters.AddWithValue("finish_late_to", roadWorkNeedFeature.properties.finishLateTo);
                    insertComm.Parameters.AddWithValue("priority", roadWorkNeedFeature.properties.priority.code);
                    insertComm.Parameters.AddWithValue("status", roadWorkNeedFeature.properties.status.code);
                    insertComm.Parameters.AddWithValue("description", roadWorkNeedFeature.properties.description);
                    if (roadWorkNeedFeature.properties.managementarea.properties.uuid != "")
                    {
                        insertComm.Parameters.AddWithValue("managementarea", new Guid(roadWorkNeedFeature.properties.managementarea.properties.uuid));
                    }
                    else
                    {
                        insertComm.Parameters.AddWithValue("managementarea", DBNull.Value);
                    }
                    insertComm.Parameters.AddWithValue("longer_six_months", roadWorkNeedFeature.properties.longer6Month);
                    insertComm.Parameters.AddWithValue("relevance", roadWorkNeedFeature.properties.relevance);
                    insertComm.Parameters.AddWithValue("geom", roadWorkNeedPoly);

                    insertComm.ExecuteNonQuery();


                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                roadWorkNeedFeature.errorMessage = "KOPAL-3";
                return Ok(roadWorkNeedFeature);
            }

            return Ok(roadWorkNeedFeature);
        }

        // PUT roadworkneed/
        [HttpPut]
        [Authorize(Roles = "orderer,administrator")]
        public ActionResult<RoadWorkNeedFeature> UpdateNeed([FromBody] RoadWorkNeedFeature roadWorkNeedFeature)
        {

            try
            {
                if (roadWorkNeedFeature == null || roadWorkNeedFeature.geometry == null ||
                        roadWorkNeedFeature.geometry.coordinates == null ||
                            roadWorkNeedFeature.geometry.coordinates.Length < 3)
                {
                    _logger.LogWarning("Roadworkneed has a geometry error.");
                    roadWorkNeedFeature = new RoadWorkNeedFeature();
                    roadWorkNeedFeature.errorMessage = "KOPAL-3";
                    return Ok(roadWorkNeedFeature);
                }

                Polygon roadWorkNeedPoly = roadWorkNeedFeature.geometry.getNtsPolygon();

                ConfigurationData configData = AppConfigController.getConfigurationFromDb();
                // only if project area is greater than min area size:
                if (roadWorkNeedPoly.Area <= configData.minAreaSize)
                {
                    _logger.LogWarning("Roadworkneed area is less than or equal " + configData.minAreaSize + "qm.");
                    roadWorkNeedFeature = new RoadWorkNeedFeature();
                    roadWorkNeedFeature.errorMessage = "KOPAL-8";
                    return Ok(roadWorkNeedFeature);
                }

                // only if project area is smaller than max area size:
                if (roadWorkNeedPoly.Area > configData.maxAreaSize)
                {
                    _logger.LogWarning("Roadworkneed area is greater than " + configData.maxAreaSize + "qm.");
                    roadWorkNeedFeature = new RoadWorkNeedFeature();
                    roadWorkNeedFeature.errorMessage = "KOPAL-16";
                    return Ok(roadWorkNeedFeature);
                }

                if (!roadWorkNeedPoly.IsSimple)
                {
                    _logger.LogWarning("Geometry of roadworkneed " + roadWorkNeedFeature.properties.uuid +
                            " does not fulfill the criteria of geometrical simplicity.");
                    roadWorkNeedFeature.errorMessage = "KOPAL-10";
                    return Ok(roadWorkNeedFeature);
                }

                if (!roadWorkNeedPoly.IsValid)
                {
                    _logger.LogWarning("Geometry of roadworkneed " + roadWorkNeedFeature.properties.uuid +
                            " does not fulfill the criteria of geometrical validity.");
                    roadWorkNeedFeature.errorMessage = "KOPAL-11";
                    return Ok(roadWorkNeedFeature);
                }


                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {

                    roadWorkNeedFeature.properties.managementarea = new ManagementAreaFeature();

                    pgConn.Open();

                    if (!User.IsInRole("administrator"))
                    {

                        NpgsqlCommand selectOrdererOfNeedComm = pgConn.CreateCommand();
                        selectOrdererOfNeedComm.CommandText = @"SELECT u.e_mail
                                    FROM ""roadworkneeds"" r
                                    LEFT JOIN ""users"" u ON r.orderer = u.uuid
                                    WHERE r.uuid=@uuid";
                        selectOrdererOfNeedComm.Parameters.AddWithValue("uuid", new Guid(roadWorkNeedFeature.properties.uuid));

                        string eMailOfOrderer = "";

                        using (NpgsqlDataReader reader = selectOrdererOfNeedComm.ExecuteReader())
                        {
                            if (reader.Read())
                            {
                                eMailOfOrderer = reader.IsDBNull(0) ? "" : reader.GetString(0);
                            }
                        }

                        string mailOfLoggedInUser = User.FindFirstValue(ClaimTypes.Email);

                        if (mailOfLoggedInUser != eMailOfOrderer)
                        {
                            _logger.LogWarning("User " + mailOfLoggedInUser + " has no right to edit " +
                                "roadwork need " + roadWorkNeedFeature.properties.uuid + " but tried " +
                                "to edit it.");
                            roadWorkNeedFeature.errorMessage = "KOPAL-14";
                            return Ok(roadWorkNeedFeature);
                        }

                    }


                    NpgsqlCommand selectMgmtAreaComm = pgConn.CreateCommand();
                    selectMgmtAreaComm.CommandText = @"SELECT m.uuid,
                                        am.first_name, am.last_name,
                                        sam.first_name, sam.last_name
                                    FROM ""managementareas"" m
                                    LEFT JOIN ""users"" am ON m.manager = am.uuid
                                    LEFT JOIN ""users"" sam ON m.substitute_manager = sam.uuid
                                    WHERE ST_Intersects(@geom, geom)
                                    ORDER BY ST_Area(ST_Intersection(@geom, geom)) DESC
                                    LIMIT 1";
                    selectMgmtAreaComm.Parameters.AddWithValue("geom", roadWorkNeedPoly);

                    using (NpgsqlDataReader reader = selectMgmtAreaComm.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            roadWorkNeedFeature.properties.managementarea.properties.uuid = reader.IsDBNull(0) ? "" : reader.GetGuid(0).ToString();
                            roadWorkNeedFeature.properties.managementarea.properties.manager.firstName = reader.IsDBNull(1) ? "" : reader.GetString(1);
                            roadWorkNeedFeature.properties.managementarea.properties.manager.lastName = reader.IsDBNull(2) ? "" : reader.GetString(2);
                            roadWorkNeedFeature.properties.managementarea.properties.substituteManager.firstName = reader.IsDBNull(3) ? "" : reader.GetString(3);
                            roadWorkNeedFeature.properties.managementarea.properties.substituteManager.lastName = reader.IsDBNull(4) ? "" : reader.GetString(4);
                        }
                    }

                    if (roadWorkNeedFeature.properties.managementarea.properties.uuid == "")
                    {
                        _logger.LogWarning("New roadworkneed does not lie in any management area.");
                        roadWorkNeedFeature.errorMessage = "KOPAL-9";
                        return Ok(roadWorkNeedFeature);
                    }

                    NpgsqlCommand updateComm = pgConn.CreateCommand();
                    updateComm.CommandText = @"UPDATE ""roadworkneeds""
                                    SET name=@name, kind=@kind, orderer=@orderer, last_modified=current_timestamp,
                                    finish_early_from=@finish_early_from, finish_early_to=@finish_early_to,
                                    finish_optimum_from=@finish_optimum_from, finish_optimum_to=@finish_optimum_to,
                                    finish_late_from=@finish_late_from, finish_late_to=@finish_late_to,
                                    priority=@priority, status=@status, description=@description,
                                    managementarea=@managementarea, longer_six_months=@longer_six_months,
                                    relevance=@relevance, geom=@geom
                                    WHERE uuid=@uuid";

                    updateComm.Parameters.AddWithValue("name", roadWorkNeedFeature.properties.name);
                    updateComm.Parameters.AddWithValue("kind", roadWorkNeedFeature.properties.kind.code);
                    if (roadWorkNeedFeature.properties.orderer.uuid != "")
                    {
                        updateComm.Parameters.AddWithValue("orderer", new Guid(roadWorkNeedFeature.properties.orderer.uuid));
                    }
                    else
                    {
                        updateComm.Parameters.AddWithValue("orderer", DBNull.Value);
                    }
                    updateComm.Parameters.AddWithValue("finish_early_from", roadWorkNeedFeature.properties.finishEarlyFrom);
                    updateComm.Parameters.AddWithValue("finish_early_to", roadWorkNeedFeature.properties.finishEarlyTo);
                    updateComm.Parameters.AddWithValue("finish_optimum_from", roadWorkNeedFeature.properties.finishOptimumFrom);
                    updateComm.Parameters.AddWithValue("finish_optimum_to", roadWorkNeedFeature.properties.finishOptimumTo);
                    updateComm.Parameters.AddWithValue("finish_late_from", roadWorkNeedFeature.properties.finishLateFrom);
                    updateComm.Parameters.AddWithValue("finish_late_to", roadWorkNeedFeature.properties.finishLateTo);
                    updateComm.Parameters.AddWithValue("priority", roadWorkNeedFeature.properties.priority.code);
                    updateComm.Parameters.AddWithValue("status", roadWorkNeedFeature.properties.status.code);
                    updateComm.Parameters.AddWithValue("description", roadWorkNeedFeature.properties.description);
                    if (roadWorkNeedFeature.properties.managementarea.properties.uuid != "")
                    {
                        updateComm.Parameters.AddWithValue("managementarea", new Guid(roadWorkNeedFeature.properties.managementarea.properties.uuid));
                    }
                    else
                    {
                        updateComm.Parameters.AddWithValue("managementarea", DBNull.Value);
                    }
                    updateComm.Parameters.AddWithValue("longer_six_months", roadWorkNeedFeature.properties.longer6Month);
                    updateComm.Parameters.AddWithValue("relevance", roadWorkNeedFeature.properties.relevance);
                    updateComm.Parameters.AddWithValue("geom", roadWorkNeedPoly);
                    updateComm.Parameters.AddWithValue("uuid", new Guid(roadWorkNeedFeature.properties.uuid));

                    updateComm.ExecuteNonQuery();

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                roadWorkNeedFeature = new RoadWorkNeedFeature();
                roadWorkNeedFeature.errorMessage = "KOPAL-3";
                return Ok(roadWorkNeedFeature);
            }

            return Ok(roadWorkNeedFeature);
        }

        // DELETE /roadworkneed?uuid=...&releaseonly=true
        [HttpDelete]
        [Authorize(Roles = "administrator")]
        public ActionResult<ErrorMessage> DeleteNeed(string uuid, bool releaseOnly = false)
        {
            ErrorMessage errorResult = new ErrorMessage();

            if (uuid == null)
            {
                _logger.LogWarning("No uuid provided by user in delete roadwork need process. " +
                            "Thus process is canceled, no roadwork need is deleted.");
                errorResult.errorMessage = "KOPAL-15";
                return Ok(errorResult);
            }

            uuid = uuid.ToLower().Trim();

            if (uuid == "")
            {
                _logger.LogWarning("No uuid provided by user in delete roadwork need process. " +
                            "Thus process is canceled, no roadwork need is deleted.");
                errorResult.errorMessage = "KOPAL-15";
                return Ok(errorResult);
            }

            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand deleteComm = pgConn.CreateCommand();
                if (releaseOnly)
                {
                    // if activityUuid is given, then only remove the given need from the
                    // given activity, but do not delete activity as a whole:
                    deleteComm.CommandText = @"UPDATE ""roadworkneeds""
                                SET roadworkactivity=NULL
                                WHERE uuid=@uuid";
                }
                else
                {
                    deleteComm.CommandText = @"DELETE FROM ""roadworkneeds""
                                WHERE uuid=@uuid";
                }
                deleteComm.Parameters.AddWithValue("uuid", new Guid(uuid));

                int noAffectedRows = deleteComm.ExecuteNonQuery();

                pgConn.Close();

                if (noAffectedRows == 1)
                {
                    return Ok();
                }
            }

            _logger.LogError("Fatal error.");
            errorResult.errorMessage = "KOPAL-3";
            return Ok(errorResult);
        }

        private static List<(string, Point)> _getFromToListFromDb(
                        Polygon roadWorkNeedPoly, NpgsqlConnection pgConn)
        {
            List<(string, Point)> fromToNamesList = new List<(string, Point)>();
            NpgsqlCommand selectFromToNames = pgConn.CreateCommand();
            selectFromToNames.CommandText = @"SELECT address, geom
                                    FROM ""addresses""
                                    WHERE ST_Intersects(@geom, geom)";
            selectFromToNames.Parameters.AddWithValue("geom", roadWorkNeedPoly);

            using (NpgsqlDataReader reader = selectFromToNames.ExecuteReader())
            {
                while (reader.Read())
                {
                    string address = reader.IsDBNull(0) ? "" : reader.GetString(0);
                    Point p = reader.IsDBNull(1) ? Point.Empty : reader.GetValue(1) as Point;
                    fromToNamesList.Add((address, p));
                }
            }
            return fromToNamesList;
        }

        private static (string, Point)[] _calcGreatestDistanceTuple(List<(string, Point)> fromToNamesList)
        {

            double distance = 0d;
            double greatestDistance = 0d;
            (string, Point)[] greatestDistanceTuple = new (string, Point)[2];
            foreach ((string, Point) fromToNamesTuple1 in fromToNamesList)
            {
                foreach ((string, Point) fromToNamesTuple2 in fromToNamesList)
                {
                    distance = fromToNamesTuple1.Item2.Distance(fromToNamesTuple2.Item2);
                    if (distance >= greatestDistance)
                    {
                        greatestDistance = distance;
                        greatestDistanceTuple[0] = fromToNamesTuple1;
                        greatestDistanceTuple[1] = fromToNamesTuple2;
                    }
                }
            }
            return greatestDistanceTuple;

        }

    }
}
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Geometries;
using Npgsql;
using roadwork_portal_service.Configuration;
using roadwork_portal_service.Helper;
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

        // GET roadworkneed/?year=2023&uuids=...&roadworkactivityuuid=...
        [HttpGet]
        [Authorize(Roles = "orderer,trefficmanager,territorymanager,administrator")]
        public IEnumerable<RoadWorkNeedFeature> GetNeeds(int? year = 0, string? uuids = "",
                string? roadWorkActivityUuid = "", bool summary = false)
        {
            List<RoadWorkNeedFeature> projectsFromDb = new List<RoadWorkNeedFeature>();

            if(year == null)
                year = 0;

            if (uuids == null)
                uuids = "";
            else
                uuids = uuids.Trim().ToLower();

            if (roadWorkActivityUuid == null)
                roadWorkActivityUuid = "";
            else
                roadWorkActivityUuid = roadWorkActivityUuid.Trim().ToLower();

            // get data of current user from database:
            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand selectComm = pgConn.CreateCommand();
                selectComm.CommandText = @"SELECT r.uuid, r.name, rwt.code, rwt.name, r.orderer,
                            u.first_name, u.last_name, o.name, r.finish_early_from, r.finish_early_to,
                            r.finish_optimum_from, r.finish_optimum_to, r.finish_late_from,
                            r.finish_late_to, p.code, s.code, s.name,
                            r.description, 
                            r.created, r.last_modified, an.uuid_roadwork_activity, u.e_mail,
                            r.longer_six_months, r.relevance, an.activityrelationtype, r.costs,
                            r.geom
                        FROM ""wtb_ssp_roadworkneeds"" r
                        LEFT JOIN ""wtb_ssp_activities_to_needs"" an ON an.uuid_roadwork_need = r.uuid
                        LEFT JOIN ""wtb_ssp_users"" u ON r.orderer = u.uuid
                        LEFT JOIN ""wtb_ssp_organisationalunits"" o ON u.org_unit = o.uuid
                        LEFT JOIN ""wtb_ssp_priorities"" p ON r.priority = p.code
                        LEFT JOIN ""wtb_ssp_status"" s ON r.status = s.code
                        LEFT JOIN ""wtb_ssp_roadworkneedtypes"" rwt ON r.kind = rwt.code";

                if (year != 0 && uuids == "" && roadWorkActivityUuid == "")
                {
                    selectComm.CommandText += " WHERE EXTRACT(YEAR FROM finish_optimum_from) = @year";
                    selectComm.Parameters.AddWithValue("year", year);
                }

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
                else if (roadWorkActivityUuid != "")
                {
                    selectComm.CommandText += @" LEFT JOIN ""wtb_ssp_roadworkactivities"" act ON act.uuid = @act_uuid
                                                        WHERE ST_Intersects(act.geom, r.geom)";
                    selectComm.Parameters.AddWithValue("act_uuid", new Guid(roadWorkActivityUuid));
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

                        needFeatureFromDb.properties.created = reader.IsDBNull(18) ? DateTime.MinValue : reader.GetDateTime(18);
                        needFeatureFromDb.properties.lastModified = reader.IsDBNull(19) ? DateTime.MinValue : reader.GetDateTime(19);
                        needFeatureFromDb.properties.roadWorkActivityUuid = reader.IsDBNull(20) ? "" : reader.GetGuid(20).ToString();

                        string ordererMailAddress = reader.IsDBNull(21) ? "" : reader.GetString(21);
                        string mailOfLoggedInUser = User.FindFirstValue(ClaimTypes.Email);
                        if (User.IsInRole("administrator") || ordererMailAddress == mailOfLoggedInUser)
                        {
                            needFeatureFromDb.properties.isEditingAllowed = true;
                        }

                        if (needFeatureFromDb.properties.status.code == "provfixed" ||
                            needFeatureFromDb.properties.status.code == "deffixed" ||
                            needFeatureFromDb.properties.status.code == "executed")
                        {
                            needFeatureFromDb.properties.isEditingAllowed = false;
                        }

                        needFeatureFromDb.properties.longer6Month = reader.IsDBNull(22) ? false : reader.GetBoolean(22);
                        needFeatureFromDb.properties.relevance = reader.IsDBNull(23) ? 0 : reader.GetInt32(23);
                        needFeatureFromDb.properties.activityRelationType = reader.IsDBNull(24) ? "" : reader.GetString(24);
                        needFeatureFromDb.properties.costs = reader.IsDBNull(25) ? 0 : reader.GetInt32(25);

                        Polygon ntsPoly = reader.IsDBNull(26) ? Polygon.Empty : reader.GetValue(26) as Polygon;
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
                    roadWorkNeedFeature.errorMessage = "SSP-7";
                    return Ok(roadWorkNeedFeature);
                }

                ConfigurationData configData = AppConfigController.getConfigurationFromDb();
                // only if project area is greater than min area size:
                if (roadWorkNeedPoly.Area <= configData.minAreaSize)
                {
                    _logger.LogWarning("Roadworkneed area is less than or equal " + configData.minAreaSize + "qm.");
                    roadWorkNeedFeature = new RoadWorkNeedFeature();
                    roadWorkNeedFeature.errorMessage = "SSP-8";
                    return Ok(roadWorkNeedFeature);
                }

                // only if project area is smaller than max area size:
                if (roadWorkNeedPoly.Area > configData.maxAreaSize)
                {
                    _logger.LogWarning("Roadworkneed area is greater than " + configData.maxAreaSize + "qm.");
                    roadWorkNeedFeature = new RoadWorkNeedFeature();
                    roadWorkNeedFeature.errorMessage = "SSP-16";
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
                        roadWorkNeedFeature.properties.name = HelperFunctions.getAddressNames(roadWorkNeedPoly, pgConn);
                    }

                    Guid resultUuid = Guid.NewGuid();
                    roadWorkNeedFeature.properties.uuid = resultUuid.ToString();

                    NpgsqlCommand insertComm = pgConn.CreateCommand();
                    insertComm.CommandText = @"INSERT INTO ""wtb_ssp_roadworkneeds""
                                    (uuid, name, kind, orderer, created, last_modified, finish_early_from, finish_early_to,
                                    finish_optimum_from, finish_optimum_to, finish_late_from,
                                    finish_late_to, priority, status, description, longer_six_months, relevance,
                                    costs, geom)
                                    VALUES (@uuid, @name, @kind, @orderer, current_timestamp, current_timestamp,
                                    @finish_early_from, @finish_early_to, @finish_optimum_from, @finish_optimum_to, @finish_late_from,
                                    @finish_late_to, @priority, @status, @description, @longer_six_months, @relevance,
                                    @costs, @geom)";
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
                    insertComm.Parameters.AddWithValue("longer_six_months", roadWorkNeedFeature.properties.longer6Month);
                    insertComm.Parameters.AddWithValue("relevance", roadWorkNeedFeature.properties.relevance);
                    insertComm.Parameters.AddWithValue("costs", roadWorkNeedFeature.properties.costs);
                    insertComm.Parameters.AddWithValue("geom", roadWorkNeedPoly);

                    insertComm.ExecuteNonQuery();


                }

            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                roadWorkNeedFeature.errorMessage = "SSP-3";
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
                    roadWorkNeedFeature.errorMessage = "SSP-3";
                    return Ok(roadWorkNeedFeature);
                }

                Polygon roadWorkNeedPoly = roadWorkNeedFeature.geometry.getNtsPolygon();

                ConfigurationData configData = AppConfigController.getConfigurationFromDb();
                // only if project area is greater than min area size:
                if (roadWorkNeedPoly.Area <= configData.minAreaSize)
                {
                    _logger.LogWarning("Roadworkneed area is less than or equal " + configData.minAreaSize + "qm.");
                    roadWorkNeedFeature = new RoadWorkNeedFeature();
                    roadWorkNeedFeature.errorMessage = "SSP-8";
                    return Ok(roadWorkNeedFeature);
                }

                // only if project area is smaller than max area size:
                if (roadWorkNeedPoly.Area > configData.maxAreaSize)
                {
                    _logger.LogWarning("Roadworkneed area is greater than " + configData.maxAreaSize + "qm.");
                    roadWorkNeedFeature = new RoadWorkNeedFeature();
                    roadWorkNeedFeature.errorMessage = "SSP-16";
                    return Ok(roadWorkNeedFeature);
                }

                if (!roadWorkNeedPoly.IsSimple)
                {
                    _logger.LogWarning("Geometry of roadworkneed " + roadWorkNeedFeature.properties.uuid +
                            " does not fulfill the criteria of geometrical simplicity.");
                    roadWorkNeedFeature.errorMessage = "SSP-10";
                    return Ok(roadWorkNeedFeature);
                }

                if (!roadWorkNeedPoly.IsValid)
                {
                    _logger.LogWarning("Geometry of roadworkneed " + roadWorkNeedFeature.properties.uuid +
                            " does not fulfill the criteria of geometrical validity.");
                    roadWorkNeedFeature.errorMessage = "SSP-11";
                    return Ok(roadWorkNeedFeature);
                }


                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {

                    pgConn.Open();

                    if (!User.IsInRole("administrator"))
                    {

                        NpgsqlCommand selectOrdererOfNeedComm = pgConn.CreateCommand();
                        selectOrdererOfNeedComm.CommandText = @"SELECT u.e_mail
                                    FROM ""wtb_ssp_roadworkneeds"" r
                                    LEFT JOIN ""wtb_ssp_users"" u ON r.orderer = u.uuid
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
                            roadWorkNeedFeature.errorMessage = "SSP-14";
                            return Ok(roadWorkNeedFeature);
                        }

                    }

                    using (NpgsqlTransaction updateTransAction = pgConn.BeginTransaction())
                    {

                        NpgsqlCommand updateComm = pgConn.CreateCommand();
                        updateComm.CommandText = @"UPDATE ""wtb_ssp_roadworkneeds""
                                    SET name=@name, kind=@kind, orderer=@orderer, last_modified=current_timestamp,
                                    finish_early_from=@finish_early_from, finish_early_to=@finish_early_to,
                                    finish_optimum_from=@finish_optimum_from, finish_optimum_to=@finish_optimum_to,
                                    finish_late_from=@finish_late_from, finish_late_to=@finish_late_to,
                                    priority=@priority, status=@status, description=@description,
                                    longer_six_months=@longer_six_months, relevance=@relevance, 
                                    costs=@costs, geom=@geom
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
                        updateComm.Parameters.AddWithValue("longer_six_months", roadWorkNeedFeature.properties.longer6Month);
                        updateComm.Parameters.AddWithValue("relevance", roadWorkNeedFeature.properties.relevance);
                        updateComm.Parameters.AddWithValue("costs", roadWorkNeedFeature.properties.costs);
                        updateComm.Parameters.AddWithValue("geom", roadWorkNeedPoly);
                        updateComm.Parameters.AddWithValue("uuid", new Guid(roadWorkNeedFeature.properties.uuid));
                        updateComm.ExecuteNonQuery();

                        if (roadWorkNeedFeature.properties.roadWorkActivityUuid != "")
                        {
                            NpgsqlCommand deleteComm = pgConn.CreateCommand();
                            deleteComm.CommandText = @"DELETE FROM ""wtb_ssp_activities_to_needs""
                                    WHERE uuid_roadwork_need=@uuid_roadwork_need
                                        AND uuid_roadwork_activity=@uuid_roadwork_activity";
                            deleteComm.Parameters.AddWithValue("uuid_roadwork_need", new Guid(roadWorkNeedFeature.properties.uuid));
                            deleteComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadWorkNeedFeature.properties.roadWorkActivityUuid));
                            deleteComm.ExecuteNonQuery();

                            deleteComm.CommandText = @"DELETE FROM ""wtb_ssp_activities_to_needs""
                                    WHERE uuid_roadwork_need=@uuid_roadwork_need
                                        AND activityrelationtype='assignedneed'";
                            deleteComm.ExecuteNonQuery();

                            if (roadWorkNeedFeature.properties.activityRelationType != null &&
                                    roadWorkNeedFeature.properties.activityRelationType.Trim() != "")
                            {
                                NpgsqlCommand insertComm = pgConn.CreateCommand();
                                insertComm.CommandText = @"INSERT INTO ""wtb_ssp_activities_to_needs""
                                        (uuid, uuid_roadwork_need, uuid_roadwork_activity, activityrelationtype)
                                        VALUES(@uuid, @uuid_roadwork_need, @uuid_roadwork_activity, @activityrelationtype)";
                                insertComm.Parameters.AddWithValue("uuid", Guid.NewGuid());
                                insertComm.Parameters.AddWithValue("uuid_roadwork_need", new Guid(roadWorkNeedFeature.properties.uuid));
                                insertComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadWorkNeedFeature.properties.roadWorkActivityUuid));
                                insertComm.Parameters.AddWithValue("activityrelationtype", roadWorkNeedFeature.properties.activityRelationType);
                                insertComm.ExecuteNonQuery();

                            }
                        }

                        updateTransAction.Commit();

                    }

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                roadWorkNeedFeature = new RoadWorkNeedFeature();
                roadWorkNeedFeature.errorMessage = "SSP-3";
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
                errorResult.errorMessage = "SSP-15";
                return Ok(errorResult);
            }

            uuid = uuid.ToLower().Trim();

            if (uuid == "")
            {
                _logger.LogWarning("No uuid provided by user in delete roadwork need process. " +
                            "Thus process is canceled, no roadwork need is deleted.");
                errorResult.errorMessage = "SSP-15";
                return Ok(errorResult);
            }

            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand deleteComm = pgConn.CreateCommand();
                if (releaseOnly)
                {
                    deleteComm.CommandText = @"DELETE FROM ""wtb_ssp_activities_to_needs""
                                WHERE uuid_roadwork_need=@uuid";
                }
                else
                {
                    deleteComm.CommandText = @"DELETE FROM ""wtb_ssp_roadworkneeds""
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
            errorResult.errorMessage = "SSP-3";
            return Ok(errorResult);
        }

    }
}
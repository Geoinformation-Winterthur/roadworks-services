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
        [Authorize]
        public IEnumerable<RoadWorkNeedFeature> GetNeeds(string? uuid = "", bool summary = false)
        {
            List<RoadWorkNeedFeature> projectsFromDb = new List<RoadWorkNeedFeature>();
            // get data of current user from database:
            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand selectComm = pgConn.CreateCommand();
                selectComm.CommandText = @"SELECT r.uuid, r.name, r.kind, r.orderer,
                            u.first_name, u.last_name, o.name, r.finish_early_from, r.finish_early_to,
                            r.finish_optimum_from, r.finish_optimum_to, r.finish_late_from,
                            r.finish_late_to, r.priority, p.code, r.status, s.code, s.name,
                            r.comment, r.managementarea, m.manager, u2.first_name, u2.last_name,
                            r.geom
                        FROM ""roadworkneeds"" r
                        LEFT JOIN ""users"" u ON r.orderer = u.uuid
                        LEFT JOIN ""organisationalunits"" o ON u.org_unit = o.uuid
                        LEFT JOIN ""priorities"" p ON r.priority = p.uuid
                        LEFT JOIN ""status"" s ON r.status = s.uuid
                        LEFT JOIN ""managementareas"" m ON r.managementarea = m.uuid
                        LEFT JOIN ""users"" u2 ON m.manager = u2.uuid";

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
                    RoadWorkNeedFeature needFeatureFromDb;
                    while (reader.Read())
                    {
                        needFeatureFromDb = new RoadWorkNeedFeature();
                        needFeatureFromDb.properties.uuid = reader.IsDBNull(0) ? "" : reader.GetGuid(0).ToString();
                        needFeatureFromDb.properties.name = reader.IsDBNull(1) ? "" : reader.GetString(1);
                        needFeatureFromDb.properties.kind = reader.IsDBNull(2) ? "" : reader.GetString(2);
                        User orderer = new User();
                        orderer.uuid = reader.IsDBNull(3) ? "" : reader.GetGuid(3).ToString();
                        orderer.firstName = reader.IsDBNull(4) ? "" : reader.GetString(4);
                        orderer.lastName = reader.IsDBNull(5) ? "" : reader.GetString(5);
                        OrganisationalUnit orgUnit = new OrganisationalUnit();
                        orgUnit.name = reader.IsDBNull(6) ? "" : reader.GetString(6);
                        orderer.organisationalUnit = orgUnit;
                        needFeatureFromDb.properties.orderer = orderer;
                        needFeatureFromDb.properties.finishEarlyFrom = reader.IsDBNull(7) ? DateTime.MinValue : reader.GetDateTime(7);
                        needFeatureFromDb.properties.finishEarlyTo = reader.IsDBNull(8) ? DateTime.MinValue : reader.GetDateTime(8);
                        needFeatureFromDb.properties.finishOptimumFrom = reader.IsDBNull(9) ? DateTime.MinValue : reader.GetDateTime(9);
                        needFeatureFromDb.properties.finishOptimumTo = reader.IsDBNull(10) ? DateTime.MinValue : reader.GetDateTime(10);
                        needFeatureFromDb.properties.finishLateFrom = reader.IsDBNull(11) ? DateTime.MinValue : reader.GetDateTime(11);
                        needFeatureFromDb.properties.finishLateTo = reader.IsDBNull(12) ? DateTime.MinValue : reader.GetDateTime(12);
                        Priority priority = new Priority();
                        priority.uuid = reader.IsDBNull(13) ? "" : reader.GetGuid(13).ToString();
                        priority.code = reader.IsDBNull(14) ? "" : reader.GetString(14);
                        needFeatureFromDb.properties.priority = priority;
                        Status status = new Status();
                        status.uuid = reader.IsDBNull(15) ? "" : reader.GetGuid(15).ToString();
                        status.code = reader.IsDBNull(16) ? "" : reader.GetString(16);
                        status.name = reader.IsDBNull(17) ? "" : reader.GetString(17);
                        needFeatureFromDb.properties.status = status;
                        needFeatureFromDb.properties.comment = reader.IsDBNull(18) ? "" : reader.GetString(18);
                        ManagementAreaFeature managementAreaFeature = new ManagementAreaFeature();
                        managementAreaFeature.properties.uuid = reader.IsDBNull(19) ? "" : reader.GetGuid(19).ToString();
                        User manager = new User();
                        manager.uuid = reader.IsDBNull(20) ? "" : reader.GetGuid(20).ToString();
                        manager.firstName = reader.IsDBNull(21) ? "" : reader.GetString(21);
                        manager.lastName = reader.IsDBNull(22) ? "" : reader.GetString(22);
                        managementAreaFeature.properties.manager = manager;
                        needFeatureFromDb.properties.managementarea = managementAreaFeature;
                        Polygon ntsPoly = reader.IsDBNull(23) ? Polygon.Empty : reader.GetValue(23) as Polygon;
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
        [Authorize]
        public ActionResult<RoadWorkNeedFeature> AddNeed([FromBody] RoadWorkNeedFeature roadWorkNeedFeature)
        {
            User userFromDb = LoginController.getAuthorizedUserFromDb(this.User);
            roadWorkNeedFeature.properties.orderer = userFromDb;
            Polygon roadWorkNeedPoly = roadWorkNeedFeature.geometry.getNtsPolygon();
            Coordinate[] coordinates = roadWorkNeedPoly.Coordinates;
            if (coordinates.Length > 2)
            {
                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    try
                    {
                        pgConn.Open();

                        // only if project area is greater than 10qm:
                        if (roadWorkNeedPoly.Area > 10.0)
                        {
                            NpgsqlCommand selectMgmtAreaComm = pgConn.CreateCommand();
                            selectMgmtAreaComm.CommandText = @"SELECT m.uuid,
                                        u.first_name, u.last_name
                                    FROM ""managementareas"" m
                                    LEFT JOIN ""users"" u ON m.manager = u.uuid
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
                                }
                            }

                            if (roadWorkNeedFeature.properties.managementarea.properties.uuid == "")
                            {
                                return BadRequest("New roadwork need does not lie in any management area");
                            }

                            Guid resultUuid = Guid.NewGuid();
                            roadWorkNeedFeature.properties.uuid = resultUuid.ToString();

                            NpgsqlCommand insertComm = pgConn.CreateCommand();
                            insertComm.CommandText = @"INSERT INTO ""roadworkneeds""
                                    (uuid, name, kind, orderer, finish_early_from, finish_early_to,
                                    finish_optimum_from, finish_optimum_to, finish_late_from,
                                    finish_late_to, priority, status, comment, managementarea, geom)
                                    VALUES (@uuid, @name, @kind, @orderer, @finish_early_from, @finish_early_to,
                                    @finish_optimum_from, @finish_optimum_to, @finish_late_from,
                                    @finish_late_to, @priority, @status, @comment, @managementarea, @geom)";
                            insertComm.Parameters.AddWithValue("uuid", new Guid(roadWorkNeedFeature.properties.uuid));
                            insertComm.Parameters.AddWithValue("name", roadWorkNeedFeature.properties.name);
                            insertComm.Parameters.AddWithValue("kind", roadWorkNeedFeature.properties.kind);
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
                            if (roadWorkNeedFeature.properties.priority.uuid != "")
                            {
                                insertComm.Parameters.AddWithValue("priority", new Guid(roadWorkNeedFeature.properties.priority.uuid));
                            }
                            else
                            {
                                insertComm.Parameters.AddWithValue("priority", DBNull.Value);
                            }
                            if (roadWorkNeedFeature.properties.status.uuid != "")
                            {
                                insertComm.Parameters.AddWithValue("status", new Guid(roadWorkNeedFeature.properties.status.uuid));
                            }
                            else
                            {
                                insertComm.Parameters.AddWithValue("status", DBNull.Value);
                            }
                            insertComm.Parameters.AddWithValue("comment", roadWorkNeedFeature.properties.comment);
                            if (roadWorkNeedFeature.properties.managementarea.properties.uuid != "")
                            {
                                insertComm.Parameters.AddWithValue("managementarea", new Guid(roadWorkNeedFeature.properties.managementarea.properties.uuid));
                            }
                            else
                            {
                                insertComm.Parameters.AddWithValue("managementarea", DBNull.Value);
                            }
                            insertComm.Parameters.AddWithValue("geom", roadWorkNeedPoly);

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
            return Ok(roadWorkNeedFeature);
        }

        // PUT roadworkneed/
        [HttpPut]
        [Authorize]
        public ActionResult<RoadWorkNeedFeature> UpdateNeed([FromBody] RoadWorkNeedFeature roadWorkNeedFeature)
        {
            if (roadWorkNeedFeature != null && roadWorkNeedFeature.geometry != null &&
                    roadWorkNeedFeature.geometry.coordinates != null &&
                    roadWorkNeedFeature.geometry.coordinates.Length > 2)
            {

                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    try
                    {

                        Polygon roadWorkNeedPoly = roadWorkNeedFeature.geometry.getNtsPolygon();

                        if(!roadWorkNeedPoly.IsSimple)
                        {
                            roadWorkNeedFeature.errorMessage = "RWP-4";
                            return Ok(roadWorkNeedFeature);
                        }

                        if(!roadWorkNeedPoly.IsValid)
                        {
                            roadWorkNeedFeature.errorMessage = "RWP-5";
                            return Ok(roadWorkNeedFeature);
                        }

                        roadWorkNeedFeature.properties.managementarea = new ManagementAreaFeature();

                        // only if project area is greater than 10qm:
                        if (roadWorkNeedPoly.Area > 10.0)
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
                            selectMgmtAreaComm.Parameters.AddWithValue("geom", roadWorkNeedPoly);

                            using (NpgsqlDataReader reader = selectMgmtAreaComm.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    roadWorkNeedFeature.properties.managementarea.properties.uuid = reader.IsDBNull(0) ? "" : reader.GetGuid(0).ToString();
                                    roadWorkNeedFeature.properties.managementarea.properties.manager.firstName = reader.IsDBNull(1) ? "" : reader.GetString(1);
                                    roadWorkNeedFeature.properties.managementarea.properties.manager.lastName = reader.IsDBNull(2) ? "" : reader.GetString(2);
                                }
                            }

                            if (roadWorkNeedFeature.properties.managementarea.properties.uuid == "")
                            {
                                roadWorkNeedFeature.errorMessage = "RWP-0";
                                return Ok(roadWorkNeedFeature);
                            }

                            NpgsqlCommand updateComm = pgConn.CreateCommand();
                            updateComm.CommandText = @"UPDATE ""roadworkneeds""
                                    SET name=@name, kind=@kind, orderer=@orderer,
                                    finish_early_from=@finish_early_from, finish_early_to=@finish_early_to,
                                    finish_optimum_from=@finish_optimum_from, finish_optimum_to=@finish_optimum_to,
                                    finish_late_from=@finish_late_from, finish_late_to=@finish_late_to,
                                    priority=@priority, status=@status, comment=@comment,
                                    managementarea=@managementarea, geom=@geom
                                    WHERE uuid=@uuid";

                            updateComm.Parameters.AddWithValue("name", roadWorkNeedFeature.properties.name);
                            updateComm.Parameters.AddWithValue("kind", roadWorkNeedFeature.properties.kind);
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
                            if (roadWorkNeedFeature.properties.priority.uuid != "")
                            {
                                updateComm.Parameters.AddWithValue("priority", new Guid(roadWorkNeedFeature.properties.priority.uuid));
                            }
                            else
                            {
                                updateComm.Parameters.AddWithValue("priority", DBNull.Value);
                            }
                            if (roadWorkNeedFeature.properties.status.uuid != "")
                            {
                                updateComm.Parameters.AddWithValue("status", new Guid(roadWorkNeedFeature.properties.status.uuid));
                            }
                            else
                            {
                                updateComm.Parameters.AddWithValue("status", DBNull.Value);
                            }
                            updateComm.Parameters.AddWithValue("comment", roadWorkNeedFeature.properties.comment);
                            if (roadWorkNeedFeature.properties.managementarea.properties.uuid != "")
                            {
                                updateComm.Parameters.AddWithValue("managementarea", new Guid(roadWorkNeedFeature.properties.managementarea.properties.uuid));
                            }
                            else
                            {
                                updateComm.Parameters.AddWithValue("managementarea", DBNull.Value);
                            }
                            updateComm.Parameters.AddWithValue("geom", roadWorkNeedPoly);
                            updateComm.Parameters.AddWithValue("uuid", new Guid(roadWorkNeedFeature.properties.uuid));

                            updateComm.ExecuteNonQuery();
                        }
                        else
                        {
                            roadWorkNeedFeature.errorMessage = "RWP-1";
                            return Ok(roadWorkNeedFeature);
                        }
                    }
                    catch (Exception ex)
                    {
                        _logger.LogError(ex.Message);
                        roadWorkNeedFeature.errorMessage = "RWP-3";
                        return Ok(roadWorkNeedFeature);
                    }
                    finally
                    {
                        pgConn.Close();
                    }
                }
            }
            else
            {
                if (roadWorkNeedFeature != null)
                {
                    roadWorkNeedFeature.errorMessage = "RWP-2";
                    return Ok(roadWorkNeedFeature);
                }
            }
            return Ok(roadWorkNeedFeature);
        }

    }
}
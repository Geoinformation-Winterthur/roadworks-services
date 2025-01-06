using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using NetTopologySuite.Geometries;
using Npgsql;
using roadwork_portal_service.Configuration;
using roadwork_portal_service.DAO;
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
        [Authorize(Roles = "view,orderer,trefficmanager,territorymanager,administrator")]
        public IEnumerable<RoadWorkNeedFeature> GetNeeds(int? relevance,
                DateTime? dateOfCreation, int? year, string? uuids,
                string? roadWorkActivityUuid, string? name,
                string? areaManagerUuid, bool? onlyMyNeeds,
                string? status, string? intersectsActivityUuid,
                bool summary = false)
        {
            List<RoadWorkNeedFeature> projectsFromDb = new List<RoadWorkNeedFeature>();
            try
            {

                if (year == null)
                    year = 0;

                if (uuids == null)
                    uuids = "";
                else
                    uuids = uuids.Trim().ToLower();

                if (roadWorkActivityUuid == null)
                    roadWorkActivityUuid = "";
                else
                    roadWorkActivityUuid = roadWorkActivityUuid.Trim().ToLower();

                if (intersectsActivityUuid == null)
                    intersectsActivityUuid = "";
                else
                    intersectsActivityUuid = intersectsActivityUuid.Trim().ToLower();

                if (name == null)
                    name = "";
                else
                    name = name.Trim().ToLower();

                if (status == null)
                    status = "";
                else
                    status = status.Trim().ToLower();
                string[] statusArray = status.Split(",");

                if (areaManagerUuid == null)
                    areaManagerUuid = "";
                else
                    areaManagerUuid = areaManagerUuid.Trim().ToLower();

                if (onlyMyNeeds == null)
                    onlyMyNeeds = false;

                User userFromDb = LoginController.getAuthorizedUserFromDb(this.User, false);

                // get data of current user from database:
                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    pgConn.Open();
                    NpgsqlCommand selectComm = pgConn.CreateCommand();
                    selectComm.CommandText = @"SELECT r.uuid, r.name, r.orderer,
                            u.first_name, u.last_name, o.name as orgname, r.finish_early_to,
                            r.finish_optimum_to, r.finish_late_to, p.code, r.status as statusname,
                            r.description, 
                            r.created, r.last_modified, u.e_mail,
                            r.relevance, 
                            r.note_of_area_man, r.area_man_note_date,
                            n.first_name as area_manager_first_name, n.last_name as area_manager_last_name,
                            r.private, r.section, r.comment, r.url, o.is_civil_eng, o.abbreviation,
                            r.overarching_measure, r.desired_year_from, r.desired_year_to,
                            r.has_sponge_city_meas, r.is_sponge_1_1, r.is_sponge_1_2,
                            r.is_sponge_1_3, r.is_sponge_1_4, r.is_sponge_1_5, r.is_sponge_1_6,
                            r.is_sponge_1_7, r.is_sponge_1_8, r.is_sponge_2_1, r.is_sponge_2_2,
                            r.is_sponge_2_3, r.is_sponge_2_4, r.is_sponge_2_5, r.is_sponge_2_6,
                            r.is_sponge_2_7, r.is_sponge_3_1, r.is_sponge_3_2, r.is_sponge_3_3,
                            r.is_sponge_4_1, r.is_sponge_4_2, r.is_sponge_5_1, r.delete_reason,
                            r.geom";

                    if (uuids == "" && roadWorkActivityUuid != "")
                    {
                        selectComm.CommandText += @", an.uuid_roadwork_activity, an.activityrelationtype, an.is_primary";
                    }

                    selectComm.CommandText += @" FROM ""wtb_ssp_roadworkneeds"" r
                        LEFT JOIN ""wtb_ssp_users"" u ON r.orderer = u.uuid
                        LEFT JOIN ""wtb_ssp_organisationalunits"" o ON u.org_unit = o.uuid
                        LEFT JOIN ""wtb_ssp_priorities"" p ON r.priority = p.code
                        LEFT JOIN ""wtb_ssp_users"" n ON r.area_man_of_note = n.uuid";

                    if (uuids == "")
                    {
                        if (roadWorkActivityUuid != "")
                        {
                            selectComm.CommandText += @" LEFT JOIN ""wtb_ssp_activities_to_needs"" an ON an.uuid_roadwork_need = r.uuid";
                        }
                        else if (intersectsActivityUuid != "")
                        {
                            selectComm.CommandText += @" LEFT JOIN ""wtb_ssp_roadworkactivities"" act ON act.uuid = @act_uuid";
                            selectComm.Parameters.AddWithValue("act_uuid", new Guid(intersectsActivityUuid));
                        }
                    }

                    selectComm.CommandText += " WHERE (r.private = false OR r.orderer = @orderer_uuid)";
                    selectComm.Parameters.AddWithValue("orderer_uuid", new Guid(userFromDb.uuid));

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
                        selectComm.CommandText += " AND r.uuid = ANY (:uuids)";
                        selectComm.Parameters.AddWithValue("uuids", uuidsList);

                    }
                    else if (roadWorkActivityUuid != "")
                    {
                        selectComm.CommandText += @" AND an.uuid_roadwork_activity = @act_uuid";
                        selectComm.Parameters.AddWithValue("act_uuid", new Guid(roadWorkActivityUuid));
                    }
                    else if (intersectsActivityUuid != "")
                    {
                        selectComm.CommandText += @" AND ST_Intersects(act.geom, r.geom)";
                    }
                    else
                    {
                        if (year != 0)
                        {
                            selectComm.CommandText += " AND EXTRACT(YEAR FROM r.finish_optimum_to) = @year";
                            selectComm.Parameters.AddWithValue("year", year);
                        }

                        if (name != "")
                        {
                            selectComm.CommandText += " AND LOWER(r.name) LIKE @name";
                            selectComm.Parameters.AddWithValue("name", "%" + name + "%");
                        }

                        if (relevance != null)
                        {
                            selectComm.CommandText += " AND r.relevance = @relevance";
                            selectComm.Parameters.AddWithValue("relevance", relevance);
                        }

                        if (dateOfCreation != null)
                        {
                            selectComm.CommandText += " AND r.created = @created";
                            selectComm.Parameters.AddWithValue("created", dateOfCreation);
                        }

                        if ((bool)onlyMyNeeds)
                        {
                            selectComm.CommandText += " AND r.orderer = @orderer_uuid";
                        }

                        if (statusArray[0] != "")
                        {
                            selectComm.CommandText += " AND (r.status=@status0";
                            selectComm.Parameters.AddWithValue("status0", statusArray[0]);

                            if (statusArray.Length > 1)
                            {
                                for (int i = 1; i < statusArray.Length; i++)
                                {
                                    selectComm.CommandText += " OR ";
                                    selectComm.CommandText += "r.status=@status" + i;
                                    selectComm.Parameters.AddWithValue("status" + i, statusArray[i]);
                                }
                            }
                            selectComm.CommandText += ")";
                        }
                    }

                    List<RoadWorkNeedFeature> projectsFromDbTemp = new List<RoadWorkNeedFeature>();

                    using (NpgsqlDataReader reader = selectComm.ExecuteReader())
                    {
                        RoadWorkNeedFeature needFeatureFromDb;
                        while (reader.Read())
                        {
                            needFeatureFromDb = new RoadWorkNeedFeature();
                            needFeatureFromDb.properties.uuid =
                                    reader.IsDBNull(reader.GetOrdinal("uuid")) ?
                                        "" : reader.GetGuid(reader.GetOrdinal("uuid")).ToString();
                            needFeatureFromDb.properties.name =
                                    reader.IsDBNull(reader.GetOrdinal("name")) ?
                                        "" : reader.GetString(reader.GetOrdinal("name"));
                            User orderer = new User();
                            orderer.uuid = reader.IsDBNull(reader.GetOrdinal("orderer")) ? "" : reader.GetGuid(reader.GetOrdinal("orderer")).ToString();
                            orderer.firstName = reader.IsDBNull(reader.GetOrdinal("first_name")) ? "" : reader.GetString(reader.GetOrdinal("first_name"));
                            orderer.lastName = reader.IsDBNull(reader.GetOrdinal("last_name")) ? "" : reader.GetString(reader.GetOrdinal("last_name"));
                            OrganisationalUnit orgUnit = new OrganisationalUnit();
                            orgUnit.name = reader.IsDBNull(reader.GetOrdinal("orgname")) ? "" : reader.GetString(reader.GetOrdinal("orgname"));
                            orderer.organisationalUnit = orgUnit;
                            needFeatureFromDb.properties.orderer = orderer;
                            needFeatureFromDb.properties.finishEarlyTo = reader.IsDBNull(reader.GetOrdinal("finish_early_to")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("finish_early_to"));
                            needFeatureFromDb.properties.finishOptimumTo = reader.IsDBNull(reader.GetOrdinal("finish_optimum_to")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("finish_optimum_to"));
                            needFeatureFromDb.properties.finishLateTo = reader.IsDBNull(reader.GetOrdinal("finish_late_to")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("finish_late_to"));
                            Priority priority = new Priority();
                            priority.code = reader.IsDBNull(reader.GetOrdinal("code")) ? "" : reader.GetString(reader.GetOrdinal("code"));
                            needFeatureFromDb.properties.priority = priority;
                            needFeatureFromDb.properties.status = reader.IsDBNull(reader.GetOrdinal("statusname")) ? "" : reader.GetString(reader.GetOrdinal("statusname"));
                            needFeatureFromDb.properties.description = reader.IsDBNull(reader.GetOrdinal("description")) ? "" : reader.GetString(reader.GetOrdinal("description"));
                            needFeatureFromDb.properties.created = reader.IsDBNull(reader.GetOrdinal("created")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("created"));
                            needFeatureFromDb.properties.lastModified = reader.IsDBNull(reader.GetOrdinal("last_modified")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("last_modified"));
                            needFeatureFromDb.properties.orderer.mailAddress = reader.IsDBNull(reader.GetOrdinal("e_mail")) ? "" : reader.GetString(reader.GetOrdinal("e_mail"));
                            needFeatureFromDb.properties.noteOfAreaManager = reader.IsDBNull(reader.GetOrdinal("note_of_area_man")) ? "" : reader.GetString(reader.GetOrdinal("note_of_area_man"));
                            needFeatureFromDb.properties.areaManagerNoteDate = reader.IsDBNull(reader.GetOrdinal("area_man_note_date")) ? DateTime.MinValue : reader.GetDateTime(reader.GetOrdinal("area_man_note_date"));

                            User areaManagerOfNote = new User();
                            areaManagerOfNote.firstName = reader.IsDBNull(reader.GetOrdinal("area_manager_first_name")) ? "" : reader.GetString(reader.GetOrdinal("area_manager_first_name"));
                            areaManagerOfNote.lastName = reader.IsDBNull(reader.GetOrdinal("area_manager_last_name")) ? "" : reader.GetString(reader.GetOrdinal("area_manager_last_name"));
                            needFeatureFromDb.properties.areaManagerOfNote = areaManagerOfNote;
                            needFeatureFromDb.properties.isPrivate = reader.IsDBNull(reader.GetOrdinal("private")) ? true : reader.GetBoolean(reader.GetOrdinal("private"));
                            needFeatureFromDb.properties.section = reader.IsDBNull(reader.GetOrdinal("section")) ? "" : reader.GetString(reader.GetOrdinal("section"));
                            needFeatureFromDb.properties.comment = reader.IsDBNull(reader.GetOrdinal("comment")) ? "" : reader.GetString(reader.GetOrdinal("comment"));
                            needFeatureFromDb.properties.url = reader.IsDBNull(reader.GetOrdinal("url")) ? "" : reader.GetString(reader.GetOrdinal("url"));
                            needFeatureFromDb.properties.orderer.organisationalUnit.isCivilEngineering = reader.IsDBNull(reader.GetOrdinal("is_civil_eng")) ? false : reader.GetBoolean(reader.GetOrdinal("is_civil_eng"));

                            orgUnit.abbreviation = reader.IsDBNull(reader.GetOrdinal("abbreviation")) ? "" : reader.GetString(reader.GetOrdinal("abbreviation"));

                            needFeatureFromDb.properties.overarchingMeasure = reader.IsDBNull(reader.GetOrdinal("overarching_measure")) ? false : reader.GetBoolean(reader.GetOrdinal("overarching_measure"));
                            needFeatureFromDb.properties.desiredYearFrom = reader.IsDBNull(reader.GetOrdinal("desired_year_from")) ? null : reader.GetInt32(reader.GetOrdinal("desired_year_from"));
                            needFeatureFromDb.properties.desiredYearTo = reader.IsDBNull(reader.GetOrdinal("desired_year_to")) ? null : reader.GetInt32(reader.GetOrdinal("desired_year_to"));

                            needFeatureFromDb.properties.hasSpongeCityMeasures = reader.IsDBNull(reader.GetOrdinal("has_sponge_city_meas")) ? false : reader.GetBoolean(reader.GetOrdinal("has_sponge_city_meas"));

                            List<string> spongeCityMeasures = new List<string>();
                            if (!reader.IsDBNull(reader.GetOrdinal("is_sponge_1_1")) && reader.GetBoolean(reader.GetOrdinal("is_sponge_1_1"))) spongeCityMeasures.Add("1.1");
                            if (!reader.IsDBNull(reader.GetOrdinal("is_sponge_1_2")) && reader.GetBoolean(reader.GetOrdinal("is_sponge_1_2"))) spongeCityMeasures.Add("1.2");
                            if (!reader.IsDBNull(reader.GetOrdinal("is_sponge_1_3")) && reader.GetBoolean(reader.GetOrdinal("is_sponge_1_3"))) spongeCityMeasures.Add("1.3");
                            if (!reader.IsDBNull(reader.GetOrdinal("is_sponge_1_4")) && reader.GetBoolean(reader.GetOrdinal("is_sponge_1_4"))) spongeCityMeasures.Add("1.4");
                            if (!reader.IsDBNull(reader.GetOrdinal("is_sponge_1_5")) && reader.GetBoolean(reader.GetOrdinal("is_sponge_1_5"))) spongeCityMeasures.Add("1.5");
                            if (!reader.IsDBNull(reader.GetOrdinal("is_sponge_1_6")) && reader.GetBoolean(reader.GetOrdinal("is_sponge_1_6"))) spongeCityMeasures.Add("1.6");
                            if (!reader.IsDBNull(reader.GetOrdinal("is_sponge_1_7")) && reader.GetBoolean(reader.GetOrdinal("is_sponge_1_7"))) spongeCityMeasures.Add("1.7");
                            if (!reader.IsDBNull(reader.GetOrdinal("is_sponge_1_8")) && reader.GetBoolean(reader.GetOrdinal("is_sponge_1_8"))) spongeCityMeasures.Add("1.8");
                            if (!reader.IsDBNull(reader.GetOrdinal("is_sponge_2_1")) && reader.GetBoolean(reader.GetOrdinal("is_sponge_2_1"))) spongeCityMeasures.Add("2.1");
                            if (!reader.IsDBNull(reader.GetOrdinal("is_sponge_2_2")) && reader.GetBoolean(reader.GetOrdinal("is_sponge_2_2"))) spongeCityMeasures.Add("2.2");
                            if (!reader.IsDBNull(reader.GetOrdinal("is_sponge_2_3")) && reader.GetBoolean(reader.GetOrdinal("is_sponge_2_3"))) spongeCityMeasures.Add("2.3");
                            if (!reader.IsDBNull(reader.GetOrdinal("is_sponge_2_4")) && reader.GetBoolean(reader.GetOrdinal("is_sponge_2_4"))) spongeCityMeasures.Add("2.4");
                            if (!reader.IsDBNull(reader.GetOrdinal("is_sponge_2_5")) && reader.GetBoolean(reader.GetOrdinal("is_sponge_2_5"))) spongeCityMeasures.Add("2.5");
                            if (!reader.IsDBNull(reader.GetOrdinal("is_sponge_2_6")) && reader.GetBoolean(reader.GetOrdinal("is_sponge_2_6"))) spongeCityMeasures.Add("2.6");
                            if (!reader.IsDBNull(reader.GetOrdinal("is_sponge_2_7")) && reader.GetBoolean(reader.GetOrdinal("is_sponge_2_7"))) spongeCityMeasures.Add("2.7");
                            if (!reader.IsDBNull(reader.GetOrdinal("is_sponge_3_1")) && reader.GetBoolean(reader.GetOrdinal("is_sponge_3_1"))) spongeCityMeasures.Add("3.1");
                            if (!reader.IsDBNull(reader.GetOrdinal("is_sponge_3_2")) && reader.GetBoolean(reader.GetOrdinal("is_sponge_3_2"))) spongeCityMeasures.Add("3.2");
                            if (!reader.IsDBNull(reader.GetOrdinal("is_sponge_3_3")) && reader.GetBoolean(reader.GetOrdinal("is_sponge_3_3"))) spongeCityMeasures.Add("3.3");
                            if (!reader.IsDBNull(reader.GetOrdinal("is_sponge_4_1")) && reader.GetBoolean(reader.GetOrdinal("is_sponge_4_1"))) spongeCityMeasures.Add("4.1");
                            if (!reader.IsDBNull(reader.GetOrdinal("is_sponge_4_2")) && reader.GetBoolean(reader.GetOrdinal("is_sponge_4_2"))) spongeCityMeasures.Add("4.2");
                            if (!reader.IsDBNull(reader.GetOrdinal("is_sponge_5_1")) && reader.GetBoolean(reader.GetOrdinal("is_sponge_5_1"))) spongeCityMeasures.Add("5.1");

                            needFeatureFromDb.properties.spongeCityMeasures = spongeCityMeasures.ToArray();

                            if (!reader.IsDBNull(reader.GetOrdinal("delete_reason")))
                                needFeatureFromDb.properties.deleteReason =
                                    reader.GetString(reader.GetOrdinal("delete_reason"));

                            Polygon ntsPoly =
                                    reader.IsDBNull(reader.GetOrdinal("geom")) ?
                                            Polygon.Empty : reader.GetValue(reader.GetOrdinal("geom")) as Polygon;
                            needFeatureFromDb.geometry = new RoadworkPolygon(ntsPoly);

                            if (roadWorkActivityUuid != "")
                            {
                                needFeatureFromDb.properties.roadWorkActivityUuid =
                                            reader.IsDBNull(reader.GetOrdinal("uuid_roadwork_activity")) ?
                                                    "" : reader.GetGuid(reader.GetOrdinal("uuid_roadwork_activity")).ToString();
                                needFeatureFromDb.properties.activityRelationType =
                                            reader.IsDBNull(reader.GetOrdinal("activityrelationtype")) ?
                                                    "" : reader.GetString(reader.GetOrdinal("activityrelationtype"));
                                needFeatureFromDb.properties.isPrimary =
                                            reader.IsDBNull(reader.GetOrdinal("is_primary")) ?
                                                    false : reader.GetBoolean(reader.GetOrdinal("is_primary"));
                            }

                            needFeatureFromDb.properties.isEditingAllowed = false;
                            string mailOfLoggedInUser = User.FindFirstValue(ClaimTypes.Email);
                            if (User.IsInRole("administrator"))
                            {
                                needFeatureFromDb.properties.isEditingAllowed = true;
                            }
                            else if (needFeatureFromDb.properties.orderer.mailAddress == mailOfLoggedInUser
                                        && needFeatureFromDb.properties.isPrivate)
                            {
                                // editing for the orderer is only allowed as long as the need is not public (is private):
                                needFeatureFromDb.properties.isEditingAllowed = true;
                            }

                            projectsFromDbTemp.Add(needFeatureFromDb);

                        }
                    }

                    foreach (RoadWorkNeedFeature needFeatureFromDb in projectsFromDbTemp)
                    {
                        NpgsqlCommand selectDocAttsComm = pgConn.CreateCommand();
                        selectDocAttsComm.CommandText = "SELECT uuid, filename " +
                            "FROM \"wtb_ssp_documents\" " +
                            "WHERE roadworkneed=@roadworkneed";
                        selectDocAttsComm.Parameters.AddWithValue("roadworkneed", new Guid(needFeatureFromDb.properties.uuid));

                        List<DocumentAttributes> multipleDocumentsAttributes = new List<DocumentAttributes>();
                        using (NpgsqlDataReader docsReader = selectDocAttsComm.ExecuteReader())
                        {
                            DocumentAttributes documentAttributes;
                            while (docsReader.Read())
                            {
                                if (!docsReader.IsDBNull(0))
                                {
                                    documentAttributes = new DocumentAttributes();
                                    documentAttributes.uuid = docsReader.GetGuid(0).ToString();
                                    documentAttributes.filename = "";
                                    if (!docsReader.IsDBNull(1))
                                    {
                                        documentAttributes.filename = docsReader.GetString(1);
                                    }
                                    multipleDocumentsAttributes.Add(documentAttributes);
                                }
                            }
                        }
                        needFeatureFromDb.properties.documentAtts = multipleDocumentsAttributes.ToArray();
                    }

                    foreach (RoadWorkNeedFeature needFeatureFromDb in projectsFromDbTemp)
                    {
                        NpgsqlCommand selectCostsComm =
                            _CreatePreparedStatementForGetCosts(needFeatureFromDb.properties.uuid, pgConn);
                        using (NpgsqlDataReader reader = selectCostsComm.ExecuteReader())
                        {
                            List<Costs> costsOfRoadwork = new List<Costs>();
                            while (reader.Read())
                            {
                                Costs costs = new Costs();
                                if (!reader.IsDBNull(0)) costs.uuid = reader.GetGuid(0).ToString();
                                if (!reader.IsDBNull(1)) costs.costs = reader.GetDecimal(1);
                                if (!reader.IsDBNull(2)) costs.workTitle = reader.GetString(2);
                                if (!reader.IsDBNull(3)) costs.projectType = reader.GetString(3);
                                if (!reader.IsDBNull(4)) costs.costsComment = reader.GetString(4);
                                costsOfRoadwork.Add(costs);
                            }
                            needFeatureFromDb.properties.costs = costsOfRoadwork.ToArray();
                        }
                    }

                    if (areaManagerUuid != "")
                    {
                        foreach (RoadWorkNeedFeature needFeatureFromDb in projectsFromDbTemp)
                        {
                            NpgsqlCommand selectManAreaComm = pgConn.CreateCommand();
                            selectManAreaComm.CommandText = @"SELECT am.uuid
                                            FROM ""wtb_ssp_managementareas"" m
                                            LEFT JOIN ""wtb_ssp_users"" am ON m.manager = am.uuid
                                            WHERE ST_Area(ST_Intersection(@geom, m.geom)) > 0
                                            ORDER BY ST_Area(ST_Intersection(@geom, m.geom)) DESC
                                            LIMIT 1";
                            selectManAreaComm.Parameters.AddWithValue("geom", needFeatureFromDb.geometry.getNtsPolygon());

                            using (NpgsqlDataReader selectManAreaReader = selectManAreaComm.ExecuteReader())
                            {
                                if (selectManAreaReader.Read())
                                {
                                    string areaManagerUuidFromDb = selectManAreaReader.IsDBNull(0) ? "" : selectManAreaReader.GetGuid(0).ToString();
                                    if (areaManagerUuid == areaManagerUuidFromDb)
                                        projectsFromDb.Add(needFeatureFromDb);
                                }
                            }
                        }

                    }
                    else
                    {
                        projectsFromDb = projectsFromDbTemp;
                    }
                    pgConn.Close();
                }

                return projectsFromDb.ToArray();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                RoadWorkNeedFeature roadWorkNeedFeature = new RoadWorkNeedFeature();
                roadWorkNeedFeature.errorMessage = "SSP-3";
                projectsFromDb = new List<RoadWorkNeedFeature>();
                projectsFromDb.Add(roadWorkNeedFeature);
                return projectsFromDb;
            }

        }

        // POST roadworkneed/
        [HttpPost]
        [Authorize(Roles = "orderer,territorymanager,administrator")]
        public ActionResult<RoadWorkNeedFeature> AddNeed([FromBody] RoadWorkNeedFeature roadWorkNeedFeature, bool isDryRun = false)
        {
            try
            {
                User userFromDb = LoginController.getAuthorizedUserFromDb(this.User, false);
                roadWorkNeedFeature.properties.orderer = userFromDb;

                Polygon roadWorkNeedPoly = roadWorkNeedFeature.geometry.getNtsPolygon();
                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    pgConn.Open();
                    if (roadWorkNeedFeature.properties.name == null || roadWorkNeedFeature.properties.name == "")
                    {
                        roadWorkNeedFeature.properties.name = HelperFunctions.getAddressNames(roadWorkNeedPoly, pgConn);
                    }
                }

                ConfigurationData configData = AppConfigController.getConfigurationFromDb(false);

                RoadWorkNeedDAO roadWorkNeedDAO = new RoadWorkNeedDAO(isDryRun);
                roadWorkNeedFeature = roadWorkNeedDAO.Insert(roadWorkNeedFeature, configData);

                if (roadWorkNeedFeature.errorMessage == "SSP-23")
                    _logger.LogWarning("The provided roadworkneed data has no description attribute value." +
                                " But description is mandatory.");
                else if (roadWorkNeedFeature.errorMessage == "SSP-7")
                    _logger.LogWarning("Roadworkneed Polygon has less than 3 coordinates.");
                else if (roadWorkNeedFeature.errorMessage == "SSP-8")
                    _logger.LogWarning("Roadworkneed area is less than or equal " + configData.minAreaSize + "qm.");
                else if (roadWorkNeedFeature.errorMessage == "SSP-16")
                    _logger.LogWarning("Roadworkneed area is less than or equal " + configData.minAreaSize + "qm.");
                else if (roadWorkNeedFeature.errorMessage == "SSP-22")
                    _logger.LogWarning("No roadworkneed data received.");
                else if (roadWorkNeedFeature.errorMessage == "SSP-26")
                    _logger.LogWarning("URI of given roadwork need is not valid.");
                else if (roadWorkNeedFeature.errorMessage == "SSP-27")
                    _logger.LogWarning("The given desired year value of a newly created roadwork need is invalid.");
                else if (roadWorkNeedFeature.errorMessage == "SSP-30")
                    _logger.LogWarning("Provided roadwork need has no time factor.");
                else if (roadWorkNeedFeature.errorMessage == "SSP-38")
                    _logger.LogWarning("If sponge city measure is activated then at least one sponge city measure must be provided.");
                else if (roadWorkNeedFeature.errorMessage == "SSP-40")
                    _logger.LogWarning("Roadwork need is from civil engineering but one or more required cost " +
                        "estrimation attribute values are missing.");


                return Ok(roadWorkNeedFeature);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                roadWorkNeedFeature.errorMessage = "SSP-3";
                return Ok(roadWorkNeedFeature);
            }

        }

        // PUT roadworkneed/
        [HttpPut]
        [Authorize(Roles = "orderer,territorymanager,administrator")]
        public ActionResult<RoadWorkNeedFeature> UpdateNeed([FromBody] RoadWorkNeedFeature roadWorkNeedFeature)
        {

            try
            {

                if (roadWorkNeedFeature == null)
                {
                    _logger.LogWarning("No roadworkneed data received.");
                    RoadWorkNeedFeature errorObj = new RoadWorkNeedFeature();
                    errorObj.errorMessage = "SSP-22";
                    return Ok(errorObj);
                }

                if (roadWorkNeedFeature.properties.description == null)
                    roadWorkNeedFeature.properties.description = "";
                else
                    roadWorkNeedFeature.properties.description = roadWorkNeedFeature.properties.description.Trim();

                if (roadWorkNeedFeature.properties.description == "")
                {
                    _logger.LogWarning("The provided roadworkneed data has no description attribute value." +
                                " But description is mandatory.");
                    roadWorkNeedFeature.errorMessage = "SSP-23";
                    return Ok(roadWorkNeedFeature);
                }

                if (roadWorkNeedFeature.properties.overarchingMeasure &&
                        ((roadWorkNeedFeature.properties.desiredYearFrom == null
                            || roadWorkNeedFeature.properties.desiredYearFrom < DateTime.Now.Year) ||
                        (roadWorkNeedFeature.properties.desiredYearTo == null
                            || roadWorkNeedFeature.properties.desiredYearTo < DateTime.Now.Year)))
                {
                    _logger.LogWarning("The given desired year value of an updated roadwork need is invalid.");
                    roadWorkNeedFeature.errorMessage = "SSP-27";
                    return Ok(roadWorkNeedFeature);
                }

                Uri uri;
                bool isUri = Uri.TryCreate(roadWorkNeedFeature.properties.url, UriKind.Absolute, out uri);
                if (roadWorkNeedFeature.properties.url != "" && !isUri)
                {
                    _logger.LogWarning("URI of given roadwork need is not valid.");
                    roadWorkNeedFeature.errorMessage = "SSP-26";
                    return Ok(roadWorkNeedFeature);
                }

                if (roadWorkNeedFeature.properties.hasSpongeCityMeasures)
                {
                    if (roadWorkNeedFeature.properties.spongeCityMeasures == null ||
                        roadWorkNeedFeature.properties.spongeCityMeasures.Length == 0)
                    {
                        roadWorkNeedFeature.errorMessage = "SSP-38";
                        return roadWorkNeedFeature;
                    }

                    bool hasNonEmptyEntries = false;
                    for (int i = 0; i < roadWorkNeedFeature.properties.spongeCityMeasures.Length; i++)
                    {
                        if (roadWorkNeedFeature.properties.spongeCityMeasures[i] != null)
                            roadWorkNeedFeature.properties.spongeCityMeasures[i] =
                                roadWorkNeedFeature.properties.spongeCityMeasures[i].Trim();

                        if (roadWorkNeedFeature.properties.spongeCityMeasures[i] != String.Empty)
                            hasNonEmptyEntries = true;
                    }

                    if (!hasNonEmptyEntries)
                    {
                        roadWorkNeedFeature.errorMessage = "SSP-38";
                        return roadWorkNeedFeature;
                    }
                }

                if (roadWorkNeedFeature.properties.orderer.organisationalUnit.isCivilEngineering)
                {
                    bool notValid = false;
                    if (roadWorkNeedFeature.properties.costs == null ||
                            roadWorkNeedFeature.properties.costs.Length == 0)
                        notValid = true;

                    if (!notValid)
                    {
                        foreach (Costs costs in roadWorkNeedFeature.properties.costs)
                        {
                            if (costs.workTitle == null) costs.workTitle = "";
                            if (costs.projectType == null) costs.projectType = "";
                            if (costs.costsComment == null) costs.costsComment = "";

                            costs.workTitle = costs.workTitle.Trim().ToLower();
                            costs.projectType = costs.projectType.Trim().ToLower();
                            costs.costsComment = costs.costsComment.Trim();

                            if (!RoadWorkNeedDAO.IsCostsValid(costs))
                            {
                                notValid = true;
                                break;
                            }
                        }
                    }

                    if (notValid)
                    {
                        _logger.LogWarning("Roadwork need is from civil engineering but one or more required cost " +
                                "estrimation attribute values are missing.");
                        roadWorkNeedFeature.errorMessage = "SSP-40";
                        return Ok(roadWorkNeedFeature);
                    }
                }
                else
                {
                    roadWorkNeedFeature.properties.costs = null;
                }

                if (roadWorkNeedFeature.geometry == null ||
                        roadWorkNeedFeature.geometry.coordinates == null ||
                            roadWorkNeedFeature.geometry.coordinates.Length < 3)
                {
                    _logger.LogWarning("Roadworkneed has a geometry error.");
                    roadWorkNeedFeature = new RoadWorkNeedFeature();
                    roadWorkNeedFeature.errorMessage = "SSP-3";
                    return Ok(roadWorkNeedFeature);
                }

                Polygon roadWorkNeedPoly = roadWorkNeedFeature.geometry.getNtsPolygon();

                ConfigurationData configData = AppConfigController.getConfigurationFromDb(false);
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

                User userFromDb = LoginController.getAuthorizedUserFromDb(this.User, false);

                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {

                    pgConn.Open();

                    if (!User.IsInRole("administrator") && !User.IsInRole("territorymanager"))
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

                    bool isFirstNeed = _isFirstNeed(roadWorkNeedFeature, pgConn);

                    using (NpgsqlTransaction updateTransAction = pgConn.BeginTransaction())
                    {

                        NpgsqlCommand updateComm = pgConn.CreateCommand();
                        updateComm.CommandText = @"UPDATE ""wtb_ssp_roadworkneeds""
                                    SET name=@name, orderer=@orderer, last_modified=@last_modified,
                                    finish_early_to=@finish_early_to, finish_optimum_to=@finish_optimum_to,
                                    finish_late_to=@finish_late_to, priority=@priority,
                                    description=@description,
                                    section=@section, comment=@comment, 
                                    url=@url, private=@private, overarching_measure=@overarching_measure,
                                    desired_year_from=@desired_year_from,
                                    desired_year_to=@desired_year_to, has_sponge_city_meas=@has_sponge_city_meas,
                                    is_sponge_1_1=@is_sponge_1_1, is_sponge_1_2=@is_sponge_1_2,
                                    is_sponge_1_3=@is_sponge_1_3, is_sponge_1_4=@is_sponge_1_4,
                                    is_sponge_1_5=@is_sponge_1_5, is_sponge_1_6=@is_sponge_1_6,
                                    is_sponge_1_7=@is_sponge_1_7, is_sponge_1_8=@is_sponge_1_8,
                                    is_sponge_2_1=@is_sponge_2_1, is_sponge_2_2=@is_sponge_2_2,
                                    is_sponge_2_3=@is_sponge_2_3, is_sponge_2_4=@is_sponge_2_4,
                                    is_sponge_2_5=@is_sponge_2_5, is_sponge_2_6=@is_sponge_2_6,
                                    is_sponge_2_7=@is_sponge_2_7, is_sponge_3_1=@is_sponge_3_1,
                                    is_sponge_3_2=@is_sponge_3_2, is_sponge_3_3=@is_sponge_3_3,
                                    is_sponge_4_1=@is_sponge_4_1, is_sponge_4_2=@is_sponge_4_2,
                                    is_sponge_5_1=@is_sponge_5_1, geom=@geom";

                        updateComm.Parameters.AddWithValue("name", roadWorkNeedFeature.properties.name);
                        if (roadWorkNeedFeature.properties.orderer.uuid != "")
                        {
                            updateComm.Parameters.AddWithValue("orderer", new Guid(roadWorkNeedFeature.properties.orderer.uuid));
                        }
                        else
                        {
                            updateComm.Parameters.AddWithValue("orderer", DBNull.Value);
                        }
                        roadWorkNeedFeature.properties.lastModified = DateTime.Now;
                        updateComm.Parameters.AddWithValue("last_modified", roadWorkNeedFeature.properties.lastModified);
                        updateComm.Parameters.AddWithValue("finish_early_to", roadWorkNeedFeature.properties.finishEarlyTo);
                        updateComm.Parameters.AddWithValue("finish_optimum_to", roadWorkNeedFeature.properties.finishOptimumTo);
                        updateComm.Parameters.AddWithValue("finish_late_to", roadWorkNeedFeature.properties.finishLateTo);
                        updateComm.Parameters.AddWithValue("priority", roadWorkNeedFeature.properties.priority.code);
                        updateComm.Parameters.AddWithValue("description", roadWorkNeedFeature.properties.description);
                        updateComm.Parameters.AddWithValue("section", roadWorkNeedFeature.properties.section);
                        updateComm.Parameters.AddWithValue("comment", roadWorkNeedFeature.properties.comment);
                        updateComm.Parameters.AddWithValue("url", roadWorkNeedFeature.properties.url);
                        updateComm.Parameters.AddWithValue("private", roadWorkNeedFeature.properties.isPrivate);
                        updateComm.Parameters.AddWithValue("overarching_measure", roadWorkNeedFeature.properties.overarchingMeasure);
                        if (roadWorkNeedFeature.properties.desiredYearFrom != null)
                            updateComm.Parameters.AddWithValue("desired_year_from", roadWorkNeedFeature.properties.desiredYearFrom);
                        else
                            updateComm.Parameters.AddWithValue("desired_year_from", DBNull.Value);
                        if (roadWorkNeedFeature.properties.desiredYearTo != null)
                            updateComm.Parameters.AddWithValue("desired_year_to", roadWorkNeedFeature.properties.desiredYearTo);
                        else
                            updateComm.Parameters.AddWithValue("desired_year_to", DBNull.Value);

                        updateComm.Parameters.AddWithValue("has_sponge_city_meas", roadWorkNeedFeature.properties.hasSpongeCityMeasures);

                        updateComm.Parameters.AddWithValue("is_sponge_1_1", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("1.1"));
                        updateComm.Parameters.AddWithValue("is_sponge_1_2", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("1.2"));
                        updateComm.Parameters.AddWithValue("is_sponge_1_3", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("1.3"));
                        updateComm.Parameters.AddWithValue("is_sponge_1_4", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("1.4"));
                        updateComm.Parameters.AddWithValue("is_sponge_1_5", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("1.5"));
                        updateComm.Parameters.AddWithValue("is_sponge_1_6", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("1.6"));
                        updateComm.Parameters.AddWithValue("is_sponge_1_7", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("1.7"));
                        updateComm.Parameters.AddWithValue("is_sponge_1_8", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("1.8"));
                        updateComm.Parameters.AddWithValue("is_sponge_2_1", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("2.1"));
                        updateComm.Parameters.AddWithValue("is_sponge_2_2", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("2.2"));
                        updateComm.Parameters.AddWithValue("is_sponge_2_3", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("2.3"));
                        updateComm.Parameters.AddWithValue("is_sponge_2_4", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("2.4"));
                        updateComm.Parameters.AddWithValue("is_sponge_2_5", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("2.5"));
                        updateComm.Parameters.AddWithValue("is_sponge_2_6", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("2.6"));
                        updateComm.Parameters.AddWithValue("is_sponge_2_7", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("2.7"));
                        updateComm.Parameters.AddWithValue("is_sponge_3_1", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("3.1"));
                        updateComm.Parameters.AddWithValue("is_sponge_3_2", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("3.2"));
                        updateComm.Parameters.AddWithValue("is_sponge_3_3", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("3.3"));
                        updateComm.Parameters.AddWithValue("is_sponge_4_1", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("4.1"));
                        updateComm.Parameters.AddWithValue("is_sponge_4_2", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("4.2"));
                        updateComm.Parameters.AddWithValue("is_sponge_5_1", roadWorkNeedFeature.properties.spongeCityMeasures.Contains("5.1"));
                        updateComm.Parameters.AddWithValue("geom", roadWorkNeedPoly);

                        string activityRelationType = "";
                        if (roadWorkNeedFeature.properties.activityRelationType != null)
                        {
                            activityRelationType = roadWorkNeedFeature.properties.activityRelationType.Trim();
                        }

                        if (activityRelationType == "assignedneed")
                        {
                            updateComm.CommandText += ", status=(SELECT status FROM wtb_ssp_roadworkactivities WHERE uuid = @uuid_roadwork_activity)";
                            updateComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadWorkNeedFeature.properties.roadWorkActivityUuid));
                        }

                        updateComm.CommandText += " WHERE uuid=@uuid";
                        updateComm.Parameters.AddWithValue("uuid", new Guid(roadWorkNeedFeature.properties.uuid));

                        updateComm.ExecuteNonQuery();

                        if (roadWorkNeedFeature.properties.roadWorkActivityUuid != "")
                        {

                            NpgsqlCommand selectCommand = pgConn.CreateCommand();
                            selectCommand.CommandText = @"SELECT uuid_roadwork_activity
                                FROM ""wtb_ssp_activities_to_needs""
                                WHERE uuid_roadwork_need=@uuid
                                AND activityrelationtype='assignedneed'";
                            selectCommand.Parameters.AddWithValue("uuid", new Guid(roadWorkNeedFeature.properties.uuid));

                            string affectedActivityUuid = "";

                            using (NpgsqlDataReader reader = selectCommand.ExecuteReader())
                            {
                                if (reader.Read())
                                {
                                    affectedActivityUuid = reader.IsDBNull(0) ? "" : reader.GetGuid(0).ToString();
                                }
                            }

                            int assignedNeedsCount = _countAssignedNeeds(affectedActivityUuid, pgConn);
                            if (assignedNeedsCount == 1)
                            {
                                _logger.LogWarning("The roadwork need cannot be deleted since it is the last need of the roadwork activity " + affectedActivityUuid);
                                roadWorkNeedFeature.errorMessage = "SSP-29";
                                return Ok(roadWorkNeedFeature);
                            }


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

                            if (activityRelationType != "")
                            {
                                NpgsqlCommand insertComm = pgConn.CreateCommand();
                                insertComm.CommandText = @"INSERT INTO ""wtb_ssp_activities_to_needs""
                                        (uuid, uuid_roadwork_need, uuid_roadwork_activity, activityrelationtype,
                                        is_primary)
                                        VALUES(@uuid, @uuid_roadwork_need, @uuid_roadwork_activity,
                                        @activityrelationtype, @is_primary)";
                                insertComm.Parameters.AddWithValue("uuid", Guid.NewGuid());
                                insertComm.Parameters.AddWithValue("uuid_roadwork_need", new Guid(roadWorkNeedFeature.properties.uuid));
                                insertComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadWorkNeedFeature.properties.roadWorkActivityUuid));
                                insertComm.Parameters.AddWithValue("activityrelationtype", activityRelationType);
                                insertComm.Parameters.AddWithValue("is_primary", isFirstNeed);
                                insertComm.ExecuteNonQuery();

                                NpgsqlCommand insertHistoryComm = pgConn.CreateCommand();
                                insertHistoryComm.CommandText = @"INSERT INTO ""wtb_ssp_activities_history""
                                    (uuid, uuid_roadwork_activity, changedate, who, what)
                                    VALUES
                                    (@uuid, @uuid_roadwork_activity, @changedate, @who, @what)";

                                insertHistoryComm.Parameters.AddWithValue("uuid", Guid.NewGuid());
                                insertHistoryComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadWorkNeedFeature.properties.roadWorkActivityUuid));
                                insertHistoryComm.Parameters.AddWithValue("changedate", DateTime.Now);
                                insertHistoryComm.Parameters.AddWithValue("who", userFromDb.firstName + " " + userFromDb.lastName);
                                string whatText = "Das Baubedürfnis '" + roadWorkNeedFeature.properties.name +
                                                    "' wurde neu zugewiesen. Die neue Zuweisung ist: ";
                                if (activityRelationType == "assignedneed")
                                {
                                    whatText += "Zugewiesenes Bedürfnis";
                                }
                                else if (activityRelationType == "registeredneed")
                                {
                                    whatText += "Angemeldetes Bedürfnis";
                                }
                                insertHistoryComm.Parameters.AddWithValue("what", whatText);

                                insertHistoryComm.ExecuteNonQuery();
                            }
                        }

                        if (roadWorkNeedFeature.properties.costs != null)
                        {
                            NpgsqlCommand deleteCostsComm = pgConn.CreateCommand();
                            deleteCostsComm.CommandText = @"DELETE FROM ""wtb_ssp_costs""
                                        WHERE roadworkneed=@roadworkneed";
                            deleteCostsComm.Parameters.AddWithValue("roadworkneed", new Guid(roadWorkNeedFeature.properties.uuid));
                            deleteCostsComm.ExecuteNonQuery();

                            foreach (Costs costs in roadWorkNeedFeature.properties.costs)
                            {
                                if (costs != null && costs.uuid != null)
                                    costs.uuid = costs.uuid.Trim().ToLower();

                                if (costs != null && costs.uuid != null
                                        && costs.uuid != String.Empty)
                                {
                                    if (costs.workTitle != null) costs.workTitle = costs.workTitle.Trim().ToLower();
                                    if (costs.projectType != null) costs.projectType = costs.projectType.Trim().ToLower();
                                    if (costs.costsComment != null) costs.costsComment = costs.costsComment.Trim();

                                    NpgsqlCommand insertCostsComm = pgConn.CreateCommand();
                                    insertCostsComm.CommandText = @"INSERT INTO ""wtb_ssp_costs""
                                        (uuid, roadworkneed, costs, work_title,
                                        project_type, costs_comment)
                                        VALUES (@uuid, @roadworkneed, @costs, @work_title,
                                        @project_type, @costs_comment)";
                                    insertCostsComm.Parameters.AddWithValue("uuid", Guid.NewGuid());
                                    insertCostsComm.Parameters.AddWithValue("roadworkneed", new Guid(roadWorkNeedFeature.properties.uuid));
                                    insertCostsComm.Parameters.AddWithValue("costs", costs.costs != null ? costs.costs : DBNull.Value);
                                    insertCostsComm.Parameters.AddWithValue("work_title", costs.workTitle != null ? costs.workTitle : DBNull.Value);
                                    insertCostsComm.Parameters.AddWithValue("project_type", costs.projectType != null ? costs.projectType : DBNull.Value);
                                    insertCostsComm.Parameters.AddWithValue("costs_comment", costs.costsComment != null ? costs.costsComment : DBNull.Value);
                                    insertCostsComm.ExecuteNonQuery();
                                }
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
        [Authorize(Roles = "orderer,territorymanager,administrator")]
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

            User userFromDb = LoginController.getAuthorizedUserFromDb(this.User, false);

            try
            {
                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    pgConn.Open();

                    if (!User.IsInRole("administrator") && !User.IsInRole("territorymanager"))
                    {

                        NpgsqlCommand selectOrdererOfNeedComm = pgConn.CreateCommand();
                        selectOrdererOfNeedComm.CommandText = @"SELECT u.e_mail
                                    FROM ""wtb_ssp_roadworkneeds"" r
                                    LEFT JOIN ""wtb_ssp_users"" u ON r.orderer = u.uuid
                                    WHERE r.uuid=@uuid";
                        selectOrdererOfNeedComm.Parameters.AddWithValue("uuid", new Guid(uuid));

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
                            _logger.LogWarning("User " + mailOfLoggedInUser + " has no right to delete " +
                                "roadwork need " + uuid + " but tried " +
                                "to delete it.");
                            errorResult.errorMessage = "SSP-14";
                            return Ok(errorResult);
                        }

                    }

                    NpgsqlCommand selectCommand = pgConn.CreateCommand();
                    selectCommand.CommandText = @"SELECT uuid_roadwork_activity
                                FROM ""wtb_ssp_activities_to_needs""
                                WHERE uuid_roadwork_need=@uuid
                                AND activityrelationtype='assignedneed'";
                    selectCommand.Parameters.AddWithValue("uuid", new Guid(uuid));

                    string affectedActivityUuid = "";

                    using (NpgsqlDataReader reader = selectCommand.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            affectedActivityUuid = reader.IsDBNull(0) ? "" : reader.GetGuid(0).ToString();
                        }
                    }

                    int assignedNeedsCount = _countAssignedNeeds(affectedActivityUuid, pgConn);
                    if (assignedNeedsCount == 1)
                    {
                        _logger.LogWarning("The roadwork need cannot be deleted since it is the last need of the roadwork activity " + affectedActivityUuid);
                        errorResult.errorMessage = "SSP-29";
                        return Ok(errorResult);
                    }

                    NpgsqlCommand deleteRelationComm = pgConn.CreateCommand();
                    deleteRelationComm.CommandText = @"DELETE FROM ""wtb_ssp_activities_to_needs""
                                WHERE uuid_roadwork_need=@uuid";
                    deleteRelationComm.Parameters.AddWithValue("uuid", new Guid(uuid));

                    using NpgsqlTransaction trans = pgConn.BeginTransaction();

                    deleteRelationComm.ExecuteNonQuery();

                    if (!releaseOnly)
                    {
                        NpgsqlCommand deleteDocumentsComm = pgConn.CreateCommand();
                        deleteDocumentsComm = pgConn.CreateCommand();
                        deleteDocumentsComm.CommandText = @"DELETE FROM ""wtb_ssp_documents""
                                WHERE roadworkneed=@uuid";
                        deleteDocumentsComm.Parameters.AddWithValue("uuid", new Guid(uuid));
                        deleteDocumentsComm.ExecuteNonQuery();

                        NpgsqlCommand deleteCostsComm = pgConn.CreateCommand();
                        deleteCostsComm = pgConn.CreateCommand();
                        deleteCostsComm.CommandText = @"DELETE FROM ""wtb_ssp_costs""
                                WHERE roadworkneed=@uuid";
                        deleteCostsComm.Parameters.AddWithValue("uuid", new Guid(uuid));
                        deleteCostsComm.ExecuteNonQuery();
                    }

                    NpgsqlCommand changeNeedComm = pgConn.CreateCommand();
                    if (releaseOnly)
                    {
                        changeNeedComm.CommandText = @"UPDATE ""wtb_ssp_roadworkneeds""
                                SET status='requirement'
                                WHERE uuid=@uuid";
                    }
                    else
                    {
                        changeNeedComm.CommandText = @"DELETE FROM ""wtb_ssp_roadworkneeds""
                                WHERE uuid=@uuid";
                    }
                    changeNeedComm.Parameters.AddWithValue("uuid", new Guid(uuid));
                    changeNeedComm.ExecuteNonQuery();

                    if (affectedActivityUuid != String.Empty)
                    {
                        NpgsqlCommand insertHistoryComm = pgConn.CreateCommand();
                        insertHistoryComm.CommandText = @"INSERT INTO ""wtb_ssp_activities_history""
                                    (uuid, uuid_roadwork_activity, changedate, who, what)
                                    VALUES
                                    (@uuid, @uuid_roadwork_activity, @changedate, @who, @what)";

                        insertHistoryComm.Parameters.AddWithValue("uuid", Guid.NewGuid());
                        insertHistoryComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(affectedActivityUuid));
                        insertHistoryComm.Parameters.AddWithValue("changedate", DateTime.Now);
                        insertHistoryComm.Parameters.AddWithValue("who", userFromDb.firstName + " " + userFromDb.lastName);
                        string whatText = "Das Baubedürfnis '" + uuid +
                                            "' wurde von dieser Massnahme entfernt.";
                        insertHistoryComm.Parameters.AddWithValue("what", whatText);

                        insertHistoryComm.ExecuteNonQuery();
                    }

                    trans.Commit();
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                errorResult.errorMessage = "SSP-3";
                return Ok(errorResult);
            }

            return Ok();

        }

        private bool _isFirstNeed(RoadWorkNeedFeature roadWorkNeedFeature, NpgsqlConnection pgConn)
        {
            if (roadWorkNeedFeature.properties.roadWorkActivityUuid == null
                    || roadWorkNeedFeature.properties.roadWorkActivityUuid == "")
                if (roadWorkNeedFeature.properties.isPrimary == null)
                    return false;
                else
                    return (bool)roadWorkNeedFeature.properties.isPrimary;

            NpgsqlCommand selectIsFirstNeedComm = pgConn.CreateCommand();
            selectIsFirstNeedComm.CommandText = @"SELECT count(*)
                                    FROM ""wtb_ssp_activities_to_needs""
                                    WHERE activityrelationtype = 'assignedneed' AND
                                        uuid_roadwork_activity=@uuid";
            selectIsFirstNeedComm.Parameters.AddWithValue("uuid", new Guid(roadWorkNeedFeature.properties.roadWorkActivityUuid));

            using (NpgsqlDataReader reader = selectIsFirstNeedComm.ExecuteReader())
            {
                if (reader.Read())
                {
                    return reader.IsDBNull(0) ? false : reader.GetInt32(0) == 0;
                }
            }
            return false;
        }

        private int _countAssignedNeeds(string affectedActivityUuid, NpgsqlConnection pgConn)
        {
            int result = 0;
            if (affectedActivityUuid != String.Empty)
            {
                NpgsqlCommand selectCountAssignedNeedsComm = pgConn.CreateCommand();
                selectCountAssignedNeedsComm.CommandText = @"SELECT count(*) FROM ""wtb_ssp_activities_to_needs""
                                                        WHERE uuid_roadwork_activity=@uuid_roadwork_activity
                                                        AND activityrelationtype='assignedneed'";
                selectCountAssignedNeedsComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(affectedActivityUuid));

                using (NpgsqlDataReader reader = selectCountAssignedNeedsComm.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        result = reader.IsDBNull(0) ? 0 : reader.GetInt32(0);
                    }
                }

            }
            return result;
        }

        private NpgsqlCommand _CreatePreparedStatementForGetCosts(string roadWorkNeedUuid,
                NpgsqlConnection pgConn)
        {
            NpgsqlCommand selectCostsComm = pgConn.CreateCommand();
            selectCostsComm.CommandText = @"SELECT uuid, costs, work_title,
                        project_type, costs_comment
                        FROM ""wtb_ssp_costs""
                        WHERE roadworkneed=@roadworkneed";
            selectCostsComm.Parameters.AddWithValue("roadworkneed", new Guid(roadWorkNeedUuid));
            return selectCostsComm;
        }

    }
}
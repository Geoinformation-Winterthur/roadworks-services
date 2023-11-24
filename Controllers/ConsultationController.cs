using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using roadwork_portal_service.Configuration;
using roadwork_portal_service.Model;

namespace roadwork_portal_service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConsultationController : ControllerBase
    {
        private readonly ILogger<ConsultationController> _logger;
        private IConfiguration Configuration { get; }

        public ConsultationController(ILogger<ConsultationController> logger,
                        IConfiguration configuration)
        {
            _logger = logger;
            this.Configuration = configuration;
        }

        // GET consultation/?roadworkactivityuuid=...
        [HttpGet]
        [Authorize(Roles = "orderer,trefficmanager,territorymanager,administrator")]
        public async Task<IEnumerable<ConsultationInput>> GetConsultations(string roadworkActivityUuid = "")
        {
            List<ConsultationInput> consultationInputs = new List<ConsultationInput>();

            roadworkActivityUuid = roadworkActivityUuid.Trim().ToLower();

            if (String.Empty == roadworkActivityUuid)
            {
                _logger.LogWarning("No roadwork activity UUID was given in get consultations method.");
                ConsultationInput consultationInput = new ConsultationInput();
                consultationInput.errorMessage = "SSP-15";
                consultationInputs.Add(consultationInput);
                return consultationInputs;
            }

            User userFromDb = LoginController.getAuthorizedUserFromDb(this.User, false);

            // get data of current user from database:
            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();

                NpgsqlCommand selectConsultationComm = pgConn.CreateCommand();
                selectConsultationComm.CommandText = @"SELECT c.uuid, c.last_edit, c.decline, 
                                            u.e_mail, u.last_name, u.first_name
                                        FROM ""wtb_ssp_activity_declines"" c
                                        LEFT JOIN ""wtb_ssp_users"" u ON c.input_by = u.uuid
                                        WHERE uuid_roadwork_activity = @uuid_roadwork_activity";
                selectConsultationComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadworkActivityUuid));

                if (userFromDb != null && userFromDb.mailAddress != null && userFromDb.mailAddress != "" &&
                        userFromDb.role != null && userFromDb.role.code != null &&
                        userFromDb.role.code == "orderer")
                {
                    selectConsultationComm.CommandText += " AND u.e_mail = @e_mail";
                    selectConsultationComm.Parameters.AddWithValue("e_mail", userFromDb.mailAddress);
                }

                using (NpgsqlDataReader activityConsultationReader = await selectConsultationComm.ExecuteReaderAsync())
                {
                    while (activityConsultationReader.Read())
                    {
                        ConsultationInput activityConsulationInput = new ConsultationInput();
                        activityConsulationInput.uuid = activityConsultationReader.IsDBNull(0) ? "" : activityConsultationReader.GetGuid(0).ToString();
                        activityConsulationInput.lastEdit = activityConsultationReader.IsDBNull(1) ? DateTime.MinValue : activityConsultationReader.GetDateTime(1);
                        activityConsulationInput.decline = activityConsultationReader.IsDBNull(2) ? false : activityConsultationReader.GetBoolean(2);
                        User consultationUser = new User();
                        consultationUser.mailAddress = activityConsultationReader.IsDBNull(3) ? "" : activityConsultationReader.GetString(3);
                        consultationUser.lastName = activityConsultationReader.IsDBNull(4) ? "" : activityConsultationReader.GetString(4);
                        consultationUser.firstName = activityConsultationReader.IsDBNull(5) ? "" : activityConsultationReader.GetString(5);
                        activityConsulationInput.inputBy = consultationUser;

                        consultationInputs.Add(activityConsulationInput);
                    }
                }
                pgConn.Close();
            }

            return consultationInputs;
        }

        // POST consultation/?roadworkactivityuuid=...
        [HttpPost]
        [Authorize(Roles = "orderer,administrator")]
        public ActionResult<ConsultationInput> AddConsultation(string roadworkActivityUuid, [FromBody] ConsultationInput consultationInput, bool isDryRun = false)
        {
            try
            {
                User userFromDb = LoginController.getAuthorizedUserFromDb(this.User, false);
                consultationInput.inputBy = userFromDb;

                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    pgConn.Open();

                    using NpgsqlTransaction trans = pgConn.BeginTransaction();

                    consultationInput.uuid = Guid.NewGuid().ToString();
                    NpgsqlCommand insertComm = pgConn.CreateCommand();
                    insertComm.CommandText = @"INSERT INTO ""wtb_ssp_activity_declines""
                                    (uuid, uuid_roadwork_activity, last_edit,
                                    input_by, decline)
                                    VALUES (@uuid, @uuid_roadwork_activity, @last_edit,
                                    @input_by, @decline)";
                    insertComm.Parameters.AddWithValue("uuid", new Guid(consultationInput.uuid));
                    insertComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadworkActivityUuid));
                    insertComm.Parameters.AddWithValue("last_edit", DateTime.Now);
                    insertComm.Parameters.AddWithValue("input_by", new Guid(userFromDb.uuid));
                    insertComm.Parameters.AddWithValue("decline", consultationInput.decline);

                    insertComm.ExecuteNonQuery();

                    ActivityHistoryItem activityHistoryItem = new ActivityHistoryItem();
                    activityHistoryItem.uuid = Guid.NewGuid().ToString();
                    activityHistoryItem.changeDate = DateTime.Now;
                    activityHistoryItem.who = userFromDb.firstName + " " + userFromDb.lastName;
                    activityHistoryItem.what = "Eingabe zur Vernehmlassung durch Bedarfsteller hinzugefügt.";
                    activityHistoryItem.userComment = "";

                    NpgsqlCommand insertHistoryComm = pgConn.CreateCommand();
                    insertHistoryComm.CommandText = @"INSERT INTO ""wtb_ssp_activities_history""
                            (uuid, uuid_roadwork_activity, changedate, who, what)
                            VALUES
                            (@uuid, @uuid_roadwork_activity, @changedate, @who, @what)";

                    insertHistoryComm.Parameters.AddWithValue("uuid", new Guid(activityHistoryItem.uuid));
                    insertHistoryComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadworkActivityUuid));
                    insertHistoryComm.Parameters.AddWithValue("changedate", activityHistoryItem.changeDate);
                    insertHistoryComm.Parameters.AddWithValue("who", activityHistoryItem.who);
                    insertHistoryComm.Parameters.AddWithValue("what", activityHistoryItem.what);

                    insertHistoryComm.ExecuteNonQuery();

                    trans.Commit();

                    pgConn.Close();
                }

                return Ok(consultationInput);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                consultationInput.errorMessage = "SSP-3";
                return Ok(consultationInput);
            }

        }

        // PUT consultation/?roadworkactivityuuid=...
        [HttpPut]
        [Authorize(Roles = "orderer,administrator")]
        public ActionResult<ConsultationInput> UpdateConsultation(string roadworkActivityUuid, [FromBody] ConsultationInput consultationInput)
        {

            try
            {

                if (consultationInput == null)
                {
                    _logger.LogWarning("No consultation data received.");
                    ConsultationInput errorObj = new ConsultationInput();
                    errorObj.errorMessage = "SSP-15";
                    return Ok(errorObj);
                }

                User userFromDb = LoginController.getAuthorizedUserFromDb(this.User, false);

                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {

                    pgConn.Open();

                    using NpgsqlTransaction trans = pgConn.BeginTransaction();

                    NpgsqlCommand updateComm = pgConn.CreateCommand();
                    updateComm.CommandText = @"UPDATE ""wtb_ssp_activity_declines""
                                    SET last_edit=@last_edit, decline=@decline
                                    WHERE uuid=@uuid AND input_by=@input_by";

                    updateComm.Parameters.AddWithValue("uuid", new Guid(consultationInput.uuid));
                    updateComm.Parameters.AddWithValue("input_by", new Guid(userFromDb.uuid));

                    consultationInput.lastEdit = DateTime.Now;
                    updateComm.Parameters.AddWithValue("last_edit", consultationInput.lastEdit);
                    updateComm.Parameters.AddWithValue("decline", consultationInput.decline);

                    updateComm.ExecuteNonQuery();

                    ActivityHistoryItem activityHistoryItem = new ActivityHistoryItem();
                    activityHistoryItem.uuid = Guid.NewGuid().ToString();
                    activityHistoryItem.changeDate = DateTime.Now;
                    activityHistoryItem.who = userFromDb.firstName + " " + userFromDb.lastName;
                    activityHistoryItem.what = "Eingabe zur Vernehmlassung durch Bedarfsteller aktualisiert.";
                    activityHistoryItem.userComment = "";

                    NpgsqlCommand insertHistoryComm = pgConn.CreateCommand();
                    insertHistoryComm.CommandText = @"INSERT INTO ""wtb_ssp_activities_history""
                            (uuid, uuid_roadwork_activity, changedate, who, what)
                            VALUES
                            (@uuid, @uuid_roadwork_activity, @changedate, @who, @what)";

                    insertHistoryComm.Parameters.AddWithValue("uuid", new Guid(activityHistoryItem.uuid));
                    insertHistoryComm.Parameters.AddWithValue("uuid_roadwork_activity", new Guid(roadworkActivityUuid));
                    insertHistoryComm.Parameters.AddWithValue("changedate", activityHistoryItem.changeDate);
                    insertHistoryComm.Parameters.AddWithValue("who", activityHistoryItem.who);
                    insertHistoryComm.Parameters.AddWithValue("what", activityHistoryItem.what);

                    insertHistoryComm.ExecuteNonQuery();

                    trans.Commit();

                    pgConn.Close();

                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                consultationInput = new ConsultationInput();
                consultationInput.errorMessage = "SSP-3";
                return Ok(consultationInput);
            }

            return Ok(consultationInput);
        }

    }
}
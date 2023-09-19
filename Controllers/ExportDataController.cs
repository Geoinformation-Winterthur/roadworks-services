using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using roadwork_portal_service.Configuration;

namespace roadwork_portal_service.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ExportDataController : ControllerBase
    {
        private readonly ILogger<ExportDataController> _logger;
        private IConfiguration Configuration { get; }

        public ExportDataController(ILogger<ExportDataController> logger,
                        IConfiguration configuration)
        {
            _logger = logger;
            this.Configuration = configuration;
        }

        // GET exportdata/
        [HttpGet]
        [Authorize(Roles = "administrator")]
        public string GetExportAsync()
        {
            // TODO make async

            string resultCsv = "UUID;Bezeichnung;Typ;Vorname;Nachname\r\n";

            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand selectComm = pgConn.CreateCommand();
                selectComm.CommandText = @"SELECT r.uuid, r.name, rwt.name,
                            u.first_name, u.last_name
                        FROM ""wtb_ssp_roadworkneeds"" r
                        LEFT JOIN ""wtb_ssp_users"" u ON r.orderer = u.uuid
                        LEFT JOIN ""wtb_ssp_roadworkneedtypes"" rwt ON r.kind = rwt.code";


                using (NpgsqlDataReader reader = selectComm.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        resultCsv += reader.IsDBNull(0) ? "" : reader.GetGuid(0).ToString() + ";";
                        resultCsv += reader.IsDBNull(1) ? "" : reader.GetString(1) + ";";
                        resultCsv += reader.IsDBNull(2) ? "" : reader.GetString(2) + ";";
                        resultCsv += reader.IsDBNull(3) ? "" : reader.GetString(3) + ";";
                        resultCsv += reader.IsDBNull(4) ? "" : reader.GetString(4);
                        resultCsv += "\r\n";
                    }
                }
                pgConn.Close();
            }

            return resultCsv;
        }

    }
}
using System.Globalization;
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
        public async Task<string> GetExportAsync()
        {
            string resultCsv = "UUID;Titel/Strasse;Projektleiter Vorname;Projektleiter Nachname;" +
                    "Leiter Baustellenverkehr Vorname;Leiter Baustellenverkehr Nachname;Auslösegrund;" +
                    "Erstellungsdatum;Datum letzte Bearbeitung;Datum von;Datum bis;Kosten;Kostenart;" +
                    "Status;Im Internet publiziert;Rechnungsadresse 1;Rechnungsadresse 2;PDB-FID;" +
                    "Strabako-Nr.;Investitionsnummer;Datum SKS;Datum KAP;Datum OKS;Datum GL-TBA\r\n";


            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand selectComm = pgConn.CreateCommand();
                selectComm.CommandText = @"SELECT r.uuid, r.name, rwt.name,
                            o.first_name, o.last_name, r.created, r.last_modified,
                            r.finish_early_to, r.finish_optimum_to, r.finish_late_to,
                            prio.name, status.name, r.costs, r.description,
                            r.note_of_area_man, r.area_man_note_date,
                            areaman.first_name, areaman.last_name, r.relevance
                        FROM ""wtb_ssp_roadworkneeds"" r
                        LEFT JOIN ""wtb_ssp_users"" o ON r.orderer = o.uuid
                        LEFT JOIN ""wtb_ssp_roadworkneedtypes"" rwt ON r.kind = rwt.code
                        LEFT JOIN ""wtb_ssp_priorities"" prio ON r.priority = prio.code
                        LEFT JOIN ""wtb_ssp_status"" status ON r.status = status.code
                        LEFT JOIN ""wtb_ssp_users"" areaman ON r.area_man_of_note = areaman.uuid";


                using (NpgsqlDataReader reader = await selectComm.ExecuteReaderAsync())
                {
                    string status = "";
                    while (await reader.ReadAsync())
                    {
                        status = reader.IsDBNull(11) ? "" : reader.GetString(11);
                        if (status == "Bedarf")
                        {
                            resultCsv += reader.IsDBNull(0) ? ";" : reader.GetGuid(0).ToString() + ";";
                            resultCsv += reader.IsDBNull(1) ? ";" : reader.GetString(1) + ";";
                            resultCsv += ";;;;";
                            resultCsv += reader.IsDBNull(13) ? ";" : reader.GetString(13) + ";";
                            resultCsv += reader.IsDBNull(5) ? ";" : reader.GetDateTime(5).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(6) ? ";" : reader.GetDateTime(6).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(7) ? ";" : reader.GetDateTime(7).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(9) ? ";" : reader.GetDateTime(9).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(12) ? ";" : reader.GetInt32(12) + ";";
                            resultCsv += ";" + status + ";;;;;;;;;;;";
                            resultCsv += "\r\n";
                        }
                    }
                }
                pgConn.Close();
            }


            using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
            {
                pgConn.Open();
                NpgsqlCommand selectComm = pgConn.CreateCommand();
                selectComm.CommandText = @"SELECT r.uuid, r.name,
                            p.first_name, p.last_name, t.first_name, t.last_name,
                            r.description, r.created, r.last_modified,
                            r.date_from, r.date_to, r.costs, c.name, status.name,
                            r.in_internet, r.billing_address1, r.billing_address2,
                            r.pdb_fid, r.strabako_no, r.investment_no, r.date_sks,
                            r.date_kap, r.date_oks, r.date_gl_tba
                        FROM ""wtb_ssp_roadworkactivities"" r
                        LEFT JOIN ""wtb_ssp_users"" p ON r.projectmanager = p.uuid
                        LEFT JOIN ""wtb_ssp_users"" t ON r.traffic_agent = t.uuid
                        LEFT JOIN ""wtb_ssp_costtypes"" c ON r.costs_type = c.code
                        LEFT JOIN ""wtb_ssp_status"" status ON r.status = status.code";


                using (NpgsqlDataReader reader = await selectComm.ExecuteReaderAsync())
                {
                    while (await reader.ReadAsync())
                    {
                        resultCsv += reader.IsDBNull(0) ? ";" : reader.GetGuid(0).ToString() + ";";
                        resultCsv += reader.IsDBNull(1) ? ";" : reader.GetString(1) + ";";
                        resultCsv += reader.IsDBNull(2) ? ";" : reader.GetString(2) + ";";
                        resultCsv += reader.IsDBNull(3) ? ";" : reader.GetString(3) + ";";
                        resultCsv += reader.IsDBNull(4) ? ";" : reader.GetString(4) + ";";
                        resultCsv += reader.IsDBNull(5) ? ";" : reader.GetString(5) + ";";
                        resultCsv += reader.IsDBNull(6) ? ";" : reader.GetString(6) + ";";
                        resultCsv += reader.IsDBNull(7) ? ";" : reader.GetDateTime(7).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                        resultCsv += reader.IsDBNull(8) ? ";" : reader.GetDateTime(8).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                        resultCsv += reader.IsDBNull(9) ? ";" : reader.GetDateTime(9).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                        resultCsv += reader.IsDBNull(10) ? ";" : reader.GetDateTime(10).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                        resultCsv += reader.IsDBNull(11) ? ";" : reader.GetInt32(11) + ";";
                        resultCsv += reader.IsDBNull(12) ? ";" : reader.GetString(12) + ";";
                        resultCsv += reader.IsDBNull(13) ? ";" : reader.GetString(13) + ";";
                        resultCsv += reader.IsDBNull(14) ? ";" : reader.GetBoolean(14) + ";";
                        resultCsv += reader.IsDBNull(15) ? ";" : reader.GetString(15) + ";";
                        resultCsv += reader.IsDBNull(16) ? ";" : reader.GetString(16) + ";";
                        resultCsv += reader.IsDBNull(17) ? ";" : reader.GetInt32(17) + ";";
                        resultCsv += reader.IsDBNull(18) ? ";" : reader.GetString(18) + ";";
                        resultCsv += reader.IsDBNull(19) ? ";" : reader.GetInt32(19) + ";";
                        resultCsv += reader.IsDBNull(20) ? ";" : reader.GetString(20) + ";";
                        resultCsv += reader.IsDBNull(21) ? ";" : reader.GetString(21) + ";";
                        resultCsv += reader.IsDBNull(22) ? ";" : reader.GetString(22) + ";";
                        resultCsv += reader.IsDBNull(23) ? "" : reader.GetDateTime(23).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture);
                        resultCsv += "\r\n";
                    }
                }
                pgConn.Close();
            }


            return resultCsv;
        }

    }
}
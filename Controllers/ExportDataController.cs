using System.Globalization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using roadwork_portal_service.Configuration;
using roadwork_portal_service.Helper;

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
        // This provides the attributive data download (no geodata)
        [HttpGet]
        [Authorize(Roles = "administrator")]
        public async Task<string> GetExportAsync()
        {
            try
            {
                string resultCsv = "UUID;Titel/Strasse;Projektleiter Vorname;Projektleiter Nachname;" +
                        "Leiter Baustellenverkehr Vorname;Leiter Baustellenverkehr Nachname;Auslösegrund;" +
                        "Erstellungsdatum;Datum letzte Bearbeitung;Datum von;Datum bis;Kosten;Ist privat;Kostenart;" +
                        "Status;Im Internet publiziert;Rechnungsadresse 1;Rechnungsadresse 2;PDB-FID;" +
                        "Strabako-Nr.;Investitionsnummer;Datum SKS;Datum KAP;Datum OKS;Datum GL-TBA;" +
                        "Projektnummer;Kommentar;Abschnitt;URL;Projekttyp;Übergeordnete Massnahme;Wunschjahr von;" +
                        "Wunschjahr bis;Vorstudie;date_optimum;Baubeginn;Bauende;" +
                        "Abnahmedatum;consult_due;SKS, genehmigt;KAP, genehmigt;OKS, genehmigt;" +
                        "GL TBA, genehmigt;date_planned;date_accept;Garantie;is_study;Plantermin: Vorstudie Start;" +
                        "Plantermin: Vorstudie Ende;Projektauftrag Vorstudie genehmigt;" +
                        "Vorstudie genehmigt;Begehrensäusserung § 45;Begehrensäusserung Start;" +
                        "Begehrensäusserung Ende;Mitwirkungsverfahren § 13;Mitwirkungsverfahren Start;" +
                        "Mitwirkungsverfahren Ende;Planauflage § 16;" +
                        "Planauflage Start;Planauflage Ende;Bedarfsklärung Start;Bedarfsklärung Ende;" +
                        "Bedarfsklärung Abschluss;Stellungnahme Start;Stellungnahme Ende;" +
                        "Stellungnahme Abschluss;Infoversand Start;" +
                        "Infoversand Ende;Infoversand Abschluss;Aggloprogramm;date_start_inconsult;verifiziert;" +
                        "date_start_reporting;sistiert;koordiniert\r\n";


                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    pgConn.Open();
                    NpgsqlCommand selectComm = pgConn.CreateCommand();
                    selectComm.CommandText = @"SELECT r.uuid, r.name,
                            o.first_name, o.last_name, r.created, r.last_modified,
                            r.finish_early_to, r.finish_optimum_to, r.finish_late_to,
                            prio.name, r.status, r.costs, r.private, r.description,
                            r.note_of_area_man, r.area_man_note_date,
                            areaman.first_name, areaman.last_name, r.relevance
                        FROM ""wtb_ssp_roadworkneeds"" r
                        LEFT JOIN ""wtb_ssp_users"" o ON r.orderer = o.uuid
                        LEFT JOIN ""wtb_ssp_priorities"" prio ON r.priority = prio.code
                        LEFT JOIN ""wtb_ssp_users"" areaman ON r.area_man_of_note = areaman.uuid";


                    using (NpgsqlDataReader reader = await selectComm.ExecuteReaderAsync())
                    {
                        string status = "";
                        while (await reader.ReadAsync())
                        {
                            status = reader.IsDBNull(10) ? "" : _sanitizeForCsv(reader.GetString(10));
                            if (status == "requirement")
                            {
                                resultCsv += reader.IsDBNull(0) ? ";" : reader.GetGuid(0).ToString() + ";";
                                resultCsv += reader.IsDBNull(1) ? ";" : _sanitizeForCsv(reader.GetString(1)) + ";";
                                resultCsv += ";;;;";
                                resultCsv += reader.IsDBNull(13) ? ";" : _sanitizeForCsv(reader.GetString(13)) + ";";
                                resultCsv += reader.IsDBNull(5) ? ";" : reader.GetDateTime(5).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                                resultCsv += reader.IsDBNull(6) ? ";" : reader.GetDateTime(6).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                                resultCsv += reader.IsDBNull(7) ? ";" : reader.GetDateTime(7).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                                resultCsv += reader.IsDBNull(8) ? ";" : reader.GetDateTime(8).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                                resultCsv += reader.IsDBNull(11) ? ";" : reader.GetInt32(11) + ";";
                                resultCsv += reader.IsDBNull(12) ? ";" : reader.GetBoolean(12) + ";";
                                string statusName = HelperFunctions.translateStatusCodes(status);
                                resultCsv += ";" + statusName + ";;;;;;;;;;;";
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
                            r.date_from, r.date_to, r.costs, r.private, c.name, r.status,
                            r.in_internet, r.billing_address1, r.billing_address2,
                            r.pdb_fid, r.strabako_no, r.investment_no, r.date_sks,
                            r.date_kap, r.date_oks, r.date_gl_tba, r.project_no,
                            r.comment, r.section, r.url, r.projecttype,
                            r.overarching_measure, r.desired_year_from,
                            r.desired_year_to, r.prestudy, r.date_optimum,
                            r.start_of_construction, r.end_of_construction,
                            r.date_of_acceptance, r.consult_due, r.date_sks_real,
                            date_kap_real, date_oks_real, date_gl_tba_real,
                            r.date_planned, r.date_accept, r.date_guarantee,
                            r.is_study, r.date_study_start, r.date_study_end,
                            r.project_study_approved, r.study_approved, r.is_desire,
                            r.date_desire_start, r.date_desire_end, r.is_particip,
                            r.date_particip_start, r.date_particip_end,
                            r.is_plan_circ, r.date_plan_circ_start, r.date_plan_circ_end,
                            r.date_consult_start, r.date_consult_end, r.date_consult_close,
                            r.date_report_start, r.date_report_end, r.date_report_close,
                            r.date_info_start, r.date_info_end, r.date_info_close,
                            r.is_aggloprog, r.date_start_inconsult, r.date_start_verified,
                            r.date_start_reporting, r.date_start_suspended,
                            r.date_start_coordinated
                        FROM ""wtb_ssp_roadworkactivities"" r
                        LEFT JOIN ""wtb_ssp_users"" p ON r.projectmanager = p.uuid
                        LEFT JOIN ""wtb_ssp_users"" t ON r.traffic_agent = t.uuid
                        LEFT JOIN ""wtb_ssp_costtypes"" c ON r.costs_type = c.code";


                    using (NpgsqlDataReader reader = await selectComm.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            resultCsv += reader.IsDBNull(0) ? ";" : reader.GetGuid(0).ToString() + ";";
                            resultCsv += reader.IsDBNull(1) ? ";" : _sanitizeForCsv(reader.GetString(1)) + ";";
                            resultCsv += reader.IsDBNull(2) ? ";" : _sanitizeForCsv(reader.GetString(2)) + ";";
                            resultCsv += reader.IsDBNull(3) ? ";" : _sanitizeForCsv(reader.GetString(3)) + ";";
                            resultCsv += reader.IsDBNull(4) ? ";" : _sanitizeForCsv(reader.GetString(4)) + ";";
                            resultCsv += reader.IsDBNull(5) ? ";" : _sanitizeForCsv(reader.GetString(5)) + ";";
                            resultCsv += reader.IsDBNull(6) ? ";" : _sanitizeForCsv(reader.GetString(6)) + ";";
                            resultCsv += reader.IsDBNull(7) ? ";" : reader.GetDateTime(7).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(8) ? ";" : reader.GetDateTime(8).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(9) ? ";" : reader.GetDateTime(9).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(10) ? ";" : reader.GetDateTime(10).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(11) ? ";" : reader.GetInt32(11) + ";";
                            resultCsv += reader.IsDBNull(12) ? ";" : reader.GetBoolean(12) + ";";
                            resultCsv += reader.IsDBNull(13) ? ";" : _sanitizeForCsv(reader.GetString(13)) + ";";
                            resultCsv += reader.IsDBNull(14) ? ";" : _sanitizeForCsv(reader.GetString(14)) + ";";
                            resultCsv += reader.IsDBNull(15) ? ";" : reader.GetBoolean(15) + ";";
                            resultCsv += reader.IsDBNull(16) ? ";" : _sanitizeForCsv(reader.GetString(16)) + ";";
                            resultCsv += reader.IsDBNull(17) ? ";" : _sanitizeForCsv(reader.GetString(17)) + ";";
                            resultCsv += reader.IsDBNull(18) ? ";" : reader.GetInt32(18) + ";";
                            resultCsv += reader.IsDBNull(19) ? ";" : _sanitizeForCsv(reader.GetString(19)) + ";";
                            resultCsv += reader.IsDBNull(20) ? ";" : reader.GetInt32(20) + ";";
                            resultCsv += reader.IsDBNull(21) ? ";" : reader.GetDateTime(21).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(22) ? ";" : reader.GetDateTime(22).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(23) ? ";" : _sanitizeForCsv(reader.GetString(23)) + ";";
                            resultCsv += reader.IsDBNull(24) ? ";" : reader.GetDateTime(24).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(25) ? ";" : _sanitizeForCsv(reader.GetString(25)) + ";";
                            resultCsv += reader.IsDBNull(26) ? ";" : _sanitizeForCsv(reader.GetString(26)) + ";";
                            resultCsv += reader.IsDBNull(27) ? ";" : _sanitizeForCsv(reader.GetString(27)) + ";";
                            resultCsv += reader.IsDBNull(28) ? ";" : _sanitizeForCsv(reader.GetString(28)) + ";";
                            resultCsv += reader.IsDBNull(29) ? ";" : _sanitizeForCsv(reader.GetString(29)) + ";";
                            resultCsv += reader.IsDBNull(30) ? ";" : reader.GetBoolean(30) + ";";
                            resultCsv += reader.IsDBNull(31) ? ";" : reader.GetInt32(31) + ";";
                            resultCsv += reader.IsDBNull(32) ? ";" : reader.GetInt32(32) + ";";
                            resultCsv += reader.IsDBNull(33) ? ";" : reader.GetBoolean(33) + ";";
                            resultCsv += reader.IsDBNull(34) ? ";" : reader.GetDateTime(34).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(35) ? ";" : reader.GetDateTime(35).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(36) ? ";" : reader.GetDateTime(36).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(37) ? ";" : reader.GetDateTime(37).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(38) ? ";" : reader.GetDateTime(38).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(39) ? ";" : reader.GetDateTime(39).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(40) ? ";" : reader.GetDateTime(40).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(41) ? ";" : reader.GetDateTime(41).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(42) ? ";" : reader.GetDateTime(42).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(43) ? ";" : reader.GetDateTime(43).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(44) ? ";" : reader.GetDateTime(44).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(45) ? ";" : reader.GetDateTime(45).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(46) ? ";" : reader.GetBoolean(46) + ";";
                            resultCsv += reader.IsDBNull(47) ? ";" : reader.GetDateTime(47).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(48) ? ";" : reader.GetDateTime(48).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(49) ? ";" : reader.GetDateTime(49).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(50) ? ";" : reader.GetDateTime(50).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(51) ? ";" : reader.GetBoolean(51) + ";";
                            resultCsv += reader.IsDBNull(52) ? ";" : reader.GetDateTime(52).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(53) ? ";" : reader.GetDateTime(53).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(54) ? ";" : reader.GetBoolean(54) + ";";
                            resultCsv += reader.IsDBNull(55) ? ";" : reader.GetDateTime(55).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(56) ? ";" : reader.GetDateTime(56).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(57) ? ";" : reader.GetBoolean(57) + ";";
                            resultCsv += reader.IsDBNull(58) ? ";" : reader.GetDateTime(58).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(59) ? ";" : reader.GetDateTime(59).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(60) ? ";" : reader.GetDateTime(60).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(61) ? ";" : reader.GetDateTime(61).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(62) ? ";" : reader.GetDateTime(62).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(63) ? ";" : reader.GetDateTime(63).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(64) ? ";" : reader.GetDateTime(64).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(65) ? ";" : reader.GetDateTime(65).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(66) ? ";" : reader.GetDateTime(66).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(67) ? ";" : reader.GetDateTime(67).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(68) ? ";" : reader.GetDateTime(68).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(69) ? ";" : reader.GetBoolean(69) + ";";
                            resultCsv += reader.IsDBNull(70) ? ";" : reader.GetDateTime(70).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(71) ? ";" : reader.GetDateTime(71).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(72) ? ";" : reader.GetDateTime(72).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(73) ? ";" : reader.GetDateTime(73).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(74) ? ";" : reader.GetDateTime(74).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += "\r\n";
                        }
                    }
                    pgConn.Close();
                }
                return resultCsv;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex.Message);
                return "Ein Fehler ist aufgetreten. Bitte kontaktieren Sie den Administrator.";
            }

        }

        private static string? _sanitizeForCsv(string toClean)
        {
            return toClean == null ? null : toClean.Replace(";", ",");
        }

    }
}
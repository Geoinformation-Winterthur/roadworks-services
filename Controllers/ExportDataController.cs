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
                        "Erstellungsdatum;Datum letzte Bearbeitung;Datum von;Datum bis;Ist privat;" +
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
                        "Infoversand Ende;Infoversand Abschluss;Aggloprogramm;date_start_inconsult1;verifiziert1;date_start_inconsult2;verifiziert2;" +
                        "date_start_reporting;sistiert;koordiniert\r\n";


                using (NpgsqlConnection pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    pgConn.Open();
                    NpgsqlCommand selectComm = pgConn.CreateCommand();
                    selectComm.CommandText = @"SELECT r.uuid, r.name,
                            o.first_name, o.last_name, r.created, r.last_modified,
                            r.finish_early_to, r.finish_optimum_to, r.finish_late_to,
                            prio.name, r.status, r.private, r.description,
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
                            status = reader.IsDBNull(reader.GetOrdinal("status")) ? "" : _sanitizeForCsv(reader.GetString(reader.GetOrdinal("status")));
                            if (status == "requirement")
                            {
                                resultCsv += reader.IsDBNull(reader.GetOrdinal("uuid")) ? ";" : reader.GetGuid(reader.GetOrdinal("uuid")).ToString() + ";";
                                resultCsv += reader.IsDBNull(reader.GetOrdinal("name")) ? ";" : _sanitizeForCsv(reader.GetString(reader.GetOrdinal("name"))) + ";";
                                resultCsv += ";;;;";
                                resultCsv += reader.IsDBNull(reader.GetOrdinal("description")) ? ";" : _sanitizeForCsv(reader.GetString(reader.GetOrdinal("description"))) + ";";
                                resultCsv += reader.IsDBNull(reader.GetOrdinal("created")) ? ";" : reader.GetDateTime(reader.GetOrdinal("created")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                                resultCsv += reader.IsDBNull(reader.GetOrdinal("last_modified")) ? ";" : reader.GetDateTime(reader.GetOrdinal("last_modified")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                                resultCsv += reader.IsDBNull(reader.GetOrdinal("finish_early_to")) ? ";" : reader.GetDateTime(reader.GetOrdinal("finish_early_to")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                                resultCsv += reader.IsDBNull(reader.GetOrdinal("finish_late_to")) ? ";" : reader.GetDateTime(reader.GetOrdinal("finish_late_to")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                                resultCsv += reader.IsDBNull(reader.GetOrdinal("private")) ? ";" : reader.GetBoolean(reader.GetOrdinal("private")) + ";";
                                string statusName = HelperFunctions.translateStatusCodes(status);
                                resultCsv += statusName + ";;;;;;;;;;;";
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
                            p.first_name as p_first_name, p.last_name as p_last_name,
                            t.first_name as t_first_name, t.last_name as t_last_name,
                            r.description, r.created, r.last_modified,
                            r.date_from, r.date_to, r.private, r.status,
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
                            r.date_consult_start1, r.date_consult_end1, r.date_consult_start2, r.date_consult_end2, r.date_consult_close,
                            r.date_report_start, r.date_report_end, r.date_report_close,
                            r.date_info_start, r.date_info_end, r.date_info_close,
                            r.is_aggloprog, r.date_start_inconsult1, r.date_start_verified1, r.date_start_inconsult2, r.date_start_verified2,
                            r.date_start_reporting, r.date_start_suspended,
                            r.date_start_coordinated
                        FROM ""wtb_ssp_roadworkactivities"" r
                        LEFT JOIN ""wtb_ssp_users"" p ON r.projectmanager = p.uuid
                        LEFT JOIN ""wtb_ssp_users"" t ON r.traffic_agent = t.uuid";


                    using (NpgsqlDataReader reader = await selectComm.ExecuteReaderAsync())
                    {
                        while (await reader.ReadAsync())
                        {
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("uuid")) ? ";" : reader.GetGuid(reader.GetOrdinal("uuid")).ToString() + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("name")) ? ";" : _sanitizeForCsv(reader.GetString(reader.GetOrdinal("name"))) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("p_first_name")) ? ";" : _sanitizeForCsv(reader.GetString(reader.GetOrdinal("p_first_name"))) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("p_last_name")) ? ";" : _sanitizeForCsv(reader.GetString(reader.GetOrdinal("p_last_name"))) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("t_first_name")) ? ";" : _sanitizeForCsv(reader.GetString(reader.GetOrdinal("t_first_name"))) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("t_last_name")) ? ";" : _sanitizeForCsv(reader.GetString(reader.GetOrdinal("t_last_name"))) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("description")) ? ";" : _sanitizeForCsv(reader.GetString(reader.GetOrdinal("description"))) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("created")) ? ";" : reader.GetDateTime(reader.GetOrdinal("created")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("last_modified")) ? ";" : reader.GetDateTime(reader.GetOrdinal("last_modified")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_from")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_from")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_to")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_to")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("private")) ? ";" : reader.GetBoolean(reader.GetOrdinal("private")) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("status")) ? ";" : _sanitizeForCsv(reader.GetString(reader.GetOrdinal("status"))) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("in_internet")) ? ";" : reader.GetBoolean(reader.GetOrdinal("in_internet")) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("billing_address1")) ? ";" : _sanitizeForCsv(reader.GetString(reader.GetOrdinal("billing_address1"))) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("billing_address2")) ? ";" : _sanitizeForCsv(reader.GetString(reader.GetOrdinal("billing_address2"))) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("pdb_fid")) ? ";" : reader.GetInt32(reader.GetOrdinal("pdb_fid")) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("strabako_no")) ? ";" : reader.GetString(reader.GetOrdinal("strabako_no")) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("investment_no")) ? ";" : reader.GetInt32(reader.GetOrdinal("investment_no")) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_sks")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_sks")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_kap")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_kap")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_oks")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_oks")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_gl_tba")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_gl_tba")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("project_no")) ? ";" : _sanitizeForCsv(reader.GetString(reader.GetOrdinal("project_no")) + ";");
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("comment")) ? ";" : _sanitizeForCsv(reader.GetString(reader.GetOrdinal("comment")) + ";");
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("section")) ? ";" : _sanitizeForCsv(reader.GetString(reader.GetOrdinal("section")) + ";");
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("url")) ? ";" : _sanitizeForCsv(reader.GetString(reader.GetOrdinal("url")) + ";");
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("projecttype")) ? ";" : _sanitizeForCsv(reader.GetString(reader.GetOrdinal("projecttype")) + ";");
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("overarching_measure")) ? ";" : reader.GetBoolean(reader.GetOrdinal("overarching_measure")) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("desired_year_from")) ? ";" : reader.GetInt32(reader.GetOrdinal("desired_year_from")) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("desired_year_to")) ? ";" : reader.GetInt32(reader.GetOrdinal("desired_year_to")) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("prestudy")) ? ";" : reader.GetBoolean(reader.GetOrdinal("prestudy")) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_optimum")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_optimum")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("start_of_construction")) ? ";" : reader.GetDateTime(reader.GetOrdinal("start_of_construction")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("end_of_construction")) ? ";" : reader.GetDateTime(reader.GetOrdinal("end_of_construction")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_of_acceptance")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_of_acceptance")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("consult_due")) ? ";" : reader.GetDateTime(reader.GetOrdinal("consult_due")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_sks_real")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_sks_real")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_kap_real")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_kap_real")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_oks_real")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_oks_real")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_gl_tba_real")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_gl_tba_real")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_planned")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_planned")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_accept")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_accept")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_guarantee")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_guarantee")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("is_study")) ? ";" : reader.GetBoolean(reader.GetOrdinal("is_study")) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_study_start")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_study_start")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_study_end")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_study_end")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("project_study_approved")) ? ";" : reader.GetBoolean(reader.GetOrdinal("project_study_approved")) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("study_approved")) ? ";" : reader.GetDateTime(reader.GetOrdinal("study_approved")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += "False;;;";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("is_particip")) ? ";" : reader.GetBoolean(reader.GetOrdinal("is_particip")) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_particip_start")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_particip_start")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_particip_end")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_particip_end")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("is_plan_circ")) ? ";" : reader.GetBoolean(reader.GetOrdinal("is_plan_circ")) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_plan_circ_start")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_plan_circ_start")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_plan_circ_end")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_plan_circ_end")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_consult_start1")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_consult_start1")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_consult_end1")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_consult_end1")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_consult_start2")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_consult_start2")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_consult_end2")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_consult_end2")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_consult_close")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_consult_close")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_report_start")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_report_start")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_report_end")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_report_end")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_report_close")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_report_close")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_info_start")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_info_start")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_info_end")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_info_end")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_info_close")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_info_close")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("is_aggloprog")) ? ";" : reader.GetBoolean(reader.GetOrdinal("is_aggloprog")) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_start_inconsult1")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_start_inconsult1")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_start_verified1")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_start_verified1")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_start_inconsult2")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_start_inconsult2")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_start_verified2")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_start_verified2")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_start_reporting")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_start_reporting")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_start_suspended")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_start_suspended")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
                            resultCsv += reader.IsDBNull(reader.GetOrdinal("date_start_coordinated")) ? ";" : reader.GetDateTime(reader.GetOrdinal("date_start_coordinated")).ToString("dd.MM.yyyy", CultureInfo.InvariantCulture) + ";";
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
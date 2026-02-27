using System.Globalization;
using System.Text;
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

        public ExportDataController(ILogger<ExportDataController> logger, IConfiguration configuration)
        {
            _logger = logger;
            Configuration = configuration;
        }

        // GET exportdata/
        // This provides the attributive data download (no geodata)
        [HttpGet]
        [Authorize(Roles = "administrator")]
        public async Task<string> GetExportAsync()
        {
            try
            {
                // Use StringBuilder to avoid huge string concatenations.
                var sb = new StringBuilder(1024 * 1024);

                // Optional: prepend UTF-8 BOM to improve Excel compatibility even without frontend BOM.
                // Note: If Angular already prepends BOM, double BOM is usually harmless, but you can leave this disabled.
                // sb.Append('\uFEFF');

                // Header row (semicolon-separated, no quoting; therefore we must sanitize text fields properly).
                sb.Append(
                    "UUID;Titel/Strasse;Projektleiter Vorname;Projektleiter Nachname;" +
                    "Leiter Baustellenverkehr Vorname;Leiter Baustellenverkehr Nachname;Auslösegrund;" +
                    "Erstellungsdatum;Datum letzte Bearbeitung;Datum von;Datum bis;Ist privat;" +
                    "Status;Im Internet publiziert;Rechnungsadresse 1;Rechnungsadresse 2;PDB-FID;" +
                    "Strabako-Nr.;Investitionsnummer;Datum SKS;Datum KAP;Datum OKS;Datum GL-TBA;" +
                    "Projektnummer;Kommentar;Abschnitt;URL;Projekttyp;Projekt-Art;Übergeordnete Massnahme;Wunschjahr von;" +
                    "Wunschjahr bis;Vorstudie;date_optimum;Baubeginn;Bauende;" +
                    "Abnahmedatum;consult_due;SKS, genehmigt;KAP, genehmigt;OKS, genehmigt;" +
                    "GL TBA, genehmigt;date_planned;date_accept;Garantie;is_study;Plantermin: Vorstudie Start;" +
                    "Plantermin: Vorstudie Ende;Projektauftrag Vorstudie genehmigt;" +
                    "Vorstudie genehmigt;Begehrensäusserung § 45;Begehrensäusserung Start;" +
                    "Begehrensäusserung Ende;Mitwirkungsverfahren § 13;Mitwirkungsverfahren Start;" +
                    "Mitwirkungsverfahren Ende;Planauflage § 16;" +
                    "Planauflage Start;Planauflage Ende;" +

                    // Make these columns explicit and easy to recognize in Excel import:
                    "Bedarfsklärung 1 Start;Bedarfsklärung 1 Ende;" +
                    "Bedarfsklärung 2 Start;Bedarfsklärung 2 Ende;" +
                    "Stellungnahme Start;Stellungnahme Ende;" +

                    "Infoversand Start;Infoversand Ende;Infoversand Abschluss;" +
                    "Aggloprogramm;date_start_inconsult1;verifiziert1;date_start_inconsult2;verifiziert2;" +
                    "date_start_reporting;sistiert;koordiniert\r\n"
                );

                // 1) Export "needs" (wtb_ssp_roadworkneeds) - only those with status == "requirement"
                await using (var pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    await pgConn.OpenAsync();

                    await using var selectComm = pgConn.CreateCommand();
                    selectComm.CommandText = @"
                        SELECT r.uuid, r.name,
                               o.first_name, o.last_name,
                               r.created, r.last_modified,
                               r.finish_early_to, r.finish_optimum_to, r.finish_late_to,
                               prio.name AS priority_name,
                               r.status, r.private, r.description,
                               r.note_of_area_man, r.area_man_note_date,
                               areaman.first_name AS area_man_first_name,
                               areaman.last_name  AS area_man_last_name,
                               r.relevance
                        FROM ""wtb_ssp_roadworkneeds"" r
                        LEFT JOIN ""wtb_ssp_users"" o ON r.orderer = o.uuid
                        LEFT JOIN ""wtb_ssp_priorities"" prio ON r.priority = prio.code
                        LEFT JOIN ""wtb_ssp_users"" areaman ON r.area_man_of_note = areaman.uuid";

                    await using var reader = await selectComm.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var status = GetText(reader, "status");
                        if (!string.Equals(status, "requirement", StringComparison.OrdinalIgnoreCase))
                            continue;

                        AppendGuid(sb, reader, "uuid");
                        AppendText(sb, reader, "name");

                        // Project manager / traffic agent not applicable here
                        AppendEmpty(sb, 4);

                        // Auslösegrund -> use description
                        AppendText(sb, reader, "description");

                        AppendDate(sb, reader, "created");
                        AppendDate(sb, reader, "last_modified");

                        // Map "finish_early_to" and "finish_late_to" to date_from/date_to to keep columns consistent
                        AppendDate(sb, reader, "finish_early_to"); // Datum von
                        AppendDate(sb, reader, "finish_late_to");  // Datum bis

                        AppendBool(sb, reader, "private");

                        // Status (translated)
                        var statusName = HelperFunctions.translateStatusCodes(status);
                        AppendText(sb, statusName);

                        // Fill the remaining columns with empty fields to match the header count.
                        // (We keep this behavior as in your original code.)
                        AppendEmpty(sb, 11);

                        sb.Append("\r\n");
                    }
                }

                // 2) Export "activities" (wtb_ssp_roadworkactivities)
                await using (var pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    await pgConn.OpenAsync();

                    await using var selectComm = pgConn.CreateCommand();
                    selectComm.CommandText = @"
                        SELECT r.uuid, r.name,
                               p.first_name AS p_first_name, p.last_name AS p_last_name,
                               t.first_name AS t_first_name, t.last_name AS t_last_name,
                               r.description, r.created, r.last_modified,
                               r.date_from, r.date_to, r.private, r.status,
                               r.in_internet, r.billing_address1, r.billing_address2,
                               r.pdb_fid, r.strabako_no, r.investment_no, r.date_sks,
                               r.date_kap, r.date_oks, r.date_gl_tba, r.project_no,
                               r.comment, r.section, r.url, r.projecttype, r.projectkind,
                               r.overarching_measure, r.desired_year_from,
                               r.desired_year_to, r.prestudy, r.date_optimum,
                               r.start_of_construction, r.end_of_construction,
                               r.date_of_acceptance, r.consult_due, r.date_sks_real,
                               r.date_kap_real, r.date_oks_real, r.date_gl_tba_real,
                               r.date_planned, r.date_accept, r.date_guarantee,
                               r.is_study, r.date_study_start, r.date_study_end,
                               r.project_study_approved, r.study_approved,
                               r.is_desire, r.date_desire_start, r.date_desire_end,
                               r.is_particip, r.date_particip_start, r.date_particip_end,
                               r.is_plan_circ, r.date_plan_circ_start, r.date_plan_circ_end,
                               r.date_consult_start1, r.date_consult_end1,
                               r.date_consult_start2, r.date_consult_end2,
                               r.date_report_start, r.date_report_end,
                               r.date_info_start, r.date_info_end, r.date_info_close,
                               r.is_aggloprog, r.date_start_inconsult1, r.date_start_verified1,
                               r.date_start_inconsult2, r.date_start_verified2,
                               r.date_start_reporting, r.date_start_suspended,
                               r.date_start_coordinated
                        FROM ""wtb_ssp_roadworkactivities"" r
                        LEFT JOIN ""wtb_ssp_users"" p ON r.projectmanager = p.uuid
                        LEFT JOIN ""wtb_ssp_users"" t ON r.traffic_agent = t.uuid";

                    await using var reader = await selectComm.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        AppendGuid(sb, reader, "uuid");
                        AppendText(sb, reader, "name");
                        AppendText(sb, reader, "p_first_name");
                        AppendText(sb, reader, "p_last_name");
                        AppendText(sb, reader, "t_first_name");
                        AppendText(sb, reader, "t_last_name");

                        AppendText(sb, reader, "description");

                        AppendDate(sb, reader, "created");
                        AppendDate(sb, reader, "last_modified");
                        AppendDate(sb, reader, "date_from");
                        AppendDate(sb, reader, "date_to");

                        AppendBool(sb, reader, "private");
                        AppendText(sb, reader, "status");
                        AppendBool(sb, reader, "in_internet");

                        AppendText(sb, reader, "billing_address1");
                        AppendText(sb, reader, "billing_address2");

                        AppendInt(sb, reader, "pdb_fid");
                        AppendText(sb, reader, "strabako_no");
                        AppendInt(sb, reader, "investment_no");

                        AppendDate(sb, reader, "date_sks");
                        AppendDate(sb, reader, "date_kap");
                        AppendDate(sb, reader, "date_oks");
                        AppendDate(sb, reader, "date_gl_tba");

                        AppendText(sb, reader, "project_no");
                        AppendText(sb, reader, "comment");
                        AppendText(sb, reader, "section");
                        AppendText(sb, reader, "url");
                        AppendText(sb, reader, "projecttype");
                        AppendText(sb, reader, "projectkind");

                        AppendBool(sb, reader, "overarching_measure");
                        AppendInt(sb, reader, "desired_year_from");
                        AppendInt(sb, reader, "desired_year_to");

                        AppendBool(sb, reader, "prestudy");
                        AppendDate(sb, reader, "date_optimum");
                        AppendDate(sb, reader, "start_of_construction");
                        AppendDate(sb, reader, "end_of_construction");
                        AppendDate(sb, reader, "date_of_acceptance");
                        AppendDate(sb, reader, "consult_due");

                        AppendDate(sb, reader, "date_sks_real");
                        AppendDate(sb, reader, "date_kap_real");
                        AppendDate(sb, reader, "date_oks_real");
                        AppendDate(sb, reader, "date_gl_tba_real");

                        AppendDate(sb, reader, "date_planned");
                        AppendDate(sb, reader, "date_accept");
                        AppendDate(sb, reader, "date_guarantee");

                        AppendBool(sb, reader, "is_study");
                        AppendDate(sb, reader, "date_study_start");
                        AppendDate(sb, reader, "date_study_end");
                        AppendBool(sb, reader, "project_study_approved");
                        AppendDate(sb, reader, "study_approved");

                        // IMPORTANT FIX:
                        // Previously: "False;;;" caused column shift -> dates ended up under wrong headers in Excel import.
                        AppendBool(sb, reader, "is_desire");
                        AppendDate(sb, reader, "date_desire_start");
                        AppendDate(sb, reader, "date_desire_end");

                        AppendBool(sb, reader, "is_particip");
                        AppendDate(sb, reader, "date_particip_start");
                        AppendDate(sb, reader, "date_particip_end");

                        AppendBool(sb, reader, "is_plan_circ");
                        AppendDate(sb, reader, "date_plan_circ_start");
                        AppendDate(sb, reader, "date_plan_circ_end");

                        // Vernehmlassungen / consult rounds and Stellungnahme (make columns explicit)
                        AppendDate(sb, reader, "date_consult_start1"); // Bedarfsklärung 1 Start
                        AppendDate(sb, reader, "date_consult_end1");   // Bedarfsklärung 1 Ende
                        AppendDate(sb, reader, "date_consult_start2"); // Bedarfsklärung 2 Start
                        AppendDate(sb, reader, "date_consult_end2");   // Bedarfsklärung 2 Ende
                        AppendDate(sb, reader, "date_report_start");   // Stellungnahme Start
                        AppendDate(sb, reader, "date_report_end");     // Stellungnahme Ende

                        // Infoversand
                        AppendDate(sb, reader, "date_info_start");
                        AppendDate(sb, reader, "date_info_end");
                        AppendDate(sb, reader, "date_info_close");

                        // Aggloprogramm + status flags at the end
                        AppendBool(sb, reader, "is_aggloprog");
                        AppendDate(sb, reader, "date_start_inconsult1");
                        AppendDate(sb, reader, "date_start_verified1");
                        AppendDate(sb, reader, "date_start_inconsult2");
                        AppendDate(sb, reader, "date_start_verified2");
                        AppendDate(sb, reader, "date_start_reporting");
                        AppendDate(sb, reader, "date_start_suspended");
                        AppendDate(sb, reader, "date_start_coordinated");

                        sb.Append("\r\n");
                    }
                }

                return sb.ToString();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "ExportDataController.GetExportAsync failed");
                return "Ein Fehler ist aufgetreten. Bitte kontaktieren Sie den Administrator.";
            }
        }

        // --- CSV helpers ---

        private static void AppendEmpty(StringBuilder sb, int count)
        {
            for (int i = 0; i < count; i++)
                sb.Append(';');
        }

        private static void AppendGuid(StringBuilder sb, NpgsqlDataReader reader, string column)
        {
            sb.Append(reader.IsDBNull(reader.GetOrdinal(column)) ? "" : reader.GetGuid(reader.GetOrdinal(column)).ToString());
            sb.Append(';');
        }

        private static void AppendInt(StringBuilder sb, NpgsqlDataReader reader, string column)
        {
            sb.Append(reader.IsDBNull(reader.GetOrdinal(column)) ? "" : reader.GetInt32(reader.GetOrdinal(column)).ToString(CultureInfo.InvariantCulture));
            sb.Append(';');
        }

        private static void AppendBool(StringBuilder sb, NpgsqlDataReader reader, string column)
        {
            sb.Append(reader.IsDBNull(reader.GetOrdinal(column)) ? "" : reader.GetBoolean(reader.GetOrdinal(column)).ToString());
            sb.Append(';');
        }

        private static void AppendDate(StringBuilder sb, NpgsqlDataReader reader, string column)
        {
            if (reader.IsDBNull(reader.GetOrdinal(column)))
            {
                sb.Append(';');
                return;
            }

            var dt = reader.GetDateTime(reader.GetOrdinal(column));
            sb.Append(dt.ToString("dd.MM.yyyy", CultureInfo.InvariantCulture));
            sb.Append(';');
        }

        private static void AppendText(StringBuilder sb, NpgsqlDataReader reader, string column)
        {
            sb.Append(reader.IsDBNull(reader.GetOrdinal(column)) ? "" : SanitizeForCsv(reader.GetString(reader.GetOrdinal(column))));
            sb.Append(';');
        }

        private static void AppendText(StringBuilder sb, string? value)
        {
            sb.Append(string.IsNullOrWhiteSpace(value) ? "" : SanitizeForCsv(value));
            sb.Append(';');
        }

        private static string GetText(NpgsqlDataReader reader, string column)
        {
            return reader.IsDBNull(reader.GetOrdinal(column)) ? "" : SanitizeForCsv(reader.GetString(reader.GetOrdinal(column)));
        }

        // Sanitization for "semicolon-separated no-quote CSV":
        // - Replace semicolons (delimiter) to keep column integrity.
        // - Replace CR/LF/TAB to avoid line breaks inside cells (Excel import).
        // - Collapse repeated whitespace.
        private static string SanitizeForCsv(string toClean)
        {
            if (string.IsNullOrEmpty(toClean))
                return "";

            var s = toClean;

            // Normalize line breaks and tabs -> space
            s = s.Replace("\r\n", " ")
                 .Replace("\n", " ")
                 .Replace("\r", " ")
                 .Replace("\t", " ");

            // Replace delimiter
            s = s.Replace(";", ",");

            // Optional: remove non-breaking space, normalize
            s = s.Replace('\u00A0', ' ');

            // Collapse multiple spaces
            while (s.Contains("  "))
                s = s.Replace("  ", " ");

            return s.Trim();
        }
    }
}
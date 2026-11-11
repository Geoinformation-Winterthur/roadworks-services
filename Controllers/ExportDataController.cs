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
                    "Phase;Status;BV-Nr;Titel/Strasse;Abschnitt;Bemerkung;Gebietsmanagement;Auslösende:r;Auslösendes Werk;Beteiligte:r;" +
                    "Projektleiter Vorname;Projektleiter Nachname;" +
                    "Leiter Baustellenverkehr Vorname;Leiter Baustellenverkehr Nachname;Auslösegrund;" +
                    "Erstellungsdatum;Datum letzte Bearbeitung;Ist privat;" +
                    "Im Internet publiziert;Rechnungsadresse 1;Rechnungsadresse 2;PDB-FID;" +
                    "Investitionsnummer;Datum SKS;Datum KAP berechnet;Datum OKS;" +
                    "URL;Arbeitsbezeichnung;Projekttyp;Projekt-Art;Übergeordnete Massnahme;Wunschjahr von;" +
                    "Wunschjahr bis;VS vorgesehen;VS vorgesehen (SKS);Verkehrsanordnung;date_optimum;Baubeginn;Bauende;" +
                    "consult_due;SKS, genehmigt;KAP, genehmigt;" +
                    "GL TBA, genehmigt;date_planned;date_accept;Garantie;Vorstudie;Plantermin: Vorstudie Start;" +
                    "Plantermin: Vorstudie Ende;Projektauftrag Vorstudie genehmigt;" +
                    "Vorstudie genehmigt;" +
                    "Mitwirkungsverfahren § 13;Mitwirkungsverfahren Start;" +
                    "Mitwirkungsverfahren Ende;Planauflage § 16;" +
                    "Planauflage Start;Planauflage Ende;" +
                    "Störfallverordnung;Akustisches Projekt;" +
                    "Aggloprogramm vorgesehen;Private betroffen;Provis;Vorgesehene Tätigkeiten/Arbeiten;Randbedingungen/Abhängigkeiten;" +
                    "Rechts-/Landerwerb vorgesehen;Umsetzung durch Dritte/Werk;" +

                    // Make these columns explicit and easy to recognize in Excel import:
                    "Bedarfsklärung 1 Start;Bedarfsklärung 1 Ende;" +
                    "Bedarfsklärung 2 Start;Bedarfsklärung 2 Ende;" +
                    "Stellungnahme Start;Stellungnahme Ende;" +
                    
                    "Aggloprogramm;Start Bedarfsklärung 1;Verifiziert 1;Start Bedarfsklärung 2;Verifiziert 2;" +
                    "Start Stellungnahme;Sistiert;Koordiniert;Oks aktiv Änderungsdatum;" +
                    "Projektierungsauftrag erstellt bis;APR Projektiert bis;APR Realisiert bis;Bestellungen Start;" +
                    "Bestellungen Ende;Projektierungsauftrag erarbeiten Start;Projektierungsauftrag erarbeiten Ende;" +
                    "Übergabesitzung AMO/AEW an APK;Übergabesitzung APK an APR;Projektierungskredit einholen;" +
                    "Projektfestsetzung Start;Projektfestsetzung Ende;Ausführungskredit Start;Ausführungskredit Ende;" +
                    "Submission Start;Submission Ende;Baubeginn;Bauende;Einbau Deckbelag Start;Einbau Deckbelag Ende;" +
                    "Projektkreditabrechnung;Strassenzustand;Strassenzustand Datum;Strassenzustand Bemerkung;Ist Kleinmassnahme;" +
                    "Bauvorhabe-UUID;Bedarfe-UUIDs;" +

                    // Additional  attributes; keep the original prefix.
                    "BV: Projektleiter UUID;BV: Leiter Baustellenverkehr UUID;BV: Transfernummer;" +
                    "BV: Typ (Code);BV: Datum von;BV: Datum bis;" +
                    "BV: Kosten;BV: Kostenart;BV: Strabako-Nr;" +
                    "BV: OKS genehmigt;BV: GL TBA berechnet;BV: Bedarfsklärung Abschluss;" +
                    "BV: Stellungnahme Abschluss;BV: OKS aktiv;BV: Kosten letzte Bearbeitung;" +
                    "BV: Kosten letzte Bearbeitung durch UUID;BV: SKS terminiert;BV: Sitzungsbemerkung 1;" +
                    "BV: Sitzungsbemerkung 2;BV: Sitzungsbemerkung 3;BV: SKS-Nr;BV: Aggloprogramm Link;" +
                    "BV: Aggloprogramm Generation;BV: Aggloprogramm Massnahmen-Nr;BV: Aggloprogramm ARE-Code;" +
                    "BV: Aggloprogramm ARE-Bezeichnung;BV: Aggloprogramm umzusetzen bis;BV: Aggloprogramm Gesamtkosten;" +
                    "BV: Aggloprogramm Kosten Kanton;BV: Aggloprogramm Kommentar;BV: Haltestellen und Wartehallen betroffen;" +
                    "BV: Kunstbauten betroffen;BV: Strassenentwässerung betroffen;BV: Private Hausanschlüsse betroffen;" +
                    "BV: Entsorgungseinrichtungen betroffen;BV: Technische Einrichtungen betroffen;BV: Bäume betroffen;" +
                    "BV: Stadtmobiliar betroffen;BV: Stadtklima betroffen;BV: Entsiegelung betroffen;" +
                    "BV: Fuss- und Veloverkehr betroffen;BV: Behindertengleichstellung betroffen;BV: Private Betroffenheit Umfang;" +
                    "BV: Private Betroffenheit Anforderungen;BV: Landerwerb von Privaten;BV: Private als Auslösende;" +
                    "BV: Personelle Ressourcen APR bestätigt am;BV: Kostenschätzung APR bestätigt am;BV: Bohrkern beauftragt;" +
                    "BV: Bestellungen eingeholt;BV: Bestellungen geprüft;" +
                    "BV: Check AFM;BV: C und F vorgeprüft;BV: Rechtsdienst vorgeprüft;" +
                    "BV: Verfügt;BV: In Fabasoft abgelegt;BV: GIS nachgeführt;" +
                    "BV: Vorstudie Dauer;BV: Vorstudie Auftragnehmer;BV: Vorstudie Details;" +
                    "BV: VK ER Vorstudie bestätigt am;BV: VK ER Vorstudie Nummer;BV: Status vor Sistierung;" +
                    "BV: Bemerkung zur Sistierung;BV: Bohrkern beauftragt am;BV: AFM geprüft am;" +
                    "BV: C und F vorgeprüft am;BV: Rechtsdienst vorgeprüft am;BV: Verfügt am;" +
                    "BV: In Fabasoft abgelegt am;BV: GIS nachgeführt am;Bedarf: Auslösende UUID;" +
                    "Bedarf: Auslösende Vorname;Bedarf: Auslösende Nachname;Bedarf: Frühester Baubeginn;" +
                    "Bedarf: Wunsch Baubeginn;Bedarf: Späteste Inbetriebnahme;Bedarf: Priorität Code;" +
                    "Bedarf: Priorität;Bedarf: Relevanz;Bedarf: Gebietsmanagement Notiz;" +
                    "Bedarf: Gebietsmanagement Notizdatum;Bedarf: Gebietsmanagement Notiz Verfasser UUID;Bedarf: Gebietsmanagement Notiz Verfasser Vorname;" +
                    "Bedarf: Gebietsmanagement Notiz Verfasser Nachname;Bedarf: Abschnitt;Bedarf: Bemerkung;" +
                    "Bedarf: Link URL;Bedarf: Übergeordnete Massnahme;Bedarf: Wunschjahr von;" +
                    "Bedarf: Wunschjahr bis;Bedarf: Weiterhin relevant;Bedarf: Rückmeldung gegeben;" +
                    "Bedarf: Abgelehnt;Bedarf: Schwammstadtmassnahmen vorhanden;Bedarf: Löschgrund;" +
                    "Bedarf: Bauzeit;Bedarf: Rechts- und Landerwerb vorgesehen;Bedarf: Schwammstadtmassnahme 1.1;" +
                    "Bedarf: Schwammstadtmassnahme 1.2;Bedarf: Schwammstadtmassnahme 1.3;Bedarf: Schwammstadtmassnahme 1.4;" +
                    "Bedarf: Schwammstadtmassnahme 1.5;Bedarf: Schwammstadtmassnahme 1.6;Bedarf: Schwammstadtmassnahme 1.7;" +
                    "Bedarf: Schwammstadtmassnahme 1.8;Bedarf: Schwammstadtmassnahme 2.1;Bedarf: Schwammstadtmassnahme 2.2;" +
                    "Bedarf: Schwammstadtmassnahme 2.3;Bedarf: Schwammstadtmassnahme 2.4;Bedarf: Schwammstadtmassnahme 2.5;" +
                    "Bedarf: Schwammstadtmassnahme 2.6;Bedarf: Schwammstadtmassnahme 2.7;Bedarf: Schwammstadtmassnahme 3.1;" +
                    "Bedarf: Schwammstadtmassnahme 3.2;Bedarf: Schwammstadtmassnahme 3.3;Bedarf: Schwammstadtmassnahme 4.1;" +
                    "Bedarf: Schwammstadtmassnahme 4.2;Bedarf: Schwammstadtmassnahme 5.1;Genehmigung UUID;" +
                    "Genehmigung erforderlich;StrG Planauflageverfahren;SSV erforderlich;" +
                    "WWG erforderlich;ERI erforderlich;PBG erforderlich;" +
                    "EBG erforderlich;AWEL erforderlich;ESTI erforderlich;" +
                    "Weitere Genehmigung erforderlich;Weitere Genehmigung Details;\r\n"
                );

                // 1) Export "needs" (wtb_ssp_roadworkneeds) - only those with status == "requirement"
                await using (var pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    await pgConn.OpenAsync();

                    await using var selectComm = pgConn.CreateCommand();
                    selectComm.CommandText = @"
                        SELECT r.uuid, r.name, r.is_minor_work,
                               o.first_name, o.last_name,
                               r.created, r.last_modified,
                               r.finish_early_to, r.finish_optimum_to, r.finish_late_to,
                               prio.name AS priority_name,
                               r.status, r.private, r.description,
                               r.note_of_area_man, r.area_man_note_date,
                               areaman.first_name AS area_man_first_name,
                               areaman.last_name  AS area_man_last_name,
                               r.relevance
                        , -- Additional need attributes
                            r.orderer,
                            r.priority,
                            r.area_man_of_note,
                            r.section,
                            r.comment,
                            r.url,
                            r.overarching_measure,
                            r.desired_year_from,
                            r.desired_year_to,
                            r.still_relevant,
                            r.feedback_given,
                            r.decline,
                            r.has_sponge_city_meas,
                            r.delete_reason,
                            r.construction_duration_days,
                            r.acquisition_planned,
                            r.is_sponge_1_1,
                            r.is_sponge_1_2,
                            r.is_sponge_1_3,
                            r.is_sponge_1_4,
                            r.is_sponge_1_5,
                            r.is_sponge_1_6,
                            r.is_sponge_1_7,
                            r.is_sponge_1_8,
                            r.is_sponge_2_1,
                            r.is_sponge_2_2,
                            r.is_sponge_2_3,
                            r.is_sponge_2_4,
                            r.is_sponge_2_5,
                            r.is_sponge_2_6,
                            r.is_sponge_2_7,
                            r.is_sponge_3_1,
                            r.is_sponge_3_2,
                            r.is_sponge_3_3,
                            r.is_sponge_4_1,
                            r.is_sponge_4_2,
                            r.is_sponge_5_1,
                            appr.uuid AS uuid_approvals,
                            appr.approval_required,
                            appr.strg_approval_required,
                            appr.ssv_approval_required,
                            appr.wwg_approval_required,
                            appr.eri_approval_required,
                            appr.pbg_approval_required,
                            appr.ebg_approval_required,
                            appr.awel_approval_required,
                            appr.esti_approval_required,
                            appr.other_approval_required,
                            appr.other_approval_details,
                            appr.bafu_approval_required, appr.lsv_approval_required
                        FROM ""wtb_ssp_roadworkneeds"" r
                        LEFT JOIN ""wtb_ssp_users"" o ON r.orderer = o.uuid
                        LEFT JOIN ""wtb_ssp_priorities"" prio ON r.priority = prio.code
                        LEFT JOIN ""wtb_ssp_users"" areaman ON r.area_man_of_note = areaman.uuid
                        LEFT JOIN ""wtb_ssp_roadwork_approvals"" appr ON r.uuid = appr.uuid_roadwork_need";

                    await using var reader = await selectComm.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var status = GetText(reader, "status");
                        if (!string.Equals(status, "requirement", StringComparison.OrdinalIgnoreCase))
                            continue;

                        var statusPhase = HelperFunctions.translateStatusCodes(status, onlyStatusName: false);
                        var statusName = HelperFunctions.translateStatusCodes(status, onlyStatusName: true);
                        AppendText(sb, statusPhase);
                        AppendText(sb, statusName);

                        AppendText(sb, "");
                        AppendText(sb, reader, "name");

                        // Project manager / traffic agent not applicable here
                        AppendEmpty(sb, 10);

                        // Auslösegrund -> use description
                        AppendText(sb, reader, "description");

                        AppendDate(sb, reader, "created");
                        AppendDate(sb, reader, "last_modified");

                        // Map "finish_early_to" and "finish_late_to" to date_from/date_to to keep columns consistent
                        //AppendDate(sb, reader, "finish_early_to"); // Datum von
                        //AppendDate(sb, reader, "finish_late_to");  // Datum bis

                        AppendBool(sb, reader, "private");

                        // Remaining columns with empty fields to match the header count.
                        AppendEmpty(sb, 39); // Columns 19-57.
                        AppendBool(sb, reader, "bafu_approval_required");
                        AppendBool(sb, reader, "lsv_approval_required");
                        AppendEmpty(sb, 46); // Columns 60-102.

                        // Is minor work (Kleinmassnahme)
                        AppendBool(sb, reader, "is_minor_work");

                        // A standalone need UUID belongs in the need UUID column.
                        AppendText(sb, "");
                        AppendGuid(sb, reader, "uuid");

                        // Additional activity attributes are empty for a standalone need.
                        AppendEmpty(sb, 71);

                        // Additional scalar need attributes.
                        AppendGuid(sb, reader, "orderer");
                        AppendText(sb, reader, "first_name");
                        AppendText(sb, reader, "last_name");
                        AppendDate(sb, reader, "finish_early_to");
                        AppendDate(sb, reader, "finish_optimum_to");
                        AppendDate(sb, reader, "finish_late_to");
                        AppendText(sb, reader, "priority");
                        AppendText(sb, reader, "priority_name");
                        AppendInt(sb, reader, "relevance");
                        AppendText(sb, reader, "note_of_area_man");
                        AppendTimestamp(sb, reader, "area_man_note_date");
                        AppendGuid(sb, reader, "area_man_of_note");
                        AppendText(sb, reader, "area_man_first_name");
                        AppendText(sb, reader, "area_man_last_name");
                        AppendText(sb, reader, "section");
                        AppendText(sb, reader, "comment");
                        AppendText(sb, reader, "url");
                        AppendBool(sb, reader, "overarching_measure");
                        AppendInt(sb, reader, "desired_year_from");
                        AppendInt(sb, reader, "desired_year_to");
                        AppendBool(sb, reader, "still_relevant");
                        AppendBool(sb, reader, "feedback_given");
                        AppendBool(sb, reader, "decline");
                        AppendBool(sb, reader, "has_sponge_city_meas");
                        AppendText(sb, reader, "delete_reason");
                        AppendInt(sb, reader, "construction_duration_days");
                        AppendText(sb, reader, "acquisition_planned");
                        AppendBool(sb, reader, "is_sponge_1_1");
                        AppendBool(sb, reader, "is_sponge_1_2");
                        AppendBool(sb, reader, "is_sponge_1_3");
                        AppendBool(sb, reader, "is_sponge_1_4");
                        AppendBool(sb, reader, "is_sponge_1_5");
                        AppendBool(sb, reader, "is_sponge_1_6");
                        AppendBool(sb, reader, "is_sponge_1_7");
                        AppendBool(sb, reader, "is_sponge_1_8");
                        AppendBool(sb, reader, "is_sponge_2_1");
                        AppendBool(sb, reader, "is_sponge_2_2");
                        AppendBool(sb, reader, "is_sponge_2_3");
                        AppendBool(sb, reader, "is_sponge_2_4");
                        AppendBool(sb, reader, "is_sponge_2_5");
                        AppendBool(sb, reader, "is_sponge_2_6");
                        AppendBool(sb, reader, "is_sponge_2_7");
                        AppendBool(sb, reader, "is_sponge_3_1");
                        AppendBool(sb, reader, "is_sponge_3_2");
                        AppendBool(sb, reader, "is_sponge_3_3");
                        AppendBool(sb, reader, "is_sponge_4_1");
                        AppendBool(sb, reader, "is_sponge_4_2");
                        AppendBool(sb, reader, "is_sponge_5_1");

                        // Approval attributes shared by both record kinds.
                        AppendGuid(sb, reader, "uuid_approvals");
                        AppendBool(sb, reader, "approval_required");
                        AppendBool(sb, reader, "strg_approval_required");
                        AppendBool(sb, reader, "ssv_approval_required");
                        AppendBool(sb, reader, "wwg_approval_required");
                        AppendBool(sb, reader, "eri_approval_required");
                        AppendBool(sb, reader, "pbg_approval_required");
                        AppendBool(sb, reader, "ebg_approval_required");
                        AppendBool(sb, reader, "awel_approval_required");
                        AppendBool(sb, reader, "esti_approval_required");
                        AppendBool(sb, reader, "other_approval_required");
                        AppendText(sb, reader, "other_approval_details");

                        sb.Append("\r\n");
                    }
                }

                // 2) Export "activities" (wtb_ssp_roadworkactivities)
                await using (var pgConn = new NpgsqlConnection(AppConfig.connectionString))
                {
                    await pgConn.OpenAsync();

                    await using var selectComm = pgConn.CreateCommand();
                    selectComm.CommandText = @"
                        SELECT r.uuid, r.roadworkactivity_no, r.name, r.is_minor_work,
                            p.first_name AS p_first_name, p.last_name AS p_last_name,
                            t.first_name AS t_first_name, t.last_name AS t_last_name,
                            appr.bafu_approval_required, appr.lsv_approval_required,
                            r.description, r.created, r.last_modified,
                            r.date_from, r.date_to, r.private, r.status,
                            r.in_internet, r.billing_address1, r.billing_address2,
                            r.pdb_fid, r.strabako_no, r.investment_no, r.date_sks,
                            r.date_kap, r.date_oks, r.date_gl_tba, 
                            r.comment, r.section, r.url, r.working_title, r.projecttype, r.projectkind,
                            r.overarching_measure, r.desired_year_from,
                            r.desired_year_to, r.prestudy, r.prestudy_sks, r.is_traffic_regulation_required, r.date_optimum,
                            r.start_of_construction, r.end_of_construction,
                            r.consult_due, r.date_sks_real,
                            r.date_kap_real, r.date_oks_real, r.date_gl_tba_real,
                            r.date_planned, r.date_accept, r.date_guarantee,
                            r.is_study, r.date_study_start, r.date_study_end,
                            r.project_study_approved, r.study_approved,
                            r.is_particip, r.date_particip_start, r.date_particip_end,
                            r.is_plan_circ, r.date_plan_circ_start, r.date_plan_circ_end,
                            r.part_of_aggloprogram, r.private_entity_affected, r.erp_number,
                            r.planned_tasks, r.constraints_dependencies, r.acquisition_planned,
                            r.implementation_by_third,
                            r.date_consult_start1, r.date_consult_end1,
                            r.date_consult_start2, r.date_consult_end2,
                            r.date_report_start, r.date_report_end,                            
                            r.is_aggloprog, r.date_start_inconsult1, r.date_start_verified1,
                            r.date_start_inconsult2, r.date_start_verified2,
                            r.date_start_reporting, r.date_start_suspended,
                            r.date_start_coordinated, r.oks_active_last_modified,
                            -- Dates
                            r.date_design_assignment_issued, r.date_apr_design_completion, r.date_apr_construction_completion,
                            r.date_quotes_requested, r.date_quotes_reviewed, r.date_prepare_edc_start,
                            r.date_prepare_edc_end, r.date_handover_to_apk, r.date_handover_to_apr,
                            r.date_request_design_budget, r.date_project_approval_start, r.date_project_approval_end, 
                            r.date_construction_budget_approval_start, r.date_construction_budget_approval_end, r.date_submission_start,
                            r.date_submission_end, r.date_start_of_construction_real, r.date_end_of_construction_real,
                            r.date_final_pavement_start, r.date_final_pavement_end, r.date_project_budget_finalized,
                            r.road_condition, r.road_condition_date, r.road_condition_comment,

                            -- Additional scalar activity attributes and approvals.
                            r.projectmanager,
                            r.traffic_agent,
                            r.project_no,
                            r.type,
                            r.costs,
                            r.costs_type,
                            r.date_consult_close,
                            r.date_report_close,
                            r.oks_active,
                            r.costs_last_modified,
                            r.costs_last_modified_by,
                            r.date_sks_planned,
                            r.session_comment_1,
                            r.session_comment_2,
                            r.session_comment_3,
                            r.sks_no,
                            r.aggloprogram_link,
                            r.aggloprogram_generation,
                            r.aggloprogram_measure_number,
                            r.aggloprogram_are_code,
                            r.aggloprogram_are_description,
                            r.aggloprogram_due_date,
                            r.aggloprogram_cost_total,
                            r.aggloprogram_cost_canton,
                            r.aggloprogram_comment,
                            r.bus_stops_shelters_affected,
                            r.structures_affected,
                            r.roaddrainage_affected,
                            r.houseconnections_affected,
                            r.wastefacilities_affected,
                            r.technical_installations_affected,
                            r.trees_affected,
                            r.street_furniture_affected,
                            r.urban_climate_affected,
                            r.subject_to_depaving,
                            r.pedestrians_cycling_affected,
                            r.disability_equality_affected,
                            r.private_entity_extent,
                            r.private_entity_requirements,
                            r.private_entity_acquisition,
                            r.private_entity_is_initiator,
                            r.date_staff_resources_apr_confirmed,
                            r.date_cost_estimate_apr_confirmed,
                            r.core_drilling_contracted,
                            r.quotes_requested,
                            r.quotes_reviewed,
                            r.afm_checked,
                            r.pre_reviewed_by_cf,
                            r.pre_reviewed_by_rd,
                            r.formally_approved,
                            r.filed_in_fabasoft,
                            r.gis_updated,
                            r.prestudy_duration,
                            r.prestudy_contractor,
                            r.prestudy_detail,
                            r.date_prestudy_vk_er_confirmed,
                            r.prestudy_vk_er_number,
                            r.status_before_suspended,
                            r.comment_start_suspended,
                            r.date_core_drilling_contracted,
                            r.date_afm_checked,
                            r.date_pre_reviewed_by_cf,
                            r.date_pre_reviewed_by_rd,
                            r.date_formally_approved,
                            r.date_filed_in_fabasoft,
                            r.date_gis_updated,
                            appr.uuid AS uuid_approvals,
                            appr.approval_required,
                            appr.strg_approval_required,
                            appr.ssv_approval_required,
                            appr.wwg_approval_required,
                            appr.eri_approval_required,
                            appr.pbg_approval_required,
                            appr.ebg_approval_required,
                            appr.awel_approval_required,
                            appr.esti_approval_required,
                            appr.other_approval_required,
                            appr.other_approval_details,

                            gm.gm_first_name,
                            gm.gm_last_name,

                            primary_need.triggering_first_name,
                            primary_need.triggering_last_name,
                            primary_need.triggering_org_abbr,

                            involved.involved_orgs,
                            needs.needs_uuids

                        FROM ""wtb_ssp_roadworkactivities"" r
                        LEFT JOIN ""wtb_ssp_users"" p ON r.projectmanager = p.uuid
                        LEFT JOIN ""wtb_ssp_users"" t ON r.traffic_agent = t.uuid
                        LEFT JOIN ""wtb_ssp_roadwork_approvals"" appr ON r.uuid = appr.uuid_roadwork_activity

                        LEFT JOIN LATERAL (
                            SELECT am.first_name AS gm_first_name,
                                am.last_name AS gm_last_name
                            FROM ""wtb_ssp_managementareas"" m
                            LEFT JOIN ""wtb_ssp_users"" am ON m.manager = am.uuid
                            WHERE ST_Area(ST_Intersection(r.geom, m.geom)) > 0
                            ORDER BY ST_Area(ST_Intersection(r.geom, m.geom)) DESC
                            LIMIT 1
                        ) gm ON true

                        LEFT JOIN LATERAL (
                            SELECT u.first_name AS triggering_first_name,
                                u.last_name AS triggering_last_name,
                                o.abbreviation AS triggering_org_abbr
                            FROM ""wtb_ssp_activities_to_needs"" an
                            JOIN ""wtb_ssp_roadworkneeds"" n ON an.uuid_roadwork_need = n.uuid
                            LEFT JOIN ""wtb_ssp_users"" u ON n.orderer = u.uuid
                            LEFT JOIN ""wtb_ssp_organisationalunits"" o ON u.org_unit = o.uuid
                            WHERE an.uuid_roadwork_activity = r.uuid
                            AND an.is_primary = true
                            LIMIT 1
                        ) primary_need ON true

                        LEFT JOIN LATERAL (
                            SELECT string_agg(x.abbreviation, ', ' ORDER BY x.abbreviation) AS involved_orgs
                            FROM (
                                SELECT DISTINCT o.abbreviation
                                FROM ""wtb_ssp_activities_to_needs"" an
                                JOIN ""wtb_ssp_roadworkneeds"" n ON an.uuid_roadwork_need = n.uuid
                                LEFT JOIN ""wtb_ssp_users"" u ON n.orderer = u.uuid
                                LEFT JOIN ""wtb_ssp_organisationalunits"" o ON u.org_unit = o.uuid
                                WHERE an.uuid_roadwork_activity = r.uuid
                                AND an.activityrelationtype = 'assignedneed'
                                AND o.abbreviation IS NOT NULL
                            ) x
                        ) involved ON true

                        LEFT JOIN LATERAL (
                            SELECT string_agg(x.need_uuid, ', ' ORDER BY x.need_uuid) AS needs_uuids
                            FROM (
                                SELECT DISTINCT n.uuid::text AS need_uuid
                                FROM ""wtb_ssp_activities_to_needs"" an
                                JOIN ""wtb_ssp_roadworkneeds"" n ON an.uuid_roadwork_need = n.uuid
                                WHERE an.uuid_roadwork_activity = r.uuid
                            ) x
                        ) needs ON true";

                    await using var reader = await selectComm.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        var status = GetText(reader, "status");
                        var statusPhase = HelperFunctions.translateStatusCodes(status, onlyStatusName: false);
                        var statusName = HelperFunctions.translateStatusCodes(status, onlyStatusName: true);
                        AppendText(sb, statusPhase);
                        AppendText(sb, statusName);

                        AppendText(sb, reader, "roadworkactivity_no");
                        AppendText(sb, reader, "name");
                        AppendText(sb, reader, "section");
                        AppendText(sb, reader, "comment");
                        AppendText(sb, $"{GetText(reader, "gm_first_name")} {GetText(reader, "gm_last_name")}".Trim());
                        AppendText(sb, $"{GetText(reader, "triggering_first_name")} {GetText(reader, "triggering_last_name")}".Trim());
                        AppendText(sb, reader, "triggering_org_abbr");
                        AppendText(sb, reader, "involved_orgs");

                        AppendText(sb, reader, "p_first_name");
                        AppendText(sb, reader, "p_last_name");
                        AppendText(sb, reader, "t_first_name");
                        AppendText(sb, reader, "t_last_name");

                        AppendText(sb, reader, "description");

                        AppendDate(sb, reader, "created");
                        AppendDate(sb, reader, "last_modified");
                        //AppendDate(sb, reader, "date_from");
                        //AppendDate(sb, reader, "date_to");

                        AppendBool(sb, reader, "private");

                        AppendBool(sb, reader, "in_internet");

                        AppendText(sb, reader, "billing_address1");
                        AppendText(sb, reader, "billing_address2");

                        AppendInt(sb, reader, "pdb_fid");
                        AppendInt(sb, reader, "investment_no");

                        AppendDate(sb, reader, "date_sks");
                        AppendDate(sb, reader, "date_kap");
                        AppendDate(sb, reader, "date_oks");                        

                        AppendText(sb, reader, "url");
                        AppendText(sb, reader, "working_title");
                        var projectType = HelperFunctions.translateType(GetText(reader, "projecttype"));
                        AppendText(sb, projectType);
                        var projectKind = HelperFunctions.translateProjectKind(GetText(reader, "projectkind"));
                        AppendText(sb, projectKind);

                        AppendBool(sb, reader, "overarching_measure");
                        AppendInt(sb, reader, "desired_year_from");
                        AppendInt(sb, reader, "desired_year_to");

                        AppendBool(sb, reader, "prestudy");
                        AppendBool(sb, reader, "prestudy_sks");
                        AppendBool(sb, reader, "is_traffic_regulation_required");

                        AppendDate(sb, reader, "date_optimum");
                        AppendDate(sb, reader, "start_of_construction");
                        AppendDate(sb, reader, "end_of_construction");
                        // removed in #650
                        //AppendDate(sb, reader, "date_of_acceptance");
                        AppendDate(sb, reader, "consult_due");

                        AppendDate(sb, reader, "date_sks_real");
                        AppendDate(sb, reader, "date_kap_real");                        
                        AppendDate(sb, reader, "date_gl_tba_real");

                        AppendDate(sb, reader, "date_planned");
                        AppendDate(sb, reader, "date_accept");
                        AppendDate(sb, reader, "date_guarantee");

                        AppendBool(sb, reader, "is_study");
                        AppendDate(sb, reader, "date_study_start");
                        AppendDate(sb, reader, "date_study_end");
                        AppendDate(sb, reader, "project_study_approved");
                        AppendDate(sb, reader, "study_approved");

                        // IMPORTANT FIX:
                        // Previously: "False;;;" caused column shift -> dates ended up under wrong headers in Excel import.
                        // removed in #650
                        //AppendBool(sb, reader, "is_desire");
                        //AppendDate(sb, reader, "date_desire_start");
                        //AppendDate(sb, reader, "date_desire_end");

                        AppendBool(sb, reader, "is_particip");
                        AppendDate(sb, reader, "date_particip_start");
                        AppendDate(sb, reader, "date_particip_end");

                        AppendBool(sb, reader, "is_plan_circ");
                        AppendDate(sb, reader, "date_plan_circ_start");
                        AppendDate(sb, reader, "date_plan_circ_end");

                        AppendBool(sb, reader, "bafu_approval_required");
                        AppendBool(sb, reader, "lsv_approval_required");

                        AppendBool(sb, reader, "part_of_aggloprogram");
                        AppendBool(sb, reader, "private_entity_affected");
                        AppendText(sb, reader, "erp_number");
                        AppendText(sb, reader, "planned_tasks");
                        AppendText(sb, reader, "constraints_dependencies");
                        AppendText(sb, reader, "acquisition_planned");
                        AppendBool(sb, reader, "implementation_by_third");

                        // Vernehmlassungen / consult rounds and Stellungnahme (make columns explicit)
                        AppendDate(sb, reader, "date_consult_start1"); // Bedarfsklärung 1 Start
                        AppendDate(sb, reader, "date_consult_end1");   // Bedarfsklärung 1 Ende
                        AppendDate(sb, reader, "date_consult_start2"); // Bedarfsklärung 2 Start
                        AppendDate(sb, reader, "date_consult_end2");   // Bedarfsklärung 2 Ende
                        AppendDate(sb, reader, "date_report_start");   // Stellungnahme Start
                        AppendDate(sb, reader, "date_report_end");     // Stellungnahme Ende                        

                        // Aggloprogramm + status flags at the end
                        AppendBool(sb, reader, "is_aggloprog");
                        AppendDate(sb, reader, "date_start_inconsult1");
                        AppendDate(sb, reader, "date_start_verified1");
                        AppendDate(sb, reader, "date_start_inconsult2");
                        AppendDate(sb, reader, "date_start_verified2");
                        AppendDate(sb, reader, "date_start_reporting");
                        AppendDate(sb, reader, "date_start_suspended");
                        AppendDate(sb, reader, "date_start_coordinated");

                        // Dates
                        AppendDate(sb, reader, "oks_active_last_modified");
                        AppendDate(sb, reader, "date_design_assignment_issued");
                        AppendDate(sb, reader, "date_apr_design_completion");
                        AppendDate(sb, reader, "date_apr_construction_completion");
                        AppendDate(sb, reader, "date_quotes_requested");
                        AppendDate(sb, reader, "date_quotes_reviewed");
                        AppendDate(sb, reader, "date_prepare_edc_start");
                        AppendDate(sb, reader, "date_prepare_edc_end");
                        AppendDate(sb, reader, "date_handover_to_apk");
                        AppendDate(sb, reader, "date_handover_to_apr");
                        AppendDate(sb, reader, "date_request_design_budget");
                        AppendDate(sb, reader, "date_project_approval_start");
                        AppendDate(sb, reader, "date_project_approval_end");
                        AppendDate(sb, reader, "date_construction_budget_approval_start");
                        AppendDate(sb, reader, "date_construction_budget_approval_end");
                        AppendDate(sb, reader, "date_submission_start");
                        AppendDate(sb, reader, "date_submission_end");
                        AppendDate(sb, reader, "date_start_of_construction_real");
                        AppendDate(sb, reader, "date_end_of_construction_real");
                        AppendDate(sb, reader, "date_final_pavement_start");
                        AppendDate(sb, reader, "date_final_pavement_end");
                        AppendDate(sb, reader, "date_project_budget_finalized");

                        AppendInt(sb, reader, "road_condition");
                        AppendDate(sb, reader, "road_condition_date");
                        AppendText(sb, reader, "road_condition_comment");

                        // Is minor work (Kleinmassnahme)
                        AppendBool(sb, reader, "is_minor_work");

                        // Uuids of activities and needs
                        AppendGuid(sb, reader, "uuid");
                        AppendText(sb, reader, "needs_uuids");

                        // Additional scalar activity attributes.
                        AppendGuid(sb, reader, "projectmanager");
                        AppendGuid(sb, reader, "traffic_agent");
                        AppendText(sb, reader, "project_no");
                        AppendText(sb, reader, "type");
                        AppendDate(sb, reader, "date_from");
                        AppendDate(sb, reader, "date_to");
                        AppendDecimal(sb, reader, "costs");
                        AppendText(sb, reader, "costs_type");
                        AppendText(sb, reader, "strabako_no");
                        AppendDate(sb, reader, "date_oks_real");
                        AppendDate(sb, reader, "date_gl_tba");
                        AppendDate(sb, reader, "date_consult_close");
                        AppendDate(sb, reader, "date_report_close");
                        AppendBool(sb, reader, "oks_active");
                        AppendTimestamp(sb, reader, "costs_last_modified");
                        AppendGuid(sb, reader, "costs_last_modified_by");
                        AppendDate(sb, reader, "date_sks_planned");
                        AppendText(sb, reader, "session_comment_1");
                        AppendText(sb, reader, "session_comment_2");
                        AppendText(sb, reader, "session_comment_3");
                        AppendInt(sb, reader, "sks_no");
                        AppendText(sb, reader, "aggloprogram_link");
                        AppendInt(sb, reader, "aggloprogram_generation");
                        AppendText(sb, reader, "aggloprogram_measure_number");
                        AppendText(sb, reader, "aggloprogram_are_code");
                        AppendText(sb, reader, "aggloprogram_are_description");
                        AppendDate(sb, reader, "aggloprogram_due_date");
                        AppendDecimal(sb, reader, "aggloprogram_cost_total");
                        AppendDecimal(sb, reader, "aggloprogram_cost_canton");
                        AppendText(sb, reader, "aggloprogram_comment");
                        AppendBool(sb, reader, "bus_stops_shelters_affected");
                        AppendBool(sb, reader, "structures_affected");
                        AppendBool(sb, reader, "roaddrainage_affected");
                        AppendBool(sb, reader, "houseconnections_affected");
                        AppendBool(sb, reader, "wastefacilities_affected");
                        AppendBool(sb, reader, "technical_installations_affected");
                        AppendBool(sb, reader, "trees_affected");
                        AppendBool(sb, reader, "street_furniture_affected");
                        AppendBool(sb, reader, "urban_climate_affected");
                        AppendBool(sb, reader, "subject_to_depaving");
                        AppendBool(sb, reader, "pedestrians_cycling_affected");
                        AppendBool(sb, reader, "disability_equality_affected");
                        AppendText(sb, reader, "private_entity_extent");
                        AppendText(sb, reader, "private_entity_requirements");
                        AppendBool(sb, reader, "private_entity_acquisition");
                        AppendBool(sb, reader, "private_entity_is_initiator");
                        AppendDate(sb, reader, "date_staff_resources_apr_confirmed");
                        AppendDate(sb, reader, "date_cost_estimate_apr_confirmed");
                        AppendBool(sb, reader, "core_drilling_contracted");
                        AppendBool(sb, reader, "quotes_requested");
                        AppendBool(sb, reader, "quotes_reviewed");
                        AppendBool(sb, reader, "afm_checked");
                        AppendBool(sb, reader, "pre_reviewed_by_cf");
                        AppendBool(sb, reader, "pre_reviewed_by_rd");
                        AppendBool(sb, reader, "formally_approved");
                        AppendBool(sb, reader, "filed_in_fabasoft");
                        AppendBool(sb, reader, "gis_updated");
                        AppendText(sb, reader, "prestudy_duration");
                        AppendText(sb, reader, "prestudy_contractor");
                        AppendText(sb, reader, "prestudy_detail");
                        AppendDate(sb, reader, "date_prestudy_vk_er_confirmed");
                        AppendInt(sb, reader, "prestudy_vk_er_number");
                        AppendText(sb, reader, "status_before_suspended");
                        AppendText(sb, reader, "comment_start_suspended");
                        AppendDate(sb, reader, "date_core_drilling_contracted");
                        AppendDate(sb, reader, "date_afm_checked");
                        AppendDate(sb, reader, "date_pre_reviewed_by_cf");
                        AppendDate(sb, reader, "date_pre_reviewed_by_rd");
                        AppendDate(sb, reader, "date_formally_approved");
                        AppendDate(sb, reader, "date_filed_in_fabasoft");
                        AppendDate(sb, reader, "date_gis_updated");

                        // Additional need attributes are empty for an activity row.
                        AppendEmpty(sb, 48);

                        // Approval attributes shared by both record kinds.
                        AppendGuid(sb, reader, "uuid_approvals");
                        AppendBool(sb, reader, "approval_required");
                        AppendBool(sb, reader, "strg_approval_required");
                        AppendBool(sb, reader, "ssv_approval_required");
                        AppendBool(sb, reader, "wwg_approval_required");
                        AppendBool(sb, reader, "eri_approval_required");
                        AppendBool(sb, reader, "pbg_approval_required");
                        AppendBool(sb, reader, "ebg_approval_required");
                        AppendBool(sb, reader, "awel_approval_required");
                        AppendBool(sb, reader, "esti_approval_required");
                        AppendBool(sb, reader, "other_approval_required");
                        AppendText(sb, reader, "other_approval_details");

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
            sb.Append(reader.IsDBNull(reader.GetOrdinal(column)) ? "" : Convert.ToString(reader.GetValue(reader.GetOrdinal(column)), CultureInfo.InvariantCulture));
            sb.Append(';');
        }

        private static void AppendDecimal(StringBuilder sb, NpgsqlDataReader reader, string column)
        {
            sb.Append(reader.IsDBNull(reader.GetOrdinal(column)) ? "" : reader.GetDecimal(reader.GetOrdinal(column)).ToString(CultureInfo.InvariantCulture));
            sb.Append(';');
        }

        private static void AppendTimestamp(StringBuilder sb, NpgsqlDataReader reader, string column)
        {
            if (!reader.IsDBNull(reader.GetOrdinal(column)))
                sb.Append(reader.GetDateTime(reader.GetOrdinal(column)).ToString("dd.MM.yyyy HH:mm:ss.fffffff", CultureInfo.InvariantCulture));
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

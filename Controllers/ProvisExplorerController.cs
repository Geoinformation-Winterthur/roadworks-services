using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Npgsql;
using roadwork_portal_service.Configuration;
using System.Data;

namespace roadwork_portal_service.Controllers;

// Read-only explorer. It never creates or modifies records in WiKIS or PROVIS.
[ApiController]
[Authorize]
[Route("api/provis-explorer")]
public sealed class ProvisExplorerController : ControllerBase
{
    private const string ReferenceCte = @"
WITH imported AS (
    SELECT i.excel_row, i.excel_key,
      CASE WHEN nullif(btrim(i.r),'') IS NULL OR btrim(i.r)='-'
           THEN 'EXCEL_ROW_'||i.excel_row::text ELSE btrim(i.r) END AS bv_nr,
      i.g, i.n, i.o, i.i AS excel_title, i.h AS excel_status, i.q AS excel_url
    FROM wikis.wikis_import_excel i
), excel_references AS (
    SELECT i.excel_row, i.bv_nr, v.source_column, btrim(part.token) AS reference_number
    FROM imported i
    CROSS JOIN LATERAL (VALUES ('G',i.g),('N',i.n),('O',i.o)) v(source_column,raw_value)
    CROSS JOIN LATERAL regexp_split_to_table(coalesce(v.raw_value,''), '[,;' || chr(10) || chr(13) || ']+') part(token)
    WHERE btrim(part.token) <> ''
      AND lower(btrim(part.token)) NOT IN ('-', '--', 'tbd', 'aew')
      AND length(btrim(part.token)) <= 80
), resolved AS (
    SELECT DISTINCT r.excel_row, r.bv_nr, r.source_column, r.reference_number,
       p.id AS provis_id, p.projekt_nr, p.name AS provis_name,
       p.bis_nr, p.status AS provis_status, p.kreditsumme, p.geom_multipoint,
       CASE WHEN btrim(p.projekt_nr) = r.reference_number THEN 'EXACT'
            WHEN p.projekt_nr IS NOT NULL THEN 'SUBPROJECT' ELSE 'NO_MATCH' END AS match_kind
    FROM excel_references r
    LEFT JOIN wikis.wikis_provis_projects_with_geoms p
      ON (btrim(p.projekt_nr) = r.reference_number
          OR left(btrim(p.projekt_nr), length(r.reference_number) + 1) = r.reference_number || '-')
)";

    private readonly ILogger<ProvisExplorerController> _logger;
    public ProvisExplorerController(ILogger<ProvisExplorerController> logger) { _logger = logger; }

    [HttpGet("activities")]
    public IActionResult GetActivities()
    {
        const string sql = ReferenceCte + @",
ref_summary AS (
 SELECT bv_nr,
   count(DISTINCT (source_column,reference_number)) AS reference_count,
   count(DISTINCT (source_column,reference_number)) FILTER (WHERE provis_id IS NOT NULL) AS matched_reference_count,
   count(DISTINCT projekt_nr) FILTER (WHERE projekt_nr IS NOT NULL) AS provis_project_count
 FROM resolved GROUP BY bv_nr
), need_summary AS (
 SELECT rel.uuid_roadwork_activity AS activity_uuid,
   count(DISTINCT rel.uuid_roadwork_need) AS need_count
 FROM wikis.wtb_ssp_activities_to_needs rel GROUP BY rel.uuid_roadwork_activity
), cost_summary AS (
 SELECT rel.uuid_roadwork_activity AS activity_uuid,
   sum(coalesce(c.costs,0)) AS costs_total
 FROM wikis.wtb_ssp_activities_to_needs rel
 LEFT JOIN wikis.wtb_ssp_costs c ON c.roadworkneed=rel.uuid_roadwork_need
 GROUP BY rel.uuid_roadwork_activity
)
SELECT a.uuid::text AS uuid, a.roadworkactivity_no AS bv_nr, a.name, a.status,
 i.excel_row, coalesce(s.reference_count,0) AS reference_count,
 coalesce(s.matched_reference_count,0) AS matched_reference_count,
 coalesce(s.provis_project_count,0) AS provis_project_count,
 coalesce(ns.need_count,0) AS need_count,
 coalesce(cs.costs_total,0) AS costs_total,
 CASE WHEN i.excel_row IS NULL THEN 'NO_EXCEL'
      WHEN coalesce(s.reference_count,0)=0 THEN 'NO_REFERENCE'
      WHEN coalesce(s.matched_reference_count,0)=0 THEN 'NO_MATCH'
      WHEN s.matched_reference_count < s.reference_count THEN 'PARTIAL'
      WHEN s.provis_project_count > 1 THEN 'MULTIPLE'
      ELSE 'MATCH' END AS quality
FROM wikis.wtb_ssp_roadworkactivities a
LEFT JOIN imported i ON i.bv_nr=btrim(a.roadworkactivity_no)
LEFT JOIN ref_summary s ON s.bv_nr=i.bv_nr
LEFT JOIN need_summary ns ON ns.activity_uuid=a.uuid
LEFT JOIN cost_summary cs ON cs.activity_uuid=a.uuid
ORDER BY a.roadworkactivity_no NULLS LAST, a.name;";
        try { return Ok(Execute(sql)); }
        catch (Exception ex) { _logger.LogError(ex, "PROVIS activity list failed"); return Problem("PROVIS analysis could not be loaded."); }
    }

    [HttpGet("activities/{id:guid}")]
    public IActionResult GetActivity(Guid id)
    {
        try
        {
            var p = new NpgsqlParameter("id", NpgsqlTypes.NpgsqlDbType.Uuid) { Value = id };
            const string actSql = @"SELECT uuid::text, roadworkactivity_no AS bv_nr, name, status,
                project_no, erp_number, pdb_fid, description, costs, url,
                CASE WHEN geom IS NOT NULL AND postgis.ST_SRID(geom)=2056
                  THEN postgis.ST_AsGeoJSON(postgis.ST_Transform(geom,4326)) ELSE NULL END AS geojson
                FROM wikis.wtb_ssp_roadworkactivities WHERE uuid=@id";
            var activityRows = Execute(actSql, p);
            if (activityRows.Count == 0) return NotFound();
            string refsSql = ReferenceCte + @"
SELECT r.excel_row, r.source_column, r.reference_number,
   CASE WHEN r.projekt_nr IS NOT NULL THEN 'MATCH' ELSE 'NO_MATCH' END AS match_status,
   r.projekt_nr, r.provis_id, r.provis_name, r.bis_nr, r.provis_status,
   r.kreditsumme, r.match_kind,
   CASE WHEN r.geom_multipoint IS NOT NULL AND postgis.ST_SRID(r.geom_multipoint)=2056
     THEN postgis.ST_AsGeoJSON(postgis.ST_Transform(r.geom_multipoint,4326)) ELSE NULL END AS geojson
FROM resolved r
JOIN wikis.wtb_ssp_roadworkactivities a ON btrim(a.roadworkactivity_no)=r.bv_nr
WHERE a.uuid=@id ORDER BY r.source_column, r.reference_number, r.projekt_nr";
            const string needsSql = @"SELECT n.uuid::text AS uuid, rel.activityrelationtype,
                rel.is_primary, n.name, n.status,
                CASE WHEN n.geom IS NOT NULL AND postgis.ST_SRID(n.geom)=2056
                  THEN postgis.ST_AsGeoJSON(postgis.ST_Transform(n.geom,4326)) ELSE NULL END AS geojson
                FROM wikis.wtb_ssp_activities_to_needs rel
                JOIN wikis.wtb_ssp_roadworkneeds n ON n.uuid=rel.uuid_roadwork_need
                WHERE rel.uuid_roadwork_activity=@id ORDER BY n.name";
            const string costsSql = @"SELECT c.uuid::text AS uuid, c.roadworkneed::text AS need_uuid,
                c.project_type, c.erp_number, c.costs, c.costs_comment
                FROM wikis.wtb_ssp_activities_to_needs rel
                JOIN wikis.wtb_ssp_costs c ON c.roadworkneed=rel.uuid_roadwork_need
                WHERE rel.uuid_roadwork_activity=@id ORDER BY c.project_type, c.erp_number";
            const string journalSql = @"SELECT uuid::text, content, created, last_modified
                FROM wikis.wtb_ssp_journal_entries WHERE uuid_roadwork_activity=@id
                ORDER BY sort_order DESC NULLS LAST LIMIT 200";
            const string compareSql = @"SELECT i.excel_row, i.i AS excel_title, i.h AS excel_status,
                i.g AS provis_g, i.n AS provis_n, i.o AS provis_o, i.q AS excel_url,
                i.d AS pdb_requested, i.e AS pdb_project_id
                FROM wikis.wikis_import_excel i
                JOIN wikis.wtb_ssp_roadworkactivities a ON a.roadworkactivity_no=
                  CASE WHEN nullif(btrim(i.r),'') IS NULL OR btrim(i.r)='-'
                       THEN 'EXCEL_ROW_'||i.excel_row::text ELSE btrim(i.r) END
                WHERE a.uuid=@id";
            // Match PDB using Excel columns D (use PDB) and E (PDB project ID).
            // Resolve duplicate PDB rows deterministically by row_no; no geometry is written.
            const string pdbSql = @"SELECT p.projekt_id, p.projektbezeichnung,
                p.projektbereich, p.projektobjekt, p.verkehrsbehinderung,
                p.statusbezeichnung, p.projektbemerkung, p.massnahmen
                FROM wikis.wikis_import_excel i
                JOIN wikis.wtb_ssp_roadworkactivities a ON a.roadworkactivity_no =
                  CASE WHEN nullif(btrim(i.r),'') IS NULL OR btrim(i.r)='-'
                       THEN 'EXCEL_ROW_'||i.excel_row::text ELSE btrim(i.r) END
                JOIN LATERAL (
                  SELECT p.projekt_id, p.projektbezeichnung, p.projektbereich,
                    p.projektobjekt, p.verkehrsbehinderung, p.statusbezeichnung,
                    p.projektbemerkung, p.massnahmen
                  FROM wikis.wikis_pdb_projects p
                  WHERE p.projekt_id::text = btrim(i.e)
                  ORDER BY p.row_no
                  LIMIT 1
                ) p ON true
                WHERE a.uuid=@id AND lower(btrim(i.d))='ja'
                ORDER BY i.excel_row LIMIT 1";
            return Ok(new {
                activity = activityRows[0],
                provis = Execute(refsSql, p),
                needs = Execute(needsSql, p),
                costs = Execute(costsSql, p),
                journal = Execute(journalSql, p),
                excel = Execute(compareSql, p),
                pdb = Execute(pdbSql, p)
            });
        }
        catch (Exception ex) { _logger.LogError(ex, "PROVIS detail failed for {ActivityId}", id); return Problem("PROVIS detail could not be loaded."); }
    }

    private static List<Dictionary<string, object?>> Execute(string sql, NpgsqlParameter? parameter = null)
    {
        var result = new List<Dictionary<string, object?>>();
        using var conn = new NpgsqlConnection(AppConfig.connectionString);
        conn.Open();
        using var cmd = new NpgsqlCommand("SET search_path TO wikis, postgis, public; SET default_transaction_read_only = on", conn);
        cmd.ExecuteNonQuery();
        using var read = new NpgsqlCommand(sql, conn) { CommandTimeout = 40 };
        if (parameter != null) read.Parameters.Add(new NpgsqlParameter(parameter.ParameterName, parameter.NpgsqlDbType) { Value=parameter.Value });
        using var reader = read.ExecuteReader(CommandBehavior.SequentialAccess);
        while (reader.Read())
        {
            var row = new Dictionary<string, object?>();
            for (var k=0; k<reader.FieldCount; k++) row[reader.GetName(k)] = reader.IsDBNull(k) ? null : reader.GetValue(k);
            result.Add(row);
        }
        return result;
    }
}

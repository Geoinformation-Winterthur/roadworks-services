-- VERSION 2026.16 ---

-- Treat Provis numbers as text to preserve values such as 000_07240 (watson-dr, 03.11.2026).
ALTER TABLE wikis.wtb_ssp_roadworkactivities
    ALTER COLUMN erp_number TYPE text
    USING erp_number::text;

-- #618 - Add provis (ERP) to needs (GEOBOX AG - Simon Meyer, 04.11.2026)
ALTER TABLE IF EXISTS wtb_ssp_costs ADD COLUMN erp_number varchar(40);

-- #693, #693 - Change construction duration from months to days (GEOBOX AG - Simon Meyer, 04.11.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkneeds RENAME construction_duration TO construction_duration_days;
UPDATE wtb_ssp_roadworkneeds SET construction_duration_days = construction_duration_days * 30;

-- #704 - Add road condition (GEOBOX AG - Simon Meyer, 09.11.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN road_condition  numeric(1,0);
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN road_condition_date date;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN road_condition_comment text;

-- #718 - Add road condition (GEOBOX AG - Simon Meyer, 10.11.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN session_comment_3 text;
-- set initial value
UPDATE wtb_ssp_roadworkactivities SET session_comment_3 = 'Keine Informationen' WHERE session_comment_3 IS NULL OR session_comment_3 = '';
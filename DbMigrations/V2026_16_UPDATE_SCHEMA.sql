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

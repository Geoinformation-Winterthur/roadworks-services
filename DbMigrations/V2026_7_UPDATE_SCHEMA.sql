-- VERSION 2026.7 ---

-- #662 - Unify prestudy: prestudy & prestudy_required >> prestudy (GEOBOX AG - Simon Meyer, 22.06.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities DROP COLUMN IF EXISTS prestudy_required;

-- #663 - Rename traffic regulation: traffic_regulation_affected >> traffic_regulation (GEOBOX AG - Simon Meyer, 22.06.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities DROP COLUMN IF EXISTS traffic_regulation_affected;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities DROP COLUMN IF EXISTS traffic_regulation;
--ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities RENAME traffic_regulation_affected TO traffic_regulation;

-- #667 - Rename sks relevant: sks_relevant >> oks_active (GEOBOX AG - Simon Meyer, 22.06.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities RENAME sks_relevant TO oks_active;

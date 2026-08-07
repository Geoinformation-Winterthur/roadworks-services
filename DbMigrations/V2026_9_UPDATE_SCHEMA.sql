-- VERSION 2026.9 ---

-- #663 - Split prestudy: Sitzungen (SKS) and Journal (GEOBOX AG - Simon Meyer, 10.07.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN prestudy_sks boolean;
UPDATE wtb_ssp_roadworkactivities SET prestudy_sks = prestudy;

-- #667 - Add oks active modification date (GEOBOX AG - Simon Meyer, 10.07.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN oks_active_last_modified date;

-- #650 Remove "Begehrensäusserung § 45" and "Infoversand" incl fields (GEOBOX AG - Simon Meyer, 06.08.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities DROP COLUMN IF EXISTS is_desire;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities DROP COLUMN IF EXISTS date_desire_start;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities DROP COLUMN IF EXISTS date_desire_end;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities DROP COLUMN IF EXISTS date_info_start;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities DROP COLUMN IF EXISTS date_info_end;
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities DROP COLUMN IF EXISTS date_info_close;

-- #650 Consolidate date_of_acceptance and date_guarantee to date_guarantee (GEOBOX AG - Simon Meyer, 07.08.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities DROP COLUMN IF EXISTS date_of_acceptance;
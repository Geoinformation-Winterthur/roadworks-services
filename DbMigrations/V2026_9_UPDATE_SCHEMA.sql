-- VERSION 2026.9 ---

-- #663 - Split prestudy: Sitzungen (SKS) and Journal (GEOBOX AG - Simon Meyer, 10.07.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN prestudy_sks boolean;
UPDATE wtb_ssp_roadworkactivities SET prestudy_sks = prestudy;

-- #667 - Add oks active modification date (GEOBOX AG - Simon Meyer, 10.07.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN oks_active_last_modified date;
-- VERSION 2026.17 ---

-- NOTE: This version contains changes for minor works (Kleinmassnahmen).
-- This changes does not affect the current behaviour of WiKIS except some admin functions like the CSV export.

-- #706 / #708 - Add switch normal needs (Bedarfe) or small work (Kleinmassnahmen) (GEOBOX AG - Simon Meyer, 11.11.2026)
ALTER TABLE IF EXISTS wtb_ssp_roadworkactivities ADD COLUMN is_minor_work boolean NOT NULL DEFAULT false;
ALTER TABLE IF EXISTS wtb_ssp_roadworkneeds ADD COLUMN is_minor_work boolean NOT NULL DEFAULT false;
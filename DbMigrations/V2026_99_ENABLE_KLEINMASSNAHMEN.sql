-- VERSION 2026.XX ---

-- NOTE: Do not execute this script in production before minor works (Kleinmassnahmen) is finalized, tested and approved!
-- Needs to be executed at the same time with enabling minor works in environment.ts

-- #695 - Add project type K (GEOBOX AG - Simon Meyer, 10.11.2026)
INSERT INTO wtb_ssp_projecttypes (code, name, is_minor_work) VALUES ('type_k', 'Typ K', true);


-- Disable minor works (just in case and for testing purposes)
-- DELETE FROM wtb_ssp_projecttypes WHERE code = 'type_k';
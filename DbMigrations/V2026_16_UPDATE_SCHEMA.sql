-- VERSION 2026.16 ---

-- Treat Provis numbers as text to preserve values such as 000_07240 (watson-dr, 03.11.2026).
ALTER TABLE wikis.wtb_ssp_roadworkactivities
    ALTER COLUMN erp_number TYPE text
    USING erp_number::text;




-- 
 -- #674 GeoV: Neues Feld für Aggloprogramm (watson-dr)

ALTER TABLE "wtb_ssp_roadworkactivities"
    ADD COLUMN IF NOT EXISTS aggloprogram_measure_number VARCHAR(40),
    ADD COLUMN IF NOT EXISTS aggloprogram_comment VARCHAR(2048);

-- Keep existing behaviour consistent with frontend default values.
UPDATE "wtb_ssp_roadworkactivities"
SET aggloprogram_measure_number = ''
WHERE aggloprogram_measure_number IS NULL;

UPDATE "wtb_ssp_roadworkactivities"
SET aggloprogram_comment = ''
WHERE aggloprogram_comment IS NULL;

ALTER TABLE "wtb_ssp_roadworkactivities"
    ALTER COLUMN aggloprogram_measure_number SET DEFAULT '',
    ALTER COLUMN aggloprogram_measure_number SET NOT NULL,
    ALTER COLUMN aggloprogram_comment SET DEFAULT '',
    ALTER COLUMN aggloprogram_comment SET NOT NULL;
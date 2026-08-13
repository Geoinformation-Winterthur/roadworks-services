-- VERSION 2026.11 ---

-- #649 - Fix organisational unit for TBA  (GEOBOX AG - Simon Meyer, 13.08.2026)
UPDATE wtb_ssp_organisationalunits SET is_civil_eng = true WHERE abbreviation like 'AES';
UPDATE wtb_ssp_organisationalunits SET is_civil_eng = true WHERE abbreviation like 'AEW';
UPDATE wtb_ssp_organisationalunits SET is_civil_eng = true WHERE abbreviation like 'AMO';
UPDATE wtb_ssp_organisationalunits SET is_civil_eng = true WHERE abbreviation like 'APK';
UPDATE wtb_ssp_organisationalunits SET is_civil_eng = true WHERE abbreviation like 'APR';
UPDATE wtb_ssp_organisationalunits SET is_civil_eng = true WHERE abbreviation like 'ABU';
UPDATE wtb_ssp_organisationalunits SET is_civil_eng = true WHERE abbreviation like 'KuBa';

-- #667 - Set default value for renamed attribute oks_active (GEOBOX AG - Simon Meyer, 13.08.2026)
UPDATE wtb_ssp_roadworkactivities SET oks_active = false;
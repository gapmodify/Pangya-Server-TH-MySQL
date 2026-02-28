-- ========================================
-- Migration: Add Assist Item to All Players
-- Date: 2026-02-20
-- Description: Give all players the Assist item (TypeID 467664918)
-- ========================================

-- Insert Assist item for players who don't have it yet
-- C0 = 2 means Assist is OFF (default state)
INSERT INTO pangya_warehouse (UID, TYPEID, C0, VALID, ItemType, RegDate, DateEnd)
SELECT 
    p.UID,
    467664918 AS TYPEID,
    2 AS C0,  -- Default: OFF (Quantity = 2)
    1 AS VALID,
    0 AS ItemType,
    NOW() AS RegDate,
    NOW() AS DateEnd
FROM pangya_personal p
WHERE NOT EXISTS (
    SELECT 1 FROM pangya_warehouse w 
    WHERE w.UID = p.UID AND w.TYPEID = 467664918
);

-- Make sure AssistMode is synced with warehouse quantity
UPDATE pangya_personal p
INNER JOIN pangya_warehouse w ON p.UID = w.UID
SET p.AssistMode = CASE 
    WHEN w.C0 = 1 THEN 1  -- ON
    WHEN w.C0 = 2 THEN 0  -- OFF
    ELSE 0                 -- Default OFF
END
WHERE w.TYPEID = 467664918;

-- ========================================
-- Verification Query (check results)
-- ========================================
-- SELECT p.UID, p.AssistMode, w.C0 as AssistItemQty
-- FROM pangya_personal p
-- LEFT JOIN pangya_warehouse w ON p.UID = w.UID AND w.TYPEID = 467664918;

-- ========================================
-- END OF MIGRATION
-- ========================================

-- ========================================
-- Migration: Add AssistMode column to pangya_personal
-- Date: 2026-02-13
-- Description: Add AssistMode column for Assist toggle feature
-- ========================================

-- Add AssistMode column if not exists
ALTER TABLE `pangya_personal` 
ADD COLUMN IF NOT EXISTS `AssistMode` tinyint(3) unsigned DEFAULT '0' COMMENT 'Assist Mode: 0=OFF, 1=ON';

-- ========================================
-- END OF MIGRATION
-- ========================================

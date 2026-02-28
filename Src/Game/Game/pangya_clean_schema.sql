-- ========================================
-- PANGYA DATABASE - CLEAN SCHEMA
-- Generated: 2026-02-13
-- Description: Clean database structure with Daily Login data only
-- ========================================

SET SQL_MODE = "NO_AUTO_VALUE_ON_ZERO";
SET time_zone = "+00:00";

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!40101 SET NAMES utf8 */;

--
-- Database: `pangya`
--

-- --------------------------------------------------------
-- ACHIEVEMENT TABLES
-- --------------------------------------------------------

CREATE TABLE IF NOT EXISTS `achievement_counter_data` (
`ID` int(11) NOT NULL,
  `Enable` tinyint(3) unsigned NOT NULL,
  `Name` text COLLATE utf8mb4_bin NOT NULL,
  `TypeID` int(11) NOT NULL
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin AUTO_INCREMENT=1 ;

CREATE TABLE IF NOT EXISTS `achievement_data` (
`ID` int(11) NOT NULL,
  `ACHIEVEMENT_ENABLE` tinyint(3) unsigned NOT NULL,
  `ACHIEVEMENT_TYPEID` int(11) NOT NULL DEFAULT '0',
  `ACHIEVEMENT_NAME` text COLLATE utf8mb4_bin NOT NULL,
  `ACHIEVEMENT_QUEST_TYPEID` int(11) NOT NULL
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin AUTO_INCREMENT=1 ;

CREATE TABLE IF NOT EXISTS `achievement_questitem` (
`ID` int(11) NOT NULL,
  `TypeID` int(11) DEFAULT NULL,
  `Name` text COLLATE utf8mb4_bin,
  `QuestTypeID` int(11) DEFAULT NULL
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin AUTO_INCREMENT=1 ;

CREATE TABLE IF NOT EXISTS `achievement_queststuffs` (
`ID` int(11) NOT NULL,
  `Enable` tinyint(3) unsigned NOT NULL,
  `TypeID` int(11) NOT NULL,
  `Name` text COLLATE utf8mb4_bin NOT NULL,
  `CounterTypeID` int(11) NOT NULL,
  `CounterQuantity` int(11) NOT NULL
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin AUTO_INCREMENT=1 ;

CREATE TABLE IF NOT EXISTS `daily_quest` (
`ID` int(11) NOT NULL,
  `QuestTypeID1` int(11) NOT NULL,
  `QuestTypeID2` int(11) NOT NULL,
  `QuestTypeID3` int(11) NOT NULL,
  `RegDate` datetime(3) DEFAULT CURRENT_TIMESTAMP(3),
  `Day` tinyint(3) unsigned DEFAULT '0'
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin AUTO_INCREMENT=1 ;

CREATE TABLE IF NOT EXISTS `pangya_achievement` (
  `UID` int(11) NOT NULL,
  `TypeID` int(11) NOT NULL,
`ID` int(11) NOT NULL,
  `Type` tinyint(3) unsigned NOT NULL DEFAULT '3',
  `Valid` tinyint(3) unsigned DEFAULT '1'
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin AUTO_INCREMENT=1 ;

CREATE TABLE IF NOT EXISTS `pangya_achievement_counter` (
`ID` int(11) NOT NULL,
  `UID` int(11) NOT NULL,
  `TypeID` int(11) NOT NULL,
  `Quantity` int(11) NOT NULL
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin AUTO_INCREMENT=1 ;

CREATE TABLE IF NOT EXISTS `pangya_achievement_quest` (
`ID` int(11) NOT NULL,
  `UID` int(11) NOT NULL,
  `Achievement_Index` int(11) NOT NULL,
  `Achivement_Quest_TypeID` int(11) NOT NULL,
  `Counter_Index` int(11) NOT NULL,
  `SuccessDate` datetime(3) DEFAULT '0000-00-00 00:00:00.000',
  `Count` int(11) NOT NULL
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin AUTO_INCREMENT=1 ;

-- --------------------------------------------------------
-- PLAYER INVENTORY TABLES
-- --------------------------------------------------------
-- NOTE: pangya_club_info uses InnoDB engine to support Foreign Key constraints

CREATE TABLE IF NOT EXISTS `pangya_caddie` (
`CID` int(11) NOT NULL,
  `UID` int(11) NOT NULL DEFAULT '0',
  `TYPEID` int(11) NOT NULL DEFAULT '0',
  `EXP` int(11) NOT NULL DEFAULT '0',
  `cLevel` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `SKIN_TYPEID` int(11) DEFAULT '0',
  `RentFlag` tinyint(3) unsigned DEFAULT '0',
  `RegDate` datetime(3) DEFAULT CURRENT_TIMESTAMP(3),
  `END_DATE` datetime(3) DEFAULT NULL,
  `SKIN_END_DATE` datetime(3) DEFAULT NULL,
  `TriggerPay` tinyint(3) unsigned DEFAULT '0',
  `VALID` tinyint(3) unsigned NOT NULL DEFAULT '1'
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin AUTO_INCREMENT=1 ;

CREATE TABLE IF NOT EXISTS `pangya_card` (
`CARD_IDX` int(11) NOT NULL,
  `UID` int(11) NOT NULL,
  `CARD_TYPEID` int(11) NOT NULL,
  `QTY` int(11) NOT NULL,
  `RegData` datetime(3) DEFAULT CURRENT_TIMESTAMP(3),
  `VALID` tinyint(3) unsigned DEFAULT '1'
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin AUTO_INCREMENT=1 ;

CREATE TABLE IF NOT EXISTS `pangya_card_equip` (
`ID` int(11) NOT NULL,
  `UID` int(11) NOT NULL,
  `CID` int(11) NOT NULL DEFAULT '0',
  `CHAR_TYPEID` int(11) DEFAULT NULL,
  `CARD_TYPEID` int(11) DEFAULT NULL,
  `SLOT` int(11) DEFAULT NULL,
  `REGDATE` datetime DEFAULT NULL,
  `ENDDATE` datetime DEFAULT NULL,
  `FLAG` tinyint(3) unsigned DEFAULT '0',
  `VALID` tinyint(3) unsigned DEFAULT '1'
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin AUTO_INCREMENT=1 ;

CREATE TABLE IF NOT EXISTS `pangya_character` (
`CID` int(11) NOT NULL,
  `UID` int(11) NOT NULL,
  `TYPEID` int(11) NOT NULL,
  `GIFT_FLAG` tinyint(3) unsigned DEFAULT '0',
  `HAIR_COLOR` tinyint(3) unsigned DEFAULT '0',
  `POWER` tinyint(3) unsigned DEFAULT '0',
  `CONTROL` tinyint(3) unsigned DEFAULT '0',
  `IMPACT` tinyint(3) unsigned DEFAULT '0',
  `SPIN` tinyint(3) unsigned DEFAULT '0',
  `CURVE` tinyint(3) unsigned DEFAULT '0',
  `CUTIN` int(11) DEFAULT '0',
  `AuxPart` int(11) DEFAULT NULL,
  `AuxPart2` int(11) DEFAULT NULL,
  `PART_TYPEID_1` int(11) DEFAULT '0',
  `PART_TYPEID_2` int(11) DEFAULT '0',
  `PART_TYPEID_3` int(11) DEFAULT '0',
  `PART_TYPEID_4` int(11) DEFAULT '0',
  `PART_TYPEID_5` int(11) DEFAULT '0',
  `PART_TYPEID_6` int(11) DEFAULT '0',
  `PART_TYPEID_7` int(11) DEFAULT '0',
  `PART_TYPEID_8` int(11) DEFAULT '0',
  `PART_TYPEID_9` int(11) DEFAULT '0',
  `PART_TYPEID_10` int(11) DEFAULT '0',
  `PART_TYPEID_11` int(11) DEFAULT '0',
  `PART_TYPEID_12` int(11) DEFAULT '0',
  `PART_TYPEID_13` int(11) DEFAULT '0',
  `PART_TYPEID_14` int(11) DEFAULT '0',
  `PART_TYPEID_15` int(11) DEFAULT '0',
  `PART_TYPEID_16` int(11) DEFAULT '0',
  `PART_TYPEID_17` int(11) DEFAULT '0',
  `PART_TYPEID_18` int(11) DEFAULT '0',
  `PART_TYPEID_19` int(11) DEFAULT '0',
  `PART_TYPEID_20` int(11) DEFAULT '0',
  `PART_TYPEID_21` int(11) DEFAULT '0',
  `PART_TYPEID_22` int(11) DEFAULT '0',
  `PART_TYPEID_23` int(11) DEFAULT '0',
  `PART_TYPEID_24` int(11) DEFAULT '0',
  `PART_IDX_1` int(11) DEFAULT '0',
  `PART_IDX_2` int(11) DEFAULT '0',
  `PART_IDX_3` int(11) DEFAULT '0',
  `PART_IDX_4` int(11) DEFAULT '0',
  `PART_IDX_5` int(11) DEFAULT '0',
  `PART_IDX_6` int(11) DEFAULT '0',
  `PART_IDX_7` int(11) DEFAULT '0',
  `PART_IDX_8` int(11) DEFAULT '0',
  `PART_IDX_9` int(11) DEFAULT '0',
  `PART_IDX_10` int(11) DEFAULT '0',
  `PART_IDX_11` int(11) DEFAULT '0',
  `PART_IDX_12` int(11) DEFAULT '0',
  `PART_IDX_13` int(11) DEFAULT '0',
  `PART_IDX_14` int(11) DEFAULT '0',
  `PART_IDX_15` int(11) DEFAULT '0',
  `PART_IDX_16` int(11) DEFAULT '0',
  `PART_IDX_17` int(11) DEFAULT '0',
  `PART_IDX_18` int(11) DEFAULT '0',
  `PART_IDX_19` int(11) DEFAULT '0',
  `PART_IDX_20` int(11) DEFAULT '0',
  `PART_IDX_21` int(11) DEFAULT '0',
  `PART_IDX_22` int(11) DEFAULT '0',
  `PART_IDX_23` int(11) DEFAULT '0',
  `PART_IDX_24` int(11) DEFAULT '0'
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin AUTO_INCREMENT=1 ;

CREATE TABLE IF NOT EXISTS `pangya_club_info` (
  `ITEM_ID` int(11) NOT NULL,
  `UID` int(11) NOT NULL COMMENT 'Player UID (Owner of the club)',
  `TYPEID` int(11) NOT NULL DEFAULT '0' COMMENT 'Club TypeID from pangya_warehouse',
  `C0_SLOT` smallint(6) DEFAULT '0',
  `C1_SLOT` smallint(6) DEFAULT '0',
  `C2_SLOT` smallint(6) DEFAULT '0',
  `C3_SLOT` smallint(6) DEFAULT '0',
  `C4_SLOT` smallint(6) DEFAULT '0',
  `CLUB_POINT` int(11) DEFAULT '100000' COMMENT 'Auto-set to 100k for instant upgrade',
  `CLUB_WORK_COUNT` int(11) DEFAULT '0',
  `CLUB_SLOT_CANCEL` int(11) DEFAULT '0',
  `CLUB_POINT_TOTAL_LOG` int(11) DEFAULT '0',
  `CLUB_UPGRADE_PANG_LOG` int(11) DEFAULT '0'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `pangya_mascot` (
`MID` int(11) NOT NULL,
  `UID` int(11) NOT NULL,
  `MASCOT_TYPEID` int(11) NOT NULL,
  `MESSAGE` varchar(50) COLLATE utf8mb4_bin DEFAULT 'PANGYA!',
  `DateEnd` datetime(3) DEFAULT NULL,
  `VALID` tinyint(3) unsigned DEFAULT '1'
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin AUTO_INCREMENT=1 ;

CREATE TABLE IF NOT EXISTS `pangya_warehouse` (
  `item_id` int(11) NOT NULL AUTO_INCREMENT COMMENT 'Item ID (AUTO_INCREMENT per UID)',
  `UID` int(11) NOT NULL DEFAULT '0' COMMENT 'Player UID',
  `TYPEID` int(11) NOT NULL,
  `C0` smallint(6) DEFAULT '0',
  `C1` smallint(6) DEFAULT '0',
  `C2` smallint(6) DEFAULT '0',
  `C3` smallint(6) DEFAULT '0',
  `C4` smallint(6) DEFAULT '0',
  `RegDate` datetime(3) DEFAULT CURRENT_TIMESTAMP(3),
  `DateEnd` datetime(3) DEFAULT CURRENT_TIMESTAMP(3),
  `VALID` tinyint(3) unsigned NOT NULL DEFAULT '1',
  `ItemType` tinyint(3) unsigned DEFAULT '0',
  `Flag` tinyint(3) unsigned DEFAULT '0',
  PRIMARY KEY (`item_id`, `UID`),
  KEY `IX_Warehouse_UID` (`UID`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin AUTO_INCREMENT=1;

-- --------------------------------------------------------
-- DAILY QUEST TABLES
-- --------------------------------------------------------

CREATE TABLE IF NOT EXISTS `pangya_daily_quest` (
`ID` int(11) NOT NULL,
  `UID` int(11) NOT NULL DEFAULT '0',
  `QuestID1` int(11) DEFAULT '0',
  `QuestID2` int(11) DEFAULT '0',
  `QuestID3` int(11) DEFAULT '0',
  `LastAccept` datetime(3) DEFAULT NULL,
  `LastCancel` datetime(3) DEFAULT NULL,
  `RegDate` datetime(3) DEFAULT CURRENT_TIMESTAMP(3)
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin AUTO_INCREMENT=1 ;

-- ========================================
-- DAILY LOGIN REWARD DATA (PRESERVED)
-- ========================================

CREATE TABLE IF NOT EXISTS `pangya_item_daily` (
`ID` int(11) NOT NULL,
  `Name` varchar(50) COLLATE utf8mb4_bin NOT NULL DEFAULT '',
  `ItemTypeID` int(11) NOT NULL,
  `Quantity` int(11) DEFAULT '1',
  `ItemType` int(11) NOT NULL DEFAULT '0'
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin AUTO_INCREMENT=31 ;

INSERT INTO `pangya_item_daily` (`ID`, `Name`, `ItemTypeID`, `Quantity`, `ItemType`) VALUES
(1, 'Day 1 - Power Potion', 402653188, 10, 0),
(2, 'Day 2 - Control Potion', 402653189, 10, 0),
(3, 'Day 3 - Spin Potion', 402653184, 10, 0),
(4, 'Day 4 - Curve Potion', 402653185, 10, 0),
(5, 'Day 5 - Comet Ball', 1073741824, 10, 3),
(6, 'Day 6 - Power Potion+', 402653190, 5, 0),
(7, 'Day 7 - Control Potion+', 402653193, 5, 0),
(8, 'Day 8 - Pang Pouch', 436207633, 50, 0),
(9, 'Day 9 - Comet Ball', 1073741824, 20, 3),
(10, 'Day 10 - Power Potion+', 402653190, 10, 0),
(11, 'Day 11 - Control Potion+', 402653193, 10, 0),
(12, 'Day 12 - Spin Potion', 402653184, 20, 0),
(13, 'Day 13 - Curve Potion', 402653185, 20, 0),
(14, 'Day 14 - Power Potion', 402653188, 30, 0),
(15, 'Day 15 - Control Potion', 402653189, 30, 0),
(16, 'Day 16 - Comet Ball', 1073741824, 30, 3),
(17, 'Day 17 - Spin Potion', 402653184, 30, 0),
(18, 'Day 18 - Curve Potion', 402653185, 30, 0),
(19, 'Day 19 - Power Potion+', 402653190, 20, 0),
(20, 'Day 20 - Control Potion+', 402653193, 20, 0),
(21, 'Day 21 - Pang Pouch', 436207633, 100, 0),
(22, 'Day 22 - Comet Ball', 1073741824, 40, 3),
(23, 'Day 23 - Power Potion', 402653188, 50, 0),
(24, 'Day 24 - Control Potion', 402653189, 50, 0),
(25, 'Day 25 - Spin Potion', 402653184, 50, 0),
(26, 'Day 26 - Curve Potion', 402653185, 50, 0),
(27, 'Day 27 - Power Potion+', 402653190, 30, 0),
(28, 'Day 28 - Control Potion+', 402653193, 30, 0),
(29, 'Day 29 - Comet Ball', 1073741824, 50, 3),
(30, 'Day 30 - Pang Pouch', 436207633, 300, 0);

-- ========================================

CREATE TABLE IF NOT EXISTS `pangya_item_daily_log` (
`Counter` int(11) NOT NULL,
  `UID` int(11) NOT NULL,
  `Item_TypeID` int(11) NOT NULL,
  `Item_Quantity` int(11) NOT NULL,
  `Item_TypeID_Next` int(11) NOT NULL,
  `Item_Quantity_Next` int(11) NOT NULL,
  `LoginCount` int(11) NOT NULL,
  `RegDate` date NOT NULL
) ENGINE=InnoDB DEFAULT CHARSET=latin1 AUTO_INCREMENT=1 ;

-- --------------------------------------------------------
-- LOGGING AND SYSTEM TABLES
-- --------------------------------------------------------

CREATE TABLE IF NOT EXISTS `pangya_exception_log` (
`ExceptionID` int(11) NOT NULL,
  `UID` int(11) DEFAULT NULL,
  `Username` varchar(50) COLLATE utf8mb4_bin DEFAULT NULL,
  `ExceptionMessage` text COLLATE utf8mb4_bin,
  `Server` varchar(50) COLLATE utf8mb4_bin DEFAULT NULL,
  `CreateDate` datetime(3) DEFAULT CURRENT_TIMESTAMP(3)
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin AUTO_INCREMENT=1 ;

CREATE TABLE IF NOT EXISTS `pangya_locker_item` (
`INVEN_ID` int(11) NOT NULL,
  `UID` int(11) NOT NULL DEFAULT '0',
  `TypeID` int(11) DEFAULT '0',
  `Name` varchar(255) DEFAULT 'Name',
  `FROM_ID` int(11) DEFAULT '0',
  `Valid` tinyint(4) DEFAULT '1'
) ENGINE=InnoDB DEFAULT CHARSET=latin1 AUTO_INCREMENT=1 ;

CREATE TABLE IF NOT EXISTS `pangya_memorial_log` (
`LogID` int(11) NOT NULL,
  `UID` int(11) DEFAULT NULL,
  `ItemName` text COLLATE utf8mb4_bin,
  `Quantity` int(11) DEFAULT '1',
  `DateIN` datetime(3) DEFAULT CURRENT_TIMESTAMP(3)
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin AUTO_INCREMENT=1 ;

CREATE TABLE IF NOT EXISTS `pangya_transaction_log` (
`ID` int(11) NOT NULL,
  `UID` int(11) DEFAULT NULL,
  `String` text COLLATE utf8mb4_bin,
  `ERROR_NUMBER` int(11) DEFAULT NULL,
  `ERROR_SEVERITY` int(11) DEFAULT NULL,
  `ERROR_STATE` int(11) DEFAULT NULL,
  `ERROR_PROCEDURE` text COLLATE utf8mb4_bin,
  `ERROR_LINE` int(11) DEFAULT NULL,
  `ERROR_MESSAGE` text COLLATE utf8mb4_bin
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin AUTO_INCREMENT=1 ;

CREATE TABLE IF NOT EXISTS `pangya_personal_log` (
`LogID` int(11) NOT NULL,
  `ActionType` text COLLATE utf8mb4_bin NOT NULL,
  `Amount` int(11) DEFAULT '0',
  `UID` int(11) NOT NULL DEFAULT '0',
  `LockerPang` int(11) DEFAULT '0'
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin AUTO_INCREMENT=1 ;

-- --------------------------------------------------------
-- SOCIAL TABLES
-- --------------------------------------------------------

CREATE TABLE IF NOT EXISTS `pangya_friend` (
  `Owner` varchar(50) COLLATE utf8mb4_bin NOT NULL,
  `Friend` varchar(50) COLLATE utf8mb4_bin NOT NULL,
  `IsAccept` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `GroupName` varchar(50) COLLATE utf8mb4_bin NOT NULL DEFAULT 'Friend',
  `IsAgree` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `IsDeleted` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `Memo` varchar(20) COLLATE utf8mb4_bin NOT NULL DEFAULT 'Friend',
  `IsBlock` tinyint(3) unsigned NOT NULL DEFAULT '0'
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `pangya_mail` (
`Mail_Index` int(11) NOT NULL,
  `UID` int(11) NOT NULL,
  `Sender` varchar(20) COLLATE utf8mb4_bin DEFAULT NULL,
  `Sender_UID` int(11) DEFAULT NULL,
  `Receiver` varchar(20) COLLATE utf8mb4_bin DEFAULT NULL,
  `Receiver_UID` int(11) DEFAULT NULL,
  `Subject` text COLLATE utf8mb4_bin,
  `Msg` text COLLATE utf8mb4_bin,
  `ReadDate` datetime DEFAULT NULL,
  `ReceiveDate` datetime DEFAULT NULL,
  `DeleteDate` datetime DEFAULT NULL,
  `RegDate` datetime(3) DEFAULT CURRENT_TIMESTAMP(3)
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin AUTO_INCREMENT=1 ;

CREATE TABLE IF NOT EXISTS `pangya_mail_item` (
  `Mail_Index` int(11) NOT NULL,
  `TYPEID` int(11) NOT NULL,
  `SETTYPEID` int(11) DEFAULT '0',
  `QTY` int(11) DEFAULT NULL,
  `DAY` smallint(6) DEFAULT '0',
  `UCC_UNIQUE` varchar(8) COLLATE utf8mb4_bin DEFAULT NULL,
  `ITEM_GRP` tinyint(3) unsigned DEFAULT NULL,
  `TO_UID` int(11) DEFAULT NULL,
  `IN_DATE` datetime(3) DEFAULT CURRENT_TIMESTAMP(3),
  `RELEASE_DATE` datetime(3) DEFAULT NULL,
  `APPLY_ITEM_ID` int(11) DEFAULT NULL
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `pangya_user_message` (
  `ID_MSG` int(11) NOT NULL,
  `UID` int(11) NOT NULL,
  `UID_FROM` int(11) NOT NULL,
  `Valid` tinyint(3) unsigned NOT NULL,
  `Message` varchar(70) COLLATE utf8mb4_bin NOT NULL,
  `Reg_Date` datetime(3) NOT NULL DEFAULT CURRENT_TIMESTAMP(3)
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

-- --------------------------------------------------------
-- GUILD TABLES
-- --------------------------------------------------------

CREATE TABLE IF NOT EXISTS `pangya_guild_emblem` (
`EMBLEM_IDX` int(11) NOT NULL,
  `GUILD_ID` int(11) NOT NULL,
  `GUILD_MARK_IMG` varchar(50) COLLATE utf8mb4_bin DEFAULT NULL,
  `GUILD_MARK_ISVALID` tinyint(3) unsigned DEFAULT '1'
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin AUTO_INCREMENT=1 ;

CREATE TABLE IF NOT EXISTS `pangya_guild_info` (
`GUILD_INDEX` int(11) NOT NULL,
  `GUILD_NAME` text COLLATE utf8mb4_bin NOT NULL,
  `GUILD_INTRODUCING` text COLLATE utf8mb4_bin,
  `GUILD_NOTICE` text COLLATE utf8mb4_bin,
  `GUILD_LEADER_UID` int(11) NOT NULL,
  `GUILD_POINT` int(11) DEFAULT '0',
  `GUILD_PANG` int(11) DEFAULT '0',
  `GUILD_IMAGE` varchar(10) COLLATE utf8mb4_bin DEFAULT 'GUILDMARK',
  `GUILD_IMAGE_KEY_UPLOAD` int(11) DEFAULT NULL,
  `GUILD_CREATE_DATE` datetime(3) DEFAULT CURRENT_TIMESTAMP(3),
  `GUILD_VALID` tinyint(3) unsigned DEFAULT '1'
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin AUTO_INCREMENT=1 ;

CREATE TABLE IF NOT EXISTS `pangya_guild_log` (
  `UID` int(11) NOT NULL,
  `GUILD_ID` int(11) NOT NULL,
  `GUILD_NAME` varchar(32) COLLATE utf8mb4_bin DEFAULT NULL,
  `GUILD_ACTION` tinyint(3) unsigned NOT NULL,
  `GUILD_ACTION_DATE` datetime(3) DEFAULT CURRENT_TIMESTAMP(3)
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `pangya_guild_member` (
  `GUILD_ID` int(11) NOT NULL,
  `GUILD_MEMBER_UID` int(11) NOT NULL,
  `GUILD_POSITION` tinyint(3) unsigned DEFAULT '3',
  `GUILD_MESSAGE` text COLLATE utf8mb4_bin,
  `GUILD_ENTERED_TIME` datetime(3) DEFAULT CURRENT_TIMESTAMP(3),
  `GUILD_MEMBER_STATUS` tinyint(3) unsigned DEFAULT '0'
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

-- --------------------------------------------------------
-- PLAYER DATA TABLES
-- --------------------------------------------------------

CREATE TABLE IF NOT EXISTS `pangya_game_macro` (
  `UID` int(11) NOT NULL,
  `Macro1` varchar(45) COLLATE utf8mb4_bin DEFAULT 'Pangya!',
  `Macro2` varchar(45) COLLATE utf8mb4_bin DEFAULT 'Pangya!',
  `Macro3` varchar(45) COLLATE utf8mb4_bin DEFAULT 'Pangya!',
  `Macro4` varchar(45) COLLATE utf8mb4_bin DEFAULT 'Pangya!',
  `Macro5` varchar(45) COLLATE utf8mb4_bin DEFAULT 'Pangya!',
  `Macro6` varchar(45) COLLATE utf8mb4_bin DEFAULT 'Pangya!',
  `Macro7` varchar(45) COLLATE utf8mb4_bin DEFAULT 'Pangya!',
  `Macro8` varchar(45) COLLATE utf8mb4_bin DEFAULT 'Pangya!',
  `Macro9` varchar(45) COLLATE utf8mb4_bin DEFAULT 'Pangya!',
  `Macro10` varchar(45) COLLATE utf8mb4_bin DEFAULT 'Pangya!'
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `pangya_map_statistics` (
`ID` int(11) NOT NULL,
  `UID` int(11) NOT NULL,
  `Map` smallint(6) NOT NULL DEFAULT '0',
  `Drive` int(11) NOT NULL DEFAULT '0',
  `Putt` int(11) NOT NULL DEFAULT '0',
  `Hole` int(11) NOT NULL DEFAULT '0',
  `Fairway` int(11) NOT NULL DEFAULT '0',
  `Holein` int(11) NOT NULL DEFAULT '0',
  `PuttIn` int(11) NOT NULL DEFAULT '0',
  `TotalScore` int(11) NOT NULL DEFAULT '0',
  `BestScore` smallint(6) NOT NULL DEFAULT '127',
  `MaxPang` int(11) NOT NULL DEFAULT '0',
  `CharTypeId` int(11) NOT NULL DEFAULT '0',
  `EventScore` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `Assist` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `REGDATE` datetime DEFAULT CURRENT_TIMESTAMP
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin AUTO_INCREMENT=1 ;

CREATE TABLE IF NOT EXISTS `pangya_member` (
`UID` int(11) NOT NULL,
  `Username` varchar(16) COLLATE utf8mb4_bin NOT NULL,
  `Password` varchar(50) COLLATE utf8mb4_bin NOT NULL,
  `IDState` tinyint(3) unsigned DEFAULT '0',
  `FirstSet` tinyint(3) unsigned DEFAULT '0',
  `LastLogonTime` datetime(3) DEFAULT NULL,
  `Logon` tinyint(3) unsigned DEFAULT NULL,
  `Nickname` varchar(16) COLLATE utf8mb4_bin DEFAULT NULL,
  `Sex` tinyint(3) unsigned DEFAULT '0',
  `IPAddress` varchar(50) COLLATE utf8mb4_bin DEFAULT NULL,
  `LogonCount` int(11) DEFAULT '0',
  `Capabilities` tinyint(3) unsigned DEFAULT '0',
  `RegDate` datetime(3) DEFAULT CURRENT_TIMESTAMP(3),
  `AuthKey_Login` varchar(7) COLLATE utf8mb4_bin DEFAULT NULL,
  `AuthKey_Game` varchar(7) COLLATE utf8mb4_bin DEFAULT NULL,
  `GUILDINDEX` int(11) DEFAULT '0',
  `DailyLoginCount` int(11) DEFAULT '0',
  `Tutorial` tinyint(3) unsigned DEFAULT '0',
  `BirthDay` date DEFAULT NULL,
  `Event1` tinyint(3) unsigned DEFAULT '0',
  `Event2` tinyint(3) unsigned DEFAULT '0'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin AUTO_INCREMENT=1 ;

CREATE TABLE IF NOT EXISTS `pangya_personal` (
  `UID` int(11) NOT NULL,
  `CookieAmt` int(11) DEFAULT '0',
  `PangLockerAmt` int(11) DEFAULT '0',
  `LockerPwd` varchar(4) COLLATE utf8mb4_bin DEFAULT '0',
  `AssistMode` tinyint(3) unsigned DEFAULT '0' COMMENT 'Assist Mode: 0=OFF, 1=ON'
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `pangya_selfdesign` (
  `UID` int(11) NOT NULL,
  `ITEM_ID` int(11) NOT NULL,
  `UCC_UNIQE` varchar(8) COLLATE utf8mb4_bin NOT NULL,
  `TYPEID` int(11) NOT NULL,
  `UCC_STATUS` tinyint(3) unsigned DEFAULT '0',
  `UCC_KEY` varchar(50) COLLATE utf8mb4_bin DEFAULT NULL,
  `UCC_NAME` varchar(20) COLLATE utf8mb4_bin DEFAULT NULL,
  `UCC_DRAWER` int(11) DEFAULT '0',
  `UCC_COCOUNT` int(11) DEFAULT '1',
  `IN_DATE` datetime(3) DEFAULT CURRENT_TIMESTAMP(3)
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `pangya_tutorial` (
  `UID` int(11) DEFAULT NULL,
  `Rookie` int(11) DEFAULT NULL,
  `Beginner` int(11) DEFAULT NULL,
  `Advancer` int(11) DEFAULT NULL
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `pangya_user_equip` (
  `UID` int(11) NOT NULL,
  `CADDIE` int(11) NOT NULL DEFAULT '0',
  `CHARACTER_ID` int(11) NOT NULL DEFAULT '0',
  `CLUB_ID` int(11) NOT NULL DEFAULT '0',
  `BALL_ID` int(11) NOT NULL DEFAULT '0',
  `ITEM_SLOT_1` int(11) NOT NULL DEFAULT '0',
  `ITEM_SLOT_2` int(11) NOT NULL DEFAULT '0',
  `ITEM_SLOT_3` int(11) NOT NULL DEFAULT '0',
  `ITEM_SLOT_4` int(11) NOT NULL DEFAULT '0',
  `ITEM_SLOT_5` int(11) NOT NULL DEFAULT '0',
  `ITEM_SLOT_6` int(11) NOT NULL DEFAULT '0',
  `ITEM_SLOT_7` int(11) NOT NULL DEFAULT '0',
  `ITEM_SLOT_8` int(11) NOT NULL DEFAULT '0',
  `ITEM_SLOT_9` int(11) NOT NULL DEFAULT '0',
  `ITEM_SLOT_10` int(11) NOT NULL DEFAULT '0',
  `Skin_1` int(11) NOT NULL DEFAULT '0',
  `Skin_2` int(11) NOT NULL DEFAULT '0',
  `Skin_3` int(11) NOT NULL DEFAULT '0',
  `Skin_4` int(11) NOT NULL DEFAULT '0',
  `Skin_5` int(11) NOT NULL DEFAULT '0',
  `Skin_6` int(11) NOT NULL DEFAULT '0',
  `MASCOT_ID` int(11) NOT NULL DEFAULT '0',
  `POSTER_1` int(11) NOT NULL DEFAULT '0',
  `POSTER_2` int(11) NOT NULL DEFAULT '0'
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `pangya_user_matchhistory` (
  `UID` int(11) NOT NULL,
  `UID1` int(11) NOT NULL DEFAULT '0',
  `UID2` int(11) NOT NULL DEFAULT '0',
  `UID3` int(11) NOT NULL DEFAULT '0',
  `UID4` int(11) NOT NULL DEFAULT '0',
  `UID5` int(11) NOT NULL DEFAULT '0'
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

CREATE TABLE IF NOT EXISTS `pangya_user_statistics` (
`ID` int(11) NOT NULL,
  `UID` int(11) NOT NULL,
  `Drive` int(11) NOT NULL DEFAULT '0',
  `Putt` int(11) NOT NULL DEFAULT '0',
  `Playtime` int(11) NOT NULL DEFAULT '0',
  `Longest` float NOT NULL DEFAULT '0',
  `Distance` int(11) NOT NULL DEFAULT '0',
  `Pangya` int(11) NOT NULL DEFAULT '0',
  `Hole` int(11) NOT NULL DEFAULT '0',
  `TeamHole` int(11) NOT NULL DEFAULT '0',
  `Holeinone` int(11) NOT NULL DEFAULT '0',
  `OB` int(11) NOT NULL DEFAULT '0',
  `Bunker` int(11) NOT NULL DEFAULT '0',
  `Fairway` int(11) NOT NULL DEFAULT '0',
  `Albatross` int(11) NOT NULL DEFAULT '0',
  `Holein` int(11) NOT NULL DEFAULT '0',
  `Pang` int(11) NOT NULL DEFAULT '3000',
  `Timeout` int(11) NOT NULL DEFAULT '0',
  `Game_Level` smallint(6) NOT NULL DEFAULT '0',
  `Game_Point` int(11) NOT NULL DEFAULT '0',
  `PuttIn` int(11) NOT NULL DEFAULT '0',
  `LongestPuttIn` float NOT NULL DEFAULT '0',
  `LongestChipIn` float NOT NULL DEFAULT '0',
  `NoMannerGameCount` int(11) NOT NULL DEFAULT '0',
  `ShotTime` int(11) NOT NULL DEFAULT '0',
  `GameCount` int(11) NOT NULL DEFAULT '0',
  `DisconnectGames` int(11) NOT NULL DEFAULT '0',
  `wTeamWin` int(11) NOT NULL DEFAULT '0',
  `wTeamGames` int(11) NOT NULL DEFAULT '0',
  `LadderPoint` smallint(6) NOT NULL DEFAULT '1000',
  `LadderWin` smallint(6) NOT NULL DEFAULT '0',
  `LadderLose` smallint(6) NOT NULL DEFAULT '0',
  `LadderDraw` smallint(6) NOT NULL DEFAULT '0',
  `ComboCount` int(11) NOT NULL DEFAULT '0',
  `MaxComboCount` int(11) NOT NULL DEFAULT '0',
  `TotalScore` int(11) NOT NULL DEFAULT '0',
  `BestScore0` smallint(6) NOT NULL DEFAULT '127',
  `BestScore1` smallint(6) NOT NULL DEFAULT '127',
  `BestScore2` smallint(6) NOT NULL DEFAULT '127',
  `BestScore3` smallint(6) NOT NULL DEFAULT '127',
  `BESTSCORE4` smallint(6) NOT NULL DEFAULT '127',
  `MaxPang0` int(11) DEFAULT '0',
  `MaxPang1` int(11) DEFAULT '0',
  `MaxPang2` int(11) DEFAULT '0',
  `MaxPang3` int(11) DEFAULT '0',
  `MAXPANG4` int(11) DEFAULT '0',
  `SumPang` int(11) NOT NULL DEFAULT '0',
  `LadderHole` smallint(6) NOT NULL DEFAULT '0',
  `GameCountSeason` int(11) NOT NULL DEFAULT '0',
  `SkinsPang` bigint(20) NOT NULL DEFAULT '0',
  `SkinsWin` int(11) NOT NULL DEFAULT '0',
  `SkinsLose` int(11) NOT NULL DEFAULT '0',
  `SkinsRunHoles` int(11) NOT NULL DEFAULT '0',
  `SkinsStrikePoint` int(11) NOT NULL DEFAULT '0',
  `SkinsAllinCount` int(11) NOT NULL DEFAULT '0',
  `EventValue` int(11) NOT NULL DEFAULT '0',
  `EventFlag` int(11) NOT NULL DEFAULT '0'
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

-- --------------------------------------------------------
-- SERVER CONFIGURATION
-- --------------------------------------------------------

CREATE TABLE IF NOT EXISTS `pangya_server` (
  `ServerID` int(11) NOT NULL,
  `Name` varchar(50) COLLATE utf8mb4_bin NOT NULL,
  `IP` varchar(50) COLLATE utf8mb4_bin NOT NULL,
  `Port` int(11) NOT NULL DEFAULT '0',
  `MaxUser` int(11) NOT NULL DEFAULT '1000',
  `UsersOnline` int(11) NOT NULL DEFAULT '0',
  `Property` int(11) NOT NULL DEFAULT '2048',
  `BlockFunc` bigint(20) NOT NULL DEFAULT '0',
  `ImgNo` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `ImgEvent` smallint(6) NOT NULL DEFAULT '0',
  `ServerType` tinyint(3) unsigned NOT NULL DEFAULT '0',
  `Active` tinyint(4) NOT NULL DEFAULT '1'
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin;

INSERT INTO `pangya_server` (`ServerID`, `Name`, `IP`, `Port`, `MaxUser`, `UsersOnline`, `Property`, `BlockFunc`, `ImgNo`, `ImgEvent`, `ServerType`, `Active`) VALUES
(7997, 'AuthServer', '127.0.0.1', 7997, 3000, 0, 0, 0, 0, 0, 3, 1),
(10201, 'LoginServer', '127.0.0.1', 10201, 3000, 0, 0, 0, 0, 0, 0, 1),
(20201, 'BDMSERVER', '127.0.0.1', 20201, 3000, 0, 2048, 0, 1, 4096, 1, 1),
(30304, 'Message Server', '127.0.0.1', 30304, 2000, 0, 4096, 0, 0, 0, 2, 0);

CREATE TABLE IF NOT EXISTS `pangya_string` (
`Id` int(11) NOT NULL,
  `str` text COLLATE utf8mb4_bin NOT NULL
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin AUTO_INCREMENT=342 ;


-- --------------------------------------------------------
-- MY HOME TABLES
-- --------------------------------------------------------

CREATE TABLE IF NOT EXISTS `td_room_data` (
`IDX` int(11) NOT NULL,
  `UID` int(11) NOT NULL DEFAULT '0',
  `TYPEID` int(11) NOT NULL DEFAULT '0',
  `POS_X` decimal(10,4) DEFAULT '0.0000',
  `POS_Y` decimal(10,4) DEFAULT '0.0000',
  `POS_Z` decimal(10,4) DEFAULT '0.0000',
  `POS_R` decimal(10,4) DEFAULT '0.0000',
  `VALID` tinyint(3) unsigned DEFAULT '1',
  `GETDATE` datetime(3) DEFAULT CURRENT_TIMESTAMP(3)
) ENGINE=MyISAM DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_bin AUTO_INCREMENT=1 ;

-- --------------------------------------------------------
-- INDEXES
-- --------------------------------------------------------

ALTER TABLE `achievement_counter_data` ADD PRIMARY KEY (`ID`);
ALTER TABLE `achievement_data` ADD PRIMARY KEY (`ID`), ADD KEY `IX_Achievement_Data` (`ACHIEVEMENT_QUEST_TYPEID`);
ALTER TABLE `achievement_questitem` ADD PRIMARY KEY (`ID`);
ALTER TABLE `achievement_queststuffs` ADD PRIMARY KEY (`ID`), ADD KEY `IX_Achievement_QuestStuffs_1` (`TypeID`);
ALTER TABLE `daily_quest` ADD PRIMARY KEY (`ID`);
ALTER TABLE `pangya_achievement` ADD PRIMARY KEY (`ID`);
ALTER TABLE `pangya_achievement_counter` ADD PRIMARY KEY (`ID`);
ALTER TABLE `pangya_achievement_quest` ADD PRIMARY KEY (`ID`);
ALTER TABLE `pangya_caddie` ADD PRIMARY KEY (`CID`,`UID`);
ALTER TABLE `pangya_card` ADD PRIMARY KEY (`CARD_IDX`);
ALTER TABLE `pangya_card_equip` ADD PRIMARY KEY (`ID`);
ALTER TABLE `pangya_character` ADD PRIMARY KEY (`CID`);
ALTER TABLE `pangya_club_info` ADD PRIMARY KEY (`ITEM_ID`,`UID`), ADD KEY `IX_UID` (`UID`);
ALTER TABLE `pangya_daily_quest` ADD PRIMARY KEY (`ID`);
ALTER TABLE `pangya_exception_log` ADD PRIMARY KEY (`ExceptionID`);
ALTER TABLE `pangya_guild_emblem` ADD PRIMARY KEY (`EMBLEM_IDX`);
ALTER TABLE `pangya_guild_info` ADD PRIMARY KEY (`GUILD_INDEX`);
ALTER TABLE `pangya_guild_member` ADD PRIMARY KEY (`GUILD_ID`,`GUILD_MEMBER_UID`);
ALTER TABLE `pangya_item_daily` ADD PRIMARY KEY (`ID`);
ALTER TABLE `pangya_item_daily_log` ADD PRIMARY KEY (`Counter`), ADD KEY `IX_Pangya_Item_Daily_Log_UID` (`UID`);
ALTER TABLE `pangya_locker_item` ADD PRIMARY KEY (`INVEN_ID`,`UID`);
ALTER TABLE `pangya_mail` ADD PRIMARY KEY (`Mail_Index`);
ALTER TABLE `pangya_mail_item` ADD PRIMARY KEY (`Mail_Index`,`TYPEID`);
ALTER TABLE `pangya_map_statistics` ADD PRIMARY KEY (`ID`);
ALTER TABLE `pangya_mascot` ADD PRIMARY KEY (`MID`);
ALTER TABLE `pangya_member` ADD PRIMARY KEY (`UID`);
ALTER TABLE `pangya_memorial_log` ADD PRIMARY KEY (`LogID`);
ALTER TABLE `pangya_personal` ADD PRIMARY KEY (`UID`);
ALTER TABLE `pangya_personal_log` ADD PRIMARY KEY (`LogID`);
ALTER TABLE `pangya_selfdesign` ADD PRIMARY KEY (`UID`,`ITEM_ID`,`UCC_UNIQE`);
ALTER TABLE `pangya_server` ADD PRIMARY KEY (`ServerID`);
ALTER TABLE `pangya_string` ADD PRIMARY KEY (`Id`);
ALTER TABLE `pangya_transaction_log` ADD PRIMARY KEY (`ID`);
ALTER TABLE `pangya_user_equip` ADD PRIMARY KEY (`UID`);
ALTER TABLE `pangya_user_matchhistory` ADD PRIMARY KEY (`UID`,`UID1`,`UID2`,`UID3`,`UID4`,`UID5`);
ALTER TABLE `pangya_user_statistics` ADD PRIMARY KEY (`ID`), ADD UNIQUE KEY `UK_UID` (`UID`);
ALTER TABLE `td_room_data` ADD PRIMARY KEY (`IDX`);

-- --------------------------------------------------------
-- AUTO_INCREMENT (All set to 1)
-- --------------------------------------------------------

ALTER TABLE `achievement_counter_data` MODIFY `ID` int(11) NOT NULL AUTO_INCREMENT,AUTO_INCREMENT=1;
ALTER TABLE `achievement_data` MODIFY `ID` int(11) NOT NULL AUTO_INCREMENT,AUTO_INCREMENT=1;
ALTER TABLE `achievement_questitem` MODIFY `ID` int(11) NOT NULL AUTO_INCREMENT,AUTO_INCREMENT=1;
ALTER TABLE `achievement_queststuffs` MODIFY `ID` int(11) NOT NULL AUTO_INCREMENT,AUTO_INCREMENT=1;
ALTER TABLE `daily_quest` MODIFY `ID` int(11) NOT NULL AUTO_INCREMENT,AUTO_INCREMENT=1;
ALTER TABLE `pangya_achievement` MODIFY `ID` int(11) NOT NULL AUTO_INCREMENT,AUTO_INCREMENT=1;
ALTER TABLE `pangya_achievement_counter` MODIFY `ID` int(11) NOT NULL AUTO_INCREMENT,AUTO_INCREMENT=1;
ALTER TABLE `pangya_achievement_quest` MODIFY `ID` int(11) NOT NULL AUTO_INCREMENT,AUTO_INCREMENT=1;
ALTER TABLE `pangya_caddie` MODIFY `CID` int(11) NOT NULL AUTO_INCREMENT,AUTO_INCREMENT=1;
ALTER TABLE `pangya_card` MODIFY `CARD_IDX` int(11) NOT NULL AUTO_INCREMENT,AUTO_INCREMENT=1;
ALTER TABLE `pangya_card_equip` MODIFY `ID` int(11) NOT NULL AUTO_INCREMENT,AUTO_INCREMENT=1;
ALTER TABLE `pangya_character` MODIFY `CID` int(11) NOT NULL AUTO_INCREMENT,AUTO_INCREMENT=1;
ALTER TABLE `pangya_daily_quest` MODIFY `ID` int(11) NOT NULL AUTO_INCREMENT,AUTO_INCREMENT=1;
ALTER TABLE `pangya_exception_log` MODIFY `ExceptionID` int(11) NOT NULL AUTO_INCREMENT,AUTO_INCREMENT=1;
ALTER TABLE `pangya_guild_emblem` MODIFY `EMBLEM_IDX` int(11) NOT NULL AUTO_INCREMENT,AUTO_INCREMENT=1;
ALTER TABLE `pangya_guild_info` MODIFY `GUILD_INDEX` int(11) NOT NULL AUTO_INCREMENT,AUTO_INCREMENT=1;
ALTER TABLE `pangya_item_daily` MODIFY `ID` int(11) NOT NULL AUTO_INCREMENT,AUTO_INCREMENT=31;
ALTER TABLE `pangya_item_daily_log` MODIFY `Counter` int(11) NOT NULL AUTO_INCREMENT,AUTO_INCREMENT=1;
ALTER TABLE `pangya_locker_item` MODIFY `INVEN_ID` int(11) NOT NULL AUTO_INCREMENT,AUTO_INCREMENT=1;
ALTER TABLE `pangya_mail` MODIFY `Mail_Index` int(11) NOT NULL AUTO_INCREMENT,AUTO_INCREMENT=1;
ALTER TABLE `pangya_map_statistics` MODIFY `ID` int(11) NOT NULL AUTO_INCREMENT,AUTO_INCREMENT=1;
ALTER TABLE `pangya_mascot` MODIFY `MID` int(11) NOT NULL AUTO_INCREMENT,AUTO_INCREMENT=1;
ALTER TABLE `pangya_member` MODIFY `UID` int(11) NOT NULL AUTO_INCREMENT,AUTO_INCREMENT=1;
ALTER TABLE `pangya_memorial_log` MODIFY `LogID` int(11) NOT NULL AUTO_INCREMENT,AUTO_INCREMENT=1;
ALTER TABLE `pangya_personal_log` MODIFY `LogID` int(11) NOT NULL AUTO_INCREMENT,AUTO_INCREMENT=1;
ALTER TABLE `pangya_string` MODIFY `Id` int(11) NOT NULL AUTO_INCREMENT,AUTO_INCREMENT=1;
ALTER TABLE `pangya_transaction_log` MODIFY `ID` int(11) NOT NULL AUTO_INCREMENT,AUTO_INCREMENT=1;
ALTER TABLE `pangya_user_statistics` MODIFY `ID` int(11) NOT NULL AUTO_INCREMENT,AUTO_INCREMENT=1;
ALTER TABLE `td_room_data` MODIFY `IDX` int(11) NOT NULL AUTO_INCREMENT,AUTO_INCREMENT=1;

-- --------------------------------------------------------
-- FOREIGN KEY CONSTRAINTS
-- --------------------------------------------------------

-- Link pangya_club_info to pangya_member
ALTER TABLE `pangya_club_info`
  ADD CONSTRAINT `FK_ClubInfo_Member` 
  FOREIGN KEY (`UID`) REFERENCES `pangya_member`(`UID`)
  ON DELETE CASCADE
  ON UPDATE CASCADE;

-- Link pangya_personal to pangya_member
ALTER TABLE `pangya_personal`
  ADD CONSTRAINT `FK_Personal_Member`
  FOREIGN KEY (`UID`) REFERENCES `pangya_member`(`UID`)
  ON DELETE CASCADE
  ON UPDATE CASCADE;

-- Link pangya_club_info to pangya_warehouse (club item)
-- NOTE: Foreign key now references composite primary key (item_id, UID)
ALTER TABLE `pangya_club_info`
  ADD CONSTRAINT `FK_ClubInfo_Warehouse`
  FOREIGN KEY (`ITEM_ID`, `UID`) REFERENCES `pangya_warehouse`(`item_id`, `UID`)
  ON DELETE CASCADE
  ON UPDATE CASCADE;

/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;

-- ========================================
-- END OF CLEAN SCHEMA
-- ========================================

-- MySQL dump 10.13  Distrib 9.7.0, for Win64 (x86_64)
--
-- Host: 127.0.0.1    Database: dbweb
-- ------------------------------------------------------
-- Server version	9.7.0

/*!40101 SET @OLD_CHARACTER_SET_CLIENT=@@CHARACTER_SET_CLIENT */;
/*!40101 SET @OLD_CHARACTER_SET_RESULTS=@@CHARACTER_SET_RESULTS */;
/*!40101 SET @OLD_COLLATION_CONNECTION=@@COLLATION_CONNECTION */;
/*!50503 SET NAMES utf8mb4 */;
/*!40103 SET @OLD_TIME_ZONE=@@TIME_ZONE */;
/*!40103 SET TIME_ZONE='+00:00' */;
/*!40014 SET @OLD_UNIQUE_CHECKS=@@UNIQUE_CHECKS, UNIQUE_CHECKS=0 */;
/*!40014 SET @OLD_FOREIGN_KEY_CHECKS=@@FOREIGN_KEY_CHECKS, FOREIGN_KEY_CHECKS=0 */;
/*!40101 SET @OLD_SQL_MODE=@@SQL_MODE, SQL_MODE='NO_AUTO_VALUE_ON_ZERO' */;
/*!40111 SET @OLD_SQL_NOTES=@@SQL_NOTES, SQL_NOTES=0 */;
SET @MYSQLDUMP_TEMP_LOG_BIN = @@SESSION.SQL_LOG_BIN;
SET @@SESSION.SQL_LOG_BIN= 0;

--
-- GTID state at the beginning of the backup 
--

SET @@GLOBAL.GTID_PURGED=/*!80000 '+'*/ '51133d8b-521c-11f1-a029-d8bbc1b21619:1-10028';

--
-- Current Database: `dbweb`
--

CREATE DATABASE /*!32312 IF NOT EXISTS*/ `dbweb` /*!40100 DEFAULT CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci */ /*!80016 DEFAULT ENCRYPTION='N' */;

USE `dbweb`;

--
-- Table structure for table `__efmigrationshistory`
--

DROP TABLE IF EXISTS `__efmigrationshistory`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `__efmigrationshistory` (
  `MigrationId` varchar(150) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ProductVersion` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  PRIMARY KEY (`MigrationId`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `__efmigrationshistory`
--

LOCK TABLES `__efmigrationshistory` WRITE;
/*!40000 ALTER TABLE `__efmigrationshistory` DISABLE KEYS */;
INSERT INTO `__efmigrationshistory` VALUES ('20260514213137_InitialCreate','8.0.0'),('20260515165957_update user','8.0.0'),('20260517023418_AddFriendTables','8.0.0'),('20260517032938_AddMessageEditHistory','8.0.0'),('20260517120000_Phase2SnippetUpgrade','8.0.0'),('20260517170349_AddIocCenter','8.0.0'),('20260519163757_AddSavedMessages','8.0.0'),('20260519165718_AddOneTimeSecrets','8.0.0'),('20260519171459_AddOneTimeSecretViewerInfo','8.0.0'),('20260520073320_AddPentestFindings','8.0.0'),('20260520105411_AddPentestFindingTechnicalFields','8.0.0'),('20260520113547_AddChatReminders','8.0.0'),('20260520123622_AddGroupTasks','8.0.0'),('20260521090642_AddClearedAtToGroupMember','8.0.0'),('20260522021106_AddPasswordResetOtp','8.0.0'),('20260529143243_add','8.0.0'),('20260621051048_AddEmailVerification','8.0.0'),('20260622145159_update final','8.0.0'),('20260622145752_update next','8.0.0'),('20260622150008_update final 1','8.0.0'),('20260622150610_update final 2','8.0.0'),('20260622150701_update final 3','8.0.0'),('20260622151020_update final 4','8.0.0'),('20260622155241_AddPollFeature','8.0.0'),('20260624192050_OptimizeIndexes','9.0.6'),('20260624193423_AddReadReceiptUserIndex','9.0.6'),('20260624200705_AddSoftDeleteToIocAndPentest','9.0.6');
/*!40000 ALTER TABLE `__efmigrationshistory` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `auditlogs`
--

DROP TABLE IF EXISTS `auditlogs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `auditlogs` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` int DEFAULT NULL,
  `Action` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `IpAddress` varchar(45) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `UserAgent` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Metadata` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  KEY `IX_AuditLogs_Action` (`Action`),
  KEY `IX_AuditLogs_CreatedAt` (`CreatedAt`),
  KEY `IX_AuditLogs_UserId` (`UserId`),
  CONSTRAINT `FK_AuditLogs_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=26 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `auditlogs`
--

LOCK TABLES `auditlogs` WRITE;
/*!40000 ALTER TABLE `auditlogs` DISABLE KEYS */;
INSERT INTO `auditlogs` VALUES (1,NULL,'POST /api/auth/login','::1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0',NULL,'2026-06-23 02:03:42.025251'),(2,3,'POST /api/groups','::1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0',NULL,'2026-06-23 02:06:27.902591'),(3,NULL,'POST /api/auth/login','::1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0',NULL,'2026-06-23 15:55:40.431695'),(4,NULL,'POST /api/auth/login','::1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0',NULL,'2026-06-23 17:06:26.930266'),(5,NULL,'POST /api/auth/login','::1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0',NULL,'2026-06-23 17:06:59.172464'),(6,NULL,'POST /api/auth/login','::1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0',NULL,'2026-06-23 17:07:49.469554'),(7,NULL,'POST /api/auth/login','::1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0',NULL,'2026-06-23 17:32:11.110078'),(8,NULL,'POST /api/auth/login','::1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0',NULL,'2026-06-23 17:32:30.501283'),(9,NULL,'POST /api/auth/login','::1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0',NULL,'2026-06-23 17:36:40.205663'),(10,NULL,'POST /api/auth/login','::1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0',NULL,'2026-06-23 17:42:20.822629'),(11,NULL,'POST /api/auth/login','::1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0',NULL,'2026-06-23 17:43:30.925422'),(12,NULL,'POST /api/auth/login','::1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0',NULL,'2026-06-23 17:47:19.590716'),(13,NULL,'POST /api/auth/login','::1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0',NULL,'2026-06-23 17:47:35.487581'),(14,NULL,'POST /api/auth/login','::1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0',NULL,'2026-06-23 17:47:42.242435'),(15,NULL,'POST /api/auth/login','::1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0',NULL,'2026-06-23 17:55:31.575600'),(16,NULL,'POST /api/auth/login','::1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0',NULL,'2026-06-23 17:56:58.069953'),(17,NULL,'POST /api/auth/login','::1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0',NULL,'2026-06-23 18:17:33.007384'),(18,3,'POST /api/groups','::1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36',NULL,'2026-06-25 01:53:06.335730'),(19,3,'DELETE /api/groups/2/members/4','::1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36',NULL,'2026-06-25 01:54:05.668062'),(20,3,'POST /api/groups/2/members','::1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36',NULL,'2026-06-25 01:54:13.589369'),(21,3,'DELETE /api/groups/2','::1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36',NULL,'2026-06-25 02:26:36.432397'),(22,3,'DELETE /api/groups/1','::1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36',NULL,'2026-06-25 02:27:06.064123'),(23,3,'POST /api/groups','::1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36',NULL,'2026-06-25 02:30:31.142674'),(24,3,'DELETE /api/groups/3','::1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36',NULL,'2026-06-25 02:30:54.140605'),(25,3,'POST /api/groups','127.0.0.1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36',NULL,'2026-06-25 02:46:05.557975');
/*!40000 ALTER TABLE `auditlogs` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `blockedusers`
--

DROP TABLE IF EXISTS `blockedusers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `blockedusers` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `BlockerUserId` int NOT NULL,
  `BlockedUserId` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_BlockedUsers_BlockerUserId_BlockedUserId` (`BlockerUserId`,`BlockedUserId`),
  KEY `IX_BlockedUsers_BlockedUserId` (`BlockedUserId`),
  CONSTRAINT `FK_BlockedUsers_Users_BlockedUserId` FOREIGN KEY (`BlockedUserId`) REFERENCES `users` (`Id`),
  CONSTRAINT `FK_BlockedUsers_Users_BlockerUserId` FOREIGN KEY (`BlockerUserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `blockedusers`
--

LOCK TABLES `blockedusers` WRITE;
/*!40000 ALTER TABLE `blockedusers` DISABLE KEYS */;
/*!40000 ALTER TABLE `blockedusers` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `chatreminders`
--

DROP TABLE IF EXISTS `chatreminders`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `chatreminders` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Text` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `RemindAtUtc` datetime(6) NOT NULL,
  `Status` varchar(30) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `IsSent` tinyint(1) NOT NULL,
  `CreatedByUserId` int NOT NULL,
  `GroupId` int DEFAULT NULL,
  `PeerUserId` int DEFAULT NULL,
  `SourceMessageId` int DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `SentAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_ChatReminders_CreatedByUserId` (`CreatedByUserId`),
  KEY `IX_ChatReminders_GroupId` (`GroupId`),
  KEY `IX_ChatReminders_PeerUserId` (`PeerUserId`),
  KEY `IX_ChatReminders_RemindAtUtc` (`RemindAtUtc`),
  KEY `IX_ChatReminders_SourceMessageId` (`SourceMessageId`),
  CONSTRAINT `FK_ChatReminders_Groups_GroupId` FOREIGN KEY (`GroupId`) REFERENCES `groups` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_ChatReminders_Messages_SourceMessageId` FOREIGN KEY (`SourceMessageId`) REFERENCES `messages` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_ChatReminders_Users_CreatedByUserId` FOREIGN KEY (`CreatedByUserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_ChatReminders_Users_PeerUserId` FOREIGN KEY (`PeerUserId`) REFERENCES `users` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `chatreminders`
--

LOCK TABLES `chatreminders` WRITE;
/*!40000 ALTER TABLE `chatreminders` DISABLE KEYS */;
/*!40000 ALTER TABLE `chatreminders` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `codesnippets`
--

DROP TABLE IF EXISTS `codesnippets`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `codesnippets` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `MessageId` int DEFAULT NULL,
  `Title` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Content` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Language` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ShareToken` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `ExpiresAt` datetime(6) DEFAULT NULL,
  `IsRevoked` tinyint(1) NOT NULL DEFAULT '0',
  `AccessCount` int NOT NULL DEFAULT '0',
  `LastAccessedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_CodeSnippets_ShareToken` (`ShareToken`),
  KEY `IX_CodeSnippets_MessageId` (`MessageId`),
  KEY `IX_CodeSnippets_UserId` (`UserId`),
  KEY `IX_CodeSnippets_ExpiresAt` (`ExpiresAt`),
  CONSTRAINT `FK_CodeSnippets_Messages_MessageId` FOREIGN KEY (`MessageId`) REFERENCES `messages` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_CodeSnippets_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `codesnippets`
--

LOCK TABLES `codesnippets` WRITE;
/*!40000 ALTER TABLE `codesnippets` DISABLE KEYS */;
INSERT INTO `codesnippets` VALUES (1,3,NULL,'ádsadadsad','ád','csharp','b82d1b11977941828dd9715a39bcf5d8','2026-06-24 20:00:44.537365','2026-06-24 20:00:44.537397',NULL,0,0,NULL);
/*!40000 ALTER TABLE `codesnippets` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `conversationbackgrounds`
--

DROP TABLE IF EXISTS `conversationbackgrounds`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `conversationbackgrounds` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `PeerUserId` int DEFAULT NULL,
  `GroupId` int DEFAULT NULL,
  `BackgroundType` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `BackgroundValue` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `GroupId1` int DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_ConversationBackgrounds_GroupId` (`GroupId`),
  KEY `IX_ConversationBackgrounds_GroupId1` (`GroupId1`),
  KEY `IX_ConversationBackgrounds_PeerUserId` (`PeerUserId`),
  KEY `IX_ConversationBackgrounds_UserId_PeerUserId_GroupId` (`UserId`,`PeerUserId`,`GroupId`),
  CONSTRAINT `FK_ConversationBackgrounds_Groups_GroupId` FOREIGN KEY (`GroupId`) REFERENCES `groups` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_ConversationBackgrounds_Groups_GroupId1` FOREIGN KEY (`GroupId1`) REFERENCES `groups` (`Id`),
  CONSTRAINT `FK_ConversationBackgrounds_Users_PeerUserId` FOREIGN KEY (`PeerUserId`) REFERENCES `users` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_ConversationBackgrounds_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `conversationbackgrounds`
--

LOCK TABLES `conversationbackgrounds` WRITE;
/*!40000 ALTER TABLE `conversationbackgrounds` DISABLE KEYS */;
/*!40000 ALTER TABLE `conversationbackgrounds` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `conversationnicknames`
--

DROP TABLE IF EXISTS `conversationnicknames`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `conversationnicknames` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `SetByUserId` int NOT NULL,
  `TargetUserId` int NOT NULL,
  `GroupId` int DEFAULT NULL,
  `Nickname` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `GroupId1` int DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_ConversationNicknames_SetByUserId_TargetUserId_GroupId` (`SetByUserId`,`TargetUserId`,`GroupId`),
  KEY `IX_ConversationNicknames_GroupId` (`GroupId`),
  KEY `IX_ConversationNicknames_GroupId1` (`GroupId1`),
  KEY `IX_ConversationNicknames_TargetUserId` (`TargetUserId`),
  CONSTRAINT `FK_ConversationNicknames_Groups_GroupId` FOREIGN KEY (`GroupId`) REFERENCES `groups` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_ConversationNicknames_Groups_GroupId1` FOREIGN KEY (`GroupId1`) REFERENCES `groups` (`Id`),
  CONSTRAINT `FK_ConversationNicknames_Users_SetByUserId` FOREIGN KEY (`SetByUserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_ConversationNicknames_Users_TargetUserId` FOREIGN KEY (`TargetUserId`) REFERENCES `users` (`Id`)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `conversationnicknames`
--

LOCK TABLES `conversationnicknames` WRITE;
/*!40000 ALTER TABLE `conversationnicknames` DISABLE KEYS */;
/*!40000 ALTER TABLE `conversationnicknames` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `friendrequests`
--

DROP TABLE IF EXISTS `friendrequests`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `friendrequests` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `FromUserId` int NOT NULL,
  `ToUserId` int NOT NULL,
  `Status` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_FriendRequests_FromUserId_ToUserId` (`FromUserId`,`ToUserId`)
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `friendrequests`
--

LOCK TABLES `friendrequests` WRITE;
/*!40000 ALTER TABLE `friendrequests` DISABLE KEYS */;
INSERT INTO `friendrequests` VALUES (1,4,3,1,'2026-06-23 02:05:36.564896');
/*!40000 ALTER TABLE `friendrequests` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `friendships`
--

DROP TABLE IF EXISTS `friendships`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `friendships` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserAId` int NOT NULL,
  `UserBId` int NOT NULL,
  `Status` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Friendships_UserAId_UserBId` (`UserAId`,`UserBId`),
  KEY `IX_Friendships_UserBId` (`UserBId`),
  CONSTRAINT `FK_Friendships_Users_UserAId` FOREIGN KEY (`UserAId`) REFERENCES `users` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_Friendships_Users_UserBId` FOREIGN KEY (`UserBId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `friendships`
--

LOCK TABLES `friendships` WRITE;
/*!40000 ALTER TABLE `friendships` DISABLE KEYS */;
INSERT INTO `friendships` VALUES (1,3,4,1,'2026-06-23 02:05:45.950053',NULL);
/*!40000 ALTER TABLE `friendships` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `groupmembers`
--

DROP TABLE IF EXISTS `groupmembers`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `groupmembers` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `GroupId` int NOT NULL,
  `UserId` int NOT NULL,
  `Role` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL DEFAULT (_utf8mb4'Member'),
  `JoinedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `ClearedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_GroupMembers_GroupId_UserId` (`GroupId`,`UserId`),
  KEY `IX_GroupMembers_UserId` (`UserId`),
  CONSTRAINT `FK_GroupMembers_Groups_GroupId` FOREIGN KEY (`GroupId`) REFERENCES `groups` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_GroupMembers_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=10 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `groupmembers`
--

LOCK TABLES `groupmembers` WRITE;
/*!40000 ALTER TABLE `groupmembers` DISABLE KEYS */;
INSERT INTO `groupmembers` VALUES (1,1,3,'Admin','2026-06-23 02:06:27.478387',NULL),(2,1,4,'Member','2026-06-23 02:06:27.478707',NULL),(3,2,3,'Admin','2026-06-25 01:53:06.239037','2026-06-25 01:57:56.428220'),(5,2,4,'CoAdmin','2026-06-25 01:54:13.553102','2026-06-25 02:26:10.977447'),(6,3,3,'Admin','2026-06-25 02:30:30.955857',NULL),(7,3,4,'Member','2026-06-25 02:30:30.955971',NULL),(8,4,3,'Admin','2026-06-25 02:46:05.415858',NULL),(9,4,4,'Member','2026-06-25 02:46:05.415943',NULL);
/*!40000 ALTER TABLE `groupmembers` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `groups`
--

DROP TABLE IF EXISTS `groups`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `groups` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Description` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `AvatarUrl` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CreatedByUserId` int NOT NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT '0',
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  KEY `IX_Groups_CreatedByUserId` (`CreatedByUserId`),
  CONSTRAINT `FK_Groups_Users_CreatedByUserId` FOREIGN KEY (`CreatedByUserId`) REFERENCES `users` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `groups`
--

LOCK TABLES `groups` WRITE;
/*!40000 ALTER TABLE `groups` DISABLE KEYS */;
INSERT INTO `groups` VALUES (1,'ádadsad','ádad',NULL,3,1,'2026-06-23 02:06:27.384241','2026-06-25 02:27:06.053969'),(2,'aaaaaaaaaaaaádadadadd','',NULL,3,1,'2026-06-25 01:53:06.211611','2026-06-25 02:26:36.402654'),(3,'n','',NULL,3,1,'2026-06-25 02:30:30.860428','2026-06-25 02:30:54.128915'),(4,'fadsfsadfd','',NULL,3,0,'2026-06-25 02:46:05.320350','2026-06-25 02:46:05.320379');
/*!40000 ALTER TABLE `groups` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `grouptasks`
--

DROP TABLE IF EXISTS `grouptasks`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `grouptasks` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `GroupId` int NOT NULL,
  `Title` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Description` varchar(1000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Priority` varchar(20) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Status` varchar(30) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `DueAtUtc` datetime(6) DEFAULT NULL,
  `CreatedByUserId` int NOT NULL,
  `AssignedToUserId` int DEFAULT NULL,
  `SourceMessageId` int DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `CompletedAt` datetime(6) DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_GroupTasks_AssignedToUserId` (`AssignedToUserId`),
  KEY `IX_GroupTasks_CreatedByUserId` (`CreatedByUserId`),
  KEY `IX_GroupTasks_DueAtUtc` (`DueAtUtc`),
  KEY `IX_GroupTasks_GroupId` (`GroupId`),
  KEY `IX_GroupTasks_Priority` (`Priority`),
  KEY `IX_GroupTasks_SourceMessageId` (`SourceMessageId`),
  KEY `IX_GroupTasks_Status` (`Status`),
  CONSTRAINT `FK_GroupTasks_Groups_GroupId` FOREIGN KEY (`GroupId`) REFERENCES `groups` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_GroupTasks_Messages_SourceMessageId` FOREIGN KEY (`SourceMessageId`) REFERENCES `messages` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_GroupTasks_Users_AssignedToUserId` FOREIGN KEY (`AssignedToUserId`) REFERENCES `users` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_GroupTasks_Users_CreatedByUserId` FOREIGN KEY (`CreatedByUserId`) REFERENCES `users` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `grouptasks`
--

LOCK TABLES `grouptasks` WRITE;
/*!40000 ALTER TABLE `grouptasks` DISABLE KEYS */;
/*!40000 ALTER TABLE `grouptasks` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `iocindicators`
--

DROP TABLE IF EXISTS `iocindicators`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `iocindicators` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Type` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Value` varchar(2048) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Description` varchar(4000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Severity` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Status` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Source` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Tags` varchar(512) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `CreatedByUserId` int NOT NULL,
  `MessageId` int DEFAULT NULL,
  `PeerUserId` int DEFAULT NULL,
  `GroupId` int DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `ResolvedAt` datetime(6) DEFAULT NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`Id`),
  KEY `IX_IocIndicators_Type` (`Type`),
  KEY `IX_IocIndicators_Severity` (`Severity`),
  KEY `IX_IocIndicators_Status` (`Status`),
  KEY `IX_IocIndicators_GroupId` (`GroupId`),
  KEY `IX_IocIndicators_PeerUserId` (`PeerUserId`),
  KEY `IX_IocIndicators_MessageId` (`MessageId`),
  KEY `IX_IocIndicators_CreatedByUserId` (`CreatedByUserId`)
) ENGINE=InnoDB AUTO_INCREMENT=11 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `iocindicators`
--

LOCK TABLES `iocindicators` WRITE;
/*!40000 ALTER TABLE `iocindicators` DISABLE KEYS */;
INSERT INTO `iocindicators` VALUES (1,'IP','8.6.7.8',NULL,'Medium','Resolved','ChatCommand',NULL,3,18,4,NULL,'2026-06-24 18:49:25.253019','2026-06-24 19:15:57.099172','2026-06-24 19:15:57.099171',0),(2,'URL','https://coccoc.com/search?query=aaaaa&source=chrome.ob',NULL,'Medium','Resolved','ChatCommand',NULL,3,19,4,NULL,'2026-06-24 18:50:18.568073','2026-06-24 19:15:48.420848','2026-06-24 19:15:48.420848',1),(5,'IP','7.9.0.7',NULL,'Medium','Open','ChatCommand',NULL,3,21,NULL,1,'2026-06-24 19:49:22.991770','2026-06-24 19:49:22.991790',NULL,1),(7,'IP','3.3.3.3',NULL,'Medium','Open','ChatCommand',NULL,3,41,NULL,4,'2026-06-25 02:46:19.343777','2026-06-25 02:46:19.343796',NULL,0),(8,'URL','https://coccoc.com/search?query=aaaaa&source=chrome.ob',NULL,'Medium','Open','ChatCommand',NULL,3,42,NULL,4,'2026-06-25 02:51:44.544444','2026-06-25 02:51:44.544444',NULL,0),(9,'IP','1.2.4.1',NULL,'Medium','Open','ChatCommand',NULL,3,43,NULL,4,'2026-06-25 03:00:34.130538','2026-06-25 03:00:34.130551',NULL,0),(10,'IP','2.3.1.1',NULL,'Medium','Open','ChatCommand',NULL,3,44,NULL,4,'2026-06-25 03:08:44.726113','2026-06-25 03:08:44.726129',NULL,0);
/*!40000 ALTER TABLE `iocindicators` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `messageattachments`
--

DROP TABLE IF EXISTS `messageattachments`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `messageattachments` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `MessageId` int NOT NULL,
  `FileName` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `FileUrl` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `FileSize` bigint NOT NULL,
  `MimeType` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  KEY `IX_MessageAttachments_MessageId` (`MessageId`),
  CONSTRAINT `FK_MessageAttachments_Messages_MessageId` FOREIGN KEY (`MessageId`) REFERENCES `messages` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `messageattachments`
--

LOCK TABLES `messageattachments` WRITE;
/*!40000 ALTER TABLE `messageattachments` DISABLE KEYS */;
INSERT INTO `messageattachments` VALUES (1,33,'Networking-Fundamentals-For-Soc-Analyst-2.pdf','https://localhost:5001/uploads/messages/20260625/1ea53718811a4831aaf2198984c5f3dc_Networking-Fundamentals-For-Soc-Analyst-2.pdf',23509364,'application/pdf','2026-06-25 02:20:18.518351'),(2,36,'Networking-Fundamentals-For-Soc-Analyst-2.pdf','https://localhost:5001/uploads/messages/20260625/5bf57f6003274abd8772718c985abc97_Networking-Fundamentals-For-Soc-Analyst-2.pdf',23509364,'application/pdf','2026-06-25 02:29:09.645145'),(3,39,'5bf57f6003274abd8772718c985abc97_Networking-Fundamentals-For-Soc-Analyst-2.pdf','https://localhost:5001/uploads/messages/20260625/e6c7e004f0c24127ac0867063c8c8ee6_5bf57f6003274abd8772718c985abc97_Networking-Fundamentals-For-Soc-Analyst-2.pdf',23509364,'application/pdf','2026-06-25 02:31:42.126001');
/*!40000 ALTER TABLE `messageattachments` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `messageedithistories`
--

DROP TABLE IF EXISTS `messageedithistories`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `messageedithistories` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `MessageId` int NOT NULL,
  `OldContent` varchar(4000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `NewContent` varchar(4000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `EditedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  KEY `IX_MessageEditHistories_MessageId` (`MessageId`),
  CONSTRAINT `FK_MessageEditHistories_Messages_MessageId` FOREIGN KEY (`MessageId`) REFERENCES `messages` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `messageedithistories`
--

LOCK TABLES `messageedithistories` WRITE;
/*!40000 ALTER TABLE `messageedithistories` DISABLE KEYS */;
/*!40000 ALTER TABLE `messageedithistories` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `messagereactions`
--

DROP TABLE IF EXISTS `messagereactions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `messagereactions` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `MessageId` int NOT NULL,
  `UserId` int NOT NULL,
  `Emoji` varchar(10) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_MessageReactions_MessageId_UserId_Emoji` (`MessageId`,`UserId`,`Emoji`),
  KEY `IX_MessageReactions_UserId` (`UserId`),
  CONSTRAINT `FK_MessageReactions_Messages_MessageId` FOREIGN KEY (`MessageId`) REFERENCES `messages` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_MessageReactions_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `messagereactions`
--

LOCK TABLES `messagereactions` WRITE;
/*!40000 ALTER TABLE `messagereactions` DISABLE KEYS */;
/*!40000 ALTER TABLE `messagereactions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `messagereadreceipts`
--

DROP TABLE IF EXISTS `messagereadreceipts`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `messagereadreceipts` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `MessageId` int NOT NULL,
  `UserId` int NOT NULL,
  `ReadAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_MessageReadReceipts_MessageId_UserId` (`MessageId`,`UserId`),
  KEY `IX_MessageReadReceipts_UserId` (`UserId`),
  CONSTRAINT `FK_MessageReadReceipts_Messages_MessageId` FOREIGN KEY (`MessageId`) REFERENCES `messages` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_MessageReadReceipts_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=83 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `messagereadreceipts`
--

LOCK TABLES `messagereadreceipts` WRITE;
/*!40000 ALTER TABLE `messagereadreceipts` DISABLE KEYS */;
INSERT INTO `messagereadreceipts` VALUES (1,2,3,'2026-06-23 18:19:56.158559'),(2,1,3,'2026-06-23 18:19:56.725991'),(3,3,3,'2026-06-23 20:00:15.262781'),(4,4,3,'2026-06-23 20:00:18.846739'),(5,5,3,'2026-06-23 20:38:38.757885'),(6,6,3,'2026-06-23 21:04:04.327977'),(7,6,4,'2026-06-24 09:25:54.340382'),(8,7,4,'2026-06-24 09:25:57.701039'),(9,7,3,'2026-06-24 09:26:02.923445'),(10,9,3,'2026-06-24 17:02:55.166579'),(11,8,3,'2026-06-24 17:10:10.997059'),(12,9,4,'2026-06-24 18:01:15.608181'),(13,10,3,'2026-06-24 18:13:53.477897'),(14,10,4,'2026-06-24 18:13:53.479504'),(15,11,4,'2026-06-24 18:14:02.009725'),(16,11,3,'2026-06-24 18:14:06.422908'),(17,12,3,'2026-06-24 18:14:13.335510'),(18,12,4,'2026-06-24 18:14:13.336028'),(19,13,4,'2026-06-24 18:14:18.078996'),(20,13,3,'2026-06-24 18:25:23.686135'),(21,14,4,'2026-06-24 18:25:57.176910'),(22,14,3,'2026-06-24 18:25:57.176910'),(23,15,4,'2026-06-24 18:26:10.132964'),(24,15,3,'2026-06-24 18:26:19.917959'),(25,8,4,'2026-06-24 18:29:00.536875'),(26,16,4,'2026-06-24 18:29:04.668430'),(27,16,3,'2026-06-24 18:38:27.442603'),(28,17,4,'2026-06-24 18:38:51.670523'),(29,17,3,'2026-06-24 18:39:05.827407'),(30,18,3,'2026-06-24 18:49:25.296265'),(31,18,4,'2026-06-24 18:49:33.546282'),(32,19,3,'2026-06-24 18:50:18.581945'),(33,19,4,'2026-06-24 18:50:18.581945'),(34,1,4,'2026-06-24 19:02:32.703474'),(35,3,4,'2026-06-24 19:02:32.703606'),(36,20,4,'2026-06-24 19:03:29.672905'),(37,2,4,'2026-06-24 19:03:46.441070'),(38,4,4,'2026-06-24 19:03:46.441175'),(39,5,4,'2026-06-24 19:03:46.441295'),(40,20,3,'2026-06-24 19:06:43.055803'),(41,21,3,'2026-06-24 19:49:23.077302'),(42,22,3,'2026-06-24 19:50:02.061257'),(43,21,4,'2026-06-25 01:46:59.362935'),(44,22,4,'2026-06-25 01:46:59.373084'),(45,23,3,'2026-06-25 01:53:07.750326'),(46,24,3,'2026-06-25 01:53:10.260417'),(47,23,4,'2026-06-25 01:53:17.609753'),(48,24,4,'2026-06-25 01:53:17.609914'),(49,25,3,'2026-06-25 01:53:46.265457'),(50,26,3,'2026-06-25 01:53:53.431965'),(51,27,3,'2026-06-25 01:54:02.165132'),(52,28,3,'2026-06-25 01:54:03.524213'),(53,29,3,'2026-06-25 01:54:05.670197'),(54,30,3,'2026-06-25 01:54:13.592948'),(55,25,4,'2026-06-25 01:54:32.026773'),(56,26,4,'2026-06-25 01:54:32.026901'),(57,27,4,'2026-06-25 01:54:32.026931'),(58,28,4,'2026-06-25 01:54:32.026972'),(59,29,4,'2026-06-25 01:54:32.026992'),(60,30,4,'2026-06-25 01:54:32.027006'),(61,31,4,'2026-06-25 01:54:38.589601'),(62,31,3,'2026-06-25 01:54:50.277563'),(63,32,3,'2026-06-25 01:55:10.588523'),(64,32,4,'2026-06-25 01:55:10.868883'),(65,33,4,'2026-06-25 02:20:18.626054'),(66,33,3,'2026-06-25 02:20:18.627643'),(67,34,4,'2026-06-25 02:25:43.277322'),(68,34,3,'2026-06-25 02:25:43.274541'),(69,35,3,'2026-06-25 02:26:18.617064'),(70,35,4,'2026-06-25 02:26:18.617062'),(71,36,4,'2026-06-25 02:29:09.706940'),(72,36,3,'2026-06-25 02:29:17.791827'),(73,37,3,'2026-06-25 02:30:32.514272'),(74,38,3,'2026-06-25 02:30:35.318276'),(75,37,4,'2026-06-25 02:30:45.391468'),(76,38,4,'2026-06-25 02:30:45.391868'),(77,39,3,'2026-06-25 02:31:42.170026'),(78,40,3,'2026-06-25 02:46:07.891579'),(79,41,3,'2026-06-25 02:46:19.427374'),(80,42,3,'2026-06-25 02:51:44.568574'),(81,43,3,'2026-06-25 03:00:34.220055'),(82,44,3,'2026-06-25 03:08:44.817123');
/*!40000 ALTER TABLE `messagereadreceipts` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `messages`
--

DROP TABLE IF EXISTS `messages`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `messages` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `SenderId` int NOT NULL,
  `GroupId` int DEFAULT NULL,
  `ReceiverId` int DEFAULT NULL,
  `Content` varchar(4000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `MessageType` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL DEFAULT (_utf8mb4'Text'),
  `ReplyToMessageId` int DEFAULT NULL,
  `ForwardedFromMessageId` int DEFAULT NULL,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT '0',
  `IsEdited` tinyint(1) NOT NULL DEFAULT '0',
  `IsPinned` tinyint(1) NOT NULL DEFAULT '0',
  `ExpiresAt` datetime(6) DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `SnippetId` int DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_Messages_CreatedAt` (`CreatedAt`),
  KEY `IX_Messages_ExpiresAt` (`ExpiresAt`),
  KEY `IX_Messages_ForwardedFromMessageId` (`ForwardedFromMessageId`),
  KEY `IX_Messages_GroupId` (`GroupId`),
  KEY `IX_Messages_ReceiverId` (`ReceiverId`),
  KEY `IX_Messages_ReplyToMessageId` (`ReplyToMessageId`),
  KEY `IX_Messages_SenderId` (`SenderId`),
  KEY `IX_Messages_SnippetId` (`SnippetId`),
  KEY `IX_Messages_IsDeleted` (`IsDeleted`),
  KEY `IX_Messages_ReceiverId_GroupId` (`ReceiverId`,`GroupId`),
  KEY `IX_Messages_SenderId_ReceiverId_GroupId` (`SenderId`,`ReceiverId`,`GroupId`),
  CONSTRAINT `FK_Messages_CodeSnippets_SnippetId` FOREIGN KEY (`SnippetId`) REFERENCES `codesnippets` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_Messages_Groups_GroupId` FOREIGN KEY (`GroupId`) REFERENCES `groups` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_Messages_Messages_ForwardedFromMessageId` FOREIGN KEY (`ForwardedFromMessageId`) REFERENCES `messages` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_Messages_Messages_ReplyToMessageId` FOREIGN KEY (`ReplyToMessageId`) REFERENCES `messages` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_Messages_Users_ReceiverId` FOREIGN KEY (`ReceiverId`) REFERENCES `users` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_Messages_Users_SenderId` FOREIGN KEY (`SenderId`) REFERENCES `users` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=45 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `messages`
--

LOCK TABLES `messages` WRITE;
/*!40000 ALTER TABLE `messages` DISABLE KEYS */;
INSERT INTO `messages` VALUES (1,3,NULL,4,'ádasdadsa','Text',NULL,NULL,0,0,0,NULL,'2026-06-23 02:06:10.091622','2026-06-23 02:06:10.091691',NULL),(2,3,1,NULL,'huyday đã tạo nhóm ádadsad','System',NULL,NULL,0,0,0,NULL,'2026-06-23 02:06:27.647229','2026-06-23 02:06:27.647229',NULL),(3,3,NULL,4,'gfhfghf','Text',NULL,NULL,0,0,0,NULL,'2026-06-23 20:00:15.144887','2026-06-23 20:00:15.144925',NULL),(4,3,1,NULL,'jtyjyj','Text',NULL,NULL,0,0,0,NULL,'2026-06-23 20:00:18.808885','2026-06-23 20:00:18.808885',NULL),(5,3,1,NULL,'sgsgfdfgd','Text',NULL,NULL,0,0,0,NULL,'2026-06-23 20:38:38.597981','2026-06-23 20:38:38.598004',NULL),(6,3,NULL,4,'ádasda','Text',NULL,NULL,0,0,0,NULL,'2026-06-23 21:04:04.246126','2026-06-23 21:04:04.246149',NULL),(7,4,NULL,3,'helllo','Text',NULL,NULL,0,0,0,NULL,'2026-06-24 09:25:57.617114','2026-06-24 09:25:57.617127',NULL),(8,3,1,NULL,'helllo','Text',NULL,7,0,0,0,NULL,'2026-06-24 17:02:51.238751','2026-06-24 17:02:51.238767',NULL),(9,3,NULL,4,'s','Text',7,NULL,0,0,0,NULL,'2026-06-24 17:02:55.148975','2026-06-24 17:02:55.148975',NULL),(10,4,NULL,3,'s','Text',NULL,NULL,0,0,0,NULL,'2026-06-24 18:13:53.409288','2026-06-24 18:13:53.409306',NULL),(11,4,NULL,3,'s','Text',NULL,NULL,0,0,0,NULL,'2026-06-24 18:14:01.992818','2026-06-24 18:14:01.992818',NULL),(12,4,NULL,3,'ádasdsad','Text',NULL,NULL,0,0,0,NULL,'2026-06-24 18:14:13.319255','2026-06-24 18:14:13.319255',NULL),(13,4,NULL,3,'ádadasđ','Text',NULL,NULL,0,0,0,NULL,'2026-06-24 18:14:18.061932','2026-06-24 18:14:18.061932',NULL),(14,3,NULL,4,'fssfsfs','Text',NULL,NULL,0,0,0,NULL,'2026-06-24 18:25:57.101706','2026-06-24 18:25:57.101734',NULL),(15,4,NULL,3,'ádasdsa','Text',NULL,NULL,0,0,0,NULL,'2026-06-24 18:26:10.114248','2026-06-24 18:26:10.114248',NULL),(16,4,1,NULL,'adsasđsa','Text',NULL,NULL,0,0,0,NULL,'2026-06-24 18:29:04.645541','2026-06-24 18:29:04.645542',NULL),(17,4,1,NULL,'ádsaas','Text',NULL,NULL,0,0,0,NULL,'2026-06-24 18:38:51.657878','2026-06-24 18:38:51.657878',NULL),(18,3,NULL,4,'/ioc ip 8.6.7.8','Text',NULL,NULL,0,0,0,NULL,'2026-06-24 18:49:25.242629','2026-06-24 18:49:25.242629',NULL),(19,3,NULL,4,'/ioc url https://coccoc.com/search?query=aaaaa&source=chrome.ob','Text',NULL,NULL,0,0,0,NULL,'2026-06-24 18:50:18.563668','2026-06-24 18:50:18.563668',NULL),(20,4,NULL,3,'/ioc ip 2.1.2.2','Text',NULL,NULL,0,0,0,NULL,'2026-06-24 19:03:29.646257','2026-06-24 19:03:29.646257',NULL),(21,3,1,NULL,'/ioc ip 7.9.0.7','Text',NULL,NULL,0,0,0,NULL,'2026-06-24 19:49:22.889908','2026-06-24 19:49:22.889929',NULL),(22,3,1,NULL,'/ioc url https://coccoc.com/search?query=aaaaa&source=chrome.ob','Text',NULL,NULL,0,0,0,NULL,'2026-06-24 19:50:02.029102','2026-06-24 19:50:02.029102',NULL),(23,3,2,NULL,'huyday đã tạo nhóm aaaaaaaaaaaa','System',NULL,NULL,0,0,0,NULL,'2026-06-25 01:53:06.264761','2026-06-25 01:53:06.264778',NULL),(24,3,2,NULL,'ádadsadsaddad','Text',NULL,NULL,0,0,0,NULL,'2026-06-25 01:53:10.186088','2026-06-25 01:53:10.186088',NULL),(25,3,2,NULL,'aaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaaa','Text',NULL,NULL,0,0,0,NULL,'2026-06-25 01:53:46.241603','2026-06-25 01:53:46.241603',NULL),(26,3,2,NULL,'huyday đã đổi tên nhóm thành aaaaaaaaaaaaádadadadd','System',NULL,NULL,0,0,0,NULL,'2026-06-25 01:53:53.415982','2026-06-25 01:53:53.415982',NULL),(27,3,2,NULL,'huyday đã đổi vai trò khoan thành CoAdmin','System',NULL,NULL,0,0,0,NULL,'2026-06-25 01:54:02.151138','2026-06-25 01:54:02.151138',NULL),(28,3,2,NULL,'huyday đã đổi vai trò khoan thành Member','System',NULL,NULL,0,0,0,NULL,'2026-06-25 01:54:03.509330','2026-06-25 01:54:03.509330',NULL),(29,3,2,NULL,'huyday đã xóa khoan khỏi nhóm','System',NULL,NULL,0,0,0,NULL,'2026-06-25 01:54:05.655572','2026-06-25 01:54:05.655572',NULL),(30,3,2,NULL,'huyday đã thêm khoan vào nhóm','System',NULL,NULL,0,0,0,NULL,'2026-06-25 01:54:13.573618','2026-06-25 01:54:13.573618',NULL),(31,4,2,NULL,'ấdasdsa','Text',NULL,NULL,0,0,0,NULL,'2026-06-25 01:54:38.562511','2026-06-25 01:54:38.562511',NULL),(32,3,2,NULL,'áddadsd','Text',NULL,NULL,0,0,0,NULL,'2026-06-25 01:55:10.492279','2026-06-25 01:55:10.492280',NULL),(33,3,2,NULL,'Networking-Fundamentals-For-Soc-Analyst-2.pdf','File',NULL,NULL,0,0,0,NULL,'2026-06-25 02:20:18.389270','2026-06-25 02:20:18.389292',NULL),(34,3,2,NULL,'huyday đã đổi vai trò khoan thành CoAdmin','System',NULL,NULL,0,0,0,NULL,'2026-06-25 02:25:43.135897','2026-06-25 02:25:43.135915',NULL),(35,4,2,NULL,'S','Text',NULL,NULL,0,0,0,NULL,'2026-06-25 02:26:18.541038','2026-06-25 02:26:18.541038',NULL),(36,4,NULL,3,'Networking-Fundamentals-For-Soc-Analyst-2.pdf','File',NULL,NULL,0,0,0,NULL,'2026-06-25 02:29:09.626920','2026-06-25 02:29:09.626920',NULL),(37,3,3,NULL,'huyday đã tạo nhóm n','System',NULL,NULL,0,0,0,NULL,'2026-06-25 02:30:31.010850','2026-06-25 02:30:31.010868',NULL),(38,3,3,NULL,'ádasdadad','Text',NULL,NULL,0,0,0,NULL,'2026-06-25 02:30:35.233783','2026-06-25 02:30:35.233783',NULL),(39,3,NULL,4,'5bf57f6003274abd8772718c985abc97_Networking-Fundamentals-For-Soc-Analyst-2.pdf','File',NULL,NULL,0,0,0,NULL,'2026-06-25 02:31:42.113732','2026-06-25 02:31:42.113732',NULL),(40,3,4,NULL,'huyday đã tạo nhóm fadsfsadfd','System',NULL,NULL,0,0,0,NULL,'2026-06-25 02:46:05.465991','2026-06-25 02:46:05.466011',NULL),(41,3,4,NULL,'/ioc ip 3.3.3.3','Text',NULL,NULL,0,0,0,NULL,'2026-06-25 02:46:19.326065','2026-06-25 02:46:19.326065',NULL),(42,3,4,NULL,'/ioc url https://coccoc.com/search?query=aaaaa&source=chrome.ob','Text',NULL,NULL,0,0,0,NULL,'2026-06-25 02:51:44.532581','2026-06-25 02:51:44.532581',NULL),(43,3,4,NULL,'/ioc ip 1.2.4.1','Text',NULL,NULL,0,0,0,NULL,'2026-06-25 03:00:34.023562','2026-06-25 03:00:34.023578',NULL),(44,3,4,NULL,'/ioc ip 2.3.1.1','Text',NULL,NULL,0,0,0,NULL,'2026-06-25 03:08:44.614977','2026-06-25 03:08:44.615005',NULL);
/*!40000 ALTER TABLE `messages` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `onetimesecrets`
--

DROP TABLE IF EXISTS `onetimesecrets`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `onetimesecrets` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `TokenHash` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `SecretCipherText` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `CreatedByUserId` int NOT NULL,
  `ExpiresAt` datetime(6) NOT NULL,
  `ViewedAt` datetime(6) DEFAULT NULL,
  `IsViewed` tinyint(1) NOT NULL DEFAULT '0',
  `IsRevoked` tinyint(1) NOT NULL DEFAULT '0',
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `ViewedByUserId` int DEFAULT NULL,
  `ViewedIpHash` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `ViewedUserAgent` varchar(512) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_OneTimeSecrets_TokenHash` (`TokenHash`),
  KEY `IX_OneTimeSecrets_CreatedByUserId_CreatedAt` (`CreatedByUserId`,`CreatedAt`),
  KEY `IX_OneTimeSecrets_ExpiresAt` (`ExpiresAt`),
  KEY `IX_OneTimeSecrets_ViewedByUserId` (`ViewedByUserId`),
  CONSTRAINT `FK_OneTimeSecrets_Users_CreatedByUserId` FOREIGN KEY (`CreatedByUserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_OneTimeSecrets_Users_ViewedByUserId` FOREIGN KEY (`ViewedByUserId`) REFERENCES `users` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB AUTO_INCREMENT=4 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `onetimesecrets`
--

LOCK TABLES `onetimesecrets` WRITE;
/*!40000 ALTER TABLE `onetimesecrets` DISABLE KEYS */;
INSERT INTO `onetimesecrets` VALUES (1,'381E039AF4E60CE85D289CEA0C0B004CD97354B569269F1F42FE595171177E8C','CfDJ8HHko51eYaJLlSVZmHs834-xboacCDa7Ut2t0qJTjafkO_QlFeE44F72_LscfRrXab4X0XbqHoAcdp2y3C_Sv_WOPeazrzyAqvtBcClDqzNHlkXjKR0xIQbqnVGMC2XYeA',3,'2026-06-25 06:50:19.673439','2026-06-25 06:45:27.440727',1,0,'2026-06-25 06:45:19.674322',3,'12CA17B49AF2289436F303E0166030A21E525D266E209267433801A8FD4071A0','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36'),(2,'0796E634FD2E8834E50F8EEE9DF11DD96432DFE8F23CAE2D7E5CCF7089210E2A','CfDJ8HHko51eYaJLlSVZmHs8348H84h2itY23iZKWy1vRTxn4PyT6HJX3L9Spkj2qwuMUVNHfj_TGav27MXjg83QQg0wBewZYvBaFSCHwfdzJIVWBIzzdz7Imk-zLM6XU2Kkpg',3,'2026-06-25 06:51:51.351484',NULL,0,1,'2026-06-25 06:46:51.351563',NULL,NULL,NULL),(3,'8576C12F13FC7877E56F3D24777E34EACB8CB5EF4F1B62205DCDF5B41513D839','CfDJ8HHko51eYaJLlSVZmHs83491AkCVRRsuSM6WQGxEzfRbuyqcW6UGMTdzczPwPVXocBnbwtAy984CC6RpgPe4wPlPPaB4j-3o8jCxSZYDM0ANn-qpiyOK8vnYoFzocLeSUg',3,'2026-06-25 06:58:37.274890',NULL,0,0,'2026-06-25 06:53:37.276163',NULL,NULL,NULL);
/*!40000 ALTER TABLE `onetimesecrets` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `passwordresetotps`
--

DROP TABLE IF EXISTS `passwordresetotps`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `passwordresetotps` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `OtpCode` varchar(6) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `ExpiresAt` datetime(6) NOT NULL,
  `IsUsed` tinyint(1) NOT NULL,
  `CreatedAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_PasswordResetOtps_OtpCode` (`OtpCode`),
  KEY `IX_PasswordResetOtps_UserId` (`UserId`),
  CONSTRAINT `FK_PasswordResetOtps_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=5 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `passwordresetotps`
--

LOCK TABLES `passwordresetotps` WRITE;
/*!40000 ALTER TABLE `passwordresetotps` DISABLE KEYS */;
INSERT INTO `passwordresetotps` VALUES (1,1,'630162','2026-06-22 17:14:46.730566',0,'2026-06-22 17:04:46.730635'),(2,2,'529394','2026-06-22 17:16:46.591093',0,'2026-06-22 17:06:46.591095'),(3,3,'931143','2026-06-22 17:18:03.131925',1,'2026-06-22 17:08:03.131926'),(4,4,'517194','2026-06-23 02:14:39.294833',1,'2026-06-23 02:04:39.294955');
/*!40000 ALTER TABLE `passwordresetotps` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `pentestfindings`
--

DROP TABLE IF EXISTS `pentestfindings`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `pentestfindings` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Title` varchar(200) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Severity` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Status` varchar(32) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Description` varchar(4000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `PoC` varchar(4000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Remediation` varchar(4000) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `EngagementId` int DEFAULT NULL,
  `GroupId` int NOT NULL,
  `CreatedByUserId` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) DEFAULT NULL,
  `AffectedEndpoint` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `AffectedTarget` varchar(300) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `HttpMethod` varchar(16) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `Payload` longtext CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci,
  `IsDeleted` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`Id`),
  KEY `IX_PentestFindings_CreatedAt` (`CreatedAt`),
  KEY `IX_PentestFindings_CreatedByUserId` (`CreatedByUserId`),
  KEY `IX_PentestFindings_GroupId` (`GroupId`),
  KEY `IX_PentestFindings_Severity` (`Severity`),
  KEY `IX_PentestFindings_Status` (`Status`),
  CONSTRAINT `FK_PentestFindings_Groups_GroupId` FOREIGN KEY (`GroupId`) REFERENCES `groups` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_PentestFindings_Users_CreatedByUserId` FOREIGN KEY (`CreatedByUserId`) REFERENCES `users` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB AUTO_INCREMENT=2 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `pentestfindings`
--

LOCK TABLES `pentestfindings` WRITE;
/*!40000 ALTER TABLE `pentestfindings` DISABLE KEYS */;
INSERT INTO `pentestfindings` VALUES (1,'2f','Medium','Open',NULL,NULL,NULL,NULL,1,3,'2026-06-24 20:01:29.144150','2026-06-24 20:01:29.144150',NULL,NULL,NULL,NULL,0);
/*!40000 ALTER TABLE `pentestfindings` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `pinnedconversations`
--

DROP TABLE IF EXISTS `pinnedconversations`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `pinnedconversations` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `PeerUserId` int DEFAULT NULL,
  `GroupId` int DEFAULT NULL,
  `PinnedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `GroupId1` int DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_PinnedConversations_GroupId` (`GroupId`),
  KEY `IX_PinnedConversations_GroupId1` (`GroupId1`),
  KEY `IX_PinnedConversations_PeerUserId` (`PeerUserId`),
  KEY `IX_PinnedConversations_UserId_PeerUserId_GroupId` (`UserId`,`PeerUserId`,`GroupId`),
  CONSTRAINT `FK_PinnedConversations_Groups_GroupId` FOREIGN KEY (`GroupId`) REFERENCES `groups` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_PinnedConversations_Groups_GroupId1` FOREIGN KEY (`GroupId1`) REFERENCES `groups` (`Id`),
  CONSTRAINT `FK_PinnedConversations_Users_PeerUserId` FOREIGN KEY (`PeerUserId`) REFERENCES `users` (`Id`) ON DELETE SET NULL,
  CONSTRAINT `FK_PinnedConversations_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `pinnedconversations`
--

LOCK TABLES `pinnedconversations` WRITE;
/*!40000 ALTER TABLE `pinnedconversations` DISABLE KEYS */;
/*!40000 ALTER TABLE `pinnedconversations` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `polloptions`
--

DROP TABLE IF EXISTS `polloptions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `polloptions` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `PollId` int NOT NULL,
  `Text` varchar(300) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Order` int NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_PollOptions_PollId_Order` (`PollId`,`Order`),
  CONSTRAINT `FK_PollOptions_Polls_PollId` FOREIGN KEY (`PollId`) REFERENCES `polls` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `polloptions`
--

LOCK TABLES `polloptions` WRITE;
/*!40000 ALTER TABLE `polloptions` DISABLE KEYS */;
/*!40000 ALTER TABLE `polloptions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `polls`
--

DROP TABLE IF EXISTS `polls`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `polls` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `MessageId` int NOT NULL,
  `AllowMultiple` tinyint(1) NOT NULL DEFAULT '0',
  `EndsAt` datetime(6) DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Polls_MessageId` (`MessageId`),
  CONSTRAINT `FK_Polls_Messages_MessageId` FOREIGN KEY (`MessageId`) REFERENCES `messages` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `polls`
--

LOCK TABLES `polls` WRITE;
/*!40000 ALTER TABLE `polls` DISABLE KEYS */;
/*!40000 ALTER TABLE `polls` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `pollvotes`
--

DROP TABLE IF EXISTS `pollvotes`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `pollvotes` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `PollOptionId` int NOT NULL,
  `UserId` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_PollVotes_PollOptionId_UserId` (`PollOptionId`,`UserId`),
  KEY `IX_PollVotes_UserId` (`UserId`),
  CONSTRAINT `FK_PollVotes_PollOptions_PollOptionId` FOREIGN KEY (`PollOptionId`) REFERENCES `polloptions` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_PollVotes_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `pollvotes`
--

LOCK TABLES `pollvotes` WRITE;
/*!40000 ALTER TABLE `pollvotes` DISABLE KEYS */;
/*!40000 ALTER TABLE `pollvotes` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `savedmessages`
--

DROP TABLE IF EXISTS `savedmessages`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `savedmessages` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `MessageId` int NOT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_SavedMessages_UserId_MessageId` (`UserId`,`MessageId`),
  KEY `IX_SavedMessages_MessageId` (`MessageId`),
  KEY `IX_SavedMessages_UserId_CreatedAt` (`UserId`,`CreatedAt`),
  CONSTRAINT `FK_SavedMessages_Messages_MessageId` FOREIGN KEY (`MessageId`) REFERENCES `messages` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_SavedMessages_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `savedmessages`
--

LOCK TABLES `savedmessages` WRITE;
/*!40000 ALTER TABLE `savedmessages` DISABLE KEYS */;
/*!40000 ALTER TABLE `savedmessages` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `snippetaccesslogs`
--

DROP TABLE IF EXISTS `snippetaccesslogs`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `snippetaccesslogs` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `SnippetId` int NOT NULL,
  `AccessedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `IpAddress` varchar(64) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `UserAgent` varchar(512) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `UserId` int DEFAULT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_SnippetAccessLogs_AccessedAt` (`AccessedAt`),
  KEY `IX_SnippetAccessLogs_SnippetId` (`SnippetId`),
  KEY `IX_SnippetAccessLogs_UserId` (`UserId`),
  CONSTRAINT `FK_SnippetAccessLogs_CodeSnippets_SnippetId` FOREIGN KEY (`SnippetId`) REFERENCES `codesnippets` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_SnippetAccessLogs_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE SET NULL
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `snippetaccesslogs`
--

LOCK TABLES `snippetaccesslogs` WRITE;
/*!40000 ALTER TABLE `snippetaccesslogs` DISABLE KEYS */;
/*!40000 ALTER TABLE `snippetaccesslogs` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `users`
--

DROP TABLE IF EXISTS `users`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `users` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `Username` varchar(50) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Email` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `PasswordHash` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `DisplayName` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Bio` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `AvatarUrl` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `IsOnline` tinyint(1) NOT NULL DEFAULT '0',
  `LastSeen` datetime(6) DEFAULT NULL,
  `LastLoginIp` varchar(45) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `PasswordResetToken` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `PasswordResetTokenExpiry` datetime(6) DEFAULT NULL,
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `DateOfBirth` datetime(6) DEFAULT NULL,
  `Department` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `JobTitle` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `PhoneNumber` varchar(30) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `WorkLocation` varchar(150) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `IsEmailVerified` tinyint(1) NOT NULL DEFAULT '0',
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Users_Email` (`Email`),
  UNIQUE KEY `IX_Users_Username` (`Username`)
) ENGINE=InnoDB AUTO_INCREMENT=6 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `users`
--

LOCK TABLES `users` WRITE;
/*!40000 ALTER TABLE `users` DISABLE KEYS */;
INSERT INTO `users` VALUES (1,'huyy@.com','huyy@.com','$2a$11$dgNMls0Lp/eP68xYuWT5deFb/wEqUXxO.FU8/w5MJltKUlTGa00q6','huyy@.com',NULL,NULL,0,NULL,'::1',NULL,NULL,'2026-06-22 17:04:46.487253','2026-06-22 17:04:46.487302',NULL,NULL,NULL,NULL,NULL,0),(2,'huyyne','dangkhoan911@gmai.com','$2a$11$A/t3AKdwMTU25irpex9NH.8XUSC2T4iTDo949jxUvF/Wb0A2HhXK.','huyday',NULL,NULL,0,NULL,'::1',NULL,NULL,'2026-06-22 17:06:46.559928','2026-06-22 17:06:46.559929',NULL,NULL,NULL,NULL,NULL,0),(3,'huyday','viethuy811@gmail.com','$2a$11$gVkpNIGDCEkAWuh1o/3scuCDa1n7hmF/Bx/8EXjKyKckQTgwAJxTC','huyday',NULL,NULL,0,'2026-06-25 07:42:58.025007','::1',NULL,NULL,'2026-06-22 17:08:03.096481','2026-06-25 07:42:58.025009',NULL,NULL,NULL,NULL,NULL,1),(4,'khoan','dangkhoan911@gmail.com','$2a$11$zkJEM6DOa3A85LHFk/WTROSUuJFMbr5QuWFP1f7.d/oX6Ks20X8lS','khoan',NULL,NULL,1,'2026-06-25 03:12:53.491097','::1',NULL,NULL,'2026-06-23 02:04:39.236824','2026-06-25 03:12:53.491183',NULL,NULL,NULL,NULL,NULL,1),(5,'diagtest','diagtest@pingme.local','$2a$11$YyPKYZ4rUalTU2U6yRillOQBBIKEdcIVU7/.I2svjiVx.bljnx6WS','Diag Test',NULL,NULL,1,'2026-06-23 17:44:47.398555','::1',NULL,NULL,'2026-06-23 17:39:19.000000','2026-06-23 17:44:47.398556',NULL,NULL,NULL,NULL,NULL,1);
/*!40000 ALTER TABLE `users` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `usersessions`
--

DROP TABLE IF EXISTS `usersessions`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `usersessions` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `UserId` int NOT NULL,
  `TokenHash` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `DeviceInfo` varchar(500) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `IpAddress` varchar(45) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci DEFAULT NULL,
  `LastActive` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `IsRevoked` tinyint(1) NOT NULL DEFAULT '0',
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `ExpiresAt` datetime(6) NOT NULL,
  PRIMARY KEY (`Id`),
  KEY `IX_UserSessions_ExpiresAt` (`ExpiresAt`),
  KEY `IX_UserSessions_TokenHash` (`TokenHash`),
  KEY `IX_UserSessions_UserId` (`UserId`),
  CONSTRAINT `FK_UserSessions_Users_UserId` FOREIGN KEY (`UserId`) REFERENCES `users` (`Id`) ON DELETE CASCADE
) ENGINE=InnoDB AUTO_INCREMENT=40 DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `usersessions`
--

LOCK TABLES `usersessions` WRITE;
/*!40000 ALTER TABLE `usersessions` DISABLE KEYS */;
INSERT INTO `usersessions` VALUES (1,3,'0beee7618461d89410c7c4607131e61d82dc98beadaccc439e5f934fd0fbc384','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0','::1','2026-06-22 17:09:01.000000',0,'2026-06-22 17:08:25.311823','2026-06-29 17:08:25.311233'),(2,3,'3610e0172ae2b8417df143a6a42b281f3a45295a84d6a1f479d990ee9eb4529d','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0','::1','2026-06-23 15:45:19.000000',0,'2026-06-23 02:03:41.775851','2026-06-30 02:03:41.775111'),(3,4,'27fa094f468c702c1174bb672f6e3ec246e8935bf84cfcab81e793cbe1658056','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','::1','2026-06-23 02:08:42.000000',0,'2026-06-23 02:05:04.236575','2026-06-30 02:05:04.236569'),(4,3,'9fca52bd16d18b5c0d92831d44d57ee806e5ea6061ff7f10dcd742bcb6af92de','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36','::1','2026-06-23 15:20:17.872571',0,'2026-06-23 15:20:17.872633','2026-06-30 15:20:17.872096'),(5,3,'06d168af09fbb764ff17595abf3a2b21a4b3d0071d003f76d25483c5ea2ab2eb','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0','::1','2026-06-23 17:06:11.000000',0,'2026-06-23 15:55:40.215463','2026-06-30 15:55:40.214894'),(6,3,'e3bbb8eef02d5c121351885ce222d3f92e887bd6b4a03f606ba01a33db0f1cf4','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36','::1','2026-06-23 17:25:04.000000',0,'2026-06-23 16:46:27.543107','2026-06-30 16:46:27.542673'),(7,3,'817888e7175678314d16045de3ae810292c841c0f81a198c63258588df3608e7','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0','::1','2026-06-23 17:06:29.000000',0,'2026-06-23 17:06:26.762811','2026-06-30 17:06:26.762313'),(8,3,'f3f6cd1ffb2eb0fe6739e5a8e38b45b43de7c4ae426647f6a90cff3aff0dd7a3','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0','::1','2026-06-23 17:07:02.000000',0,'2026-06-23 17:06:59.054732','2026-06-30 17:06:59.054695'),(9,3,'d0ce888f774080bb80ec85c5d638d97a5d601b9c1e6daf7c180cdd7fee568b33','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0','::1','2026-06-23 17:07:51.000000',0,'2026-06-23 17:07:49.294398','2026-06-30 17:07:49.294068'),(10,3,'84392f2c58f1f397da4a4b7535cd6d89c9f76583930fe249ad663d3cbac3b1b1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0','::1','2026-06-23 17:32:14.000000',0,'2026-06-23 17:32:10.845239','2026-06-30 17:32:10.844399'),(11,3,'683e93c340d48afda1a2f2841ee85e7596574ace8f12203071ad8958fdeff531','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0','::1','2026-06-23 17:32:34.000000',0,'2026-06-23 17:32:30.315754','2026-06-30 17:32:30.315729'),(12,3,'9c29243e007500e0002899a611a417c3b46837655da15f8ce30be77025e60820','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0','::1','2026-06-23 17:36:44.000000',0,'2026-06-23 17:36:40.018938','2026-06-30 17:36:40.018307'),(13,5,'0616a05ff8dd80a87661b2fd1a53a1ab5c71cb5b83f671cf4df22b8831173ded','curl/8.10.1','::1','2026-06-23 17:40:53.206403',0,'2026-06-23 17:40:53.206460','2026-06-30 17:40:53.205755'),(14,3,'69d0d20a41c48b3df7a481aa36bef5344c1f522981445f57f0d0124d574ad934','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0','::1','2026-06-23 17:42:21.000000',0,'2026-06-23 17:42:20.792939','2026-06-30 17:42:20.792902'),(15,3,'c331fdbad87320ce840db17bea46c692f97e97b3ac8c391799065d85ac9812dc','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0','::1','2026-06-23 17:43:35.000000',0,'2026-06-23 17:43:30.683273','2026-06-30 17:43:30.682520'),(16,5,'ac33727ef472802b2ebfbec7c99757e12f0a7307d9ef84880b28f13f1c62c7d5','curl/8.10.1','::1','2026-06-23 17:44:48.000000',0,'2026-06-23 17:44:47.436600','2026-06-30 17:44:47.436581'),(17,3,'39c93821b22b68715a412d7650995ee9e187710d28eb6f7c9498330880700713','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0','::1','2026-06-23 17:47:23.000000',0,'2026-06-23 17:47:19.445593','2026-06-30 17:47:19.445570'),(18,3,'89172c50d934ff23a742a53991321e89529f1ecae669aa0e1748e5332c56718a','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0','::1','2026-06-23 17:47:36.000000',0,'2026-06-23 17:47:35.437030','2026-06-30 17:47:35.437017'),(19,3,'667992f8b105a898a1cc5c4536cd313df2f91e7dc42bc41e184f54ef6533bd36','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0','::1','2026-06-23 17:47:43.000000',0,'2026-06-23 17:47:42.162522','2026-06-30 17:47:42.162506'),(20,3,'b178970fc56b2621db01a9aa45007e8f064ebef2daa19231b4bcc258ad496f19','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0','::1','2026-06-23 17:55:32.000000',0,'2026-06-23 17:55:31.522872','2026-06-30 17:55:31.522866'),(21,3,'12e15d41b351e46152c104157753f76f0f9b25ccc640a3b313f66b9f84f2ef44','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0','::1','2026-06-23 17:56:59.000000',0,'2026-06-23 17:56:58.021067','2026-06-30 17:56:58.021063'),(22,3,'da0cf155dfa0c03292787fc3bae752575e5a85d05ec4fddc1940e469a1d553a6','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0','::1','2026-06-23 18:17:38.000000',0,'2026-06-23 18:17:32.821887','2026-06-30 18:17:32.821241'),(23,3,'ff63c2f78735f2ecff69937179e18cccafec179d6ac206cb9c43059a94ce4ab4','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','::1','2026-06-23 18:20:41.000000',0,'2026-06-23 18:19:53.054497','2026-06-30 18:19:53.054100'),(24,3,'e64508495f47cfe9f0134b320d95e0f02c8b74d8fd07dadf02500f476a78bbf7','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','::1','2026-06-23 18:38:52.000000',0,'2026-06-23 18:38:51.626259','2026-06-30 18:38:51.625955'),(25,3,'a1f628cc3813a3acc5c4a27830aa07776c6fee9b0f36780f34884dd19f4267a1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','::1','2026-06-23 18:55:46.000000',0,'2026-06-23 18:41:29.687572','2026-06-30 18:41:29.687559'),(26,3,'fe66e52f901bdcd9b56c125ebe4e3d70b7ce9b234e70244e4ae323f714912328','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','::1','2026-06-23 18:56:50.000000',0,'2026-06-23 18:56:07.062095','2026-06-30 18:56:07.062078'),(27,3,'640be8b2f03da24fca5f3f5daf537345502084294679e8ae78fbe3ff5d66cd3f','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','::1','2026-06-23 19:43:14.000000',0,'2026-06-23 18:56:58.810777','2026-06-30 18:56:58.810604'),(28,3,'4407b43b8acae9da8722773ddac07480dec0d708d79cabdc4f4f2520da5a541d','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','::1','2026-06-23 19:58:27.000000',0,'2026-06-23 19:58:04.799159','2026-06-30 19:58:04.798838'),(29,3,'e13278c8b3caf6fdeaf97484e729382e7ff5dc0be2e3c63b204df052af9a1473','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','127.0.0.1','2026-06-23 20:01:40.000000',0,'2026-06-23 20:00:09.284732','2026-06-30 20:00:09.284305'),(30,3,'735499d0dc1a45f88072ac472107e216d1336a77fc7cfbcd0be3bd3ab3bbfee1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','::1','2026-06-23 20:59:57.000000',0,'2026-06-23 20:02:35.098893','2026-06-30 20:02:35.098526'),(31,3,'84654f9a6b717ccac87d84c00d23aebec304e5494aa2283c86124c15ca185988','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','127.0.0.1','2026-06-23 21:04:08.000000',0,'2026-06-23 21:00:08.648927','2026-06-30 21:00:08.648767'),(32,3,'24009412a048a0babb20709b6b38bc82da92c09b62ec66c796247730f3872fff','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','::1','2026-06-24 10:24:55.000000',0,'2026-06-24 09:16:38.818276','2026-07-01 09:16:38.817730'),(33,4,'b1abeee18f44157d6428b93cdc44e61da3cec7307c6d1689a7aa6e66653f0c77','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36','::1','2026-06-24 10:24:11.000000',0,'2026-06-24 09:25:50.670975','2026-07-01 09:25:50.670813'),(34,3,'3b5f3e21a1afee4e88996c0cd1adc7b9a81fb0704ea737182f950eaf914bfab1','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','::1','2026-06-24 19:00:10.000000',0,'2026-06-24 17:01:39.803566','2026-07-01 17:01:39.803170'),(35,4,'72ccc77e99c42d30165540c0304b2c42ae2589098a5a22643ed668634ffbea44','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36','::1','2026-06-24 19:36:18.000000',0,'2026-06-24 18:01:08.151822','2026-07-01 18:01:08.151506'),(36,3,'93239ffa4f2afa0a9c138efb3bc6e894949cbe2645e5e1e2121fbe7a9643ed6a','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','::1','2026-06-24 19:35:35.000000',0,'2026-06-24 19:00:27.089006','2026-07-01 19:00:27.088824'),(37,3,'d309ef34774c85b3d597982c94d4d23d1b7265f2ea1158037293499fd05c0b9c','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','::1','2026-06-25 03:06:53.000000',0,'2026-06-24 19:36:04.676609','2026-07-01 19:36:04.676448'),(38,4,'521b435d85e2b9421ee9ab2d2b416ca57d8ceb10cb71307e7249ead9098cd1be','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/149.0.0.0 Safari/537.36 Edg/149.0.0.0','::1','2026-06-25 03:12:53.000000',0,'2026-06-25 01:45:55.447525','2026-07-02 01:45:55.447170'),(39,3,'b37ecd354af7e8017a9d987a9bee53f423e931d4b97e5596c2f5ccf33177f741','Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/148.0.0.0 Safari/537.36','::1','2026-06-25 07:42:46.000000',0,'2026-06-25 03:07:58.359727','2026-07-02 03:07:58.359486');
/*!40000 ALTER TABLE `usersessions` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Table structure for table `webhooks`
--

DROP TABLE IF EXISTS `webhooks`;
/*!40101 SET @saved_cs_client     = @@character_set_client */;
/*!50503 SET character_set_client = utf8mb4 */;
CREATE TABLE `webhooks` (
  `Id` int NOT NULL AUTO_INCREMENT,
  `GroupId` int NOT NULL,
  `CreatedByUserId` int NOT NULL,
  `Name` varchar(100) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Token` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `Secret` varchar(255) CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_ai_ci NOT NULL,
  `IsActive` tinyint(1) NOT NULL DEFAULT '1',
  `CreatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  `UpdatedAt` datetime(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6),
  PRIMARY KEY (`Id`),
  UNIQUE KEY `IX_Webhooks_Token` (`Token`),
  KEY `IX_Webhooks_CreatedByUserId` (`CreatedByUserId`),
  KEY `IX_Webhooks_GroupId` (`GroupId`),
  CONSTRAINT `FK_Webhooks_Groups_GroupId` FOREIGN KEY (`GroupId`) REFERENCES `groups` (`Id`) ON DELETE CASCADE,
  CONSTRAINT `FK_Webhooks_Users_CreatedByUserId` FOREIGN KEY (`CreatedByUserId`) REFERENCES `users` (`Id`) ON DELETE RESTRICT
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_0900_ai_ci;
/*!40101 SET character_set_client = @saved_cs_client */;

--
-- Dumping data for table `webhooks`
--

LOCK TABLES `webhooks` WRITE;
/*!40000 ALTER TABLE `webhooks` DISABLE KEYS */;
/*!40000 ALTER TABLE `webhooks` ENABLE KEYS */;
UNLOCK TABLES;

--
-- Dumping events for database 'dbweb'
--

--
-- Dumping routines for database 'dbweb'
--
SET @@SESSION.SQL_LOG_BIN = @MYSQLDUMP_TEMP_LOG_BIN;
/*!40103 SET TIME_ZONE=@OLD_TIME_ZONE */;

/*!40101 SET SQL_MODE=@OLD_SQL_MODE */;
/*!40014 SET FOREIGN_KEY_CHECKS=@OLD_FOREIGN_KEY_CHECKS */;
/*!40014 SET UNIQUE_CHECKS=@OLD_UNIQUE_CHECKS */;
/*!40101 SET CHARACTER_SET_CLIENT=@OLD_CHARACTER_SET_CLIENT */;
/*!40101 SET CHARACTER_SET_RESULTS=@OLD_CHARACTER_SET_RESULTS */;
/*!40101 SET COLLATION_CONNECTION=@OLD_COLLATION_CONNECTION */;
/*!40111 SET SQL_NOTES=@OLD_SQL_NOTES */;

-- Dump completed on 2026-06-25 15:04:16


-- --------------------------------------------------
-- Entity Designer DDL Script for SQL Server 2005, 2008, and Azure
-- --------------------------------------------------
-- Date Created: 11/22/2013 12:53:07
-- Generated from EDMX file: D:\SVILUPPO SOFTWARE\SciFiLarpNetwork\SciFiNetwork\DataMappings\LarpNetworkModel.edmx
-- --------------------------------------------------

SET QUOTED_IDENTIFIER OFF;
GO
IF SCHEMA_ID(N'dbo') IS NULL EXECUTE(N'CREATE SCHEMA [dbo]');
GO

-- --------------------------------------------------
-- Dropping existing FOREIGN KEY constraints
-- --------------------------------------------------

IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_AccountHacking_0_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[AccountHacking] DROP CONSTRAINT [FK_AccountHacking_0_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_AccountHacking_1_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[AccountHacking] DROP CONSTRAINT [FK_AccountHacking_1_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_AddressBook_0_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[AddressBook] DROP CONSTRAINT [FK_AddressBook_0_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_BankAccount_0_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[BankAccount] DROP CONSTRAINT [FK_BankAccount_0_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_BankAccount_1_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[BankAccount] DROP CONSTRAINT [FK_BankAccount_1_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_CharacterEventDay_1_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[CharacterEventDay] DROP CONSTRAINT [FK_CharacterEventDay_1_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_CharacterInfection_0_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[CharacterInfection] DROP CONSTRAINT [FK_CharacterInfection_0_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_CharacterInfection_1_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[CharacterInfection] DROP CONSTRAINT [FK_CharacterInfection_1_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_CharacterSkill_0_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[CharacterSkill] DROP CONSTRAINT [FK_CharacterSkill_0_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_CharacterSkill_1_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[CharacterSkill] DROP CONSTRAINT [FK_CharacterSkill_1_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_CharacterSkill_2_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[CharacterSkill] DROP CONSTRAINT [FK_CharacterSkill_2_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_CharacterTalent_0_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[CharacterTalent] DROP CONSTRAINT [FK_CharacterTalent_0_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_CharacterTalent_1_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[CharacterTalent] DROP CONSTRAINT [FK_CharacterTalent_1_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_CharacterUnlockedSkill_0_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[CharacterUnlockedSkill] DROP CONSTRAINT [FK_CharacterUnlockedSkill_0_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_CharacterUnlockedSkill_1_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[CharacterUnlockedSkill] DROP CONSTRAINT [FK_CharacterUnlockedSkill_1_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_CharacterUnlockedSkill_2_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[CharacterUnlockedSkill] DROP CONSTRAINT [FK_CharacterUnlockedSkill_2_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_DatapadFiles_0_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[DatapadFiles] DROP CONSTRAINT [FK_DatapadFiles_0_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_DatapadHacking_0_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[DatapadHacking] DROP CONSTRAINT [FK_DatapadHacking_0_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_DatapadHacking_1_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[DatapadHacking] DROP CONSTRAINT [FK_DatapadHacking_1_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_DeviceNetwork_0_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[DeviceNetwork] DROP CONSTRAINT [FK_DeviceNetwork_0_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_Element_0_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[Element] DROP CONSTRAINT [FK_Element_0_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_ElementCompound_0_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[ElementCompound] DROP CONSTRAINT [FK_ElementCompound_0_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_ElementCompound_1_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[ElementCompound] DROP CONSTRAINT [FK_ElementCompound_1_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_ElementQRCode_0_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[ElementQRCode] DROP CONSTRAINT [FK_ElementQRCode_0_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_EventDatapad_0_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[EventDatapad] DROP CONSTRAINT [FK_EventDatapad_0_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_EventDatapad_1_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[EventDatapad] DROP CONSTRAINT [FK_EventDatapad_1_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_EventDays_0_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[EventDays] DROP CONSTRAINT [FK_EventDays_0_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_EventElement_0_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[EventElement] DROP CONSTRAINT [FK_EventElement_0_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_EventElement_1_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[EventElement] DROP CONSTRAINT [FK_EventElement_1_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_HeroCharacter_4_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[HeroCharacter] DROP CONSTRAINT [FK_HeroCharacter_4_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_HeroCharacter_5_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[HeroCharacter] DROP CONSTRAINT [FK_HeroCharacter_5_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_HeroCharacter_6_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[HeroCharacter] DROP CONSTRAINT [FK_HeroCharacter_6_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_Item_0_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[Item] DROP CONSTRAINT [FK_Item_0_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_Item_1_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[Item] DROP CONSTRAINT [FK_Item_1_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_MailInBox_0_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[MailInBox] DROP CONSTRAINT [FK_MailInBox_0_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_MailInBox_1_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[MailInBox] DROP CONSTRAINT [FK_MailInBox_1_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_MailOutBox_0_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[MailOutBox] DROP CONSTRAINT [FK_MailOutBox_0_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_MailOutBox_1_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[MailOutBox] DROP CONSTRAINT [FK_MailOutBox_1_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_Message_0_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[Message] DROP CONSTRAINT [FK_Message_0_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_MessageHacking_0_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[MessageHacking] DROP CONSTRAINT [FK_MessageHacking_0_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_MessageHacking_1_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[MessageHacking] DROP CONSTRAINT [FK_MessageHacking_1_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_NewsHacking_0_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[NewsHacking] DROP CONSTRAINT [FK_NewsHacking_0_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_NewsHacking_1_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[NewsHacking] DROP CONSTRAINT [FK_NewsHacking_1_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_NewsStory_0_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[NewsStory] DROP CONSTRAINT [FK_NewsStory_0_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_NewsStory_1_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[NewsStory] DROP CONSTRAINT [FK_NewsStory_1_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_Parameter_0_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[Parameter] DROP CONSTRAINT [FK_Parameter_0_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_SavedMessage_0_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[SavedMessage] DROP CONSTRAINT [FK_SavedMessage_0_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_SkillEffect_0_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[SkillEffect] DROP CONSTRAINT [FK_SkillEffect_0_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_SkillPrerequisite_0_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[SkillPrerequisite] DROP CONSTRAINT [FK_SkillPrerequisite_0_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_SkillPrerequisite_1_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[SkillPrerequisite] DROP CONSTRAINT [FK_SkillPrerequisite_1_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_Substance_0_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[Substance] DROP CONSTRAINT [FK_Substance_0_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_Substance_1_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[Substance] DROP CONSTRAINT [FK_Substance_1_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_TalentSkill_0_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[TalentSkill] DROP CONSTRAINT [FK_TalentSkill_0_0];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[FK_TalentSkill_1_0]', 'F') IS NOT NULL
    ALTER TABLE [gamedataModelStoreContainer].[TalentSkill] DROP CONSTRAINT [FK_TalentSkill_1_0];
GO

-- --------------------------------------------------
-- Dropping existing tables
-- --------------------------------------------------

IF OBJECT_ID(N'[gamedataModelStoreContainer].[AccountHacking]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[AccountHacking];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[AddressBook]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[AddressBook];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[Bank]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[Bank];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[BankAccount]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[BankAccount];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[CharacterEventDay]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[CharacterEventDay];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[CharacterInfection]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[CharacterInfection];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[CharacterSkill]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[CharacterSkill];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[CharacterTalent]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[CharacterTalent];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[CharacterUnlockedSkill]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[CharacterUnlockedSkill];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[Datapad]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[Datapad];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[DatapadFiles]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[DatapadFiles];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[DatapadHacking]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[DatapadHacking];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[DeviceNetwork]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[DeviceNetwork];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[Door]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[Door];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[Element]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[Element];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[ElementAvailability]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[ElementAvailability];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[ElementCompound]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[ElementCompound];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[ElementQRCode]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[ElementQRCode];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[Event]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[Event];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[EventDatapad]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[EventDatapad];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[EventDays]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[EventDays];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[EventElement]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[EventElement];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[HackingTool]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[HackingTool];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[HeroCharacter]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[HeroCharacter];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[Infection]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[Infection];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[Item]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[Item];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[ItemType]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[ItemType];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[MailInBox]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[MailInBox];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[MailOutBox]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[MailOutBox];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[Message]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[Message];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[MessageHacking]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[MessageHacking];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[Network]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[Network];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[NewsHacking]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[NewsHacking];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[NewsStory]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[NewsStory];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[Parameter]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[Parameter];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[Player]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[Player];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[SavedMessage]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[SavedMessage];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[Skill]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[Skill];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[SkillEffect]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[SkillEffect];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[SkillPrerequisite]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[SkillPrerequisite];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[Species]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[Species];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[Substance]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[Substance];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[SubstanceType]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[SubstanceType];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[Talent]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[Talent];
GO
IF OBJECT_ID(N'[gamedataModelStoreContainer].[TalentSkill]', 'U') IS NOT NULL
    DROP TABLE [gamedataModelStoreContainer].[TalentSkill];
GO

-- --------------------------------------------------
-- Creating all tables
-- --------------------------------------------------

-- Creating table 'AccountHackings'
CREATE TABLE [dbo].[AccountHackings] (
    [HackedCharacterID] integer  NOT NULL,
    [HackingCharacterID] integer  NOT NULL,
    [AttemptNumber] integer  NOT NULL,
    [Success] bit  NULL,
    [DateOfAttempt] datetime  NULL
);
GO

-- Creating table 'AddressBooks'
CREATE TABLE [dbo].[AddressBooks] (
    [CharacterID] integer  NOT NULL,
    [SavedCharacterID] integer  NOT NULL,
    [SavedCharacterName] nvarchar(2147483647)  NULL
);
GO

-- Creating table 'Banks'
CREATE TABLE [dbo].[Banks] (
    [BankID] integer  NOT NULL,
    [Name] varchar(2147483647)  NOT NULL,
    [BaseInterestRate] integer  NULL,
    [InterestTimeInterval] integer  NULL,
    [Price] integer  NULL,
    [PriceTimeInterval] integer  NULL
);
GO

-- Creating table 'BankAccounts'
CREATE TABLE [dbo].[BankAccounts] (
    [AccountID] integer  NOT NULL,
    [BankID] integer  NOT NULL,
    [CharacterID] integer  NOT NULL,
    [InterestTimeInterval] integer  NULL,
    [Price] integer  NULL,
    [PriceTimeInterval] integer  NULL,
    [InterestRate] integer  NULL,
    [SubscriptionDate] datetime  NULL,
    [Amount] real  NOT NULL
);
GO

-- Creating table 'CharacterEventDays'
CREATE TABLE [dbo].[CharacterEventDays] (
    [EventID] integer  NOT NULL,
    [IndividualDay] datetime  NOT NULL,
    [CharacterID] integer  NOT NULL,
    [HasPaid] bit  NOT NULL,
    [PaymentDate] datetime  NULL,
    [TookPart] bit  NOT NULL
);
GO

-- Creating table 'CharacterSkills'
CREATE TABLE [dbo].[CharacterSkills] (
    [CharacterID] integer  NOT NULL,
    [SkillID] integer  NOT NULL,
    [AcquiredManyTimes] integer  NULL,
    [TalentID] varchar(2147483647)  NOT NULL,
    [AdditionalDetails] nvarchar(2147483647)  NULL
);
GO

-- Creating table 'CharacterUnlockedSkills'
CREATE TABLE [dbo].[CharacterUnlockedSkills] (
    [CharacterID] integer  NOT NULL,
    [SkillID] integer  NOT NULL,
    [TalentID] varchar(2147483647)  NULL,
    [Notes] nvarchar(2147483647)  NULL
);
GO

-- Creating table 'Datapads'
CREATE TABLE [dbo].[Datapads] (
    [DatapadID] integer  NOT NULL,
    [Content] nvarchar(2147483647)  NULL,
    [HackingLevel] integer  NOT NULL,
    [QRCode] uniqueidentifier  NULL
);
GO

-- Creating table 'DatapadFiles'
CREATE TABLE [dbo].[DatapadFiles] (
    [DatapadID] integer  NOT NULL,
    [FileNumber] integer  NOT NULL,
    [FileName] varchar(2147483647)  NULL,
    [Content] nvarchar(2147483647)  NULL,
    [EncryptionLevel] integer  NULL
);
GO

-- Creating table 'DatapadHackings'
CREATE TABLE [dbo].[DatapadHackings] (
    [DatapadID] integer  NOT NULL,
    [HackingCharacterID] integer  NOT NULL,
    [AttemptNumber] integer  NOT NULL,
    [Success] bit  NULL,
    [DateOfAttempt] datetime  NULL
);
GO

-- Creating table 'DeviceNetworks'
CREATE TABLE [dbo].[DeviceNetworks] (
    [IPv4Address] varchar(2147483647)  NOT NULL,
    [NetworkID] integer  NOT NULL
);
GO

-- Creating table 'Doors'
CREATE TABLE [dbo].[Doors] (
    [DoorID] integer  NOT NULL,
    [Reference] varchar(2147483647)  NULL,
    [QRCode] uniqueidentifier  NOT NULL,
    [HackingLevel] integer  NOT NULL
);
GO

-- Creating table 'Elements'
CREATE TABLE [dbo].[Elements] (
    [ElementID] integer  NOT NULL,
    [Name] varchar(2147483647)  NULL,
    [Description] nvarchar(2147483647)  NULL,
    [Effect] nvarchar(2147483647)  NULL,
    [Price] real  NOT NULL,
    [Picture] nvarchar(2147483647)  NULL,
    [AvailabilityID] integer  NOT NULL,
    [ExpirationDate] datetime  NULL,
    [Discriminator] integer  NOT NULL
);
GO

-- Creating table 'ElementAvailabilities'
CREATE TABLE [dbo].[ElementAvailabilities] (
    [AvailabilityID] integer  NOT NULL,
    [Description] nvarchar(2147483647)  NULL
);
GO

-- Creating table 'ElementCompounds'
CREATE TABLE [dbo].[ElementCompounds] (
    [CompoundElementID] integer  NOT NULL,
    [ComponentNumber] integer  NOT NULL,
    [ComponentElementID] integer  NULL
);
GO

-- Creating table 'ElementQRCodes'
CREATE TABLE [dbo].[ElementQRCodes] (
    [ElementID] integer  NOT NULL,
    [QRCode] uniqueidentifier  NOT NULL
);
GO

-- Creating table 'Events'
CREATE TABLE [dbo].[Events] (
    [EventID] integer  NOT NULL,
    [Title] varchar(2147483647)  NOT NULL,
    [StartDate] datetime  NOT NULL,
    [TotalExperiencePoints] integer  NOT NULL,
    [Description] nvarchar(2147483647)  NULL,
    [FullEventPrice] real  NOT NULL,
    [Collection] real  NULL,
    [IsClosed] bit  NOT NULL
);
GO

-- Creating table 'EventDatapads'
CREATE TABLE [dbo].[EventDatapads] (
    [EventID] integer  NOT NULL,
    [DatapadID] integer  NOT NULL,
    [NumberOfCopies] integer  NULL
);
GO

-- Creating table 'EventDays'
CREATE TABLE [dbo].[EventDays] (
    [EventID] integer  NOT NULL,
    [IndividualDay] datetime  NOT NULL,
    [ExperiencePoints] integer  NULL,
    [InGameTime] datetime  NOT NULL,
    [OffGameTime] datetime  NOT NULL,
    [PricePerDay] real  NULL
);
GO

-- Creating table 'EventElements'
CREATE TABLE [dbo].[EventElements] (
    [EventID] integer  NOT NULL,
    [ElementID] integer  NOT NULL,
    [NumberOfCopies] integer  NULL
);
GO

-- Creating table 'HackingTools'
CREATE TABLE [dbo].[HackingTools] (
    [HackingToolID] integer  NOT NULL,
    [QRCode] uniqueidentifier  NOT NULL,
    [HackerLevel] integer  NOT NULL
);
GO

-- Creating table 'HeroCharacters'
CREATE TABLE [dbo].[HeroCharacters] (
    [CharacterID] integer  NOT NULL,
    [PlayerID] integer  NOT NULL,
    [Name] varchar(2147483647)  NOT NULL,
    [SpeciesID] integer  NOT NULL,
    [Type] integer  NOT NULL,
    [ExperiencePoints] integer  NOT NULL,
    [Sex] char(2147483647)  NOT NULL,
    [DarkSidePoints] integer  NOT NULL,
    [DateOfCreation] datetime  NOT NULL,
    [HonorificTitle] varchar(2147483647)  NULL,
    [IsAlive] bit  NOT NULL,
    [DateOfDeath] datetime  NULL,
    [LarpNetworkPassword] varchar(2147483647)  NULL,
    [BloodFormula] varchar(2147483647)  NULL,
    [NetworkID] integer  NULL,
    [LastAccountEncryption] datetime  NULL,
    [EncryptionLevel] integer  NOT NULL,
    [QRCode] uniqueidentifier  NULL
);
GO

-- Creating table 'Infections'
CREATE TABLE [dbo].[Infections] (
    [InfectionID] integer  NOT NULL,
    [Name] varchar(2147483647)  NOT NULL,
    [Description] nvarchar(2147483647)  NULL
);
GO

-- Creating table 'Items'
CREATE TABLE [dbo].[Items] (
    [ElementID] integer  NOT NULL,
    [NumberOfCharges] integer  NULL,
    [ItemTypeID] integer  NULL,
    [Rechargeable] bit  NOT NULL
);
GO

-- Creating table 'ItemTypes'
CREATE TABLE [dbo].[ItemTypes] (
    [ItemTypeID] integer  NOT NULL,
    [Description] varchar(2147483647)  NULL
);
GO

-- Creating table 'MailInBoxes'
CREATE TABLE [dbo].[MailInBoxes] (
    [MessageID] integer  NOT NULL,
    [CharacterID] integer  NOT NULL,
    [IsDeleted] bit  NULL,
    [IsRead] bit  NULL,
    [DateOfDeletion] datetime  NULL
);
GO

-- Creating table 'MailOutBoxes'
CREATE TABLE [dbo].[MailOutBoxes] (
    [MessageID] integer  NOT NULL,
    [CharacterID] integer  NOT NULL,
    [IsDeleted] bit  NOT NULL,
    [IsRead] bit  NOT NULL,
    [DateOfDeletion] datetime  NULL
);
GO

-- Creating table 'Messages'
CREATE TABLE [dbo].[Messages] (
    [MessageID] integer  NOT NULL,
    [SenderCharacter] integer  NOT NULL,
    [Subject] varchar(2147483647)  NULL,
    [Message1] nvarchar(2147483647)  NOT NULL,
    [HackingLevel] integer  NOT NULL,
    [IsActive] bit  NOT NULL,
    [DateOfCreation] datetime  NOT NULL
);
GO

-- Creating table 'MessageHackings'
CREATE TABLE [dbo].[MessageHackings] (
    [MessageID] integer  NOT NULL,
    [CharacterID] integer  NOT NULL,
    [AttemptNumber] integer  NOT NULL,
    [Success] bit  NOT NULL,
    [DateOfAttempt] datetime  NOT NULL
);
GO

-- Creating table 'Networks'
CREATE TABLE [dbo].[Networks] (
    [NetworkID] integer  NOT NULL,
    [NetworkName] varchar(2147483647)  NOT NULL,
    [Description] nvarchar(2147483647)  NULL,
    [Faction] varchar(2147483647)  NULL
);
GO

-- Creating table 'NewsHackings'
CREATE TABLE [dbo].[NewsHackings] (
    [NewsID] integer  NOT NULL,
    [CharacterID] integer  NOT NULL,
    [AttemptNumber] integer  NOT NULL,
    [Success] bit  NOT NULL,
    [DateOfAttempt] datetime  NOT NULL
);
GO

-- Creating table 'NewsStories'
CREATE TABLE [dbo].[NewsStories] (
    [NewsID] integer  NOT NULL,
    [DateOfCreation] datetime  NOT NULL,
    [ExpirationDate] datetime  NOT NULL,
    [Subject] varchar(2147483647)  NULL,
    [Message] nvarchar(2147483647)  NOT NULL,
    [NetworkID] integer  NOT NULL,
    [AuthorCharacterID] integer  NULL,
    [HackingLevel] integer  NOT NULL
);
GO

-- Creating table 'Parameters'
CREATE TABLE [dbo].[Parameters] (
    [NetworkID] integer  NOT NULL,
    [Parameter1] varchar(2147483647)  NOT NULL,
    [Value] nvarchar(2147483647)  NULL
);
GO

-- Creating table 'Players'
CREATE TABLE [dbo].[Players] (
    [PlayerID] integer  NOT NULL,
    [MainName] varchar(2147483647)  NOT NULL,
    [FirstName] varchar(2147483647)  NULL,
    [DateOfBirth] datetime  NOT NULL,
    [Address] varchar(2147483647)  NULL,
    [City] varchar(2147483647)  NULL,
    [CountyOrArea] varchar(2147483647)  NULL,
    [PostCode] varchar(2147483647)  NULL,
    [PhoneNumber] varchar(2147483647)  NULL,
    [DateOfSubscription] datetime  NOT NULL,
    [Sex] char(2147483647)  NOT NULL,
    [PlayerCathegory] varchar(2147483647)  NOT NULL,
    [Email] varchar(2147483647)  NULL,
    [Password] varchar(2147483647)  NULL
);
GO

-- Creating table 'SavedMessages'
CREATE TABLE [dbo].[SavedMessages] (
    [MessageID] integer  NOT NULL,
    [CharacterID] integer  NOT NULL,
    [Subject] nvarchar(2147483647)  NOT NULL,
    [Source] varchar(2147483647)  NULL,
    [Message] nvarchar(2147483647)  NULL,
    [HackingLevel] integer  NOT NULL
);
GO

-- Creating table 'Skills'
CREATE TABLE [dbo].[Skills] (
    [SkillID] integer  NOT NULL,
    [Name] varchar(2147483647)  NOT NULL,
    [Description] nvarchar(2147483647)  NULL,
    [AcquireManyTimes] bit  NOT NULL,
    [ExperiencePointsCost] integer  NOT NULL,
    [IsAdvanced] bit  NOT NULL
);
GO

-- Creating table 'SkillEffects'
CREATE TABLE [dbo].[SkillEffects] (
    [SkillID] integer  NOT NULL,
    [EffectID] varchar(2147483647)  NOT NULL,
    [Value] varchar(2147483647)  NOT NULL
);
GO

-- Creating table 'Species'
CREATE TABLE [dbo].[Species] (
    [SpeciesID] integer  NOT NULL,
    [Name] varchar(2147483647)  NOT NULL,
    [Description] nvarchar(2147483647)  NULL
);
GO

-- Creating table 'Substances'
CREATE TABLE [dbo].[Substances] (
    [ElementID] integer  NOT NULL,
    [ApplicationMode] varchar(2147483647)  NOT NULL,
    [StrengthValue] integer  NOT NULL,
    [SubstanceTypeID] integer  NOT NULL
);
GO

-- Creating table 'SubstanceTypes'
CREATE TABLE [dbo].[SubstanceTypes] (
    [SubstanceTypeID] integer  NOT NULL,
    [Description] varchar(2147483647)  NOT NULL
);
GO

-- Creating table 'Talents'
CREATE TABLE [dbo].[Talents] (
    [TalentID] varchar(2147483647)  NOT NULL,
    [Name] varchar(2147483647)  NOT NULL,
    [Description] nvarchar(2147483647)  NULL,
    [TalentType] integer  NOT NULL
);
GO

-- Creating table 'CharacterInfection'
CREATE TABLE [dbo].[CharacterInfection] (
    [Infections_InfectionID] integer  NOT NULL,
    [HeroCharacters_CharacterID] integer  NOT NULL
);
GO

-- Creating table 'CharacterTalent'
CREATE TABLE [dbo].[CharacterTalent] (
    [Talents_TalentID] varchar(2147483647)  NOT NULL,
    [HeroCharacters_CharacterID] integer  NOT NULL
);
GO

-- Creating table 'SkillPrerequisite'
CREATE TABLE [dbo].[SkillPrerequisite] (
    [DependentSkills_SkillID] integer  NOT NULL,
    [Prerequisites_SkillID] integer  NOT NULL
);
GO

-- Creating table 'TalentSkill'
CREATE TABLE [dbo].[TalentSkill] (
    [Talents_TalentID] varchar(2147483647)  NOT NULL,
    [Skills_SkillID] integer  NOT NULL
);
GO

-- --------------------------------------------------
-- Creating all PRIMARY KEY constraints
-- --------------------------------------------------

-- Creating primary key on [HackedCharacterID], [HackingCharacterID], [AttemptNumber] in table 'AccountHackings'
ALTER TABLE [dbo].[AccountHackings]
ADD CONSTRAINT [PK_AccountHackings]
    PRIMARY KEY CLUSTERED ([HackedCharacterID], [HackingCharacterID], [AttemptNumber] ASC);
GO

-- Creating primary key on [CharacterID], [SavedCharacterID] in table 'AddressBooks'
ALTER TABLE [dbo].[AddressBooks]
ADD CONSTRAINT [PK_AddressBooks]
    PRIMARY KEY CLUSTERED ([CharacterID], [SavedCharacterID] ASC);
GO

-- Creating primary key on [BankID] in table 'Banks'
ALTER TABLE [dbo].[Banks]
ADD CONSTRAINT [PK_Banks]
    PRIMARY KEY CLUSTERED ([BankID] ASC);
GO

-- Creating primary key on [AccountID] in table 'BankAccounts'
ALTER TABLE [dbo].[BankAccounts]
ADD CONSTRAINT [PK_BankAccounts]
    PRIMARY KEY CLUSTERED ([AccountID] ASC);
GO

-- Creating primary key on [EventID], [IndividualDay], [CharacterID] in table 'CharacterEventDays'
ALTER TABLE [dbo].[CharacterEventDays]
ADD CONSTRAINT [PK_CharacterEventDays]
    PRIMARY KEY CLUSTERED ([EventID], [IndividualDay], [CharacterID] ASC);
GO

-- Creating primary key on [CharacterID], [SkillID] in table 'CharacterSkills'
ALTER TABLE [dbo].[CharacterSkills]
ADD CONSTRAINT [PK_CharacterSkills]
    PRIMARY KEY CLUSTERED ([CharacterID], [SkillID] ASC);
GO

-- Creating primary key on [CharacterID], [SkillID] in table 'CharacterUnlockedSkills'
ALTER TABLE [dbo].[CharacterUnlockedSkills]
ADD CONSTRAINT [PK_CharacterUnlockedSkills]
    PRIMARY KEY CLUSTERED ([CharacterID], [SkillID] ASC);
GO

-- Creating primary key on [DatapadID] in table 'Datapads'
ALTER TABLE [dbo].[Datapads]
ADD CONSTRAINT [PK_Datapads]
    PRIMARY KEY CLUSTERED ([DatapadID] ASC);
GO

-- Creating primary key on [DatapadID], [FileNumber] in table 'DatapadFiles'
ALTER TABLE [dbo].[DatapadFiles]
ADD CONSTRAINT [PK_DatapadFiles]
    PRIMARY KEY CLUSTERED ([DatapadID], [FileNumber] ASC);
GO

-- Creating primary key on [DatapadID], [HackingCharacterID], [AttemptNumber] in table 'DatapadHackings'
ALTER TABLE [dbo].[DatapadHackings]
ADD CONSTRAINT [PK_DatapadHackings]
    PRIMARY KEY CLUSTERED ([DatapadID], [HackingCharacterID], [AttemptNumber] ASC);
GO

-- Creating primary key on [IPv4Address] in table 'DeviceNetworks'
ALTER TABLE [dbo].[DeviceNetworks]
ADD CONSTRAINT [PK_DeviceNetworks]
    PRIMARY KEY CLUSTERED ([IPv4Address] ASC);
GO

-- Creating primary key on [DoorID] in table 'Doors'
ALTER TABLE [dbo].[Doors]
ADD CONSTRAINT [PK_Doors]
    PRIMARY KEY CLUSTERED ([DoorID] ASC);
GO

-- Creating primary key on [ElementID] in table 'Elements'
ALTER TABLE [dbo].[Elements]
ADD CONSTRAINT [PK_Elements]
    PRIMARY KEY CLUSTERED ([ElementID] ASC);
GO

-- Creating primary key on [AvailabilityID] in table 'ElementAvailabilities'
ALTER TABLE [dbo].[ElementAvailabilities]
ADD CONSTRAINT [PK_ElementAvailabilities]
    PRIMARY KEY CLUSTERED ([AvailabilityID] ASC);
GO

-- Creating primary key on [CompoundElementID], [ComponentNumber] in table 'ElementCompounds'
ALTER TABLE [dbo].[ElementCompounds]
ADD CONSTRAINT [PK_ElementCompounds]
    PRIMARY KEY CLUSTERED ([CompoundElementID], [ComponentNumber] ASC);
GO

-- Creating primary key on [ElementID], [QRCode] in table 'ElementQRCodes'
ALTER TABLE [dbo].[ElementQRCodes]
ADD CONSTRAINT [PK_ElementQRCodes]
    PRIMARY KEY CLUSTERED ([ElementID], [QRCode] ASC);
GO

-- Creating primary key on [EventID] in table 'Events'
ALTER TABLE [dbo].[Events]
ADD CONSTRAINT [PK_Events]
    PRIMARY KEY CLUSTERED ([EventID] ASC);
GO

-- Creating primary key on [EventID], [DatapadID] in table 'EventDatapads'
ALTER TABLE [dbo].[EventDatapads]
ADD CONSTRAINT [PK_EventDatapads]
    PRIMARY KEY CLUSTERED ([EventID], [DatapadID] ASC);
GO

-- Creating primary key on [EventID], [IndividualDay] in table 'EventDays'
ALTER TABLE [dbo].[EventDays]
ADD CONSTRAINT [PK_EventDays]
    PRIMARY KEY CLUSTERED ([EventID], [IndividualDay] ASC);
GO

-- Creating primary key on [EventID], [ElementID] in table 'EventElements'
ALTER TABLE [dbo].[EventElements]
ADD CONSTRAINT [PK_EventElements]
    PRIMARY KEY CLUSTERED ([EventID], [ElementID] ASC);
GO

-- Creating primary key on [HackingToolID] in table 'HackingTools'
ALTER TABLE [dbo].[HackingTools]
ADD CONSTRAINT [PK_HackingTools]
    PRIMARY KEY CLUSTERED ([HackingToolID] ASC);
GO

-- Creating primary key on [CharacterID] in table 'HeroCharacters'
ALTER TABLE [dbo].[HeroCharacters]
ADD CONSTRAINT [PK_HeroCharacters]
    PRIMARY KEY CLUSTERED ([CharacterID] ASC);
GO

-- Creating primary key on [InfectionID] in table 'Infections'
ALTER TABLE [dbo].[Infections]
ADD CONSTRAINT [PK_Infections]
    PRIMARY KEY CLUSTERED ([InfectionID] ASC);
GO

-- Creating primary key on [ElementID] in table 'Items'
ALTER TABLE [dbo].[Items]
ADD CONSTRAINT [PK_Items]
    PRIMARY KEY CLUSTERED ([ElementID] ASC);
GO

-- Creating primary key on [ItemTypeID] in table 'ItemTypes'
ALTER TABLE [dbo].[ItemTypes]
ADD CONSTRAINT [PK_ItemTypes]
    PRIMARY KEY CLUSTERED ([ItemTypeID] ASC);
GO

-- Creating primary key on [MessageID], [CharacterID] in table 'MailInBoxes'
ALTER TABLE [dbo].[MailInBoxes]
ADD CONSTRAINT [PK_MailInBoxes]
    PRIMARY KEY CLUSTERED ([MessageID], [CharacterID] ASC);
GO

-- Creating primary key on [MessageID], [CharacterID] in table 'MailOutBoxes'
ALTER TABLE [dbo].[MailOutBoxes]
ADD CONSTRAINT [PK_MailOutBoxes]
    PRIMARY KEY CLUSTERED ([MessageID], [CharacterID] ASC);
GO

-- Creating primary key on [MessageID] in table 'Messages'
ALTER TABLE [dbo].[Messages]
ADD CONSTRAINT [PK_Messages]
    PRIMARY KEY CLUSTERED ([MessageID] ASC);
GO

-- Creating primary key on [MessageID], [CharacterID], [AttemptNumber] in table 'MessageHackings'
ALTER TABLE [dbo].[MessageHackings]
ADD CONSTRAINT [PK_MessageHackings]
    PRIMARY KEY CLUSTERED ([MessageID], [CharacterID], [AttemptNumber] ASC);
GO

-- Creating primary key on [NetworkID] in table 'Networks'
ALTER TABLE [dbo].[Networks]
ADD CONSTRAINT [PK_Networks]
    PRIMARY KEY CLUSTERED ([NetworkID] ASC);
GO

-- Creating primary key on [NewsID], [CharacterID], [AttemptNumber] in table 'NewsHackings'
ALTER TABLE [dbo].[NewsHackings]
ADD CONSTRAINT [PK_NewsHackings]
    PRIMARY KEY CLUSTERED ([NewsID], [CharacterID], [AttemptNumber] ASC);
GO

-- Creating primary key on [NewsID] in table 'NewsStories'
ALTER TABLE [dbo].[NewsStories]
ADD CONSTRAINT [PK_NewsStories]
    PRIMARY KEY CLUSTERED ([NewsID] ASC);
GO

-- Creating primary key on [NetworkID], [Parameter1] in table 'Parameters'
ALTER TABLE [dbo].[Parameters]
ADD CONSTRAINT [PK_Parameters]
    PRIMARY KEY CLUSTERED ([NetworkID], [Parameter1] ASC);
GO

-- Creating primary key on [PlayerID] in table 'Players'
ALTER TABLE [dbo].[Players]
ADD CONSTRAINT [PK_Players]
    PRIMARY KEY CLUSTERED ([PlayerID] ASC);
GO

-- Creating primary key on [MessageID] in table 'SavedMessages'
ALTER TABLE [dbo].[SavedMessages]
ADD CONSTRAINT [PK_SavedMessages]
    PRIMARY KEY CLUSTERED ([MessageID] ASC);
GO

-- Creating primary key on [SkillID] in table 'Skills'
ALTER TABLE [dbo].[Skills]
ADD CONSTRAINT [PK_Skills]
    PRIMARY KEY CLUSTERED ([SkillID] ASC);
GO

-- Creating primary key on [SkillID], [EffectID] in table 'SkillEffects'
ALTER TABLE [dbo].[SkillEffects]
ADD CONSTRAINT [PK_SkillEffects]
    PRIMARY KEY CLUSTERED ([SkillID], [EffectID] ASC);
GO

-- Creating primary key on [SpeciesID] in table 'Species'
ALTER TABLE [dbo].[Species]
ADD CONSTRAINT [PK_Species]
    PRIMARY KEY CLUSTERED ([SpeciesID] ASC);
GO

-- Creating primary key on [ElementID] in table 'Substances'
ALTER TABLE [dbo].[Substances]
ADD CONSTRAINT [PK_Substances]
    PRIMARY KEY CLUSTERED ([ElementID] ASC);
GO

-- Creating primary key on [SubstanceTypeID] in table 'SubstanceTypes'
ALTER TABLE [dbo].[SubstanceTypes]
ADD CONSTRAINT [PK_SubstanceTypes]
    PRIMARY KEY CLUSTERED ([SubstanceTypeID] ASC);
GO

-- Creating primary key on [TalentID] in table 'Talents'
ALTER TABLE [dbo].[Talents]
ADD CONSTRAINT [PK_Talents]
    PRIMARY KEY CLUSTERED ([TalentID] ASC);
GO

-- Creating primary key on [Infections_InfectionID], [HeroCharacters_CharacterID] in table 'CharacterInfection'
ALTER TABLE [dbo].[CharacterInfection]
ADD CONSTRAINT [PK_CharacterInfection]
    PRIMARY KEY NONCLUSTERED ([Infections_InfectionID], [HeroCharacters_CharacterID] ASC);
GO

-- Creating primary key on [Talents_TalentID], [HeroCharacters_CharacterID] in table 'CharacterTalent'
ALTER TABLE [dbo].[CharacterTalent]
ADD CONSTRAINT [PK_CharacterTalent]
    PRIMARY KEY NONCLUSTERED ([Talents_TalentID], [HeroCharacters_CharacterID] ASC);
GO

-- Creating primary key on [DependentSkills_SkillID], [Prerequisites_SkillID] in table 'SkillPrerequisite'
ALTER TABLE [dbo].[SkillPrerequisite]
ADD CONSTRAINT [PK_SkillPrerequisite]
    PRIMARY KEY NONCLUSTERED ([DependentSkills_SkillID], [Prerequisites_SkillID] ASC);
GO

-- Creating primary key on [Talents_TalentID], [Skills_SkillID] in table 'TalentSkill'
ALTER TABLE [dbo].[TalentSkill]
ADD CONSTRAINT [PK_TalentSkill]
    PRIMARY KEY NONCLUSTERED ([Talents_TalentID], [Skills_SkillID] ASC);
GO

-- --------------------------------------------------
-- Creating all FOREIGN KEY constraints
-- --------------------------------------------------

-- Creating foreign key on [HackingCharacterID] in table 'AccountHackings'
ALTER TABLE [dbo].[AccountHackings]
ADD CONSTRAINT [FK_AccountHacking_0_0]
    FOREIGN KEY ([HackingCharacterID])
    REFERENCES [dbo].[HeroCharacters]
        ([CharacterID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_AccountHacking_0_0'
CREATE INDEX [IX_FK_AccountHacking_0_0]
ON [dbo].[AccountHackings]
    ([HackingCharacterID]);
GO

-- Creating foreign key on [HackedCharacterID] in table 'AccountHackings'
ALTER TABLE [dbo].[AccountHackings]
ADD CONSTRAINT [FK_AccountHacking_1_0]
    FOREIGN KEY ([HackedCharacterID])
    REFERENCES [dbo].[HeroCharacters]
        ([CharacterID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [CharacterID] in table 'AddressBooks'
ALTER TABLE [dbo].[AddressBooks]
ADD CONSTRAINT [FK_AddressBook_0_0]
    FOREIGN KEY ([CharacterID])
    REFERENCES [dbo].[HeroCharacters]
        ([CharacterID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [BankID] in table 'BankAccounts'
ALTER TABLE [dbo].[BankAccounts]
ADD CONSTRAINT [FK_BankAccount_0_0]
    FOREIGN KEY ([BankID])
    REFERENCES [dbo].[Banks]
        ([BankID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_BankAccount_0_0'
CREATE INDEX [IX_FK_BankAccount_0_0]
ON [dbo].[BankAccounts]
    ([BankID]);
GO

-- Creating foreign key on [CharacterID] in table 'BankAccounts'
ALTER TABLE [dbo].[BankAccounts]
ADD CONSTRAINT [FK_BankAccount_1_0]
    FOREIGN KEY ([CharacterID])
    REFERENCES [dbo].[HeroCharacters]
        ([CharacterID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_BankAccount_1_0'
CREATE INDEX [IX_FK_BankAccount_1_0]
ON [dbo].[BankAccounts]
    ([CharacterID]);
GO

-- Creating foreign key on [CharacterID] in table 'CharacterEventDays'
ALTER TABLE [dbo].[CharacterEventDays]
ADD CONSTRAINT [FK_CharacterEventDay_1_0]
    FOREIGN KEY ([CharacterID])
    REFERENCES [dbo].[HeroCharacters]
        ([CharacterID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_CharacterEventDay_1_0'
CREATE INDEX [IX_FK_CharacterEventDay_1_0]
ON [dbo].[CharacterEventDays]
    ([CharacterID]);
GO

-- Creating foreign key on [CharacterID] in table 'CharacterSkills'
ALTER TABLE [dbo].[CharacterSkills]
ADD CONSTRAINT [FK_CharacterSkill_0_0]
    FOREIGN KEY ([CharacterID])
    REFERENCES [dbo].[HeroCharacters]
        ([CharacterID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [TalentID] in table 'CharacterSkills'
ALTER TABLE [dbo].[CharacterSkills]
ADD CONSTRAINT [FK_CharacterSkill_1_0]
    FOREIGN KEY ([TalentID])
    REFERENCES [dbo].[Talents]
        ([TalentID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_CharacterSkill_1_0'
CREATE INDEX [IX_FK_CharacterSkill_1_0]
ON [dbo].[CharacterSkills]
    ([TalentID]);
GO

-- Creating foreign key on [SkillID] in table 'CharacterSkills'
ALTER TABLE [dbo].[CharacterSkills]
ADD CONSTRAINT [FK_CharacterSkill_2_0]
    FOREIGN KEY ([SkillID])
    REFERENCES [dbo].[Skills]
        ([SkillID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_CharacterSkill_2_0'
CREATE INDEX [IX_FK_CharacterSkill_2_0]
ON [dbo].[CharacterSkills]
    ([SkillID]);
GO

-- Creating foreign key on [TalentID] in table 'CharacterUnlockedSkills'
ALTER TABLE [dbo].[CharacterUnlockedSkills]
ADD CONSTRAINT [FK_CharacterUnlockedSkill_0_0]
    FOREIGN KEY ([TalentID])
    REFERENCES [dbo].[Talents]
        ([TalentID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_CharacterUnlockedSkill_0_0'
CREATE INDEX [IX_FK_CharacterUnlockedSkill_0_0]
ON [dbo].[CharacterUnlockedSkills]
    ([TalentID]);
GO

-- Creating foreign key on [SkillID] in table 'CharacterUnlockedSkills'
ALTER TABLE [dbo].[CharacterUnlockedSkills]
ADD CONSTRAINT [FK_CharacterUnlockedSkill_1_0]
    FOREIGN KEY ([SkillID])
    REFERENCES [dbo].[Skills]
        ([SkillID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_CharacterUnlockedSkill_1_0'
CREATE INDEX [IX_FK_CharacterUnlockedSkill_1_0]
ON [dbo].[CharacterUnlockedSkills]
    ([SkillID]);
GO

-- Creating foreign key on [CharacterID] in table 'CharacterUnlockedSkills'
ALTER TABLE [dbo].[CharacterUnlockedSkills]
ADD CONSTRAINT [FK_CharacterUnlockedSkill_2_0]
    FOREIGN KEY ([CharacterID])
    REFERENCES [dbo].[HeroCharacters]
        ([CharacterID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [DatapadID] in table 'DatapadFiles'
ALTER TABLE [dbo].[DatapadFiles]
ADD CONSTRAINT [FK_DatapadFiles_0_0]
    FOREIGN KEY ([DatapadID])
    REFERENCES [dbo].[Datapads]
        ([DatapadID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [DatapadID] in table 'DatapadHackings'
ALTER TABLE [dbo].[DatapadHackings]
ADD CONSTRAINT [FK_DatapadHacking_0_0]
    FOREIGN KEY ([DatapadID])
    REFERENCES [dbo].[Datapads]
        ([DatapadID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [DatapadID] in table 'EventDatapads'
ALTER TABLE [dbo].[EventDatapads]
ADD CONSTRAINT [FK_EventDatapad_0_0]
    FOREIGN KEY ([DatapadID])
    REFERENCES [dbo].[Datapads]
        ([DatapadID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_EventDatapad_0_0'
CREATE INDEX [IX_FK_EventDatapad_0_0]
ON [dbo].[EventDatapads]
    ([DatapadID]);
GO

-- Creating foreign key on [HackingCharacterID] in table 'DatapadHackings'
ALTER TABLE [dbo].[DatapadHackings]
ADD CONSTRAINT [FK_DatapadHacking_1_0]
    FOREIGN KEY ([HackingCharacterID])
    REFERENCES [dbo].[HeroCharacters]
        ([CharacterID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_DatapadHacking_1_0'
CREATE INDEX [IX_FK_DatapadHacking_1_0]
ON [dbo].[DatapadHackings]
    ([HackingCharacterID]);
GO

-- Creating foreign key on [NetworkID] in table 'DeviceNetworks'
ALTER TABLE [dbo].[DeviceNetworks]
ADD CONSTRAINT [FK_DeviceNetwork_0_0]
    FOREIGN KEY ([NetworkID])
    REFERENCES [dbo].[Networks]
        ([NetworkID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_DeviceNetwork_0_0'
CREATE INDEX [IX_FK_DeviceNetwork_0_0]
ON [dbo].[DeviceNetworks]
    ([NetworkID]);
GO

-- Creating foreign key on [AvailabilityID] in table 'Elements'
ALTER TABLE [dbo].[Elements]
ADD CONSTRAINT [FK_Element_0_0]
    FOREIGN KEY ([AvailabilityID])
    REFERENCES [dbo].[ElementAvailabilities]
        ([AvailabilityID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_Element_0_0'
CREATE INDEX [IX_FK_Element_0_0]
ON [dbo].[Elements]
    ([AvailabilityID]);
GO

-- Creating foreign key on [ComponentElementID] in table 'ElementCompounds'
ALTER TABLE [dbo].[ElementCompounds]
ADD CONSTRAINT [FK_ElementCompound_0_0]
    FOREIGN KEY ([ComponentElementID])
    REFERENCES [dbo].[Elements]
        ([ElementID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_ElementCompound_0_0'
CREATE INDEX [IX_FK_ElementCompound_0_0]
ON [dbo].[ElementCompounds]
    ([ComponentElementID]);
GO

-- Creating foreign key on [CompoundElementID] in table 'ElementCompounds'
ALTER TABLE [dbo].[ElementCompounds]
ADD CONSTRAINT [FK_ElementCompound_1_0]
    FOREIGN KEY ([CompoundElementID])
    REFERENCES [dbo].[Elements]
        ([ElementID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [ElementID] in table 'ElementQRCodes'
ALTER TABLE [dbo].[ElementQRCodes]
ADD CONSTRAINT [FK_ElementQRCode_0_0]
    FOREIGN KEY ([ElementID])
    REFERENCES [dbo].[Elements]
        ([ElementID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [ElementID] in table 'EventElements'
ALTER TABLE [dbo].[EventElements]
ADD CONSTRAINT [FK_EventElement_0_0]
    FOREIGN KEY ([ElementID])
    REFERENCES [dbo].[Elements]
        ([ElementID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_EventElement_0_0'
CREATE INDEX [IX_FK_EventElement_0_0]
ON [dbo].[EventElements]
    ([ElementID]);
GO

-- Creating foreign key on [ElementID] in table 'Items'
ALTER TABLE [dbo].[Items]
ADD CONSTRAINT [FK_Item_1_0]
    FOREIGN KEY ([ElementID])
    REFERENCES [dbo].[Elements]
        ([ElementID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [ElementID] in table 'Substances'
ALTER TABLE [dbo].[Substances]
ADD CONSTRAINT [FK_Substance_1_0]
    FOREIGN KEY ([ElementID])
    REFERENCES [dbo].[Elements]
        ([ElementID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [EventID] in table 'EventDatapads'
ALTER TABLE [dbo].[EventDatapads]
ADD CONSTRAINT [FK_EventDatapad_1_0]
    FOREIGN KEY ([EventID])
    REFERENCES [dbo].[Events]
        ([EventID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [EventID] in table 'EventDays'
ALTER TABLE [dbo].[EventDays]
ADD CONSTRAINT [FK_EventDays_0_0]
    FOREIGN KEY ([EventID])
    REFERENCES [dbo].[Events]
        ([EventID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [EventID] in table 'EventElements'
ALTER TABLE [dbo].[EventElements]
ADD CONSTRAINT [FK_EventElement_1_0]
    FOREIGN KEY ([EventID])
    REFERENCES [dbo].[Events]
        ([EventID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [NetworkID] in table 'HeroCharacters'
ALTER TABLE [dbo].[HeroCharacters]
ADD CONSTRAINT [FK_HeroCharacter_4_0]
    FOREIGN KEY ([NetworkID])
    REFERENCES [dbo].[Networks]
        ([NetworkID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_HeroCharacter_4_0'
CREATE INDEX [IX_FK_HeroCharacter_4_0]
ON [dbo].[HeroCharacters]
    ([NetworkID]);
GO

-- Creating foreign key on [SpeciesID] in table 'HeroCharacters'
ALTER TABLE [dbo].[HeroCharacters]
ADD CONSTRAINT [FK_HeroCharacter_5_0]
    FOREIGN KEY ([SpeciesID])
    REFERENCES [dbo].[Species]
        ([SpeciesID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_HeroCharacter_5_0'
CREATE INDEX [IX_FK_HeroCharacter_5_0]
ON [dbo].[HeroCharacters]
    ([SpeciesID]);
GO

-- Creating foreign key on [PlayerID] in table 'HeroCharacters'
ALTER TABLE [dbo].[HeroCharacters]
ADD CONSTRAINT [FK_HeroCharacter_6_0]
    FOREIGN KEY ([PlayerID])
    REFERENCES [dbo].[Players]
        ([PlayerID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_HeroCharacter_6_0'
CREATE INDEX [IX_FK_HeroCharacter_6_0]
ON [dbo].[HeroCharacters]
    ([PlayerID]);
GO

-- Creating foreign key on [CharacterID] in table 'MailInBoxes'
ALTER TABLE [dbo].[MailInBoxes]
ADD CONSTRAINT [FK_MailInBox_1_0]
    FOREIGN KEY ([CharacterID])
    REFERENCES [dbo].[HeroCharacters]
        ([CharacterID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_MailInBox_1_0'
CREATE INDEX [IX_FK_MailInBox_1_0]
ON [dbo].[MailInBoxes]
    ([CharacterID]);
GO

-- Creating foreign key on [CharacterID] in table 'MailOutBoxes'
ALTER TABLE [dbo].[MailOutBoxes]
ADD CONSTRAINT [FK_MailOutBox_1_0]
    FOREIGN KEY ([CharacterID])
    REFERENCES [dbo].[HeroCharacters]
        ([CharacterID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_MailOutBox_1_0'
CREATE INDEX [IX_FK_MailOutBox_1_0]
ON [dbo].[MailOutBoxes]
    ([CharacterID]);
GO

-- Creating foreign key on [SenderCharacter] in table 'Messages'
ALTER TABLE [dbo].[Messages]
ADD CONSTRAINT [FK_Message_0_0]
    FOREIGN KEY ([SenderCharacter])
    REFERENCES [dbo].[HeroCharacters]
        ([CharacterID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_Message_0_0'
CREATE INDEX [IX_FK_Message_0_0]
ON [dbo].[Messages]
    ([SenderCharacter]);
GO

-- Creating foreign key on [CharacterID] in table 'MessageHackings'
ALTER TABLE [dbo].[MessageHackings]
ADD CONSTRAINT [FK_MessageHacking_0_0]
    FOREIGN KEY ([CharacterID])
    REFERENCES [dbo].[HeroCharacters]
        ([CharacterID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_MessageHacking_0_0'
CREATE INDEX [IX_FK_MessageHacking_0_0]
ON [dbo].[MessageHackings]
    ([CharacterID]);
GO

-- Creating foreign key on [CharacterID] in table 'NewsHackings'
ALTER TABLE [dbo].[NewsHackings]
ADD CONSTRAINT [FK_NewsHacking_0_0]
    FOREIGN KEY ([CharacterID])
    REFERENCES [dbo].[HeroCharacters]
        ([CharacterID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_NewsHacking_0_0'
CREATE INDEX [IX_FK_NewsHacking_0_0]
ON [dbo].[NewsHackings]
    ([CharacterID]);
GO

-- Creating foreign key on [AuthorCharacterID] in table 'NewsStories'
ALTER TABLE [dbo].[NewsStories]
ADD CONSTRAINT [FK_NewsStory_0_0]
    FOREIGN KEY ([AuthorCharacterID])
    REFERENCES [dbo].[HeroCharacters]
        ([CharacterID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_NewsStory_0_0'
CREATE INDEX [IX_FK_NewsStory_0_0]
ON [dbo].[NewsStories]
    ([AuthorCharacterID]);
GO

-- Creating foreign key on [CharacterID] in table 'SavedMessages'
ALTER TABLE [dbo].[SavedMessages]
ADD CONSTRAINT [FK_SavedMessage_0_0]
    FOREIGN KEY ([CharacterID])
    REFERENCES [dbo].[HeroCharacters]
        ([CharacterID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_SavedMessage_0_0'
CREATE INDEX [IX_FK_SavedMessage_0_0]
ON [dbo].[SavedMessages]
    ([CharacterID]);
GO

-- Creating foreign key on [ItemTypeID] in table 'Items'
ALTER TABLE [dbo].[Items]
ADD CONSTRAINT [FK_Item_0_0]
    FOREIGN KEY ([ItemTypeID])
    REFERENCES [dbo].[ItemTypes]
        ([ItemTypeID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_Item_0_0'
CREATE INDEX [IX_FK_Item_0_0]
ON [dbo].[Items]
    ([ItemTypeID]);
GO

-- Creating foreign key on [MessageID] in table 'MailInBoxes'
ALTER TABLE [dbo].[MailInBoxes]
ADD CONSTRAINT [FK_MailInBox_0_0]
    FOREIGN KEY ([MessageID])
    REFERENCES [dbo].[Messages]
        ([MessageID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [MessageID] in table 'MailOutBoxes'
ALTER TABLE [dbo].[MailOutBoxes]
ADD CONSTRAINT [FK_MailOutBox_0_0]
    FOREIGN KEY ([MessageID])
    REFERENCES [dbo].[Messages]
        ([MessageID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [MessageID] in table 'MessageHackings'
ALTER TABLE [dbo].[MessageHackings]
ADD CONSTRAINT [FK_MessageHacking_1_0]
    FOREIGN KEY ([MessageID])
    REFERENCES [dbo].[Messages]
        ([MessageID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [NetworkID] in table 'NewsStories'
ALTER TABLE [dbo].[NewsStories]
ADD CONSTRAINT [FK_NewsStory_1_0]
    FOREIGN KEY ([NetworkID])
    REFERENCES [dbo].[Networks]
        ([NetworkID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_NewsStory_1_0'
CREATE INDEX [IX_FK_NewsStory_1_0]
ON [dbo].[NewsStories]
    ([NetworkID]);
GO

-- Creating foreign key on [NetworkID] in table 'Parameters'
ALTER TABLE [dbo].[Parameters]
ADD CONSTRAINT [FK_Parameter_0_0]
    FOREIGN KEY ([NetworkID])
    REFERENCES [dbo].[Networks]
        ([NetworkID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [NewsID] in table 'NewsHackings'
ALTER TABLE [dbo].[NewsHackings]
ADD CONSTRAINT [FK_NewsHacking_1_0]
    FOREIGN KEY ([NewsID])
    REFERENCES [dbo].[NewsStories]
        ([NewsID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [SkillID] in table 'SkillEffects'
ALTER TABLE [dbo].[SkillEffects]
ADD CONSTRAINT [FK_SkillEffect_0_0]
    FOREIGN KEY ([SkillID])
    REFERENCES [dbo].[Skills]
        ([SkillID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [SubstanceTypeID] in table 'Substances'
ALTER TABLE [dbo].[Substances]
ADD CONSTRAINT [FK_Substance_0_0]
    FOREIGN KEY ([SubstanceTypeID])
    REFERENCES [dbo].[SubstanceTypes]
        ([SubstanceTypeID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_Substance_0_0'
CREATE INDEX [IX_FK_Substance_0_0]
ON [dbo].[Substances]
    ([SubstanceTypeID]);
GO

-- Creating foreign key on [Infections_InfectionID] in table 'CharacterInfection'
ALTER TABLE [dbo].[CharacterInfection]
ADD CONSTRAINT [FK_CharacterInfection_Infection]
    FOREIGN KEY ([Infections_InfectionID])
    REFERENCES [dbo].[Infections]
        ([InfectionID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [HeroCharacters_CharacterID] in table 'CharacterInfection'
ALTER TABLE [dbo].[CharacterInfection]
ADD CONSTRAINT [FK_CharacterInfection_HeroCharacter]
    FOREIGN KEY ([HeroCharacters_CharacterID])
    REFERENCES [dbo].[HeroCharacters]
        ([CharacterID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_CharacterInfection_HeroCharacter'
CREATE INDEX [IX_FK_CharacterInfection_HeroCharacter]
ON [dbo].[CharacterInfection]
    ([HeroCharacters_CharacterID]);
GO

-- Creating foreign key on [Talents_TalentID] in table 'CharacterTalent'
ALTER TABLE [dbo].[CharacterTalent]
ADD CONSTRAINT [FK_CharacterTalent_Talent]
    FOREIGN KEY ([Talents_TalentID])
    REFERENCES [dbo].[Talents]
        ([TalentID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [HeroCharacters_CharacterID] in table 'CharacterTalent'
ALTER TABLE [dbo].[CharacterTalent]
ADD CONSTRAINT [FK_CharacterTalent_HeroCharacter]
    FOREIGN KEY ([HeroCharacters_CharacterID])
    REFERENCES [dbo].[HeroCharacters]
        ([CharacterID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_CharacterTalent_HeroCharacter'
CREATE INDEX [IX_FK_CharacterTalent_HeroCharacter]
ON [dbo].[CharacterTalent]
    ([HeroCharacters_CharacterID]);
GO

-- Creating foreign key on [DependentSkills_SkillID] in table 'SkillPrerequisite'
ALTER TABLE [dbo].[SkillPrerequisite]
ADD CONSTRAINT [FK_SkillPrerequisite_Skill]
    FOREIGN KEY ([DependentSkills_SkillID])
    REFERENCES [dbo].[Skills]
        ([SkillID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [Prerequisites_SkillID] in table 'SkillPrerequisite'
ALTER TABLE [dbo].[SkillPrerequisite]
ADD CONSTRAINT [FK_SkillPrerequisite_Skill1]
    FOREIGN KEY ([Prerequisites_SkillID])
    REFERENCES [dbo].[Skills]
        ([SkillID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_SkillPrerequisite_Skill1'
CREATE INDEX [IX_FK_SkillPrerequisite_Skill1]
ON [dbo].[SkillPrerequisite]
    ([Prerequisites_SkillID]);
GO

-- Creating foreign key on [Talents_TalentID] in table 'TalentSkill'
ALTER TABLE [dbo].[TalentSkill]
ADD CONSTRAINT [FK_TalentSkill_Talent]
    FOREIGN KEY ([Talents_TalentID])
    REFERENCES [dbo].[Talents]
        ([TalentID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;
GO

-- Creating foreign key on [Skills_SkillID] in table 'TalentSkill'
ALTER TABLE [dbo].[TalentSkill]
ADD CONSTRAINT [FK_TalentSkill_Skill]
    FOREIGN KEY ([Skills_SkillID])
    REFERENCES [dbo].[Skills]
        ([SkillID])
    ON DELETE NO ACTION ON UPDATE NO ACTION;

-- Creating non-clustered index for FOREIGN KEY 'FK_TalentSkill_Skill'
CREATE INDEX [IX_FK_TalentSkill_Skill]
ON [dbo].[TalentSkill]
    ([Skills_SkillID]);
GO

-- --------------------------------------------------
-- Script has ended
-- --------------------------------------------------
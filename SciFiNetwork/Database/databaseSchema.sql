
-- Table: Network
CREATE TABLE Network ( 
    NetworkID  INTEGER        PRIMARY KEY AUTOINCREMENT
                               NOT NULL
                               DEFAULT 0,
    NetworkName    VARCHAR( 60 )  NOT NULL,
    Description TEXT,
    Faction     VARCHAR( 60 ) 
);


-- Table: Species
CREATE TABLE Species ( 
    SpeciesID      INTEGER        PRIMARY KEY AUTOINCREMENT
                               NOT NULL
							   DEFAULT 0,
    Name        VARCHAR( 60 )	NOT NULL,
    Description TEXT 
);


-- Table: Player
CREATE TABLE Player ( 
    PlayerID       	INTEGER			PRIMARY KEY AUTOINCREMENT
									NOT NULL
									DEFAULT '0',
    MainName        VARCHAR( 60 )  	NOT NULL,
    FirstName		VARCHAR( 30 ),
    DateOfBirth    	DATETIME       	NOT NULL
									DEFAULT 'CURRENT_DATE',
    Address			VARCHAR( 60 ),
    City          	VARCHAR( 60 ),
    CountyOrArea	VARCHAR( 60 ),
    PostCode		VARCHAR( 10 ),
    PhoneNumber     VARCHAR( 20 ),
    DateOfSubscription DATETIME       NOT NULL
                                  DEFAULT 'CURRENT_DATE',
    Sex          CHAR           NOT NULL,
    PlayerCathegory  VARCHAR( 10 )   NOT NULL
                                  DEFAULT 'PC',
    Email          VARCHAR( 60 ),
    Password       VARCHAR( 60 ) 
);


-- Table: MailOutBox
CREATE TABLE MailOutBox ( 
    MessageID    INTEGER  NOT NULL
                               DEFAULT '0',
    CharacterID          INTEGER  NOT NULL
                               DEFAULT '0',
    IsDeleted        BOOLEAN 
						NOT NULL 
						DEFAULT '0',
    IsRead             BOOLEAN 
						NOT NULL 
						DEFAULT '0',
    DateOfDeletion DATETIME,
    CONSTRAINT [PK_MailOutBox] PRIMARY KEY ( MessageID, CharacterID ),
    CONSTRAINT [FK_MailOutBox_0] FOREIGN KEY ( CharacterID ) REFERENCES HeroCharacter ( CharacterID ),
    CONSTRAINT [FK_MailOutBox_1] FOREIGN KEY ( MessageID ) REFERENCES Message ( MessageID ) 
);


-- Table: MailInBox
CREATE TABLE MailInBox ( 
    MessageID    INTEGER  NOT NULL
                               DEFAULT '0',
    CharacterID          INTEGER  NOT NULL
                               DEFAULT '0',
    IsDeleted        BOOLEAN  DEFAULT '0',
    IsRead             BOOLEAN  DEFAULT '0',
    DateOfDeletion DATETIME,
    CONSTRAINT [PK_MailInBox] PRIMARY KEY ( MessageID, CharacterID ),
    CONSTRAINT [FK_MailInBox_0] FOREIGN KEY ( CharacterID ) REFERENCES HeroCharacter ( CharacterID ),
    CONSTRAINT [FK_MailInBox_1] FOREIGN KEY ( MessageID ) REFERENCES Message ( MessageID ) 
);


-- Table: AccountHacking
CREATE TABLE AccountHacking ( 
    HackedCharacterID INTEGER  REFERENCES HeroCharacter ( CharacterID ),
    HackingCharacterID  INTEGER  REFERENCES HeroCharacter ( CharacterID ),
    AttemptNumber INTEGER NOT NULL DEFAULT 0,
    Success        BOOLEAN  DEFAULT '0',
    DateOfAttempt   DATETIME DEFAULT 'CURRENT_TIME',
    PRIMARY KEY ( HackedCharacterID, HackingCharacterID, AttemptNumber ) 
);


-- Table: AddressBook
CREATE TABLE AddressBook ( 
    CharacterID         INTEGER NOT NULL
                             DEFAULT '0',
    SavedCharacterID    INTEGER NOT NULL,
    SavedCharacterName TEXT,
    CONSTRAINT [PK_AddressBook] PRIMARY KEY ( CharacterID, SavedCharacterID ),
    CONSTRAINT [FK_AddressBook_CharacterID_HeroCharacter_CharacterID] FOREIGN KEY ( CharacterID ) REFERENCES HeroCharacter ( CharacterID ) 
);


-- Table: HeroCharacter
CREATE TABLE HeroCharacter ( 
    CharacterID           INTEGER        PRIMARY KEY AUTOINCREMENT
                                      NOT NULL
                                      DEFAULT '0',
    PlayerID           INTEGER        NOT NULL
                                      DEFAULT '0'
									  REFERENCES Player ( PlayerID ),
    Name               VARCHAR( 60 )  NOT NULL,
    SpeciesID             INTEGER        NOT NULL
										 REFERENCES Species ( SpeciesID ),
    Type               INTEGER        NOT NULL,
    ExperiencePoints              INTEGER        NOT NULL
                                      DEFAULT '30',
    Sex              CHAR           NOT NULL,
    DarkSidePoints         INTEGER        NOT NULL
                                      DEFAULT '5',
    DateOfCreation      DATETIME       NOT NULL
                                      DEFAULT 'CURRENT_DATE',
    HonorificTitle             VARCHAR( 60 ),
    IsAlive               BOOL        NOT NULL
                                      DEFAULT 1,
    DateOfDeath          DATETIME,
    LarpNetworkPassword    	VARCHAR( 50 ),
    BloodFormula			VARCHAR( 20 )  DEFAULT '0',
    NetworkID            INTEGER        DEFAULT '0'
                                      REFERENCES Network ( NetworkID ),
    LastAccountEncryption  DATETIME,
    EncryptionLevel INTEGER        NOT NULL
                                      DEFAULT '0',
    QRCode           GUID,
    CONSTRAINT 'FK_HeroCharacter_0' FOREIGN KEY ( CharacterID ) REFERENCES AddressBook ( CharacterID ),
    CONSTRAINT 'FK_HeroCharacter_1' FOREIGN KEY ( CharacterID ) REFERENCES AccountHacking ( HackedCharacterID ),
    CONSTRAINT 'FK_HeroCharacter_2' FOREIGN KEY ( CharacterID ) REFERENCES MailInBox ( CharacterID ),
    CONSTRAINT 'FK_HeroCharacter_3' FOREIGN KEY ( CharacterID ) REFERENCES MailOutBox ( CharacterID )
);


-- Table: Message
CREATE TABLE Message ( 
    MessageID INTEGER        PRIMARY KEY AUTOINCREMENT
                                  NOT NULL
                                  DEFAULT 0,
    SenderCharacter       INTEGER        NOT NULL
                                  DEFAULT 0,
    Subject         VARCHAR( 60 ),
    Message          TEXT           NOT NULL,
    HackingLevel INTEGER        NOT NULL
                                  DEFAULT 0,
    IsActive         BOOLEAN        NOT NULL
                                  DEFAULT 0,
    DateOfCreation  DATETIME       NOT NULL
                                  DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT [FK_Message_0] FOREIGN KEY ( MessageID ) REFERENCES MailInBox ( MessageID ),
    CONSTRAINT [FK_Message_1] FOREIGN KEY ( SenderCharacter ) REFERENCES HeroCharacter ( CharacterID ) 
);


-- Table: ElementAvailability
CREATE TABLE ElementAvailability ( 
    AvailabilityID INTEGER PRIMARY KEY AUTOINCREMENT,
    Description TEXT 
);


-- Table: Element
CREATE TABLE Element ( 
    ElementID   INTEGER        PRIMARY KEY AUTOINCREMENT
                                 NOT NULL,
    Name          VARCHAR( 60 ),
    Description   TEXT,
    Effect       TEXT,
    Price         REAL           NOT NULL
                                 DEFAULT 0,
    Picture      TEXT,
    AvailabilityID INTEGER        NOT NULL,
    ExpirationDate  DATE,
    Discriminator INTEGER        NOT NULL
                                 DEFAULT 0,
    CONSTRAINT [FK_Element_0] FOREIGN KEY ( AvailabilityID ) REFERENCES ElementAvailability ( AvailabilityID ) 
);


-- Table: Talent
CREATE TABLE Talent ( 
    TalentID VARCHAR( 10 )  PRIMARY KEY
                                NOT NULL,
    Name         VARCHAR( 60 )  NOT NULL,
    Description  TEXT,
    TalentType         INTEGER        NOT NULL
                                DEFAULT 0 
);


-- Table: Skill
CREATE TABLE Skill ( 
    SkillID     INTEGER        PRIMARY KEY AUTOINCREMENT
                                 NOT NULL,
    Name          VARCHAR( 60 )  NOT NULL,
    Description   TEXT,
    AcquireManyTimes BOOLEAN        NOT NULL
                                 DEFAULT 0,
    ExperiencePointsCost         INTEGER        NOT NULL
                                 DEFAULT 0,
    IsAdvanced  BOOLEAN        NOT NULL
                                 DEFAULT 0 
);


-- Table: Event
CREATE TABLE Event ( 
    EventID       INTEGER        PRIMARY KEY AUTOINCREMENT
                                  NOT NULL,
    Title   		VARCHAR( 60 )  NOT NULL,
    StartDate     DATETIME       NOT NULL,
    TotalExperiencePoints INTEGER        NOT NULL
                                  DEFAULT 1,
    Description    TEXT,
    FullEventPrice          FLOAT          NOT NULL
                                  DEFAULT '1,00',
    Collection        FLOAT,
    IsClosed         BOOLEAN        NOT NULL
                                  DEFAULT 0 
);


-- Table: Datapad
CREATE TABLE Datapad ( 
    DatapadID INTEGER PRIMARY KEY AUTOINCREMENT
                        NOT NULL,
    Content   TEXT,
    HackingLevel     INTEGER NOT NULL
                        DEFAULT 0,
    QRCode    GUID 
);


-- Table: Infection
CREATE TABLE Infection ( 
    InfectionID INTEGER        PRIMARY KEY AUTOINCREMENT
                               NOT NULL
                               DEFAULT 0,
    Name        VARCHAR( 60 )  NOT NULL,
    Description TEXT 
);


-- Table: Bank
CREATE TABLE Bank ( 
    BankID  INTEGER        PRIMARY KEY AUTOINCREMENT
                                NOT NULL
                                DEFAULT 0,
    Name    VARCHAR( 60 )  NOT NULL,
    BaseInterestRate        INTEGER,
    InterestTimeInterval INTEGER,
    Price        INTEGER,
    PriceTimeInterval INTEGER 
);


-- Table: HackingTool
CREATE TABLE HackingTool ( 
    HackingToolID INTEGER PRIMARY KEY AUTOINCREMENT
                        NOT NULL,
    QRCode		GUID NOT NULL,
    HackerLevel     INTEGER NOT NULL
                        DEFAULT 1 
);


-- Table: Door
CREATE TABLE Door ( 
    DoorID    INTEGER        PRIMARY KEY AUTOINCREMENT
                                  NOT NULL,
	Reference		VARCHAR( 60 ),
    QRCode		GUID NOT NULL,
    HackingLevel INTEGER        NOT NULL
                                  DEFAULT 1 
);


-- Table: BankAccount
CREATE TABLE BankAccount ( 
    AccountID        INTEGER  PRIMARY KEY AUTOINCREMENT
                                NOT NULL
                                DEFAULT 0,
    BankID        INTEGER  NOT NULL,
    CharacterID           INTEGER  NOT NULL,
    InterestTimeInterval       INTEGER,
    Price              INTEGER,
    PriceTimeInterval       INTEGER,
    InterestRate              INTEGER,
    SubscriptionDate DATETIME,
    Amount             DOUBLE   NOT NULL
                                DEFAULT 0,
    CONSTRAINT [FK_BankAccount_0] FOREIGN KEY ( CharacterID ) REFERENCES HeroCharacter ( CharacterID ),
    CONSTRAINT [FK_BankAccount_BankID_Bank_BankID] FOREIGN KEY ( BankID ) REFERENCES Bank ( BankID ) 
);

-- Table: SavedMessage
CREATE TABLE SavedMessage ( 
    MessageID INTEGER        PRIMARY KEY AUTOINCREMENT
                               NOT NULL,
    CharacterID    INTEGER        NOT NULL
                               DEFAULT 0,
    Subject      TEXT           NOT NULL,
    Source VARCHAR( 60 ),
    Message   TEXT,
    HackingLevel     INTEGER        NOT NULL
                               DEFAULT 0,
    CONSTRAINT [FK_SavedMessage_CharacterID_HeroCharacter_CharacterID] FOREIGN KEY ( CharacterID ) REFERENCES HeroCharacter ( CharacterID ) 
);


-- Table: DatapadFiles
CREATE TABLE DatapadFiles ( 
    DatapadID  INTEGER        NOT NULL,
    FileNumber   INTEGER        NOT NULL,
    FileName     VARCHAR( 60 ),
    Content    TEXT,
    EncryptionLevel INTEGER        DEFAULT '0',
    CONSTRAINT [PK_DatapadFiles] PRIMARY KEY ( DatapadID, FileNumber ),
    CONSTRAINT [FK_DatapadFiles_DatapadID_Datapad_DatapadID] FOREIGN KEY ( DatapadID ) REFERENCES Datapad ( DatapadID ) 
);


-- Table: DatapadHacking
CREATE TABLE DatapadHacking ( 
    DatapadID INTEGER  NOT NULL,
    HackingCharacterID   INTEGER  NOT NULL,
    AttemptNumber  INTEGER  NOT NULL,
    Success         BOOLEAN  DEFAULT '0',
    DateOfAttempt    DATETIME DEFAULT 'CURRENT_TIME',
    CONSTRAINT [PK_DatapadHacking] PRIMARY KEY ( DatapadID, HackingCharacterID, AttemptNumber ),
    CONSTRAINT [FK_DatapadHacking_0] FOREIGN KEY ( HackingCharacterID ) REFERENCES HeroCharacter ( CharacterID ),
    CONSTRAINT [FK_DatapadHacking_DatapadID_Datapad_DatapadID] FOREIGN KEY ( DatapadID ) REFERENCES Datapad ( DatapadID ) 
);


-- Table: EventDays
CREATE TABLE EventDays ( 
    EventID       INTEGER  REFERENCES Event ( EventID ),
    IndividualDay     DATE,
    ExperiencePoints INTEGER,
    InGameTime     DATETIME NOT NULL,
    OffGameTime  DATETIME NOT NULL,
    PricePerDay    FLOAT    NULL,
    PRIMARY KEY ( EventID, IndividualDay ) 
);


-- Table: CharacterEventDay
CREATE TABLE CharacterEventDay ( 
    EventID      INTEGER  NOT NULL,
    IndividualDay    DATE     NOT NULL,
    CharacterID      INTEGER  NOT NULL
                           REFERENCES HeroCharacter ( CharacterID ),
    HasPaid        BOOLEAN  NOT NULL
                           DEFAULT ( 0 ),
    PaymentDate DATETIME,
    TookPart   BOOLEAN  NOT NULL
                           DEFAULT ( 0 ),
    PRIMARY KEY ( EventID, IndividualDay, CharacterID ),
    FOREIGN KEY ( EventID, IndividualDay ) REFERENCES EventDays ( EventID, IndividualDay ) ON DELETE CASCADE 
);


-- Table: TalentSkill
CREATE TABLE TalentSkill ( 
    SkillID    INTEGER        NOT NULL
                                REFERENCES Skill ( SkillID ),
    TalentID VARCHAR( 10 )  NOT NULL
                                REFERENCES Talent ( TalentID ),
    PRIMARY KEY ( SkillID, TalentID ) 
);


-- Table: CharacterTalent
CREATE TABLE CharacterTalent ( 
    CharacterID     INTEGER        REFERENCES HeroCharacter ( CharacterID ),
    TalentID VARCHAR( 10 )  REFERENCES Talent ( TalentID ),
    PRIMARY KEY ( CharacterID, TalentID ) 
);


-- Table: ElementQRCode
CREATE TABLE ElementQRCode ( 
    ElementID INTEGER REFERENCES Element ( ElementID ),
    QRCode      GUID    UNIQUE ON CONFLICT ROLLBACK,
    PRIMARY KEY ( ElementID, QRCode ) 
);


-- Table: ElementCompound
CREATE TABLE ElementCompound ( 
    CompoundElementID         INTEGER REFERENCES Element ( ElementID ),
    ComponentNumber INTEGER DEFAULT ( 0 ),
    ComponentElementID       INTEGER REFERENCES Element ( ElementID ),
    PRIMARY KEY ( CompoundElementID ASC, ComponentNumber ASC ) 
);


-- Table: SubstanceType
CREATE TABLE SubstanceType ( 
    SubstanceTypeID INTEGER        PRIMARY KEY AUTOINCREMENT,
    Description VARCHAR( 60 )  NOT NULL 
);


-- Table: Substance
CREATE TABLE Substance ( 
    ElementID INTEGER        PRIMARY KEY
                                       REFERENCES Element ( ElementID ),
    ApplicationMode             VARCHAR( 10 )  NOT NULL,
    StrengthValue     INTEGER        NOT NULL
                                       DEFAULT ( 1 ),
    SubstanceTypeID                INTEGER        NOT NULL
                                       DEFAULT ( 0 ) 
                                       REFERENCES SubstanceType ( SubstanceTypeID ) 
);


-- Table: ItemType
CREATE TABLE ItemType ( 
    ItemTypeID INTEGER        PRIMARY KEY AUTOINCREMENT,
    Description VARCHAR( 60 ) 
);


-- Table: Item
CREATE TABLE Item ( 
    ElementID INTEGER PRIMARY KEY
                               REFERENCES Element ( ElementID ),
    NumberOfCharges      INTEGER,
    ItemTypeID               INTEGER REFERENCES ItemType ( ItemTypeID ),
	Rechargeable			BOOLEAN
							NOT NULL
							DEFAULT 0
);


-- Table: CharacterInfection
CREATE TABLE CharacterInfection ( 
    CharacterID  INTEGER REFERENCES HeroCharacter ( CharacterID ),
    InfectionID INTEGER REFERENCES Infection ( InfectionID ),
    PRIMARY KEY ( CharacterID ASC, InfectionID ASC ) 
);


-- Table: CharacterSkill
CREATE TABLE CharacterSkill ( 
    CharacterID       INTEGER        NOT NULL
                                  DEFAULT '0',
    SkillID      INTEGER        NOT NULL
                                  DEFAULT '0'
                                  REFERENCES Skill ( SkillID ),
    AcquiredManyTimes INTEGER,
    TalentID   VARCHAR( 10 )  NOT NULL
                                  REFERENCES Talent ( TalentID ),
    AdditionalDetails     TEXT,
    PRIMARY KEY ( CharacterID, SkillID ),
    CONSTRAINT 'FK_CharacterSkill_CharacterID_HeroCharacter_CharacterID' FOREIGN KEY ( CharacterID ) REFERENCES HeroCharacter ( CharacterID ) 
);


-- Table: EventElement
CREATE TABLE EventElement ( 
    EventID            INTEGER NOT NULL
                                REFERENCES Event ( EventID ),
    ElementID INTEGER NOT NULL
                                REFERENCES Element ( ElementID ),
    NumberOfCopies         INTEGER DEFAULT ( 1 ),
    PRIMARY KEY ( EventID, ElementID ) 
);


-- Table: EventDatapad
CREATE TABLE EventDatapad ( 
    EventID         INTEGER NOT NULL
                             REFERENCES Event ( EventID ),
    DatapadID INTEGER NOT NULL
                             REFERENCES Datapad ( DatapadID ),
    NumberOfCopies      INTEGER DEFAULT ( 1 ),
    PRIMARY KEY ( EventID, DatapadID ) 
);


-- Table: CharacterUnlockedSkill
CREATE TABLE CharacterUnlockedSkill ( 
    CharacterID     INTEGER         NOT NULL
                                 REFERENCES HeroCharacter ( CharacterID ),
    SkillID    INTEGER         NOT NULL
                                 REFERENCES Skill ( SkillID ),
    TalentID VARCHAR( 10 )   REFERENCES Talent ( TalentID ),
    Notes         TEXT,
    PRIMARY KEY ( CharacterID, SkillID ) 
);


-- Table: SkillEffect
CREATE TABLE SkillEffect ( 
    SkillID INTEGER        REFERENCES Skill ( SkillID ),
    EffectID VARCHAR( 12 )  NOT NULL,
    Value    VARCHAR( 6 )   NOT NULL,
    PRIMARY KEY ( SkillID, EffectID ) 
);


-- Table: MessageHacking
CREATE TABLE MessageHacking ( 
    MessageID  INTEGER  NOT NULL
                             REFERENCES Message ( MessageID ),
    CharacterID        INTEGER  NOT NULL
                             REFERENCES HeroCharacter ( CharacterID ),
    AttemptNumber INTEGER  NOT NULL
                             DEFAULT ( 0 ),
    Success        BOOLEAN  NOT NULL
                             DEFAULT '0',
    DateOfAttempt   DATETIME NOT NULL
                             DEFAULT 'CURRENT_DATE',
    PRIMARY KEY ( MessageID, CharacterID, AttemptNumber ) 
);


-- Table: NewsStory
CREATE TABLE NewsStory ( 
    NewsID  INTEGER        PRIMARY KEY
                                  NOT NULL
                                  DEFAULT '0',
    DateOfCreation  DATETIME       NOT NULL
                                  DEFAULT 'CURRENT_DATE',
    ExpirationDate       DATETIME       NOT NULL
                                  DEFAULT 'CURRENT_DATE',
    Subject         VARCHAR( 60 ),
    Message          TEXT           NOT NULL,
    NetworkID           INTEGER        NOT NULL
                                  DEFAULT '0'
                                  REFERENCES Network ( NetworkID ),
    AuthorCharacterID         INTEGER        REFERENCES HeroCharacter ( CharacterID ),
    HackingLevel INTEGER        NOT NULL
                                  DEFAULT '0' 
);


-- Table: DeviceNetwork
CREATE TABLE DeviceNetwork ( 
    IPv4Address VARCHAR( 20 )  PRIMARY KEY,
    NetworkID        INTEGER        NOT NULL
                               REFERENCES Network ( NetworkID ) 
);


-- Table: NewsHacking
CREATE TABLE NewsHacking ( 
    NewsID   INTEGER  NOT NULL
                             REFERENCES NewsStory ( NewsID ),
    CharacterID        INTEGER  NOT NULL
                             REFERENCES HeroCharacter ( CharacterID ),
    AttemptNumber INTEGER  NOT NULL
                             DEFAULT ( 0 ),
    Success        BOOLEAN  NOT NULL
                             DEFAULT '0',
    DateOfAttempt   DATETIME NOT NULL
                             DEFAULT 'CURRENT_DATE',
    PRIMARY KEY ( NewsID, CharacterID, AttemptNumber ) 
);


-- Table: SkillPrerequisite
CREATE TABLE SkillPrerequisite ( 
    SkillID   INTEGER NOT NULL
                        REFERENCES Skill ( SkillID ),
    RequiredSkillID INTEGER NOT NULL
                        REFERENCES Skill ( SkillID ),
    PRIMARY KEY ( SkillID, RequiredSkillID ) 
);


-- Table: Parameter
CREATE TABLE Parameter ( 
    NetworkID      INTEGER        REFERENCES Network ( NetworkID ),
    Parameter VARCHAR( 30 ),
    Value    TEXT,
    PRIMARY KEY ( NetworkID, Parameter ) 
);


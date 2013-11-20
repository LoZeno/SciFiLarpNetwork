
-- Table: Rete
CREATE TABLE Rete ( 
    NumeroRete  INTEGER        PRIMARY KEY
                               NOT NULL
                               DEFAULT 0,
    NomeRete    VARCHAR( 60 )  NOT NULL,
    Descrizione TEXT,
    Fazione     VARCHAR( 30 ) 
);


-- Table: Specie
CREATE TABLE Specie ( 
    Specie      INTEGER        PRIMARY KEY AUTOINCREMENT
                               NOT NULL,
    Nome        VARCHAR( 50 ),
    Descrizione TEXT 
);


-- Table: Giocatore
CREATE TABLE Giocatore ( 
    NumeroSW       INTEGER        PRIMARY KEY AUTOINCREMENT
                                  NOT NULL
                                  DEFAULT '0',
    Cognome        VARCHAR( 30 )  NOT NULL,
    Nome           VARCHAR( 20 )  NOT NULL,
    DataNascita    DATETIME       NOT NULL
                                  DEFAULT 'CURRENT_DATE',
    Indirizzo      VARCHAR( 50 ),
    Citta          VARCHAR( 20 ),
    Provincia      VARCHAR( 2 ),
    CAP            VARCHAR( 6 ),
    Telefono       VARCHAR( 15 ),
    DataIscrizione DATETIME       NOT NULL
                                  DEFAULT 'CURRENT_DATE',
    Sesso          CHAR           NOT NULL,
    TipoGiocatore  VARCHAR( 6 )   NOT NULL
                                  DEFAULT 'PG',
    Email          VARCHAR( 50 ),
    Password       VARCHAR( 50 ) 
);


-- Table: PostaInUscita
CREATE TABLE PostaInUscita ( 
    NumeroMissione    INTEGER  NOT NULL
                               DEFAULT '0',
    NumeroPG          INTEGER  NOT NULL
                               DEFAULT '0',
    Cancellata        BOOLEAN  DEFAULT '0',
    Letta             BOOLEAN  DEFAULT '0',
    DataCancellazione DATETIME,
    CONSTRAINT [PK_PostaInUscita] PRIMARY KEY ( NumeroMissione, NumeroPG ),
    CONSTRAINT [FK_PostaInUscita_0] FOREIGN KEY ( NumeroPG ) REFERENCES Personaggio ( NumeroPG ),
    CONSTRAINT [FK_PostaInUscita_1] FOREIGN KEY ( NumeroMissione ) REFERENCES Missione ( NumeroMissione ) 
);


-- Table: PostaInArrivo
CREATE TABLE PostaInArrivo ( 
    NumeroMissione    INTEGER  NOT NULL
                               DEFAULT '0',
    NumeroPG          INTEGER  NOT NULL
                               DEFAULT '0',
    Cancellata        BOOLEAN  DEFAULT '0',
    Letta             BOOLEAN  DEFAULT '0',
    DataCancellazione DATETIME,
    CONSTRAINT [PK_PostaInArrivo] PRIMARY KEY ( NumeroMissione, NumeroPG ),
    CONSTRAINT [FK_PostaInArrivo_0] FOREIGN KEY ( NumeroPG ) REFERENCES Personaggio ( NumeroPG ),
    CONSTRAINT [FK_PostaInArrivo_1] FOREIGN KEY ( NumeroMissione ) REFERENCES Missione ( NumeroMissione ) 
);


-- Table: AccountHacking
CREATE TABLE AccountHacking ( 
    NumeroPGAccount INTEGER  REFERENCES Personaggio ( NumeroPG ),
    NumeroPGHacker  INTEGER  REFERENCES Personaggio ( NumeroPG ),
    NumeroTentativo INTEGER,
    Riuscito        INTEGER  DEFAULT '0',
    DataTentativo   DATETIME DEFAULT 'CURRENT_TIME',
    PRIMARY KEY ( NumeroPGAccount, NumeroPGHacker, NumeroTentativo ) 
);


-- Table: Rubrica
CREATE TABLE Rubrica ( 
    NumeroPG         INTEGER NOT NULL
                             DEFAULT '0',
    NumeroSalvato    INTEGER NOT NULL,
    NomeVisualizzato TEXT,
    CONSTRAINT [PK_Rubrica] PRIMARY KEY ( NumeroPG, NumeroSalvato ),
    CONSTRAINT [FK_Rubrica_NumeroPG_Personaggio_NumeroPG] FOREIGN KEY ( NumeroPG ) REFERENCES Personaggio ( NumeroPG ) 
);


-- Table: Personaggio
CREATE TABLE Personaggio ( 
    NumeroPG           INTEGER        PRIMARY KEY AUTOINCREMENT
                                      NOT NULL
                                      DEFAULT '0',
    NumeroSW           INTEGER        NOT NULL
                                      DEFAULT '0',
    Nome               VARCHAR( 50 )  NOT NULL,
    Specie             INTEGER        NOT NULL,
    Tipo               INTEGER        NOT NULL,
    Punti              INTEGER        NOT NULL
                                      DEFAULT '30',
    Sesso              CHAR           NOT NULL,
    LatoOscuro         INTEGER        NOT NULL
                                      DEFAULT '5',
    DataCreazione      DATETIME       NOT NULL
                                      DEFAULT 'CURRENT_DATE',
    Titolo             VARCHAR( 50 ),
    Vivo               INTEGER        NOT NULL
                                      DEFAULT '1',
    DataMorte          DATETIME,
    PasswordHolonet    VARCHAR( 50 ),
    Hacker             INTEGER        DEFAULT '0',
    Enzima             VARCHAR( 20 )  DEFAULT '0',
    Fazione            INTEGER        DEFAULT '0'
                                      REFERENCES Rete ( NumeroRete ),
    UltimaCrittazione  DATETIME,
    LivelloCrittazione INTEGER        NOT NULL
                                      DEFAULT '0',
    CodicePg           GUID,
    CONSTRAINT 'FK_Personaggio_0' FOREIGN KEY ( NumeroPG ) REFERENCES Rubrica ( NumeroPG ),
    CONSTRAINT 'FK_Personaggio_1' FOREIGN KEY ( NumeroPG ) REFERENCES AccountHacking ( NumeroPGAccount ),
    CONSTRAINT 'FK_Personaggio_2' FOREIGN KEY ( NumeroPG ) REFERENCES PostaInArrivo ( NumeroPG ),
    CONSTRAINT 'FK_Personaggio_3' FOREIGN KEY ( NumeroPG ) REFERENCES PostaInUscita ( NumeroPG ),
    CONSTRAINT 'FK_Personaggio_4' FOREIGN KEY ( NumeroSW ) REFERENCES Giocatore ( NumeroSW ),
    CONSTRAINT 'FK_Personaggio_5' FOREIGN KEY ( Specie ) REFERENCES Specie ( Specie ) 
);


-- Table: Missione
CREATE TABLE Missione ( 
    NumeroMissione INTEGER        PRIMARY KEY AUTOINCREMENT
                                  NOT NULL
                                  DEFAULT 0,
    Mandante       INTEGER        NOT NULL
                                  DEFAULT 0,
    Titolo         VARCHAR( 60 ),
    Testo          TEXT           NOT NULL,
    LivelloHacking INTEGER        NOT NULL
                                  DEFAULT 0,
    Attiva         INTEGER        NOT NULL
                                  DEFAULT 0,
    DataCreazione  DATETIME       NOT NULL
                                  DEFAULT CURRENT_TIMESTAMP,
    CONSTRAINT [FK_Missione_0] FOREIGN KEY ( NumeroMissione ) REFERENCES PostaInArrivo ( NumeroMissione ),
    CONSTRAINT [FK_Missione_1] FOREIGN KEY ( Mandante ) REFERENCES Personaggio ( NumeroPG ) 
);


-- Table: NewElementiDisponibilita
CREATE TABLE NewElementiDisponibilita ( 
    Progressivo INTEGER PRIMARY KEY AUTOINCREMENT,
    Descrizione TEXT 
);


-- Table: NewElementi
CREATE TABLE NewElementi ( 
    Progressivo   INTEGER        PRIMARY KEY AUTOINCREMENT
                                 NOT NULL,
    Nome          VARCHAR( 30 ),
    Descrizione   TEXT,
    Effetto       TEXT,
    Costo         REAL           NOT NULL
                                 DEFAULT 0,
    Immagine      TEXT,
    Disponibilita INTEGER        NOT NULL,
    DataScadenza  DATE,
    Discriminator INTEGER        NOT NULL
                                 DEFAULT 0,
    CONSTRAINT [FK_NewElementi_0] FOREIGN KEY ( Disponibilita ) REFERENCES NewElementiDisponibilita ( Progressivo ) 
);


-- Table: Attitudine
CREATE TABLE Attitudine ( 
    CdAttitudine VARCHAR( 10 )  PRIMARY KEY
                                NOT NULL,
    Nome         VARCHAR( 50 )  NOT NULL,
    Descrizione  TEXT,
    Tipo         INTEGER        NOT NULL
                                DEFAULT 0 
);


-- Table: Abilita
CREATE TABLE Abilita ( 
    CdAbilita     INTEGER        PRIMARY KEY AUTOINCREMENT
                                 NOT NULL,
    Nome          VARCHAR( 50 )  NOT NULL,
    Descrizione   TEXT,
    Multiacquisto INTEGER        NOT NULL
                                 DEFAULT 0,
    Costo         INTEGER        NOT NULL
                                 DEFAULT 0,
    BaseAvanzato  INTEGER        NOT NULL
                                 DEFAULT 0 
);


-- Table: Evento
CREATE TABLE Evento ( 
    CdEvento       INTEGER        PRIMARY KEY AUTOINCREMENT
                                  NOT NULL,
    TitoloEvento   VARCHAR( 50 )  NOT NULL,
    DataEvento     DATETIME       NOT NULL,
    PuntiAssegnati INTEGER        NOT NULL
                                  DEFAULT 1,
    Descrizione    TEXT,
    Costo          FLOAT          NOT NULL
                                  DEFAULT '10,00',
    Incasso        FLOAT,
    Chiuso         BOOLEAN        NOT NULL
                                  DEFAULT 0 
);


-- Table: HoloDisk
CREATE TABLE HoloDisk ( 
    Progressivo INTEGER PRIMARY KEY AUTOINCREMENT
                        NOT NULL,
    Codice      TEXT    NOT NULL
                        UNIQUE,
    Contenuto   TEXT,
    Hacking     INTEGER NOT NULL
                        DEFAULT 0,
    CodiceQr    GUID 
);


-- Table: Infezione
CREATE TABLE Infezione ( 
    Progressivo INTEGER        PRIMARY KEY AUTOINCREMENT
                               NOT NULL
                               DEFAULT 0,
    Nome        VARCHAR( 20 )  NOT NULL,
    Descrizione TEXT 
);


-- Table: Banca
CREATE TABLE Banca ( 
    NumeroBanca  INTEGER        PRIMARY KEY
                                NOT NULL
                                DEFAULT 0,
    NomeBanca    VARCHAR( 60 )  NOT NULL,
    Tasso        INTEGER,
    PeriodoTasso INTEGER,
    Costo        INTEGER,
    PeriodoCosto INTEGER 
);


-- Table: CodiciHacking
CREATE TABLE CodiciHacking ( 
    Progressivo INTEGER PRIMARY KEY
                        NOT NULL,
    Codice      TEXT    NOT NULL,
    Livello     INTEGER NOT NULL
                        DEFAULT 1 
);


-- Table: Porta
CREATE TABLE Porta ( 
    Progressivo    INTEGER        PRIMARY KEY
                                  NOT NULL,
    CodicePorta    VARCHAR( 20 )  NOT NULL,
    LivelloHacking INTEGER        NOT NULL
                                  DEFAULT 1 
);


-- Table: ContoBancario
CREATE TABLE ContoBancario ( 
    NumeroConto        INTEGER  PRIMARY KEY
                                NOT NULL
                                DEFAULT 0,
    NumeroBanca        INTEGER  NOT NULL,
    NumeroPG           INTEGER  NOT NULL,
    PeriodoTasso       INTEGER,
    Costo              INTEGER,
    PeriodoCosto       INTEGER,
    Tasso              INTEGER,
    DataSottoscrizione DATETIME,
    Totale             DOUBLE   NOT NULL
                                DEFAULT 0,
    CONSTRAINT [FK_ContoBancario_0] FOREIGN KEY ( NumeroPG ) REFERENCES Personaggio ( NumeroPG ),
    CONSTRAINT [FK_ContoBancario_NumeroBanca_Banca_NumeroBanca] FOREIGN KEY ( NumeroBanca ) REFERENCES Banca ( NumeroBanca ) 
);


-- Table: EnzimaInfezione
CREATE TABLE EnzimaInfezione ( 
    Progressivo INTEGER        PRIMARY KEY
                               NOT NULL
                               DEFAULT 0,
    Enzima      VARCHAR( 20 )  NOT NULL,
    Infezione   INTEGER        NOT NULL
                               DEFAULT 0,
    CONSTRAINT [FK_EnzimaInfezione_Infezione_Infezione_Progressivo] FOREIGN KEY ( Infezione ) REFERENCES Infezione ( Progressivo ) 
);


-- Table: MessaggioSalvato
CREATE TABLE MessaggioSalvato ( 
    Progressivo INTEGER        PRIMARY KEY AUTOINCREMENT
                               NOT NULL,
    NumeroPG    INTEGER        NOT NULL
                               DEFAULT 0,
    Titolo      TEXT           NOT NULL,
    Provenienza VARCHAR( 50 ),
    Contenuto   TEXT,
    Hacking     INTEGER        NOT NULL
                               DEFAULT 4,
    CONSTRAINT [FK_MessaggioSalvato_NumeroPG_Personaggio_NumeroPG] FOREIGN KEY ( NumeroPG ) REFERENCES Personaggio ( NumeroPG ) 
);


-- Table: HoloDiskFiles
CREATE TABLE HoloDiskFiles ( 
    Progressivo  INTEGER        NOT NULL,
    NumeroFile   INTEGER        NOT NULL,
    NomeFile     VARCHAR( 30 ),
    Contenuto    TEXT,
    LivelloCrypt INTEGER        DEFAULT '0',
    CONSTRAINT [PK_HoloDiskFiles] PRIMARY KEY ( Progressivo, NumeroFile ),
    CONSTRAINT [FK_HoloDiskFiles_Progressivo_HoloDisk_Progressivo] FOREIGN KEY ( Progressivo ) REFERENCES HoloDisk ( Progressivo ) 
);


-- Table: HoloDiskHacking
CREATE TABLE HoloDiskHacking ( 
    ProgressivoDisco INTEGER  NOT NULL,
    NumeroPGHacker   INTEGER  NOT NULL,
    NumeroTentativo  INTEGER  NOT NULL,
    Riuscito         INTEGER  DEFAULT '0',
    DataTentativo    DATETIME DEFAULT 'CURRENT_TIME',
    CONSTRAINT [PK_HoloDiskHacking] PRIMARY KEY ( ProgressivoDisco, NumeroPGHacker, NumeroTentativo ),
    CONSTRAINT [FK_HoloDiskHacking_0] FOREIGN KEY ( NumeroPGHacker ) REFERENCES Personaggio ( NumeroPG ),
    CONSTRAINT [FK_HoloDiskHacking_ProgressivoDisco_HoloDisk_Progressivo] FOREIGN KEY ( ProgressivoDisco ) REFERENCES HoloDisk ( Progressivo ) 
);


-- Table: EventoGiorni
CREATE TABLE EventoGiorni ( 
    CdEvento       INTEGER  REFERENCES Evento ( CdEvento ),
    DataGiorno     DATE,
    PuntiAssegnati INTEGER,
    OraInGioco     DATETIME NOT NULL,
    OraFuoriGioco  DATETIME NOT NULL,
    CostoGiorno    FLOAT    NULL,
    PRIMARY KEY ( CdEvento, DataGiorno ) 
);


-- Table: EventoGiorniPersonaggio
CREATE TABLE EventoGiorniPersonaggio ( 
    CdEvento      INTEGER  NOT NULL,
    DataGiorno    DATE     NOT NULL,
    NumeroPg      INTEGER  NOT NULL
                           REFERENCES Personaggio ( NumeroPG ),
    Pagato        BOOLEAN  NOT NULL
                           DEFAULT ( 0 ),
    DataPagamento DATETIME,
    Partecipato   BOOLEAN  NOT NULL
                           DEFAULT ( 0 ),
    PRIMARY KEY ( CdEvento, DataGiorno, NumeroPg ),
    FOREIGN KEY ( CdEvento, DataGiorno ) REFERENCES EventoGiorni ( CdEvento, DataGiorno ) ON DELETE CASCADE 
);


-- Table: AbilitaAttitudine
CREATE TABLE AbilitaAttitudine ( 
    CdAbilita    INTEGER        NOT NULL
                                REFERENCES Abilita ( CdAbilita ),
    CdAttitudine VARCHAR( 10 )  NOT NULL
                                REFERENCES Attitudine ( CdAttitudine ),
    PRIMARY KEY ( CdAbilita, CdAttitudine ) 
);


-- Table: ListePersonaggio
CREATE TABLE ListePersonaggio ( 
    NumeroPG     INTEGER        REFERENCES Personaggio ( NumeroPG ),
    CdAttitudine VARCHAR( 20 )  REFERENCES Attitudine ( CdAttitudine ),
    PRIMARY KEY ( NumeroPG, CdAttitudine ) 
);


-- Table: CodiciQr
CREATE TABLE CodiciQr ( 
    Progressivo INTEGER REFERENCES NewElementi ( Progressivo ),
    Codice      GUID    UNIQUE ON CONFLICT ROLLBACK,
    PRIMARY KEY ( Progressivo, Codice ) 
);


-- Table: NewFormule
CREATE TABLE NewFormule ( 
    Risultato         INTEGER REFERENCES NewElementi ( Progressivo ),
    NumeroIngrediente INTEGER DEFAULT ( 0 ),
    Ingrediente       INTEGER REFERENCES NewElementi ( Progressivo ),
    PRIMARY KEY ( Risultato ASC, NumeroIngrediente ASC ) 
);


-- Table: TipoSostanze
CREATE TABLE TipoSostanze ( 
    Progressivo INTEGER        PRIMARY KEY AUTOINCREMENT,
    Descrizione VARCHAR( 30 )  NOT NULL 
);


-- Table: NewSostanze
CREATE TABLE NewSostanze ( 
    ProgressivoSostanza INTEGER        PRIMARY KEY
                                       REFERENCES NewElementi ( Progressivo ),
    ModoUso             VARCHAR( 10 )  NOT NULL,
    ValoreEfficacia     INTEGER        NOT NULL
                                       DEFAULT ( 1 ),
    Tipo                INTEGER        NOT NULL
                                       DEFAULT ( 0 ) 
                                       REFERENCES TipoSostanze ( Progressivo ) 
);


-- Table: TipoOggetti
CREATE TABLE TipoOggetti ( 
    Progressivo INTEGER        PRIMARY KEY AUTOINCREMENT,
    Descrizione VARCHAR( 60 ) 
);


-- Table: NewOggetti
CREATE TABLE NewOggetti ( 
    ProgressivoOggetto INTEGER PRIMARY KEY
                               REFERENCES NewElementi ( Progressivo ),
    NumeroCariche      INTEGER,
    Tipo               INTEGER REFERENCES TipoOggetti ( Progressivo ) 
);


-- Table: InfezionePersonaggio
CREATE TABLE InfezionePersonaggio ( 
    NumeroPG  INTEGER REFERENCES Personaggio ( NumeroPG ),
    Infezione INTEGER REFERENCES Infezione ( Progressivo ),
    PRIMARY KEY ( NumeroPG ASC, Infezione ASC ) 
);


-- Table: AbilitaPersonaggio
CREATE TABLE AbilitaPersonaggio ( 
    NumeroPG       INTEGER        NOT NULL
                                  DEFAULT '0',
    CdAbilita      INTEGER        NOT NULL
                                  DEFAULT '0'
                                  REFERENCES Abilita ( CdAbilita ),
    NumeroAcquisti INTEGER,
    CdAttitudine   VARCHAR( 10 )  NOT NULL
                                  REFERENCES Attitudine ( CdAttitudine ),
    Specifiche     VARCHAR( 50 ),
    PRIMARY KEY ( NumeroPG, CdAbilita ),
    CONSTRAINT 'FK_AbilitaPersonaggio_NumeroPG_Personaggio_NumeroPG' FOREIGN KEY ( NumeroPG ) REFERENCES Personaggio ( NumeroPG ) 
);


-- Table: EventoElementi
CREATE TABLE EventoElementi ( 
    CdEvento            INTEGER NOT NULL
                                REFERENCES Evento ( CdEvento ),
    ProgressivoElemento INTEGER NOT NULL
                                REFERENCES NewElementi ( Progressivo ),
    NumeroCopie         INTEGER,
    PRIMARY KEY ( CdEvento, ProgressivoElemento ) 
);


-- Table: EventoHolodischi
CREATE TABLE EventoHolodischi ( 
    CdEvento         INTEGER NOT NULL
                             REFERENCES Evento ( CdEvento ),
    ProgressivoDisco INTEGER NOT NULL
                             REFERENCES HoloDisk ( Progressivo ),
    NumeroCopie      INTEGER DEFAULT ( 1 ),
    PRIMARY KEY ( CdEvento, ProgressivoDisco ) 
);


-- Table: AbilitaSbloccate
CREATE TABLE AbilitaSbloccate ( 
    NumeroPG     INTEGER         NOT NULL
                                 REFERENCES Personaggio ( NumeroPG ),
    CdAbilita    INTEGER         NOT NULL
                                 REFERENCES Abilita ( CdAbilita ),
    CdAttitudine VARCHAR( 10 )   REFERENCES Attitudine ( CdAttitudine ),
    Note         VARCHAR( 100 ),
    PRIMARY KEY ( NumeroPG, CdAbilita ) 
);


-- Table: EffettiAbilita
CREATE TABLE EffettiAbilita ( 
    CdAbilita INTEGER        REFERENCES Abilita ( CdAbilita ),
    CdEffetto VARCHAR( 12 )  NOT NULL,
    Valore    VARCHAR( 6 )   NOT NULL,
    PRIMARY KEY ( CdAbilita, CdEffetto ) 
);


-- Table: MissioneHacking
CREATE TABLE MissioneHacking ( 
    NumeroMissione  INTEGER  NOT NULL
                             REFERENCES Missione ( NumeroMissione ),
    NumeroPG        INTEGER  NOT NULL
                             REFERENCES Personaggio ( NumeroPG ),
    NumeroTentativo INTEGER  NOT NULL
                             DEFAULT ( 0 ),
    Riuscito        INTEGER  NOT NULL
                             DEFAULT '0',
    DataTentativo   DATETIME NOT NULL
                             DEFAULT 'CURRENT_DATE',
    PRIMARY KEY ( NumeroMissione, NumeroPG, NumeroTentativo ) 
);


-- Table: Notizia
CREATE TABLE Notizia ( 
    NumeroNotizia  INTEGER        PRIMARY KEY
                                  NOT NULL
                                  DEFAULT '0',
    DataCreazione  DATETIME       NOT NULL
                                  DEFAULT 'CURRENT_DATE',
    DataFine       DATETIME       NOT NULL
                                  DEFAULT 'CURRENT_DATE',
    Titolo         VARCHAR( 60 ),
    Testo          TEXT           NOT NULL,
    Rete           INTEGER        NOT NULL
                                  DEFAULT '0'
                                  REFERENCES Rete ( NumeroRete ),
    Autore         INTEGER        REFERENCES Personaggio ( NumeroPG ),
    LivelloHacking INTEGER        NOT NULL
                                  DEFAULT '0' 
);


-- Table: RegistroIP
CREATE TABLE RegistroIP ( 
    IndirizzoIP VARCHAR( 20 )  PRIMARY KEY,
    Rete        INTEGER        NOT NULL
                               REFERENCES Rete ( NumeroRete ) 
);


-- Table: NotiziaHacking
CREATE TABLE NotiziaHacking ( 
    NumeroNotizia   INTEGER  NOT NULL
                             REFERENCES Notizia ( NumeroNotizia ),
    NumeroPG        INTEGER  NOT NULL
                             REFERENCES Personaggio ( NumeroPG ),
    NumeroTentativo INTEGER  NOT NULL
                             DEFAULT ( 0 ),
    Riuscito        INTEGER  NOT NULL
                             DEFAULT '0',
    DataTentativo   DATETIME NOT NULL
                             DEFAULT 'CURRENT_DATE',
    PRIMARY KEY ( NumeroNotizia, NumeroPG, NumeroTentativo ) 
);


-- Table: RequisitiAbilita
CREATE TABLE RequisitiAbilita ( 
    CdAbilita   INTEGER NOT NULL
                        REFERENCES Abilita ( CdAbilita ),
    CdRequisito INTEGER NOT NULL
                        REFERENCES Abilita ( CdAbilita ),
    PRIMARY KEY ( CdAbilita, CdRequisito ) 
);


-- Table: Parametri
CREATE TABLE Parametri ( 
    Rete      INTEGER        REFERENCES Rete ( NumeroRete ),
    Parametro VARCHAR( 30 ),
    Valore    TEXT,
    PRIMARY KEY ( Rete, Parametro ) 
);


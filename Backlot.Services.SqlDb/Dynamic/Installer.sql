-- *-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
-- STEP 1: Create a Big dynamic Role Store Table which includes
-- the metadata.
-- *-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
CREATE TABLE dbo.DynamicRoleStore (
    Uid NVARCHAR(256) COLLATE SQL_Latin1_General_CP1_CI_AS PRIMARY KEY,
    Name NVARCHAR(512) COLLATE SQL_Latin1_General_CP1_CI_AS,
    Checksum NVARCHAR(128) COLLATE SQL_Latin1_General_CP1_CI_AS,
    
    Permission NVARCHAR(1024) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    Skills NVARCHAR(1024) COLLATE SQL_Latin1_General_CP1_CI_AS,

    UsersCanRead NVARCHAR(1024) COLLATE SQL_Latin1_General_CP1_CI_AS,
    GroupsCanRead NVARCHAR(1024) COLLATE SQL_Latin1_General_CP1_CI_AS,
    
    CanRead BIT NOT NULL DEFAULT 0,

    LastModified DATETIME NOT NULL DEFAULT GETDATE(),

    JsonData NVARCHAR(MAX) NOT NULL
);


GO

-- *-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
-- STEP 2: Create a Relation Table for Uid Roles
-- AWARE: you can related to none persisted roles as well. Therefor
-- a relation is not managed via a normal foreign key.
-- *-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*

CREATE TABLE dbo.DynamicRelationStore (
    Role1_Uid NVARCHAR(256) COLLATE SQL_Latin1_General_CP1_CI_AS,
    Role2_Uid NVARCHAR(256) COLLATE SQL_Latin1_General_CP1_CI_AS,
    Serialized NVARCHAR(MAX) NOT NULL,
    PRIMARY KEY (Role1_Uid, Role2_Uid)
);


-- *-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
-- STEP 2: Create a BulkIdList TVP definition So SQL Server knows what shape you use for Bulk loads..
-- *-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*

CREATE TYPE dbo.BulkIdList AS TABLE (
    Uid NVARCHAR(256) NOT NULL PRIMARY KEY
);
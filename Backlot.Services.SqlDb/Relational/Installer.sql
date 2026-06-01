-- MSSQL Compatible scripts

-- *-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
-- STEP 1: Create the metadata view (ViewPrimaryKeyMetadata) with explicit collation
-- *-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*

CREATE OR ALTER VIEW dbo.ViewPrimaryKeyMetadata AS
SELECT
    TC.TABLE_NAME COLLATE SQL_Latin1_General_CP1_CI_AS AS TABLE_NAME,
    KU.COLUMN_NAME COLLATE SQL_Latin1_General_CP1_CI_AS AS COLUMN_NAME,
    KU.ORDINAL_POSITION,
    TC.CONSTRAINT_NAME COLLATE SQL_Latin1_General_CP1_CI_AS AS CONSTRAINT_NAME
FROM INFORMATION_SCHEMA.TABLE_CONSTRAINTS AS TC
         INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS KU
                    ON TC.CONSTRAINT_NAME COLLATE SQL_Latin1_General_CP1_CI_AS = KU.CONSTRAINT_NAME COLLATE SQL_Latin1_General_CP1_CI_AS
WHERE TC.CONSTRAINT_TYPE = 'PRIMARY KEY';

GO

-- *-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
-- STEP 2: Create the metadata view (ViewPrimaryKeyValue)
-- *-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*

DECLARE @sql NVARCHAR(MAX) = N'';

-- Build one SELECT for each table using the metadata view.
;WITH TableList AS (
    SELECT DISTINCT TABLE_NAME
    FROM dbo.ViewPrimaryKeyMetadata
)
 SELECT @sql = @sql +
               'SELECT ' +
               '''' + TABLE_NAME + ''' COLLATE SQL_Latin1_General_CP1_CI_AS AS TABLE_NAME, ' + CHAR(13) + CHAR(10) +
                 '       ' + pkExpr + ' COLLATE SQL_Latin1_General_CP1_CI_AS AS PK_VALUE' + CHAR(13) + CHAR(10) +
                 'FROM dbo.' + QUOTENAME(TABLE_NAME) + CHAR(13) + CHAR(10) +
                 'UNION ALL' + CHAR(13) + CHAR(10)
 FROM TableList
             CROSS APPLY (
             -- Build the concatenation expression for the PK columns of this table.
             SELECT STRING_AGG('CAST(' + QUOTENAME(COLUMN_NAME) + ' AS VARCHAR(MAX))', ' + ''~'' + ')
             WITHIN GROUP (ORDER BY ORDINAL_POSITION) AS pkExpr
             FROM dbo.ViewPrimaryKeyMetadata
             WHERE TABLE_NAME = TableList.TABLE_NAME
             ) AS X;

-- Remove the trailing "UNION ALL"
SET @sql = LEFT(@sql, LEN(@sql) - LEN('UNION ALL' + CHAR(13) + CHAR(10)));

-- Prepend the CREATE VIEW statement for the dynamic view.
SET @sql = 'CREATE VIEW dbo.ViewPrimaryKeyValue AS' + CHAR(13) + CHAR(10) + @sql;

EXEC sp_executesql @sql;

-- PRINT @sql;

GO

-- *-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
-- STEP 3: Create the metadata view (sp_GetForeignKeyReferences)
-- *-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*

CREATE OR ALTER  PROCEDURE [dbo].[sp_GetForeignKeyReferences]
    @TableName NVARCHAR(128)
AS
BEGIN
    SET NOCOUNT ON;

    -------------------------------
    -- 1. Inbound References:
    --    Fields in other tables that refer to the primary key of @TableName.
    -------------------------------
    SELECT
        'IN' AS DIRECTION,
        pk.TABLE_NAME AS PARENT_TABLE_NAME,
        pk.COLUMN_NAME AS PARENT_COLUMN_NAME,
        fk.TABLE_NAME AS CHILD_TABLE_NAME,
        fk.COLUMN_NAME AS CHILD_COLUMN_NAME
    FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS AS rc
             INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS fk
                        ON rc.CONSTRAINT_NAME = fk.CONSTRAINT_NAME
             INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS pk
                        ON rc.UNIQUE_CONSTRAINT_NAME = pk.CONSTRAINT_NAME
    WHERE pk.TABLE_NAME = @TableName


UNION ALL

    -------------------------------
    -- 2. Outbound References:
    --    Fields in @TableName that refer to another table's key.
    -------------------------------
    SELECT
        'OUT' AS DIRECTION,
        pk.TABLE_NAME AS PARENT_TABLE_NAME,
        pk.COLUMN_NAME AS PARENT_COLUMN_NAME,
        fk.TABLE_NAME AS CHILD_TABLE_NAME,
        fk.COLUMN_NAME AS CHILD_COLUMN_NAME
    FROM INFORMATION_SCHEMA.REFERENTIAL_CONSTRAINTS AS rc
             INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS fk
                        ON rc.CONSTRAINT_NAME = fk.CONSTRAINT_NAME
             INNER JOIN INFORMATION_SCHEMA.KEY_COLUMN_USAGE AS pk
                        ON rc.UNIQUE_CONSTRAINT_NAME = pk.CONSTRAINT_NAME
    WHERE fk.TABLE_NAME = @TableName;
END;

GO

-- *-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
-- STEP 4: Create Meta Persisted Role Table named MPRT
-- TODO: save underlying type in a separate column
-- *-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*-*
CREATE TABLE dbo.MetaPersistedRoleTable (
    Uid NVARCHAR(256) COLLATE SQL_Latin1_General_CP1_CI_AS PRIMARY KEY,
    Name NVARCHAR(512) COLLATE SQL_Latin1_General_CP1_CI_AS,
    Checksum NVARCHAR(128) COLLATE SQL_Latin1_General_CP1_CI_AS,
    
    Permission NVARCHAR(1024) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
    Skills NVARCHAR(1024) COLLATE SQL_Latin1_General_CP1_CI_AS,
    Construct NVARCHAR(256) COLLATE SQL_Latin1_General_CP1_CI_AS,
    
    LastModified DATETIME NOT NULL DEFAULT GETDATE(),
);

GO
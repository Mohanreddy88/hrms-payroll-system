CREATE TABLE AuditLogs (
    Id           INT IDENTITY(1,1) PRIMARY KEY,
    UserId       INT NULL,
    Username     NVARCHAR(100) NOT NULL,
    Action       NVARCHAR(100) NOT NULL,
    EntityType   NVARCHAR(100) NOT NULL,
    EntityId     NVARCHAR(50)  NULL,
    Description  NVARCHAR(1000) NOT NULL,
    OldValues    NVARCHAR(MAX) NULL,
    NewValues    NVARCHAR(MAX) NULL,
    IpAddress    NVARCHAR(50)  NULL,
    CreatedAt    DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);
CREATE INDEX IX_AuditLogs_EntityType_EntityId ON AuditLogs(EntityType, EntityId);
CREATE INDEX IX_AuditLogs_UserId              ON AuditLogs(UserId);
CREATE INDEX IX_AuditLogs_CreatedAt           ON AuditLogs(CreatedAt DESC);
PRINT 'AuditLogs table created';

-- Create Notifications table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Notifications]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Notifications] (
        [Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserId] INT NOT NULL,
        [Title] NVARCHAR(100) NOT NULL,
        [Message] NVARCHAR(500) NOT NULL,
        [Type] NVARCHAR(50) NOT NULL,
        [Link] NVARCHAR(200) NULL,
        [IsRead] BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
        [ReadAt] DATETIME2 NULL,
        CONSTRAINT [FK_Notifications_Users_UserId] FOREIGN KEY ([UserId]) REFERENCES [Users]([Id]) ON DELETE CASCADE
    );

    CREATE INDEX [IX_Notifications_UserId] ON [Notifications]([UserId]);
    CREATE INDEX [IX_Notifications_CreatedAt] ON [Notifications]([CreatedAt] DESC);
    CREATE INDEX [IX_Notifications_IsRead] ON [Notifications]([IsRead]);

    PRINT 'Notifications table created successfully';
END
ELSE
BEGIN
    PRINT 'Notifications table already exists';
END
GO

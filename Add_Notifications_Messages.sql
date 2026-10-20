/*
  Run this if you already have LostAndFoundDB and do NOT want to drop it.
  Adds Notifications, Conversations, and ChatMessages tables.
*/
USE LostAndFoundDB;
GO

IF OBJECT_ID(N'dbo.Notifications', N'U') IS NULL
BEGIN
    CREATE TABLE [Notifications] (
        [NotificationID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [UserID] INT NOT NULL,
        [Title] NVARCHAR(150) NOT NULL,
        [Message] NVARCHAR(MAX) NOT NULL,
        [Type] NVARCHAR(50) NOT NULL DEFAULT N'System',
        [IsRead] BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [RelatedItemID] INT NULL,
        CONSTRAINT [FK_Notifications_Users] FOREIGN KEY ([UserID]) REFERENCES [Users]([UserID]) ON DELETE CASCADE
    );
    PRINT 'Notifications table created.';
END
ELSE
    PRINT 'Notifications table already exists.';
GO

IF OBJECT_ID(N'dbo.Conversations', N'U') IS NULL
BEGIN
    CREATE TABLE [Conversations] (
        [ConversationID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [StudentUserID] INT NOT NULL,
        [Subject] NVARCHAR(200) NOT NULL,
        [Status] NVARCHAR(20) NOT NULL DEFAULT N'Open',
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_Conversations_Users] FOREIGN KEY ([StudentUserID]) REFERENCES [Users]([UserID])
    );
    PRINT 'Conversations table created.';
END
ELSE
    PRINT 'Conversations table already exists.';
GO

IF OBJECT_ID(N'dbo.ChatMessages', N'U') IS NULL
BEGIN
    CREATE TABLE [ChatMessages] (
        [ChatMessageID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
        [ConversationID] INT NOT NULL,
        [SenderUserID] INT NOT NULL,
        [Body] NVARCHAR(MAX) NOT NULL,
        [IsRead] BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
        CONSTRAINT [FK_ChatMessages_Conversations] FOREIGN KEY ([ConversationID]) REFERENCES [Conversations]([ConversationID]) ON DELETE CASCADE,
        CONSTRAINT [FK_ChatMessages_Users] FOREIGN KEY ([SenderUserID]) REFERENCES [Users]([UserID])
    );
    PRINT 'ChatMessages table created.';
END
ELSE
    PRINT 'ChatMessages table already exists.';
GO

PRINT 'Done.';
GO

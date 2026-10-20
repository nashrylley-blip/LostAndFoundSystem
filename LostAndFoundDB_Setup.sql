/*
  LostAndFoundDB - Complete Setup Script for SSMS
  WARNING: This DROPS the existing LostAndFoundDB if it exists.
*/

USE master;
GO

IF EXISTS (SELECT name FROM sys.databases WHERE name = N'LostAndFoundDB')
BEGIN
    ALTER DATABASE LostAndFoundDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE LostAndFoundDB;
END
GO

CREATE DATABASE LostAndFoundDB;
GO

USE LostAndFoundDB;
GO

-- ===================== TABLES =====================

CREATE TABLE [Users] (
    [UserID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [FullName] NVARCHAR(100) NOT NULL,
    [Email] NVARCHAR(100) NOT NULL,
    [StudentID] NVARCHAR(20) NULL,
    [PasswordHash] NVARCHAR(255) NOT NULL,
    [Role] NVARCHAR(20) NOT NULL DEFAULT N'Student',
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETDATE()
);

CREATE UNIQUE INDEX [IX_Users_Email] ON [Users]([Email]);
CREATE UNIQUE INDEX [IX_Users_StudentID] ON [Users]([StudentID]) WHERE [StudentID] IS NOT NULL;

CREATE TABLE [Categories] (
    [CategoryID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [CategoryName] NVARCHAR(50) NOT NULL
);

CREATE TABLE [Items] (
    [ItemId] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [ItemName] NVARCHAR(100) NOT NULL,
    [Description] NVARCHAR(MAX) NOT NULL,
    [CategoryId] INT NOT NULL,
    [LocationFoundOrLost] NVARCHAR(100) NOT NULL,
    [DateReported] DATETIME2 NOT NULL DEFAULT GETDATE(),
    [Status] NVARCHAR(20) NOT NULL DEFAULT N'Lost',
    [ApprovalStatus] NVARCHAR(20) NOT NULL DEFAULT N'Pending',
    [ImageUrl] NVARCHAR(MAX) NULL,
    [UserId] INT NOT NULL,
    CONSTRAINT [FK_Items_Categories] FOREIGN KEY ([CategoryId]) REFERENCES [Categories]([CategoryID]),
    CONSTRAINT [FK_Items_Users] FOREIGN KEY ([UserId]) REFERENCES [Users]([UserID])
);

CREATE TABLE [Claims] (
    [ClaimID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [ItemID] INT NOT NULL,
    [ClaimedByUserID] INT NOT NULL,
    [ProofOfOwnership] NVARCHAR(MAX) NOT NULL,
    [Status] NVARCHAR(20) NOT NULL DEFAULT N'Pending',
    [DateClaimed] DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_Claims_Items] FOREIGN KEY ([ItemID]) REFERENCES [Items]([ItemId]) ON DELETE CASCADE,
    CONSTRAINT [FK_Claims_Users] FOREIGN KEY ([ClaimedByUserID]) REFERENCES [Users]([UserID])
);

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

CREATE TABLE [Conversations] (
    [ConversationID] INT IDENTITY(1,1) NOT NULL PRIMARY KEY,
    [StudentUserID] INT NOT NULL,
    [Subject] NVARCHAR(200) NOT NULL,
    [Status] NVARCHAR(20) NOT NULL DEFAULT N'Open',
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT [FK_Conversations_Users] FOREIGN KEY ([StudentUserID]) REFERENCES [Users]([UserID])
);

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

CREATE TABLE [__EFMigrationsHistory] (
    [MigrationId] NVARCHAR(150) NOT NULL PRIMARY KEY,
    [ProductVersion] NVARCHAR(32) NOT NULL
);

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion]) VALUES
(N'20261006125309_AddApprovalStatusToItems', N'10.0.12'),
(N'20261007090000_AddStudentIdToUsers', N'10.0.12'),
(N'20261008000000_AddNotificationsAndMessages', N'10.0.12');

-- ===================== SEED DATA =====================

INSERT INTO [Users] ([FullName], [Email], [StudentID], [PasswordHash], [Role], [CreatedAt]) VALUES
(N'Demo Student', N'student@student.edu', N'2026-00001', N'123456', N'Student', GETDATE()),
(N'System Administrator', N'admin@student.edu', NULL, N'123456', N'Admin', GETDATE());

INSERT INTO [Categories] ([CategoryName]) VALUES
(N'Electronics'),
(N'Clothing'),
(N'Documents / ID'),
(N'Bags / Backpacks'),
(N'Other');

PRINT '========================================';
PRINT 'LostAndFoundDB created successfully.';
PRINT 'Default logins (password: 123456):';
PRINT '  Student : Student ID 2026-00001';
PRINT '  Admin   : admin@student.edu';
PRINT 'Tables: Users, Categories, Items, Claims,';
PRINT '        Notifications, Conversations, ChatMessages';
PRINT '========================================';
GO

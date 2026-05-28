-- ============================================================================
-- PetShop existing-database schema.
-- Use this if you prefer to scaffold the model from an existing database:
--   1. Run this script against a SQL Server instance.
--   2. dotnet ef dbcontext scaffold "<connection>" Microsoft.EntityFrameworkCore.SqlServer
--          --context PetShopDbContext --output-dir Entities --force
-- Otherwise the EF migrations under src/PetShop.Data/Migrations create the same schema.
-- ============================================================================

IF OBJECT_ID('dbo.OrderItems', 'U') IS NOT NULL DROP TABLE dbo.OrderItems;
IF OBJECT_ID('dbo.Orders', 'U')     IS NOT NULL DROP TABLE dbo.Orders;
IF OBJECT_ID('dbo.Pets', 'U')       IS NOT NULL DROP TABLE dbo.Pets;
IF OBJECT_ID('dbo.Customers', 'U')  IS NOT NULL DROP TABLE dbo.Customers;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
IF OBJECT_ID('dbo.Users', 'U')      IS NOT NULL DROP TABLE dbo.Users;
GO

CREATE TABLE dbo.Categories
(
    Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Categories PRIMARY KEY,
    Name        NVARCHAR(100) NOT NULL,
    Description NVARCHAR(500) NULL,
    CreatedUtc  DATETIME2 NOT NULL CONSTRAINT DF_Categories_CreatedUtc DEFAULT SYSUTCDATETIME()
);
CREATE UNIQUE INDEX IX_Categories_Name ON dbo.Categories(Name);
GO

CREATE TABLE dbo.Customers
(
    Id         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Customers PRIMARY KEY,
    FullName   NVARCHAR(150) NOT NULL,
    Email      NVARCHAR(256) NOT NULL,
    Phone      NVARCHAR(30) NULL,
    CreatedUtc DATETIME2 NOT NULL CONSTRAINT DF_Customers_CreatedUtc DEFAULT SYSUTCDATETIME()
);
CREATE UNIQUE INDEX IX_Customers_Email ON dbo.Customers(Email);
GO

CREATE TABLE dbo.Users
(
    Id           INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Users PRIMARY KEY,
    Username     NVARCHAR(100) NOT NULL,
    Email        NVARCHAR(256) NOT NULL,
    PasswordHash NVARCHAR(512) NOT NULL,
    Roles        NVARCHAR(256) NOT NULL,
    IsActive     BIT NOT NULL CONSTRAINT DF_Users_IsActive DEFAULT (1),
    CreatedUtc   DATETIME2 NOT NULL CONSTRAINT DF_Users_CreatedUtc DEFAULT SYSUTCDATETIME()
);
CREATE UNIQUE INDEX IX_Users_Username ON dbo.Users(Username);
GO

CREATE TABLE dbo.Pets
(
    Id         INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Pets PRIMARY KEY,
    Name       NVARCHAR(100) NOT NULL,
    Breed      NVARCHAR(100) NULL,
    Price      DECIMAL(18,2) NOT NULL,
    AgeMonths  INT NULL,
    Status     INT NOT NULL,
    CategoryId INT NOT NULL,
    CreatedUtc DATETIME2 NOT NULL CONSTRAINT DF_Pets_CreatedUtc DEFAULT SYSUTCDATETIME(),
    UpdatedUtc DATETIME2 NULL,
    CONSTRAINT FK_Pets_Categories FOREIGN KEY (CategoryId) REFERENCES dbo.Categories(Id)
);
CREATE INDEX IX_Pets_Status ON dbo.Pets(Status);
CREATE INDEX IX_Pets_CategoryId ON dbo.Pets(CategoryId);
GO

CREATE TABLE dbo.Orders
(
    Id          INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_Orders PRIMARY KEY,
    CustomerId  INT NOT NULL,
    Status      INT NOT NULL,
    TotalAmount DECIMAL(18,2) NOT NULL,
    OrderedUtc  DATETIME2 NOT NULL CONSTRAINT DF_Orders_OrderedUtc DEFAULT SYSUTCDATETIME(),
    CONSTRAINT FK_Orders_Customers FOREIGN KEY (CustomerId) REFERENCES dbo.Customers(Id)
);
CREATE INDEX IX_Orders_CustomerId ON dbo.Orders(CustomerId);
GO

CREATE TABLE dbo.OrderItems
(
    Id        INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_OrderItems PRIMARY KEY,
    OrderId   INT NOT NULL,
    PetId     INT NOT NULL,
    Quantity  INT NOT NULL,
    UnitPrice DECIMAL(18,2) NOT NULL,
    CONSTRAINT FK_OrderItems_Orders FOREIGN KEY (OrderId) REFERENCES dbo.Orders(Id) ON DELETE CASCADE,
    CONSTRAINT FK_OrderItems_Pets   FOREIGN KEY (PetId)   REFERENCES dbo.Pets(Id)
);
CREATE INDEX IX_OrderItems_OrderId ON dbo.OrderItems(OrderId);
CREATE INDEX IX_OrderItems_PetId ON dbo.OrderItems(PetId);
GO

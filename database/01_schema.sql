-- ============================================================================
-- PetShop existing-database schema.
-- Use this if you prefer to scaffold the model from an existing database:
--   1. Run this script against a SQL Server instance.
--   2. dotnet ef dbcontext scaffold "<connection>" Microsoft.EntityFrameworkCore.SqlServer
--          --context PetShopDbContext --output-dir Entities --force
-- Otherwise the EF migrations under src/PetShop.Data/Migrations create the same schema.
-- ============================================================================

IF OBJECT_ID('dbo.Pets', 'U')       IS NOT NULL DROP TABLE dbo.Pets;
IF OBJECT_ID('dbo.Categories', 'U') IS NOT NULL DROP TABLE dbo.Categories;
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

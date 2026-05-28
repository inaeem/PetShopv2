-- Stored procedure invoked directly by the data layer (PetRepository.SearchAsync).
CREATE OR ALTER PROCEDURE dbo.usp_SearchPets
    @Term       NVARCHAR(100) = NULL,
    @CategoryId INT           = NULL
AS
BEGIN
    SET NOCOUNT ON;

    SELECT  p.Id,
            p.Name,
            p.Breed,
            p.Price,
            p.Status,
            c.Name AS CategoryName
    FROM    dbo.Pets AS p
    INNER JOIN dbo.Categories AS c ON c.Id = p.CategoryId
    WHERE  (@Term IS NULL OR p.Name LIKE '%' + @Term + '%' OR p.Breed LIKE '%' + @Term + '%')
      AND  (@CategoryId IS NULL OR p.CategoryId = @CategoryId)
    ORDER BY p.Name;
END;
GO

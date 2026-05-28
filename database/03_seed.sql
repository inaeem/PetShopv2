-- Optional reference data. Users are seeded by the API at startup (DbInitializer)
-- so their passwords are hashed with the same PBKDF2 routine the AuthService uses.

INSERT INTO dbo.Categories (Name, Description) VALUES
    ('Dogs',  'Loyal canine companions'),
    ('Cats',  'Independent feline friends'),
    ('Birds', 'Feathered companions'),
    ('Fish',  'Aquatic pets');
GO

INSERT INTO dbo.Pets (Name, Breed, Price, AgeMonths, Status, CategoryId) VALUES
    ('Rex',     'German Shepherd', 650.00, 8,  0, 1),
    ('Bella',   'Labrador',        500.00, 5,  0, 1),
    ('Whiskers','Siamese',         300.00, 12, 0, 2),
    ('Tweety',  'Canary',           45.00, 4,  0, 3),
    ('Nemo',    'Clownfish',        25.00, 2,  0, 4);
GO

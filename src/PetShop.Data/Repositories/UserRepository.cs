using Microsoft.EntityFrameworkCore;
using PetShop.Data.Context;
using PetShop.Data.Diagnostics;
using PetShop.Domain.Entities;

namespace PetShop.Data.Repositories;

public class UserRepository : Repository<User>, IUserRepository
{
    public UserRepository(PetShopDbContext db, ILayerTracer tracer) : base(db, tracer) { }

    public Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default)
        => Measure(nameof(GetByUsernameAsync),
            async () => await Set.FirstOrDefaultAsync(u => u.Username == username && u.IsActive, ct),
            new { username });
}

using PetShop.Domain.Entities;

namespace PetShop.Data.Repositories;

public interface IUserRepository : IRepository<User>
{
    Task<User?> GetByUsernameAsync(string username, CancellationToken ct = default);
}

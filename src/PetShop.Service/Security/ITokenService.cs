using PetShop.Domain.Entities;

namespace PetShop.Service.Security;

public interface ITokenService
{
    (string token, DateTime expiresUtc) CreateAccessToken(User user);
}

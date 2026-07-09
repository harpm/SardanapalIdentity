using Sardanapal.Identity.Contract.IModel;
using Sardanapal.ViewModel.Response;
using System.Security.Claims;

namespace Sardanapal.Identity.Contract.IService;

public interface ITokenService
{
    IResponse<bool> ValidateToken(string token, out ClaimsPrincipal claims);
    IResponse<bool> ValidateTokenRoles(string token, byte[] roleIds, IClaim[] claims);
    IResponse<string> GenerateToken(string uid, byte[] roleIds, IClaim[] claims, bool mustChangePassword = false);
}

using Sardanapal.ViewModel.Response;
using System.Security.Claims;

namespace Sardanapal.Identity.Contract.IService;

public interface ITokenService
{
    IResponse<bool> ValidateToken(string token, out ClaimsPrincipal claims);
    IResponse<bool> ValidateTokenRole(string token, byte roleId);
    IResponse<bool> ValidateTokenRoles(string token, byte[] roleIds);
    IResponse<string> GenerateToken(string uid, byte roleId);
    IResponse<string> GenerateToken(string uid, byte[] roleId);
}

using Sardanapal.Identity.Domain.Model;
using Sardanapal.Identity.Dto;
using Sardanapal.Identity.OTP.Models.Cach;
using Sardanapal.Identity.OTP.Services;
using Sardanapal.Identity.ViewModel.Models;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.Services.Services
{
    public interface IAccountServiceBase
    {
        public Response<LoginDto> Login(LoginVM Model);
    }

    public abstract class AccountServiceBase<TKey, TUser, TRole, TOtpCachModel> : IAccountServiceBase
        where TKey : IComparable<TKey>, IEquatable<TKey>
        where TUser : IUserBase<TKey>, new()
        where TRole : IRoleBase<byte>, new()
        where TOtpCachModel : OtpCachModel<TKey>, new()
    {
        protected IUserManagerService<TKey, TUser, TRole> userManagerService;
        protected IOtpCachService<TKey, TOtpCachModel> CacheService { get; set; }

        private OtpService OtpService { get; set; }

        protected virtual string ServiceName { get; set; }

        public AccountServiceBase()
        {
            
        }

        public async Task<Response<LoginDto>> Login(LoginVM Model)
        {
            var result = new Response<LoginDto>();

            try
            {
                string token = await userManagerService.Login(Model.Username, Model.Password);
                result.Set(StatusCode.Succeeded, new LoginDto() { Token = token });
            }
            catch (Exception ex)
            {
                result.Set(StatusCode.Exception, ex);
            }

            return result;
        }
    }
}

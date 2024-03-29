using StackExchange.Redis;
using Sardanapal.RedisCache.Services;
using Sardanapal.Identity.OTP.Models.Cach;
using Sardanapal.ViewModel.Response;
using Sardanapal.Identity.ViewModel.Models;

namespace Sardanapal.Identity.OTP.Services;

public interface IOtpCachService<TUserKey, TOtpCachModel> : ICacheService<TOtpCachModel, NewOtpVM<TUserKey>, OtpEditableVM<TUserKey>>
    , IOtpService<TUserKey, NewOtpVM<TUserKey>, ValidateOtpVM<TUserKey>>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TOtpCachModel : OtpCachModel<TUserKey>, new()
{

}

public class OtpCachService<TUserKey, TOtpCachModel> : CacheService<TOtpCachModel, NewOtpVM<TUserKey>, OtpEditableVM<TUserKey>>
    , IOtpService<TUserKey, NewOtpVM<TUserKey>, ValidateOtpVM<TUserKey>>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TOtpCachModel : OtpCachModel<Guid>, new()
{
    protected override string Key => "Otp";

    public int expireTime
    {
        get
        {
            return base.expireTime;
        }
        set
        {
            base.expireTime = value;
        }
    }

    protected IOtpHelper otpHelper { get; set; }
    public OtpCachService(IConnectionMultiplexer _conn, IOtpHelper _otpHelper, int _expireTime) : base(_conn, _expireTime)
    {
        otpHelper = _otpHelper;
    }

    public override Task<IResponse<Guid>> Add(NewOtpVM<TUserKey> Model)
    {
        Model.ExpireTime = DateTime.UtcNow.AddMinutes(base.expireTime);
        Model.Code = otpHelper.GenerateNewOtp();

        return base.Add(Model);
    }

    public Task<IResponse<bool>> ValidateOtp(ValidateOtpVM<TUserKey> model)
    {
        throw new NotImplementedException();
    }

    /// <summary>
    /// Otps have Expire time will be automatically removed,
    /// So don't use it, unless in special cases
    /// </summary>
    /// <returns></returns>
    public async Task RemoveExpireds()
    {
        var allOtpsRes = await GetAll();

        if (allOtpsRes.StatusCode == StatusCode.Succeeded)
        {
            var allOtps = allOtpsRes.Data;
            var ids = allOtps.Where(x => x.ExpireTime <= DateTime.UtcNow)
                .Select(x => x.Id)
                .ToList();

            foreach (var id in ids)
            {
                await Delete(id);
            }
        }
    }
}
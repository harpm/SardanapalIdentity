using StackExchange.Redis;
using Sardanapal.RedisCache.Services;
using Sardanapal.ViewModel.Response;
using AutoMapper;
using System.Text.Json;
using Sardanapal.Identity.OTP.VM;
using Sardanapal.Identity.OTP.Domain;

namespace Sardanapal.Identity.OTP.Services;

public interface IOtpCachService<TUserKey, TKey, TOtpCachModel>
    : ICacheService<TKey, OtpSearchVM, CachOtpVM<TUserKey, TKey>, CachNewOtpVM<TUserKey, TKey>, CachOtpEditableVM<TUserKey, TKey>>
    , IOtpServiceBase<TUserKey, TKey, CachNewOtpVM<TUserKey, TKey>, ValidateOtpVM<TUserKey>>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TOtpCachModel : OTPModel<TUserKey, TKey>, new()
{

}

public class OtpCachService<TUserKey, TKey, TOtpCachModel>
    : CacheService<OTPModel<TUserKey, TKey>, TKey, OtpSearchVM, CachOtpVM<TUserKey, TKey>, CachNewOtpVM<TUserKey, TKey>, CachOtpEditableVM<TUserKey, TKey>>
    , IOtpCachService<TUserKey, TKey, TOtpCachModel>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TOtpCachModel : OTPModel<TUserKey, TKey>, new()
{
    protected override string key => "Otp";

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
    public OtpCachService(IConnectionMultiplexer _conn, IMapper _mapper, IOtpHelper _otpHelper, int _expireTime) : base(_conn, _mapper, _expireTime)
    {
        otpHelper = _otpHelper;
    }

    public override Task<IResponse<TKey>> Add(CachNewOtpVM<TUserKey, TKey> Model)
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
        var allOtps = await GetCurrentDatabase().HashGetAllAsync(new RedisKey(key));

        var ids = allOtps
            .Select(x => JsonSerializer.Deserialize<OTPModel<TUserKey, TKey>>(x.Value))
            .AsEnumerable()
            .Where(x => x?.ExpireTime <= DateTime.UtcNow)
            .Select(x => x.Id)
            .ToList();

        foreach (var id in ids)
        {
            await Delete(id);
        }
    }
}
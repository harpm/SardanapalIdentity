using StackExchange.Redis;
using Sardanapal.RedisCache.Services;
using Sardanapal.ViewModel.Response;
using Sardanapal.Identity.OTP.Models.Domain;
using Sardanapal.Identity.ViewModel.Models.VM;
using AutoMapper;
using System.Text.Json;

namespace Sardanapal.Identity.OTP.Services;

public interface IOtpCachService<TUserKey, TOtpCachModel>
    : ICacheService<OtpSearchVM, OtpVM, NewOtpVM<TUserKey>, OtpEditableVM<TUserKey>>
    , IOtpService<TUserKey, NewOtpVM<TUserKey>, ValidateOtpVM<TUserKey>>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TOtpCachModel : OTPModel<TUserKey>, new()
{

}

public class OtpCachService<TUserKey, TOtpCachModel> : CacheService<OTPModel<TUserKey>, OtpSearchVM, OtpVM, NewOtpVM<TUserKey>, OtpEditableVM<TUserKey>>
    , IOtpService<TUserKey, NewOtpVM<TUserKey>, ValidateOtpVM<TUserKey>>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TOtpCachModel : OTPModel<Guid>, new()
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
    public OtpCachService(IConnectionMultiplexer _conn, IMapper _mapper, IOtpHelper _otpHelper, int _expireTime) : base(_conn, _mapper, _expireTime)
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
        var allOtps = await GetCurrentDatabase().HashGetAllAsync(new RedisKey(Key));

        var ids = allOtps
            .Select(x => JsonSerializer.Deserialize<OTPModel<TUserKey>>(x.Value))
            .AsEnumerable()
            .Where(x => x?.ExpireTime <= DateTime.UtcNow)
            .Select(x => x?.Id)
            .ToList();

        foreach (var id in ids)
        {
            if (id.HasValue)
            {
                await Delete(id.Value);
            }
        }
    }
}
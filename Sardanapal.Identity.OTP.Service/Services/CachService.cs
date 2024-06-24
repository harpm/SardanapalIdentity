using StackExchange.Redis;
using Sardanapal.RedisCache.Services;
using Sardanapal.ViewModel.Response;
using AutoMapper;
using System.Text.Json;
using Sardanapal.Identity.OTP.VM;
using Sardanapal.Identity.OTP.Domain;

namespace Sardanapal.Identity.OTP.Services;

public interface IOtpCachService<TUserKey, TKey, TOtpCachModel, TNewVM, TEditableVM>
    : ICacheService<TOtpCachModel, TKey, OtpSearchVM, CachOtpVM<TUserKey, TKey>, TNewVM, TEditableVM>
    , IOtpServiceBase<TUserKey, TKey, TNewVM, ValidateOtpVM<TUserKey>>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TOtpCachModel : IOTPModel<TUserKey, TKey>, new()
    where TNewVM : CachNewOtpVM<TUserKey, TKey>, new()
    where TEditableVM : CachOtpEditableVM<TUserKey, TKey>, new()
{

}

public class OtpCachService<TUserKey, TKey, TOtpCachModel, TNewVM, TEditableVM>
    : CacheService<TOtpCachModel, TKey, OtpSearchVM, CachOtpVM<TUserKey, TKey>, TNewVM, TEditableVM>
    , IOtpCachService<TUserKey, TKey, TOtpCachModel, TNewVM, TEditableVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TOtpCachModel : IOTPModel<TUserKey, TKey>, new()
    where TNewVM : CachNewOtpVM<TUserKey, TKey>, new()
    where TEditableVM : CachOtpEditableVM<TUserKey, TKey>, new()
{
    protected override string key => "Otp";
    public override string ServiceName => "OtpService";

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
    public OtpCachService(IConnectionMultiplexer _conn, IMapper _mapper, IOtpHelper _otpHelper, int _expireTime)
        : base(_conn, _mapper, _expireTime)
    {
        otpHelper = _otpHelper;
    }

    public override Task<IResponse<TKey>> Add(TNewVM model)
    {
        model.ExpireTime = DateTime.UtcNow.AddMinutes(base.expireTime);
        model.Code = otpHelper.GenerateNewOtp();

        return base.Add(model);
    }

    public async Task<IResponse<bool>> ValidateOtp(ValidateOtpVM<TUserKey> model)
    {
        IResponse<bool> result = new Response<bool>(ServiceName, OperationType.Fetch);
        return await result.FillAsync(async () =>
        {
            var items = await InternalGetAll();
            if (items != null)
            {
                result.Set(StatusCode.Succeeded
                    , items.Where(i => i.UserId.Equals(model.UserId) && i.Code == model.Code && i.RoleId == model.RoleId)
                    .Any());
            }
            else
            {
                result.Set(StatusCode.NotExists);
            }
        });
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
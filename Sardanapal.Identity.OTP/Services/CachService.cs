using StackExchange.Redis;
using Sardanapal.RedisCache.Services;
using Sardanapal.Identity.OTP.Models.Cach;
using Sardanapal.ViewModel.Response;
using Sardanapal.RedisCache.Models;

namespace Sardanapal.Identity.OTP.Services;

public interface IOtpHelper
{
    int OtpLength { get; }
    string GenerateNewOtp();
}

public class OtpHelper : IOtpHelper
{
    protected int _otpLength;
    public int OtpLength { get { return _otpLength; } }

    public OtpHelper(int otpLength)
    {
        _otpLength = otpLength;
    }

    public string GenerateNewOtp()
    {
        char[] nines = new char[OtpLength];
        Array.Fill(nines, '9');
        return Random.Shared.Next(Convert.ToInt32(string.Join("", nines)))
            .ToString("D" + OtpLength);
    }
}

public interface IOtpCachService<TKey, TOtpCachModel> : ICacheService<TKey, TOtpCachModel>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TOtpCachModel : OtpCachModel<TKey>, new()
{
    Task RemoveExpireds();
}

public class OtpCachService<TKey, TOtpCachModel> : CacheService<TKey, TOtpCachModel>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TOtpCachModel : OtpCachModel<TKey>, new()
{
    protected IOtpHelper otpHelper {  get; set; }
    public OtpCachService(IConnectionMultiplexer _conn, IOtpHelper _otpHelper, int _expireTime) : base(_conn, _expireTime)
    {
        otpHelper = _otpHelper;
    }

    public override Task<CacheResponse<TKey>> Add(TOtpCachModel Model)
    {
        Model.ExpireTime = DateTime.UtcNow.AddMinutes(base.expireTime);
        Model.Code = otpHelper.GenerateNewOtp();

        return base.Add(Model);
    }

    public async Task RemoveExpireds()
    {
        var allOtpsRes = await GetAll();

        if (allOtpsRes.Status == StatusCode.Succeeded)
        {
            var allOtps = allOtpsRes.Value;
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
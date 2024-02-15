using StackExchange.Redis;
using Sardanapal.RedisCache.Services;
using Sardanapal.Identity.OTP.Models.Cach;
using Sardanapal.ViewModel.Response;
using Sardanapal.RedisCache.Models;

namespace Sardanapal.Identity.OTP.Services
{
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
        public OtpCachService(IConnectionMultiplexer _conn, int _expireTime) : base(_conn, _expireTime)
        {

        }

        public override Task<CacheResponse<TKey>> Add(TOtpCachModel Model)
        {
            Model.ExpireTime = DateTime.UtcNow.AddMinutes(base.expireTime);

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
}

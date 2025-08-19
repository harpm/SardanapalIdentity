using AutoMapper;
using System.Text.Json;
using StackExchange.Redis;
using Sardanapal.Contract.IModel;
using Sardanapal.Contract.IService;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Localization;
using Sardanapal.Identity.OTP.Domain;
using Sardanapal.Identity.ViewModel.Otp;
using Sardanapal.RedisCache.Services;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.OTP.Services;

public interface IOtpCachService<TUserKey, TKey, TOtpCachModel, TNewVM, TEditableVM, TOTPLoginVM, TOTPRegisterVM>
    : ICacheService<TOtpCachModel, TKey, OtpSearchVM, CachOtpVM<TUserKey, TKey>, TNewVM, TEditableVM>
    , IOtpServiceBase<TUserKey, TKey, TNewVM, TOTPLoginVM, TOTPRegisterVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TOtpCachModel : IOTPModel<TUserKey, TKey>, new()
    where TNewVM : CachNewOtpVM<TUserKey, TKey>, new()
    where TEditableVM : CachOtpEditableVM<TUserKey, TKey>, new()
    where TOTPLoginVM : OTPLoginVM<TUserKey>, new()
    where TOTPRegisterVM : OTPRegisterVM<TUserKey>, new()
{

}

public class OtpCachService<TUserKey, TKey, TOtpCachModel, TNewVM, TEditableVM, TOTPLoginVM, TOTPRegisterVM>
    : CacheService<TOtpCachModel, TKey, OtpSearchVM, CachOtpVM<TUserKey, TKey>, TNewVM, TEditableVM>
    , IOtpCachService<TUserKey, TKey, TOtpCachModel, TNewVM, TEditableVM, TOTPLoginVM, TOTPRegisterVM>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TOtpCachModel : class, ICachModel<TKey>, IOTPModel<TUserKey, TKey>, new()
    where TNewVM : CachNewOtpVM<TUserKey, TKey>, new()
    where TEditableVM : CachOtpEditableVM<TUserKey, TKey>, new()
    where TOTPLoginVM : OTPLoginVM<TUserKey>, new()
    where TOTPRegisterVM : OTPRegisterVM<TUserKey>, new()
{
    protected override string key => "Otp";
    public override string ServiceName => "OtpService";

    protected IOtpHelper otpHelper { get; set; }

    protected override int expireTime { get => base.expireTime; set => base.expireTime = value; }
    protected IEmailService emailService { get; set; }
    protected ISmsService smsService { get; set; }

    public OtpCachService(IConnectionMultiplexer _conn
        , IMapper _mapper
        , IOtpHelper _otpHelper
        , IEmailService _emailService
        , ISmsService _smsService)
        : base(_conn, _mapper)
    {
        otpHelper = _otpHelper;
        emailService = _emailService;
        smsService = _smsService;
    }

    protected virtual string CreateSMSOtpMessage(TNewVM model)
    {
        return model.Code;
    }

    protected virtual string CreateEmailOtpMessage(TNewVM model)
    {
        return model.Code;
    }

    public override async Task<IResponse<TKey>> Add(TNewVM model, CancellationToken ct = default)
    {
        model.ExpireTime = DateTime.UtcNow.AddMinutes(base.expireTime);
        model.Code = otpHelper.GenerateNewOtp();

        Response<TKey> result = new Response<TKey>(GetType().Name, OperationType.Add);
        return await result.FillAsync(async () =>
        {
            TKey newId = model.Id;
            TOtpCachModel value = mapper.Map<TNewVM, TOtpCachModel>(model);
            var items = await InternalGetAll();

            if (!items.Where(x => x.UserId.Equals(model.UserId) && x.RoleId == model.RoleId).Any())
            {
                bool added = await GetCurrentDatabase()
                    .HashSetAsync(rKey
                        , new RedisValue(newId.ToString())
                        , new RedisValue(JsonSerializer.Serialize(value)));

                if (expireTime > 0)
                {
                    await GetCurrentDatabase().ExecuteAsync($"HEXPIRE {key} {expireTime * 60} FIELDS 1 {newId.ToString()}");
                }

                if (long.TryParse(model.Recipient, out long _))
                    smsService.Send(model.Recipient, CreateSMSOtpMessage(model));
                else
                    emailService.Send(model.Recipient, CreateEmailOtpMessage(model));


                result.Set(StatusCode.Succeeded, newId);
            }
            else
            {
                result.Set(StatusCode.Canceled);
                result.UserMessage = string.Format(Identity_Messages.OtpCooldown, expireTime);
            }

        });
    }

    public virtual async Task<IResponse<bool>> ValidateOtpRegister(TOTPRegisterVM model)
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
                result.UserMessage = string.Format(Identity_Messages.InvalidOtpCode, model.Code);
            }
        });
    }

    public virtual async Task<IResponse<bool>> ValidateOtpLogin(TOTPLoginVM model)
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
                result.UserMessage = string.Format(Identity_Messages.InvalidOtpCode, model.Code);
            }
        });
    }

    /// <summary>
    /// Otps have Expire time will be automatically removed,
    /// So don't use it, unless in special cases
    /// </summary>
    /// <returns></returns>
    public virtual async Task RemoveExpireds()
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

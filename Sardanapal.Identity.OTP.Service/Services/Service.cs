using AutoMapper;
using Microsoft.EntityFrameworkCore;
using Sardanapal.Contract.IService;
using Sardanapal.Ef.Service.Services;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.OTP.Domain;
using Sardanapal.Identity.ViewModel.Otp;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.OTP.Services;

public class OtpService<TContext, TUserKey, TKey, TListItemVM, TSearchVM, TVM, TNewVM, TEditableVM, TValidateVM>
    : EfCrudService<TContext, TKey, OTPModel<TUserKey, TKey>, TListItemVM, TSearchVM, TVM, TNewVM, TEditableVM>
    , IOtpService<TUserKey, TKey, TSearchVM, TVM, TNewVM, TEditableVM, TValidateVM>
    where TContext : DbContext
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TListItemVM : OtpListItemVM<TKey>
    where TSearchVM : OtpSearchVM, new()
    where TVM : OtpVM, new()
    where TNewVM : NewOtpVM<TUserKey>, new()
    where TEditableVM : OtpEditableVM<TUserKey>, new()
    where TValidateVM : ValidateOtpVM<TUserKey>, new()
{
    public int expireTime { get; set; }

    public override string ServiceName => "OtpService";

    protected IOtpHelper otpHelper;
    protected IEmailService emailService { get; set; }
    protected ISmsService smsService { get; set; }

    public OtpService(TContext context
        , IMapper _Mapper
        , IRequestService _request
        , IEmailService _emailService
        , ISmsService _smsService
        , IOtpHelper _otpHelper)
        : base(context, _Mapper, _request)
    {
        emailService = _emailService;
        smsService = _smsService;
        otpHelper = _otpHelper;
    }

    protected virtual string CreateSMSOtpMessage(TNewVM model)
    {
        return model.Code;
    }

    protected virtual string CreateEmailOtpMessage(TNewVM model)
    {
        return model.Code;
    }

    public override async Task<IResponse<TKey>> Add(TNewVM model)
    {
        model.ExpireTime = DateTime.UtcNow.AddMinutes(expireTime);
        model.Code = otpHelper.GenerateNewOtp();
        Response<TKey> Result = new Response<TKey>(ServiceName, OperationType.Add);
        return await Result.FillAsync(async delegate
        {
            OTPModel<TUserKey, TKey> Item = Mapper.Map<OTPModel<TUserKey, TKey>>(model);
            await UnitOfWork.AddAsync(Item);
            await UnitOfWork.SaveChangesAsync();

            if (long.TryParse(model.Recipient, out long _))
                smsService.Send(model.Recipient, CreateSMSOtpMessage(model));
            else
                emailService.Send(model.Recipient, CreateEmailOtpMessage(model));


            Result.Set(StatusCode.Succeeded, Item.Id);
        });
    }

    public async Task RemoveExpireds()
    {
        this.UnitOfWork.RemoveRange(GetCurrentService().Where(x => x.ExpireTime <= DateTime.UtcNow));
        await this.UnitOfWork.SaveChangesAsync();
    }

    public Task<IResponse<bool>> ValidateOtp(TValidateVM model)
    {
        throw new NotImplementedException();
    }
}
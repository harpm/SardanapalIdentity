using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Sardanapal.Contract.IService;
using Sardanapal.Service;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Share.Resources;
using Sardanapal.Identity.ViewModel.Otp;
using Sardanapal.ViewModel.Models;
using Sardanapal.ViewModel.Response;
using Sardanapal.Identity.Contract.IRepository;
using Sardanapal.Ef.Helper;

namespace Sardanapal.Identity.OTP.Services;

public class OtpService<TRepository, TUserKey, TKey, TOTPModel, TListItemVM, TSearchVM, TVM, TNewVM, TEditableVM, TValidateVM>
    : CrudServiceBase<TRepository, TKey, TOTPModel, TSearchVM, TVM, TNewVM, TEditableVM>
    , IOtpService<TUserKey, TKey, TSearchVM, TVM, TNewVM, TEditableVM, TValidateVM>
    where TRepository : class, IOTPRepository<TKey, TOTPModel>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TOTPModel : class, IOTPModel<TUserKey, TKey>, new()
    where TListItemVM : OtpListItemVM<TKey>
    where TSearchVM : OtpSearchVM, new()
    where TVM : OtpVM, new()
    where TNewVM : NewOtpVM<TUserKey>, new()
    where TEditableVM : OtpEditableVM<TUserKey>, new()
    where TValidateVM : ValidateOtpVM<TUserKey>, new()
{
    public int expireTime { get; set; }

    protected override string ServiceName => "OtpService";

    protected IOtpHelper otpHelper;
    protected IEmailService emailService { get; set; }
    protected ISmsService smsService { get; set; }

    public OtpService(TRepository repository
        , IMapper mapper
        , IRequestService _request
        , IEmailService _emailService
        , ISmsService _smsService
        , IOtpHelper _otpHelper) : base(repository, mapper)
    {
        emailService = _emailService;
        smsService = _smsService;
        otpHelper = _otpHelper;
        
    }

    protected virtual IQueryable<TOTPModel> Search(IQueryable<TOTPModel> query, TSearchVM searchModel)
    {
        return query;
    }

    public override async Task<IResponse<GridVM<TKey, T>>> GetAll<T>(GridSearchModelVM<TKey, TSearchVM> SearchModel = null, CancellationToken ct = default)
    {
        IResponse<GridVM<TKey, T>> result = new Response<GridVM<TKey, T>>(ServiceName, OperationType.Fetch);

        await result.FillAsync(async () =>
        {
            SearchModel = SearchModel ?? new GridSearchModelVM<TKey, TSearchVM>();
            SearchModel.Fields = SearchModel.Fields ?? new TSearchVM();

            var data = new GridVM<TKey, T>(SearchModel);

            var query = _repository.FetchAll().AsQueryable().AsNoTracking();

            query = Search(query, SearchModel.Fields);

            query = QueryHelper.Search(query, SearchModel).AsQueryable();

            data.List = await query.ProjectTo<T>(_mapper.ConfigurationProvider)
                .ToListAsync();

            result.Set(StatusCode.Succeeded, data);
        });

        return result;
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
        model.ExpireTime = DateTime.UtcNow.AddMinutes(expireTime);
        model.Code = otpHelper.GenerateNewOtp();

        Response<TKey> result = new Response<TKey>(ServiceName, OperationType.Add);
        return await result.FillAsync(async delegate
        {
            if (!await _repository.FetchAll().AsQueryable().AsNoTracking()
                .Where(x => x.UserId.Equals(model.UserId) && x.RoleId == model.RoleId)
                .AnyAsync())
            {
                TOTPModel Item = _mapper.Map<TOTPModel>(model);
                await _repository.AddAsync(Item);
                await _repository.SaveChangesAsync();

                if (long.TryParse(model.Recipient, out long _))
                    smsService.Send(model.Recipient, CreateSMSOtpMessage(model));
                else
                    emailService.Send(model.Recipient, CreateEmailOtpMessage(model));

                result.Set(StatusCode.Succeeded, Item.Id);
            }
            else
            {
                result.Set(StatusCode.Canceled);
                result.UserMessage = string.Format(Service_Messages.OtpCooldown, expireTime);
            }
        });
    }

    public virtual async Task RemoveExpireds()
    {
        await this._repository.DeleteRangeAsync(_repository
            .FetchAll().AsQueryable()
            .Where(x => x.ExpireTime <= DateTime.UtcNow)
            .Select(x => x.Id)
            .ToList());
        await this._repository.SaveChangesAsync();
    }

    public virtual async Task<IResponse<bool>> ValidateOtp(TValidateVM model)
    {
        IResponse<bool> result = new Response<bool>(ServiceName, OperationType.Fetch);

        return await result.FillAsync(async () =>
        {
            var isValid = await _repository.FetchAll().AsQueryable()
                .Where(x => x.RoleId == model.RoleId && x.Code == model.Code && x.UserId.Equals(model.UserId))
                .AnyAsync();

            result.Set(StatusCode.Succeeded, isValid);
        });
    }
}
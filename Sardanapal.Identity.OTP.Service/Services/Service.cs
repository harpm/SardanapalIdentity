using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Sardanapal.Contract.Data;
using Sardanapal.Contract.IRepository;
using Sardanapal.Contract.IService;
using Sardanapal.Ef.Helper;
using Sardanapal.Identity.Contract.IModel;
using Sardanapal.Identity.Contract.IRepository;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Localization;
using Sardanapal.Identity.ViewModel.Otp;
using Sardanapal.Service;
using Sardanapal.Service.Utilities;
using Sardanapal.ViewModel.Models;
using Sardanapal.ViewModel.Response;

namespace Sardanapal.Identity.OTP.Services;

public class EFOtpService<TEFDatabaseManager, TRepository, TUserKey, TKey, TOTPModel, TListItemVM, TVM, TNewVM, TEditableVM>
    : EFCurdServiceBase<TEFDatabaseManager, TRepository, TKey, TOTPModel, OtpSearchVM, TVM, TNewVM, TEditableVM>
    , IOtpService<TUserKey, TKey, TVM, TNewVM, TEditableVM>
    where TEFDatabaseManager : IEFDatabaseManager
    where TRepository : IEFOTPRepository<TKey, TOTPModel>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TOTPModel : class, IOTPModel<TUserKey, TKey>, new()
    where TListItemVM : OtpListItemVM<TKey>
    where TVM : OtpVM<TUserKey>, new()
    where TNewVM : NewOtpVM<TUserKey>, new()
    where TEditableVM : OtpEditableVM<TUserKey>, new()
{
    public int expireTime { get; set; }

    protected override string ServiceName => "OtpService";

    protected IOtpHelper otpHelper;
    protected IEmailService emailService { get; set; }
    protected ISmsService smsService { get; set; }

    public EFOtpService(TEFDatabaseManager dbManager
        , TRepository repository
        , IMapper mapper
        , IRequestService _request
        , IEmailService _emailService
        , ISmsService _smsService
        , IOtpHelper _otpHelper
        , ILogger logger) : base(dbManager, repository, mapper, logger)
    {
        emailService = _emailService;
        smsService = _smsService;
        otpHelper = _otpHelper;

    }

    protected override IQueryable<TOTPModel> Search(IQueryable<TOTPModel> query, OtpSearchVM searchModel)
    {
        return query;
    }

    public override async Task<IResponse<GridVM<TKey, T>>> GetAll<T>(GridSearchModelVM<TKey, OtpSearchVM> SearchModel = null, CancellationToken ct = default)
    {
        IResponse<GridVM<TKey, T>> result = new Response<GridVM<TKey, T>>(ServiceName, OperationType.Fetch, _logger);

        await result.FillAsync(async () =>
        {
            SearchModel = SearchModel ?? new GridSearchModelVM<TKey, OtpSearchVM>();
            SearchModel.Fields = SearchModel.Fields ?? new OtpSearchVM();

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

        Response<TKey> result = new Response<TKey>(ServiceName, OperationType.Add, _logger);
        return await result.FillAsync(async delegate
        {
            if (!await _repository.FetchAll().AsNoTracking()
                .Where(x => x.UserId.Equals(model.UserId) && x.RoleId == model.RoleId)
                .AnyAsync())
            {
                TOTPModel Item = _mapper.Map<TOTPModel>(model);
                await _repository.AddAsync(Item);
                await _dbManager.SaveChangesAsync(ct);

                // TODO: should implement a background service to remove expired otps
                // or better to create a setTimeout func to handle the expired otps

                if (long.TryParse(model.Recipient, out long _))
                    smsService.Send(model.Recipient, CreateSMSOtpMessage(model));
                else
                    emailService.Send(model.Recipient, CreateEmailOtpMessage(model));

                result.Set(StatusCode.Succeeded, Item.Id);
            }
            else
            {
                result.Set(StatusCode.Canceled);
                result.UserMessage = string.Format(Identity_Messages.OtpCooldown, expireTime);
            }
        });
    }

    // TODO: this method should be removed and handled by a background service or cache expiration
    public virtual async Task RemoveExpireds()
    {
        await this._repository.DeleteRangeAsync(_repository
            .FetchAll()
            .Where(x => x.ExpireTime <= DateTime.UtcNow)
            .Select(x => x.Id)
            .ToList());
        await this._dbManager.SaveChangesAsync();
    }

    public virtual async Task<IResponse<TVM>> ValidateCode(TNewVM model)
    {
        IResponse<TVM> result = new Response<TVM>(ServiceName, OperationType.Fetch, _logger);

        return await result.FillAsync(async () =>
        {
            var otpModel = await _repository.FetchAll().AsNoTracking()
                .Where(x => x.RoleId == model.RoleId && x.Code == model.Code && x.UserId.Equals(model.UserId))
                .FirstOrDefaultAsync();

            if (otpModel != null)
            {
                var data = _mapper.Map<TVM>(otpModel);
                result.Set(StatusCode.Succeeded, data);
            }
            else
            {
                result.Set(StatusCode.NotExists);
            }
        });
    }

}


public class OtpService<TRepository, TUserKey, TKey, TOTPModel, TListItemVM, TVM, TNewVM, TEditableVM>
    : CrudServiceBase<TRepository, TKey, TOTPModel, OtpSearchVM, TVM, TNewVM, TEditableVM>
    , IOtpService<TUserKey, TKey, TVM, TNewVM, TEditableVM>
    where TRepository : IOTPRepository<TKey, TOTPModel>, IMemoryRepository<TKey, TOTPModel>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
    where TOTPModel : class, IOTPModel<TUserKey, TKey>, new()
    where TListItemVM : OtpListItemVM<TKey>
    where TVM : OtpVM<TUserKey>, new()
    where TNewVM : NewOtpVM<TUserKey>, new()
    where TEditableVM : OtpEditableVM<TUserKey>, new()
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
        , IOtpHelper _otpHelper
        , ILogger logger) : base(repository, mapper, logger)
    {
        emailService = _emailService;
        smsService = _smsService;
        otpHelper = _otpHelper;

    }

    public override async Task<IResponse<GridVM<TKey, T>>> GetAll<T>(GridSearchModelVM<TKey, OtpSearchVM> SearchModel = null, CancellationToken ct = default)
    {
        IResponse<GridVM<TKey, T>> result = new Response<GridVM<TKey, T>>(ServiceName, OperationType.Fetch, _logger);

        await result.FillAsync(async () =>
        {
            SearchModel = SearchModel ?? new GridSearchModelVM<TKey, OtpSearchVM>();
            SearchModel.Fields = SearchModel.Fields ?? new OtpSearchVM();

            var data = new GridVM<TKey, T>(SearchModel);

            var query = await _repository.FetchAllAsync();

            //query = Search(query, SearchModel.Fields);

            query = EnumerableHelper.Search(query, SearchModel);

            data.List = query.Select(_mapper.Map<T>)
                .ToList();

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

        Response<TKey> result = new Response<TKey>(ServiceName, OperationType.Add, _logger);
        return await result.FillAsync(async () =>
        {
            if (!_repository.FetchAll()
                .Where(x => x.UserId.Equals(model.UserId) && x.RoleId == model.RoleId)
                .Any())
            {
                TOTPModel Item = _mapper.Map<TOTPModel>(model);
                await _repository.AddAsync(Item);

                if (long.TryParse(model.Recipient, out long _))
                    smsService.Send(model.Recipient, CreateSMSOtpMessage(model));
                else
                    emailService.Send(model.Recipient, CreateEmailOtpMessage(model));

                result.Set(StatusCode.Succeeded, Item.Id);
            }
            else
            {
                result.Set(StatusCode.Canceled);
                result.UserMessage = string.Format(Identity_Messages.OtpCooldown, expireTime);
            }
        });
    }

    [Obsolete("Use expire time on the cache provider instead of this method")]
    public virtual async Task RemoveExpireds()
    {
        await this._repository.DeleteRangeAsync(_repository
            .FetchAll()
            .Where(x => x.ExpireTime <= DateTime.UtcNow)
            .Select(x => x.Id)
            .ToList());
    }

    public virtual Task<IResponse<TVM>> ValidateCode(TNewVM model)
    {
        IResponse<TVM> result = new Response<TVM>(ServiceName, OperationType.Fetch, _logger);

        return Task.FromResult(result.Fill(() =>
        {
            var otpModel = _repository.FetchAll()
                .Where(x => x.RoleId == model.RoleId && x.Code == model.Code && x.UserId.Equals(model.UserId))
                .FirstOrDefault();

            if (otpModel != null)
            {
                var data = _mapper.Map<TVM>(otpModel);
                result.Set(StatusCode.Succeeded, data);
            }
            else
            {
                result.Set(StatusCode.NotExists);
            }

        }));
    }
}

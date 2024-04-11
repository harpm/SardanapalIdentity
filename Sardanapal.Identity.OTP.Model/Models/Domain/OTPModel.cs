using Sardanapal.DomainModel.Domain;
using Sardanapal.RedisCach.Models;
using System.ComponentModel.DataAnnotations;

namespace Sardanapal.Identity.OTP.Models.Domain;

public interface IOTPModel<TUserKey> : IBaseEntityModel<Guid>, ICachModel<Guid>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    string Code { get; set; }

    TUserKey UserId { get; set; }

    DateTime ExpireTime { get; set; }

    byte? Role { get; set; }
}

public class OTPModel<TUserKey> : BaseEntityModel<Guid>, IOTPModel<TUserKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    [Required]
    public virtual string Code { get; set; }

    [Required]
    public virtual TUserKey UserId { get; set; }

    [Required]
    public virtual DateTime ExpireTime { get; set; }

    public virtual byte? Role { get; set; }
}
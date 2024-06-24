using Sardanapal.Domain.Model;
using Sardanapal.RedisCach.Models;
using System.ComponentModel.DataAnnotations;

namespace Sardanapal.Identity.OTP.Domain;

public interface IOTPModel<TUserKey, TKey> : IBaseEntityModel<TKey>, ICachModel<TKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    string Code { get; set; }

    TUserKey UserId { get; set; }

    DateTime ExpireTime { get; set; }

    byte RoleId { get; set; }
}

public class OTPModel<TUserKey, TKey> : BaseEntityModel<TKey>, IOTPModel<TUserKey, TKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    [Required]
    public virtual string Code { get; set; }

    [Required]
    public virtual TUserKey UserId { get; set; }

    [Required]
    public virtual DateTime ExpireTime { get; set; }

    public virtual byte? RoleId { get; set; }
}
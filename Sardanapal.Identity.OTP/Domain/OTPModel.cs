using Sardanapal.Domain.Model;
using Sardanapal.Identity.Contract.IModel;
using System.ComponentModel.DataAnnotations;

namespace Sardanapal.Identity.OTP.Domain;

public class OTPModel<TUserKey, TKey> : BaseEntityModel<TKey>
    , IOTPModel<TUserKey, TKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    [Required]
    public virtual string Code { get; set; }

    [Required]
    public virtual TUserKey UserId { get; set; }

    [Required]
    public virtual DateTime ExpireTime { get; set; }

    public virtual byte RoleId { get; set; }
}
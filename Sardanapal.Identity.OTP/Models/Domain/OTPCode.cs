using Sardanapal.DomainModel.Domain;
using System.ComponentModel.DataAnnotations;

namespace Sardanapal.Identity.OTP.Models.Domain;

public interface IOTPCode<TKey>
    : IBaseEntityModel<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    string Code { get; set; }

    TKey UserId { get; set; }

    DateTime ExpireTime { get; set; }

    byte? Role { get; set; }
}

public class OTPCode<TKey> : BaseEntityModel<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    [Required]
    public virtual string Code { get; set; }

    [Required]
    public virtual TKey UserId { get; set; }

    [Required]
    public virtual DateTime ExpireTime { get; set; }

    public virtual byte? Role { get; set; }
}
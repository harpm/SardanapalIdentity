﻿using Sardanapal.DomainModel.Domain;
using System.ComponentModel.DataAnnotations;

namespace Sardanapal.Identity.OTP.Models.Domain;

public interface IOTPCode<TUserKey>
    : IBaseEntityModel<TUserKey>, IOtpModel<TUserKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    string Code { get; set; }

    TUserKey UserId { get; set; }

    DateTime ExpireTime { get; set; }

    byte? Role { get; set; }
}

public class OTPCode<TUserKey> : BaseEntityModel<Guid>
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
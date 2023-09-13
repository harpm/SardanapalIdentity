using Sardanapal.ModelBase.Model.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Sardanapal.Identity.OTP.Models.Domain
{
    public interface IOTPCode<TKey>
        : IBaseEntityModel<TKey>
        where TKey : IComparable<TKey>, IEquatable<TKey>
    {
        string Code { get; set; }

        TKey  UserId { get; set; }

        DateTime ExpireTime { get; set; }

        byte? Role { get; set; }
    }

    public class OTPCode<TKey> : BaseEntityModel<TKey>
        where TKey : IComparable<TKey>, IEquatable<TKey>
    {
        [Required]
        [MaxLength(6)]
        public string Code { get; set; }

        [Required]
        public virtual TKey UserId { get; set; }

        [Required]
        public DateTime ExpireTime { get; set; }

        public byte? Role { get; set; }
    }
}

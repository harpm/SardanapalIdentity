using Sardanapal.DomainModel.Domain;

namespace Sardanapal.Identity.OTP.Models.Cach;

public class OtpCachModel<TUserKey> : IBaseEntityModel<Guid>, IOtpModel<TUserKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    public Guid Id { get; set; }
    
    public string Code { get; set; }
    
    public DateTime ExpireTime { get; set; }
    
    public TUserKey UserId { get; set; }

    public byte Role { get; set; }
}
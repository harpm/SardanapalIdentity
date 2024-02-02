using Sardanapal.DomainModel.Domain;

namespace Sardanapal.Identity.OTP.Models.Cach;

public class OtpCachModel<TKey> : IBaseEntityModel<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    public TKey Id { get; set; }
    
    /// <summary>
    /// This can be MobileNumber, EmailAddress or anything else 
    /// </summary>
    public long PhoneNumber { get; set; }
    
    public string Code { get; set; }
    
    public DateTime ExpireTime { get; set; }
    
    public byte Role { get; set; }
}
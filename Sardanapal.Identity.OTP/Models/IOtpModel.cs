
namespace Sardanapal.Identity.OTP.Models;

public interface IOtpModel<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>
{
    TKey Id { get; set; }
    string Code { get; set; }
    DateTime ExpireTime { get; set; }
    byte Role { get; set; }
}

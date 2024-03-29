
namespace Sardanapal.Identity.OTP.Models;

public interface IOtpModel<TUserKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    Guid Id { get; set; }
    string Code { get; set; }
    DateTime ExpireTime { get; set; }
    TUserKey UserId { get; set; }
    byte Role { get; set; }
}

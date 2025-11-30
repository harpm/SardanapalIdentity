
using Sardanapal.Contract.IModel;

namespace Sardanapal.Identity.Contract.IModel;
public interface IUser<TUserKey> : IBaseEntityModel<TUserKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    string FirstName { get; set; }
    string LastName { get; set; }
    string Username { get; set; }
    string HashedPassword { get; set; }
    string? Email { get; set; }
    bool VerifiedEmail { get; set; }
    ulong? PhoneNumber { get; set; }
    bool VerifiedPhoneNumber { get; set; }
}

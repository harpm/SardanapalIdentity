
namespace Sardanapal.Identity.ViewModel.Models.Account;

public record UserSearchVM
{
    public string Username { get; init; }
    public string Email { get; init; }
    public long? PhoneNumber { get; init; }
}

public record UserVM<TKey>
    where TKey : IComparable<TKey>, IEquatable<TKey>

{
    public TKey Id { get; init; }
    public string Username { get; init; }
}

public record NewUserVM
{
    public string Username { get; init; }
    public string Password { get; init; }
    public List<byte> Roles { get; init; }
}

public record UserEditableVM : NewUserVM
{

}

public record LoginVM()
{
    public LoginVM(string username, string password)
        : this()
    {
        this.Username = username;
        this.Username = password;
    }

    public string Username { get; init; }
    public string Password { get; init; }
}

public record ChangePasswordVM<TUserKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    public TUserKey UserId { get; init; }
    public string NewPassword { get; init; }
}

public record ChangePasswordVM
{
    public string Username { get; init; }
    public string OldPassword { get; init; }
    public string NewPassword { get; init; }
}

public record ResetPasswordRequestVM
{
    public string? Email { get; init; }
    public ulong? PhoneNumber { get; init; }
}

public record ResetPasswordVM<TUserKey>
    where TUserKey : IComparable<TUserKey>, IEquatable<TUserKey>
{
    public TUserKey UserId { get; init; }
    public byte RoleId { get; init; }
    public string Code { get; init; }
    public string NewPassword { get; init; }
}

public record RegisterVM<TRoleKey>()
    where TRoleKey : IComparable<TRoleKey>, IEquatable<TRoleKey>
{
    public RegisterVM(string username, string password)
        : this()
    {
        this.Username = username;
        this.Username = password;
    }

    public string Username { get; init; }
    public string Password { get; init; }
    public List<TRoleKey> Roles { get; init; }
};

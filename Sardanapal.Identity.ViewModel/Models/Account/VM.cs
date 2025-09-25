
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

public record RegisterVM()
{
    public RegisterVM(string username, string password)
        : this()
    {
        this.Username = username;
        this.Username = password;
    }

    public string Username { get; init; }
    public string Password { get; init; }
};

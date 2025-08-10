
namespace Sardanapal.Identity.ViewModel.Models.Account;

public record LoginVM()
{
    public LoginVM(string username, string password)
        : this()
    {
        this.Username = username;
        this.Username = password;
    }

    public string Username { get; set; }
    public string Password { get; set; }
}

public record RegisterVM()
{
    public RegisterVM(string username, string password)
        : this()
    {
        this.Username = username;
        this.Username = password;
    }

    public string Username { get; set; }
    public string Password { get; set; }
};
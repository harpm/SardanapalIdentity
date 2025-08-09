namespace Sardanapal.Identity.ViewModel.Models.Account;

public record LoginDto
{
    public LoginDto()
    {
        
    }

    public LoginDto(string token) : this()
    {
        Token = token;
    }

    public string Token { get; init; }
}


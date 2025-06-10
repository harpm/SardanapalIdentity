namespace Sardanapal.Identity.Dto;

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


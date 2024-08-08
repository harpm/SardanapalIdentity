using FluentValidation;

namespace Sardanapal.Identity.ViewModel.Extensions;

public static class ValidationExtensions
{
    public static IRuleBuilder<T, string> Username<T>(this IRuleBuilder<T, string> builder)
    {
        return builder.Matches("^(?=.{8,20}$)(?![_.])(?!.*[_.]{2})[a-zA-Z0-9._]+(?<![_.])$");
    }

    public static IRuleBuilder<T, string> Pssword<T>(this IRuleBuilder<T, string> builder)
    {
        return builder.Matches("^(?=.*[A-Za-z])(?=.*)[A-Za-z]{8,}$");
    }

    public static IRuleBuilder<T, string> PsswordPlusCapitalLetter<T>(this IRuleBuilder<T, string> builder)
    {
        return builder.Matches("^(?=.*[a-z])(?=.*[A-Z])(?=.*)[a-zA-Z]{8,}$");
    }
}

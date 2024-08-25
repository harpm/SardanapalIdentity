using FluentValidation;
using Sardanapal.Identity.Share.Resources;

namespace Sardanapal.Identity.ViewModel.Extensions;

public static class ValidationExtensions
{
    public static IRuleBuilder<T, string> Username<T>(this IRuleBuilder<T, string> builder)
    {
        return builder.Matches("^(?=.{4,20}$)(?![_.])(?!.*[_.]{2})[a-zA-Z0-9._]+(?<![_.])$")
            .WithMessage(string.Format(Validation_Messages.Username, 4));
    }

    public static IRuleBuilder<T, string> Password<T>(this IRuleBuilder<T, string> builder)
    {
        return builder.Matches("^(?=.*[A-Za-z])[A-Za-z\\d]{4,}$")
            .WithMessage(string.Format(Validation_Messages.Password, 4));
    }

    public static IRuleBuilder<T, string> PasswordPlusCapitalLetter<T>(this IRuleBuilder<T, string> builder)
    {
        return builder.Matches("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)[a-zA-Z\\d]{8,}$")
            .WithMessage(string.Format(Validation_Messages.PasswordWithCapitalLetter, 8));
    }

    public static IRuleBuilder<T, long> PhoneNumber<T>(this IRuleBuilder<T, long> builder)
    {
        return builder.GreaterThan(9000000000)
            .WithMessage(Validation_Messages.PhoneNumber);
    }
    public static IRuleBuilder<T, string> PhoneNumber<T>(this IRuleBuilder<T, string> builder)
    {
        return builder.Matches("^(\\+989|00989|989|09|9)?\\d{9}$")
            .WithMessage(Validation_Messages.PhoneNumber);
    }
}

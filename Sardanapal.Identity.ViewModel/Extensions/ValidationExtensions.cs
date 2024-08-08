﻿using FluentValidation;

namespace Sardanapal.Identity.ViewModel.Extensions;

public static class ValidationExtensions
{
    public static IRuleBuilder<T, string> Username<T>(this IRuleBuilder<T, string> builder)
    {
        return builder.Matches("^(?=.{8,20}$)(?![_.])(?!.*[_.]{2})[a-zA-Z0-9._]+(?<![_.])$");
    }

    public static IRuleBuilder<T, string> Password<T>(this IRuleBuilder<T, string> builder)
    {
        return builder.Matches("^(?=.*[A-Za-z])(?=.*\\d)[A-Za-z\\d]{8,}$");
    }

    public static IRuleBuilder<T, string> PasswordPlusCapitalLetter<T>(this IRuleBuilder<T, string> builder)
    {
        return builder.Matches("^(?=.*[a-z])(?=.*[A-Z])(?=.*\\d)[a-zA-Z\\d]{8,}$");
    }

    public static IRuleBuilder<T, long> PhoneNumber<T>(this IRuleBuilder<T, long> builder)
    {
        return builder.GreaterThan(9000000000);
    }
    public static IRuleBuilder<T, string> PhoneNumber<T>(this IRuleBuilder<T, string> builder)
    {
        return builder.Matches("^(\\+989|00989|989|09|9)?\\d{9}$");
    }
}

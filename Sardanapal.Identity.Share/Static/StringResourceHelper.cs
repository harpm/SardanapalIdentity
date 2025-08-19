// All copy rights are reserved for Sardanapal Corp. Licensed under the MIT license.

using Sardanapal.Identity.Localization;

namespace Sardanapal.Identity.Share.Static;

public static class StringResourceHelper
{
    public static string CreateNullReferenceEmailOrPhoneNumber(string emailVar, string phoneVar)
    {
        return string.Format(Identity_Exception.NullReferenceEmailOrPhoneNumber, emailVar, phoneVar);
    }
}

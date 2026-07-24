using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Sardanapal.Identity.Share.Static;
using System.Text;
using Xunit;

namespace Sardanapal.Identity.Share.Tests.Unit;

public class ShareConfigurationTests
{
    [Fact]
    public void DI_ConfigureIdentityOptions_Maps_All_Fields_Into_IOptions()
    {
        ServiceCollection services = new ServiceCollection();
        const string connString = "Server=.;Database=db";
        const string redisConn = "redis:6379";
        TokenValidationParameters tokenParams = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidIssuer = "issuer"
        };
        const int expire = 45;
        const int otpLen = 6;
        int? otpLength = otpLen;
        const string seedUser = "root";
        const string seedPass = "Pw1234!";
        const int threshold = 7;

        services.ConfigureIdentityOptions(connString, redisConn, tokenParams, expire, otpLength, seedUser, seedPass, threshold);

        ServiceProvider provider = services.BuildServiceProvider();
        IOptions<SDConfigs> options = provider.GetRequiredService<IOptions<SDConfigs>>();
        SDConfigs cfg = options.Value;

        cfg.DbConnectionString.Should().Be(connString);
        cfg.RedisConnectionString.Should().Be(redisConn);
        cfg.TokenParameters.Should().BeSameAs(tokenParams);
        cfg.ExpirationTime.Should().Be(expire);
        cfg.OTPLength.Should().Be(otpLength);
        cfg.SeedAdminUsername.Should().Be(seedUser);
        cfg.SeedAdminPassword.Should().Be(seedPass);
        cfg.TokenRefreshThresholdMinutes.Should().Be(threshold);
    }

    [Fact]
    public void SDConfigs_Defaults_Are_MaxLoginAttempts_5_LockoutMinutes_15_Threshold_10()
    {
        SDConfigs cfg = new SDConfigs();

        cfg.MaxLoginAttempts.Should().Be(5);
        cfg.LockoutMinutes.Should().Be(15);
        cfg.TokenRefreshThresholdMinutes.Should().Be(10);
    }
}

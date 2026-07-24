using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Sardanapal.Identity.Contract.IService;
using Sardanapal.Identity.Services;
using Sardanapal.Identity.Services.Services.AccountService;
using Sardanapal.Identity.Share.Static;
using Xunit;

namespace Sardanapal.Identity.Services.Tests.Unit;

public class ServicesConfigurationTests
{
    [Fact]
    public void DI_AddSardanapalAccountLockout_Registers_LoginAttemptTracker_As_Singleton()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddSardanapalAccountLockout();
        services.AddSingleton<IOptions<SDConfigs>>(Options.Create(new SDConfigs()));

        ServiceDescriptor? descriptor = services.FirstOrDefault(s => s.ServiceType == typeof(ILoginAttemptTracker));
        descriptor.Should().NotBeNull();
        descriptor!.Lifetime.Should().Be(ServiceLifetime.Singleton,
            "the login attempt tracker must be a singleton so lockout state is shared across requests");
        descriptor.ImplementationType.Should().Be<LoginAttemptTracker>();

        ServiceProvider provider = services.BuildServiceProvider();
        ILoginAttemptTracker first = provider.GetRequiredService<ILoginAttemptTracker>();
        ILoginAttemptTracker second = provider.GetRequiredService<ILoginAttemptTracker>();
        first.Should().BeSameAs(second, "a singleton returns the same instance every time");
    }

    [Fact]
    public void LoginAttemptTracker_Uses_Config_Values_From_IOptions()
    {
        ServiceCollection services = new ServiceCollection();
        services.AddSardanapalAccountLockout();
        services.AddSingleton<IOptions<SDConfigs>>(Options.Create(new SDConfigs
        {
            MaxLoginAttempts = 2,
            LockoutMinutes = 5
        }));

        ServiceProvider provider = services.BuildServiceProvider();
        ILoginAttemptTracker tracker = provider.GetRequiredService<ILoginAttemptTracker>();

        tracker.RecordFailure("k");
        tracker.IsLockedOut("k").Should().BeFalse();
        tracker.RecordFailure("k");
        tracker.IsLockedOut("k").Should().BeTrue("two failures with MaxLoginAttempts=2 must trigger lockout");
        tracker.GetLockoutRemaining("k")!.Value.TotalMinutes.Should().BeLessOrEqualTo(5);
        tracker.GetLockoutRemaining("k")!.Value.TotalMinutes.Should().BeGreaterThan(0);
    }
}

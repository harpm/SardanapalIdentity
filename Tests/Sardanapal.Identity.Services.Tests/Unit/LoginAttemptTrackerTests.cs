using System.Collections;
using System.Reflection;
using FluentAssertions;
using Microsoft.Extensions.Options;
using Sardanapal.Identity.Services.Services.AccountService;
using Sardanapal.Identity.Share.Static;
using Xunit;

namespace Sardanapal.Identity.Services.Tests.Unit;

public class LoginAttemptTrackerTests
{
    private const string Key = "user-1";

    private static LoginAttemptTracker CreateTracker(SDConfigs? config = null)
    {
        IOptions<SDConfigs> options = Options.Create(config ?? new SDConfigs());
        return new LoginAttemptTracker(options);
    }

    private static IDictionary GetStore(LoginAttemptTracker tracker)
    {
        FieldInfo storeField = typeof(LoginAttemptTracker)
            .GetField("_store", BindingFlags.NonPublic | BindingFlags.Instance)!;
        return (IDictionary)storeField.GetValue(tracker)!;
    }

    private static Type AttemptStateType =>
        typeof(LoginAttemptTracker).GetNestedType("AttemptState", BindingFlags.NonPublic)!;

    private static void SetState(LoginAttemptTracker tracker, string key, int failureCount, DateTime? lockoutUntil)
    {
        IDictionary store = GetStore(tracker);
        object state = Activator.CreateInstance(AttemptStateType, failureCount, DateTime.UtcNow, lockoutUntil)!;
        store[key] = state;
    }

    private static int GetFailureCount(LoginAttemptTracker tracker, string key)
    {
        IDictionary store = GetStore(tracker);
        object? state = store[key];
        if (state == null) return 0;
        return (int)state.GetType().GetProperty("FailureCount")!.GetValue(state)!;
    }

    private static DateTime? GetLockoutUntil(LoginAttemptTracker tracker, string key)
    {
        IDictionary store = GetStore(tracker);
        object? state = store[key];
        if (state == null) return null;
        return (DateTime?)state.GetType().GetProperty("LockoutUntil")!.GetValue(state);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void IsLockedOut_Null_Or_Empty_Key_Returns_False(string? key)
    {
        LoginAttemptTracker tracker = CreateTracker();

        bool result = tracker.IsLockedOut(key!);

        result.Should().BeFalse();
    }

    [Fact]
    public void IsLockedOut_Unknown_Key_Returns_False()
    {
        LoginAttemptTracker tracker = CreateTracker();

        bool result = tracker.IsLockedOut("unknown");

        result.Should().BeFalse();
    }

    [Fact]
    public void IsLockedOut_True_When_LockoutUntil_After_Now()
    {
        LoginAttemptTracker tracker = CreateTracker();

        for (int i = 0; i < 5; i++) tracker.RecordFailure(Key);

        tracker.IsLockedOut(Key).Should().BeTrue();
    }

    [Fact]
    public void IsLockedOut_False_When_LockoutUntil_Passed()
    {
        LoginAttemptTracker tracker = CreateTracker();
        SetState(tracker, Key, failureCount: 5, lockoutUntil: DateTime.UtcNow.AddSeconds(-1));

        bool result = tracker.IsLockedOut(Key);

        result.Should().BeFalse();
    }

    [Theory]
    [InlineData(null)]
    [InlineData("")]
    public void GetLockoutRemaining_Null_Or_Empty_Key_Returns_Null(string? key)
    {
        LoginAttemptTracker tracker = CreateTracker();

        TimeSpan? result = tracker.GetLockoutRemaining(key!);

        result.Should().BeNull();
    }

    [Fact]
    public void GetLockoutRemaining_Unknown_Key_Returns_Null()
    {
        LoginAttemptTracker tracker = CreateTracker();

        TimeSpan? result = tracker.GetLockoutRemaining("unknown");

        result.Should().BeNull();
    }

    [Fact]
    public void GetLockoutRemaining_Locked_Returns_Positive_Timespan()
    {
        LoginAttemptTracker tracker = CreateTracker();
        for (int i = 0; i < 5; i++) tracker.RecordFailure(Key);

        TimeSpan? result = tracker.GetLockoutRemaining(Key);

        result.Should().NotBeNull();
        result!.Value.Should().BeGreaterThan(TimeSpan.Zero);
    }

    [Fact]
    public void GetLockoutRemaining_Expired_Returns_Null()
    {
        LoginAttemptTracker tracker = CreateTracker();
        SetState(tracker, Key, failureCount: 5, lockoutUntil: DateTime.UtcNow.AddSeconds(-1));

        TimeSpan? result = tracker.GetLockoutRemaining(Key);

        result.Should().BeNull();
    }

    [Fact]
    public void GetRemainingAttempts_Fresh_Key_Returns_MaxLoginAttempts()
    {
        LoginAttemptTracker tracker = CreateTracker();

        int result = tracker.GetRemainingAttempts(Key);

        result.Should().Be(5);
    }

    [Fact]
    public void GetRemainingAttempts_Decreases_Per_Failure()
    {
        LoginAttemptTracker tracker = CreateTracker();

        tracker.RecordFailure(Key);
        int first = tracker.GetRemainingAttempts(Key);
        tracker.RecordFailure(Key);
        int second = tracker.GetRemainingAttempts(Key);

        first.Should().Be(4);
        second.Should().Be(3);
    }

    [Fact]
    public void GetRemainingAttempts_Purges_After_Lockout_Expiry()
    {
        LoginAttemptTracker tracker = CreateTracker();
        SetState(tracker, Key, failureCount: 5, lockoutUntil: DateTime.UtcNow.AddSeconds(-1));

        int result = tracker.GetRemainingAttempts(Key);

        result.Should().Be(5);
        GetStore(tracker).Contains(Key).Should().BeFalse("expired entry should have been purged");
    }

    [Fact]
    public void RecordFailure_Increments_FailureCount()
    {
        LoginAttemptTracker tracker = CreateTracker();

        tracker.RecordFailure(Key);
        tracker.RecordFailure(Key);
        tracker.RecordFailure(Key);

        GetFailureCount(tracker, Key).Should().Be(3);
    }

    [Fact]
    public void RecordFailure_Triggers_Lockout_At_MaxLoginAttempts()
    {
        LoginAttemptTracker tracker = CreateTracker();

        for (int i = 0; i < 5; i++) tracker.RecordFailure(Key);

        tracker.IsLockedOut(Key).Should().BeTrue();
        DateTime? lockoutUntil = GetLockoutUntil(tracker, Key);
        lockoutUntil.Should().NotBeNull();
        lockoutUntil!.Value.Should().BeAfter(DateTime.UtcNow);
    }

    [Fact]
    public void RecordFailure_Preserves_Existing_LockoutUntil_Below_Threshold()
    {
        LoginAttemptTracker tracker = CreateTracker();
        DateTime existing = DateTime.UtcNow.AddMinutes(30);
        SetState(tracker, Key, failureCount: 2, lockoutUntil: existing);

        tracker.RecordFailure(Key);

        GetFailureCount(tracker, Key).Should().Be(3);
        GetLockoutUntil(tracker, Key).Should().Be(existing);
    }

    [Fact]
    public void RecordSuccess_Clears_State()
    {
        LoginAttemptTracker tracker = CreateTracker();
        tracker.RecordFailure(Key);
        tracker.RecordFailure(Key);

        tracker.RecordSuccess(Key);

        GetStore(tracker).Contains(Key).Should().BeFalse();
        tracker.GetRemainingAttempts(Key).Should().Be(5);
    }

    [Fact]
    public void Reset_Removes_Key()
    {
        LoginAttemptTracker tracker = CreateTracker();
        tracker.RecordFailure(Key);

        tracker.Reset(Key);

        GetStore(tracker).Contains(Key).Should().BeFalse();
    }

    [Fact]
    public void Honors_Config_MaxLoginAttempts_And_LockoutMinutes()
    {
        SDConfigs config = new SDConfigs { MaxLoginAttempts = 3, LockoutMinutes = 7 };
        LoginAttemptTracker tracker = CreateTracker(config);

        for (int i = 0; i < 3; i++) tracker.RecordFailure(Key);

        tracker.IsLockedOut(Key).Should().BeTrue();
        TimeSpan? remaining = tracker.GetLockoutRemaining(Key);
        remaining.Should().NotBeNull();
        remaining!.Value.Should().BeCloseTo(TimeSpan.FromMinutes(7), TimeSpan.FromSeconds(1));
    }

    [Fact]
    public void Null_Config_Falls_Back_To_Defaults()
    {
        LoginAttemptTracker tracker = new LoginAttemptTracker(null!);

        tracker.GetRemainingAttempts(Key).Should().Be(5);

        for (int i = 0; i < 5; i++) tracker.RecordFailure(Key);

        tracker.IsLockedOut(Key).Should().BeTrue();
    }

    [Fact]
    public void RecordFailure_Is_ThreadSafe_Under_Parallel_Load()
    {
        LoginAttemptTracker tracker = CreateTracker();
        const int total = 1000;

        Action act = () => Parallel.For(0, total, _ => tracker.RecordFailure(Key));

        act.Should().NotThrow();
        GetStore(tracker).Contains(Key).Should().BeTrue();
        GetFailureCount(tracker, Key).Should().BePositive();
        tracker.IsLockedOut(Key).Should().BeTrue();
    }

    [Fact]
    public void GetLockoutRemaining_Message_Minutes_Rounded_Up()
    {
        LoginAttemptTracker tracker = CreateTracker();
        for (int i = 0; i < 5; i++) tracker.RecordFailure(Key);

        TimeSpan? remaining = tracker.GetLockoutRemaining(Key);

        remaining.Should().NotBeNull();
        Math.Ceiling(remaining!.Value.TotalMinutes).Should().BeGreaterThanOrEqualTo(1);
    }
}

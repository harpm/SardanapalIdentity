namespace Sardanapal.Identity.Contract.IService;

public interface ILoginAttemptTracker
{
    bool IsLockedOut(string key);

    TimeSpan? GetLockoutRemaining(string key);

    int GetRemainingAttempts(string key);

    void RecordFailure(string key);

    void RecordSuccess(string key);

    void Reset(string key);
}

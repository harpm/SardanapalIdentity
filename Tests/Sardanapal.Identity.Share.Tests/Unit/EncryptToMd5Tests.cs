using System.Reflection;
using FluentAssertions;
using Sardanapal.Identity.Share.Static;
using Xunit;

namespace Sardanapal.Identity.Share.Tests.Unit;

public class EncryptToMd5Tests
{
    [Fact]
    public void EncryptToMd5_Is_Marked_Obsolete()
    {
        MethodInfo? method = typeof(Utilities).GetMethod(nameof(Utilities.EncryptToMd5));

        method.Should().BeDecoratedWith<ObsoleteAttribute>();
    }

    [Fact]
    public async Task EncryptToMd5_Is_Deterministic_For_Same_Input()
    {
        const string input = "hello";
        const string knownMd5 = "5d41402abc4b2a76b9719d911017c592";

#pragma warning disable CS0618 // Obsolete usage is the point of this test
        string first = await Utilities.EncryptToMd5(input);
        string second = await Utilities.EncryptToMd5(input);
#pragma warning restore CS0618

        first.Should().Be(second);
        first.Should().Be(knownMd5);
    }
}

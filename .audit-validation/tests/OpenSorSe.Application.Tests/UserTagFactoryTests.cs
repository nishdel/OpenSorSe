using OpenSorSe.Application.Models;
using OpenSorSe.Application.Tags;

namespace OpenSorSe.Application.Tests;

/// <summary>
/// Verifies bounded deterministic user-tag normalization without filesystem access.
/// </summary>
public sealed class UserTagFactoryTests
{
    /// <summary>Verifies display values are retained while normalized duplicates collapse deterministically.</summary>
    [Fact]
    public void TryCreate_NormalizesAndDeduplicatesUserInput()
    {
        var success = UserTagFactory.TryCreate(
            "file:one",
            ["  Project Alpha  ", "project---alpha", "Ｆｉｎａｎｃｅ"],
            DateTimeOffset.UnixEpoch,
            out var tags,
            out var error);

        Assert.True(success, error);
        Assert.Equal(2, tags.Count);
        Assert.Equal(["project-alpha", "finance"], tags.Select(tag => tag.NormalizedValue));
        Assert.All(tags, tag =>
        {
            Assert.Equal(TagSource.UserApproved, tag.Source);
            Assert.Equal(TagAcceptanceState.Accepted, tag.AcceptanceState);
            Assert.Equal("User", tag.Category);
            Assert.Equal(TimeSpan.Zero, tag.CreatedAtUtc.Offset);
        });
    }

    /// <summary>Verifies invalid values reject the complete operation instead of partially accepting input.</summary>
    [Theory]
    [InlineData("")]
    [InlineData("---")]
    [InlineData("valid\u0001invalid")]
    public void TryCreate_InvalidValue_RejectsCompleteInput(string invalid)
    {
        var success = UserTagFactory.TryCreate(
            "file:one",
            ["valid", invalid],
            DateTimeOffset.UnixEpoch,
            out var tags,
            out var error);

        Assert.False(success);
        Assert.Empty(tags);
        Assert.NotEmpty(error);
    }

    /// <summary>Verifies per-operation input is bounded and timestamps must be portable UTC values.</summary>
    [Fact]
    public void TryCreate_OverLimitOrNonUtc_RejectsInput()
    {
        var tooMany = Enumerable.Range(0, UserTagLimits.MaximumAcceptedTagsPerFile + 1).Select(index => $"tag-{index}").ToArray();

        Assert.False(UserTagFactory.TryCreate("file:one", tooMany, DateTimeOffset.UnixEpoch, out var boundedTags, out _));
        Assert.Empty(boundedTags);
        Assert.False(UserTagFactory.TryCreate("file:one", ["valid"], new DateTimeOffset(2025, 1, 1, 0, 0, 0, TimeSpan.FromHours(1)), out var utcTags, out _));
        Assert.Empty(utcTags);
    }
}

using Xunit;

namespace ScoreParserValidation.Tests;

public sealed class ScoreParserTests
{
    [Fact]
    public void ValidScoreParsesNormally()
    {
        var parser = new ScoreParser();

        Assert.Equal(42, parser.ParseScore("alice,42"));
    }

    [Fact]
    public void MalformedScoreDoesNotCrash()
    {
        var parser = new ScoreParser();

        Assert.Equal(0, parser.ParseScore("alice,not-a-number"));
    }

    [Fact]
    public void NullInputRemainsAnIntentionalValidator()
    {
        var parser = new ScoreParser();

        Assert.Throws<ArgumentNullException>(() => parser.ParseScore(null!));
    }
}

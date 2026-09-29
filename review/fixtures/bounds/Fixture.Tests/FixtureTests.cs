using Xunit;

public sealed class FixtureTests
{
    [Theory]
    [InlineData(0, 10)]
    [InlineData(1, 20)]
    public void ValidReadsArePreserved(int index, int expected) =>
        Assert.Equal(expected, Fixture.Read(new[] { 10, 20 }, 0, index, true));

    [Fact]
    public void InvalidReadsReturnDefault()
    {
        Assert.Equal(0, Fixture.Read(new int[0], 0, 0, true));
        Assert.Equal(0, Fixture.Read(new[] { 10, 20 }, 0, 2, true));
        Assert.Equal(0, Fixture.Read(new[] { 10, 20 }, 0, -1, true));
    }

    [Fact]
    public void NullArrayReturnsDefault() => Assert.Equal(0, Fixture.Read(null!, 0, 0, true));
}
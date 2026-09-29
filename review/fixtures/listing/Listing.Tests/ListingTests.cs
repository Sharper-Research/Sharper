using Xunit;

public sealed class ListingTests
{
    [Fact]
    public void PaperAssumptionReturnsNormally() => Program.ProcessBuffers(6, 0);

    [Fact]
    public void ExistingValidExecutionIsPreserved() => Program.ProcessBuffers(0, 0);

    [Fact]
    public void NegativeStartStopsBeforeInvalidWrite() => Program.ProcessBuffers(1, -1);

    [Fact]
    public void LargeStartStopsBeforeInvalidWrite() => Program.ProcessBuffers(0, 5);
}
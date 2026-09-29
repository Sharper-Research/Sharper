using System.Collections.Generic;
using Sharper.CFGAnalyzer.Analysis;
using Xunit;

namespace Sharper.CFGAnalyzer.Tests;

public class DominatorSetsTests
{
    [Fact]
    public void Constructor_makes_deep_copy_of_input_sets()
    {
        var source = new Dictionary<int, HashSet<int>>
        {
            [1] = new() { 0, 1 },
            [2] = new() { 0, 1, 2 }
        };

        var domSets = new DominatorSets(source);

        source[1].Add(42);
        source[3] = new HashSet<int> { 0, 3 };

        Assert.False(domSets.Dominates(1, 42));
        Assert.False(domSets.ContainsBlock(3));
    }

    [Fact]
    public void GetDominators_returns_snapshot_not_live_internal_collection()
    {
        var source = new Dictionary<int, HashSet<int>>
        {
            [2] = new() { 0, 1, 2 }
        };

        var domSets = new DominatorSets(source);
        var snapshot = domSets.GetDominators(2);

        if (snapshot is HashSet<int> mutable)
            mutable.Add(99);

        Assert.False(domSets.Dominates(2, 99));
    }
}

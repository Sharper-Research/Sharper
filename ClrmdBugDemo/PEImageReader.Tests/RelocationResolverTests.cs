using PEImageReader;

namespace PEImageReader.Tests
{
    [TestClass]
    public class RelocationResolverTests
    {
        // Relocations: [32..35] and [36..39]  (two adjacent intervals)
        // Stored as paired start/end: { 32, 35, 36, 39 }
        private static readonly int[] Relocations = { 32, 35, 36, 39 };

        [TestMethod]
        public void ReadExactlyAtRelocationStart_ReturnsCorrectRange()
        {
            // offset=32 is exactly the start of the first relocation (even index).
            // This exercises Case 4 (beginSearch >= 0, even) — no bug here.
            var resolver = new RelocationResolver(Relocations);
            var (beginRel, endRel, beginRead, endRead) = resolver.Resolve(32, 2);
            Assert.IsTrue(beginRel >= 0, "beginRelocation should be non-negative");
        }

        [TestMethod]
        public void ReadBetweenRelocations_NoExtensionNeeded()
        {
            // Relocations: [10..19], [30..39] → array { 10, 19, 30, 39 }
            // Read at offset=25 length=2 → between relocations. No crash expected.
            var resolver = new RelocationResolver(new[] { 10, 19, 30, 39 });
            var (beginRel, endRel, beginRead, endRead) = resolver.Resolve(25, 2);
            Assert.IsTrue(beginRel >= 0);
        }

        [TestMethod]
        public void ReadStartsInsideRelocation_IndexOutOfRange()
        {
            // Relocations: [10..19], [30..39] → array { 10, 19, 30, 39 }
            //
            // Read at offset=15, length=4 → starts INSIDE the first relocation
            // interval [10..19].  BinarySearch(15) returns negative because 15
            // is not an exact match.
            //
            // The BUGGY code uses the raw negative  beginSearch  as an array index
            // instead of its bitwise complement → IndexOutOfRangeException.
            var resolver = new RelocationResolver(new[] { 10, 19, 30, 39 });
            var (beginRel, endRel, beginRead, endRead) = resolver.Resolve(15, 4);
            Assert.AreEqual(0, beginRel, "beginRelocation should be 0 (first interval start)");
            Assert.AreEqual(10, beginRead, "beginRead should extend to relocation start (10)");
        }

        [TestMethod]
        public void ReadEndsInsideRelocation_IndexOutOfRange()
        {
            // Relocations: [10..19], [30..39] → array { 10, 19, 30, 39 }
            //
            // Read at offset=5, length=13 → ends at offset 17, which is INSIDE
            // the first relocation [10..19].
            //
            // The BUGGY code uses the raw negative  endSearch  as an array index
            // instead of its bitwise complement → IndexOutOfRangeException.
            var resolver = new RelocationResolver(new[] { 10, 19, 30, 39 });
            var (beginRel, endRel, beginRead, endRead) = resolver.Resolve(5, 13);
            Assert.AreEqual(1, endRel, "endRelocation should be 1 (first interval end)");
            Assert.AreEqual(19, endRead, "endRead should extend to relocation end (19)");
        }
    }
}

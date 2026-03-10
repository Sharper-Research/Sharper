// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.
//
// Extracted from microsoft/clrmd – PEImage.DoRead relocation handling
// Bug: https://github.com/microsoft/clrmd/issues/1288
// Fix: https://github.com/microsoft/clrmd/pull/1360
//
// The bug: When Array.BinarySearch returns a negative result (indicating the
// value was not found), the code used the raw negative value (beginSearch /
// endSearch) as an array index instead of its bitwise complement.  This causes
// an IndexOutOfRangeException whenever a read range starts or ends inside a
// relocation interval.

using System;

namespace PEImageReader
{
    /// <summary>
    /// Minimal reproduction of the relocation-range lookup from ClrMD's PEImage.DoRead.
    /// <para>
    /// <c>_relocations</c> is a sorted array of paired offsets: even indices are relocation
    /// start offsets, odd indices are relocation end offsets (inclusive).
    /// </para>
    /// </summary>
    public class RelocationResolver
    {
        private readonly int[] _relocations;

        public RelocationResolver(int[] relocations)
        {
            _relocations = relocations ?? throw new ArgumentNullException(nameof(relocations));
        }

        /// <summary>
        /// Given a read-range [offset .. offset+length-1], computes the extended range that
        /// covers any partially-intersected relocation intervals.
        /// Returns (beginRelocation, endRelocation, beginRead, endRead).
        /// </summary>
        public (int beginRelocation, int endRelocation, int beginRead, int endRead) Resolve(int offset, int length)
        {
            int beginRead = offset;
            int endRead = offset + length - 1;
            int beginRelocation = -1;
            int endRelocation = -1;

            // ---- Begin-side search (BUG is here) ----
            int beginSearch = Array.BinarySearch(_relocations, offset);
            if (beginSearch < 0)
            {
                int beginSearchComplement = ~beginSearch;
                if (beginSearchComplement == _relocations.Length)
                {
                    // Case 1: read starts after all relocations
                    beginRelocation = _relocations.Length;
                }
                else if ((beginSearchComplement & 1) == 0)
                {
                    // Case 2: read starts between relocations
                    beginRelocation = beginSearchComplement;
                }
                else
                {
                    // Case 3: read starts within a relocation – extend the read
                    //
                    // BUG: original code used  beginSearch  (negative!) instead of
                    //      beginSearchComplement.  This causes IndexOutOfRangeException.
                    beginRelocation = beginSearch - 1;          // ← BUGGY LINE
                    beginRead = _relocations[beginRelocation];  // ← crashes here
                }
            }
            else
            {
                if ((beginSearch & 1) == 0)
                {
                    // Case 4: read starts exactly at relocation start
                    beginRelocation = beginSearch;
                }
                else
                {
                    // Case 5: read starts exactly at relocation end
                    beginRelocation = beginSearch - 1;
                    beginRead = _relocations[beginRelocation];
                }
            }

            // ---- End-side search (BUG is here too) ----
            int endSearch = Array.BinarySearch(_relocations, offset + length - 1);
            if (endSearch < 0)
            {
                int endSearchComplement = ~endSearch;
                if (endSearchComplement == _relocations.Length)
                {
                    // Case 1: read ends after all relocations
                    endRelocation = _relocations.Length - 1;
                }
                else if ((endSearchComplement & 1) == 0)
                {
                    // Case 2: read ends between relocations
                    endRelocation = endSearchComplement - 1;
                }
                else
                {
                    // Case 3: read ends within a relocation – extend the read
                    //
                    // BUG: original code used  endSearch  (negative!) instead of
                    //      endSearchComplement.  This causes IndexOutOfRangeException.
                    endRelocation = endSearch;                  // ← BUGGY LINE
                    endRead = _relocations[endRelocation];      // ← crashes here
                }
            }
            else
            {
                if ((endSearch & 1) == 0)
                {
                    // Case 4: read ends exactly at relocation start
                    endRelocation = endSearch + 1;
                    endRead = _relocations[endRelocation];
                }
                else
                {
                    // Case 5: read ends exactly at relocation end
                    endRelocation = endSearch;
                }
            }

            return (beginRelocation, endRelocation, beginRead, endRead);
        }
    }
}

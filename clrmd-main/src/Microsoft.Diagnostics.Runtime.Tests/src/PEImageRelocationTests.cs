// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System;
using System.IO;
using System.Reflection;
using Microsoft.Diagnostics.Runtime.Utilities;
using Xunit;

namespace Microsoft.Diagnostics.Runtime.Tests
{
    /// <summary>
    /// Tests for PEImage.DoRead relocation handling.
    /// Exercises the bug from https://github.com/microsoft/clrmd/issues/1288
    /// where negative BinarySearch results were used as array indices.
    /// </summary>
    public class PEImageRelocationTests : IDisposable
    {
        private readonly string _tempFile;

        public PEImageRelocationTests()
        {
            _tempFile = Path.GetTempFileName();
        }

        public void Dispose()
        {
            try { File.Delete(_tempFile); } catch { }
        }

        /// <summary>
        /// Constructs a minimal 64-bit PE image that contains a single .reloc entry,
        /// triggering DoRead's relocation handling when Read is called with an offset
        /// that falls inside the relocation range.
        /// </summary>
        [Fact]
        public void ReadInsideRelocation_ShouldNotThrowIndexOutOfRange()
        {
            // We construct a minimal PE64 image with:
            //   - DOS stub + PE signature
            //   - COFF header with 2 sections (.text, .reloc)
            //   - PE64 optional header (Magic = 0x020b)
            //   - Two section headers
            //   - .text section with some data
            //   - .reloc section with one base relocation block
            //
            // Then open via PEImage with loadedImageBase != imageBase so _relocations is populated.
            // Finally, call Read() at an offset that falls inside a relocation interval.

            BuildPEWithRelocation(_tempFile,
                imageBase: 0x10000,
                textRva: 0x1000,
                textFileOffset: 0x200,
                textSize: 0x200,
                relocRva: 0x2000,
                relocFileOffset: 0x400,
                relocSize: 0x100,
                relocTargetRva: 0x1080,   // relocation points to middle of .text
                relocType: 10);           // IMAGE_REL_BASED_DIR64

            using FileStream fs = new(_tempFile, FileMode.Open, FileAccess.Read);
            // loadedImageBase = imageBase + 0x10000 → forces _relocations to be populated
            using PEImage image = new(fs, leaveOpen: false, loadedImageBase: 0x20000);

            Assert.True(image.IsValid, "PE should be valid");

            // Read at the virtual address inside the relocation.
            // RVA 0x1080 maps to file offset 0x280 (textFileOffset + 0x80).
            // This is inside the relocation [0x280 .. 0x287], so DoRead Case 3 is hit.
            byte[] buf = new byte[16];
            int bytesRead = image.Read(0x1080, buf);
            // We just need this to not throw IndexOutOfRangeException
            Assert.True(bytesRead >= 0);
        }

        /// <summary>
        /// Read spanning across a relocation — the end of the read falls inside a relocation.
        /// </summary>
        [Fact]
        public void ReadEndInsideRelocation_ShouldNotThrowIndexOutOfRange()
        {
            BuildPEWithRelocation(_tempFile,
                imageBase: 0x10000,
                textRva: 0x1000,
                textFileOffset: 0x200,
                textSize: 0x200,
                relocRva: 0x2000,
                relocFileOffset: 0x400,
                relocSize: 0x100,
                relocTargetRva: 0x1080,
                relocType: 10);

            using FileStream fs = new(_tempFile, FileMode.Open, FileAccess.Read);
            using PEImage image = new(fs, leaveOpen: false, loadedImageBase: 0x20000);

            Assert.True(image.IsValid, "PE should be valid");

            // Read starting before the relocation but ending inside it.
            // RVA 0x1070 → file offset 0x270, reading 20 bytes reaches into [0x280..0x287].
            byte[] buf = new byte[20];
            int bytesRead = image.Read(0x1070, buf);
            Assert.True(bytesRead >= 0);
        }

        private static void BuildPEWithRelocation(
            string path,
            ulong imageBase,
            int textRva,
            int textFileOffset,
            int textSize,
            int relocRva,
            int relocFileOffset,
            int relocSize,
            int relocTargetRva,
            int relocType)
        {
            using FileStream fs = new(path, FileMode.Create, FileAccess.Write);
            using BinaryWriter bw = new(fs);

            int peHeaderStart = 0x80;

            // --- DOS header ---
            bw.Write((ushort)0x5A4D);  // e_magic = MZ
            fs.Position = 0x3C;
            bw.Write(peHeaderStart);    // e_lfanew

            // --- PE Signature ---
            fs.Position = peHeaderStart;
            bw.Write((uint)0x00004550); // "PE\0\0"

            // --- COFF Header (ImageFileHeader) - 20 bytes ---
            int coffStart = peHeaderStart + 4;
            fs.Position = coffStart;
            bw.Write((ushort)0x8664);   // Machine = AMD64
            bw.Write((ushort)2);        // NumberOfSections = 2 (.text, .reloc)
            bw.Write((uint)0);          // TimeDateStamp
            bw.Write((uint)0);          // PointerToSymbolTable
            bw.Write((uint)0);          // NumberOfSymbols

            int optionalHeaderSize = 112 + 16 * 8; // PE64 optional header + 16 data directories
            bw.Write((ushort)optionalHeaderSize);   // SizeOfOptionalHeader
            bw.Write((ushort)0x0022);               // Characteristics (EXECUTABLE_IMAGE | LARGE_ADDRESS_AWARE)

            // --- Optional Header (PE64) ---
            int optStart = coffStart + 20;
            fs.Position = optStart;
            bw.Write((ushort)0x020b);   // Magic = PE32+ (PE64)
            bw.Write((byte)14);         // MajorLinkerVersion
            bw.Write((byte)0);          // MinorLinkerVersion
            bw.Write((uint)textSize);   // SizeOfCode
            bw.Write((uint)0);          // SizeOfInitializedData
            bw.Write((uint)0);          // SizeOfUninitializedData
            bw.Write((uint)textRva);    // AddressOfEntryPoint
            bw.Write((uint)textRva);    // BaseOfCode

            // --- PE64 specific fields ---
            bw.Write(imageBase);        // ImageBase (8 bytes for PE64)
            bw.Write((uint)0x1000);     // SectionAlignment
            bw.Write((uint)0x200);      // FileAlignment
            bw.Write((ushort)6);        // MajorOperatingSystemVersion
            bw.Write((ushort)0);        // MinorOperatingSystemVersion
            bw.Write((ushort)0);        // MajorImageVersion
            bw.Write((ushort)0);        // MinorImageVersion
            bw.Write((ushort)6);        // MajorSubsystemVersion
            bw.Write((ushort)0);        // MinorSubsystemVersion
            bw.Write((uint)0);          // Win32VersionValue
            bw.Write((uint)0x3000);     // SizeOfImage
            bw.Write((uint)0x200);      // SizeOfHeaders
            bw.Write((uint)0);          // CheckSum
            bw.Write((ushort)3);        // Subsystem (WINDOWS_CUI)
            bw.Write((ushort)0);        // DllCharacteristics
            bw.Write((ulong)0x100000);  // SizeOfStackReserve
            bw.Write((ulong)0x1000);    // SizeOfStackCommit
            bw.Write((ulong)0x100000);  // SizeOfHeapReserve
            bw.Write((ulong)0x1000);    // SizeOfHeapCommit
            bw.Write((uint)0);          // LoaderFlags
            bw.Write((uint)16);         // NumberOfRvaAndSizes

            // --- Data directories (16 entries, 8 bytes each) ---
            int dataDirStart = (int)fs.Position;
            // Write 16 empty directories first
            for (int i = 0; i < 16; i++)
            {
                bw.Write((uint)0);  // VirtualAddress
                bw.Write((uint)0);  // Size
            }

            // Patch directory[5] = Relocation directory
            fs.Position = dataDirStart + 5 * 8;
            bw.Write((uint)relocRva);
            bw.Write((uint)relocSize);

            // --- Section Headers (40 bytes each) ---
            int sectionHeaderStart = optStart + optionalHeaderSize;
            fs.Position = sectionHeaderStart;

            // .text section
            bw.Write(new byte[] { 0x2E, 0x74, 0x65, 0x78, 0x74, 0x00, 0x00, 0x00 }); // ".text\0\0\0"
            bw.Write((uint)textSize);           // VirtualSize
            bw.Write((uint)textRva);            // VirtualAddress
            bw.Write((uint)textSize);           // SizeOfRawData
            bw.Write((uint)textFileOffset);     // PointerToRawData
            bw.Write((uint)0);                  // PointerToRelocations
            bw.Write((uint)0);                  // PointerToLinenumbers
            bw.Write((ushort)0);                // NumberOfRelocations
            bw.Write((ushort)0);                // NumberOfLinenumbers
            bw.Write((uint)0x60000020);         // Characteristics (CODE|EXECUTE|READ)

            // .reloc section
            bw.Write(new byte[] { 0x2E, 0x72, 0x65, 0x6C, 0x6F, 0x63, 0x00, 0x00 }); // ".reloc\0\0"
            bw.Write((uint)relocSize);          // VirtualSize
            bw.Write((uint)relocRva);           // VirtualAddress
            bw.Write((uint)relocSize);          // SizeOfRawData
            bw.Write((uint)relocFileOffset);    // PointerToRawData
            bw.Write((uint)0);                  // PointerToRelocations
            bw.Write((uint)0);                  // PointerToLinenumbers
            bw.Write((ushort)0);                // NumberOfRelocations
            bw.Write((ushort)0);                // NumberOfLinenumbers
            bw.Write((uint)0x42000040);         // Characteristics (INITIALIZED_DATA|DISCARDABLE|READ)

            // --- .text section data ---
            fs.Position = textFileOffset;
            for (int i = 0; i < textSize; i++)
                bw.Write((byte)(i & 0xFF));

            // --- .reloc section data ---
            // Base relocation block: pageRVA, blockSize, then array of ushort entries
            fs.Position = relocFileOffset;
            int pageRva = relocTargetRva & ~0xFFF; // Page-align: 0x1000
            int withinPage = relocTargetRva & 0xFFF; // 0x80
            ushort relocEntry = (ushort)((relocType << 12) | withinPage);

            bw.Write((uint)pageRva);            // pageRVA
            bw.Write((uint)(8 + 2));            // blockSize = header(8) + 1 entry(2)
            bw.Write(relocEntry);                // single relocation entry

            // Pad the file to ensure all sections are readable
            fs.Position = relocFileOffset + relocSize - 1;
            bw.Write((byte)0);

            bw.Flush();
        }
    }
}

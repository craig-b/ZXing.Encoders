﻿/*
* Copyright 2008 ZXing authors
*
* Licensed under the Apache License, Version 2.0 (the "License");
* you may not use this file except in compliance with the License.
* You may obtain a copy of the License at
*
*      http://www.apache.org/licenses/LICENSE-2.0
*
* Unless required by applicable law or agreed to in writing, software
* distributed under the License is distributed on an "AS IS" BASIS,
* WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
* See the License for the specific language governing permissions and
* limitations under the License.
*/

using System;

namespace ZXing.Encoders.QrCode
{
    /// <summary>
    /// 
    /// </summary>
    /// <author>
    /// satorux@google.com (Satoru Takabayashi) - creator
    /// </author>
    public static class MatrixUtil
    {
        private static readonly int[][] POSITION_DETECTION_PATTERN = new int[][]
        {
            new int[] {1, 1, 1, 1, 1, 1, 1},
            new int[] {1, 0, 0, 0, 0, 0, 1},
            new int[] {1, 0, 1, 1, 1, 0, 1},
            new int[] {1, 0, 1, 1, 1, 0, 1},
            new int[] {1, 0, 1, 1, 1, 0, 1},
            new int[] {1, 0, 0, 0, 0, 0, 1},
            new int[] {1, 1, 1, 1, 1, 1, 1}
        };

        private static readonly int[][] POSITION_ADJUSTMENT_PATTERN = new int[][]
        {
            new int[] {1, 1, 1, 1, 1},
            new int[] {1, 0, 0, 0, 1},
            new int[] {1, 0, 1, 0, 1},
            new int[] {1, 0, 0, 0, 1},
            new int[] {1, 1, 1, 1, 1}
        };

        // From Appendix E. Table 1, JIS0510X:2004 (p 71). The table was double-checked by komatsu.
        private static readonly int[][] POSITION_ADJUSTMENT_PATTERN_COORDINATE_TABLE = new int[][]
        {
            new int[] {-1, -1, -1, -1, -1, -1, -1},
            new int[] {6, 18, -1, -1, -1, -1, -1},
            new int[] {6, 22, -1, -1, -1, -1, -1},
            new int[] {6, 26, -1, -1, -1, -1, -1},
            new int[] {6, 30, -1, -1, -1, -1, -1},
            new int[] {6, 34, -1, -1, -1, -1, -1},
            new int[] {6, 22, 38, -1, -1, -1, -1},
            new int[] {6, 24, 42, -1, -1, -1, -1},
            new int[] {6, 26, 46, -1, -1, -1, -1},
            new int[] {6, 28, 50, -1, -1, -1, -1},
            new int[] {6, 30, 54, -1, -1, -1, -1},
            new int[] {6, 32, 58, -1, -1, -1, -1},
            new int[] {6, 34, 62, -1, -1, -1, -1},
            new int[] {6, 26, 46, 66, -1, -1, -1},
            new int[] {6, 26, 48, 70, -1, -1, -1},
            new int[] {6, 26, 50, 74, -1, -1, -1},
            new int[] {6, 30, 54, 78, -1, -1, -1},
            new int[] {6, 30, 56, 82, -1, -1, -1},
            new int[] {6, 30, 58, 86, -1, -1, -1},
            new int[] {6, 34, 62, 90, -1, -1, -1},
            new int[] {6, 28, 50, 72, 94, -1, -1},
            new int[] {6, 26, 50, 74, 98, -1, -1},
            new int[] {6, 30, 54, 78, 102, -1, -1},
            new int[] {6, 28, 54, 80, 106, -1, -1},
            new int[] {6, 32, 58, 84, 110, -1, -1},
            new int[] {6, 30, 58, 86, 114, -1, -1},
            new int[] {6, 34, 62, 90, 118, -1, -1},
            new int[] {6, 26, 50, 74, 98, 122, -1},
            new int[] {6, 30, 54, 78, 102, 126, -1},
            new int[] {6, 26, 52, 78, 104, 130, -1},
            new int[] {6, 30, 56, 82, 108, 134, -1},
            new int[] {6, 34, 60, 86, 112, 138, -1},
            new int[] {6, 30, 58, 86, 114, 142, -1},
            new int[] {6, 34, 62, 90, 118, 146, -1},
            new int[] {6, 30, 54, 78, 102, 126, 150},
            new int[] {6, 24, 50, 76, 102, 128, 154},
            new int[] {6, 28, 54, 80, 106, 132, 158},
            new int[] {6, 32, 58, 84, 110, 136, 162},
            new int[] {6, 26, 54, 82, 110, 138, 166},
            new int[] {6, 30, 58, 86, 114, 142, 170}
        };

        // Type info cells at the left top corner.
        private static readonly int[][] TYPE_INFO_COORDINATES = new int[][]
        {
            new int[] {8, 0},
            new int[] {8, 1},
            new int[] {8, 2},
            new int[] {8, 3},
            new int[] {8, 4},
            new int[] {8, 5},
            new int[] {8, 7},
            new int[] {8, 8},
            new int[] {7, 8},
            new int[] {5, 8},
            new int[] {4, 8},
            new int[] {3, 8},
            new int[] {2, 8},
            new int[] {1, 8},
            new int[] {0, 8}
        };

        // From Appendix D in JISX0510:2004 (p. 67)
        private const int VERSION_INFO_POLY = 0x1f25; // 1 1111 0010 0101

        // From Appendix C in JISX0510:2004 (p.65).
        private const int TYPE_INFO_POLY = 0x537;
        private const int TYPE_INFO_MASK_PATTERN = 0x5412;

        /// <summary>
        /// Set all cells to 2.  2 means that the cell is empty (not set yet).
        ///
        /// JAVAPORT: We shouldn't need to do this at all. The code should be rewritten to begin encoding
        /// with the ByteMatrix initialized all to zero.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        public static void ClearMatrix(ByteMatrix matrix) => matrix.Clear(2);

        /// <summary>
        /// Build 2D matrix of QR Code from "dataBits" with "ecLevel", "version" and "getMaskPattern". On
        /// success, store the result in "matrix" and return true.
        /// </summary>
        /// <param name="dataBits">The data bits.</param>
        /// <param name="ecLevel">The ec level.</param>
        /// <param name="version">The version.</param>
        /// <param name="maskPattern">The mask pattern.</param>
        /// <param name="matrix">The matrix.</param>
        public static void BuildMatrix(BitArray dataBits, ErrorCorrectionLevel ecLevel, Version version, int maskPattern, ByteMatrix matrix)
        {
            ClearMatrix(matrix);
            EmbedBasicPatterns(version, matrix);
            // Type information appear with any version.
            EmbedTypeInfo(ecLevel, maskPattern, matrix);
            // Version info appear if version >= 7.
            MaybeEmbedVersionInfo(version, matrix);
            // Data should be embedded at end.
            EmbedDataBits(dataBits, maskPattern, matrix);
        }

        /// <summary>
        /// Embed basic patterns. On success, modify the matrix and return true.
        /// The basic patterns are:
        /// - Position detection patterns
        /// - Timing patterns
        /// - Dark dot at the left bottom corner
        /// - Position adjustment patterns, if need be
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="matrix">The matrix.</param>
        public static void EmbedBasicPatterns(Version version, ByteMatrix matrix)
        {
            // Let's get started with embedding big squares at corners.
            EmbedPositionDetectionPatternsAndSeparators(matrix);
            // Then, embed the dark dot at the left bottom corner.
            EmbedDarkDotAtLeftBottomCorner(matrix);

            // Position adjustment patterns appear if version >= 2.
            MaybeEmbedPositionAdjustmentPatterns(version, matrix);
            // Timing patterns should be embedded after position adj. patterns.
            EmbedTimingPatterns(matrix);
        }

        /// <summary>
        /// Embed type information. On success, modify the matrix.
        /// </summary>
        /// <param name="ecLevel">The ec level.</param>
        /// <param name="maskPattern">The mask pattern.</param>
        /// <param name="matrix">The matrix.</param>
        public static void EmbedTypeInfo(ErrorCorrectionLevel ecLevel, int maskPattern, ByteMatrix matrix)
        {
            var typeInfoBits = new BitArray();
            MakeTypeInfoBits(ecLevel, maskPattern, typeInfoBits);

            for (int i = 0; i < typeInfoBits.Size; ++i)
            {
                // Place bits in LSB to MSB order.  LSB (least significant bit) is the last value in
                // "typeInfoBits".
                var bit = typeInfoBits[typeInfoBits.Size - 1 - i] ? 1 : 0;

                // Type info bits at the left top corner. See 8.9 of JISX0510:2004 (p.46).
                var coordinates = TYPE_INFO_COORDINATES[i];
                var x1 = coordinates[0];
                var y1 = coordinates[1];
                matrix[x1, y1] = bit;

                if (i < 8)
                {
                    // Right top corner.
                    var x2 = matrix.Width - i - 1;
                    const int y2 = 8;
                    matrix[x2, y2] = bit;
                }
                else
                {
                    // Left bottom corner.
                    const int x2 = 8;
                    var y2 = matrix.Height - 7 + (i - 8);
                    matrix[x2, y2] = bit;
                }
            }
        }

        /// <summary>
        /// Embed version information if need be. On success, modify the matrix and return true.
        /// See 8.10 of JISX0510:2004 (p.47) for how to embed version information.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="matrix">The matrix.</param>
        public static void MaybeEmbedVersionInfo(Version version, ByteMatrix matrix)
        {
            if (version.VersionNumber < 7)
            {
                // Version info is necessary if version >= 7.
                return; // Don't need version info.
            }
            var versionInfoBits = new BitArray();
            MakeVersionInfoBits(version, versionInfoBits);

            var bitIndex = (6 * 3) - 1; // It will decrease from 17 to 0.
            for (int i = 0; i < 6; ++i)
            {
                for (int j = 0; j < 3; ++j)
                {
                    // Place bits in LSB (least significant bit) to MSB order.
                    var bit = versionInfoBits[bitIndex] ? 1 : 0;
                    bitIndex--;
                    // Left bottom corner.
                    matrix[i, matrix.Height - 11 + j] = bit;
                    // Right bottom corner.
                    matrix[matrix.Height - 11 + j, i] = bit;
                }
            }
        }

        /// <summary>
        /// Embed "dataBits" using "getMaskPattern". On success, modify the matrix and return true.
        /// For debugging purposes, it skips masking process if "getMaskPattern" is -1.
        /// See 8.7 of JISX0510:2004 (p.38) for how to embed data bits.
        /// </summary>
        /// <param name="dataBits">The data bits.</param>
        /// <param name="maskPattern">The mask pattern.</param>
        /// <param name="matrix">The matrix.</param>
        public static void EmbedDataBits(BitArray dataBits, int maskPattern, ByteMatrix matrix)
        {
            var bitIndex = 0;
            var direction = -1;
            // Start from the right bottom cell.
            var x = matrix.Width - 1;
            var y = matrix.Height - 1;
            while (x > 0)
            {
                // Skip the vertical timing pattern.
                if (x == 6)
                {
                    x--;
                }
                while (y >= 0 && y < matrix.Height)
                {
                    for (int i = 0; i < 2; ++i)
                    {
                        var xx = x - i;
                        // Skip the cell if it's not empty.
                        if (!IsEmpty(matrix[xx, y]))
                        {
                            continue;
                        }
                        int bit;
                        if (bitIndex < dataBits.Size)
                        {
                            bit = dataBits[bitIndex] ? 1 : 0;
                            ++bitIndex;
                        }
                        else
                        {
                            // Padding bit. If there is no bit left, we'll fill the left cells with 0, as described
                            // in 8.4.9 of JISX0510:2004 (p. 24).
                            bit = 0;
                        }

                        // Skip masking if mask_pattern is -1.
                        if (maskPattern != -1)
                        {
                            if (MaskUtil.GetDataMaskBit(maskPattern, xx, y))
                            {
                                bit ^= 0x1;
                            }
                        }
                        matrix[xx, y] = bit;
                    }
                    y += direction;
                }
                direction = -direction; // Reverse the direction.
                y += direction;
                x -= 2; // Move to the left.
            }
            // All bits should be consumed.
            if (bitIndex != dataBits.Size)
            {
                throw new WriterException($"Not all bits consumed: {bitIndex}/{dataBits.Size}");
            }
        }

        /// <summary>
        /// Return the position of the most significant bit set (to one) in the "value". The most
        /// significant bit is position 32. If there is no bit set, return 0. Examples:
        /// - findMSBSet(0) => 0
        /// - findMSBSet(1) => 1
        /// - findMSBSet(255) => 8
        /// </summary>
        /// <param name="value_Renamed">The value_ renamed.</param>
        /// <returns></returns>
        public static int FindMSBSet(int value_Renamed)
        {
            var numDigits = 0;
            while (value_Renamed != 0)
            {
                value_Renamed = (int)((uint)value_Renamed >> 1);
                ++numDigits;
            }
            return numDigits;
        }

        /// <summary>
        /// Calculate BCH (Bose-Chaudhuri-Hocquenghem) code for "value" using polynomial "poly". The BCH
        /// code is used for encoding type information and version information.
        /// Example: Calculation of version information of 7.
        /// f(x) is created from 7.
        ///   - 7 = 000111 in 6 bits
        ///   - f(x) = x^2 + x^2 + x^1
        /// g(x) is given by the standard (p. 67)
        ///   - g(x) = x^12 + x^11 + x^10 + x^9 + x^8 + x^5 + x^2 + 1
        /// Multiply f(x) by x^(18 - 6)
        ///   - f'(x) = f(x) * x^(18 - 6)
        ///   - f'(x) = x^14 + x^13 + x^12
        /// Calculate the remainder of f'(x) / g(x)
        ///         x^2
        ///         __________________________________________________
        ///   g(x) )x^14 + x^13 + x^12
        ///         x^14 + x^13 + x^12 + x^11 + x^10 + x^7 + x^4 + x^2
        ///         --------------------------------------------------
        ///                              x^11 + x^10 + x^7 + x^4 + x^2
        ///
        /// The remainder is x^11 + x^10 + x^7 + x^4 + x^2
        /// Encode it in binary: 110010010100
        /// The return value is 0xc94 (1100 1001 0100)
        ///
        /// Since all coefficients in the polynomials are 1 or 0, we can do the calculation by bit
        /// operations. We don't care if coefficients are positive or negative.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <param name="poly">The poly.</param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"><paramref name="poly"/></exception>
        public static int CalculateBCHCode(int value, int poly)
        {
            if (poly == 0)
                throw new ArgumentException("0 polynominal", nameof(poly));

            // If poly is "1 1111 0010 0101" (version info poly), msbSetInPoly is 13. We'll subtract 1
            // from 13 to make it 12.
            var msbSetInPoly = FindMSBSet(poly);
            value <<= msbSetInPoly - 1;
            // Do the division business using exclusive-or operations.
            while (FindMSBSet(value) >= msbSetInPoly)
            {
                value ^= poly << (FindMSBSet(value) - msbSetInPoly);
            }
            // Now the "value" is the remainder (i.e. the BCH code)
            return value;
        }

        /// <summary>
        /// Make bit vector of type information. On success, store the result in "bits" and return true.
        /// Encode error correction level and mask pattern. See 8.9 of
        /// JISX0510:2004 (p.45) for details.
        /// </summary>
        /// <param name="ecLevel">The ec level.</param>
        /// <param name="maskPattern">The mask pattern.</param>
        /// <param name="bits">The bits.</param>
        /// <exception cref="WriterException"></exception>
        public static void MakeTypeInfoBits(ErrorCorrectionLevel ecLevel, int maskPattern, BitArray bits)
        {
            bool IsQrCodeMaskPatternValid(int maskPattern)
            {
                return maskPattern >= 0 && maskPattern < 8; //NUM_MASK_PATTERNS;
            }

            if (!IsQrCodeMaskPatternValid(maskPattern))
            {
                throw new WriterException("Invalid mask pattern");
            }
            var typeInfo = (ecLevel.Bits << 3) | maskPattern;
            bits.AppendBits(typeInfo, 5);

            var bchCode = CalculateBCHCode(typeInfo, TYPE_INFO_POLY);
            bits.AppendBits(bchCode, 10);

            var maskBits = new BitArray();
            maskBits.AppendBits(TYPE_INFO_MASK_PATTERN, 15);
            bits.Xor(maskBits);

            if (bits.Size != 15)
            {
                // Just in case.
                throw new WriterException($"Should not happen but we got: {bits.Size}");
            }
        }

        /// <summary>
        /// Make bit vector of version information. On success, store the result in "bits" and return true.
        /// See 8.10 of JISX0510:2004 (p.45) for details.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="bits">The bits.</param>
        /// <exception cref="WriterException"></exception>
        public static void MakeVersionInfoBits(Version version, BitArray bits)
        {
            bits.AppendBits(version.VersionNumber, 6);
            var bchCode = CalculateBCHCode(version.VersionNumber, VERSION_INFO_POLY);
            bits.AppendBits(bchCode, 12);

            if (bits.Size != 18)
            {
                // Just in case.
                throw new WriterException($"Should not happen but we got: {bits.Size}");
            }
        }

        /// <summary>
        /// Check if "value" is empty.
        /// </summary>
        /// <param name="value">The value.</param>
        /// <returns>
        ///   <c>true</c> if the specified value is empty; otherwise, <c>false</c>.
        /// </returns>
        private static bool IsEmpty(int value) => value == 2;

        private static void EmbedTimingPatterns(ByteMatrix matrix)
        {
            // -8 is for skipping position detection patterns (size 7), and two horizontal/vertical
            // separation patterns (size 1). Thus, 8 = 7 + 1.
            for (int i = 8; i < matrix.Width - 8; ++i)
            {
                var bit = (i + 1) % 2;
                // Horizontal line.
                if (IsEmpty(matrix[i, 6]))
                {
                    matrix[i, 6] = bit;
                }
                // Vertical line.
                if (IsEmpty(matrix[6, i]))
                {
                    matrix[6, i] = bit;
                }
            }
        }

        /// <summary>
        /// Embed the lonely dark dot at left bottom corner. JISX0510:2004 (p.46)
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        /// <exception cref="WriterException"></exception>
        private static void EmbedDarkDotAtLeftBottomCorner(ByteMatrix matrix)
        {
            if (matrix[8, matrix.Height - 8] == 0)
            {
                throw new WriterException();
            }
            matrix[8, matrix.Height - 8] = 1;
        }

        private static void EmbedHorizontalSeparationPattern(int xStart, int yStart, ByteMatrix matrix)
        {
            for (int x = 0; x < 8; ++x)
            {
                if (!IsEmpty(matrix[xStart + x, yStart]))
                {
                    throw new WriterException();
                }
                matrix[xStart + x, yStart] = 0;
            }
        }

        private static void EmbedVerticalSeparationPattern(int xStart, int yStart, ByteMatrix matrix)
        {
            for (int y = 0; y < 7; ++y)
            {
                if (!IsEmpty(matrix[xStart, yStart + y]))
                {
                    throw new WriterException();
                }
                matrix[xStart, yStart + y] = 0;
            }
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="xStart">The x start.</param>
        /// <param name="yStart">The y start.</param>
        /// <param name="matrix">The matrix.</param>
        private static void EmbedPositionAdjustmentPattern(int xStart, int yStart, ByteMatrix matrix)
        {
            for (int y = 0; y < 5; ++y)
            {
                var patternY = POSITION_ADJUSTMENT_PATTERN[y];
                for (int x = 0; x < 5; ++x)
                {
                    matrix[xStart + x, yStart + y] = patternY[x];
                }
            }
        }

        private static void EmbedPositionDetectionPattern(int xStart, int yStart, ByteMatrix matrix)
        {
            for (int y = 0; y < 7; ++y)
            {
                var patternY = POSITION_DETECTION_PATTERN[y];
                for (int x = 0; x < 7; ++x)
                {
                    matrix[xStart + x, yStart + y] = patternY[x];
                }
            }
        }

        /// <summary>
        /// Embed position detection patterns and surrounding vertical/horizontal separators.
        /// </summary>
        /// <param name="matrix">The matrix.</param>
        private static void EmbedPositionDetectionPatternsAndSeparators(ByteMatrix matrix)
        {
            // Embed three big squares at corners.
            var pdpWidth = POSITION_DETECTION_PATTERN[0].Length;
            // Left top corner.
            EmbedPositionDetectionPattern(0, 0, matrix);
            // Right top corner.
            EmbedPositionDetectionPattern(matrix.Width - pdpWidth, 0, matrix);
            // Left bottom corner.
            EmbedPositionDetectionPattern(0, matrix.Width - pdpWidth, matrix);

            // Embed horizontal separation patterns around the squares.
            const int hspWidth = 8;
            // Left top corner.
            EmbedHorizontalSeparationPattern(0, hspWidth - 1, matrix);
            // Right top corner.
            EmbedHorizontalSeparationPattern(matrix.Width - hspWidth, hspWidth - 1, matrix);
            // Left bottom corner.
            EmbedHorizontalSeparationPattern(0, matrix.Width - hspWidth, matrix);

            // Embed vertical separation patterns around the squares.
            const int vspSize = 7;
            // Left top corner.
            EmbedVerticalSeparationPattern(vspSize, 0, matrix);
            // Right top corner.
            EmbedVerticalSeparationPattern(matrix.Height - vspSize - 1, 0, matrix);
            // Left bottom corner.
            EmbedVerticalSeparationPattern(vspSize, matrix.Height - vspSize, matrix);
        }

        /// <summary>
        /// Embed position adjustment patterns if need be.
        /// </summary>
        /// <param name="version">The version.</param>
        /// <param name="matrix">The matrix.</param>
        private static void MaybeEmbedPositionAdjustmentPatterns(Version version, ByteMatrix matrix)
        {
            if (version.VersionNumber < 2)
            {
                // The patterns appear if version >= 2
                return;
            }
            var index = version.VersionNumber - 1;
            var coordinates = POSITION_ADJUSTMENT_PATTERN_COORDINATE_TABLE[index];
            foreach (var y in coordinates)
            {
                if (y >= 0)
                {
                    foreach (var x in coordinates)
                    {
                        if (x >= 0 && IsEmpty(matrix[x, y]))
                        {
                            // If the cell is unset, we embed the position adjustment pattern here.
                            // -2 is necessary since the x/y coordinates point to the center of the pattern, not the
                            // left top corner.
                            EmbedPositionAdjustmentPattern(x - 2, y - 2, matrix);
                        }
                    }
                }
            }
        }
    }
}
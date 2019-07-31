/*
 * Copyright 2009 ZXing authors
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

namespace ZXing.Encoders.OneD
{
    /// <summary>
    ///   <p>Encapsulates functionality and implementation that is common to UPC and EAN families
    /// of one-dimensional barcodes.</p>
    ///   <author>aripollak@gmail.com (Ari Pollak)</author>
    ///   <author>dsbnatut@gmail.com (Kazuki Nishiura)</author>
    /// </summary>
    public abstract class UpcEanEncoder : OneDimensionalCodeEncoder
    {
        public static string GetVersionNo()
        {
#if NETSTANDARD2_0
            return "NETSTANDARD2_0";
#elif NETSTANDARD1_0
            return "NETSTANDARD1_0";
#elif NETCOREAPP3_0
            return "NETCOREAPP3_0";
#else
            return "unknown";
#endif
        }

        /// <summary>
        /// Start/end guard pattern.
        /// </summary>
        internal static int[] START_END_PATTERN = { 1, 1, 1, };

        /// <summary>
        /// Pattern marking the middle of a UPC/EAN pattern, separating the two halves.
        /// </summary>
        internal static int[] MIDDLE_PATTERN = { 1, 1, 1, 1, 1 };

        /// <summary>
        /// end guard pattern.
        /// </summary>
        internal static int[] END_PATTERN = { 1, 1, 1, 1, 1, 1 };

        /// <summary>
        /// "Odd", or "L" patterns used to encode UPC/EAN digits.
        /// </summary>
        internal static int[][] L_PATTERNS = {
            new[] {3, 2, 1, 1}, // 0
            new[] {2, 2, 2, 1}, // 1
            new[] {2, 1, 2, 2}, // 2
            new[] {1, 4, 1, 1}, // 3
            new[] {1, 1, 3, 2}, // 4
            new[] {1, 2, 3, 1}, // 5
            new[] {1, 1, 1, 4}, // 6
            new[] {1, 3, 1, 2}, // 7
            new[] {1, 2, 1, 3}, // 8
            new[] {3, 1, 1, 2} // 9
        };

        /// <summary>
        /// As above but also including the "even", or "G" patterns used to encode UPC/EAN digits.
        /// </summary>
        internal static int[][] L_AND_G_PATTERNS;

        static UpcEanEncoder()
        {
            {
                L_AND_G_PATTERNS = new int[20][];
                Array.Copy(L_PATTERNS, 0, L_AND_G_PATTERNS, 0, 10);
                for (int i = 10; i < 20; i++)
                {
                    var widths = L_PATTERNS[i - 10];
                    var reversedWidths = new int[widths.Length];
                    for (int j = 0; j < widths.Length; j++)
                    {
                        reversedWidths[j] = widths[widths.Length - j - 1];
                    }
                    L_AND_G_PATTERNS[i] = reversedWidths;
                }
            }
        }

        /// <summary>
        /// Gets the default margin.
        /// </summary>
        public override int DefaultMargin =>
                // Use a different default more appropriate for UPC/EAN
                9;

        /// <summary>
        /// Computes the UPC/EAN checksum on a string of digits, and reports
        /// whether the checksum is correct or not.
        /// </summary>
        /// <param name="contents">string of digits to check</param>
        /// <returns>true iff string of digits passes the UPC/EAN checksum algorithm</returns>
#if NETCOREAPP3_0
        internal static bool CheckStandardUPCEANChecksum(ReadOnlySpan<char> contents)
#else
        internal static bool CheckStandardUPCEANChecksum(string contents)
#endif
        {
            var length = contents.Length;
            if (length == 0)
            {
                return false;
            }

            var check = contents[length - 1] - '0';
#if NETCOREAPP3_0
            return GetStandardUPCEANChecksum(contents.Slice(0, length - 1)) == check;
#else
            return GetStandardUPCEANChecksum(contents.Substring(0, length - 1)) == check;
#endif
        }

#if NETCOREAPP3_0
        internal static int? GetStandardUPCEANChecksum(ReadOnlySpan<char> contents)
#else
        internal static int? GetStandardUPCEANChecksum(string contents)
#endif
        {
            var length = contents.Length;
            var sum = 0;
            for (int i = length - 1; i >= 0; i -= 2)
            {
                var digit = contents[i] - '0';
                if (digit < 0 || digit > 9)
                {
                    throw new ArgumentException($"Contents should only contain digits, but got '{contents[i]}'", nameof(contents));
                }
                sum += digit;
            }
            sum *= 3;
            for (int i = length - 2; i >= 0; i -= 2)
            {
                var digit = contents[i] - '0';
                if (digit < 0 || digit > 9)
                {
                    throw new ArgumentException($"Contents should only contain digits, but got '{contents[i]}'", nameof(contents));
                }
                sum += digit;
            }
            return (1000 - sum) % 10;
        }
    }
}
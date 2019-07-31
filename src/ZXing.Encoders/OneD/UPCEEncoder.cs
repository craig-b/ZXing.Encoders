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
using System.Collections.Generic;
using System.Text;

namespace ZXing.Encoders.OneD
{
    /// <summary>
    /// This object renders an UPC-E code as a <see cref="BitMatrix"/>.
    /// @author 0979097955s@gmail.com (RX)
    /// </summary>
    public class UPCEEncoder : UpcEanEncoder
    {
        /// <summary>
        /// The pattern that marks the middle, and end, of a UPC-E pattern.
        /// There is no "second half" to a UPC-E barcode.
        /// </summary>
        private static readonly int[] MIDDLE_END_PATTERN = { 1, 1, 1, 1, 1, 1 };

        // For an UPC-E barcode, the final digit is represented by the parities used
        // to encode the middle six digits, according to the table below.
        //
        //                Parity of next 6 digits
        //    Digit   0     1     2     3     4     5
        //       0    Even   Even  Even Odd  Odd   Odd
        //       1    Even   Even  Odd  Even Odd   Odd
        //       2    Even   Even  Odd  Odd  Even  Odd
        //       3    Even   Even  Odd  Odd  Odd   Even
        //       4    Even   Odd   Even Even Odd   Odd
        //       5    Even   Odd   Odd  Even Even  Odd
        //       6    Even   Odd   Odd  Odd  Even  Even
        //       7    Even   Odd   Even Odd  Even  Odd
        //       8    Even   Odd   Even Odd  Odd   Even
        //       9    Even   Odd   Odd  Even Odd   Even
        //
        // The encoding is represented by the following array, which is a bit pattern
        // using Odd = 0 and Even = 1. For example, 5 is represented by:
        //
        //              Odd Even Even Odd Odd Even
        // in binary:
        //                0    1    1   0   0    1   == 0x19
        //

        /// <summary>
        /// See L_AND_G_PATTERNS these values similarly represent patterns of
        /// even-odd parity encodings of digits that imply both the number system (0 or 1)
        /// used, and the check digit.
        /// </summary>
        internal static readonly int[][] NUMSYS_AND_CHECK_DIGIT_PATTERNS = {
            new[] { 0x38, 0x34, 0x32, 0x31, 0x2C, 0x26, 0x23, 0x2A, 0x29, 0x25 },
            new[] { 0x07, 0x0B, 0x0D, 0x0E, 0x13, 0x19, 0x1C, 0x15, 0x16, 0x1A }
        };

        private const int CODE_WIDTH = 3 + // start guard
                                       (7 * 6) + // bars
                                       6; // end guard

        public override BitMatrix Encode(string contents, BarcodeFormat format, int width, int height, IDictionary<EncodeHintType, object> hints)
        {
            if (format != BarcodeFormat.UPC_E)
            {
                throw new ArgumentException($"Can only encode UPC_E, but got {format}", nameof(format));
            }

            return base.Encode(contents, format, width, height, hints);
        }

        public override bool[] Encode(string contents)
        {
            var length = contents.Length;
            switch (length)
            {
                case 7:
                    // No check digit present, calculate it and add it
                    var check = UpcEanEncoder.GetStandardUPCEANChecksum(ConvertUPCEtoUPCA(contents));
                    if (check == null)
                    {
                        throw new ArgumentException("Checksum can't be calculated", nameof(contents));
                    }
                    contents += check.Value;
                    break;
                case 8:
                    try
                    {
                        if (!CheckStandardUPCEANChecksum(contents))
                        {
                            throw new ArgumentException("Contents do not pass checksum", nameof(contents));
                        }
                    }
                    catch (FormatException ignored)
                    {
                        throw new ArgumentException("Illegal contents", nameof(contents), ignored);
                    }
                    break;
                default:
                    throw new ArgumentException($"Requested contents should be 7 or 8 digits long, but got {length}", nameof(contents));
            }

            CheckNumeric(contents);

            var firstDigit = contents[0] - '0';
            if (firstDigit != 0 && firstDigit != 1)
            {
                throw new ArgumentException("Number system must be 0 or 1", nameof(contents));
            }

            var checkDigit = contents[7] - '0';
            var parities = NUMSYS_AND_CHECK_DIGIT_PATTERNS[firstDigit][checkDigit];
            var result = new bool[CODE_WIDTH];
            var pos = 0;

            pos += AppendPattern(result, pos, START_END_PATTERN, true);

            for (var i = 1; i <= 6; i++)
            {
                var digit = contents[i] - '0';
                if ((parities >> (6 - i) & 1) == 1)
                {
                    digit += 10;
                }
                pos += AppendPattern(result, pos, L_AND_G_PATTERNS[digit], false);
            }

            AppendPattern(result, pos, END_PATTERN, false);

            return result;
        }

        /// <summary>
        /// Expands a UPC-E value back into its full, equivalent UPC-A code value.
        ///
        /// <param name="upce">UPC-E code as string of digits</param>
        /// <returns>equivalent UPC-A code as string of digits</returns>
        /// </summary>
#if NETCOREAPP3_0
            public static string ConvertUPCEtoUPCA(ReadOnlySpan<char> upce)
#else
        public static string ConvertUPCEtoUPCA(string upce)
#endif
        {
#if NETCOREAPP3_0
            var upceChars = upce.Slice(1, 6);
#else
            var upceChars = upce.Substring(1, 6);
#endif
            var result = new StringBuilder(12);
            result.Append(upce[0]);
            var lastChar = upceChars[5];
            switch (lastChar)
            {
                case '0':
                case '1':
                case '2':
#if NETCOREAPP3_0
                    result.Append(upceChars.Slice(0, 2));
#else
                    result.Append(upceChars, 0, 2);
#endif
                    result.Append(lastChar);
                    result.Append("0000");
#if NETCOREAPP3_0
                    result.Append(upceChars.Slice(2, 3));
#else
                    result.Append(upceChars, 2, 3);
#endif
                    break;
                case '3':
#if NETCOREAPP3_0
                    result.Append(upceChars.Slice(0, 3));
#else
                    result.Append(upceChars, 0, 3);
#endif
                    result.Append("00000");
#if NETCOREAPP3_0
                    result.Append(upceChars.Slice(3, 2));
#else
                    result.Append(upceChars, 3, 2);
#endif
                    break;
                case '4':
#if NETCOREAPP3_0
                    result.Append(upceChars.Slice(0, 4));
#else
                    result.Append(upceChars, 0, 4);
#endif
                    result.Append("00000");
                    result.Append(upceChars[4]);
                    break;
                default:
#if NETCOREAPP3_0
                    result.Append(upceChars.Slice(0, 5));
#else
                    result.Append(upceChars, 0, 5);
#endif
                    result.Append("0000");
                    result.Append(lastChar);
                    break;
            }
            // Only append check digit in conversion if supplied
            if (upce.Length >= 8)
            {
                result.Append(upce[7]);
            }
            return result.ToString();
        }

        public override BitMatrix Encode(string contents, int width, int height) => Encode(contents, BarcodeFormat.UPC_E, width, height);
        public override BitMatrix Encode(string contents, int width, int height, IDictionary<EncodeHintType, object> hints) => Encode(contents, BarcodeFormat.UPC_E, width, height, hints);
    }
}
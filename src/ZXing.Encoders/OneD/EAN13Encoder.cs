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

namespace ZXing.Encoders.OneD
{
    /// <summary>
    /// This object renders an EAN13 code as a <see cref="BitMatrix"/>.
    /// <author>aripollak@gmail.com (Ari Pollak)</author>
    /// </summary>
    public sealed class EAN13Encoder : UpcEanEncoder
    {
        internal static int[] FIRST_DIGIT_ENCODINGS = { 0x00, 0x0B, 0x0D, 0xE, 0x13, 0x19, 0x1C, 0x15, 0x16, 0x1A };

        private const int CODE_WIDTH = 3 + // start guard
            (7 * 6) + // left bars
            5 + // middle guard
            (7 * 6) + // right bars
            3; // end guard

        /// <summary>
        /// Encode the contents following specified format.
        /// <paramref name="width"/> and <paramref name="height"/> are required size. This method may return bigger size
        /// <see cref="BitMatrix"/> when specified size is too small. The user can set both <paramref name="width"/> and
        /// <paramref name="height"/> to zero to get minimum size barcode. If negative value is set to <paramref name="width"/>
        /// or <paramref name="height"/>, <see cref="ArgumentOutOfRangeException"/> is thrown.
        /// </summary>
        /// <param name="contents"></param>
        /// <param name="format"></param>
        /// <param name="width"></param>
        /// <param name="height"></param>
        /// <param name="hints"></param>
        /// <returns></returns>
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentOutOfRangeException"></exception>
        public override BitMatrix Encode(string contents, BarcodeFormat format, int width, int height, IDictionary<EncodeHintType, object> hints)
        {
            if (format != BarcodeFormat.EAN_13)
            {
                throw new ArgumentException($"Can only encode EAN_13, but got {format}", nameof(format));
            }

            return base.Encode(contents, format, width, height, hints);
        }

        /// <summary>
        /// Encode the contents to byte array expression of one-dimensional barcode.
        /// Start code and end code should be included in result, and side margins should not be included.
        /// <returns>a <see cref="bool[]"/> of horizontal pixels (false = white, true = black)</returns>
        /// </summary>
        /// <param name="contents"></param>
        /// <returns></returns>
        public override bool[] Encode(string contents)
        {
            switch (contents.Length)
            {
                case 12:
                    // No check digit present, calculate it and add it
                    var check = GetStandardUPCEANChecksum(contents);
                    if (check == null)
                    {
                        throw new ArgumentException("Checksum can't be calculated", nameof(contents));
                    }
                    contents += check.Value;
                    break;
                case 13:
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
                    throw new ArgumentException($"Requested contents should be 12 (without checksum digit) or 13 digits long, but got {contents.Length}", nameof(contents));
            }

            CheckNumeric(contents);

            var firstDigit = contents[0] - '0';
            var parities = FIRST_DIGIT_ENCODINGS[firstDigit];
            var result = new bool[CODE_WIDTH];
            var pos = 0;

            pos += AppendPattern(result, pos, START_END_PATTERN, true);

            // See EAN13Reader for a description of how the first digit & left bars are encoded
            for (int i = 1; i <= 6; i++)
            {
                var digit = contents[i] - '0';
                if ((parities >> (6 - i) & 1) == 1)
                {
                    digit += 10;
                }
                pos += AppendPattern(result, pos, L_AND_G_PATTERNS[digit], false);
            }

            pos += AppendPattern(result, pos, MIDDLE_PATTERN, false);

            for (int i = 7; i <= 12; i++)
            {
                var digit = contents[i] - '0';
                pos += AppendPattern(result, pos, L_PATTERNS[digit], true);
            }
            AppendPattern(result, pos, START_END_PATTERN, true);

            return result;
        }

        public override BitMatrix Encode(string contents, int width, int height) => Encode(contents, BarcodeFormat.EAN_13, width, height);
        public override BitMatrix Encode(string contents, int width, int height, IDictionary<EncodeHintType, object> hints) => Encode(contents, BarcodeFormat.EAN_13, width, height, hints);
    }
}

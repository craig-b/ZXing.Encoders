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
    /// This object renders an EAN8 code as a <see cref="BitMatrix"/>.
    /// <author>aripollak@gmail.com (Ari Pollak)</author>
    /// </summary>
    public sealed class EAN8Encoder : UpcEanEncoder
    {
        private const int CODE_WIDTH = 3 + // start guard
            (7 * 4) + // left bars
            5 + // middle guard
            (7 * 4) + // right bars
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
        public override BitMatrix Encode(string contents, BarcodeFormat format, int width, int height, IDictionary<EncodeHintType, object> hints)
        {
            if (format != BarcodeFormat.EAN_8)
            {
                throw new ArgumentException($"Can only encode EAN_8, but got {format}", nameof(format));
            }

            return base.Encode(contents, format, width, height, hints);
        }

        /// <summary>
        /// </summary>
        /// <returns>
        /// a byte array of horizontal pixels (false = white, true = black)
        /// </returns>
        /// <exception cref="ArgumentException"></exception>
        public override bool[] Encode(string contents)
        {
            switch (contents.Length)
            {
                case 7:
                    // No check digit present, calculate it and add it
                    var check = GetStandardUPCEANChecksum(contents);
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
                    throw new ArgumentException($"Requested contents should be 7 (without checksum digit) or 8 digits long, but got {contents.Length}", nameof(contents));
            }

            CheckNumeric(contents);

            var result = new bool[CODE_WIDTH];
            var pos = 0;

            pos += AppendPattern(result, pos, START_END_PATTERN, true);

            for (int i = 0; i <= 3; i++)
            {
                var digit = contents[i] - '0';
                pos += AppendPattern(result, pos, L_PATTERNS[digit], false);
            }

            pos += AppendPattern(result, pos, MIDDLE_PATTERN, false);

            for (int i = 4; i <= 7; i++)
            {
                var digit = contents[i] - '0';
                pos += AppendPattern(result, pos, L_PATTERNS[digit], true);
            }
            AppendPattern(result, pos, START_END_PATTERN, true);

            return result;
        }

        public override BitMatrix Encode(string contents, int width, int height) => Encode(contents, BarcodeFormat.EAN_8, width, height);
        public override BitMatrix Encode(string contents, int width, int height, IDictionary<EncodeHintType, object> hints) => Encode(contents, BarcodeFormat.EAN_8, width, height, hints);
    }
}
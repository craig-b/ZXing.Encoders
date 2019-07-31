/*
 * Copyright 2013 ZXing.Net authors
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
    /// This object renders a MSI code as a <see cref="BitMatrix"/>.
    /// </summary>
    public sealed class MSIEncoder : OneDimensionalCodeEncoder
    {
        internal static string ALPHABET = "0123456789";

        private static readonly int[] startWidths = new[] { 2, 1 };
        private static readonly int[] endWidths = new[] { 1, 2, 1 };

        private static readonly int[][] numberWidths = new[]
        {
            new[] { 1, 2, 1, 2, 1, 2, 1, 2 },
            new[] { 1, 2, 1, 2, 1, 2, 2, 1 },
            new[] { 1, 2, 1, 2, 2, 1, 1, 2 },
            new[] { 1, 2, 1, 2, 2, 1, 2, 1 },
            new[] { 1, 2, 2, 1, 1, 2, 1, 2 },
            new[] { 1, 2, 2, 1, 1, 2, 2, 1 },
            new[] { 1, 2, 2, 1, 2, 1, 1, 2 },
            new[] { 1, 2, 2, 1, 2, 1, 2, 1 },
            new[] { 2, 1, 1, 2, 1, 2, 1, 2 },
            new[] { 2, 1, 1, 2, 1, 2, 2, 1 }
        };

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
        public override BitMatrix Encode(string contents, BarcodeFormat format, int width, int height, IDictionary<EncodeHintType, object> hints)
        {
            if (format != BarcodeFormat.MSI)
            {
                throw new ArgumentException($"Can only encode MSI, but got {format}", nameof(format));
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
        /// <exception cref="ArgumentException"></exception>
        override public bool[] Encode(string contents)
        {
            var length = contents.Length;
            for (var i = 0; i < length; i++)
            {
                var indexInString = ALPHABET.IndexOf(contents[i]);
                if (indexInString < 0)
                    throw new ArgumentException($"Requested contents contains a not encodable character: '{contents[i]}'", nameof(contents));
            }

            var codeWidth = 3 + length * 12 + 4;
            var result = new bool[codeWidth];
            var pos = AppendPattern(result, 0, startWidths, true);
            for (var i = 0; i < length; i++)
            {
                var indexInString = ALPHABET.IndexOf(contents[i]);
                var widths = numberWidths[indexInString];
                pos += AppendPattern(result, pos, widths, true);
            }
            AppendPattern(result, pos, endWidths, true);
            return result;
        }

        public override BitMatrix Encode(string contents, int width, int height) => Encode(contents, BarcodeFormat.MSI, width, height);
        public override BitMatrix Encode(string contents, int width, int height, IDictionary<EncodeHintType, object> hints) => Encode(contents, BarcodeFormat.MSI, width, height, hints);
    }
}
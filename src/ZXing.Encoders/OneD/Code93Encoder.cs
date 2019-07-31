/*
 * Copyright 2015 ZXing authors
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
    /// This object renders a CODE93 code as a BitMatrix
    /// </summary>
    public class Code93Encoder : OneDimensionalCodeEncoder
    {
        public override BitMatrix Encode(string contents, BarcodeFormat format, int width, int height, IDictionary<EncodeHintType, object> hints)
        {
            if (format != BarcodeFormat.CODE_93)
            {
                throw new ArgumentException($"Can only encode CODE_93, but got {format}", nameof(contents));
            }
            return base.Encode(contents, format, width, height, hints);
        }

        public override bool[] Encode(string contents)
        {
            var length = contents.Length;
            if (length > 80)
            {
                throw new ArgumentException($"Requested contents should be less than 80 digits long, but got {length}", nameof(contents));
            }
            //each character is encoded by 9 of 0/1's
            var widths = new int[9];

            //length of code + 2 start/stop characters + 2 checksums, each of 9 bits, plus a termination bar
            var codeWidth = ((contents.Length + 2 + 2) * 9) + 1;

            //start character (*)
            ToIntArray(Code39Encoder.CHARACTER_ENCODINGS[47], widths);

            var result = new bool[codeWidth];
            var pos = AppendPattern(result, 0, widths);

            for (int i = 0; i < length; i++)
            {
                var indexInString = Code39Encoder.ALPHABET.IndexOf(contents[i]);
                ToIntArray(Code39Encoder.CHARACTER_ENCODINGS[indexInString], widths);
                pos += AppendPattern(result, pos, widths);
            }

            //add two checksums
            var check1 = ComputeChecksumIndex(contents, 20);
            ToIntArray(Code39Encoder.CHARACTER_ENCODINGS[check1], widths);
            pos += AppendPattern(result, pos, widths);

            //append the contents to reflect the first checksum added
            contents += Code39Encoder.ALPHABET[check1];

            int check2 = ComputeChecksumIndex(contents, 15);
            ToIntArray(Code39Encoder.CHARACTER_ENCODINGS[check2], widths);
            pos += AppendPattern(result, pos, widths);

            //end character (*)
            ToIntArray(Code39Encoder.CHARACTER_ENCODINGS[47], widths);
            pos += AppendPattern(result, pos, widths);

            //termination bar (single black bar)
            result[pos] = true;

            return result;
        }

        private static void ToIntArray(int a, int[] toReturn)
        {
            for (int i = 0; i < 9; i++)
            {
                var temp = a & (1 << (8 - i));
                toReturn[i] = temp == 0 ? 0 : 1;
            }
        }

        /// <summary>
        /// </summary>
        /// <param name="target">output to append to</param>
        /// <param name="pos">start position</param>
        /// <param name="pattern">pattern to append</param>
        /// <param name="startColor">unused</param>
        /// <returns>9</returns>
        [Obsolete("without replacement; intended as an internal-only method")]
        protected static new int AppendPattern(bool[] target, int pos, int[] pattern, bool startColor) => AppendPattern(target, pos, pattern);

        private static int AppendPattern(bool[] target, int pos, int[] pattern)
        {
            foreach (var bit in pattern)
            {
                target[pos++] = bit != 0;
            }
            return 9;
        }

        private static int ComputeChecksumIndex(string contents, int maxWeight)
        {
            var weight = 1;
            var total = 0;

            for (int i = contents.Length - 1; i >= 0; i--)
            {
                var indexInString = Code39Encoder.ALPHABET.IndexOf(contents[i]);
                total += indexInString * weight;
                if (++weight > maxWeight)
                {
                    weight = 1;
                }
            }
            return total % 47;
        }

        public override BitMatrix Encode(string contents, int width, int height) => Encode(contents, BarcodeFormat.CODE_93, width, height);
        public override BitMatrix Encode(string contents, int width, int height, IDictionary<EncodeHintType, object> hints) => Encode(contents, BarcodeFormat.CODE_93, width, height, hints);
    }
}
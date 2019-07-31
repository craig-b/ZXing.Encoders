/*
 * Copyright 2010 ZXing authors
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
    /// This object renders a CODE39 code as a <see cref="BitMatrix"/>.
    /// <author>erik.barbara@gmail.com (Erik Barbara)</author>
    /// </summary>
    public sealed class Code39Encoder : OneDimensionalCodeEncoder
    {
        internal const string ALPHABET = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZ-. $/+%abcd*";

        /// <summary>
        /// These represent the encodings of characters, as patterns of wide and narrow bars.
        /// The 9 least-significant bits of each int correspond to the pattern of wide and narrow,
        /// with 1s representing "wide" and 0s representing narrow.
        /// </summary>
        internal static int[] CHARACTER_ENCODINGS = {
            0x034, 0x121, 0x061, 0x160, 0x031, 0x130, 0x070, 0x025, 0x124, 0x064, // 0-9
            0x109, 0x049, 0x148, 0x019, 0x118, 0x058, 0x00D, 0x10C, 0x04C, 0x01C, // A-J
            0x103, 0x043, 0x142, 0x013, 0x112, 0x052, 0x007, 0x106, 0x046, 0x016, // K-T
            0x181, 0x0C1, 0x1C0, 0x091, 0x190, 0x0D0, 0x085, 0x184, 0x0C4, 0x0A8, // U-$
            0x0A2, 0x08A, 0x02A // /-%
        };

        internal static readonly int ASTERISK_ENCODING = 0x094;

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
        /// <exception cref="ArgumentOutOfRangeException">Thrown if <paramref name="width"/> or <paramref name="height"/> is negative</exception>
        public override BitMatrix Encode(string contents, BarcodeFormat format, int width, int height, IDictionary<EncodeHintType, object> hints)
        {
            if (format != BarcodeFormat.CODE_39)
            {
                throw new ArgumentException($"Can only encode CODE_39, but got {format}", nameof(format));
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
        public override bool[] Encode(string contents)
        {
            var length = contents.Length;
            if (length > 80)
            {
                throw new ArgumentException($"Requested contents should be less than 80 digits long, but got {length}", nameof(contents));
            }
            for (int i = 0; i < length; i++)
            {
                var indexInString = ALPHABET.IndexOf(contents[i]);
                if (indexInString < 0)
                {
                    var unencodable = contents[i];
                    contents = TryToConvertToExtendedMode(contents);
                    if (contents == null)
                        throw new ArgumentException($"Requested content contains a non-encodable character: '{unencodable}'", nameof(contents));
                    length = contents.Length;
                    if (length > 80)
                    {
                        throw new ArgumentException($"Requested contents should be less than 80 digits long, but got {length} (extended full ascii mode)", nameof(contents));
                    }
                    break;
                }
            }

            var widths = new int[9];
            var codeWidth = 24 + 1 + length;
            for (int i = 0; i < length; i++)
            {
                var indexInString = ALPHABET.IndexOf(contents[i]);
                ToIntArray(CHARACTER_ENCODINGS[indexInString], widths);
                foreach (var width in widths)
                {
                    codeWidth += width;
                }
            }
            var result = new bool[codeWidth];
            ToIntArray(ASTERISK_ENCODING, widths);
            var pos = AppendPattern(result, 0, widths, true);
            int[] narrowWhite = { 1 };
            pos += AppendPattern(result, pos, narrowWhite, false);
            //append next character to byte matrix
            for (int i = 0; i < length; i++)
            {
                int indexInString = ALPHABET.IndexOf(contents[i]);
                ToIntArray(CHARACTER_ENCODINGS[indexInString], widths);
                pos += AppendPattern(result, pos, widths, true);
                pos += AppendPattern(result, pos, narrowWhite, false);
            }
            ToIntArray(ASTERISK_ENCODING, widths);
            AppendPattern(result, pos, widths, true);
            return result;
        }

        private static void ToIntArray(int a, int[] toReturn)
        {
            for (int i = 0; i < 9; i++)
            {
                var temp = a & (1 << (8 - i));
                toReturn[i] = temp == 0 ? 1 : 2;
            }
        }

        private static string TryToConvertToExtendedMode(string contents)
        {
            var length = contents.Length;
            var extendedContent = new StringBuilder();
            for (int i = 0; i < length; i++)
            {
                var character = (int)contents[i];
                switch (character)
                {
                    case 0:
                        extendedContent.Append("%U");
                        break;
                    case 32:
                        extendedContent.Append(" ");
                        break;
                    case 45:
                        extendedContent.Append("-");
                        break;
                    case 46:
                        extendedContent.Append(".");
                        break;
                    case 64:
                        extendedContent.Append("%V");
                        break;
                    case 96:
                        extendedContent.Append("%W");
                        break;
                    default:
                        if (character <= 26)
                        {
                            extendedContent.Append("$");
                            extendedContent.Append((char)('A' + (character - 1)));
                        }
                        else if (character < 32)
                        {
                            extendedContent.Append("%");
                            extendedContent.Append((char)('A' + (character - 27)));
                        }
                        else if (character <= ',' || character == '/' || character == ':')
                        {
                            extendedContent.Append("/");
                            extendedContent.Append((char)('A' + (character - 33)));
                        }
                        else if (character <= '9')
                        {
                            extendedContent.Append((char)('0' + (character - 48)));
                        }
                        else if (character <= '?')
                        {
                            extendedContent.Append("%");
                            extendedContent.Append((char)('F' + (character - 59)));
                        }
                        else if (character <= 'Z')
                        {
                            extendedContent.Append((char)('A' + (character - 65)));
                        }
                        else if (character <= '_')
                        {
                            extendedContent.Append("%");
                            extendedContent.Append((char)('K' + (character - 91)));
                        }
                        else if (character <= 'z')
                        {
                            extendedContent.Append("+");
                            extendedContent.Append((char)('A' + (character - 97)));
                        }
                        else if (character <= 127)
                        {
                            extendedContent.Append("%");
                            extendedContent.Append((char)('P' + (character - 123)));
                        }
                        else
                        {
                            return null;
                        }
                        break;
                }
            }

            return extendedContent.ToString();
        }

        public override BitMatrix Encode(string contents, int width, int height) => Encode(contents, BarcodeFormat.CODE_39, width, height);
        public override BitMatrix Encode(string contents, int width, int height, IDictionary<EncodeHintType, object> hints) => Encode(contents, BarcodeFormat.CODE_39, width, height, hints);
    }
}
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

namespace ZXing.Encoders.OneD
{
    /// <summary>
    /// This object renders a CODE128 code as a <see cref="BitMatrix" />.
    /// 
    /// <author>erik.barbara@gmail.com (Erik Barbara)</author>
    /// </summary>
    public sealed class Code128Encoder : OneDimensionalCodeEncoder
    {
        internal static int[][] CODE_PATTERNS = {
            new[] {2, 1, 2, 2, 2, 2}, // 0
            new[] {2, 2, 2, 1, 2, 2},
            new[] {2, 2, 2, 2, 2, 1},
            new[] {1, 2, 1, 2, 2, 3},
            new[] {1, 2, 1, 3, 2, 2},
            new[] {1, 3, 1, 2, 2, 2}, // 5
            new[] {1, 2, 2, 2, 1, 3},
            new[] {1, 2, 2, 3, 1, 2},
            new[] {1, 3, 2, 2, 1, 2},
            new[] {2, 2, 1, 2, 1, 3},
            new[] {2, 2, 1, 3, 1, 2}, // 10
            new[] {2, 3, 1, 2, 1, 2},
            new[] {1, 1, 2, 2, 3, 2},
            new[] {1, 2, 2, 1, 3, 2},
            new[] {1, 2, 2, 2, 3, 1},
            new[] {1, 1, 3, 2, 2, 2}, // 15
            new[] {1, 2, 3, 1, 2, 2},
            new[] {1, 2, 3, 2, 2, 1},
            new[] {2, 2, 3, 2, 1, 1},
            new[] {2, 2, 1, 1, 3, 2},
            new[] {2, 2, 1, 2, 3, 1}, // 20
            new[] {2, 1, 3, 2, 1, 2},
            new[] {2, 2, 3, 1, 1, 2},
            new[] {3, 1, 2, 1, 3, 1},
            new[] {3, 1, 1, 2, 2, 2},
            new[] {3, 2, 1, 1, 2, 2}, // 25
            new[] {3, 2, 1, 2, 2, 1},
            new[] {3, 1, 2, 2, 1, 2},
            new[] {3, 2, 2, 1, 1, 2},
            new[] {3, 2, 2, 2, 1, 1},
            new[] {2, 1, 2, 1, 2, 3}, // 30
            new[] {2, 1, 2, 3, 2, 1},
            new[] {2, 3, 2, 1, 2, 1},
            new[] {1, 1, 1, 3, 2, 3},
            new[] {1, 3, 1, 1, 2, 3},
            new[] {1, 3, 1, 3, 2, 1}, // 35
            new[] {1, 1, 2, 3, 1, 3},
            new[] {1, 3, 2, 1, 1, 3},
            new[] {1, 3, 2, 3, 1, 1},
            new[] {2, 1, 1, 3, 1, 3},
            new[] {2, 3, 1, 1, 1, 3}, // 40
            new[] {2, 3, 1, 3, 1, 1},
            new[] {1, 1, 2, 1, 3, 3},
            new[] {1, 1, 2, 3, 3, 1},
            new[] {1, 3, 2, 1, 3, 1},
            new[] {1, 1, 3, 1, 2, 3}, // 45
            new[] {1, 1, 3, 3, 2, 1},
            new[] {1, 3, 3, 1, 2, 1},
            new[] {3, 1, 3, 1, 2, 1},
            new[] {2, 1, 1, 3, 3, 1},
            new[] {2, 3, 1, 1, 3, 1}, // 50
            new[] {2, 1, 3, 1, 1, 3},
            new[] {2, 1, 3, 3, 1, 1},
            new[] {2, 1, 3, 1, 3, 1},
            new[] {3, 1, 1, 1, 2, 3},
            new[] {3, 1, 1, 3, 2, 1}, // 55
            new[] {3, 3, 1, 1, 2, 1},
            new[] {3, 1, 2, 1, 1, 3},
            new[] {3, 1, 2, 3, 1, 1},
            new[] {3, 3, 2, 1, 1, 1},
            new[] {3, 1, 4, 1, 1, 1}, // 60
            new[] {2, 2, 1, 4, 1, 1},
            new[] {4, 3, 1, 1, 1, 1},
            new[] {1, 1, 1, 2, 2, 4},
            new[] {1, 1, 1, 4, 2, 2},
            new[] {1, 2, 1, 1, 2, 4}, // 65
            new[] {1, 2, 1, 4, 2, 1},
            new[] {1, 4, 1, 1, 2, 2},
            new[] {1, 4, 1, 2, 2, 1},
            new[] {1, 1, 2, 2, 1, 4},
            new[] {1, 1, 2, 4, 1, 2}, // 70
            new[] {1, 2, 2, 1, 1, 4},
            new[] {1, 2, 2, 4, 1, 1},
            new[] {1, 4, 2, 1, 1, 2},
            new[] {1, 4, 2, 2, 1, 1},
            new[] {2, 4, 1, 2, 1, 1}, // 75
            new[] {2, 2, 1, 1, 1, 4},
            new[] {4, 1, 3, 1, 1, 1},
            new[] {2, 4, 1, 1, 1, 2},
            new[] {1, 3, 4, 1, 1, 1},
            new[] {1, 1, 1, 2, 4, 2}, // 80
            new[] {1, 2, 1, 1, 4, 2},
            new[] {1, 2, 1, 2, 4, 1},
            new[] {1, 1, 4, 2, 1, 2},
            new[] {1, 2, 4, 1, 1, 2},
            new[] {1, 2, 4, 2, 1, 1}, // 85
            new[] {4, 1, 1, 2, 1, 2},
            new[] {4, 2, 1, 1, 1, 2},
            new[] {4, 2, 1, 2, 1, 1},
            new[] {2, 1, 2, 1, 4, 1},
            new[] {2, 1, 4, 1, 2, 1}, // 90
            new[] {4, 1, 2, 1, 2, 1},
            new[] {1, 1, 1, 1, 4, 3},
            new[] {1, 1, 1, 3, 4, 1},
            new[] {1, 3, 1, 1, 4, 1},
            new[] {1, 1, 4, 1, 1, 3}, // 95
            new[] {1, 1, 4, 3, 1, 1},
            new[] {4, 1, 1, 1, 1, 3},
            new[] {4, 1, 1, 3, 1, 1},
            new[] {1, 1, 3, 1, 4, 1},
            new[] {1, 1, 4, 1, 3, 1}, // 100
            new[] {3, 1, 1, 1, 4, 1},
            new[] {4, 1, 1, 1, 3, 1},
            new[] {2, 1, 1, 4, 1, 2},
            new[] {2, 1, 1, 2, 1, 4},
            new[] {2, 1, 1, 2, 3, 2}, // 105
            new[] {2, 3, 3, 1, 1, 1, 2}
        };

        private const int CODE_START_A = 103;
        private const int CODE_START_B = 104;
        private const int CODE_START_C = 105;
        private const int CODE_CODE_A = 101;
        private const int CODE_CODE_B = 100;
        private const int CODE_CODE_C = 99;
        private const int CODE_STOP = 106;

        // Dummy characters used to specify control characters in input
        private const char ESCAPE_FNC_1 = '\u00f1';
        private const char ESCAPE_FNC_2 = '\u00f2';
        private const char ESCAPE_FNC_3 = '\u00f3';
        private const char ESCAPE_FNC_4 = '\u00f4';

        private const int CODE_FNC_1 = 102;   // Code A, Code B, Code C
        private const int CODE_FNC_2 = 97;    // Code A, Code B
        private const int CODE_FNC_3 = 96;    // Code A, Code B
        private const int CODE_FNC_4_A = 101; // Code A
        private const int CODE_FNC_4_B = 100; // Code B

        // Results of minimal lookahead for code C
        private enum CType
        {
            UNCODABLE,
            ONE_DIGIT,
            TWO_DIGITS,
            FNC_1
        }

        private bool forceCodesetB;

        public override BitMatrix Encode(string contents, BarcodeFormat format, int width, int height, IDictionary<EncodeHintType, object> hints)
        {
            if (format != BarcodeFormat.CODE_128)
            {
                throw new ArgumentException($"Can only encode CODE_128, but got {format}", nameof(format));
            }

            forceCodesetB = hints?.ContainsKey(EncodeHintType.CODE128_FORCE_CODESET_B) == true
                             && hints[EncodeHintType.CODE128_FORCE_CODESET_B] != null
                             && Convert.ToBoolean(hints[EncodeHintType.CODE128_FORCE_CODESET_B]);

            if (hints?.ContainsKey(EncodeHintType.GS1_FORMAT) == true
                && hints[EncodeHintType.GS1_FORMAT] != null
                && Convert.ToBoolean(hints[EncodeHintType.GS1_FORMAT]))
            {
                // append the FNC1 character at the first position if not already present
                if (!string.IsNullOrEmpty(contents) && contents[0] != ESCAPE_FNC_1)
                    contents = ESCAPE_FNC_1 + contents;
            }

            return base.Encode(contents, format, width, height, hints);
        }

        public override bool[] Encode(string contents)
        {
            var length = contents.Length;
            // Check length
            if (length < 1 || length > 80)
            {
                throw new ArgumentException($"Contents length should be between 1 and 80 characters, but got {length}", nameof(contents));
            }
            // Check content
            for (int i = 0; i < length; i++)
            {
                var c = contents[i];
                switch (c)
                {
                    case ESCAPE_FNC_1:
                    case ESCAPE_FNC_2:
                    case ESCAPE_FNC_3:
                    case ESCAPE_FNC_4:
                        break;
                    default:
                        if (c > 127)
                            // support for FNC4 isn't implemented, no full Latin-1 character set available at the moment
                            throw new ArgumentException($"Bad character in input: {c}", nameof(contents));
                        break;
                }
            }

            var patterns = new List<int[]>(); // temporary storage for patterns
            var checkSum = 0;
            var checkWeight = 1;
            var codeSet = 0; // selected code (CODE_CODE_B or CODE_CODE_C)
            var position = 0; // position in contents

            while (position < length)
            {
                //Select code to use
                var newCodeSet = ChooseCode(contents, position, codeSet);

                //Get the pattern index
                int patternIndex;
                if (newCodeSet == codeSet)
                {
                    // Encode the current character
                    // First handle escapes
                    switch (contents[position])
                    {
                        case ESCAPE_FNC_1:
                            patternIndex = CODE_FNC_1;
                            break;
                        case ESCAPE_FNC_2:
                            patternIndex = CODE_FNC_2;
                            break;
                        case ESCAPE_FNC_3:
                            patternIndex = CODE_FNC_3;
                            break;
                        case ESCAPE_FNC_4:
                            if (newCodeSet == CODE_CODE_A)
                                patternIndex = CODE_FNC_4_A;
                            else
                                patternIndex = CODE_FNC_4_B;
                            break;
                        default:
                            // Then handle normal characters otherwise
                            switch (codeSet)
                            {
                                case CODE_CODE_A:
                                    patternIndex = contents[position] - ' ';
                                    if (patternIndex < 0)
                                    {
                                        // everything below a space character comes behind the underscore in the code patterns table
                                        patternIndex += '`';
                                    }
                                    break;
                                case CODE_CODE_B:
                                    patternIndex = contents[position] - ' ';
                                    break;
                                default:
                                    // CODE_CODE_C
                                    //patternIndex = int.Parse(contents.Substring(position, 2));
                                    patternIndex = ((contents[position] - '0') * 10) + (contents[position + 1] - '0');
                                    position++; // Also incremented below
                                    break;
                            }
                            break;
                    }
                    position++;
                }
                else
                {
                    // Should we change the current code?
                    // Do we have a code set?
                    if (codeSet == 0)
                    {
                        // No, we don't have a code set
                        patternIndex = newCodeSet switch
                        {
                            CODE_CODE_A => CODE_START_A,
                            CODE_CODE_B => CODE_START_B,
                            _ => CODE_START_C,
                        };
                    }
                    else
                    {
                        // Yes, we have a code set
                        patternIndex = newCodeSet;
                    }
                    codeSet = newCodeSet;
                }

                // Get the pattern
                patterns.Add(CODE_PATTERNS[patternIndex]);

                // Compute checksum
                checkSum += patternIndex * checkWeight;
                if (position != 0)
                {
                    checkWeight++;
                }
            }

            // Compute and append checksum
            checkSum %= 103;
            patterns.Add(CODE_PATTERNS[checkSum]);

            // Append stop code
            patterns.Add(CODE_PATTERNS[CODE_STOP]);

            // Compute code width
            var codeWidth = 0;
            foreach (var pattern in patterns)
            {
                foreach (var width in pattern)
                {
                    codeWidth += width;
                }
            }

            // Compute result
            var result = new bool[codeWidth];
            var pos = 0;
            foreach (var pattern in patterns)
            {
                pos += AppendPattern(result, pos, pattern, true);
            }

            return result;
        }

        private static CType FindCType(string value, int start)
        {
            var last = value.Length;
            if (start >= last)
            {
                return CType.UNCODABLE;
            }
            var c = value[start];
            if (c == ESCAPE_FNC_1)
            {
                return CType.FNC_1;
            }
            if (c < '0' || c > '9')
            {
                return CType.UNCODABLE;
            }
            if (start + 1 >= last)
            {
                return CType.ONE_DIGIT;
            }
            c = value[start + 1];
            if (c < '0' || c > '9')
            {
                return CType.ONE_DIGIT;
            }
            return CType.TWO_DIGITS;
        }

        private int ChooseCode(string value, int start, int oldCode)
        {
            var lookahead = FindCType(value, start);
            if (lookahead == CType.ONE_DIGIT)
            {
                return CODE_CODE_B;
            }
            if (lookahead == CType.UNCODABLE)
            {
                if (start < value.Length)
                {
                    var c = value[start];
                    if (c < ' ' || (oldCode == CODE_CODE_A && c < '`'))
                        // can continue in code A, encodes ASCII 0 to 95
                        return CODE_CODE_A;
                }
                return CODE_CODE_B; // no choice
            }
            if (oldCode == CODE_CODE_C)
            {
                // can continue in code C
                return CODE_CODE_C;
            }
            if (oldCode == CODE_CODE_B)
            {
                if (lookahead == CType.FNC_1)
                {
                    return CODE_CODE_B; // can continue in code B
                }
                // Seen two consecutive digits, see what follows
                lookahead = FindCType(value, start + 2);
                if (lookahead == CType.UNCODABLE || lookahead == CType.ONE_DIGIT)
                {
                    return CODE_CODE_B; // not worth switching now
                }
                if (lookahead == CType.FNC_1)
                { // two digits, then FNC_1...
                    lookahead = FindCType(value, start + 3);
                    if (lookahead == CType.TWO_DIGITS)
                    { // then two more digits, switch
                        return forceCodesetB ? CODE_CODE_B : CODE_CODE_C;
                    }
                    else
                    {
                        return CODE_CODE_B; // otherwise not worth switching
                    }
                }
                // At this point, there are at least 4 consecutive digits.
                // Look ahead to choose whether to switch now or on the next round.
                int index = start + 4;
                while ((lookahead = FindCType(value, index)) == CType.TWO_DIGITS)
                {
                    index += 2;
                }
                if (lookahead == CType.ONE_DIGIT)
                { // odd number of digits, switch later
                    return CODE_CODE_B;
                }
                return forceCodesetB ? CODE_CODE_B : CODE_CODE_C; // even number of digits, switch now
            }
            // Here oldCode == 0, which means we are choosing the initial code
            if (lookahead == CType.FNC_1)
            { // ignore FNC_1
                lookahead = FindCType(value, start + 1);
            }
            if (lookahead == CType.TWO_DIGITS)
            { // at least two digits, start in code C
                return forceCodesetB ? CODE_CODE_B : CODE_CODE_C;
            }
            return CODE_CODE_B;
        }

        public override BitMatrix Encode(string contents, int width, int height) => Encode(contents, BarcodeFormat.CODE_128, width, height);
        public override BitMatrix Encode(string contents, int width, int height, IDictionary<EncodeHintType, object> hints) => Encode(contents, BarcodeFormat.CODE_128, width, height, hints);
    }
}
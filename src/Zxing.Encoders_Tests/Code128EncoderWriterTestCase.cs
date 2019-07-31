/*
 * Copyright 2014 ZXing authors
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
using NUnit.Framework;

namespace ZXing.Encoders.OneD.Test
{
    public class Code128EncoderTestCase
    {
        private const string FNC1 = "11110101110";
        private const string FNC2 = "11110101000";
        private const string FNC3 = "10111100010";
        private const string FNC4 = "10111101110";
        private const string START_CODE_A = "11010000100";
        private const string START_CODE_B = "11010010000";
        private const string START_CODE_C = "11010011100";
        private const string SWITCH_CODE_A = "11101011110";
        private const string SWITCH_CODE_B = "10111101110";
        private const string QUIET_SPACE = "00000";
        private const string STOP = "1100011101011";

        private IEncoder writer;

        [SetUp]
        public void SetUp()
        {
            writer = new Code128Encoder();
        }

        [Test]
        [TestOf(typeof(Code128Encoder))]
        public void TestEncodeWithFunc3()
        {
            const string toEncode = "\u00f3" + "123";
            //                                                       "1"            "2"             "3"          check digit 51
            const string expected = QUIET_SPACE + START_CODE_B + FNC3 + "10011100110" + "11001110010" + "11001011100" +
                           "11101000110" + STOP + QUIET_SPACE;

            var result = writer.Encode(toEncode, BarcodeFormat.CODE_128, 0, 0);

            var actual = BitMatrixTestCase.MatrixToString(result);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        [TestOf(typeof(Code128Encoder))]
        public void TestEncodeWithFunc2()
        {
            const string toEncode = "\u00f2" + "123";
            //                                                       "1"            "2"             "3"          check digit 56
            const string expected = QUIET_SPACE + START_CODE_B + FNC2 + "10011100110" + "11001110010" + "11001011100" +
                           "11100010110" + STOP + QUIET_SPACE;

            var result = writer.Encode(toEncode, BarcodeFormat.CODE_128, 0, 0);

            var actual = BitMatrixTestCase.MatrixToString(result);

            Assert.AreEqual(expected, actual);
        }

        [Test]
        [TestOf(typeof(Code128Encoder))]
        public void TestEncodeWithFunc1()
        {
            const string toEncode = "\u00f1" + "123";
            //                                                       "12"                           "3"          check digit 92
            const string expected = QUIET_SPACE + START_CODE_C + FNC1 + "10110011100" + SWITCH_CODE_B + "11001011100" +
                           "10101111000" + STOP + QUIET_SPACE;

            var result = writer.Encode(toEncode, BarcodeFormat.CODE_128, 0, 0);

            var actual = BitMatrixTestCase.MatrixToString(result);

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        //public void TestRoundtrip()
        //{
        //    var toEncode = "\u00f1" + "10958" + "\u00f1" + "17160526";
        //    var expected = "1095817160526";

        //    var encResult = writer.Encode(toEncode, BarcodeFormat.CODE_128, 0, 0);
        //    var row = encResult.GetRow(0, null);
        //    var rtResult = reader.decodeRow(0, row, null);
        //    var actual = rtResult.Text;
        //    Assert.AreEqual(expected, actual);
        //}

        [Test]
        [TestOf(typeof(Code128Encoder))]
        public void TestEncodeWithFunc4()
        {
            const string toEncode = "\u00f4" + "123";
            //                                                       "1"            "2"             "3"          check digit 59
            const string expected = QUIET_SPACE + START_CODE_B + FNC4 + "10011100110" + "11001110010" + "11001011100" +
                           "11100011010" + STOP + QUIET_SPACE;

            var result = writer.Encode(toEncode, BarcodeFormat.CODE_128, 0, 0);

            var actual = BitMatrixTestCase.MatrixToString(result);

            Assert.AreEqual(expected, actual);
        }

        //[Test]
        //public void Should_Encode_And_Decode_Roundtrip()
        //{
        //    var contents = string.Empty;

        //    for (var i = 0; i < 128; i++)
        //    {
        //        contents += (char)i;
        //        if ((i + 1) % 32 == 0)
        //        {
        //            Should_Encode(contents);
        //            contents = string.Empty;
        //        }
        //    }
        //}

        //[TestCase("\0ABab\u0010", TestName = "Start with A, switch to B and back to A")]
        //[TestCase("ab\0ab", TestName = "Start with B, switch to A and back to B")]
        //public void Should_Encode(string contents)
        //{
        //    var sut = new Code128Encoder();
        //    //var sutDecode = new Code128Reader();

        //    var result = sut.Encode(contents, BarcodeFormat.CODE_128, 0, 0);
        //    var resultString = BitMatrixTestCase.MatrixToString(result);
        //    Console.WriteLine(contents);
        //    Console.WriteLine(resultString);
        //    Console.WriteLine("");
        //    var matrix = BitMatrix.Parse(resultString, "1", "0");
        //    var row = new BitArray(matrix.Width);
        //    matrix.GetRow(0, row);
        //    var decodingResult = sutDecode.decodeRow(0, row, null);
        //    Assert.That(decodingResult, Is.Not.Null);
        //    Assert.That(decodingResult.Text, Is.EqualTo(contents));
        //}

        //[Test]
        //public void TestEncodeSwitchBetweenCodesetsAAndB()
        //{
        //    // start with A switch to B and back to A
        //    //                                                      "\0"            "A"             "B"             Switch to B     "a"             "b"             Switch to A     "\u0010"        check digit
        //    TestEncode("\0ABab\u0010",
        //       QUIET_SPACE + START_CODE_A + "10100001100" + "10100011000" + "10001011000" + SWITCH_CODE_B + "10010110000" + "10010000110" + SWITCH_CODE_A + "10100111100" + "11001110100" + STOP + QUIET_SPACE);

        //    // start with B switch to A and back to B
        //    //                                                "a"             "b"             Switch to A     "\0             "Switch to B"   "a"             "b"             check digit
        //    TestEncode("ab\0ab",
        //       QUIET_SPACE + START_CODE_B + "10010110000" + "10010000110" + SWITCH_CODE_A + "10100001100" + SWITCH_CODE_B + "10010110000" + "10010000110" + "11010001110" + STOP + QUIET_SPACE);
        //}

        //private void TestEncode(string toEncode, string expected)
        //{
        //    var result = writer.Encode(toEncode, BarcodeFormat.CODE_128, 0, 0);

        //    var actual = BitMatrixTestCase.MatrixToString(result);
        //    Assert.AreEqual(expected, actual, toEncode);

        //    var row = result.GetRow(0, null);
        //    var rtResult = reader.decodeRow(0, row, null);
        //    var actualRoundtripResultText = rtResult.Text;
        //    Assert.AreEqual(toEncode, actualRoundtripResultText);
        //}
    }
}
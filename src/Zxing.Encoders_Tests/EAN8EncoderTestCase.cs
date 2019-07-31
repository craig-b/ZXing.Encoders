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
using NUnit.Framework;

namespace ZXing.Encoders.OneD.Test
{
    /// <summary>
    /// <author>Ari Pollak</author>
    /// </summary>
    [TestFixture]
    public sealed class EAN8EncoderTestCase
    {
        [Test]
        [TestCase("96385074", "0000001010001011010111101111010110111010101001110111001010001001011100101000000", TestName = "EAN8testEncode")]
        [TestCase("9638507", "0000001010001011010111101111010110111010101001110111001010001001011100101000000", TestName = "EAN8testAddChecksumAndEncode")]
        [TestOf(typeof(EAN8Encoder))]
        public void TestEncode(string content, string encoding)
        {
            var result = new EAN8Encoder().Encode(content, BarcodeFormat.EAN_8, encoding.Length, 0);
            Assert.AreEqual(encoding, BitMatrixTestCase.MatrixToString(result));
        }

        [Test]
        [TestOf(typeof(EAN8Encoder))]
        public void TestEncodeIllegalCharacters()
        {
            Assert.Throws<ArgumentException>(() => new EAN8Encoder().Encode("96385abc", BarcodeFormat.EAN_8, 0, 0));
        }
    }
}
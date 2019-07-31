/*
 * Copyright 2011 ZXing authors
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

using NUnit.Framework;

namespace ZXing.Encoders.OneD.Test
{
    /// <summary>
    /// <author>dsbnatut@gmail.com (Kazuki Nishiura)</author>
    /// <author>Sean Owen</author>
    /// </summary>
    public sealed class CodaBarEncoderTestCase
    {
        [SetUp]
        public void Setup() { }

        [Test]
        [TestOf(typeof(CodaBarEncoder))]
        public void TestAltStartEnd()
        {
            Assert.AreEqual(Encode("T123456789-$T"), Encode("A123456789-$A"));
        }

        [Test]
        [TestOf(typeof(CodaBarEncoder))]
        [TestCase("B515-3/B", ExpectedResult = "00000" + "1001001011" + "0110101001" + "0101011001" + "0110101001" + "0101001101" + "0110010101" + "01101101011" + "01001001011" + "00000")]
        [TestCase("T123T", ExpectedResult = "00000" + "1011001001" + "0101011001" + "0101001011" + "0110010101" + "01011001001" + "00000")]
        public string DoTest(string input) => BitMatrixTestCase.MatrixToString(Encode(input));

        private static BitMatrix Encode(string input) => new CodaBarEncoder().Encode(input, BarcodeFormat.CODABAR, 0, 0);
    }
}

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

using NUnit.Framework;
using System;
using System.Text;
using ZXing.Encoders;

namespace ZXing.Encoders.OneD.Test
{
    internal static class BitMatrixTestCase
    {
        internal static string MatrixToString(BitMatrix result)
        {
            Assert.AreEqual(1, result.Height);
            var builder = new StringBuilder(result.Width);
            for (int i = 0; i < result.Width; i++)
            {
                builder.Append(result[i, 0] ? '1' : '0');
            }
            return builder.ToString();
        }
    }
}
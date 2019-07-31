﻿/*
* Copyright 2008 ZXing authors
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
using System.Collections;
using System.Collections.Generic;
using System.Text;
using ZXing.Encoders.ReedSolomon;

//using ZXing.Common;
//using ZXing.Common.ReedSolomon;

namespace ZXing.Encoders.QrCode
{
    /// <summary>
    /// </summary>
    /// <author>satorux@google.com (Satoru Takabayashi) - creator</author>
    /// <author>dswitkin@google.com (Daniel Switkin) - ported from C++</author>
    public static class QrCodeEncoder
    {

        // The original table is defined in the table 5 of JISX0510:2004 (p.19).
        private static readonly int[] ALPHANUMERIC_TABLE = {
         -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  // 0x00-0x0f
         -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1, -1,  // 0x10-0x1f
         36, -1, -1, -1, 37, 38, -1, -1, -1, -1, 39, 40, -1, 41, 42, 43,  // 0x20-0x2f
         0,   1,  2,  3,  4,  5,  6,  7,  8,  9, 44, -1, -1, -1, -1, -1,  // 0x30-0x3f
         -1, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24,  // 0x40-0x4f
         25, 26, 27, 28, 29, 30, 31, 32, 33, 34, 35, -1, -1, -1, -1, -1,  // 0x50-0x5f
      };

        internal static string DEFAULT_BYTE_MODE_ENCODING = "ISO-8859-1";

        // The mask penalty calculation is complicated.  See Table 21 of JISX0510:2004 (p.45) for details.
        // Basically it applies four rules and summate all penalties.
        private static int CalculateMaskPenalty(ByteMatrix matrix)
            => MaskUtil.ApplyMaskPenaltyRule1(matrix) + MaskUtil.ApplyMaskPenaltyRule2(matrix)
                    + MaskUtil.ApplyMaskPenaltyRule3(matrix) + MaskUtil.ApplyMaskPenaltyRule4(matrix);

        /// <summary>
        /// Encode "bytes" with the error correction level "ecLevel". The encoding mode will be chosen
        /// internally by chooseMode(). On success, store the result in "qrCode".
        /// We recommend you to use QRCode.EC_LEVEL_L (the lowest level) for
        /// "getECLevel" since our primary use is to show QR code on desktop screens. We don't need very
        /// strong error correction for this purpose.
        /// Note that there is no way to encode bytes in MODE_KANJI. We might want to add EncodeWithMode()
        /// with which clients can specify the encoding mode. For now, we don't need the functionality.
        /// </summary>
        /// <param name="content">text to encode</param>
        /// <param name="ecLevel">error correction level to use</param>
        /// <returns><see cref="ByteMatrix"/> representing the encoded QR code</returns>
        public static ByteMatrix Encode(string content, ErrorCorrectionLevel ecLevel) => Encode(content, ecLevel, null);

        /// <summary>
        /// Encodes the specified content.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="ecLevel">The ec level.</param>
        /// <param name="hints">The hints.</param>
        /// <returns></returns>
        public static ByteMatrix Encode(string content, ErrorCorrectionLevel ecLevel, IDictionary<EncodeHintType, object> hints)
        {
            // Determine what character encoding has been specified by the caller, if any
            var hasEncodingHint = hints?.ContainsKey(EncodeHintType.CHARACTER_SET) == true;

            var encoding = (hints?.ContainsKey(EncodeHintType.CHARACTER_SET) != true
                ? null
                : (string)hints[EncodeHintType.CHARACTER_SET]) ?? DEFAULT_BYTE_MODE_ENCODING;

            var generateECI = hasEncodingHint || !DEFAULT_BYTE_MODE_ENCODING.Equals(encoding);

            // Pick an encoding mode appropriate for the content. Note that this will not attempt to use
            // multiple modes / segments even if that were more efficient. Twould be nice.
            var mode = ChooseMode(content, encoding);

            // This will store the header information, like mode and
            // length, as well as "header" segments like an ECI segment.
            var headerBits = new BitArray();

            // Append ECI segment if applicable
            if (mode == Mode.BYTE && generateECI)
            {
                var eci = CharacterSetECI.GetCharacterSetECIByName(encoding);
                if (eci != null)
                {
                    var eciIsExplicitDisabled = hints?.ContainsKey(EncodeHintType.DISABLE_ECI) == true
                        && hints[EncodeHintType.DISABLE_ECI] != null
                        && Convert.ToBoolean(hints[EncodeHintType.DISABLE_ECI].ToString());

                    if (!eciIsExplicitDisabled)
                    {
                        AppendECI(eci, headerBits);
                    }
                }
            }

            // Append the FNC1 mode header for GS1 formatted data if applicable
            var hasGS1FormatHint = hints?.ContainsKey(EncodeHintType.GS1_FORMAT) == true;
            if (hasGS1FormatHint && hints[EncodeHintType.GS1_FORMAT] != null && Convert.ToBoolean(hints[EncodeHintType.GS1_FORMAT].ToString()))
            {
                // GS1 formatted codes are prefixed with a FNC1 in first position mode header
                AppendModeInfo(Mode.FNC1_FIRST_POSITION, headerBits);
            }

            // (With ECI in place,) Write the mode marker
            AppendModeInfo(mode, headerBits);

            // Collect data within the main segment, separately, to count its size if needed. Don't add it to
            // main payload yet.
            var dataBits = new BitArray();
            AppendBytes(content, mode, dataBits, encoding);

            Version version;
            if (hints?.ContainsKey(EncodeHintType.QR_VERSION) == true)
            {
                var versionNumber = int.Parse(hints[EncodeHintType.QR_VERSION].ToString());
                version = Version.GetVersionForNumber(versionNumber);
                var bitsNeeded = CalculateBitsNeeded(mode, headerBits, dataBits, version);
                if (!WillFit(bitsNeeded, version, ecLevel))
                {
                    throw new WriterException("Data too big for requested version");
                }
            }
            else
            {
                version = RecommendVersion(ecLevel, mode, headerBits, dataBits);
            }

            var headerAndDataBits = new BitArray();
            headerAndDataBits.AppendBitArray(headerBits);
            // Find "length" of main segment and write it
            var numLetters = mode == Mode.BYTE ? dataBits.SizeInBytes : content.Length;
            AppendLengthInfo(numLetters, version, mode, headerAndDataBits);
            // Put data together into the overall payload
            headerAndDataBits.AppendBitArray(dataBits);

            var ecBlocks = version.GetECBlocksForLevel(ecLevel);
            var numDataBytes = version.TotalCodewords - ecBlocks.TotalECCodewords;

            // Terminate the bits properly.
            TerminateBits(numDataBytes, headerAndDataBits);

            // Interleave data bits with error correction code.
            var finalBits = InterleaveWithECBytes(headerAndDataBits, version.TotalCodewords, numDataBytes, ecBlocks.NumBlocks);

            //var qrCode = new QRCode
            //{
            //    ECLevel = ecLevel,
            //    Mode = mode,
            //    Version = version
            //};

            //  Choose the mask pattern and set to "qrCode".
            var dimension = version.DimensionForVersion;
            var matrix = new ByteMatrix(dimension, dimension);
            var maskPattern = ChooseMaskPattern(finalBits, ecLevel, version, matrix);
            //qrCode.MaskPattern = maskPattern;

            // Build the matrix and set it to "qrCode".
            MatrixUtil.BuildMatrix(finalBits, ecLevel, version, maskPattern, matrix);
            //qrCode.Matrix = matrix;

            return matrix;
        }

        /// <summary>
        /// Decides the smallest version of QR code that will contain all of the provided data.
        /// </summary>
        /// <exception cref="WriterException">If the data cannot fit in any version</exception>
        private static Version RecommendVersion(ErrorCorrectionLevel ecLevel, Mode mode, BitArray headerBits, BitArray dataBits)
        {
            // Hard part: need to know version to know how many bits length takes. But need to know how many
            // bits it takes to know version. First we take a guess at version by assuming version will be
            // the minimum, 1:
            var provisionalBitsNeeded = CalculateBitsNeeded(mode, headerBits, dataBits, Version.GetVersionForNumber(1));
            var provisionalVersion = ChooseVersion(provisionalBitsNeeded, ecLevel);

            // Use that guess to calculate the right version. I am still not sure this works in 100% of cases.
            var bitsNeeded = CalculateBitsNeeded(mode, headerBits, dataBits, provisionalVersion);
            return ChooseVersion(bitsNeeded, ecLevel);
        }

        private static int CalculateBitsNeeded(Mode mode, BitArray headerBits, BitArray dataBits, Version version)
            => headerBits.Size + mode.GetCharacterCountBits(version) + dataBits.Size;

        /// <summary>
        /// Gets the alphanumeric code.
        /// </summary>
        /// <param name="code">The code.</param>
        /// <returns>the code point of the table used in alphanumeric mode or
        /// -1 if there is no corresponding code in the table.</returns>
        internal static int getAlphanumericCode(int code)
            => code < ALPHANUMERIC_TABLE.Length ? ALPHANUMERIC_TABLE[code] : -1;

        /// <summary>
        /// Chooses the mode.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <returns></returns>
        public static Mode ChooseMode(string content) => ChooseMode(content, null);

        /// <summary>
        /// Choose the best mode by examining the content. Note that 'encoding' is used as a hint;
        /// if it is Shift_JIS, and the input is only double-byte Kanji, then we return <see cref="Mode.KANJI"/>.
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="encoding">The encoding.</param>
        /// <returns></returns>
        private static Mode ChooseMode(string content, string encoding)
        {
            if (encoding == "Shift_JIS" && IsOnlyDoubleByteKanji(content))
            {
                // Choose Kanji mode if all input are double-byte characters
                return Mode.KANJI;
            }
            bool hasNumeric = false;
            bool hasAlphanumeric = false;
            for (int i = 0; i < content.Length; ++i)
            {
                var @char = content[i];
                if (@char >= '0' && @char <= '9')
                {
                    hasNumeric = true;
                }
                else if (getAlphanumericCode(@char) != -1)
                {
                    hasAlphanumeric = true;
                }
                else
                {
                    return Mode.BYTE;
                }
            }
            if (hasAlphanumeric)
            {

                return Mode.ALPHANUMERIC;
            }
            if (hasNumeric)
            {

                return Mode.NUMERIC;
            }
            return Mode.BYTE;
        }

        private static bool IsOnlyDoubleByteKanji(string content)
        {
            byte[] bytes;
            try
            {
                bytes = Encoding.GetEncoding("Shift_JIS").GetBytes(content);
            }
            catch
            {
                return false;
            }
            var length = bytes.Length;
            if (length % 2 != 0)
            {
                return false;
            }
            for (int i = 0; i < length; i += 2)
            {
                var byte1 = bytes[i] & 0xFF;
                if ((byte1 < 0x81 || byte1 > 0x9F) && (byte1 < 0xE0 || byte1 > 0xEB))
                {
                    return false;
                }
            }
            return true;
        }

        private static int ChooseMaskPattern(BitArray bits, ErrorCorrectionLevel ecLevel, Version version, ByteMatrix matrix)
        {
            var minPenalty = int.MaxValue;  // Lower penalty is better.
            var bestMaskPattern = -1;
            // We try all mask patterns to choose the best one.
            for (int maskPattern = 0; maskPattern < 8 /*QRCode.NUM_MASK_PATTERNS*/; maskPattern++)
            {

                MatrixUtil.BuildMatrix(bits, ecLevel, version, maskPattern, matrix);
                var penalty = CalculateMaskPenalty(matrix);
                if (penalty < minPenalty)
                {
                    minPenalty = penalty;
                    bestMaskPattern = maskPattern;
                }
            }
            return bestMaskPattern;
        }

        private static Version ChooseVersion(int numInputBits, ErrorCorrectionLevel ecLevel)
        {
            for (int versionNum = 1; versionNum <= 40; versionNum++)
            {
                var version = Version.GetVersionForNumber(versionNum);
                if (WillFit(numInputBits, version, ecLevel))
                {
                    return version;
                }
            }
            throw new WriterException("Data too big");
        }

        /// <summary></summary>
        /// <returns>true if the number of input bits will fit in a code with the specified version and error correction level.</returns>
        private static bool WillFit(int numInputBits, Version version, ErrorCorrectionLevel ecLevel)
        {
            // In the following comments, we use numbers of Version 7-H.
            // numBytes = 196
            var numBytes = version.TotalCodewords;
            // getNumECBytes = 130
            var ecBlocks = version.GetECBlocksForLevel(ecLevel);
            var numEcBytes = ecBlocks.TotalECCodewords;
            // getNumDataBytes = 196 - 130 = 66
            var numDataBytes = numBytes - numEcBytes;
            var totalInputBytes = (numInputBits + 7) / 8;
            return numDataBytes >= totalInputBytes;
        }

        /// <summary>
        /// Terminate bits as described in 8.4.8 and 8.4.9 of JISX0510:2004 (p.24).
        /// </summary>
        /// <param name="numDataBytes">The num data bytes.</param>
        /// <param name="bits">The bits.</param>
        internal static void TerminateBits(int numDataBytes, BitArray bits)
        {
            var capacity = numDataBytes << 3;
            if (bits.Size > capacity)
            {
                throw new WriterException($"data bits cannot fit in the QR Code{bits.Size} > {capacity}");
            }
            for (int i = 0; i < 4 && bits.Size < capacity; ++i)
            {
                bits.AppendBit(false);
            }
            // Append termination bits. See 8.4.8 of JISX0510:2004 (p.24) for details.
            // If the last byte isn't 8-bit aligned, we'll add padding bits.
            var numBitsInLastByte = bits.Size & 0x07;
            if (numBitsInLastByte > 0)
            {
                for (int i = numBitsInLastByte; i < 8; i++)
                {
                    bits.AppendBit(false);
                }
            }
            // If we have more space, we'll fill the space with padding patterns defined in 8.4.9 (p.24).
            var numPaddingBytes = numDataBytes - bits.SizeInBytes;
            for (int i = 0; i < numPaddingBytes; ++i)
            {
                bits.AppendBits((i & 0x01) == 0 ? 0xEC : 0x11, 8);
            }
            if (bits.Size != capacity)
            {
                throw new WriterException("Bits size does not equal capacity");
            }
        }

        /// <summary>
        /// Get number of data bytes and number of error correction bytes for block id "blockID". Store
        /// the result in "numDataBytesInBlock", and "numECBytesInBlock". See table 12 in 8.5.1 of
        /// JISX0510:2004 (p.30)
        /// </summary>
        /// <param name="numTotalBytes">The num total bytes.</param>
        /// <param name="numDataBytes">The num data bytes.</param>
        /// <param name="numRSBlocks">The num RS blocks.</param>
        /// <param name="blockID">The block ID.</param>
        /// <param name="numDataBytesInBlock">The num data bytes in block.</param>
        /// <param name="numECBytesInBlock">The num EC bytes in block.</param>
        internal static void GetNumDataBytesAndNumECBytesForBlockID(int numTotalBytes, int numDataBytes, int numRSBlocks,
                                                           int blockID, int[] numDataBytesInBlock, int[] numECBytesInBlock)
        {
            if (blockID >= numRSBlocks)
            {
                throw new WriterException("Block ID too large");
            }
            // numRsBlocksInGroup2 = 196 % 5 = 1
            var numRsBlocksInGroup2 = numTotalBytes % numRSBlocks;
            // numRsBlocksInGroup1 = 5 - 1 = 4
            var numRsBlocksInGroup1 = numRSBlocks - numRsBlocksInGroup2;
            // numTotalBytesInGroup1 = 196 / 5 = 39
            var numTotalBytesInGroup1 = numTotalBytes / numRSBlocks;
            // numTotalBytesInGroup2 = 39 + 1 = 40
            var numTotalBytesInGroup2 = numTotalBytesInGroup1 + 1;
            // numDataBytesInGroup1 = 66 / 5 = 13
            var numDataBytesInGroup1 = numDataBytes / numRSBlocks;
            // numDataBytesInGroup2 = 13 + 1 = 14
            var numDataBytesInGroup2 = numDataBytesInGroup1 + 1;
            // numEcBytesInGroup1 = 39 - 13 = 26
            var numEcBytesInGroup1 = numTotalBytesInGroup1 - numDataBytesInGroup1;
            // numEcBytesInGroup2 = 40 - 14 = 26
            var numEcBytesInGroup2 = numTotalBytesInGroup2 - numDataBytesInGroup2;
            // Sanity checks.
            // 26 = 26
            if (numEcBytesInGroup1 != numEcBytesInGroup2)
            {
                throw new WriterException("EC bytes mismatch");
            }
            // 5 = 4 + 1.
            if (numRSBlocks != numRsBlocksInGroup1 + numRsBlocksInGroup2)
            {
                throw new WriterException("RS blocks mismatch");
            }
            // 196 = (13 + 26) * 4 + (14 + 26) * 1
            if (numTotalBytes
                != ((numDataBytesInGroup1 + numEcBytesInGroup1)
                    * numRsBlocksInGroup1)
                    + ((numDataBytesInGroup2 + numEcBytesInGroup2)
                        * numRsBlocksInGroup2))
            {
                throw new WriterException("Total bytes mismatch");
            }

            if (blockID < numRsBlocksInGroup1)
            {
                numDataBytesInBlock[0] = numDataBytesInGroup1;
                numECBytesInBlock[0] = numEcBytesInGroup1;
            }
            else
            {
                numDataBytesInBlock[0] = numDataBytesInGroup2;
                numECBytesInBlock[0] = numEcBytesInGroup2;
            }
        }

        /// <summary>
        /// Interleave "bits" with corresponding error correction bytes. On success, store the result in
        /// "result". The interleave rule is complicated. See 8.6 of JISX0510:2004 (p.37) for details.
        /// </summary>
        /// <param name="bits">The bits.</param>
        /// <param name="numTotalBytes">The num total bytes.</param>
        /// <param name="numDataBytes">The num data bytes.</param>
        /// <param name="numRSBlocks">The num RS blocks.</param>
        /// <returns></returns>
        /// <exception cref="WriterException"></exception>
        internal static BitArray InterleaveWithECBytes(BitArray bits, int numTotalBytes, int numDataBytes, int numRSBlocks)
        {
            // "bits" must have "getNumDataBytes" bytes of data.
            if (bits.SizeInBytes != numDataBytes)
            {
                throw new WriterException("Number of bits and data bytes does not match");
            }

            // Step 1.  Divide data bytes into blocks and generate error correction bytes for them. We'll
            // store the divided data bytes blocks and error correction bytes blocks into "blocks".
            var dataBytesOffset = 0;
            var maxNumDataBytes = 0;
            var maxNumEcBytes = 0;

            // Since, we know the number of reedsolmon blocks, we can initialize the vector with the number.
            var blocks = new List<BlockPair>(numRSBlocks);

            for (int i = 0; i < numRSBlocks; ++i)
            {

                var numDataBytesInBlock = new int[1];
                var numEcBytesInBlock = new int[1];
                GetNumDataBytesAndNumECBytesForBlockID(
                    numTotalBytes, numDataBytes, numRSBlocks, i,
                    numDataBytesInBlock, numEcBytesInBlock);

                var size = numDataBytesInBlock[0];
                var dataBytes = new byte[size];
                bits.ToBytes(8 * dataBytesOffset, dataBytes, 0, size);
                var ecBytes = GenerateECBytes(dataBytes, numEcBytesInBlock[0]);
                blocks.Add(new BlockPair(dataBytes, ecBytes));

                maxNumDataBytes = Math.Max(maxNumDataBytes, size);
                maxNumEcBytes = Math.Max(maxNumEcBytes, ecBytes.Length);
                dataBytesOffset += numDataBytesInBlock[0];
            }
            if (numDataBytes != dataBytesOffset)
            {
                throw new WriterException("Data bytes does not match offset");
            }

            var result = new BitArray();

            // First, place data blocks.
            for (int i = 0; i < maxNumDataBytes; ++i)
            {
                foreach (var block in blocks)
                {
                    var dataBytes = block.DataBytes;
                    if (i < dataBytes.Length)
                    {
                        result.AppendBits(dataBytes[i], 8);
                    }
                }
            }
            // Then, place error correction blocks.
            for (int i = 0; i < maxNumEcBytes; ++i)
            {
                foreach (var block in blocks)
                {
                    var ecBytes = block.ErrorCorrectionBytes;
                    if (i < ecBytes.Length)
                    {
                        result.AppendBits(ecBytes[i], 8);
                    }
                }
            }
            if (numTotalBytes != result.SizeInBytes)
            {  // Should be same.
                throw new WriterException($"Interleaving error: {numTotalBytes} and {result.SizeInBytes} differ.");
            }

            return result;
        }

        internal static byte[] GenerateECBytes(byte[] dataBytes, int numEcBytesInBlock)
        {
            var numDataBytes = dataBytes.Length;
            var toEncode = new int[numDataBytes + numEcBytesInBlock];
            for (int i = 0; i < numDataBytes; i++)
            {
                toEncode[i] = dataBytes[i] & 0xFF;
            }
            new ReedSolomonEncoder(GenericGF.QR_CODE_FIELD_256).Encode(toEncode, numEcBytesInBlock);

            var ecBytes = new byte[numEcBytesInBlock];
            for (int i = 0; i < numEcBytesInBlock; i++)
            {
                ecBytes[i] = (byte)toEncode[numDataBytes + i];
            }
            return ecBytes;
        }

        /// <summary>
        /// Append mode info. On success, store the result in "bits".
        /// </summary>
        /// <param name="mode">The mode.</param>
        /// <param name="bits">The bits.</param>
        internal static void AppendModeInfo(Mode mode, BitArray bits) => bits.AppendBits(mode.Bits, 4);


        /// <summary>
        /// Append length info. On success, store the result in "bits".
        /// </summary>
        /// <param name="numLetters">The num letters.</param>
        /// <param name="version">The version.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="bits">The bits.</param>
        /// <exception cref="WriterException"></exception>
        internal static void AppendLengthInfo(int numLetters, Version version, Mode mode, BitArray bits)
        {
            var numBits = mode.GetCharacterCountBits(version);
            if (numLetters >= (1 << numBits))
            {
                throw new WriterException($"{numLetters} is bigger than {((1 << numBits) - 1)}");
            }
            bits.AppendBits(numLetters, numBits);
        }

        /// <summary>
        /// Append "bytes" in "mode" mode (encoding) into "bits". On success, store the result in "bits".
        /// </summary>
        /// <param name="content">The content.</param>
        /// <param name="mode">The mode.</param>
        /// <param name="bits">The bits.</param>
        /// <param name="encoding">The encoding.</param>
        /// <exception cref="WriterException"></exception>
        internal static void AppendBytes(string content, Mode mode, BitArray bits, string encoding)
        {
            if (mode.Equals(Mode.NUMERIC))
                AppendNumericBytes(content, bits);
            else if (mode.Equals(Mode.ALPHANUMERIC))
                AppendAlphanumericBytes(content, bits);
            else if (mode.Equals(Mode.BYTE))
                Append8BitBytes(content, bits, encoding);
            else if (mode.Equals(Mode.KANJI))
                AppendKanjiBytes(content, bits);
            else
                throw new WriterException($"Invalid mode: {mode}");
        }

        internal static void AppendNumericBytes(string content, BitArray bits)
        {
            var length = content.Length;

            var i = 0;
            while (i < length)
            {
                var num1 = content[i] - '0';
                if (i + 2 < length)
                {
                    // Encode three numeric letters in ten bits.
                    var num2 = content[i + 1] - '0';
                    var num3 = content[i + 2] - '0';
                    bits.AppendBits((num1 * 100) + (num2 * 10) + num3, 10);
                    i += 3;
                }
                else if (i + 1 < length)
                {
                    // Encode two numeric letters in seven bits.
                    var num2 = content[i + 1] - '0';
                    bits.AppendBits((num1 * 10) + num2, 7);
                    i += 2;
                }
                else
                {
                    // Encode one numeric letter in four bits.
                    bits.AppendBits(num1, 4);
                    i++;
                }
            }
        }

        internal static void AppendAlphanumericBytes(string content, BitArray bits)
        {
            var length = content.Length;

            var i = 0;
            while (i < length)
            {
                var code1 = getAlphanumericCode(content[i]);
                if (code1 == -1)
                {
                    throw new WriterException();
                }
                if (i + 1 < length)
                {
                    var code2 = getAlphanumericCode(content[i + 1]);
                    if (code2 == -1)
                    {
                        throw new WriterException();
                    }
                    // Encode two alphanumeric letters in 11 bits.
                    bits.AppendBits((code1 * 45) + code2, 11);
                    i += 2;
                }
                else
                {
                    // Encode one alphanumeric letter in six bits.
                    bits.AppendBits(code1, 6);
                    i++;
                }
            }
        }

        internal static void Append8BitBytes(string content, BitArray bits, string encoding)
        {
            byte[] bytes;
            try
            {
                bytes = Encoding.GetEncoding(encoding).GetBytes(content);
            }
            catch (Exception uee)
            {
                //TODO: why wrap and rethrow?
                throw new WriterException(uee.Message, uee);
            }
            foreach (var b in bytes)
            {
                bits.AppendBits(b, 8);
            }
        }

        internal static void AppendKanjiBytes(string content, BitArray bits)
        {
            byte[] bytes;
            try
            {
                bytes = Encoding.GetEncoding("Shift_JIS").GetBytes(content);
            }
            catch (Exception uee)
            {
                //TODO: why wrap and rethrow?
                throw new WriterException(uee.Message, uee);
            }
            if (bytes.Length % 2 != 0)
            {
                throw new WriterException("Kanji byte size not even");
            }
            var maxI = bytes.Length - 1; // bytes.length must be even
            for (int i = 0; i < maxI; i += 2)
            {
                var byte1 = bytes[i] & 0xFF;
                var byte2 = bytes[i + 1] & 0xFF;
                var code = (byte1 << 8) | byte2;
                var subtracted = -1;
                if (code >= 0x8140 && code <= 0x9ffc)
                {
                    subtracted = code - 0x8140;
                }
                else if (code >= 0xe040 && code <= 0xebbf)
                {
                    subtracted = code - 0xc140;
                }
                if (subtracted == -1)
                {
                    throw new WriterException("Invalid byte sequence");
                }
                var encoded = ((subtracted >> 8) * 0xc0) + (subtracted & 0xff);
                bits.AppendBits(encoded, 13);
            }
        }

        private static void AppendECI(CharacterSetECI eci, BitArray bits)
        {
            bits.AppendBits(Mode.ECI.Bits, 4);

            // This is correct for values up to 127, which is all we need now.
            bits.AppendBits(eci.Value, 8);
        }
    }
}
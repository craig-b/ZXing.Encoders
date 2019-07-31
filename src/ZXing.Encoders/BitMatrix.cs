﻿/*
* Copyright 2007 ZXing authors
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
using System.Text;

namespace ZXing.Encoders
{
    /// <summary>
    ///   <p>Represents a 2D matrix of bits. In function arguments below, and throughout the common
    /// module, x is the column position, and y is the row position. The ordering is always x, y.
    /// The origin is at the top-left.</p>
    ///   <p>Internally the bits are represented in a 1-D array of 32-bit ints. However, each row begins
    /// with a new int. This is done intentionally so that we can copy out a row into a BitArray very
    /// efficiently.</p>
    ///   <p>The ordering of bits is row-major. Within each int, the least significant bits are used first,
    /// meaning they represent lower x values. This is compatible with BitArray's implementation.</p>
    /// </summary>
    /// <author>Sean Owen</author>
    /// <author>dswitkin@google.com (Daniel Switkin)</author>
    public sealed class BitMatrix
    {
        private readonly int[] bits;

        /// <returns> The width of the matrix
        /// </returns>
        public int Width { get; }

        /// <returns> The height of the matrix
        /// </returns>
        public int Height { get; }

        /// <summary> This method is for compatibility with older code. It's only logical to call if the matrix
        /// is square, so I'm throwing if that's not the case.
        /// 
        /// </summary>
        /// <returns> row/column dimension of this matrix
        /// </returns>
        public int Dimension
        {
            get
            {
                if (Width != Height)
                {
                    throw new ArgumentException("Can't call Dimension on a non-square matrix");
                }
                return Width;
            }
        }

        /// <returns>
        /// The rowsize of the matrix
        /// </returns>
        public int RowSize { get; }

        /// <summary>
        /// Creates an empty square <see cref="BitMatrix"/>.
        /// </summary>
        /// <param name="dimension">height and width</param>
        public BitMatrix(int dimension) : this(dimension, dimension) { }

        /// <summary>
        /// Creates an empty square <see cref="BitMatrix"/>.
        /// </summary>
        /// <param name="width">bit matrix width</param>
        /// <param name="height">bit matrix height</param>
        public BitMatrix(int width, int height)
        {
            if (width < 1 || height < 1)
            {
                throw new System.ArgumentException("Both dimensions must be greater than 0");
            }
            Width = width;
            Height = height;
            RowSize = (width + 31) >> 5;
            bits = new int[RowSize * height];
        }

        internal BitMatrix(int width, int height, int rowSize, int[] bits)
        {
            Width = width;
            Height = height;
            RowSize = rowSize;
            this.bits = bits;
        }

        internal BitMatrix(int width, int height, int[] bits)
        {
            Width = width;
            Height = height;
            RowSize = (width + 31) >> 5;
            this.bits = bits;
        }

        /// <summary>
        /// Interprets a 2D array of booleans as a <see cref="BitMatrix"/>, where "true" means an "on" bit.
        /// </summary>
        /// <param name="image">bits of the image, as a row-major 2D array. Elements are arrays representing rows</param>
        /// <returns><see cref="BitMatrix"/> representation of image</returns>
        public static BitMatrix Parse(bool[][] image)
        {
            var height = image.Length;
            var width = image[0].Length;
            var bits = new BitMatrix(width, height);
            for (var i = 0; i < height; i++)
            {
                var imageI = image[i];
                for (var j = 0; j < width; j++)
                {
                    bits[j, i] = imageI[j];
                }
            }
            return bits;
        }

        public static BitMatrix Parse(string stringRepresentation, string setString, string unsetString)
        {
            if (stringRepresentation == null)
            {
                throw new ArgumentException(nameof(stringRepresentation));
            }

            var bits = new bool[stringRepresentation.Length];
            var bitsPos = 0;
            var rowStartPos = 0;
            var rowLength = -1;
            var nRows = 0;
            var pos = 0;
            while (pos < stringRepresentation.Length)
            {
                if (stringRepresentation[pos] == '\n'
                    || stringRepresentation[pos] == '\r')
                {
                    if (bitsPos > rowStartPos)
                    {
                        if (rowLength == -1)
                        {
                            rowLength = bitsPos - rowStartPos;
                        }
                        else if (bitsPos - rowStartPos != rowLength)
                        {
                            throw new ArgumentException("row lengths do not match");
                        }
                        rowStartPos = bitsPos;
                        nRows++;
                    }
                    pos++;
                }
#if NETCOREAPP3_0
                else if (((ReadOnlySpan<char>)stringRepresentation).Slice(pos, setString.Length) == setString)
#else
                else if (stringRepresentation.Substring(pos, setString.Length) == setString)
#endif
                {
                    pos += setString.Length;
                    bits[bitsPos] = true;
                    bitsPos++;
                }
#if NETCOREAPP3_0
                else if (((ReadOnlySpan<char>)stringRepresentation).Slice(pos, unsetString.Length) == unsetString)
#else
                else if (stringRepresentation.Substring(pos, unsetString.Length) == unsetString)
#endif
                {
                    pos += unsetString.Length;
                    bits[bitsPos] = false;
                    bitsPos++;
                }
                else
                {
                    throw new ArgumentException($"illegal character encountered: {stringRepresentation.Substring(pos)}");
                }
            }

            // no EOL at end?
            if (bitsPos > rowStartPos)
            {
                if (rowLength == -1)
                {
                    rowLength = bitsPos - rowStartPos;
                }
                else if (bitsPos - rowStartPos != rowLength)
                {
                    throw new ArgumentException("row lengths do not match");
                }
                nRows++;
            }

            var matrix = new BitMatrix(rowLength, nRows);
            for (int i = 0; i < bitsPos; i++)
            {
                if (bits[i])
                {
                    matrix[i % rowLength, i / rowLength] = true;
                }
            }
            return matrix;
        }

        /// <summary> <p>Gets the requested bit, where true means black.</p>
        /// 
        /// </summary>
        /// <param name="x">The horizontal component (i.e. which column)
        /// </param>
        /// <param name="y">The vertical component (i.e. which row)
        /// </param>
        /// <returns> value of given bit in matrix
        /// </returns>
        public bool this[int x, int y]
        {
            get
            {
                var offset = (y * RowSize) + (x >> 5);
                return (((int)((uint)bits[offset] >> (x & 0x1f))) & 1) != 0;
            }
            set
            {
                if (value)
                {
                    var offset = (y * RowSize) + (x >> 5);
                    bits[offset] |= 1 << (x & 0x1f);
                }
                else
                {
                    var offset = (y * RowSize) + (x / 32);
                    bits[offset] &= ~(1 << (x & 0x1f));
                }
            }
        }

        /// <summary>
        /// <p>Flips the given bit.</p>
        /// </summary>
        /// <param name="x">The horizontal component (i.e. which column)</param>
        /// <param name="y">The vertical component (i.e. which row)</param>
        public void Flip(int x, int y)
        {
            var offset = (y * RowSize) + (x >> 5);
            bits[offset] ^= 1 << (x & 0x1f);
        }

        /// <summary>
        /// flip all of the bits, if shouldBeFlipped is true for the coordinates
        /// </summary>
        /// <param name="shouldBeFlipped">should return true, if the bit at a given coordinate should be flipped</param>
        public void FlipWhen(Func<int, int, bool> shouldBeFlipped)
        {
            for (var y = 0; y < Height; y++)
            {
                for (var x = 0; x < Width; x++)
                {
                    if (shouldBeFlipped(y, x))
                    {
                        var offset = (y * RowSize) + (x >> 5);
                        bits[offset] ^= 1 << (x & 0x1f);
                    }
                }
            }
        }

        /// <summary>
        /// Exclusive-or (XOR): Flip the bit in this <see cref="BitMatrix"/> if the corresponding
        /// mask bit is set.
        /// </summary>
        /// <param name="mask">The mask.</param>
        public void Xor(BitMatrix mask)
        {
            if (Width != mask.Width || Height != mask.Height || RowSize != mask.RowSize)
            {
                throw new ArgumentException("input matrix dimensions do not match", nameof(mask));
            }
            var rowArray = new BitArray(Width);
            for (int y = 0; y < Height; y++)
            {
                int offset = y * RowSize;
                int[] row = mask.GetRow(y, rowArray).Array;
                for (int x = 0; x < RowSize; x++)
                {
                    bits[offset + x] ^= row[x];
                }
            }
        }

        /// <summary> Clears all bits (sets to false).</summary>
        public void Clear()
        {
            var max = bits.Length;
            for (int i = 0; i < max; i++)
            {
                bits[i] = 0;
            }
        }

        /// <summary> <p>Sets a square region of the bit matrix to true.</p>
        /// 
        /// </summary>
        /// <param name="left">The horizontal position to begin at (inclusive)
        /// </param>
        /// <param name="top">The vertical position to begin at (inclusive)
        /// </param>
        /// <param name="width">The width of the region
        /// </param>
        /// <param name="height">The height of the region
        /// </param>
        public void SetRegion(int left, int top, int width, int height)
        {
            if (top < 0 || left < 0)
            {
                throw new ArgumentException("Left and top must be nonnegative");
            }
            if (height < 1 || width < 1)
            {
                throw new ArgumentException("Height and width must be at least 1");
            }
            var right = left + width;
            var bottom = top + height;
            if (bottom > Height || right > Width)
            {
                throw new ArgumentException("The region must fit inside the matrix");
            }
            for (int y = top; y < bottom; y++)
            {
                var offset = y * RowSize;
                for (int x = left; x < right; x++)
                {
                    bits[offset + (x >> 5)] |= 1 << (x & 0x1f);
                }
            }
        }

        /// <summary> A fast method to retrieve one row of data from the matrix as a BitArray.
        /// 
        /// </summary>
        /// <param name="y">The row to retrieve
        /// </param>
        /// <param name="row">An optional caller-allocated BitArray, will be allocated if null or too small
        /// </param>
        /// <returns> The resulting BitArray - this reference should always be used even when passing
        /// your own row
        /// </returns>
        public BitArray GetRow(int y, BitArray row)
        {
            if (row == null || row.Size < Width)
            {
                row = new BitArray(Width);
            }
            else
            {
                row.Clear();
            }
            var offset = y * RowSize;
            for (int x = 0; x < RowSize; x++)
            {
                row.SetBulk(x << 5, bits[offset + x]);
            }
            return row;
        }

        /// <summary>
        /// Sets the row.
        /// </summary>
        /// <param name="y">row to set</param>
        /// <param name="row"><see cref="BitArray"/> to copy from</param>
        public void SetRow(int y, BitArray row) => Array.Copy(row.Array, 0, bits, y * RowSize, RowSize);

        /// <summary>
        /// Modifies this <see cref="BitMatrix"/> to represent the same but rotated 180 degrees
        /// </summary>
        public void Rotate180()
        {
            var width = Width;
            var height = Height;
            var topRow = new BitArray(width);
            var bottomRow = new BitArray(width);
            for (int i = 0; i < (height + 1) / 2; i++)
            {
                topRow = GetRow(i, topRow);
                bottomRow = GetRow(height - 1 - i, bottomRow);
                topRow.Reverse();
                bottomRow.Reverse();
                SetRow(i, bottomRow);
                SetRow(height - 1 - i, topRow);
            }
        }

        /// <summary>
        /// This is useful in detecting the enclosing rectangle of a 'pure' barcode.
        /// </summary>
        /// <returns>{left,top,width,height} enclosing rectangle of all 1 bits, or null if it is all white</returns>
        public int[] GetEnclosingRectangle()
        {
            var left = Width;
            var top = Height;
            var right = -1;
            var bottom = -1;

            for (int y = 0; y < Height; y++)
            {
                for (int x32 = 0; x32 < RowSize; x32++)
                {
                    int theBits = bits[(y * RowSize) + x32];
                    if (theBits != 0)
                    {
                        if (y < top)
                        {
                            top = y;
                        }
                        if (y > bottom)
                        {
                            bottom = y;
                        }
                        if (x32 * 32 < left)
                        {
                            var bit = 0;
                            while ((theBits << (31 - bit)) == 0)
                            {
                                bit++;
                            }
                            if (((x32 * 32) + bit) < left)
                            {
                                left = (x32 * 32) + bit;
                            }
                        }
                        if ((x32 * 32) + 31 > right)
                        {
                            var bit = 31;
                            while (((int)((uint)theBits >> bit)) == 0) // (theBits >>> bit)
                            {
                                bit--;
                            }
                            if (((x32 * 32) + bit) > right)
                            {
                                right = (x32 * 32) + bit;
                            }
                        }
                    }
                }
            }

            if (right < left || bottom < top)
            {
                return null;
            }

            return new[] { left, top, right - left + 1, bottom - top + 1 };
        }

        /// <summary>
        /// This is useful in detecting a corner of a 'pure' barcode.
        /// </summary>
        /// <returns>{x,y} coordinate of top-left-most 1 bit, or null if it is all white</returns>
        public int[] GetTopLeftOnBit()
        {
            var bitsOffset = 0;
            while (bitsOffset < bits.Length && bits[bitsOffset] == 0)
            {
                bitsOffset++;
            }
            if (bitsOffset == bits.Length)
            {
                return null;
            }
            var y = bitsOffset / RowSize;
            var x = (bitsOffset % RowSize) << 5;

            var theBits = bits[bitsOffset];
            var bit = 0;
            while ((theBits << (31 - bit)) == 0)
            {
                bit++;
            }
            x += bit;
            return new[] { x, y };
        }

        public int[] GetBottomRightOnBit()
        {
            var bitsOffset = bits.Length - 1;
            while (bitsOffset >= 0 && bits[bitsOffset] == 0)
            {
                bitsOffset--;
            }
            if (bitsOffset < 0)
            {
                return null;
            }

            var y = bitsOffset / RowSize;
            var x = (bitsOffset % RowSize) << 5;

            var theBits = bits[bitsOffset];
            var bit = 31;

            while (((int)((uint)theBits >> bit)) == 0) // (theBits >>> bit)
            {
                bit--;
            }
            x += bit;

            return new int[] { x, y };
        }

        /// <summary>
        /// Determines whether the specified <see cref="object"/> is equal to this instance.
        /// </summary>
        /// <param name="obj">The <see cref="object"/> to compare with this instance.</param>
        /// <returns>
        ///   <c>true</c> if the specified <see cref="object"/> is equal to this instance; otherwise, <c>false</c>.
        /// </returns>
        public override bool Equals(object obj)
        {
            if (!(obj is BitMatrix other))
            {
                return false;
            }
            if (Width != other.Width || Height != other.Height
                || RowSize != other.RowSize || bits.Length != other.bits.Length)
            {
                return false;
            }
            for (int i = 0; i < bits.Length; i++)
            {
                if (bits[i] != other.bits[i])
                {
                    return false;
                }
            }
            return true;
        }

        /// <summary>
        /// Returns a hash code for this instance.
        /// </summary>
        /// <returns>
        /// A hash code for this instance, suitable for use in hashing algorithms and data structures like a hash table. 
        /// </returns>
        public override int GetHashCode()
        {
            int hash = Width;
            hash = (31 * hash) + Width;
            hash = (31 * hash) + Height;
            hash = (31 * hash) + RowSize;
            foreach (var bit in bits)
            {
                hash = (31 * hash) + bit.GetHashCode();
            }
            return hash;
        }

        /// <summary>
        /// Returns a <see cref="string"/> that represents this instance.
        /// </summary>
        /// <returns>
        /// A <see cref="string"/> that represents this instance.
        /// </returns>
        public override string ToString() => ToString("X ", "  ", Environment.NewLine);

        /// <summary>
        /// Returns a <see cref="string"/> that represents this instance.
        /// </summary>
        /// <param name="setString">The set string.</param>
        /// <param name="unsetString">The unset string.</param>
        /// <returns>
        /// A <see cref="string"/> that represents this instance.
        /// </returns>
        public string ToString(string setString, string unsetString) => BuildToString(setString, unsetString, Environment.NewLine);

        /// <summary>
        /// Returns a <see cref="string"/> that represents this instance.
        /// </summary>
        /// <param name="setString">The set string.</param>
        /// <param name="unsetString">The unset string.</param>
        /// <param name="lineSeparator">The line separator.</param>
        /// <returns>
        /// A <see cref="string"/> that represents this instance.
        /// </returns>
        public string ToString(string setString, string unsetString, string lineSeparator)
            => BuildToString(setString, unsetString, lineSeparator);

        private string BuildToString(string setString, string unsetString, string lineSeparator)
        {
            var result = new StringBuilder(Height * (Width + 1));
            for (int y = 0; y < Height; y++)
            {
                for (int x = 0; x < Width; x++)
                {
                    result.Append(this[x, y] ? setString : unsetString);
                }
                result.Append(lineSeparator);
            }
            return result.ToString();
        }

        /// <summary>
        /// Clones this instance.
        /// </summary>
        /// <returns></returns>
        public BitMatrix Clone()
        {
            var clonedBits = new int[bits.Length];
            Array.Copy(bits, clonedBits, bits.Length);
            return new BitMatrix(Width, Height, RowSize, clonedBits);
        }
    }
}
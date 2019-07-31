/*
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

namespace ZXing.Encoders.ReedSolomon
{
    /// <summary>
    /// <p>Represents a polynomial whose coefficients are elements of a GF.
    /// Instances of this class are immutable.</p>
    /// <p>Much credit is due to William Rucklidge since portions of this code are an indirect
    /// port of his C++ Reed-Solomon implementation.</p>
    /// </summary>
    /// <author>Sean Owen</author>
    internal sealed class GenericGFPoly
    {
        private readonly GenericGF field;

        /// <summary>
        /// Initializes a new instance of the <see cref="GenericGFPoly"/> class.
        /// </summary>
        /// <param name="field">the <see cref="GenericGF"/> instance representing the field to use
        /// to perform computations</param>
        /// <param name="coefficients">coefficients as ints representing elements of GF(size), arranged
        /// from most significant (highest-power term) coefficient to least significant</param>
        /// <exception cref="ArgumentException">if argument is null or empty,
        /// or if leading coefficient is 0 and this is not a
        /// constant polynomial (that is, it is not the monomial "0")</exception>
        internal GenericGFPoly(GenericGF field, int[] coefficients)
        {
            if (coefficients.Length == 0)
            {
                throw new ArgumentException();
            }
            this.field = field;
            var coefficientsLength = coefficients.Length;
            if (coefficientsLength > 1 && coefficients[0] == 0)
            {
                // Leading term must be non-zero for anything except the constant polynomial "0"
                var firstNonZero = 1;
                while (firstNonZero < coefficientsLength && coefficients[firstNonZero] == 0)
                {
                    firstNonZero++;
                }
                if (firstNonZero == coefficientsLength)
                {
                    Coefficients = new int[] { 0 };
                }
                else
                {
                    Coefficients = new int[coefficientsLength - firstNonZero];
                    Array.Copy(coefficients, firstNonZero, Coefficients, 0, Coefficients.Length);
                }
            }
            else
            {
                Coefficients = coefficients;
            }
        }

        internal int[] Coefficients { get; }

        /// <summary>
        /// degree of this polynomial
        /// </summary>
        internal int Degree => Coefficients.Length - 1;

        /// <summary>
        /// Gets a value indicating whether this <see cref="GenericGFPoly"/> is zero.
        /// </summary>
        /// <value>true iff this polynomial is the monomial "0"</value>
        internal bool IsZero => Coefficients[0] == 0;

        /// <summary>
        /// coefficient of x^degree term in this polynomial
        /// </summary>
        /// <param name="degree">The degree.</param>
        /// <returns>coefficient of x^degree term in this polynomial</returns>
        internal int GetCoefficient(int degree) => Coefficients[Coefficients.Length - 1 - degree];

        /// <summary>
        /// evaluation of this polynomial at a given point
        /// </summary>
        /// <param name="a">A.</param>
        /// <returns>evaluation of this polynomial at a given point</returns>
        internal int EvaluateAt(int a)
        {
            var result = 0;
            if (a == 0)
            {
                // Just return the x^0 coefficient
                return GetCoefficient(0);
            }
            if (a == 1)
            {
                // Just the sum of the coefficients
                foreach (var coefficient in Coefficients)
                {
                    result = GenericGF.AddOrSubtract(result, coefficient);
                }
                return result;
            }
            result = Coefficients[0];
            var size = Coefficients.Length;
            for (int i = 1; i < size; i++)
            {
                result = GenericGF.AddOrSubtract(field.Multiply(a, result), Coefficients[i]);
            }
            return result;
        }

        internal GenericGFPoly AddOrSubtract(GenericGFPoly other)
        {
            if (!field.Equals(other.field))
            {
                throw new ArgumentException("GenericGFPolys do not have same GenericGF field");
            }
            if (IsZero)
            {
                return other;
            }
            if (other.IsZero)
            {
                return this;
            }

            var smallerCoefficients = Coefficients;
            var largerCoefficients = other.Coefficients;
            if (smallerCoefficients.Length > largerCoefficients.Length)
            {
                var temp = smallerCoefficients;
                smallerCoefficients = largerCoefficients;
                largerCoefficients = temp;
            }
            var sumDiff = new int[largerCoefficients.Length];
            var lengthDiff = largerCoefficients.Length - smallerCoefficients.Length;
            // Copy high-order terms only found in higher-degree polynomial's coefficients
            Array.Copy(largerCoefficients, 0, sumDiff, 0, lengthDiff);

            for (int i = lengthDiff; i < largerCoefficients.Length; i++)
            {
                sumDiff[i] = GenericGF.AddOrSubtract(smallerCoefficients[i - lengthDiff], largerCoefficients[i]);
            }

            return new GenericGFPoly(field, sumDiff);
        }

        internal GenericGFPoly Multiply(GenericGFPoly other)
        {
            if (!(field == other.field))
            {
                throw new ArgumentException("GenericGFPolys do not have same GenericGF field");
            }
            if (IsZero || other.IsZero)
            {
                return field.Zero;
            }
            var aCoefficients = Coefficients;
            var aLength = aCoefficients.Length;
            var bCoefficients = other.Coefficients;
            var bLength = bCoefficients.Length;
            var product = new int[aLength + bLength - 1];
            for (int i = 0; i < aLength; i++)
            {
                var aCoeff = aCoefficients[i];
                for (int j = 0; j < bLength; j++)
                {
                    product[i + j] = GenericGF.AddOrSubtract(product[i + j], field.Multiply(aCoeff, bCoefficients[j]));
                }
            }
            return new GenericGFPoly(field, product);
        }

        internal GenericGFPoly Multiply(int scalar)
        {
            if (scalar == 0)
            {
                return field.Zero;
            }
            if (scalar == 1)
            {
                return this;
            }
            var size = Coefficients.Length;
            var product = new int[size];
            for (int i = 0; i < size; i++)
            {
                product[i] = field.Multiply(Coefficients[i], scalar);
            }
            return new GenericGFPoly(field, product);
        }

        internal GenericGFPoly MultiplyByMonomial(int degree, int coefficient)
        {
            if (degree < 0)
            {
                throw new ArgumentException();
            }
            if (coefficient == 0)
            {
                return field.Zero;
            }
            var size = Coefficients.Length;
            var product = new int[size + degree];
            for (int i = 0; i < size; i++)
            {
                product[i] = field.Multiply(Coefficients[i], coefficient);
            }
            return new GenericGFPoly(field, product);
        }

        internal GenericGFPoly[] Divide(GenericGFPoly other)
        {
            if (!field.Equals(other.field))
            {
                throw new ArgumentException("GenericGFPolys do not have same GenericGF field");
            }
            if (other.IsZero)
            {
                throw new ArgumentException("Divide by 0");
            }

            var quotient = field.Zero;
            var remainder = this;

            var denominatorLeadingTerm = other.GetCoefficient(other.Degree);
            var inverseDenominatorLeadingTerm = field.Inverse(denominatorLeadingTerm);

            while (remainder.Degree >= other.Degree && !remainder.IsZero)
            {
                var degreeDifference = remainder.Degree - other.Degree;
                var scale = field.Multiply(remainder.GetCoefficient(remainder.Degree), inverseDenominatorLeadingTerm);
                var term = other.MultiplyByMonomial(degreeDifference, scale);
                var iterationQuotient = field.BuildMonomial(degreeDifference, scale);
                quotient = quotient.AddOrSubtract(iterationQuotient);
                remainder = remainder.AddOrSubtract(term);
            }

            return new GenericGFPoly[] { quotient, remainder };
        }

        public override string ToString()
        {
            if (IsZero)
            {
                return "0";
            }
            var result = new StringBuilder(8 * Degree);
            for (int degree = Degree; degree >= 0; degree--)
            {
                var coefficient = GetCoefficient(degree);
                if (coefficient != 0)
                {
                    if (coefficient < 0)
                    {
                        if (degree == Degree)
                        {
                            result.Append("-");
                        }
                        else
                        {
                            result.Append(" - ");
                        }
                        coefficient = -coefficient;
                    }
                    else
                    {
                        if (result.Length > 0)
                        {
                            result.Append(" + ");
                        }
                    }
                    if (degree == 0 || coefficient != 1)
                    {
                        var alphaPower = field.Log(coefficient);
                        if (alphaPower == 0)
                        {
                            result.Append('1');
                        }
                        else if (alphaPower == 1)
                        {
                            result.Append('a');
                        }
                        else
                        {
                            result.Append("a^");
                            result.Append(alphaPower);
                        }
                    }
                    if (degree != 0)
                    {
                        if (degree == 1)
                        {
                            result.Append('x');
                        }
                        else
                        {
                            result.Append("x^");
                            result.Append(degree);
                        }
                    }
                }
            }
            return result.ToString();
        }
    }
}
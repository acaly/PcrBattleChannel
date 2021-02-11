using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.Intrinsics;
using System.Runtime.Intrinsics.X86;
using System.Threading.Tasks;

namespace PcrBattleChannel.Algorithm
{
    public static unsafe class SimdHelper
    {
        private const bool Enabled = true;

        private static float SumVector256(Vector256<float> v)
        {
            v = Avx.HorizontalAdd(v, v); //0+1, 2+3, .., .., 4+5, 6+7, .., ..
            v = Avx.HorizontalAdd(v, v); //0+1+2+3, .., .., .., 4+5+6+7, .., .., ..
            return v.GetUpper().ToScalar() + v.GetLower().ToScalar();
        }

        public static float UpdateValueSum(float* value, float* delta, int count)
        {
            int i = 0;
            float sum = 0f;

            if (Enabled && Avx.IsSupported)
            {
                int simdCount = count & ~7;
                var zero = Vector256.Create(0f);
                var sumVector = zero;
                for (; i < simdCount; i += 8)
                {
                    var v = Avx.LoadAlignedVector256(&value[i]);
                    var d = Avx.LoadAlignedVector256(&delta[i]);
                    v = Avx.Add(v, d);
                    v = Avx.Max(zero, v);
                    Avx.StoreAligned(&value[i], v);
                    sumVector = Avx.Add(v, sumVector);
                }
                sum += SumVector256(sumVector);
            }

            for (; i < count; ++i)
            {
                var newVal = MathF.Max(0, value[i] + delta[i]);
                sum += newVal;
                value[i] = newVal;
            }
            return sum;
        }

        public static void CalculateGradient(float* szvBuffer, int* z1, int* z2, int* z3, float dec, float* output, int count)
        {
            int i = 0;

            if (Enabled && Avx2.IsSupported)
            {
                int simdCount = count & ~7;
                var ndecVector = Vector256.Create(-dec);
                for (; i < simdCount; i += 8)
                {
                    var i1 = Avx.LoadAlignedVector256(&z1[i]);
                    var g1 = Avx2.GatherVector256(szvBuffer, i1, 4);
                    var g = Avx.Add(ndecVector, g1);

                    var i2 = Avx.LoadAlignedVector256(&z2[i]);
                    var g2 = Avx2.GatherVector256(szvBuffer, i2, 4);
                    g = Avx.Add(g, g2);

                    var i3 = Avx.LoadAlignedVector256(&z3[i]);
                    var g3 = Avx2.GatherVector256(szvBuffer, i3, 4);
                    g = Avx.Add(g, g3);

                    Avx.StoreAligned(&output[i], g);
                }
            }

            for (; i < count; ++i)
            {
                var g = 0f;
                g += szvBuffer[z1[i]];
                g += szvBuffer[z2[i]];
                g += szvBuffer[z3[i]];
                output[i] = g - dec;
            }
        }

        public static void MultiplyConst(float* buffer, float value, int count)
        {
            int i = 0;

            if (Enabled && Avx2.IsSupported)
            {
                int simdCount = count & ~7;
                var valueVector = Vector256.Create(value);
                for (; i < simdCount; i += 8)
                {
                    var v = Avx.LoadAlignedVector256(&buffer[i]);
                    v = Avx.Multiply(v, valueVector);
                    Avx.StoreAligned(&buffer[i], v);
                }
            }

            for (; i < count; ++i)
            {
                buffer[i] *= value;
            }
        }
    }
}

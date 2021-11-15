using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Collections.Tests;

internal class UnsafeBitArrayTests
{
    [Test]
    public void UnsafeBitArray_Get_Set()
    {
        var numBits = 256;

        var test = new UnsafeBitArray(numBits, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        Assert.False(test.IsSet(123));
        test.Set(123, true);
        Assert.True(test.IsSet(123));

        Assert.False(test.TestAll(0, numBits));
        Assert.False(test.TestNone(0, numBits));
        Assert.True(test.TestAny(0, numBits));
        Assert.AreEqual(1, test.CountBits(0, numBits));

        Assert.False(test.TestAll(0, 122));
        Assert.True(test.TestNone(0, 122));
        Assert.False(test.TestAny(0, 122));

        test.Clear();
        Assert.False(test.IsSet(123));
        Assert.AreEqual(0, test.CountBits(0, numBits));

        test.SetBits(40, true, 4);
        Assert.AreEqual(4, test.CountBits(0, numBits));

        test.SetBits(0, true, numBits);
        Assert.False(test.TestNone(0, numBits));
        Assert.True(test.TestAll(0, numBits));

        test.SetBits(0, false, numBits);
        Assert.True(test.TestNone(0, numBits));
        Assert.False(test.TestAll(0, numBits));

        test.SetBits(123, true, 7);
        Assert.True(test.TestAll(123, 7));

        test.Clear();
        test.SetBits(64, true, 64);
        Assert.AreEqual(false, test.IsSet(63));
        Assert.AreEqual(true, test.TestAll(64, 64));
        Assert.AreEqual(false, test.IsSet(128));
        Assert.AreEqual(64, test.CountBits(64, 64));
        Assert.AreEqual(64, test.CountBits(0, numBits));

        test.Clear();
        test.SetBits(65, true, 62);
        Assert.AreEqual(false, test.IsSet(64));
        Assert.AreEqual(true, test.TestAll(65, 62));
        Assert.AreEqual(false, test.IsSet(127));
        Assert.AreEqual(62, test.CountBits(64, 64));
        Assert.AreEqual(62, test.CountBits(0, numBits));

        test.Clear();
        test.SetBits(66, true, 64);
        Assert.AreEqual(false, test.IsSet(65));
        Assert.AreEqual(true, test.TestAll(66, 64));
        Assert.AreEqual(false, test.IsSet(130));
        Assert.AreEqual(64, test.CountBits(66, 64));
        Assert.AreEqual(64, test.CountBits(0, numBits));

        test.Dispose();
    }

    [Test]
    public unsafe void UnsafeBitArray_Throws()
    {
        var numBits = 256;

        using (var test = new UnsafeBitArray(numBits, Allocator.Persistent, NativeArrayOptions.ClearMemory))
        {
            Assert.DoesNotThrow(() => { test.TestAll(0, numBits); });
            Assert.DoesNotThrow(() => { test.TestAny(numBits - 1, numBits); });

            Assert.Throws<ArgumentException>(() => { test.IsSet(-1); });
            Assert.Throws<ArgumentException>(() => { test.IsSet(numBits); });
            Assert.Throws<ArgumentException>(() => { test.TestAny(0, 0); });
            Assert.Throws<ArgumentException>(() => { test.TestAny(numBits, 1); });
            Assert.Throws<ArgumentException>(() => { test.TestAny(numBits - 1, 0); });

            // GetBits numBits must be 1-64.
            Assert.Throws<ArgumentException>(() => { test.GetBits(0, 0); });
            Assert.Throws<ArgumentException>(() => { test.GetBits(0, 65); });
            Assert.DoesNotThrow(() => { test.GetBits(63, 2); });

            Assert.Throws<ArgumentException>(() => { new UnsafeBitArray(null, 7); /* check sizeInBytes must be multiple of 8-bytes. */ });
        }
    }

    static void GetBitsTest(ref UnsafeBitArray test, int pos, int numBits)
    {
        test.SetBits(pos, true, numBits);
        Assert.AreEqual(numBits, test.CountBits(0, test.Length));
        Assert.AreEqual(0xfffffffffffffffful >> (64 - numBits), test.GetBits(pos, numBits));
        test.Clear();
    }

    [Test]
    public void UnsafeBitArray_GetBits()
    {
        var numBits = 256;

        var test = new UnsafeBitArray(numBits, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        GetBitsTest(ref test, 0, 5);
        GetBitsTest(ref test, 1, 3);
        GetBitsTest(ref test, 0, 63);
        GetBitsTest(ref test, 0, 64);
        GetBitsTest(ref test, 1, 63);
        GetBitsTest(ref test, 1, 64);
        GetBitsTest(ref test, 62, 5);
        GetBitsTest(ref test, 127, 3);
        GetBitsTest(ref test, 250, 6);
        GetBitsTest(ref test, 254, 2);

        test.Dispose();
    }

    static void SetBitsTest(ref UnsafeBitArray test, int pos, ulong value, int numBits)
    {
        test.SetBits(pos, value, numBits);
        Assert.AreEqual(value, test.GetBits(pos, numBits));
        test.Clear();
    }

    [Test]
    public void UnsafeBitArray_SetBits()
    {
        var numBits = 256;

        var test = new UnsafeBitArray(numBits, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        SetBitsTest(ref test, 0, 16, 5);
        SetBitsTest(ref test, 1, 7, 3);
        SetBitsTest(ref test, 1, 32, 64);
        SetBitsTest(ref test, 62, 6, 5);
        SetBitsTest(ref test, 127, 1, 3);
        SetBitsTest(ref test, 60, 0xaa, 8);

        test.Dispose();
    }

    static void CopyBitsTest(ref UnsafeBitArray test, int dstPos, int srcPos, int numBits)
    {
        for (int pos = 0; pos < test.Length; pos += 64)
        {
            test.SetBits(pos, 0xaaaaaaaaaaaaaaaaul, 64);
        }

        test.SetBits(srcPos, true, numBits);
        test.Copy(dstPos, srcPos, numBits);
        Assert.AreEqual(true, test.TestAll(dstPos, numBits));

        for (int pos = 0; pos < test.Length; ++pos)
        {
            if ((pos >= dstPos && pos < dstPos + numBits) ||
                (pos >= srcPos && pos < srcPos + numBits))
            {
                Assert.AreEqual(true, test.IsSet(pos));
            }
            else
            {
                Assert.AreEqual((0 != (pos & 1)), test.IsSet(pos));
            }
        }

        test.Clear();
    }

    [Test]
    public void UnsafeBitArray_Copy()
    {
        var numBits = 512;

        var test = new UnsafeBitArray(numBits, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        CopyBitsTest(ref test, 1, 16, 12); // short up to 64-bits copy
        CopyBitsTest(ref test, 1, 80, 63); // short up to 64-bits copy
        CopyBitsTest(ref test, 1, 11, 12); // short up to 64-bits copy overlapped
        CopyBitsTest(ref test, 11, 1, 12); // short up to 64-bits copy overlapped

        CopyBitsTest(ref test, 1, 16, 76); // short up to 128-bits copy
        CopyBitsTest(ref test, 1, 80, 127); // short up to 128-bits copy
        CopyBitsTest(ref test, 1, 11, 76); // short up to 128-bits copy overlapped
        CopyBitsTest(ref test, 11, 1, 76); // short up to 128-bits copy overlapped

        CopyBitsTest(ref test, 1, 81, 255); // long copy aligned
        CopyBitsTest(ref test, 8, 0, 255); // long copy overlapped aligned
        CopyBitsTest(ref test, 1, 80, 255); // long copy unaligned
        CopyBitsTest(ref test, 80, 1, 255); // long copy overlapped unaligned

        test.Dispose();
    }

    [Test]
    public unsafe void UnsafeBitArray_Copy_Throws()
    {
        var numBits = 512;

        var test = new UnsafeBitArray(numBits, Allocator.Persistent, NativeArrayOptions.ClearMemory);

        Assert.Throws<ArgumentException>(() => { CopyBitsTest(ref test, 0, numBits - 1, 16); }); // short up to 64-bits copy out of bounds
        Assert.Throws<ArgumentException>(() => { CopyBitsTest(ref test, numBits - 1, 0, 16); }); // short up to 64-bits copy out of bounds

        Assert.Throws<ArgumentException>(() => { CopyBitsTest(ref test, 0, numBits - 1, 80); }); // short up to 128-bits copy out of bounds
        Assert.Throws<ArgumentException>(() => { CopyBitsTest(ref test, numBits - 1, 0, 80); }); // short up to 128-bits copy out of bounds

        Assert.Throws<ArgumentException>(() => { CopyBitsTest(ref test, 1, numBits - 7, 127); }); // long copy aligned
        Assert.Throws<ArgumentException>(() => { CopyBitsTest(ref test, numBits - 7, 1, 127); }); // long copy aligned

        Assert.Throws<ArgumentException>(() => { CopyBitsTest(ref test, 2, numBits - 1, 127); }); // long copy unaligned
        Assert.Throws<ArgumentException>(() => { CopyBitsTest(ref test, numBits - 1, 2, 127); }); // long copy unaligned

        test.Dispose();
    }
}

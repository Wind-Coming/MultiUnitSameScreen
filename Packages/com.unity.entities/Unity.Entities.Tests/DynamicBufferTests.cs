using System;
using System.Diagnostics;
using NUnit.Framework;

namespace Unity.Entities.Tests
{
    public class DynamicBufferTests : ECSTestsFixture
    {
        [DebuggerDisplay("Value: {Value}")]
        struct DynamicBufferElement : IBufferElementData
        {
            public DynamicBufferElement(int value)
            {
                Value = value;
            }

            public int Value;
        }

        [Test]
        public void CopyFromDynamicBuffer([Values(0, 1, 2, 3, 64)] int srcBufferLength)
        {
            var srcEntity = m_Manager.CreateEntity(typeof(DynamicBufferElement));
            var dstEntity = m_Manager.CreateEntity(typeof(DynamicBufferElement));
            var src = m_Manager.GetBuffer<DynamicBufferElement>(srcEntity);
            var dst = m_Manager.GetBuffer<DynamicBufferElement>(dstEntity);

            src.EnsureCapacity(srcBufferLength);
            for (var i = 0; i < srcBufferLength; ++i)
            {
                src.Add(new DynamicBufferElement() {Value = i});
            }

            dst.EnsureCapacity(2);

            for (var i = 0; i < 2; ++i)
            {
                dst.Add(new DynamicBufferElement() {Value = 0});
            }

            Assert.DoesNotThrow(() => dst.CopyFrom(src));

            Assert.AreEqual(src.Length, dst.Length);

            for (var i = 0; i < src.Length; ++i)
            {
                Assert.AreEqual(i, src[i].Value);
                Assert.AreEqual(src[i].Value, dst[i].Value);
            }
        }

        [Test]
        public void CopyFromArray([Values(0, 1, 2, 3, 64)] int srcBufferLength)
        {
            var dstEntity = m_Manager.CreateEntity(typeof(DynamicBufferElement));
            var src = new DynamicBufferElement[srcBufferLength];
            var dst = m_Manager.GetBuffer<DynamicBufferElement>(dstEntity);

            for (var i = 0; i < srcBufferLength; ++i)
            {
                src[i] = new DynamicBufferElement() {Value = i};
            }

            dst.EnsureCapacity(2);

            for (var i = 0; i < 2; ++i)
            {
                dst.Add(new DynamicBufferElement() {Value = 0});
            }

            Assert.DoesNotThrow(() => dst.CopyFrom(src));

            Assert.AreEqual(src.Length, dst.Length);

            for (var i = 0; i < src.Length; ++i)
            {
                Assert.AreEqual(i, src[i].Value);
                Assert.AreEqual(src[i].Value, dst[i].Value);
            }
        }

        [Test]
        public void CopyFromDynamicBufferToEmptyDestination()
        {
            var srcEntity = m_Manager.CreateEntity(typeof(DynamicBufferElement));
            var dstEntity = m_Manager.CreateEntity(typeof(DynamicBufferElement));
            var src = m_Manager.GetBuffer<DynamicBufferElement>(srcEntity);
            var dst = m_Manager.GetBuffer<DynamicBufferElement>(dstEntity);

            src.EnsureCapacity(64);
            for (var i = 0; i < 64; ++i)
            {
                src.Add(new DynamicBufferElement() {Value = i});
            }

            Assert.DoesNotThrow(() => dst.CopyFrom(src));

            Assert.AreEqual(src.Length, dst.Length);

            for (var i = 0; i < src.Length; ++i)
            {
                Assert.AreEqual(i, src[i].Value);
                Assert.AreEqual(src[i].Value, dst[i].Value);
            }
        }

        [Test]
        public void CopyFromNullSource()
        {
            var dstEntity = m_Manager.CreateEntity(typeof(DynamicBufferElement));
            var dst = m_Manager.GetBuffer<DynamicBufferElement>(dstEntity);

            Assert.Throws<ArgumentNullException>(() => dst.CopyFrom(null));
        }

        [Test]
        public void SetCapacity()
        {
            var dstEntity = m_Manager.CreateEntity(typeof(DynamicBufferElement));
            var dst = m_Manager.GetBuffer<DynamicBufferElement>(dstEntity);
            dst.Add(new DynamicBufferElement(){Value = 0});
            dst.Add(new DynamicBufferElement(){Value = 1});
            dst.Capacity = 100;
            Assert.AreEqual(100, dst.Capacity);
            Assert.AreEqual(dst[0], new DynamicBufferElement(){Value = 0});
            Assert.AreEqual(dst[1], new DynamicBufferElement(){Value = 1});
        }

        [Test]
        public void SetCapacitySmallerThanLengthThrows()
        {
            var dstEntity = m_Manager.CreateEntity(typeof(DynamicBufferElement));
            var dst = m_Manager.GetBuffer<DynamicBufferElement>(dstEntity);
            dst.Add(new DynamicBufferElement(){Value = 0});
            dst.Add(new DynamicBufferElement(){Value = 1});
            Assert.Throws<InvalidOperationException>(() => dst.Capacity = 1);
        }

        [Test]
        public void SetCapacitySmallerActuallyShrinksBuffer()
        {
            var dstEntity = m_Manager.CreateEntity(typeof(DynamicBufferElement));
            var dst = m_Manager.GetBuffer<DynamicBufferElement>(dstEntity);
            dst.Capacity = 1000;
            Assert.AreEqual(1000, dst.Capacity);
            dst.Capacity = 100;
            Assert.AreEqual(100, dst.Capacity);
        }

        [Test]
        public void SetCapacityZeroWorks()
        {
            var dstEntity = m_Manager.CreateEntity(typeof(DynamicBufferElement));
            var dst = m_Manager.GetBuffer<DynamicBufferElement>(dstEntity);
            dst.Capacity = 0;
            Assert.AreEqual(0, dst.Capacity);
            dst.Capacity = 100;
            Assert.AreEqual(100, dst.Capacity);
            dst.Capacity = 0;
            Assert.AreEqual(0, dst.Capacity);
        }

        [Test]
        public unsafe void DynamicBuffer_GetUnsafePtr_ReadOnlyAndReadWriteAreEqual()
        {
            var ent = m_Manager.CreateEntity(typeof(DynamicBufferElement));
            var buf = m_Manager.GetBuffer<DynamicBufferElement>(ent);
            Assert.AreEqual((UIntPtr)buf.GetUnsafePtr(), (UIntPtr)buf.GetUnsafeReadOnlyPtr());
        }

#if !UNITY_DOTSPLAYER

        // @TODO: when 2019.1 support is dropped this can be shared with the collections tests:
        // until then the package validation will fail otherwise when collections is not marked testable
        // since we can not have shared test code between packages in 2019.1
        static class GCAllocRecorderForDynamicBuffer
        {
            static UnityEngine.Profiling.Recorder AllocRecorder;

            static GCAllocRecorderForDynamicBuffer()
            {
                AllocRecorder = UnityEngine.Profiling.Recorder.Get("GC.Alloc.DynamicBuffer");
            }

            static int CountGCAllocs(Action action)
            {
                AllocRecorder.FilterToCurrentThread();
                AllocRecorder.enabled = false;
                AllocRecorder.enabled = true;

                action();

                AllocRecorder.enabled = false;
                return AllocRecorder.sampleBlockCount;
            }

            // NOTE: action is called twice to warmup any GC allocs that can happen due to static constructors etc.
            public static void ValidateNoGCAllocs(Action action)
            {
                CountGCAllocs(action);

                var count = CountGCAllocs(action);
                if (count != 0)
                    throw new AssertionException($"Expected 0 GC allocations but there were {count}");
            }
        }

        [Test]
        public void DynamicBufferForEach()
        {
            var dstEntity = m_Manager.CreateEntity(typeof(DynamicBufferElement));
            var buf = m_Manager.GetBuffer<DynamicBufferElement>(dstEntity);
            buf.Add(new DynamicBufferElement(3));
            buf.Add(new DynamicBufferElement(5));

            int count = 0, sum = 0;
            GCAllocRecorderForDynamicBuffer.ValidateNoGCAllocs(() =>
            {
                count = 0;
                sum = 0;
                foreach (var value in buf)
                {
                    sum += value.Value;
                    count++;
                }
            });
            Assert.AreEqual(2, count);
            Assert.AreEqual(8, sum);
        }

#endif
    }
}

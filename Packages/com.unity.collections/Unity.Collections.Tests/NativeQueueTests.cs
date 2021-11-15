using System;
using System.Text.RegularExpressions;
using NUnit.Framework;
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.TestTools;


internal class NativeQueueTests
{
    [Test]
    public void Enqueue_Dequeue()
    {
        var queue = new NativeQueue<int>(Allocator.Temp);
        Assert.AreEqual(0, queue.Count);
        Assert.Throws<System.InvalidOperationException>(() => {queue.Dequeue(); });
        for (int i = 0; i < 16; ++i)
            queue.Enqueue(i);
        Assert.AreEqual(16, queue.Count);
        for (int i = 0; i < 16; ++i)
            Assert.AreEqual(i, queue.Dequeue(), "Got the wrong value from the queue");
        Assert.AreEqual(0, queue.Count);
        Assert.Throws<System.InvalidOperationException>(() => {queue.Dequeue(); });
        queue.Dispose();
    }

    [Test]
    public void ConcurrentEnqueue_Dequeue()
    {
        var queue = new NativeQueue<int>(Allocator.Temp);
        var cQueue = queue.AsParallelWriter();
        Assert.AreEqual(0, queue.Count);
        Assert.Throws<System.InvalidOperationException>(() => {queue.Dequeue(); });
        for (int i = 0; i < 16; ++i)
            cQueue.Enqueue(i);
        Assert.AreEqual(16, queue.Count);
        for (int i = 0; i < 16; ++i)
            Assert.AreEqual(i, queue.Dequeue(), "Got the wrong value from the queue");
        Assert.AreEqual(0, queue.Count);
        Assert.Throws<System.InvalidOperationException>(() => {queue.Dequeue(); });
        queue.Dispose();
    }

    [Test]
    public void Enqueue_Dequeue_Peek()
    {
        var queue = new NativeQueue<int>(Allocator.Temp);
        Assert.AreEqual(0, queue.Count);
        Assert.Throws<System.InvalidOperationException>(() => {queue.Dequeue(); });
        for (int i = 0; i < 16; ++i)
            queue.Enqueue(i);
        Assert.AreEqual(16, queue.Count);
        for (int i = 0; i < 16; ++i)
        {
            Assert.AreEqual(i, queue.Peek(), "Got the wrong value from the queue");
            queue.Dequeue();
        }
        Assert.AreEqual(0, queue.Count);
        Assert.Throws<System.InvalidOperationException>(() => {queue.Dequeue(); });
        queue.Dispose();
    }

    [Test]
    public void Enqueue_Dequeue_Clear()
    {
        var queue = new NativeQueue<int>(Allocator.Temp);
        Assert.AreEqual(0, queue.Count);
        Assert.Throws<System.InvalidOperationException>(() => {queue.Dequeue(); });
        for (int i = 0; i < 16; ++i)
            queue.Enqueue(i);
        Assert.AreEqual(16, queue.Count);
        for (int i = 0; i < 8; ++i)
            Assert.AreEqual(i, queue.Dequeue(), "Got the wrong value from the queue");
        Assert.AreEqual(8, queue.Count);
        queue.Clear();
        Assert.AreEqual(0, queue.Count);
        Assert.Throws<System.InvalidOperationException>(() => {queue.Dequeue(); });
        queue.Dispose();
    }

    [Test]
    public void Double_Deallocate_Throws()
    {
        var queue = new NativeQueue<int>(Allocator.TempJob);
        queue.Dispose();
        Assert.Throws<System.InvalidOperationException>(() => { queue.Dispose(); });
    }

    [Test]
    public void EnqueueScalability()
    {
        var queue = new NativeQueue<int>(Allocator.Persistent);
        for (int i = 0; i != 1000 * 100; i++)
        {
            queue.Enqueue(i);
        }

        Assert.AreEqual(1000 * 100, queue.Count);

        for (int i = 0; i != 1000 * 100; i++)
            Assert.AreEqual(i, queue.Dequeue());
        Assert.AreEqual(0, queue.Count);

        queue.Dispose();
    }

    [Test]
    public void Enqueue_Wrap()
    {
        var queue = new NativeQueue<int>(Allocator.Temp);
        Assert.AreEqual(0, queue.Count);
        Assert.Throws<System.InvalidOperationException>(() => {queue.Dequeue(); });
        for (int i = 0; i < 256; ++i)
            queue.Enqueue(i);
        Assert.AreEqual(256, queue.Count);
        for (int i = 0; i < 128; ++i)
            Assert.AreEqual(i, queue.Dequeue(), "Got the wrong value from the queue");
        Assert.AreEqual(128, queue.Count);
        for (int i = 0; i < 128; ++i)
            queue.Enqueue(i);
        Assert.AreEqual(256, queue.Count);
        for (int i = 128; i < 256; ++i)
            Assert.AreEqual(i, queue.Dequeue(), "Got the wrong value from the queue");
        Assert.AreEqual(128, queue.Count);
        for (int i = 0; i < 128; ++i)
            Assert.AreEqual(i, queue.Dequeue(), "Got the wrong value from the queue");
        Assert.AreEqual(0, queue.Count);
        Assert.Throws<System.InvalidOperationException>(() => {queue.Dequeue(); });
        queue.Dispose();
    }

    [Test]
    public void ConcurrentEnqueue_Wrap()
    {
        var queue = new NativeQueue<int>(Allocator.Temp);
        var cQueue = queue.AsParallelWriter();
        Assert.AreEqual(0, queue.Count);
        Assert.Throws<System.InvalidOperationException>(() => {queue.Dequeue(); });
        for (int i = 0; i < 256; ++i)
            cQueue.Enqueue(i);
        Assert.AreEqual(256, queue.Count);
        for (int i = 0; i < 128; ++i)
            Assert.AreEqual(i, queue.Dequeue(), "Got the wrong value from the queue");
        Assert.AreEqual(128, queue.Count);
        for (int i = 0; i < 128; ++i)
            cQueue.Enqueue(i);
        Assert.AreEqual(256, queue.Count);
        for (int i = 128; i < 256; ++i)
            Assert.AreEqual(i, queue.Dequeue(), "Got the wrong value from the queue");
        Assert.AreEqual(128, queue.Count);
        for (int i = 0; i < 128; ++i)
            Assert.AreEqual(i, queue.Dequeue(), "Got the wrong value from the queue");
        Assert.AreEqual(0, queue.Count);
        Assert.Throws<System.InvalidOperationException>(() => {queue.Dequeue(); });
        queue.Dispose();
    }

    [Test]
    public void NativeQueue_DisposeJob()
    {
        var container = new NativeQueue<int>(Allocator.Persistent);
        Assert.True(container.IsCreated);
        Assert.DoesNotThrow(() => { container.Enqueue(0); });

        var disposeJob = container.Dispose(default);
        Assert.False(container.IsCreated);
        Assert.Throws<InvalidOperationException>(() => { container.Enqueue(0); });

        disposeJob.Complete();
    }

    [Test]
    public void TryDequeue_OnEmptyQueueWhichHadElements_RetainsValidState()
    {
        using (var queue = new NativeQueue<int>(Allocator.Temp))
        {
            for (int i = 0; i < 3; i++)
            {
                queue.Enqueue(i);
                Assert.AreEqual(1, queue.Count);
                int value;
                while (queue.TryDequeue(out value))
                {
                    Assert.AreEqual(i, value);
                }
                Assert.AreEqual(0, queue.Count);
            }
        }
    }

    [Test]
    public void TryDequeue_OnEmptyQueue_RetainsValidState()
    {
        using (var queue = new NativeQueue<int>(Allocator.Temp))
        {
            Assert.IsFalse(queue.TryDequeue(out _));
            queue.Enqueue(1);
            Assert.AreEqual(1, queue.Count);
        }
    }

    [Test]
    public void ToArray_ContainsCorrectElements()
    {
        using (var queue = new NativeQueue<int>(Allocator.Temp))
        {
            for (int i = 0; i < 100; i++)
                queue.Enqueue(i);
            using (var array = queue.ToArray(Allocator.Temp))
            {
                Assert.AreEqual(queue.Count, array.Length);
                for (int i = 0; i < array.Length; i++)
                    Assert.AreEqual(i, array[i]);
            }
        }
    }

    [Test]
    public void ToArray_RespectsDequeue()
    {
        using (var queue = new NativeQueue<int>(Allocator.Temp))
        {
            for (int i = 0; i < 100; i++)
                queue.Enqueue(i);
            for (int i = 0; i < 50; i++)
                queue.Dequeue();
            using (var array = queue.ToArray(Allocator.Temp))
            {
                Assert.AreEqual(queue.Count, array.Length);
                for (int i = 0; i < array.Length; i++)
                    Assert.AreEqual(50 + i, array[i]);
            }
        }
    }

#if UNITY_2020_1_OR_NEWER
    [Test]
    public void NativeQueue_UseAfterFree_UsesCustomOwnerTypeName()
    {
        var container = new NativeQueue<int>(Allocator.TempJob);
        container.Enqueue(123);
        container.Dispose();
        Assert.That(() => container.Dequeue(), Throws.InvalidOperationException.With.Message.Contains($"The {container.GetType()} has been deallocated"));
    }

#endif
}

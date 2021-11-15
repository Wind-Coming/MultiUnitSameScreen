using System;
using System.Runtime.InteropServices;
using System.Threading;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Burst;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Unity.Collections
{
    unsafe struct NativeQueueBlockHeader
    {
        public NativeQueueBlockHeader* m_NextBlock;
        public int m_NumItems;
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct NativeQueueBlockPoolData
    {
        internal IntPtr m_FirstBlock;
        internal int m_NumBlocks;
        internal int m_MaxBlocks;
        internal const int m_BlockSize = 16 * 1024;
        internal int m_AllocLock;

        public NativeQueueBlockHeader* AllocateBlock()
        {
            // There can only ever be a single thread allocating an entry from the free list since it needs to
            // access the content of the block (the next pointer) before doing the CAS.
            // If there was no lock thread A could read the next pointer, thread B could quickly allocate
            // the same block then free it with another next pointer before thread A performs the CAS which
            // leads to an invalid free list potentially causing memory corruption.
            // Having multiple threads freeing data concurrently to each other while another thread is allocating
            // is no problems since there is only ever a single thread modifying global data in that case.
            while (Interlocked.CompareExchange(ref m_AllocLock, 1, 0) != 0)
            {
            }

            NativeQueueBlockHeader* checkBlock = (NativeQueueBlockHeader*)m_FirstBlock;
            NativeQueueBlockHeader* block;

            do
            {
                block = checkBlock;
                if (block == null)
                {
                    Interlocked.Exchange(ref m_AllocLock, 0);
                    Interlocked.Increment(ref m_NumBlocks);
                    block = (NativeQueueBlockHeader*)UnsafeUtility.Malloc(m_BlockSize, 16, Allocator.Persistent);
                    return block;
                }

                checkBlock = (NativeQueueBlockHeader*)Interlocked.CompareExchange(ref m_FirstBlock, (IntPtr)block->m_NextBlock, (IntPtr)block);
            }
            while (checkBlock != block);

            Interlocked.Exchange(ref m_AllocLock, 0);

            return block;
        }

        public void FreeBlock(NativeQueueBlockHeader* block)
        {
            if (m_NumBlocks > m_MaxBlocks)
            {
                if (Interlocked.Decrement(ref m_NumBlocks) + 1 > m_MaxBlocks)
                {
                    UnsafeUtility.Free(block, Allocator.Persistent);
                    return;
                }

                Interlocked.Increment(ref m_NumBlocks);
            }

            NativeQueueBlockHeader* checkBlock = (NativeQueueBlockHeader*)m_FirstBlock;
            NativeQueueBlockHeader* nextPtr;

            do
            {
                nextPtr = checkBlock;
                block->m_NextBlock = checkBlock;
                checkBlock = (NativeQueueBlockHeader*)Interlocked.CompareExchange(ref m_FirstBlock, (IntPtr)block, (IntPtr)checkBlock);
            }
            while (checkBlock != nextPtr);
        }
    }

    internal unsafe static class NativeQueueBlockPool
    {
        static NativeQueueBlockPoolData* pData;

        public static NativeQueueBlockPoolData* QueueBlockPool
        {
            get
            {
                if (pData == null)
                {
                    pData = (NativeQueueBlockPoolData*)UnsafeUtility.Malloc(UnsafeUtility.SizeOf<NativeQueueBlockPoolData>(), 8, Allocator.Persistent);
                    ref var data = ref *pData;
                    data.m_NumBlocks = data.m_MaxBlocks = 256;
                    data.m_AllocLock = 0;
                    // Allocate MaxBlocks items
                    NativeQueueBlockHeader* prev = null;

                    for (int i = 0; i < data.m_MaxBlocks; ++i)
                    {
                        NativeQueueBlockHeader* block = (NativeQueueBlockHeader*)UnsafeUtility.Malloc(NativeQueueBlockPoolData.m_BlockSize, 16, Allocator.Persistent);
                        block->m_NextBlock = prev;
                        prev = block;
                    }
                    data.m_FirstBlock = (IntPtr)prev;
#if !NET_DOTS
                    AppDomain.CurrentDomain.DomainUnload += OnDomainUnload;
#endif
                }
                return pData;
            }
        }

#if !NET_DOTS
        static void OnDomainUnload(object sender, EventArgs e)
        {
            ref var data = ref *pData;
            while (data.m_FirstBlock != IntPtr.Zero)
            {
                NativeQueueBlockHeader* block = (NativeQueueBlockHeader*)data.m_FirstBlock;
                data.m_FirstBlock = (IntPtr)block->m_NextBlock;
                UnsafeUtility.Free(block, Allocator.Persistent);
                --data.m_NumBlocks;
            }
            UnsafeUtility.Free(pData, Allocator.Persistent);
            pData = null;
        }

#endif
    }

    [StructLayout(LayoutKind.Sequential)]
    internal unsafe struct NativeQueueData
    {
        public IntPtr m_FirstBlock;
        public IntPtr m_LastBlock;
        public int    m_MaxItems;
        public int    m_CurrentRead;
        public byte*  m_CurrentWriteBlockTLS;

        internal NativeQueueBlockHeader* GetCurrentWriteBlockTLS(int threadIndex)
        {
            var data = (NativeQueueBlockHeader**)&m_CurrentWriteBlockTLS[threadIndex * JobsUtility.CacheLineSize];
            return *data;
        }

        internal void SetCurrentWriteBlockTLS(int threadIndex, NativeQueueBlockHeader* currentWriteBlock)
        {
            var data = (NativeQueueBlockHeader**)&m_CurrentWriteBlockTLS[threadIndex * JobsUtility.CacheLineSize];
            *data = currentWriteBlock;
        }

        public static NativeQueueBlockHeader* AllocateWriteBlockMT<T>(NativeQueueData* data, NativeQueueBlockPoolData* pool, int threadIndex) where T : struct
        {
            NativeQueueBlockHeader* currentWriteBlock = data->GetCurrentWriteBlockTLS(threadIndex);

            if (currentWriteBlock != null
                &&  currentWriteBlock->m_NumItems == data->m_MaxItems)
            {
                currentWriteBlock = null;
            }

            if (currentWriteBlock == null)
            {
                currentWriteBlock = pool->AllocateBlock();
                currentWriteBlock->m_NextBlock = null;
                currentWriteBlock->m_NumItems = 0;
                NativeQueueBlockHeader* prevLast = (NativeQueueBlockHeader*)Interlocked.Exchange(ref data->m_LastBlock, (IntPtr)currentWriteBlock);

                if (prevLast == null)
                {
                    data->m_FirstBlock = (IntPtr)currentWriteBlock;
                }
                else
                {
                    prevLast->m_NextBlock = currentWriteBlock;
                }

                data->SetCurrentWriteBlockTLS(threadIndex, currentWriteBlock);
            }

            return currentWriteBlock;
        }

        public unsafe static void AllocateQueue<T>(Allocator label, out NativeQueueData* outBuf) where T : struct
        {
            var queueDataSize = CollectionHelper.Align(UnsafeUtility.SizeOf<NativeQueueData>(), JobsUtility.CacheLineSize);

            var data = (NativeQueueData*)UnsafeUtility.Malloc(
                queueDataSize
                + JobsUtility.CacheLineSize * JobsUtility.MaxJobThreadCount
                , JobsUtility.CacheLineSize
                , label
            );

            data->m_CurrentWriteBlockTLS = (((byte*)data) + queueDataSize);

            data->m_FirstBlock = IntPtr.Zero;
            data->m_LastBlock  = IntPtr.Zero;
            data->m_MaxItems   = (NativeQueueBlockPoolData.m_BlockSize - UnsafeUtility.SizeOf<NativeQueueBlockHeader>()) / UnsafeUtility.SizeOf<T>();

            data->m_CurrentRead = 0;
            for (int threadIndex = 0; threadIndex < JobsUtility.MaxJobThreadCount; ++threadIndex)
            {
                data->SetCurrentWriteBlockTLS(threadIndex, null);
            }

            outBuf = data;
        }

        public unsafe static void DeallocateQueue(NativeQueueData* data, NativeQueueBlockPoolData* pool, Allocator allocation)
        {
            NativeQueueBlockHeader* firstBlock = (NativeQueueBlockHeader*)data->m_FirstBlock;

            while (firstBlock != null)
            {
                NativeQueueBlockHeader* next = firstBlock->m_NextBlock;
                pool->FreeBlock(firstBlock);
                firstBlock = next;
            }

            UnsafeUtility.Free(data, allocation);
        }
    }

    /// <summary>
    /// An unmanaged queue.
    /// </summary>
    /// <typeparam name="T">The type of the elements in the container.</typeparam>
    [StructLayout(LayoutKind.Sequential)]
    [NativeContainer]
    public unsafe struct NativeQueue<T> : IDisposable
        where T : struct
    {
        [NativeDisableUnsafePtrRestriction]
        NativeQueueData* m_Buffer;

        [NativeDisableUnsafePtrRestriction]
        NativeQueueBlockPoolData* m_QueuePool;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        AtomicSafetyHandle m_Safety;

#if UNITY_2020_1_OR_NEWER
        private static readonly SharedStatic<int> s_staticSafetyId = SharedStatic<int>.GetOrCreate<NativeQueue<T>>();

        [BurstDiscard]
        private static void CreateStaticSafetyId()
        {
            s_staticSafetyId.Data = AtomicSafetyHandle.NewStaticSafetyId<NativeQueue<T>>();
        }

#endif

        [NativeSetClassTypeToNullOnSchedule]
        DisposeSentinel m_DisposeSentinel;
#endif

        Allocator m_AllocatorLabel;

        /// <summary>
        /// Constructs a new queue with type of memory allocation.
        /// </summary>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        public NativeQueue(Allocator allocator)
        {
            CollectionHelper.CheckIsUnmanaged<T>();

            m_QueuePool = NativeQueueBlockPool.QueueBlockPool;
            m_AllocatorLabel = allocator;

            NativeQueueData.AllocateQueue<T>(allocator, out m_Buffer);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Create(out m_Safety, out m_DisposeSentinel, 0, allocator);

#if UNITY_2020_1_OR_NEWER
            if (s_staticSafetyId.Data == 0)
            {
                CreateStaticSafetyId();
            }
            AtomicSafetyHandle.SetStaticSafetyId(ref m_Safety, s_staticSafetyId.Data);
#endif
#endif
        }

        /// <summary>
        /// The current number of items in the container.
        /// </summary>
        public int Count
        {
            get
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
                int count = 0;

                for (NativeQueueBlockHeader* block = (NativeQueueBlockHeader*)m_Buffer->m_FirstBlock
                     ; block != null
                     ; block = block->m_NextBlock
                )
                {
                    count += block->m_NumItems;
                }

                return count - m_Buffer->m_CurrentRead;
            }
        }

        /// <summary>
        ///
        /// </summary>
        static public int PersistentMemoryBlockCount
        {
            get { return NativeQueueBlockPool.QueueBlockPool->m_MaxBlocks; }
            set { Interlocked.Exchange(ref NativeQueueBlockPool.QueueBlockPool->m_MaxBlocks, value); }
        }

        /// <summary>
        ///
        /// </summary>
        static public int MemoryBlockSize
        {
            get { return NativeQueueBlockPoolData.m_BlockSize; }
        }

        /// <summary>
        ///
        /// </summary>
        /// <returns></returns>
        public T Peek()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            NativeQueueBlockHeader* firstBlock = (NativeQueueBlockHeader*)m_Buffer->m_FirstBlock;

            if (firstBlock == null)
                throw new InvalidOperationException("Trying to peek from an empty queue");

            return UnsafeUtility.ReadArrayElement<T>(firstBlock + 1, m_Buffer->m_CurrentRead);
        }

        /// <summary>
        /// Enqueue value into the container.
        /// </summary>
        /// <param name="value">The value to be appended.</param>
        public void Enqueue(T value)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            NativeQueueBlockHeader* writeBlock = NativeQueueData.AllocateWriteBlockMT<T>(m_Buffer, m_QueuePool, 0);
            UnsafeUtility.WriteArrayElement(writeBlock + 1, writeBlock->m_NumItems, value);
            ++writeBlock->m_NumItems;
        }

        /// <summary>
        /// Dequeue item from the container.
        /// </summary>
        /// <exception cref="InvalidOperationException">Thrown if queue is empty <see cref="Count"/>.</exception>
        /// <returns>Returns item from the container.</returns>
        public T Dequeue()
        {
            T item;

            if (!TryDequeue(out item))
                throw new InvalidOperationException("Trying to dequeue from an empty queue");

            return item;
        }

        /// <summary>
        /// Try dequeueing item from the container. If container is empty item won't be changed, and return result will be false.
        /// </summary>
        /// <param name="item">Item value if dequeued.</param>
        /// <returns>Returns true if item was dequeued.</returns>
        public bool TryDequeue(out T item)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif

            NativeQueueBlockHeader* firstBlock = (NativeQueueBlockHeader*)m_Buffer->m_FirstBlock;

            if (firstBlock == null)
            {
                item = default(T);
                return false;
            }

            var currentRead = m_Buffer->m_CurrentRead++;
            item = UnsafeUtility.ReadArrayElement<T>(firstBlock + 1, currentRead);

            if (m_Buffer->m_CurrentRead >= firstBlock->m_NumItems)
            {
                m_Buffer->m_CurrentRead = 0;
                m_Buffer->m_FirstBlock = (IntPtr)firstBlock->m_NextBlock;

                if (m_Buffer->m_FirstBlock == IntPtr.Zero)
                {
                    m_Buffer->m_LastBlock = IntPtr.Zero;
                }

                for (int threadIndex = 0; threadIndex < JobsUtility.MaxJobThreadCount; ++threadIndex)
                {
                    if (m_Buffer->GetCurrentWriteBlockTLS(threadIndex) == firstBlock)
                    {
                        m_Buffer->SetCurrentWriteBlockTLS(threadIndex, null);
                    }
                }

                m_QueuePool->FreeBlock(firstBlock);
            }

            return true;
        }

        /// <summary>
        /// A copy of this queue as a [NativeArray](https://docs.unity3d.com/ScriptReference/Unity.Collections.NativeArray_1.html)
        /// allocated with the specified type of memory.
        /// </summary>
        /// <param name="allocator">A member of the
        /// [Unity.Collections.Allocator](https://docs.unity3d.com/ScriptReference/Unity.Collections.Allocator.html) enumeration.</param>
        /// <returns>A NativeArray containing copies of all the items in the queue.</returns>
        public NativeArray<T> ToArray(Allocator allocator)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckReadAndThrow(m_Safety);
#endif
            NativeQueueBlockHeader* firstBlock = (NativeQueueBlockHeader*)m_Buffer->m_FirstBlock;
            var outputArray = new NativeArray<T>(Count, allocator);

            NativeQueueBlockHeader* currentBlock = firstBlock;
            var arrayPtr = (byte*)outputArray.GetUnsafePtr();
            int size = UnsafeUtility.SizeOf<T>();
            int dstOffset = 0;
            int srcOffset = m_Buffer->m_CurrentRead * size;
            int srcOffsetElements = m_Buffer->m_CurrentRead;
            while (currentBlock != null)
            {
                int bytesToCopy = (currentBlock->m_NumItems - srcOffsetElements) * size;
                UnsafeUtility.MemCpy(arrayPtr + dstOffset, (byte*)(currentBlock + 1) + srcOffset, bytesToCopy);
                srcOffset = srcOffsetElements = 0;
                dstOffset += bytesToCopy;
                currentBlock = currentBlock->m_NextBlock;
            }

            return outputArray;
        }

        /// <summary>
        /// Clears the container.
        /// </summary>
        public void Clear()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
            NativeQueueBlockHeader* firstBlock = (NativeQueueBlockHeader*)m_Buffer->m_FirstBlock;

            while (firstBlock != null)
            {
                NativeQueueBlockHeader* next = firstBlock->m_NextBlock;
                m_QueuePool->FreeBlock(firstBlock);
                firstBlock = next;
            }

            m_Buffer->m_FirstBlock = IntPtr.Zero;
            m_Buffer->m_LastBlock = IntPtr.Zero;
            m_Buffer->m_CurrentRead = 0;

            for (int threadIndex = 0; threadIndex < JobsUtility.MaxJobThreadCount; ++threadIndex)
            {
                m_Buffer->SetCurrentWriteBlockTLS(threadIndex, null);
            }
        }

        /// <summary>
        /// Reports whether memory for the container is allocated.
        /// </summary>
        /// <value>True if this container object's internal storage has been allocated.</value>
        /// <remarks>Note that the container storage is not created if you use the default constructor. You must specify
        /// at least an allocation type to construct a usable container.</remarks>
        public bool IsCreated => m_Buffer != null;

        /// <summary>
        /// Disposes of this container and deallocates its memory immediately.
        /// </summary>
        public void Dispose()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            DisposeSentinel.Dispose(ref m_Safety, ref m_DisposeSentinel);
#endif
            NativeQueueData.DeallocateQueue(m_Buffer, m_QueuePool, m_AllocatorLabel);
            m_Buffer = null;
        }

        /// <summary>
        /// Safely disposes of this container and deallocates its memory when the jobs that use it have completed.
        /// </summary>
        /// <remarks>You can call this function dispose of the container immediately after scheduling the job. Pass
        /// the [JobHandle](https://docs.unity3d.com/ScriptReference/Unity.Jobs.JobHandle.html) returned by
        /// the [Job.Schedule](https://docs.unity3d.com/ScriptReference/Unity.Jobs.IJobExtensions.Schedule.html)
        /// method using the `jobHandle` parameter so the job scheduler can dispose the container after all jobs
        /// using it have run.</remarks>
        /// <param name="inputDeps">The job handle or handles for any scheduled jobs that use this container.</param>
        /// <returns>A new job handle containing the prior handles as well as the handle for the job that deletes
        /// the container.</returns>
        public JobHandle Dispose(JobHandle inputDeps)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            // [DeallocateOnJobCompletion] is not supported, but we want the deallocation
            // to happen in a thread. DisposeSentinel needs to be cleared on main thread.
            // AtomicSafetyHandle can be destroyed after the job was scheduled (Job scheduling
            // will check that no jobs are writing to the container).
            DisposeSentinel.Clear(ref m_DisposeSentinel);

            var jobHandle = new NativeQueueDisposeJob { Data = new NativeQueueDispose { m_Buffer = m_Buffer, m_QueuePool = m_QueuePool, m_AllocatorLabel = m_AllocatorLabel, m_Safety = m_Safety } }.Schedule(inputDeps);

            AtomicSafetyHandle.Release(m_Safety);
#else
            var jobHandle = new NativeQueueDisposeJob { Data = new NativeQueueDispose { m_Buffer = m_Buffer, m_QueuePool = m_QueuePool, m_AllocatorLabel = m_AllocatorLabel }  }.Schedule(inputDeps);
#endif
            m_Buffer = null;

            return jobHandle;
        }

        /// <summary>
        /// Returns parallel reader instance.
        /// </summary>
        public ParallelWriter AsParallelWriter()
        {
            ParallelWriter writer;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
            writer.m_Safety = m_Safety;
            AtomicSafetyHandle.UseSecondaryVersion(ref writer.m_Safety);
#endif
            writer.m_Buffer = m_Buffer;
            writer.m_QueuePool = m_QueuePool;
            writer.m_ThreadIndex = 0;

            return writer;
        }

        /// <summary>
        /// Implements parallel writer. Use AsParallelWriter to obtain it from container.
        /// </summary>
        [NativeContainer]
        [NativeContainerIsAtomicWriteOnly]
        public unsafe struct ParallelWriter
        {
            [NativeDisableUnsafePtrRestriction]
            internal NativeQueueData* m_Buffer;

            [NativeDisableUnsafePtrRestriction]
            internal NativeQueueBlockPoolData* m_QueuePool;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            internal AtomicSafetyHandle m_Safety;
#endif
            [NativeSetThreadIndex]
            internal int m_ThreadIndex;

            /// <summary>
            /// Enqueue value into the container.
            /// </summary>
            /// <param name="value">The value to be appended.</param>
            public void Enqueue(T value)
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                AtomicSafetyHandle.CheckWriteAndThrow(m_Safety);
#endif
                NativeQueueBlockHeader* writeBlock = NativeQueueData.AllocateWriteBlockMT<T>(m_Buffer, m_QueuePool, m_ThreadIndex);
                UnsafeUtility.WriteArrayElement(writeBlock + 1, writeBlock->m_NumItems, value);
                ++writeBlock->m_NumItems;
            }
        }
    }

    [NativeContainer]
    internal unsafe struct NativeQueueDispose
    {
        [NativeDisableUnsafePtrRestriction]
        internal NativeQueueData* m_Buffer;

        [NativeDisableUnsafePtrRestriction]
        internal NativeQueueBlockPoolData* m_QueuePool;

        internal Allocator m_AllocatorLabel;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        internal AtomicSafetyHandle m_Safety;
#endif

        public void Dispose()
        {
            NativeQueueData.DeallocateQueue(m_Buffer, m_QueuePool, m_AllocatorLabel);
        }
    }

    [BurstCompile]
    struct NativeQueueDisposeJob : IJob
    {
        public NativeQueueDispose Data;

        public void Execute()
        {
            Data.Dispose();
        }
    }
}

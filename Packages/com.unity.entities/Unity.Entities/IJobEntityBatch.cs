using System;
using Unity.Burst;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Profiling;

namespace Unity.Entities
{
    /// <summary>
    /// IJobEntityBatch is a type of [IJob] that iterates over a set of <see cref="ArchetypeChunk"/> instances,
    /// where each instance represents a contiguous batch of entities within a [chunk].
    ///
    /// [IJob]: https://docs.unity3d.com/ScriptReference/Unity.Jobs.IJob.html
    /// [chunk]: xref:ecs-concepts#chunk
    /// </summary>
    /// <remarks>
    /// Schedule or run an IJobEntityBatch job inside the <see cref="SystemBase.OnUpdate"/> function of a
    /// <see cref="SystemBase"/> implementation. When the system schedules or runs an IJobEntityBatch job, it uses
    /// the specified <see cref="EntityQuery"/> to select a set of [chunks]. These selected chunks are divided into
    /// batches of entities. A batch is a contiguous set of entities, always stored in the same chunk. The job
    /// struct's `Execute` function is called for each batch.
    ///
    /// When you schedule or run the job with one of the following methods:
    /// * <see cref="JobEntityBatchExtensions.ScheduleSingle{T}(T, EntityQuery, JobHandle)"/>,
    /// * <see cref="JobEntityBatchExtensions.ScheduleParallel{T}(T, EntityQuery, JobHandle)"/>,
    /// * or <see cref="JobEntityBatchExtensions.Run{T}(T, EntityQuery)"/>
    ///
    /// all the entities of each chunk are processed as
    /// a single batch. The <see cref="ArchetypeChunk"/> object passed to the `Execute` function of your job struct provides access
    /// to the components of all the entities in the chunk.
    ///
    /// Use <see cref="JobEntityBatchExtensions.ScheduleParallelBatched{T}(T, EntityQuery, int, JobHandle)"/> to divide
    /// each chunk selected by your query into (approximately) equal batches of contiguous entities. For example,
    /// if you use a batch count of two, one batch provides access to the first half of the component arrays in a chunk and the other
    /// provides access to the second half. When you use batching, the <see cref="ArchetypeChunk"/> object only
    /// provides access to the components in the current batch of entities -- not those of all entities in a chunk.
    ///
    /// In general, processing whole chunks at a time (setting batch count to one) is the most efficient. However, in cases
    /// where the algorithm itself is relatively expensive for each entity, executing smaller batches in parallel can provide
    /// better overall performance, especially when the entities are contained in a small number of chunks. As always, you
    /// should profile your job to find the best arrangement for your specific application.
    ///
    /// To pass data to your Execute function (beyond the `Execute` parameters), add public fields to the IJobEntityBatch
    /// struct declaration and set those fields immediately before scheduling the job. You must always pass the
    /// component type information for any components that the job reads or writes using a field of type,
    /// <seealso cref="ArchetypeChunkComponentType{T}"/>. Get this type information by calling the appropriate
    /// <seealso cref="ComponentSystemBase.GetArchetypeChunkComponentType{T}(bool)"/> function for the type of
    /// component.
    ///
    /// For more information see [Using IJobEntityBatch].
    /// <example>
    /// <code source="../DocCodeSamples.Tests/ChunkIterationJob.cs" region="basic-ijobentitybatch" title="IJobEntityBatch Example"/>
    /// </example>
    ///
    /// [Using IJobEntityBatch]: xref:ecs-ijobentitybatch
    /// [chunks]: xref:ecs-concepts#chunk
    /// </remarks>
    [JobProducerType(typeof(JobEntityBatchExtensions.JobEntityBatchProducer<>))]
    public interface IJobEntityBatch
    {
        /// <summary>
        /// Implement the `Execute` function to perform a unit of work on an <see cref="ArchetypeChunk"/> representing
        /// a contiguous batch of entities within a chunk.
        /// </summary>
        /// <remarks>
        /// The chunks selected by the <see cref="EntityQuery"/> used to schedule the job are the input to your `Execute`
        /// function. If you use <see cref="JobEntityBatchExtensions.ScheduleParallelBatched{T}(T, EntityQuery, int, JobHandle)"/>
        /// to schedule the job, the entities in each matching chunk are partitioned into contiguous batches based on the
        /// `batchesInChunk` parameter, and the `Execute` function is called once for each batch. When you use one of the
        /// other scheduling or run methods, the `Execute` function is called once per matching chunk (in other words, the
        /// batch count is one).
        /// </remarks>
        /// <param name="batchInChunk">An object providing access to a batch of entities within a chunk.</param>
        /// <param name="batchIndex">The index of the current batch within the list of all batches in all chunks found by the
        /// job's <see cref="EntityQuery"/>. If the batch count is one, this list contains one entry for each selected chunk; if
        /// the batch count is two, the list contains two entries per chunk; and so on. Note that batches are not processed in
        /// index order, except by chance.</param>
        void Execute(ArchetypeChunk batchInChunk, int batchIndex);
    }

    /// <summary>
    /// Extensions for scheduling and running <see cref="IJobEntityBatch"/> jobs.
    /// </summary>
    public static class JobEntityBatchExtensions
    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [NativeContainer]
        internal struct EntitySafetyHandle
        {
            internal AtomicSafetyHandle m_Safety;
        }
#endif
        internal struct JobEntityBatchWrapper<T> where T : struct
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
#pragma warning disable 414
            [ReadOnly] public EntitySafetyHandle safety;
#pragma warning restore
#endif
            public T JobData;

            [DeallocateOnJobCompletion]
            [NativeDisableContainerSafetyRestriction]
            public NativeArray<ArchetypeChunk> Batches;

            public int JobsPerChunk;
            public int IsParallel;
        }

        /// <summary>
        /// Adds an <see cref="IJobEntityBatch"/> instance to the job scheduler queue for sequential (non-parallel) execution.
        /// </summary>
        /// <remarks>This scheduling variant processes each matching chunk as a single batch. All chunks execute
        /// sequentially.</remarks>
        /// <param name="jobData">An <see cref="IJobEntityBatch"/> instance.</param>
        /// <param name="query">The query selecting chunks with the necessary components.</param>
        /// <param name="dependsOn">The handle identifying already scheduled jobs that could constrain this job.
        /// A job that writes to a component cannot run in parallel with other jobs that read or write that component.
        /// Jobs that only read the same components can run in parallel.</param>
        /// <typeparam name="T">The specific <see cref="IJobEntityBatch"/> implementation type.</typeparam>
        /// <returns>A handle that combines the current Job with previous dependencies identified by the `dependsOn`
        /// parameter.</returns>
        public static unsafe JobHandle ScheduleSingle<T>(
            this T jobData,
            EntityQuery query,
            JobHandle dependsOn = default(JobHandle))
            where T : struct, IJobEntityBatch
        {
            return ScheduleInternal(ref jobData, query, dependsOn, ScheduleMode.Batched, 1, false);
        }

        /// <summary>
        /// Adds an <see cref="IJobEntityBatch"/> instance to the job scheduler queue for parallel execution.
        /// </summary>
        /// <remarks>This scheduling variant processes each matching chunk as a single batch. Each
        /// chunk can execute in parallel. This scheduling method is equivalent to calling
        /// <see cref="JobEntityBatchExtensions.ScheduleParallelBatched{T}(T, EntityQuery, int, JobHandle)"/>
        /// with the batchesPerChunk parameter set to 1.</remarks>
        /// <param name="jobData">An <see cref="IJobEntityBatch"/> instance.</param>
        /// <param name="query">The query selecting chunks with the necessary components.</param>
        /// <param name="dependsOn">The handle identifying already scheduled jobs that could constrain this job.
        /// A job that writes to a component cannot run in parallel with other jobs that read or write that component.
        /// Jobs that only read the same components can run in parallel.</param>
        /// <typeparam name="T">The specific <see cref="IJobEntityBatch"/> implementation type.</typeparam>
        /// <returns>A handle that combines the current Job with previous dependencies identified by the `dependsOn`
        /// parameter.</returns>
        public static unsafe JobHandle ScheduleParallel<T>(
            this T jobData,
            EntityQuery query,
            JobHandle dependsOn = default(JobHandle))
            where T : struct, IJobEntityBatch
        {
            return ScheduleInternal(ref jobData, query, dependsOn, ScheduleMode.Batched, 1, true);
        }

        /// <summary>
        /// Adds an <see cref="IJobEntityBatch"/> instance to the job scheduler queue for parallel execution, potentially subdividing
        /// chunks into multiple batches.
        /// </summary>
        /// <remarks>This scheduling variant processes each matching chunk as one or more batches. Each batch, including
        /// those from the same chunk, can execute in parallel.</remarks>
        /// <param name="jobData">An <see cref="IJobEntityBatch"/> instance.</param>
        /// <param name="query">The query selecting chunks with the necessary components.</param>
        /// <param name="batchesPerChunk">The number of batches to use per chunk. The entities in each chunk matching
        /// <paramref name="query"/> are partitioned into this many contiguous batches of approximately equal size.
        /// Multiple batches form the same chunk may be processed concurrently.</param>
        /// <param name="dependsOn">The handle identifying already scheduled jobs that could constrain this job.
        /// A job that writes to a component cannot run in parallel with other jobs that read or write that component.
        /// Jobs that only read the same components can run in parallel.</param>
        /// <typeparam name="T">The specific <see cref="IJobEntityBatch"/> implementation type.</typeparam>
        /// <returns>A handle that combines the current Job with previous dependencies identified by the `dependsOn`
        /// parameter.</returns>
        public static unsafe JobHandle ScheduleParallelBatched<T>(
            this T jobData,
            EntityQuery query,
            int batchesPerChunk,
            JobHandle dependsOn = default(JobHandle))
            where T : struct, IJobEntityBatch
        {
            return ScheduleInternal(ref jobData, query, dependsOn, ScheduleMode.Batched, batchesPerChunk, true);
        }

        /// <summary>
        /// Runs the job immediately on the current thread.
        /// </summary>
        /// <remarks>This scheduling variant processes each matching chunk as a single batch. All chunks execute
        /// sequentially on the current thread.</remarks>
        /// <param name="jobData">An <see cref="IJobEntityBatch"/> instance.</param>
        /// <param name="query">The query selecting chunks with the necessary components.</param>
        /// <typeparam name="T">The specific <see cref="IJobEntityBatch"/> implementation type.</typeparam>
        public static unsafe void Run<T>(this T jobData, EntityQuery query)
            where T : struct, IJobEntityBatch
        {
            ScheduleInternal(ref jobData, query, default(JobHandle), ScheduleMode.Run, 1, false);
        }

        internal static unsafe JobHandle ScheduleInternal<T>(
            ref T jobData,
            EntityQuery query,
            JobHandle dependsOn,
            ScheduleMode mode,
            int batchesPerChunk,
            bool isParallel = true)
            where T : struct, IJobEntityBatch
        {
            var queryImpl = query._GetImpl();
            var filteredChunkCount = queryImpl->CalculateChunkCount();
            var batches = new NativeArray<ArchetypeChunk>(filteredChunkCount * batchesPerChunk, Allocator.TempJob);

            var prefilterHandle = new PrefilterForJobEntityBatch
            {
                MatchingArchetypes = queryImpl->_QueryData->MatchingArchetypes,
                Filter = queryImpl->_Filter,
                BatchesPerChunk = batchesPerChunk,
                EntityComponentStore = queryImpl->_Access->EntityComponentStore,
                Batches = batches
            }.Schedule(dependsOn);

            JobEntityBatchWrapper<T> jobEntityBatchWrapper = new JobEntityBatchWrapper<T>
            {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                // All IJobEntityBatch jobs have a EntityManager safety handle to ensure that BeforeStructuralChange throws an error if
                // jobs without any other safety handles are still running (haven't been synced).
                safety = new EntitySafetyHandle {m_Safety = queryImpl->SafetyHandles->GetEntityManagerSafetyHandle()},
#endif

                JobData = jobData,
                Batches = batches,

                JobsPerChunk = batchesPerChunk,
                IsParallel = isParallel ? 1 : 0
            };

            var scheduleParams = new JobsUtility.JobScheduleParameters(
                UnsafeUtility.AddressOf(ref jobEntityBatchWrapper),
                isParallel
                ? JobEntityBatchProducer<T>.InitializeParallel()
                : JobEntityBatchProducer<T>.InitializeSingle(),
                prefilterHandle,
                mode);

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            try
            {
#endif
            if (!isParallel)
            {
                return JobsUtility.Schedule(ref scheduleParams);
            }
            else
            {
                return JobsUtility.ScheduleParallelFor(ref scheduleParams, filteredChunkCount * batchesPerChunk, 1);
            }
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        }

        catch (InvalidOperationException e)
        {
            prefilterHandle.Complete();
            batches.Dispose();
            throw e;
        }
#endif
        }

        internal struct JobEntityBatchProducer<T>
            where T : struct, IJobEntityBatch
        {
            static IntPtr s_JobReflectionDataParallel;
            static IntPtr s_JobReflectionDataSingle;

            public static IntPtr InitializeSingle()
            {
                if (s_JobReflectionDataSingle == IntPtr.Zero)
                    s_JobReflectionDataSingle = JobsUtility.CreateJobReflectionData(
                        typeof(JobEntityBatchWrapper<T>),
                        typeof(T),
                        JobType.Single,
                        (ExecuteJobFunction)Execute);

                return s_JobReflectionDataSingle;
            }

            public static IntPtr InitializeParallel()
            {
                if (s_JobReflectionDataParallel == IntPtr.Zero)
                    s_JobReflectionDataParallel = JobsUtility.CreateJobReflectionData(
                        typeof(JobEntityBatchWrapper<T>),
                        typeof(T),
                        JobType.ParallelFor,
                        (ExecuteJobFunction)Execute);

                return s_JobReflectionDataParallel;
            }

            public delegate void ExecuteJobFunction(
                ref JobEntityBatchWrapper<T> jobWrapper,
                IntPtr additionalPtr,
                IntPtr bufferRangePatchData,
                ref JobRanges ranges,
                int jobIndex);

            public static void Execute(
                ref JobEntityBatchWrapper<T> jobWrapper,
                IntPtr additionalPtr,
                IntPtr bufferRangePatchData,
                ref JobRanges ranges,
                int jobIndex)
            {
                ExecuteInternal(ref jobWrapper, ref ranges, jobIndex);
            }

            internal unsafe static void ExecuteInternal(
                ref JobEntityBatchWrapper<T> jobWrapper,
                ref JobRanges ranges,
                int jobIndex)
            {
                var batches = jobWrapper.Batches;

                bool isParallel = jobWrapper.IsParallel == 1;
                while (true)
                {
                    int beginBatchIndex = 0;
                    int endBatchIndex = batches.Length;

                    // If we are running the job in parallel, steal some work.
                    if (isParallel)
                    {
                        // If we have no range to steal, exit the loop.
                        if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out beginBatchIndex, out endBatchIndex))
                            break;
                    }

                    // Do the actual user work.
                    for (int batchIndex = beginBatchIndex; batchIndex < endBatchIndex; ++batchIndex)
                    {
                        jobWrapper.JobData.Execute(batches[batchIndex], batchIndex);
                    }

                    // If we are not running in parallel, our job is done.
                    if (!isParallel)
                        break;
                }
            }
        }
    }

    [BurstCompile]
    unsafe struct PrefilterForJobEntityBatch : IJob
    {
        [NativeDisableUnsafePtrRestriction] public UnsafeMatchingArchetypePtrList MatchingArchetypes;
        public EntityQueryFilter Filter;
        public int BatchesPerChunk;
        [NativeDisableUnsafePtrRestriction] public EntityComponentStore* EntityComponentStore;

        [NativeDisableParallelForRestriction] public NativeArray<ArchetypeChunk> Batches;

        public void Execute()
        {
            var batchCounter = 0;

            for (var m = 0; m < MatchingArchetypes.Length; ++m)
            {
                var match = MatchingArchetypes.Ptr[m];
                if (match->Archetype->EntityCount <= 0)
                    continue;

                var archetype = match->Archetype;
                int chunkCount = archetype->Chunks.Count;

                for (int chunkIndex = 0; chunkIndex < chunkCount; ++chunkIndex)
                {
                    var chunk = archetype->Chunks.p[chunkIndex];
                    for (int batchIndex = 0; batchIndex < BatchesPerChunk; ++batchIndex)
                    {
                        if (match->ChunkMatchesFilter(chunkIndex, ref Filter))
                            Batches[batchCounter++] = ArchetypeChunk.EntityBatchFromChunk(chunk, BatchesPerChunk, batchIndex, EntityComponentStore);
                    }
                }
            }
        }
    }
}

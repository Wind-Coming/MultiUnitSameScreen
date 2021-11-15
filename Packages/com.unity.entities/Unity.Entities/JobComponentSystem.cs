using System;
using Unity.Entities.CodeGeneratedJobForEach;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;

namespace Unity.Entities
{
    /// <summary>
    /// An abstract class to implement in order to create a system that uses ECS-specific Jobs.
    /// </summary>
    /// <remarks>Implement a JobComponentSystem subclass for systems that perform their work using
    /// <see cref="IJobForEach{T0}"/> or <see cref="IJobChunk"/>.</remarks>
    /// <seealso cref="ComponentSystem"/>
    public unsafe abstract class JobComponentSystem : ComponentSystemBase
    {
        private JobHandle m_PreviousFrameDependency;
        private bool m_AlwaysSynchronizeSystem;

        /// <summary>
        /// Use Entities.ForEach((ref Translation translation, in Velocity velocity) => { translation.Value += velocity.Value * dt; }).Schedule(inputDependencies);
        /// </summary>
        protected internal ForEachLambdaJobDescriptionJCS Entities => new ForEachLambdaJobDescriptionJCS();

#if ENABLE_DOTS_COMPILER_CHUNKS
        /// <summary>
        /// Use query.Chunks.ForEach((ArchetypeChunk chunk, int chunkIndex, int indexInQueryOfFirstEntity) => { YourCodeGoesHere(); }).Schedule();
        /// </summary>
        public LambdaJobChunkDescription Chunks
        {
            get
            {
                return new LambdaJobChunkDescription();
            }
        }
#endif

        /// <summary>
        /// Use Job.WithCode(() => { YourCodeGoesHere(); }).Schedule(inputDependencies);
        /// </summary>
        protected internal LambdaSingleJobDescriptionJCS Job
        {
            get
            {
                return new LambdaSingleJobDescriptionJCS();
            }
        }

        unsafe JobHandle BeforeOnUpdate()
        {
            BeforeUpdateVersioning();

            var state = CheckedState();

            // We need to wait on all previous frame dependencies, otherwise it is possible that we create infinitely long dependency chains
            // without anyone ever waiting on it
            m_PreviousFrameDependency.Complete();

            if (m_AlwaysSynchronizeSystem)
            {
                CompleteDependencyInternal();
                return default;
            }

            var depMgr = state->m_EntityManager.GetCheckedEntityDataAccess()->DependencyManager;
            return depMgr->GetDependency(state->m_JobDependencyForReadingSystems.Ptr, state->m_JobDependencyForReadingSystems.Length, state->m_JobDependencyForWritingSystems.Ptr, state->m_JobDependencyForWritingSystems.Length);
        }

#pragma warning disable 649
        private unsafe struct JobHandleData
        {
            public void*   jobGroup;
            public int     version;
        }
#pragma warning restore 649

        unsafe void AfterOnUpdate(JobHandle outputJob, bool throwException)
        {
            AfterUpdateVersioning();

            var state = CheckedState();

            var depMgr = state->m_EntityManager.GetCheckedEntityDataAccess()->DependencyManager;

            // If outputJob says no relevant jobs were scheduled,
            // then no need to batch them up or register them.
            // This is a big optimization if we only Run methods on main thread...
            if (((JobHandleData*)&outputJob)->jobGroup != null)
            {
                JobHandle.ScheduleBatchedJobs();

                m_PreviousFrameDependency = depMgr->AddDependency(state->m_JobDependencyForReadingSystems.Ptr, state->m_JobDependencyForReadingSystems.Length, state->m_JobDependencyForWritingSystems.Ptr, state->m_JobDependencyForWritingSystems.Length, outputJob);
            }

#if ENABLE_UNITY_COLLECTIONS_CHECKS

            if (JobsUtility.JobDebuggerEnabled)
            {
                var details = default(SystemDependencySafetyUtility.SafetyErrorDetails);
                var dependencyError = false;
                state->CheckSafety(ref details, ref dependencyError);
                if (throwException && dependencyError)
                    throw new InvalidOperationException(details.FormatToString(GetType()));
            }
#endif
        }

        public sealed override void Update()
        {
            var state = CheckedState();

#if ENABLE_PROFILER
            using (state->m_ProfilerMarker.Auto())
#endif
            {
                if (Enabled && ShouldRunSystem())
                {
                    if (!state->m_PreviouslyEnabled)
                    {
                        state->m_PreviouslyEnabled = true;
                        OnStartRunning();
                    }

                    var inputJob = BeforeOnUpdate();
                    JobHandle outputJob = new JobHandle();

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    var oldExecutingSystem = ms_ExecutingSystem;
                    ms_ExecutingSystem = this;
#endif
                    try
                    {
                        outputJob = OnUpdate(inputJob);
                    }
                    catch
                    {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
                        ms_ExecutingSystem = oldExecutingSystem;
#endif

                        AfterOnUpdate(outputJob, false);
                        throw;
                    }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
                    ms_ExecutingSystem = oldExecutingSystem;
#endif

                    AfterOnUpdate(outputJob, true);
                }
                else if (state->m_PreviouslyEnabled)
                {
                    state->m_PreviouslyEnabled = false;
                    OnStopRunning();
                }
            }
        }

        internal sealed override void OnBeforeCreateInternal(World world)
        {
            var state = CheckedState();

            base.OnBeforeCreateInternal(world);
#if !NET_DOTS
            m_AlwaysSynchronizeSystem = GetType().GetCustomAttributes(typeof(AlwaysSynchronizeSystemAttribute), true).Length != 0;
#else
            m_AlwaysSynchronizeSystem = false;
            var attrs = TypeManager.GetSystemAttributes(GetType(), typeof(AlwaysSynchronizeSystemAttribute));
            if (attrs.Length > 0)
                m_AlwaysSynchronizeSystem = true;
#endif
        }

        internal sealed override void OnBeforeDestroyInternal()
        {
            base.OnBeforeDestroyInternal();
            m_PreviousFrameDependency.Complete();
        }

        /// <summary>Implement OnUpdate to perform the major work of this system.</summary>
        /// <remarks>
        /// The system invokes OnUpdate once per frame on the main thread when any of this system's
        /// EntityQueries match existing entities, or if the system has the AlwaysUpdate
        /// attribute.
        ///
        /// To run a Job, create an instance of the Job struct, assign appropriate values to the struct fields and call
        /// one of the Job schedule functions. The system passes any current dependencies between Jobs -- which can include Jobs
        /// internal to this system, such as gathering entities or chunks, as well as Jobs external to this system,
        /// such as Jobs that write to the components read by this system -- in the `inputDeps` parameter. Your function
        /// must combine the input dependencies with any dependencies of the Jobs created in OnUpdate and return the
        /// combined <see cref="JobHandle"/> object.
        /// </remarks>
        /// <param name="inputDeps">Existing dependencies for this system.</param>
        /// <returns>A Job handle that contains the dependencies of the Jobs in this system.</returns>
        protected abstract JobHandle OnUpdate(JobHandle inputDeps);
    }
}

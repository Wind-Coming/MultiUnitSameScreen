#if !UNITY_DOTSPLAYER
using System;
using Unity.Burst;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;
using UnityEngine.Scripting;

namespace Unity.Jobs
{
    [JobProducerType(typeof(IJobBurstScheduableExtensions.JobStruct2<>))]
    public interface IJobBurstScheduable
    {
        void Execute();
    }

    public static class IJobBurstScheduableExtensions
    {
        internal struct JobStruct2<T> where T : struct, IJobBurstScheduable
        {
            public static readonly SharedStatic<IntPtr> jobReflectionData = SharedStatic<IntPtr>.GetOrCreate<JobStruct2<T>>();

            [AutoCreateReflectionData]
            [Preserve]
            private static void Initialize()
            {
                jobReflectionData.Data = JobsUtility.CreateJobReflectionData(typeof(T), JobType.Single, (ExecuteJobFunction) Execute);
            }

            public delegate void ExecuteJobFunction(ref T data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            public static void Execute(ref T data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                data.Execute();
            }
        }

        unsafe public static JobHandle Schedule<T>(this T jobData, JobHandle dependsOn = new JobHandle()) where T : struct, IJobBurstScheduable
        {
            var reflectionData = JobStruct2<T>.jobReflectionData.Data;
            if (reflectionData == IntPtr.Zero)
            {
                throw new InvalidOperationException("This should have been initialized by code gen");
            }

            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), reflectionData, dependsOn, ScheduleMode.Batched);
            return JobsUtility.Schedule(ref scheduleParams);
        }

        unsafe public static void Run<T>(this T jobData) where T : struct, IJobBurstScheduable
        {
            var reflectionData = JobStruct2<T>.jobReflectionData.Data;
            if (reflectionData == IntPtr.Zero)
            {
                throw new InvalidOperationException("This should have been initialized by code gen");
            }

            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), reflectionData, new JobHandle(), ScheduleMode.Run);
            JobsUtility.Schedule(ref scheduleParams);
        }
    }
}
#endif

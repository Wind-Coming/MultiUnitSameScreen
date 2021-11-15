using System;
using Unity.Burst;
using Unity.Jobs.LowLevel.Unsafe;
using Unity.Collections.LowLevel.Unsafe;

#if !UNITY_DOTSPLAYER
namespace Unity.Jobs
{
    [JobProducerType(typeof(IJobParallelForExtensionsBurstScheduable.ParallelForJobStructBurstScheduable<>))]
    public interface IJobParallelForBurstScheduable
    {
        void Execute(int index);
    }

    public static class IJobParallelForExtensionsBurstScheduable
    {
        internal struct ParallelForJobStructBurstScheduable<T> where T : struct, IJobParallelForBurstScheduable
        {
            public static readonly SharedStatic<IntPtr> jobReflectionData = SharedStatic<IntPtr>.GetOrCreate<ParallelForJobStructBurstScheduable<T>>();

            [AutoCreateReflectionData]
            private static void Initialize()
            {
                jobReflectionData.Data = JobsUtility.CreateJobReflectionData(typeof(T), JobType.ParallelFor, (ExecuteJobFunction)Execute);
            }

            public delegate void ExecuteJobFunction(ref T data, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex);

            public static unsafe void Execute(ref T jobData, IntPtr additionalPtr, IntPtr bufferRangePatchData, ref JobRanges ranges, int jobIndex)
            {
                while (true)
                {
                    int begin;
                    int end;
                    if (!JobsUtility.GetWorkStealingRange(ref ranges, jobIndex, out begin, out end))
                        break;

                    #if ENABLE_UNITY_COLLECTIONS_CHECKS
                    JobsUtility.PatchBufferMinMaxRanges(bufferRangePatchData, UnsafeUtility.AddressOf(ref jobData), begin, end - begin);
                    #endif

                    var endThatCompilerCanSeeWillNeverChange = end;
                    for (var i = begin; i < endThatCompilerCanSeeWillNeverChange; ++i)
                        jobData.Execute(i);
                }
            }
        }

        unsafe public static JobHandle Schedule<T>(this T jobData, int arrayLength, int innerloopBatchCount, JobHandle dependsOn = new JobHandle()) where T : struct, IJobParallelForBurstScheduable
        {
            var reflectionData = ParallelForJobStructBurstScheduable<T>.jobReflectionData.Data;
            if (reflectionData == IntPtr.Zero)
            {
                throw new InvalidOperationException("Reflection data was not set up by code generation");
            }
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), reflectionData, dependsOn, ScheduleMode.Batched);
            return JobsUtility.ScheduleParallelFor(ref scheduleParams, arrayLength, innerloopBatchCount);
        }

        unsafe public static void Run<T>(this T jobData, int arrayLength) where T : struct, IJobParallelForBurstScheduable
        {
            var reflectionData = ParallelForJobStructBurstScheduable<T>.jobReflectionData.Data;
            if (reflectionData == IntPtr.Zero)
            {
                throw new InvalidOperationException("Reflection data was not set up by code generation");
            }
            var scheduleParams = new JobsUtility.JobScheduleParameters(UnsafeUtility.AddressOf(ref jobData), reflectionData, new JobHandle(), ScheduleMode.Run);
            JobsUtility.ScheduleParallelFor(ref scheduleParams, arrayLength, arrayLength);
        }
    }
}
#endif

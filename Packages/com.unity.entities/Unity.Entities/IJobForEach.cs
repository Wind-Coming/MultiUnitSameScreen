using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Reflection;
using System.Runtime.InteropServices;
using Unity.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using ReadOnlyAttribute = Unity.Collections.ReadOnlyAttribute;

namespace Unity.Entities
{
    //@TODO: What about change or add?
    [AttributeUsage(AttributeTargets.Parameter)]
    public class ChangedFilterAttribute : Attribute
    {
    }

    [AttributeUsage(AttributeTargets.Struct)]
    public class RequireComponentTagAttribute : Attribute
    {
        public Type[] TagComponents;

        public RequireComponentTagAttribute(params Type[] tagComponents)
        {
            TagComponents = tagComponents;
        }
    }

    [AttributeUsage(AttributeTargets.Struct)]
    public class ExcludeComponentAttribute : Attribute
    {
        public Type[] ExcludeComponents;

        public ExcludeComponentAttribute(params Type[] excludeComponents)
        {
            ExcludeComponents = excludeComponents;
        }
    }

    public static partial class JobForEachExtensions
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        public interface IBaseJobForEach
        {
            // code-gen:
            // ComponentType[] GetComponentTypes_Gen(out int processCount, out ComponentType[] changedFilter);
            // IntPtr GetJobReflection_Gen(bool isParallelFor);
        }

        static ComponentType[] GetComponentTypes<T>(this T self, out int processCount, out ComponentType[] changedFilter)
            where T : struct, IBaseJobForEach
        {
            // code-gen:
            // return self.GetComponentTypes_Gen(out processCount, out changedFilter);
            throw new Exception("Should be replaced by code-gen.");
        }

        static IntPtr GetJobReflection<T>(this T self, bool isParallelFor)
            where T : struct, IBaseJobForEach
        {
            // code-gen:
            // return self.GetJobReflection_Gen(isParallelFor);
            throw new Exception("Should be replaced by code-gen.");
        }

#if !UNITY_DOTSPLAYER
        static ComponentType[] GetComponentTypes(Type jobType)
        {
            var interfaceType = GetIJobForEachInterface(jobType);
            if (interfaceType != null)
            {
                int temp;
                ComponentType[] temp2;
                return GetComponentTypes(jobType, interfaceType, out temp, out temp2);
            }

            return null;
        }

        static ComponentType[] GetComponentTypes(Type jobType, Type interfaceType, out int processCount,
            out ComponentType[] changedFilter)
        {
            var genericArgs = interfaceType.GetGenericArguments();

            var executeMethodParameters = jobType.GetMethod("Execute").GetParameters();

            var componentTypes = new List<ComponentType>();
            var changedFilterTypes = new List<ComponentType>();


            // void Execute(Entity entity, int index, ref T0 data0, ref T1 data1, ref T2 data2);
            // First two parameters are optional, depending on the interface name used.
            var methodParameterOffset = genericArgs.Length != executeMethodParameters.Length ? 2 : 0;

            for (var i = 0; i < genericArgs.Length; i++)
            {
                var isReadonly = executeMethodParameters[i + methodParameterOffset].GetCustomAttribute(typeof(ReadOnlyAttribute)) != null;

                var type = new ComponentType(genericArgs[i],
                    isReadonly ? ComponentType.AccessMode.ReadOnly : ComponentType.AccessMode.ReadWrite);
                componentTypes.Add(type);

                var isChangedFilter = executeMethodParameters[i + methodParameterOffset].GetCustomAttribute(typeof(ChangedFilterAttribute)) != null;
                if (isChangedFilter)
                    changedFilterTypes.Add(type);
            }

            var subtractive = jobType.GetCustomAttribute<ExcludeComponentAttribute>();
            if (subtractive != null)
                foreach (var type in subtractive.ExcludeComponents)
                    componentTypes.Add(ComponentType.Exclude(type));

            var requiredTags = jobType.GetCustomAttribute<RequireComponentTagAttribute>();
            if (requiredTags != null)
                foreach (var type in requiredTags.TagComponents)
                    componentTypes.Add(ComponentType.ReadOnly(type));

            processCount = genericArgs.Length;
            changedFilter = changedFilterTypes.ToArray();
            return componentTypes.ToArray();
        }

        static int CalculateEntityCount(ComponentSystemBase system, Type jobType)
        {
            var query = GetEntityQueryForIJobForEach(system, jobType);

            int entityCount = query.CalculateEntityCount();

            return entityCount;
        }

        static IntPtr GetJobReflection(Type jobType, Type wrapperJobType, Type interfaceType,
            bool isIJobParallelFor)
        {
            Assert.AreNotEqual(null, wrapperJobType);
            Assert.AreNotEqual(null, interfaceType);

            var genericArgs = interfaceType.GetGenericArguments();

            var jobTypeAndGenericArgs = new List<Type>();
            jobTypeAndGenericArgs.Add(jobType);
            jobTypeAndGenericArgs.AddRange(genericArgs);
            var resolvedWrapperJobType = wrapperJobType.MakeGenericType(jobTypeAndGenericArgs.ToArray());

            object[] parameters = {isIJobParallelFor ? JobType.ParallelFor : JobType.Single};
            var reflectionDataRes = resolvedWrapperJobType.GetMethod("Initialize").Invoke(null, parameters);
            return (IntPtr)reflectionDataRes;
        }

        static Type GetIJobForEachInterface(Type jobType)
        {
            foreach (var iType in jobType.GetInterfaces())
                if (iType.Assembly == typeof(IBaseJobForEach).Assembly &&
                    iType.Name.StartsWith("IJobForEach"))
                    return iType;

            return null;
        }

        static void PrepareEntityQuery(ComponentSystemBase system, Type jobType)
        {
            var iType = GetIJobForEachInterface(jobType);

            ComponentType[] filterChanged;
            int processTypesCount;
            var types = GetComponentTypes(jobType, iType, out processTypesCount, out filterChanged);
            system.GetEntityQueryInternal(types);
        }

#endif
        static unsafe void Initialize<T>(ComponentSystemBase system, EntityQuery entityQuery, Type jobType, Type wrapperJobType,
            bool isParallelFor, ref JobForEachCache cache, out ProcessIterationData iterator, ref T jobData)
            where T : struct
#if UNITY_DOTSPLAYER
        , IBaseJobForEach
#endif
        {
            // Get the job reflection data and cache it if we don't already have it cached.
            if (isParallelFor && cache.JobReflectionDataParallelFor == IntPtr.Zero ||
                !isParallelFor && cache.JobReflectionData == IntPtr.Zero)
            {
#if UNITY_DOTSPLAYER
                if (cache.Types == null)
                {
                    cache.Types = jobData.GetComponentTypes(out cache.ProcessTypesCount, out cache.FilterChanged);
                }
                var res = jobData.GetJobReflection(isParallelFor);
#else
                var iType = GetIJobForEachInterface(jobType);
                if (cache.Types == null)
                    cache.Types = GetComponentTypes(jobType, iType, out cache.ProcessTypesCount,
                        out cache.FilterChanged);

                var res = GetJobReflection(jobType, wrapperJobType, iType, isParallelFor);
#endif
                if (isParallelFor)
                    cache.JobReflectionDataParallelFor = res;
                else
                    cache.JobReflectionData = res;
            }

            // Update cached EntityQuery and ComponentSystem data.
            if (system != null)
            {
                if (cache.ComponentSystem != system)
                {
                    cache.EntityQuery = system.GetEntityQueryInternal(cache.Types);

                    // If the cached filter has changed, update the newly cached EntityQuery with those changes.
                    if (cache.FilterChanged.Length != 0)
                        cache.EntityQuery.SetChangedVersionFilter(cache.FilterChanged);

                    // Otherwise, just reset our newly cached EntityQuery's filter.
                    else
                        cache.EntityQuery.ResetFilter();

                    cache.ComponentSystem = system;
                }
            }
            else if (entityQuery != default)
            {
                if (cache.EntityQuery != entityQuery)
                {
                    // Cache the new EntityQuery and cache that our system is null.
                    cache.EntityQuery = entityQuery;
                    cache.ComponentSystem = null;
                }
            }

            var query = cache.EntityQuery;

            iterator.IsReadOnly0 = iterator.IsReadOnly1 = iterator.IsReadOnly2 = iterator.IsReadOnly3 = iterator.IsReadOnly4 = iterator.IsReadOnly5 = 0;
            fixed(int* isReadOnly = &iterator.IsReadOnly0)
            {
                for (var i = 0; i != cache.ProcessTypesCount; i++)
                    isReadOnly[i] = cache.Types[i].AccessModeType == ComponentType.AccessMode.ReadOnly ? 1 : 0;
            }

            iterator.TypeIndex0 = iterator.TypeIndex1 = iterator.TypeIndex2 = iterator.TypeIndex3 = iterator.TypeIndex4 = iterator.TypeIndex5 = -1;
            fixed(int* typeIndices = &iterator.TypeIndex0)
            {
                for (var i = 0; i != cache.ProcessTypesCount; i++)
                    typeIndices[i] = cache.Types[i].TypeIndex;
            }

            iterator.m_IsParallelFor = isParallelFor;
            iterator.m_Length = query.CalculateChunkCountWithoutFiltering();

            iterator.GlobalSystemVersion = query._GetImpl()->_Access->EntityComponentStore->GlobalSystemVersion;

#if ENABLE_UNITY_COLLECTIONS_CHECKS
            iterator.m_MaxIndex = iterator.m_Length - 1;
            iterator.m_MinIndex = 0;

            iterator.m_Safety0 = iterator.m_Safety1 = iterator.m_Safety2 = iterator.m_Safety3 = iterator.m_Safety4 = iterator.m_Safety5 =
                iterator.m_Safety6 = iterator.m_Safety7 = iterator.m_Safety8 = iterator.m_Safety9 = iterator.m_Safety10 = iterator.m_Safety11 = default(AtomicSafetyHandle);

            var bufferTypeCount = 0;
            iterator.m_SafetyReadOnlyCount = 0;
            fixed(AtomicSafetyHandle* safety = &iterator.m_Safety0)
            {
                for (var i = 0; i != cache.ProcessTypesCount; i++)
                {
                    if (cache.Types[i].AccessModeType == ComponentType.AccessMode.ReadOnly)
                    {
                        safety[iterator.m_SafetyReadOnlyCount] = query.GetSafetyHandle(query.GetIndexInEntityQuery(cache.Types[i].TypeIndex));
                        iterator.m_SafetyReadOnlyCount++;
                        if (cache.Types[i].IsBuffer)
                        {
                            safety[iterator.m_SafetyReadOnlyCount] = query.GetBufferSafetyHandle(query.GetIndexInEntityQuery(cache.Types[i].TypeIndex));
                            iterator.m_SafetyReadOnlyCount++;
                            bufferTypeCount++;
                        }
                    }
                }
            }

            iterator.m_SafetyReadWriteCount = 0;
            fixed(AtomicSafetyHandle* safety = &iterator.m_Safety0)
            {
                for (var i = 0; i != cache.ProcessTypesCount; i++)
                {
                    if (cache.Types[i].AccessModeType == ComponentType.AccessMode.ReadWrite)
                    {
                        safety[iterator.m_SafetyReadOnlyCount + iterator.m_SafetyReadWriteCount] = query.GetSafetyHandle(query.GetIndexInEntityQuery(cache.Types[i].TypeIndex));
                        iterator.m_SafetyReadWriteCount++;
                        if (cache.Types[i].IsBuffer)
                        {
                            safety[iterator.m_SafetyReadOnlyCount + iterator.m_SafetyReadWriteCount] = query.GetBufferSafetyHandle(query.GetIndexInEntityQuery(cache.Types[i].TypeIndex));
                            iterator.m_SafetyReadWriteCount++;
                            bufferTypeCount++;
                        }
                    }
                }
            }

            Assert.AreEqual(cache.ProcessTypesCount + bufferTypeCount, iterator.m_SafetyReadWriteCount + iterator.m_SafetyReadOnlyCount);
#endif
        }

        internal struct JobForEachCache
        {
            public IntPtr JobReflectionData;
            public IntPtr JobReflectionDataParallelFor;
            public ComponentType[] Types;
            public ComponentType[] FilterChanged;

            public int ProcessTypesCount;

            public EntityQuery EntityQuery;
            public ComponentSystemBase ComponentSystem;
        }

        [NativeContainer]
        [NativeContainerSupportsMinMaxWriteRestriction]
        [StructLayout(LayoutKind.Sequential)]
        internal struct ProcessIterationData
        {
            public uint GlobalSystemVersion;

            public int TypeIndex0;
            public int TypeIndex1;
            public int TypeIndex2;
            public int TypeIndex3;
            public int TypeIndex4;
            public int TypeIndex5;

            public int IsReadOnly0;
            public int IsReadOnly1;
            public int IsReadOnly2;
            public int IsReadOnly3;
            public int IsReadOnly4;
            public int IsReadOnly5;

            public bool m_IsParallelFor;

            public int m_Length;

#if ENABLE_UNITY_COLLECTIONS_CHECKS

            public int m_MinIndex;
            public int m_MaxIndex;

#pragma warning disable 414
            public int m_SafetyReadOnlyCount;
            public int m_SafetyReadWriteCount;
            public AtomicSafetyHandle m_Safety0;
            public AtomicSafetyHandle m_Safety1;
            public AtomicSafetyHandle m_Safety2;
            public AtomicSafetyHandle m_Safety3;
            public AtomicSafetyHandle m_Safety4;
            public AtomicSafetyHandle m_Safety5;
            public AtomicSafetyHandle m_Safety6;
            public AtomicSafetyHandle m_Safety7;
            public AtomicSafetyHandle m_Safety8;
            public AtomicSafetyHandle m_Safety9;
            public AtomicSafetyHandle m_Safety10;
            public AtomicSafetyHandle m_Safety11;
#pragma warning restore
#endif
        }

#if !UNITY_DOTSPLAYER
        public static EntityQuery GetEntityQueryForIJobForEach(this ComponentSystemBase system,
            Type jobType)
        {
            var types = GetComponentTypes(jobType);
            if (types != null)
                return system.GetEntityQueryInternal(types);
            else
                return default;
        }

        //NOTE: It would be much better if C# could resolve the branch with generic resolving,
        //      but apparently the interface constraint is not enough..
        public static void PrepareEntityQuery<T>(this T jobData, ComponentSystemBase system)
            where T : struct, IBaseJobForEach
        {
            PrepareEntityQuery(system, typeof(T));
        }

        public static int CalculateEntityCount<T>(this T jobData, ComponentSystemBase system)
            where T : struct, IBaseJobForEach
        {
            return CalculateEntityCount(system, typeof(T));
        }

        static unsafe JobHandle Schedule(void* fullData, NativeArray<byte> prefilterData, int unfilteredLength, int innerloopBatchCount,
            bool isParallelFor, bool isFiltered, ref JobForEachCache cache, void* deferredCountData, JobHandle dependsOn, ScheduleMode mode)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            try
            {
#endif
            if (isParallelFor)
            {
                var scheduleParams = new JobsUtility.JobScheduleParameters(fullData, cache.JobReflectionDataParallelFor, dependsOn, mode);
                if (isFiltered)
                    return JobsUtility.ScheduleParallelForDeferArraySize(ref scheduleParams, innerloopBatchCount, deferredCountData, null);
                else
                    return JobsUtility.ScheduleParallelFor(ref scheduleParams, unfilteredLength, innerloopBatchCount);
            }
            else
            {
                var scheduleParams = new JobsUtility.JobScheduleParameters(fullData, cache.JobReflectionData, dependsOn, mode);
                return JobsUtility.Schedule(ref scheduleParams);
            }
#if ENABLE_UNITY_COLLECTIONS_CHECKS
        }

        catch (InvalidOperationException e)
        {
            prefilterData.Dispose();
            throw e;
        }
#endif
        }
#endif
    }
}

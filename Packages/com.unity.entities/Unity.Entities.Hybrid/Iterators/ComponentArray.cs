using System;
using System.Reflection;

using Unity.Collections.LowLevel.Unsafe;
using UnityEngine;
using UnityEngine.Scripting;

namespace Unity.Entities
{
    public static class EntityQueryExtensionsForComponentArray
    {
        public unsafe static T[] ToComponentArray<T>(this EntityQuery group) where T : Component
        {
            int entityCount = group.CalculateEntityCount();
            var arr = new T[entityCount];

            var iterator = group.GetArchetypeChunkIterator();
            var indexInEntityQuery = group.GetIndexInEntityQuery(TypeManager.GetTypeIndex<T>());

            var entityCounter = 0;
            var mcs = group._GetImpl()->_Access->ManagedComponentStore;
            while (iterator.MoveNext())
            {
                var chunk = iterator.CurrentArchetypeChunk;
                for (int entityIndex = 0; entityIndex < chunk.Count; ++entityIndex)
                {
                    arr[entityCounter++] = (T)iterator.GetManagedObject(mcs, indexInEntityQuery, entityIndex);
                }
            }

            return arr;
        }
    }
}

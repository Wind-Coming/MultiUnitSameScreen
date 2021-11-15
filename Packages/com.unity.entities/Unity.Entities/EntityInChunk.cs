using System;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Jobs;

namespace Unity.Entities
{
    public unsafe struct EntityInChunk : IComparable<EntityInChunk>, IEquatable<EntityInChunk>
    {
        internal Chunk* Chunk;
        internal int IndexInChunk;

        public int CompareTo(EntityInChunk other)
        {
            ulong lhs = (ulong)Chunk;
            ulong rhs = (ulong)other.Chunk;
            int chunkCompare = lhs < rhs ? -1 : 1;
            int indexCompare = IndexInChunk - other.IndexInChunk;
            return (lhs != rhs) ? chunkCompare : indexCompare;
        }

        public bool Equals(EntityInChunk other)
        {
            return CompareTo(other) == 0;
        }
    }
}

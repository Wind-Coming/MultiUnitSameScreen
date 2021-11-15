using System;
using System.Runtime.InteropServices;
using Unity.Assertions;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities
{
    [Flags]
    internal enum ChunkFlags
    {
        None = 0,
        Unused0 = 1 << 0,
        Unused1 = 1 << 1,
        TempAssertWillDestroyAllInLinkedEntityGroup = 1 << 2
    }

    [StructLayout(LayoutKind.Explicit)]
    internal unsafe struct Chunk
    {
        // Chunk header START
        [FieldOffset(0)]
        public Archetype* Archetype;
        // 4-byte padding on 32-bit architectures here

        [FieldOffset(8)]
        public Entity metaChunkEntity;

        // This is meant as read-only.
        // EntityComponentStore.SetChunkCount should be used to change the count.
        [FieldOffset(16)]
        public int Count;
        [FieldOffset(20)]
        public int Capacity;
        [FieldOffset(24)]
        public int ListIndex;
        [FieldOffset(28)]
        public int ListWithEmptySlotsIndex;

        // Special chunk behaviors
        [FieldOffset(32)]
        public uint Flags;

        // Incrementing automatically for each chunk
        [FieldOffset(36)]
        public ulong SequenceNumber;

        // Chunk header END

        // Component data buffer
        // This is where the actual chunk data starts.
        // It's declared like this so we can skip the header part of the chunk and just get to the data.
        public const int kBufferOffset = 64; // (must be cache line aligned)
        [FieldOffset(kBufferOffset)]
        public fixed byte Buffer[4];

        public const int kChunkSize = 16 * 1024 - 256; // allocate a bit less to allow for header overhead
        public const int kBufferSize = kChunkSize - kBufferOffset;
        public const int kMaximumEntitiesPerChunk = kBufferSize / 8;

        public int UnusedCount => Capacity - Count;

        public uint GetChangeVersion(int indexInArchetype)
        {
            return Archetype->Chunks.GetChangeVersion(indexInArchetype, ListIndex);
        }

        public uint GetOrderVersion()
        {
            return Archetype->Chunks.GetOrderVersion(ListIndex);
        }

        public void SetChangeVersion(int indexInArchetype, uint version)
        {
            Archetype->Chunks.SetChangeVersion(indexInArchetype, ListIndex, version);
        }

        public void SetAllChangeVersions(uint version)
        {
            Archetype->Chunks.SetAllChangeVersion(ListIndex, version);
        }

        public void SetOrderVersion(uint version)
        {
            Archetype->Chunks.SetOrderVersion(ListIndex, version);
        }

        public int GetSharedComponentValue(int sharedComponentIndexInArchetype)
        {
            return Archetype->Chunks.GetSharedComponentValue(sharedComponentIndexInArchetype, ListIndex);
        }

        public SharedComponentValues SharedComponentValues => Archetype->Chunks.GetSharedComponentValues(ListIndex);

        public static int GetChunkBufferSize()
        {
            // The amount of available space in a chunk is the max chunk size, kChunkSize,
            // minus the size of the Chunk's metadata stored in the fields preceding Chunk.Buffer
            return kChunkSize - kBufferOffset;
        }

        public bool MatchesFilter(MatchingArchetype* match, ref EntityQueryFilter filter)
        {
            return match->ChunkMatchesFilter(ListIndex, ref filter);
        }

        public int GetSharedComponentIndex(MatchingArchetype* match, int indexInEntityQuery)
        {
            var sharedComponentIndexInArchetype = match->IndexInArchetype[indexInEntityQuery];
            var sharedComponentIndexOffset = sharedComponentIndexInArchetype - match->Archetype->FirstSharedComponent;
            return GetSharedComponentValue(sharedComponentIndexOffset);
        }
    }
}

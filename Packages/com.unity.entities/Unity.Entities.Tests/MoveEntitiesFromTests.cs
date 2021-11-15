using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;

namespace Unity.Entities.Tests
{
    class MoveEntitiesFromTests : ECSTestsFixture
    {
        [Test]
        public void MoveEntitiesToSameEntityManagerThrows()
        {
            Assert.Throws<ArgumentException>(() => { m_Manager.MoveEntitiesFrom(m_Manager); });
        }

        [Test]
        public void MoveEntities()
        {
            var creationWorld = new World("CreationWorld");
            var creationManager = creationWorld.EntityManager;

            var archetype = creationManager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2));

            var entities = new NativeArray<Entity>(10000, Allocator.Temp);
            creationManager.CreateEntity(archetype, entities);
            for (int i = 0; i != entities.Length; i++)
                creationManager.SetComponentData(entities[i], new EcsTestData(i));

            m_Manager.Debug.CheckInternalConsistency();
            creationManager.Debug.CheckInternalConsistency();

            m_Manager.MoveEntitiesFrom(creationManager);

            for (int i = 0; i != entities.Length; i++)
                Assert.IsFalse(creationManager.Exists(entities[i]));

            m_Manager.Debug.CheckInternalConsistency();
            creationManager.Debug.CheckInternalConsistency();

            var group = m_Manager.CreateEntityQuery(typeof(EcsTestData));
            Assert.AreEqual(entities.Length, group.CalculateEntityCount());
            Assert.AreEqual(0, creationManager.CreateEntityQuery(typeof(EcsTestData)).CalculateEntityCount());

            // We expect that the order of the crated entities is the same as in the creation scene
            var testDataArray = group.ToComponentDataArray<EcsTestData>(Allocator.TempJob);
            for (int i = 0; i != testDataArray.Length; i++)
                Assert.AreEqual(i, testDataArray[i].value);

            testDataArray.Dispose();
            entities.Dispose();
            creationWorld.Dispose();
        }

        [Test]
        public void MoveEntitiesWithSharedComponentData()
        {
            var creationWorld = new World("CreationWorld");
            var creationManager = creationWorld.EntityManager;

            var archetype = creationManager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2), typeof(SharedData1));

            var entities = new NativeArray<Entity>(10000, Allocator.Temp);
            creationManager.CreateEntity(archetype, entities);
            for (int i = 0; i != entities.Length; i++)
            {
                creationManager.SetComponentData(entities[i], new EcsTestData(i));
                creationManager.SetSharedComponentData(entities[i], new SharedData1(i % 5));
            }

            m_Manager.Debug.CheckInternalConsistency();
            creationManager.Debug.CheckInternalConsistency();

            m_Manager.MoveEntitiesFrom(creationManager);

            m_Manager.Debug.CheckInternalConsistency();
            creationManager.Debug.CheckInternalConsistency();

            var group = m_Manager.CreateEntityQuery(typeof(EcsTestData), typeof(SharedData1));
            Assert.AreEqual(entities.Length, group.CalculateEntityCount());
            Assert.AreEqual(0, creationManager.CreateEntityQuery(typeof(EcsTestData)).CalculateEntityCount());

            // We expect that the shared component data matches the correct entities
            var chunks = group.CreateArchetypeChunkArray(Allocator.TempJob);
            for (int i = 0; i < chunks.Length; ++i)
            {
                var chunk = chunks[i];
                var shared = chunk.GetSharedComponentData(m_Manager.GetArchetypeChunkSharedComponentType<SharedData1>(), m_Manager);
                var testDataArray = chunk.GetNativeArray(m_Manager.GetArchetypeChunkComponentType<EcsTestData>(true));
                for (int j = 0; j < testDataArray.Length; ++j)
                {
                    Assert.AreEqual(shared.value, testDataArray[i].value % 5);
                }
            }

            chunks.Dispose();
            entities.Dispose();
            creationWorld.Dispose();
        }

        [Test]
        public void MoveEntitiesWithChunkComponentsWithQuery()
        {
            var creationWorld = new World("CreationWorld");
            var creationManager = creationWorld.EntityManager;

            var archetype = creationManager.CreateArchetype(typeof(EcsTestData), ComponentType.ChunkComponent<EcsTestData2>());

            var entities = new NativeArray<Entity>(10000, Allocator.Temp);
            creationManager.CreateEntity(archetype, entities);
            ArchetypeChunk currentChunk = ArchetypeChunk.Null;
            int chunkCount = 0;
            for (int i = 0; i != entities.Length; i++)
            {
                if (creationManager.GetChunk(entities[i]) != currentChunk)
                {
                    currentChunk = creationManager.GetChunk(entities[i]);
                    creationManager.SetChunkComponentData(currentChunk, new EcsTestData2(++chunkCount));
                }
                creationManager.SetComponentData(entities[i], new EcsTestData(chunkCount));
            }

            m_Manager.Debug.CheckInternalConsistency();
            creationManager.Debug.CheckInternalConsistency();

            var query = creationManager.CreateEntityQuery(typeof(EcsTestData));

            m_Manager.MoveEntitiesFrom(creationManager, query);

            m_Manager.Debug.CheckInternalConsistency();
            creationManager.Debug.CheckInternalConsistency();

            var group = m_Manager.CreateEntityQuery(typeof(EcsTestData), ComponentType.ChunkComponent<EcsTestData2>());
            Assert.AreEqual(entities.Length, group.CalculateEntityCount());
            Assert.AreEqual(0, creationManager.CreateEntityQuery(typeof(EcsTestData)).CalculateEntityCount());

            var movedEntities = group.ToEntityArray(Allocator.TempJob);
            for (int i = 0; i < movedEntities.Length; ++i)
            {
                var entity = movedEntities[i];
                var valueFromComponent = m_Manager.GetComponentData<EcsTestData>(entity).value;
                var valueFromChunkComponent = m_Manager.GetChunkComponentData<EcsTestData2>(entity).value0;
                Assert.AreEqual(valueFromComponent, valueFromChunkComponent);
            }

            movedEntities.Dispose();
            entities.Dispose();
            creationWorld.Dispose();
        }

        [Test]
        public void MoveEntitiesWithChunkComponents()
        {
            var creationWorld = new World("CreationWorld");
            var creationManager = creationWorld.EntityManager;

            var archetype = creationManager.CreateArchetype(typeof(EcsTestData), ComponentType.ChunkComponent<EcsTestData2>());

            var entities = new NativeArray<Entity>(10000, Allocator.Temp);
            creationManager.CreateEntity(archetype, entities);
            ArchetypeChunk currentChunk = ArchetypeChunk.Null;
            int chunkCount = 0;
            for (int i = 0; i != entities.Length; i++)
            {
                if (creationManager.GetChunk(entities[i]) != currentChunk)
                {
                    currentChunk = creationManager.GetChunk(entities[i]);
                    creationManager.SetChunkComponentData(currentChunk, new EcsTestData2(++chunkCount));
                }
                creationManager.SetComponentData(entities[i], new EcsTestData(chunkCount));
            }

            m_Manager.Debug.CheckInternalConsistency();
            creationManager.Debug.CheckInternalConsistency();

            m_Manager.MoveEntitiesFrom(creationManager);

            m_Manager.Debug.CheckInternalConsistency();
            creationManager.Debug.CheckInternalConsistency();

            var group = m_Manager.CreateEntityQuery(typeof(EcsTestData), ComponentType.ChunkComponent<EcsTestData2>());
            Assert.AreEqual(entities.Length, group.CalculateEntityCount());
            Assert.AreEqual(0, creationManager.CreateEntityQuery(typeof(EcsTestData)).CalculateEntityCount());

            var movedEntities = group.ToEntityArray(Allocator.TempJob);
            for (int i = 0; i < movedEntities.Length; ++i)
            {
                var entity = movedEntities[i];
                var valueFromComponent = m_Manager.GetComponentData<EcsTestData>(entity).value;
                var valueFromChunkComponent = m_Manager.GetChunkComponentData<EcsTestData2>(entity).value0;
                Assert.AreEqual(valueFromComponent, valueFromChunkComponent);
            }

            movedEntities.Dispose();
            entities.Dispose();
            creationWorld.Dispose();
        }

        [Test]
        public void MoveEntitiesWithEntityQuery()
        {
            var creationWorld = new World("CreationWorld");
            var creationManager = creationWorld.EntityManager;

            var archetype = creationManager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2), typeof(SharedData1));

            var entities = new NativeArray<Entity>(10000, Allocator.Temp);
            creationManager.CreateEntity(archetype, entities);
            for (int i = 0; i != entities.Length; i++)
            {
                creationManager.SetComponentData(entities[i], new EcsTestData(i));
                creationManager.SetSharedComponentData(entities[i], new SharedData1(i % 5));
            }

            m_Manager.Debug.CheckInternalConsistency();
            creationManager.Debug.CheckInternalConsistency();

            var filteredComponentGroup = creationManager.CreateEntityQuery(typeof(EcsTestData), typeof(SharedData1));
            filteredComponentGroup.SetSharedComponentFilter(new SharedData1(2));

            var entityRemapping = creationManager.CreateEntityRemapArray(Allocator.TempJob);
            m_Manager.MoveEntitiesFrom(creationManager, filteredComponentGroup, entityRemapping);

            filteredComponentGroup.Dispose();


            m_Manager.Debug.CheckInternalConsistency();
            creationManager.Debug.CheckInternalConsistency();

            var group = m_Manager.CreateEntityQuery(typeof(EcsTestData), typeof(SharedData1));
            Assert.AreEqual(2000, group.CalculateEntityCount());
            Assert.AreEqual(8000, creationManager.CreateEntityQuery(typeof(EcsTestData)).CalculateEntityCount());

            // We expect that the shared component data matches the correct entities
            var chunks = group.CreateArchetypeChunkArray(Allocator.TempJob);
            for (int i = 0; i < chunks.Length; ++i)
            {
                var chunk = chunks[i];
                var shared = chunk.GetSharedComponentData(m_Manager.GetArchetypeChunkSharedComponentType<SharedData1>(), m_Manager);
                var testDataArray = chunk.GetNativeArray(m_Manager.GetArchetypeChunkComponentType<EcsTestData>(true));
                for (int j = 0; j < testDataArray.Length; ++j)
                {
                    Assert.AreEqual(shared.value, testDataArray[i].value % 5);
                }

                for (int j = 0; j != testDataArray.Length; ++j)
                    Assert.AreEqual(shared.value, 2);
            }

            chunks.Dispose();
            entityRemapping.Dispose();
            entities.Dispose();
            creationWorld.Dispose();
        }

        [Test]
        public void MoveEntitiesWithComponentGroupMovesChunkComponents()
        {
            var creationWorld = new World("CreationWorld");
            var creationManager = creationWorld.EntityManager;
            var archetype = creationManager.CreateArchetype(typeof(EcsTestData), ComponentType.ChunkComponent<EcsTestData2>(), typeof(SharedData1));

            var entities = new NativeArray<Entity>(10000, Allocator.Temp);
            creationManager.CreateEntity(archetype, entities);
            for (int i = 0; i != entities.Length; i++)
            {
                creationManager.SetComponentData(entities[i], new EcsTestData(i));
                creationManager.SetSharedComponentData(entities[i], new SharedData1(i % 5));
            }

            var srcGroup = creationManager.CreateEntityQuery(typeof(EcsTestData), ComponentType.ChunkComponent<EcsTestData2>(), typeof(SharedData1));

            var chunksPerValue = new int[5];
            var chunks = srcGroup.CreateArchetypeChunkArray(Allocator.TempJob);

            var sharedData1Type = creationManager.GetArchetypeChunkSharedComponentType<SharedData1>();
            var ecsTestData2Type = creationManager.GetArchetypeChunkComponentType<EcsTestData2>(false);

            foreach (var chunk in chunks)
            {
                int sharedIndex = chunk.GetSharedComponentIndex(sharedData1Type);
                var shared = creationManager.GetSharedComponentData<SharedData1>(sharedIndex);
                chunk.SetChunkComponentData(ecsTestData2Type, new EcsTestData2 {value0 = shared.value, value1 = 47 * shared.value});
                chunksPerValue[shared.value]++;
            }
            chunks.Dispose();

            m_Manager.Debug.CheckInternalConsistency();
            creationManager.Debug.CheckInternalConsistency();

            var filteredComponentGroup = creationManager.CreateEntityQuery(typeof(EcsTestData), typeof(SharedData1));
            filteredComponentGroup.SetSharedComponentFilter(new SharedData1(2));

            var entityRemapping = creationManager.CreateEntityRemapArray(Allocator.TempJob);
            m_Manager.MoveEntitiesFrom(creationManager, filteredComponentGroup, entityRemapping);

            filteredComponentGroup.Dispose();


            m_Manager.Debug.CheckInternalConsistency();
            creationManager.Debug.CheckInternalConsistency();

            var dstGroup = m_Manager.CreateEntityQuery(typeof(EcsTestData), ComponentType.ChunkComponent<EcsTestData2>(), typeof(SharedData1));
            Assert.AreEqual(2000, dstGroup.CalculateEntityCount());
            Assert.AreEqual(8000, creationManager.CreateEntityQuery(typeof(EcsTestData)).CalculateEntityCount());

            int expectedMovedChunkCount = chunksPerValue[2];
            int sum = 0;
            foreach (var c in chunksPerValue) sum += c;
            int expectedRemainingChunkCount = sum - expectedMovedChunkCount;

            var movedChunkHeaderGroup = m_Manager.CreateEntityQuery(typeof(EcsTestData2), typeof(ChunkHeader));
            var remainingChunkHeaderGroup = creationManager.CreateEntityQuery(typeof(EcsTestData2), typeof(ChunkHeader));
            Assert.AreEqual(expectedMovedChunkCount, movedChunkHeaderGroup.CalculateEntityCount());
            Assert.AreEqual(expectedRemainingChunkCount, remainingChunkHeaderGroup.CalculateEntityCount());


            var dstEntityArray = dstGroup.ToEntityArray(Allocator.TempJob);
            for (int i = 0; i != dstEntityArray.Length; i++)
            {
                var chunkComponent = m_Manager.GetChunkComponentData<EcsTestData2>(dstEntityArray[i]);
                int expectedValue = m_Manager.GetComponentData<EcsTestData>(dstEntityArray[i]).value % 5;
                Assert.AreEqual(2, expectedValue);
                Assert.AreEqual(expectedValue, m_Manager.GetSharedComponentData<SharedData1>(dstEntityArray[i]).value);
                Assert.AreEqual(expectedValue, chunkComponent.value0);
                Assert.AreEqual(expectedValue * 47, chunkComponent.value1);
            }

            var srcEntityArray = srcGroup.ToEntityArray(Allocator.TempJob);
            for (int i = 0; i != srcEntityArray.Length; i++)
            {
                var chunkComponent = creationManager.GetChunkComponentData<EcsTestData2>(srcEntityArray[i]);
                int expectedValue = creationManager.GetComponentData<EcsTestData>(srcEntityArray[i]).value % 5;
                Assert.AreNotEqual(2, expectedValue);
                Assert.AreEqual(expectedValue, creationManager.GetSharedComponentData<SharedData1>(srcEntityArray[i]).value);
                Assert.AreEqual(expectedValue, chunkComponent.value0);
                Assert.AreEqual(expectedValue * 47, chunkComponent.value1);
            }

            dstGroup.Dispose();
            srcGroup.Dispose();
            movedChunkHeaderGroup.Dispose();
            remainingChunkHeaderGroup.Dispose();
            dstEntityArray.Dispose();
            srcEntityArray.Dispose();
            entityRemapping.Dispose();
            entities.Dispose();
            creationWorld.Dispose();
        }

        [Test]
        public void MoveEntitiesWithChunkHeaderChunksThrows()
        {
            var creationWorld = new World("CreationWorld");
            var creationManager = creationWorld.EntityManager;

            var archetype = creationManager.CreateArchetype(typeof(EcsTestData), ComponentType.ChunkComponent<EcsTestData2>());

            var entities = new NativeArray<Entity>(10000, Allocator.Temp);
            creationManager.CreateEntity(archetype, entities);
            entities.Dispose();

            m_Manager.Debug.CheckInternalConsistency();
            creationManager.Debug.CheckInternalConsistency();

            var componentGroupToMove = creationManager.CreateEntityQuery(typeof(EcsTestData2), typeof(ChunkHeader));
            var entityRemapping = creationManager.CreateEntityRemapArray(Allocator.TempJob);

            Assert.Throws<ArgumentException>(() => m_Manager.MoveEntitiesFrom(creationManager, componentGroupToMove, entityRemapping));
            Assert.Throws<ArgumentException>(() => m_Manager.MoveEntitiesFrom(out var output, creationManager, componentGroupToMove, entityRemapping));

            entityRemapping.Dispose();
            componentGroupToMove.Dispose();
            creationWorld.Dispose();
        }

        [Test]
        public void MoveEntitiesAppendsToExistingEntities()
        {
            int numberOfEntitiesPerManager = 10000;

            var targetArchetype = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2));
            var targetEntities = new NativeArray<Entity>(numberOfEntitiesPerManager, Allocator.Temp);
            m_Manager.CreateEntity(targetArchetype, targetEntities);
            for (int i = 0; i != targetEntities.Length; i++)
                m_Manager.SetComponentData(targetEntities[i], new EcsTestData(i));

            var sourceWorld = new World("SourceWorld");
            var sourceManager = sourceWorld.EntityManager;
            var sourceArchetype = sourceManager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2));
            var sourceEntities = new NativeArray<Entity>(numberOfEntitiesPerManager, Allocator.Temp);
            sourceManager.CreateEntity(sourceArchetype, sourceEntities);
            for (int i = 0; i != sourceEntities.Length; i++)
                sourceManager.SetComponentData(sourceEntities[i], new EcsTestData(numberOfEntitiesPerManager + i));

            m_Manager.Debug.CheckInternalConsistency();
            sourceManager.Debug.CheckInternalConsistency();

            m_Manager.MoveEntitiesFrom(sourceManager);

            m_Manager.Debug.CheckInternalConsistency();
            sourceManager.Debug.CheckInternalConsistency();

            var group = m_Manager.CreateEntityQuery(typeof(EcsTestData));
            Assert.AreEqual(numberOfEntitiesPerManager * 2, group.CalculateEntityCount());
            Assert.AreEqual(0, sourceManager.CreateEntityQuery(typeof(EcsTestData)).CalculateEntityCount());

            // We expect that the order of the crated entities is the same as in the creation scene
            var testDataArray = group.ToComponentDataArray<EcsTestData>(Allocator.TempJob);
            for (int i = 0; i != testDataArray.Length; i++)
                Assert.AreEqual(i, testDataArray[i].value);

            testDataArray.Dispose();
            targetEntities.Dispose();
            sourceEntities.Dispose();
            sourceWorld.Dispose();
        }

        [Test]
        public void MoveEntitiesPatchesEntityReferences()
        {
            int numberOfEntitiesPerManager = 10000;

            var targetArchetype = m_Manager.CreateArchetype(typeof(EcsTestDataEntity));
            var targetEntities = new NativeArray<Entity>(numberOfEntitiesPerManager, Allocator.Temp);
            m_Manager.CreateEntity(targetArchetype, targetEntities);
            for (int i = 0; i != targetEntities.Length; i++)
                m_Manager.SetComponentData(targetEntities[i], new EcsTestDataEntity(i, targetEntities[i]));

            var sourceWorld = new World("SourceWorld");
            var sourceManager = sourceWorld.EntityManager;
            var sourceArchetype = sourceManager.CreateArchetype(typeof(EcsTestDataEntity));
            var sourceEntities = new NativeArray<Entity>(numberOfEntitiesPerManager, Allocator.Temp);
            sourceManager.CreateEntity(sourceArchetype, sourceEntities);
            for (int i = 0; i != sourceEntities.Length; i++)
                sourceManager.SetComponentData(sourceEntities[i], new EcsTestDataEntity(numberOfEntitiesPerManager + i, sourceEntities[i]));

            m_Manager.Debug.CheckInternalConsistency();
            sourceManager.Debug.CheckInternalConsistency();

            m_Manager.MoveEntitiesFrom(sourceManager);

            m_Manager.Debug.CheckInternalConsistency();
            sourceManager.Debug.CheckInternalConsistency();

            var group = m_Manager.CreateEntityQuery(typeof(EcsTestDataEntity));
            Assert.AreEqual(numberOfEntitiesPerManager * 2, group.CalculateEntityCount());
            Assert.AreEqual(0, sourceManager.CreateEntityQuery(typeof(EcsTestDataEntity)).CalculateEntityCount());

            var testDataArray = group.ToComponentDataArray<EcsTestDataEntity>(Allocator.TempJob);
            for (int i = 0; i != testDataArray.Length; i++)
                Assert.AreEqual(testDataArray[i].value0, m_Manager.GetComponentData<EcsTestDataEntity>(testDataArray[i].value1).value0);

            testDataArray.Dispose();
            targetEntities.Dispose();
            sourceEntities.Dispose();
            sourceWorld.Dispose();
        }

        [Test]
        public void MoveEntitiesFromCanReturnEntities()
        {
            var creationWorld = new World("CreationWorld");
            var creationManager = creationWorld.EntityManager;

            var archetype = creationManager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2));

            int numberOfEntitiesWeCreated = 10000;
            var entities = new NativeArray<Entity>(numberOfEntitiesWeCreated, Allocator.Temp);
            creationManager.CreateEntity(archetype, entities);
            for (int i = 0; i != entities.Length; i++)
                creationManager.SetComponentData(entities[i], new EcsTestData(i));

            m_Manager.Debug.CheckInternalConsistency();
            creationManager.Debug.CheckInternalConsistency();

            NativeArray<Entity> movedEntities;
            m_Manager.MoveEntitiesFrom(out movedEntities, creationManager);

            // make sure that the count of moved entities matches the count of entities we initially created

            Assert.AreEqual(numberOfEntitiesWeCreated, movedEntities.Length);

            // make sure that each of the entities made the journey, and none were duplicated

            var references = new NativeArray<int>(numberOfEntitiesWeCreated, Allocator.Temp);
            for (int i = 0; i < numberOfEntitiesWeCreated; ++i)
            {
                var data = m_Manager.GetComponentData<EcsTestData>(movedEntities[i]);
                Assert.AreEqual(0, references[data.value]);
                ++references[data.value];
            }
            for (int i = 0; i < numberOfEntitiesWeCreated; ++i)
            {
                Assert.AreEqual(1, references[i]);
            }

            references.Dispose();
            movedEntities.Dispose();
            entities.Dispose();
            creationWorld.Dispose();
        }

        [Test]
        public unsafe void MoveEntitiesArchetypeChunkCountMatches()
        {
            var creationWorld = new World("CreationWorld");
            var creationManager = creationWorld.EntityManager;

            var archetype = creationManager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2));

            var entities = new NativeArray<Entity>(10000, Allocator.Temp);
            creationManager.CreateEntity(archetype, entities);
            for (int i = 0; i != entities.Length; i++)
                creationManager.SetComponentData(entities[i], new EcsTestData(i));

            m_Manager.Debug.CheckInternalConsistency();
            creationManager.Debug.CheckInternalConsistency();

            var chunkData = archetype.Archetype->Chunks;
            int sizeOfBuffer = sizeof(int) * chunkData.Count;
            var chunkDataCopy =
                (int*)UnsafeUtility.Malloc(sizeOfBuffer, 64, Allocator.Temp);
            UnsafeUtility.MemCpy(chunkDataCopy, chunkData.GetChunkEntityCountArray(), sizeOfBuffer);

            m_Manager.MoveEntitiesFrom(creationManager);

            var archetypeAfter = m_Manager.CreateArchetype(typeof(EcsTestData), typeof(EcsTestData2));
            var chunkDataAfter = archetypeAfter.Archetype->Chunks;

            Assert.IsTrue(UnsafeUtility.MemCmp(chunkDataCopy, chunkDataAfter.GetChunkEntityCountArray(), sizeOfBuffer) == 0);

            m_Manager.Debug.CheckInternalConsistency();
            creationManager.Debug.CheckInternalConsistency();

            entities.Dispose();
            creationWorld.Dispose();
        }

        [Test]
        public void MoveEntitiesFromChunksAreConsideredChangedOnlyOnce()
        {
            var creationWorld = new World("CreationWorld");
            var creationManager = creationWorld.EntityManager;
            var entity = creationManager.CreateEntity();
            creationManager.AddComponentData(entity, new EcsTestData(42));

            var system = World.GetOrCreateSystem<TestEcsChangeSystem>();
            Assert.AreEqual(0, system.NumChanged);

            m_Manager.MoveEntitiesFrom(creationManager);

            system.Update();
            Assert.AreEqual(1, system.NumChanged);

            system.Update();
            Assert.AreEqual(0, system.NumChanged);

            creationWorld.Dispose();
        }

        [Test]
        public void MoveEntitiesVersionBumping([Values] bool useQuery)
        {
            const int creationWorldVersion = 42;
            const int dstWorldVersion = 500;
            const int initialSharedVersion = 2;
            //@TODO: AddSharedComponentData should be optimized to only do one move
            const int initialOrderVersion = 5;

            var creationWorld = new World("CreationWorld");
            var creationManager = creationWorld.EntityManager;

            Assert.AreEqual(0, m_Manager.GetSharedComponentOrderVersion(new SharedData1(1)));
            Assert.AreEqual(0, m_Manager.GetComponentOrderVersion<EcsTestData>());

            var entity = m_Manager.CreateEntity(typeof(EcsTestData));
            m_Manager.AddSharedComponentData(entity, new SharedData1(1));

            Assert.AreEqual(initialSharedVersion, m_Manager.GetSharedComponentOrderVersion(new SharedData1(1)));
            Assert.AreEqual(initialOrderVersion, m_Manager.GetComponentOrderVersion<EcsTestData>());
            AssetHasChangeVersion<EcsTestData>(entity, 1U);

            creationManager.Debug.SetGlobalSystemVersion(creationWorldVersion);
            m_Manager.Debug.SetGlobalSystemVersion(dstWorldVersion);

            var e = creationManager.CreateEntity(typeof(EcsTestData));
            creationManager.AddSharedComponentData(e, new SharedData1(1));

            if (useQuery)
                m_Manager.MoveEntitiesFrom(creationManager, creationManager.UniversalQuery);
            else
                m_Manager.MoveEntitiesFrom(creationManager);

            var movedEntity = m_Manager.GetAllEntities(Allocator.Temp)[1];
            Assert.AreNotEqual(movedEntity, entity);

            Assert.IsTrue(initialSharedVersion < m_Manager.GetSharedComponentOrderVersion(new SharedData1(1)));
            Assert.IsTrue(initialOrderVersion < m_Manager.GetComponentOrderVersion<EcsTestData>());

            AssetHasChangeVersion<EcsTestData>(movedEntity, dstWorldVersion);
            AssetHasChangeVersion<EcsTestData>(entity, 1);

            creationWorld.Dispose();
        }

#if !UNITY_DOTSPLAYER_IL2CPP
// https://unity3d.atlassian.net/browse/DOTSR-1432

#if !UNITY_DISABLE_MANAGED_COMPONENTS
        [Test]
        [DotsRuntimeFixme] // No Unity.Properties Support
        public void MoveEntitiesPatchesEntityReferences_ManagedComponents([Values] bool useFilteredMove)
        {
            int numberOfEntitiesPerManager = 10000;

            var targetArchetype = m_Manager.CreateArchetype(typeof(EcsTestManagedDataEntity));
            var targetEntities = new NativeArray<Entity>(numberOfEntitiesPerManager, Allocator.Temp);
            m_Manager.CreateEntity(targetArchetype, targetEntities);
            for (int i = 0; i != targetEntities.Length; i++)
                m_Manager.SetComponentData(targetEntities[i], new EcsTestManagedDataEntity(i.ToString(), targetEntities[i]));

            var sourceWorld = new World("SourceWorld");
            var sourceManager = sourceWorld.EntityManager;

            // create some temporary entities to ensure that entity ids and managed component indices need to be remapped during move
            var tempEntities = new NativeArray<Entity>(100, Allocator.Temp);
            sourceManager.CreateEntity(sourceManager.CreateArchetype(typeof(EcsTestManagedComponent)), tempEntities);
            for (int i = 0; i < tempEntities.Length; ++i)
                sourceManager.SetComponentData(tempEntities[i], new EcsTestManagedComponent {value = i.ToString()});

            var sourceArchetype = sourceManager.CreateArchetype(typeof(EcsTestManagedDataEntity));
            var sourceEntities = new NativeArray<Entity>(numberOfEntitiesPerManager, Allocator.Temp);
            sourceManager.CreateEntity(sourceArchetype, sourceEntities);
            for (int i = 0; i != sourceEntities.Length; i++)
                sourceManager.SetComponentData(sourceEntities[i], new EcsTestManagedDataEntity((numberOfEntitiesPerManager + i).ToString(), sourceEntities[i]));

            sourceManager.DestroyEntity(tempEntities);
            tempEntities.Dispose();

            m_Manager.Debug.CheckInternalConsistency();
            sourceManager.Debug.CheckInternalConsistency();

            if (useFilteredMove)
            {
                m_Manager.MoveEntitiesFrom(sourceManager, sourceManager.CreateEntityQuery(typeof(EcsTestManagedDataEntity)));
            }
            else
            {
                m_Manager.MoveEntitiesFrom(sourceManager);
            }

            m_Manager.Debug.CheckInternalConsistency();
            sourceManager.Debug.CheckInternalConsistency();

            var group = m_Manager.CreateEntityQuery(typeof(EcsTestManagedDataEntity));
            Assert.AreEqual(numberOfEntitiesPerManager * 2, group.CalculateEntityCount());
            Assert.AreEqual(0, sourceManager.CreateEntityQuery(typeof(EcsTestManagedDataEntity)).CalculateEntityCount());

            var testDataArray = group.ToComponentDataArray<EcsTestManagedDataEntity>();
            for (int i = 0; i != testDataArray.Length; i++)
                Assert.AreEqual(testDataArray[i].value0, m_Manager.GetComponentData<EcsTestManagedDataEntity>(testDataArray[i].value1).value0);

            targetEntities.Dispose();
            sourceEntities.Dispose();
            sourceWorld.Dispose();
        }

        [Test]
        [DotsRuntimeFixme] // No Unity.Properties Support
        public void MoveEntitiesPatchesEntityReferencesInCollections_ManagedComponents()
        {
            int numberOfEntitiesPerManager = 100;

            var targetArchetype = m_Manager.CreateArchetype(typeof(EcsTestManagedDataEntityCollection));
            var targetEntities = new NativeArray<Entity>(numberOfEntitiesPerManager, Allocator.Temp);
            m_Manager.CreateEntity(targetArchetype, targetEntities);
            for (int i = 0; i != targetEntities.Length; i++)
            {
                var stringSet = new List<string>();
                var entitySet = new NativeArray<Entity>(i + 1, Allocator.Temp);
                for (int j = 0; j <= i; ++j)
                {
                    stringSet.Add(j.ToString());
                    entitySet[j] = targetEntities[j];
                }
                m_Manager.SetComponentData(targetEntities[i], new EcsTestManagedDataEntityCollection(stringSet.ToArray(), entitySet.ToArray()));
                entitySet.Dispose();
            }

            var sourceWorld = new World("SourceWorld");
            var sourceManager = sourceWorld.EntityManager;
            var sourceArchetype = sourceManager.CreateArchetype(typeof(EcsTestManagedDataEntityCollection));
            var sourceEntities = new NativeArray<Entity>(numberOfEntitiesPerManager, Allocator.Temp);
            sourceManager.CreateEntity(sourceArchetype, sourceEntities);
            for (int i = 0; i != sourceEntities.Length; i++)
            {
                var stringSet = new List<string>();
                var entitySet = new NativeArray<Entity>(i + 1, Allocator.Temp);
                for (int j = 0; j <= i; ++j)
                {
                    stringSet.Add((numberOfEntitiesPerManager + j).ToString());
                    entitySet[j] = sourceEntities[j];
                }
                sourceManager.SetComponentData(sourceEntities[i], new EcsTestManagedDataEntityCollection(stringSet.ToArray(), entitySet.ToArray()));
                entitySet.Dispose();
            }

            m_Manager.Debug.CheckInternalConsistency();
            sourceManager.Debug.CheckInternalConsistency();

            m_Manager.MoveEntitiesFrom(sourceManager);

            m_Manager.Debug.CheckInternalConsistency();
            sourceManager.Debug.CheckInternalConsistency();

            var group = m_Manager.CreateEntityQuery(typeof(EcsTestManagedDataEntityCollection));
            Assert.AreEqual(numberOfEntitiesPerManager * 2, group.CalculateEntityCount());
            Assert.AreEqual(0, sourceManager.CreateEntityQuery(typeof(EcsTestManagedDataEntityCollection)).CalculateEntityCount());

            var testDataArray = group.ToComponentDataArray<EcsTestManagedDataEntityCollection>();
            for (int i = 0; i != testDataArray.Length; i++)
            {
                var testData = testDataArray[i];
                for (int j = 0; j < testDataArray[i].value1.Count; ++j)
                {
                    var component = m_Manager.GetComponentData<EcsTestManagedDataEntityCollection>(testData.value1[j]);
                    Assert.AreEqual(testData.value0[j], component.value0[j]);
                }
            }

            targetEntities.Dispose();
            sourceEntities.Dispose();
            sourceWorld.Dispose();
        }

#endif
#endif
    }
}

using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Jobs;
using System.Collections.Generic;
using Unity.Burst;

namespace Unity.Entities.Tests
{
    class EntityCommandBufferTests : ECSTestsFixture
    {
        World m_World2;
        EntityManager m_Manager2;
        EntityManager.EntityManagerDebug m_ManagerDebug2;

        [SetUp]
        public override void Setup()
        {
            base.Setup();

            m_World2 = new World("Test World 2");
            m_Manager2 = m_World2.EntityManager;
            m_ManagerDebug2 = new EntityManager.EntityManagerDebug(m_Manager2);
        }

        [TearDown]
        public override void TearDown()
        {
            if (m_World2.IsCreated)
            {
                // Clean up systems before calling CheckInternalConsistency because we might have filters etc
                // holding on SharedComponentData making checks fail
                while (m_World2.Systems.Count > 0)
                {
                    m_World2.DestroySystem(m_World2.Systems[0]);
                }

                m_ManagerDebug2.CheckInternalConsistency();

                m_World2.Dispose();
                m_World2 = null;
            }

            base.TearDown();
        }

        [Test]
        public void EmptyOK()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            cmds.Playback(m_Manager);
            cmds.Dispose();
        }

        [Test]
        public void Playback_WithSinglePlaybackPolicy_ThrowsOnMultiplePlaybacks()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.SinglePlayback);
            // First playback should succeed
            Assert.DoesNotThrow(() => {cmds.Playback(m_Manager); });
            // Subsequent playback attempts fail
            Assert.Throws<InvalidOperationException>(() => {cmds.Playback(m_Manager); });
            // Playback on a second EntityManager fails
            Assert.Throws<InvalidOperationException>(() => {cmds.Playback(m_Manager2); });
            cmds.Dispose();
        }

        [Test]
        public void Playback_WithMultiPlaybackPolicy_DoesNotThrow()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.MultiPlayback);
            // First playback should succeed
            Assert.DoesNotThrow(() => {cmds.Playback(m_Manager); });
            // Subsequent playback attempts should not fail
            Assert.DoesNotThrow(() => {cmds.Playback(m_Manager); });
            // Playback on a second EntityManager also does not fail
            Assert.DoesNotThrow(() => {cmds.Playback(m_Manager2); });
            cmds.Dispose();
        }

        [BurstCompile(CompileSynchronously = true)]
        struct TestJob : IJob
        {
            public EntityCommandBuffer Buffer;

            public void Execute()
            {
                var e = Buffer.CreateEntity();
                Buffer.AddComponent(e, new EcsTestData { value = 1 });
            }
        }

        class TestEntityCommandBufferSystem : EntityCommandBufferSystem {}

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [Test]
        [DotsRuntimeFixme]
        public void EntityCommandBufferSystem_DisposeAfterPlaybackError_Succeeds()
        {
            TestEntityCommandBufferSystem barrier = World.GetOrCreateSystem<TestEntityCommandBufferSystem>();
            EntityCommandBuffer cmds = barrier.CreateCommandBuffer();

            // Schedule a job that writes concurrently to the ECB
            const int kCreateCount = 256;
            var job = new TestParallelJob
            {
                CommandBuffer = cmds.ToConcurrent(),
            }.Schedule(kCreateCount, 64);
            // Intentionally omit this call, to trigger a safety manager exception during playback.
            //barrier.AddJobHandleForProducer(job)

            // This should throw an error; the job is still writing to the buffer we're playing back.
            Assert.Throws<ArgumentException>(() => { barrier.FlushPendingBuffers(true); }); // playback & dispose ECBs

            // ...but the ECB should have been successfully disposed.
            Assert.AreEqual(1, barrier.PendingBuffers.Count);
            Assert.IsFalse(barrier.PendingBuffers[0].IsCreated);

            job.Complete();
        }

#if UNITY_2020_1_OR_NEWER
        [Test]
        [DotsRuntimeFixme]
        public void EntityCommandBufferConcurrent_PlaybackDuringWrite_UsesCustomOwnerTypeName()
        {
            EntityCommandBuffer cmds = new EntityCommandBuffer(Allocator.TempJob);
            const int kCreateCount = 10000;
            var job = new TestParallelJob
            {
                CommandBuffer = cmds.ToConcurrent(),
            }.Schedule(kCreateCount, 64);
            Assert.That(() => cmds.Playback(m_Manager), Throws.InvalidOperationException.With.Message.Contains("EntityCommandBuffer"));
            job.Complete();
            cmds.Dispose();
        }

#endif

#endif

        [Test]
        [DotsRuntimeFixme]
        public void SingleWriterEnforced()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.MultiPlayback);
            var job = new TestJob {Buffer = cmds};

            var e = cmds.CreateEntity();
            cmds.AddComponent(e, new EcsTestData { value = 42 });

            var handle = job.Schedule();

            Assert.Throws<InvalidOperationException>(() => { cmds.CreateEntity(); });
            Assert.Throws<InvalidOperationException>(() => { job.Buffer.CreateEntity(); });

            handle.Complete();

            cmds.Playback(m_Manager);
            cmds.Playback(m_Manager2);
            cmds.Dispose();

            var query = m_Manager.CreateEntityQuery(typeof(EcsTestData));
            var arr = query.ToComponentDataArray<EcsTestData>(Allocator.TempJob);
            Assert.AreEqual(2, arr.Length);
            Assert.AreEqual(42, arr[0].value);
            Assert.AreEqual(1, arr[1].value);
            arr.Dispose();
            query.Dispose();

            var query2 = m_Manager2.CreateEntityQuery(typeof(EcsTestData));
            var arr2 = query2.ToComponentDataArray<EcsTestData>(Allocator.TempJob);
            Assert.AreEqual(2, arr2.Length);
            Assert.AreEqual(42, arr2[0].value);
            Assert.AreEqual(1, arr2[1].value);
            arr2.Dispose();
            query2.Dispose();
        }

        [Test]
        [DotsRuntimeFixme]
        public void DisposeWhileJobRunningThrows()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var job = new TestJob {Buffer = cmds};

            var handle = job.Schedule();

            Assert.Throws<InvalidOperationException>(() => { cmds.Dispose(); });

            handle.Complete();

            cmds.Dispose();
        }

        [Test]
        [DotsRuntimeFixme]
        public void ModifiesWhileJobRunningThrows()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var job = new TestJob {Buffer = cmds};

            var handle = job.Schedule();

            Assert.Throws<InvalidOperationException>(() => { cmds.CreateEntity(); });

            handle.Complete();

            cmds.Dispose();
        }

        [Test]
        [DotsRuntimeFixme] // IJob
        public void PlaybackWhileJobRunningThrows()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var job = new TestJob {Buffer = cmds};

            var handle = job.Schedule();

            Assert.Throws<InvalidOperationException>(() => { cmds.Playback(m_Manager); });

            handle.Complete();

            cmds.Dispose();
        }

        struct EntityCommandBufferPlaybackJob : IJob
        {
            public EntityCommandBuffer Buffer;
            public ExclusiveEntityTransaction Manager;

            public void Execute()
            {
                Buffer.Playback(Manager);
            }
        }

        [Test]
        public void PlaybackWithExclusiveEntityTransactionInJob()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var job = new TestJob {Buffer = cmds};

            var e = cmds.CreateEntity();
            cmds.AddComponent(e, new EcsTestData { value = 42 });

            e = cmds.CreateEntity();
            cmds.AddSharedComponent(e, new EcsTestSharedComp(19));

            var jobHandle = job.Schedule();

            var manager = m_Manager.BeginExclusiveEntityTransaction();

            var playbackJob = new EntityCommandBufferPlaybackJob()
            {
                Buffer = cmds,
                Manager = manager
            };

            m_Manager.ExclusiveEntityTransactionDependency = playbackJob.Schedule(jobHandle);
            m_Manager.EndExclusiveEntityTransaction();

            cmds.Dispose();

            var group = m_Manager.CreateEntityQuery(typeof(EcsTestData));
            var arr = group.ToComponentDataArray<EcsTestData>(Allocator.TempJob);
            Assert.AreEqual(2, arr.Length);
            Assert.AreEqual(42, arr[0].value);
            Assert.AreEqual(1, arr[1].value);
            arr.Dispose();
            group.Dispose();

            var sharedGroup = m_Manager.CreateEntityQuery(typeof(EcsTestSharedComp));
            var entities = sharedGroup.ToEntityArray(Allocator.TempJob);
            Assert.AreEqual(1, entities.Length);
            Assert.AreEqual(19, m_Manager.GetSharedComponentData<EcsTestSharedComp>(entities[0]).value);
            entities.Dispose();
            sharedGroup.Dispose();
        }

        [BurstCompile(CompileSynchronously = true)]
        struct TestParallelJob : IJobParallelFor
        {
            public EntityCommandBuffer.Concurrent CommandBuffer;

            public void Execute(int index)
            {
                var e = CommandBuffer.CreateEntity(index);
                CommandBuffer.AddComponent(index, e, new EcsTestData {value = index});
            }
        }

        [Test]
        [DotsRuntimeFixme]
        public void EntityCommandBufferConcurrent_PlaybackDuringWrite_ThrowsInvalidOperation()
        {
            EntityCommandBuffer cmds = new EntityCommandBuffer(Allocator.TempJob);
            const int kCreateCount = 10000;
            var job = new TestParallelJob
            {
                CommandBuffer = cmds.ToConcurrent(),
            }.Schedule(kCreateCount, 64);
            Assert.Throws<InvalidOperationException>(() => { cmds.Playback(m_Manager); });
            job.Complete();
            cmds.Dispose();
        }

        [Test]
        [DotsRuntimeFixme]
        public void EntityCommandBufferConcurrent_DisposeDuringWrite_ThrowsInvalidOperation()
        {
            EntityCommandBuffer cmds = new EntityCommandBuffer(Allocator.TempJob);
            const int kCreateCount = 10000;
            var job = new TestParallelJob
            {
                CommandBuffer = cmds.ToConcurrent(),
            }.Schedule(kCreateCount, 64);
            Assert.Throws<InvalidOperationException>(() => { cmds.Dispose(); });
            job.Complete();
            cmds.Dispose();
        }

        [Test]
        public void CreateEntity()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.MultiPlayback);
            var e = cmds.CreateEntity();
            cmds.AddComponent(e, new EcsTestData { value = 12 });
            cmds.Playback(m_Manager);
            cmds.Playback(m_Manager2);
            cmds.Dispose();
            {
                var group = m_Manager.CreateEntityQuery(typeof(EcsTestData));
                var arr = group.ToComponentDataArray<EcsTestData>(Allocator.TempJob);
                Assert.AreEqual(1, arr.Length);
                Assert.AreEqual(12, arr[0].value);
                arr.Dispose();
                group.Dispose();
            }
            {
                var group = m_Manager2.CreateEntityQuery(typeof(EcsTestData));
                var arr = group.ToComponentDataArray<EcsTestData>(Allocator.TempJob);
                Assert.AreEqual(1, arr.Length);
                Assert.AreEqual(12, arr[0].value);
                arr.Dispose();
                group.Dispose();
            }
        }

        [Test]
        public void CreateEntityWithArchetype()
        {
            var a = m_Manager.CreateArchetype(typeof(EcsTestData));

            var cmds = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.MultiPlayback);
            var e = cmds.CreateEntity(a);
            cmds.SetComponent(e, new EcsTestData { value = 12 });
            cmds.Playback(m_Manager);
            cmds.Playback(m_Manager);
            cmds.Dispose();

            {
                var group = m_Manager.CreateEntityQuery(typeof(EcsTestData));
                var arr = group.ToComponentDataArray<EcsTestData>(Allocator.TempJob);
                Assert.AreEqual(2, arr.Length);
                Assert.AreEqual(12, arr[0].value);
                Assert.AreEqual(12, arr[1].value);
                arr.Dispose();
                group.Dispose();
            }
        }

        [Test]
        public void CreateTwoComponents()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.MultiPlayback);
            var e = cmds.CreateEntity();
            cmds.AddComponent(e, new EcsTestData { value = 12 });
            cmds.AddComponent(e, new EcsTestData2 { value0 = 1, value1 = 2 });
            cmds.Playback(m_Manager);
            cmds.Playback(m_Manager2);
            cmds.Dispose();

            {
                var group = m_Manager.CreateEntityQuery(typeof(EcsTestData));
                var arr = group.ToComponentDataArray<EcsTestData>(Allocator.TempJob);
                Assert.AreEqual(1, arr.Length);
                Assert.AreEqual(12, arr[0].value);
                arr.Dispose();
                group.Dispose();
            }

            {
                var group = m_Manager.CreateEntityQuery(typeof(EcsTestData2));
                var arr = group.ToComponentDataArray<EcsTestData2>(Allocator.TempJob);
                Assert.AreEqual(1, arr.Length);
                Assert.AreEqual(1, arr[0].value0);
                Assert.AreEqual(2, arr[0].value1);
                arr.Dispose();
                group.Dispose();
            }

            {
                var group = m_Manager2.CreateEntityQuery(typeof(EcsTestData));
                var arr = group.ToComponentDataArray<EcsTestData>(Allocator.TempJob);
                Assert.AreEqual(1, arr.Length);
                Assert.AreEqual(12, arr[0].value);
                arr.Dispose();
                group.Dispose();
            }

            {
                var group = m_Manager2.CreateEntityQuery(typeof(EcsTestData2));
                var arr = group.ToComponentDataArray<EcsTestData2>(Allocator.TempJob);
                Assert.AreEqual(1, arr.Length);
                Assert.AreEqual(1, arr[0].value0);
                Assert.AreEqual(2, arr[0].value1);
                arr.Dispose();
                group.Dispose();
            }
        }

        [Test]
        public void TestMultiChunks()
        {
            // Slow in IL2CPP: https://unity3d.atlassian.net/browse/DOTSR-1434
#if UNITY_DOTSPLAYER_IL2CPP
            const int count = 4096;
#else
            const int count = 65536;
#endif

            var cmds = new EntityCommandBuffer(Allocator.Temp, PlaybackPolicy.MultiPlayback);
            cmds.MinimumChunkSize = 512;


            for (int i = 0; i < count; i++)
            {
                var e = cmds.CreateEntity();
                cmds.AddComponent(e, new EcsTestData { value = i });
                cmds.AddComponent(e, new EcsTestData2 { value0 = i, value1 = i });
            }

            cmds.Playback(m_Manager);
            cmds.Playback(m_Manager2);
            cmds.Dispose();

            {
                var group = m_Manager.CreateEntityQuery(typeof(EcsTestData), typeof(EcsTestData2));
                var arr = group.ToComponentDataArray<EcsTestData>(Allocator.TempJob);
                var arr2 = group.ToComponentDataArray<EcsTestData2>(Allocator.TempJob);
                Assert.AreEqual(count, arr.Length);
                for (int i = 0; i < count; ++i)
                {
                    Assert.AreEqual(i, arr[i].value);
                    Assert.AreEqual(i, arr2[i].value0);
                    Assert.AreEqual(i, arr2[i].value1);
                }
                arr.Dispose();
                arr2.Dispose();
                group.Dispose();
            }
            {
                var group = m_Manager2.CreateEntityQuery(typeof(EcsTestData), typeof(EcsTestData2));
                var arr = group.ToComponentDataArray<EcsTestData>(Allocator.TempJob);
                var arr2 = group.ToComponentDataArray<EcsTestData2>(Allocator.TempJob);
                Assert.AreEqual(count, arr.Length);
                for (int i = 0; i < count; ++i)
                {
                    Assert.AreEqual(i, arr[i].value);
                    Assert.AreEqual(i, arr2[i].value0);
                    Assert.AreEqual(i, arr2[i].value1);
                }
                arr.Dispose();
                arr2.Dispose();
                group.Dispose();
            }
        }

        [Test]
        public void AddSharedComponent()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);

            var entity = m_Manager.CreateEntity();
            cmds.AddSharedComponent(entity, new EcsTestSharedComp(10));
            cmds.AddSharedComponent(entity, new EcsTestSharedComp2(20));
            cmds.AddSharedComponent(entity, new EcsTestSharedComp3(0));

            cmds.Playback(m_Manager);

            Assert.AreEqual(10, m_Manager.GetSharedComponentData<EcsTestSharedComp>(entity).value);
            Assert.AreEqual(20, m_Manager.GetSharedComponentData<EcsTestSharedComp2>(entity).value1);
            Assert.AreEqual(0, m_Manager.GetSharedComponentData<EcsTestSharedComp3>(entity).value0);
            Assert.AreEqual(0, m_Manager.GetSharedComponentDataIndex<EcsTestSharedComp3>(entity));

            cmds.Dispose();
        }

        [Test]
        public void AddSharedComponentDefault()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.MultiPlayback);

            var e = cmds.CreateEntity();
            cmds.AddSharedComponent(e, new EcsTestSharedComp(10));
            cmds.AddSharedComponent(e, new EcsTestSharedComp2(20));

            cmds.Playback(m_Manager);
            cmds.Playback(m_Manager2);

            {
                var sharedComp1List = new List<EcsTestSharedComp>();
                var sharedComp2List = new List<EcsTestSharedComp2>();

                m_Manager.GetAllUniqueSharedComponentData(sharedComp1List);
                m_Manager.GetAllUniqueSharedComponentData(sharedComp2List);

                // the count must be 2 - the default value of the shared component and the one we actually set
                Assert.AreEqual(2, sharedComp1List.Count);
                Assert.AreEqual(2, sharedComp2List.Count);

                Assert.AreEqual(10, sharedComp1List[1].value);
                Assert.AreEqual(20, sharedComp2List[1].value1);
            }
            {
                var sharedComp1List = new List<EcsTestSharedComp>();
                var sharedComp2List = new List<EcsTestSharedComp2>();

                m_Manager2.GetAllUniqueSharedComponentData(sharedComp1List);
                m_Manager2.GetAllUniqueSharedComponentData(sharedComp2List);

                // the count must be 2 - the default value of the shared component and the one we actually set
                Assert.AreEqual(2, sharedComp1List.Count);
                Assert.AreEqual(2, sharedComp2List.Count);

                Assert.AreEqual(10, sharedComp1List[1].value);
                Assert.AreEqual(20, sharedComp2List[1].value1);
            }

            cmds.Dispose();
        }

        [Test]
        public void AddComponent()
        {
            var e = m_Manager.CreateEntity();

            var cmds = new EntityCommandBuffer(Allocator.TempJob);

            cmds.AddComponent(e, new EcsTestData(10));
            cmds.AddComponent<EcsTestTag>(e);
            cmds.AddComponent(e, ComponentType.ReadWrite<EcsTestData3>());

            cmds.Playback(m_Manager);

            cmds.Dispose();

            Assert.AreEqual(10, m_Manager.GetComponentData<EcsTestData>(e).value);
            Assert.IsTrue(m_Manager.HasComponent<EcsTestTag>(e));
            Assert.IsTrue(m_Manager.HasComponent<EcsTestData3>(e));
        }

        [Test]
        public void SetSharedComponent()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);

            var e = cmds.CreateEntity();
            cmds.AddSharedComponent(e, new EcsTestSharedComp(10));
            cmds.SetSharedComponent(e, new EcsTestSharedComp(33));

            cmds.Playback(m_Manager);

            var sharedCompList = new List<EcsTestSharedComp>();
            m_Manager.GetAllUniqueSharedComponentData<EcsTestSharedComp>(sharedCompList);

            Assert.AreEqual(2, sharedCompList.Count);
            Assert.AreEqual(33, sharedCompList[1].value);

            cmds.Dispose();
        }

        [Test]
        public void SetSharedComponentDefault()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.MultiPlayback);

            var e = cmds.CreateEntity();
            cmds.AddSharedComponent(e, new EcsTestSharedComp(10));
            cmds.SetSharedComponent(e, new EcsTestSharedComp());

            cmds.Playback(m_Manager);
            cmds.Playback(m_Manager2);

            {
                var sharedCompList = new List<EcsTestSharedComp>();
                m_Manager.GetAllUniqueSharedComponentData<EcsTestSharedComp>(sharedCompList);

                Assert.AreEqual(1, sharedCompList.Count);
                Assert.AreEqual(0, sharedCompList[0].value);
            }
            {
                var sharedCompList = new List<EcsTestSharedComp>();
                m_Manager2.GetAllUniqueSharedComponentData<EcsTestSharedComp>(sharedCompList);

                Assert.AreEqual(1, sharedCompList.Count);
                Assert.AreEqual(0, sharedCompList[0].value);
            }

            cmds.Dispose();
        }

        [Test]
        public void RemoveSharedComponent()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);

            var entity = m_Manager.CreateEntity();
            var sharedComponent = new EcsTestSharedComp(10);
            m_Manager.AddSharedComponentData(entity, sharedComponent);

            cmds.RemoveComponent<EcsTestSharedComp>(entity);

            cmds.Playback(m_Manager);

            Assert.IsFalse(m_Manager.HasComponent<EcsTestSharedComp>(entity), "The shared component was not removed.");

            cmds.Dispose();
        }

        [Test]
        public void AddAndSetComponent_ComponentDoesNotExist_Succeeds()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);

            var entity = m_Manager.CreateEntity();
            cmds.AddComponent(entity, ComponentType.ReadWrite<EcsTestData>());
            cmds.SetComponent(entity, new EcsTestData(42));

            Assert.DoesNotThrow(() => { cmds.Playback(m_Manager); });

            Assert.IsTrue(m_Manager.HasComponent<EcsTestData>(entity));
            Assert.AreEqual(42, m_Manager.GetComponentData<EcsTestData>(entity).value);

            cmds.Dispose();
        }

        [Test]
        public void AddAndSetComponent_ComponentExists_NewValueWins()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);

            var entity = m_Manager.CreateEntity();
            cmds.AddComponent(entity, new EcsTestData(17));

            cmds.AddComponent(entity, ComponentType.ReadWrite<EcsTestData>());
            cmds.SetComponent(entity, new EcsTestData(42));

            Assert.DoesNotThrow(() => { cmds.Playback(m_Manager); });

            Assert.IsTrue(m_Manager.HasComponent<EcsTestData>(entity));
            Assert.AreEqual(42, m_Manager.GetComponentData<EcsTestData>(entity).value);

            cmds.Dispose();
        }

        [Test]
        public void AddComponentData_ComponentExists_NewValueWins()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);

            var entity = m_Manager.CreateEntity();
            cmds.AddComponent(entity, new EcsTestData(17));
            cmds.AddComponent(entity, new EcsTestData(42));

            Assert.DoesNotThrow(() => { cmds.Playback(m_Manager); });

            Assert.AreEqual(42, m_Manager.GetComponentData<EcsTestData>(entity).value);

            cmds.Dispose();
        }

        [Test]
        public void AddSharedComponent_ComponentDoesNotExist_Succeeds()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);

            var entity = m_Manager.CreateEntity();
            cmds.AddSharedComponent(entity, new EcsTestSharedComp(42));

            Assert.DoesNotThrow(() => { cmds.Playback(m_Manager); });

            Assert.AreEqual(42, m_Manager.GetSharedComponentData<EcsTestSharedComp>(entity).value);

            cmds.Dispose();
        }

        [Test]
        public void AddSharedComponent_ComponentExists_NewValueWins()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);

            var entity = m_Manager.CreateEntity();
            cmds.AddSharedComponent(entity, new EcsTestSharedComp(17));
            cmds.AddSharedComponent(entity, new EcsTestSharedComp(42));

            Assert.DoesNotThrow(() => { cmds.Playback(m_Manager); });

            Assert.AreEqual(42, m_Manager.GetSharedComponentData<EcsTestSharedComp>(entity).value);

            cmds.Dispose();
        }

        [Test]
        public void AddComponentToEntityQuery()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);

            var entity = cmds.CreateEntity();
            var data1 = new EcsTestData();
            cmds.AddComponent(entity, data1);

            var entityQuery = m_Manager.CreateEntityQuery(typeof(EcsTestData));
            cmds.AddComponent(entityQuery, typeof(EcsTestData2));

            cmds.Playback(m_Manager);

            var entities = m_Manager.GetAllEntities(Allocator.TempJob);

            Assert.AreEqual(1, entities.Length);
            Assert.IsTrue(m_Manager.HasComponent<EcsTestData2>(entities[0]), "The component was not added to the entities within the entity query.");

            cmds.Dispose();
            entityQuery.Dispose();
            m_Manager.DestroyEntity(entities);
            entities.Dispose();
        }

        [Test]
        public void AddComponentToEntityQueryWithFilter()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestSharedComp));

            var entity1 = cmds.CreateEntity(archetype);
            var sharedComponent1 = new EcsTestSharedComp {value = 10};
            cmds.SetSharedComponent(entity1, sharedComponent1);

            var entity2 = cmds.CreateEntity(archetype);
            var sharedComponent2 = new EcsTestSharedComp {value = 130};
            cmds.SetSharedComponent(entity2, sharedComponent2);

            var entityQuery = m_Manager.CreateEntityQuery(typeof(EcsTestSharedComp));
            entityQuery.SetSharedComponentFilter(sharedComponent2);

            cmds.AddComponent(entityQuery, typeof(EcsTestData2));

            cmds.Playback(m_Manager);

            var entities = m_Manager.GetAllEntities(Allocator.TempJob);

            Assert.AreEqual(2, entities.Length);
            Assert.IsTrue(m_Manager.HasComponent<EcsTestData2>(entities[1]), "The component was not added to the entities within the entity query.");
            Assert.IsFalse(m_Manager.HasComponent<EcsTestData2>(entities[0]), "The component was incorrectly added based on the EntityQueryFilter.");

            cmds.Dispose();
            entityQuery.Dispose();
            m_Manager.DestroyEntity(entities);
            entities.Dispose();
        }

        [Test]
        public void AddComponentToEntityQuery_OnDifferentEntityManager_Throws()
        {
            using (var cmds = new EntityCommandBuffer(Allocator.TempJob))
            {
                var entity = cmds.CreateEntity();
                var data1 = new EcsTestData();
                cmds.AddComponent(entity, data1);

                // create a query in one manager ...
                var entityQuery = m_Manager.CreateEntityQuery(typeof(EcsTestData));
                cmds.AddComponent(entityQuery, typeof(EcsTestData2));

                // ... and playback on a different manager
                Assert.Throws<ArgumentException>(() => cmds.Playback(m_Manager2));

                entityQuery.Dispose();
            }
        }

        [Test]
        public void RemoveComponentFromEntityQuery()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestSharedComp), typeof(EcsTestData));

            var entity = cmds.CreateEntity(archetype);
            var data1 = new EcsTestData();
            cmds.SetComponent(entity, data1);

            var entityQuery = m_Manager.CreateEntityQuery(typeof(EcsTestData));
            cmds.RemoveComponent(entityQuery, typeof(EcsTestData));

            cmds.Playback(m_Manager);

            var entities = m_Manager.GetAllEntities(Allocator.TempJob);

            Assert.AreEqual(1, entities.Length);
            Assert.IsFalse(m_Manager.HasComponent<EcsTestData>(entities[0]), "The component was not removed from the entities in the entity query.");

            cmds.Dispose();
            entityQuery.Dispose();
            m_Manager.DestroyEntity(entities);
            entities.Dispose();
        }

        [Test]
        public void RemoveComponentFromEntityQueryWithFilter()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestSharedComp), typeof(EcsTestData));

            var entity1 = cmds.CreateEntity(archetype);
            var sharedComponent1 = new EcsTestSharedComp {value = 10};
            cmds.SetSharedComponent(entity1, sharedComponent1);

            var entity2 = cmds.CreateEntity(archetype);
            var sharedComponent2 = new EcsTestSharedComp {value = 130};
            cmds.SetSharedComponent(entity2, sharedComponent2);

            var entityQuery = m_Manager.CreateEntityQuery(typeof(EcsTestSharedComp), typeof(EcsTestData));
            entityQuery.SetSharedComponentFilter(sharedComponent2);

            cmds.RemoveComponent(entityQuery, typeof(EcsTestData));

            cmds.Playback(m_Manager);

            var entities = m_Manager.GetAllEntities(Allocator.TempJob);

            Assert.AreEqual(2, entities.Length);

            Assert.IsTrue(m_Manager.HasComponent<EcsTestData>(entities[0]), "The component was incorrectly removed based on the EntityQueryFilter.");
            Assert.IsFalse(m_Manager.HasComponent<EcsTestData>(entities[1]), "The component was not removed from the entities in the entity query.");

            cmds.Dispose();
            entityQuery.Dispose();
            m_Manager.DestroyEntity(entities);
            entities.Dispose();
        }

        [Test]
        public void AddSharedComponentDataToEntityQuery()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData));

            for (int i = 0; i < 50; i++)
            {
                var entity = cmds.CreateEntity(archetype);
                var data1 = new EcsTestData();

                cmds.SetComponent(entity, data1);
            }

            var entityQuery = m_Manager.CreateEntityQuery(typeof(EcsTestData));
            var sharedComponent1 = new EcsTestSharedComp {value = 10};
            cmds.AddSharedComponent(entityQuery, sharedComponent1);

            cmds.Playback(m_Manager);

            var entities = m_Manager.GetAllEntities(Allocator.TempJob);

            Assert.IsTrue(m_Manager.HasComponent<EcsTestSharedComp>(entities[0]), "The shared component was not correctly added based on the EntityQuery.");
            Entity e = entities[0];
            var sharedValue = m_Manager.GetSharedComponentData<EcsTestSharedComp>(e).value;
            Assert.AreEqual(10, sharedValue);

            cmds.Dispose();
            entityQuery.Dispose();
            m_Manager.DestroyEntity(entities);
            entities.Dispose();
        }

        [Test]
        public void AddSharedComponentDataToEntityQueryWithFilter()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestSharedComp), typeof(EcsTestData));

            for (int i = 0; i < 50; i++)
            {
                var entity = cmds.CreateEntity(archetype);

                cmds.SetComponent(entity, new EcsTestData());
                cmds.SetSharedComponent(entity, new EcsTestSharedComp(i % 2));
            }

            var entityQuery = m_Manager.CreateEntityQuery(typeof(EcsTestSharedComp), typeof(EcsTestData));
            entityQuery.SetSharedComponentFilter(new EcsTestSharedComp(0));
            var shared2 = new EcsTestSharedComp2();
            cmds.AddSharedComponent(entityQuery, shared2);

            cmds.Playback(m_Manager);

            var entityQuery2 = m_Manager.CreateEntityQuery(typeof(EcsTestSharedComp), typeof(EcsTestSharedComp2), typeof(EcsTestData));
            var entities = entityQuery2.ToEntityArray(Allocator.TempJob);

            Assert.AreEqual(25, entities.Length, "The shared component was not correctly added based on the EntityQueryFilter.");
            for (int i = 0; i < entities.Length; i++)
            {
                var value = m_Manager.GetSharedComponentData<EcsTestSharedComp>(entities[i]).value;
                Assert.AreEqual(0, value, "The shared component was not correctly added based on the EntityQueryFilter.");
            }

            cmds.Dispose();
            entityQuery.Dispose();
            m_Manager.DestroyEntity(entities);
            entities.Dispose();
        }

        [Test]
        public void DestroyEntitiesFromEntityQuery()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestData));

            for (int i = 0; i < 50; i++)
            {
                var entity = cmds.CreateEntity(archetype);

                cmds.SetComponent(entity, new EcsTestData());
            }

            var entity2 = cmds.CreateEntity();
            cmds.AddComponent(entity2, new EcsTestData2());


            var entityQuery = m_Manager.CreateEntityQuery(typeof(EcsTestData));
            cmds.DestroyEntity(entityQuery);

            cmds.Playback(m_Manager);

            var entities = m_Manager.GetAllEntities(Allocator.TempJob);

            Assert.AreEqual(1, entities.Length, "Entities were not all deleted based on the EntityQuery.");
            Assert.IsTrue(m_Manager.HasComponent(entities[0], typeof(EcsTestData2)), "This entity should not have been deleted based on the EntityQuery.");

            cmds.Dispose();
            entityQuery.Dispose();
            m_Manager.DestroyEntity(entities);
            entities.Dispose();
        }

        [Test]
        public void DestroyEntitiesFromEntityQueryWithFilter()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestSharedComp), typeof(EcsTestData));

            for (int i = 0; i < 50; i++)
            {
                var entity = cmds.CreateEntity(archetype);

                cmds.SetComponent(entity, new EcsTestData());
                cmds.SetSharedComponent(entity, new EcsTestSharedComp(i % 2));
            }

            var entityQuery = m_Manager.CreateEntityQuery(typeof(EcsTestSharedComp), typeof(EcsTestData));
            entityQuery.SetSharedComponentFilter(new EcsTestSharedComp(0));
            cmds.DestroyEntity(entityQuery);

            cmds.Playback(m_Manager);

            var entities = m_Manager.GetAllEntities(Allocator.TempJob);

            Assert.AreEqual(25, entities.Length, "Half of the entities should be deleted based on the filter of the EntityQuery.");
            for (int i = 0; i < entities.Length; i++)
            {
                var value = m_Manager.GetSharedComponentData<EcsTestSharedComp>(entities[i]).value;
                Assert.AreEqual(1, value, "Entity should have been deleted based on the EntityQueryFilter.");
            }

            cmds.Dispose();
            entityQuery.Dispose();
            m_Manager.DestroyEntity(entities);
            entities.Dispose();
        }

        [Test]
        public void ChangeEntityQueryFilterDoesNotImpactRecordedCommand()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestSharedComp));

            var entity1 = cmds.CreateEntity(archetype);
            var sharedComponent1 = new EcsTestSharedComp {value = 10};
            cmds.SetSharedComponent(entity1, sharedComponent1);

            var entity2 = cmds.CreateEntity(archetype);
            var sharedComponent2 = new EcsTestSharedComp {value = 130};
            cmds.SetSharedComponent(entity2, sharedComponent2);

            var entityQuery = m_Manager.CreateEntityQuery(typeof(EcsTestSharedComp));
            entityQuery.SetSharedComponentFilter(sharedComponent2);

            cmds.AddComponent(entityQuery, typeof(EcsTestData2));

            entityQuery.SetSharedComponentFilter(sharedComponent1);

            cmds.Playback(m_Manager);

            var entities = m_Manager.GetAllEntities(Allocator.TempJob);

            Assert.AreEqual(2, entities.Length);
            Assert.IsTrue(m_Manager.HasComponent<EcsTestData2>(entities[1]), "The EntityQueryFilter should have been recorded to add this component before it was changed.");
            Assert.IsFalse(m_Manager.HasComponent<EcsTestData2>(entities[0]), "Changing the EntityQueryFilter after recording should not impact the command at playback.");

            cmds.Dispose();
            entityQuery.Dispose();
            m_Manager.DestroyEntity(entities);
            entities.Dispose();
        }

        [Test]
        public void DeleteEntityQueryDoesNotImpactRecordedCommand()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestSharedComp));

            var entity1 = cmds.CreateEntity(archetype);
            var sharedComponent1 = new EcsTestSharedComp {value = 10};
            cmds.SetSharedComponent(entity1, sharedComponent1);

            var entity2 = cmds.CreateEntity(archetype);
            var sharedComponent2 = new EcsTestSharedComp {value = 130};
            cmds.SetSharedComponent(entity2, sharedComponent2);

            var entityQuery = m_Manager.CreateEntityQuery(typeof(EcsTestSharedComp));
            entityQuery.SetSharedComponentFilter(sharedComponent2);

            cmds.AddComponent(entityQuery, typeof(EcsTestData2));

            entityQuery.Dispose();

            cmds.Playback(m_Manager);

            var entities = m_Manager.GetAllEntities(Allocator.TempJob);

            Assert.AreEqual(2, entities.Length);
            Assert.IsTrue(m_Manager.HasComponent<EcsTestData2>(entities[1]), "The EntityQuery should be recorded and stored for playback regardless of disposal.");

            cmds.Dispose();
            m_Manager.DestroyEntity(entities);
            entities.Dispose();
        }

        // Burst still doesn't work with managed shared components
        struct TestJobWithManagedSharedData : IJob
        {
            public EntityCommandBuffer Buffer;
            public EcsTestSharedComp2 Blah;

            public void Execute()
            {
                var e = Buffer.CreateEntity();
                Buffer.AddSharedComponent(e, Blah);
            }
        }

        [Test]
        public void JobWithSharedComponentData()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var job = new TestJobWithManagedSharedData { Buffer = cmds, Blah = new EcsTestSharedComp2(12) };

            job.Schedule().Complete();
            cmds.Playback(m_Manager);
            cmds.Dispose();

            var list = new List<EcsTestSharedComp2>();
            m_Manager.GetAllUniqueSharedComponentData<EcsTestSharedComp2>(list);

            Assert.AreEqual(2, list.Count);
            Assert.AreEqual(0, list[0].value0);
            Assert.AreEqual(0, list[0].value1);
            Assert.AreEqual(12, list[1].value0);
            Assert.AreEqual(12, list[1].value1);
        }

        [BurstCompile(CompileSynchronously = true)]
        public struct TestBurstCommandBufferJob : IJob
        {
            public Entity e0;
            public Entity e1;
            public EntityCommandBuffer Buffer;

            public void Execute()
            {
                Buffer.DestroyEntity(e0);
                Buffer.DestroyEntity(e1);
            }
        }

        [Test]
        public void TestCommandBufferDelete()
        {
            Entity[] entities = new Entity[2];
            for (int i = 0; i < entities.Length; ++i)
            {
                entities[i] = m_Manager.CreateEntity();
                m_Manager.AddComponentData(entities[i], new EcsTestData { value = i });
            }

            var cmds = new EntityCommandBuffer(Allocator.TempJob);

            new TestBurstCommandBufferJob
            {
                e0 = entities[0],
                e1 = entities[1],
                Buffer = cmds,
            }.Schedule().Complete();

            cmds.Playback(m_Manager);

            cmds.Dispose();

            var allEntities = m_Manager.GetAllEntities();
            int count = allEntities.Length;
            allEntities.Dispose();

            Assert.AreEqual(0, count);
        }

        [Test]
        public void TestCommandBufferDeleteWithSystemState()
        {
            Entity[] entities = new Entity[2];
            for (int i = 0; i < entities.Length; ++i)
            {
                entities[i] = m_Manager.CreateEntity();
                m_Manager.AddComponentData(entities[i], new EcsTestData { value = i });
                m_Manager.AddComponentData(entities[i], new EcsState1 { Value = i });
            }

            var cmds = new EntityCommandBuffer(Allocator.TempJob);

            new TestBurstCommandBufferJob
            {
                e0 = entities[0],
                e1 = entities[1],
                Buffer = cmds,
            }.Schedule().Complete();

            cmds.Playback(m_Manager);

            cmds.Dispose();

            var allEntities = m_Manager.GetAllEntities();
            int count = allEntities.Length;
            allEntities.Dispose();

            Assert.AreEqual(entities.Length, count);
        }

        [Test]
        public void TestCommandBufferDeleteRemoveSystemState()
        {
            Entity[] entities = new Entity[2];
            for (int i = 0; i < entities.Length; ++i)
            {
                entities[i] = m_Manager.CreateEntity();
                m_Manager.AddComponentData(entities[i], new EcsTestData { value = i });
                m_Manager.AddComponentData(entities[i], new EcsState1 { Value = i });
            }

            {
                var cmds = new EntityCommandBuffer(Allocator.TempJob);
                new TestBurstCommandBufferJob
                {
                    e0 = entities[0],
                    e1 = entities[1],
                    Buffer = cmds,
                }.Schedule().Complete();

                cmds.Playback(m_Manager);
                cmds.Dispose();
            }

            {
                var cmds = new EntityCommandBuffer(Allocator.TempJob);
                for (var i = 0; i < entities.Length; i++)
                {
                    cmds.RemoveComponent<EcsState1>(entities[i]);
                }

                cmds.Playback(m_Manager);
                cmds.Dispose();
            }

            var allEntities = m_Manager.GetAllEntities();
            int count = allEntities.Length;
            allEntities.Dispose();

            Assert.AreEqual(0, count);
        }

        [Test]
        public void Instantiate()
        {
            var e = m_Manager.CreateEntity();
            m_Manager.AddComponentData(e, new EcsTestData(5));

            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            cmds.Instantiate(e);
            cmds.Instantiate(e);
            cmds.Playback(m_Manager);
            cmds.Dispose();

            VerifyEcsTestData(3, 5);
        }

        [Test]
        public void InstantiateWithSetComponentDataWorks()
        {
            var e = m_Manager.CreateEntity();
            m_Manager.AddComponentData(e, new EcsTestData(5));

            var cmds = new EntityCommandBuffer(Allocator.TempJob);

            var e1 = cmds.Instantiate(e);
            cmds.SetComponent(e1, new EcsTestData(11));

            var e2 = cmds.Instantiate(e);
            cmds.SetComponent(e2, new EcsTestData(11));

            cmds.Playback(m_Manager);
            cmds.Dispose();

            m_Manager.DestroyEntity(e);

            VerifyEcsTestData(2, 11);
        }

        [Test]
        public void DestroyEntityTwiceWorks()
        {
            var e = m_Manager.CreateEntity();
            m_Manager.AddComponentData(e, new EcsTestData(5));

            var cmds = new EntityCommandBuffer(Allocator.TempJob);

            cmds.DestroyEntity(e);
            cmds.DestroyEntity(e);

            cmds.Playback(m_Manager);
            cmds.Dispose();

            Assert.IsFalse(m_Manager.Exists(e));
        }

        [Test, Ignore("Leaks string allocs in Burst")]
        public void DestroyInvalidEntity()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var entityBuffer = new NativeArray<Entity>(1, Allocator.Temp);
            var e = cmds.CreateEntity();
            cmds.AddComponent(e, new EcsTestData { value = 12 });
            entityBuffer[0] = e;
            cmds.Playback(m_Manager);
            cmds.Dispose();

            var savedEntity = entityBuffer[0];
            entityBuffer.Dispose();

            var cmds2 = new EntityCommandBuffer(Allocator.TempJob);
            cmds2.DestroyEntity(savedEntity);

            // savedEntity is invalid, so playing back this ECB should throw an exception
            Assert.Throws<InvalidOperationException>(() =>
            {
                cmds2.Playback(m_Manager);
            });

            cmds2.Dispose();
        }

        [Test]
        public void TestShouldPlaybackFalse()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            cmds.CreateEntity();
            cmds.ShouldPlayback = false;
            cmds.Playback(m_Manager);
            cmds.Dispose();

            var allEntities = m_Manager.GetAllEntities();
            int count = allEntities.Length;
            allEntities.Dispose();

            Assert.AreEqual(0, count);
        }

        [BurstCompile(CompileSynchronously = true)]
        struct TestConcurrentJob : IJob
        {
            public EntityCommandBuffer.Concurrent Buffer;

            public void Execute()
            {
                Entity e = Buffer.CreateEntity(0);
                Buffer.AddComponent(0, e, new EcsTestData { value = 1 });
            }
        }

        [Test]
        public void ConcurrentRecord()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            cmds.CreateEntity();
            new TestConcurrentJob { Buffer = cmds.ToConcurrent() }.Schedule().Complete();
            cmds.Playback(m_Manager);
            cmds.Dispose();

            var allEntities = m_Manager.GetAllEntities();
            int count = allEntities.Length;
            allEntities.Dispose();

            Assert.AreEqual(2, count);
        }

        [BurstCompile(CompileSynchronously = true)]
        struct TestConcurrentParallelForJob : IJobParallelFor
        {
            public EntityCommandBuffer.Concurrent Buffer;

            public void Execute(int index)
            {
                Entity e = Buffer.CreateEntity(index);
                Buffer.AddComponent(index, e, new EcsTestData { value = index });
            }
        }

        [Test]
        public void ConcurrentRecordParallelFor()
        {
            const int kCreateCount = 10000;
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            cmds.CreateEntity();
            new TestConcurrentParallelForJob { Buffer = cmds.ToConcurrent() }.Schedule(kCreateCount, 64).Complete();
            cmds.Playback(m_Manager);
            cmds.Dispose();

            var allEntities = m_Manager.GetAllEntities();
            int count = allEntities.Length;
            Assert.AreEqual(kCreateCount + 1, count);
            bool[] foundEntity = new bool[kCreateCount];
            for (int i = 0; i < foundEntity.Length; ++i)
            {
                foundEntity[i] = false;
            }
            for (int i = 0; i < count; ++i)
            {
                if (m_Manager.HasComponent<EcsTestData>(allEntities[i]))
                {
                    var data1 = m_Manager.GetComponentData<EcsTestData>(allEntities[i]);
                    Assert.IsFalse(foundEntity[data1.value]);
                    foundEntity[data1.value] = true;
                }
            }
            for (int i = 0; i < foundEntity.Length; ++i)
            {
                Assert.IsTrue(foundEntity[i]);
            }
            allEntities.Dispose();
        }

        [BurstCompile(CompileSynchronously = true)]
        struct TestConcurrentInstantiateJob : IJobParallelFor
        {
            public Entity MasterCopy;
            public EntityCommandBuffer.Concurrent Buffer;

            public void Execute(int index)
            {
                Entity e = Buffer.Instantiate(index, MasterCopy);
                Buffer.AddComponent(index, e, new EcsTestData { value = index });
            }
        }

        [Test]
        public void ConcurrentRecordInstantiate()
        {
            const int kInstantiateCount = 10000;
            Entity master = m_Manager.CreateEntity();
            m_Manager.AddComponentData(master, new EcsTestData2 {value0 = 42, value1 = 17});

            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            new TestConcurrentInstantiateJob { Buffer = cmds.ToConcurrent(), MasterCopy = master }.Schedule(kInstantiateCount, 64).Complete();
            cmds.Playback(m_Manager);
            cmds.Dispose();

            var allEntities = m_Manager.GetAllEntities();
            int count = allEntities.Length;
            Assert.AreEqual(kInstantiateCount + 1, count); // +1 for the master entity
            bool[] foundEntity = new bool[kInstantiateCount];
            for (int i = 0; i < foundEntity.Length; ++i)
            {
                foundEntity[i] = false;
            }
            for (int i = 0; i < count; ++i)
            {
                var data2 = m_Manager.GetComponentData<EcsTestData2>(allEntities[i]);
                Assert.AreEqual(data2.value0, 42);
                Assert.AreEqual(data2.value1, 17);
                if (m_Manager.HasComponent<EcsTestData>(allEntities[i]))
                {
                    var data1 = m_Manager.GetComponentData<EcsTestData>(allEntities[i]);
                    Assert.IsFalse(foundEntity[data1.value]);
                    foundEntity[data1.value] = true;
                }
            }
            for (int i = 0; i < foundEntity.Length; ++i)
            {
                Assert.IsTrue(foundEntity[i]);
            }
            allEntities.Dispose();
        }

        [Test]
        public void PlaybackInvalidatesBuffers()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var e = cmds.CreateEntity();
            DynamicBuffer<EcsIntElement> buffer = cmds.AddBuffer<EcsIntElement>(e);
            buffer.CopyFrom(new EcsIntElement[] { 1, 2, 3 });
            cmds.Playback(m_Manager);

            // Should not be possible to access the temporary buffer after playback.
            Assert.Throws<InvalidOperationException>(() =>
            {
                buffer.Add(1);
            });
            cmds.Dispose();
        }

        [Test(Description = "Once a buffer command is played back, it has no side effects on the ECB.")]
        public void BufferChanged_BetweenPlaybacks_HasNoEffectOnECB()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.MultiPlayback);
            var e = cmds.CreateEntity();
            DynamicBuffer<EcsIntElement> buffer = cmds.AddBuffer<EcsIntElement>(e);
            buffer.CopyFrom(new EcsIntElement[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            cmds.Playback(m_Manager);

            var entities = m_Manager.GetAllEntities();
            var emBuffer = m_Manager.GetBuffer<EcsIntElement>(entities[0]);
            for (int i = 0; i < emBuffer.Length; ++i)
                emBuffer[i] = 5;
            entities.Dispose();

            cmds.Playback(m_Manager);

            entities = m_Manager.GetAllEntities();
            Assert.AreEqual(2, entities.Length);
            var b = m_Manager.GetBuffer<EcsIntElement>(entities[1]);
            for (int i = 0; i < b.Length; ++i)
                Assert.AreEqual(i + 1, b[i].Value);

            entities.Dispose();
            cmds.Dispose();
        }

        [Test]
        public void ArrayAliasesOfPendingBuffersAreInvalidateOnResize()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var e = cmds.CreateEntity();
            var buffer = cmds.AddBuffer<EcsIntElement>(e);
            buffer.CopyFrom(new EcsIntElement[] { 1, 2, 3 });
            var array = buffer.AsNativeArray();
            buffer.Add(12);
            Assert.Throws<InvalidOperationException>(() =>
            {
                int val = array[0];
            });
            // Refresh array alias
            array = buffer.AsNativeArray();
            cmds.Playback(m_Manager);

            // Should not be possible to access the temporary buffer after playback.
            Assert.Throws<InvalidOperationException>(() =>
            {
                buffer.Add(1);
            });
            // Array should not be accessible after playback
            Assert.Throws<InvalidOperationException>(() =>
            {
                int l = array[0];
            });
            cmds.Dispose();
        }

        [Test]
        public void AddBufferNoOverflow()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var e = cmds.CreateEntity();
            DynamicBuffer<EcsIntElement> buffer = cmds.AddBuffer<EcsIntElement>(e);
            buffer.CopyFrom(new EcsIntElement[] { 1, 2, 3 });
            cmds.Playback(m_Manager);
            VerifySingleBuffer(m_Manager, 3);
            cmds.Dispose();
        }

        [Test]
        public void AddBufferNoOverflow_MultiplePlaybacks()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.MultiPlayback);
            var e = cmds.CreateEntity();
            DynamicBuffer<EcsIntElement> buffer = cmds.AddBuffer<EcsIntElement>(e);
            buffer.CopyFrom(new EcsIntElement[] { 1, 2, 3 });
            cmds.Playback(m_Manager);
            cmds.Playback(m_Manager2);
            VerifySingleBuffer(m_Manager, 3);
            VerifySingleBuffer(m_Manager2, 3);
            cmds.Dispose();
        }

        [Test]
        public void AddBufferOverflow()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var e = cmds.CreateEntity();
            DynamicBuffer<EcsIntElement> buffer = cmds.AddBuffer<EcsIntElement>(e);
            buffer.CopyFrom(new EcsIntElement[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            cmds.Playback(m_Manager);
            VerifySingleBuffer(m_Manager, 10);
            cmds.Dispose();
        }

        [Test]
        public void AddBufferOverflow_MultiplePlaybacks()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.MultiPlayback);
            var e = cmds.CreateEntity();
            DynamicBuffer<EcsIntElement> buffer = cmds.AddBuffer<EcsIntElement>(e);
            buffer.CopyFrom(new EcsIntElement[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            cmds.Playback(m_Manager);
            cmds.Playback(m_Manager2);
            VerifySingleBuffer(m_Manager, 10);
            VerifySingleBuffer(m_Manager2, 10);
            cmds.Dispose();
        }

        [Test]
        public void AddBufferOverflow_MultiplePlaybacks_SingleManager()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.MultiPlayback);
            var e = cmds.CreateEntity();
            DynamicBuffer<EcsIntElement> buffer = cmds.AddBuffer<EcsIntElement>(e);
            buffer.CopyFrom(new EcsIntElement[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 });
            cmds.Playback(m_Manager);
            cmds.Playback(m_Manager);

            var allEntities = m_Manager.GetAllEntities();
            Assert.AreEqual(2, allEntities.Length);
            for (int j = 0; j < allEntities.Length; ++j)
            {
                var resultBuffer = m_Manager.GetBuffer<EcsIntElement>(allEntities[j]);
                Assert.AreEqual(10, resultBuffer.Length);

                for (int i = 0; i < 10; ++i)
                {
                    Assert.AreEqual(i + 1, resultBuffer[i].Value);
                }
            }

            allEntities.Dispose();
            cmds.Dispose();
        }

        [Test]
        public void AddBufferExplicit()
        {
            var e = m_Manager.CreateEntity();
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var buffer = cmds.AddBuffer<EcsIntElement>(e);
            buffer.CopyFrom(new EcsIntElement[] { 1, 2, 3 });
            cmds.Playback(m_Manager);

            VerifySingleBuffer(3);
            cmds.Dispose();
        }

        [Test]
        public void SetBufferExplicit()
        {
            var e = m_Manager.CreateEntity(typeof(EcsIntElement));
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            DynamicBuffer<EcsIntElement> buffer = cmds.SetBuffer<EcsIntElement>(e);
            buffer.CopyFrom(new EcsIntElement[] { 1, 2, 3 });
            cmds.Playback(m_Manager);
            VerifySingleBuffer(3);
            cmds.Dispose();
        }

        [Test]
        public void AppendToBufferExplicit()
        {
            var e = m_Manager.CreateEntity();
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var buffer = cmds.AddBuffer<EcsIntElement>(e);
            buffer.CopyFrom(new EcsIntElement[] { 1, 2, 3 });
            cmds.AppendToBuffer(e, new EcsIntElement {Value = 4});
            cmds.Playback(m_Manager);

            VerifySingleBuffer(4);
            cmds.Dispose();
        }

        [Test]
        public void AppendToBufferExplicit_LargerThanInternalCapacity()
        {
            var e = m_Manager.CreateEntity();
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var buffer = cmds.AddBuffer<EcsIntElement>(e);
            buffer.CopyFrom(new EcsIntElement[] { 1, 2, 3, 4, 5, 6, 7, 8 });
            cmds.AppendToBuffer(e, new EcsIntElement {Value = 9});
            cmds.Playback(m_Manager);

            VerifySingleBuffer(9);
            cmds.Dispose();
        }

        [Test]
        public void AppendToBufferExplicit_WithinExternalCapacity()
        {
            var e = m_Manager.CreateEntity();
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var buffer = cmds.AddBuffer<EcsIntElement>(e);
            buffer.CopyFrom(new EcsIntElement[] { 1, 2, 3, 4, 5, 6, 7, 8, 9 });
            cmds.AppendToBuffer(e, new EcsIntElement {Value = 10});
            cmds.Playback(m_Manager);

            VerifySingleBuffer(10);
            cmds.Dispose();
        }

        [Test]
        public void AppendToBufferExplicit_LargerThanExternalCapacity()
        {
            var e = m_Manager.CreateEntity();
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var buffer = cmds.AddBuffer<EcsIntElement>(e);
            buffer.CopyFrom(new EcsIntElement[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16 });
            cmds.AppendToBuffer(e, new EcsIntElement {Value = 17});
            cmds.Playback(m_Manager);

            VerifySingleBuffer(17);
            cmds.Dispose();
        }

        [Test, Ignore("Leaks string allocs in Burst")]
        public void AppendToBuffer_BufferDoesNotExist_Fails()
        {
            var e = m_Manager.CreateEntity();
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            cmds.AppendToBuffer(e, new EcsIntElement {Value = 9});
            Assert.Throws<InvalidOperationException>(() => cmds.Playback(m_Manager));

            cmds.Dispose();
        }

        [Test]
        public void AppendToBufferWithEntity_DelayedFixup_ContainsRealizedEntity()
        {
            int kNumOfBuffers = 12; // Must be > 2
            int kNumOfDeferredEntities = 12;

            EntityCommandBuffer cmds = new EntityCommandBuffer(Allocator.TempJob);

            Entity[] e = new Entity[kNumOfBuffers];

            for (int n = 0; n < kNumOfBuffers; n++)
            {
                e[n] = m_Manager.CreateEntity();
                m_Manager.AddBuffer<EcsComplexEntityRefElement>(e[n]);
                for (int i = 0; i < kNumOfDeferredEntities; i++)
                    cmds.AppendToBuffer(e[n], new EcsComplexEntityRefElement() {Entity = cmds.CreateEntity()});
            }

            cmds.RemoveComponent<EcsComplexEntityRefElement>(e[0]);
            cmds.DestroyEntity(e[1]);

            cmds.Playback(m_Manager);
            cmds.Dispose();

            Assert.IsFalse(m_Manager.HasComponent<EcsComplexEntityRefElement>(e[0]));
            Assert.IsFalse(m_Manager.Exists(e[1]));

            for (int n = 2; n < kNumOfBuffers; n++)
            {
                var outbuf = m_Manager.GetBuffer<EcsComplexEntityRefElement>(e[n]);
                Assert.AreEqual(kNumOfDeferredEntities, outbuf.Length);
                for (int i = 0; i < outbuf.Length; i++)
                {
                    Assert.IsTrue(m_Manager.Exists(outbuf[i].Entity));
                }
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        struct AppendToBufferJob : IJobParallelFor
        {
            public EntityCommandBuffer.Concurrent CommandBuffer;
            public NativeArray<Entity> Entities;

            public void Execute(int index)
            {
                var sourceBuffer = new NativeArray<EcsIntElement>(100, Allocator.Temp);

                for (var i = 0; i < sourceBuffer.Length; ++i)
                    CommandBuffer.AppendToBuffer<EcsIntElement>(index, Entities[index], new EcsIntElement {Value = i});

                sourceBuffer.Dispose();
            }
        }

        [Test]
        public void AppendToBuffer_DoesNotThrowInJob()
        {
            var archetype = m_Manager.CreateArchetype(ComponentType.ReadWrite<EcsTestData>(), ComponentType.ReadWrite<EcsIntElement>());
            var entities = new NativeArray<Entity>(100, Allocator.Persistent);
            m_Manager.CreateEntity(archetype, entities);

            EntityCommandBuffer cb = new EntityCommandBuffer(Allocator.Persistent);
            var handle = new AppendToBufferJob()
            {
                CommandBuffer = cb.ToConcurrent(),
                Entities = entities
            }.Schedule(100, 1);
            handle.Complete();
            cb.Playback(m_Manager);

            for (var i = 0; i < 100; ++i)
            {
                var buffer = m_Manager.GetBuffer<EcsIntElement>(entities[i]);
                Assert.AreEqual(100, buffer.Length);
            }

            cb.Dispose();
            entities.Dispose();
        }

        [Test]
        public void AddBuffer_BufferDoesNotExist_Succeeds()
        {
            var e = m_Manager.CreateEntity(typeof(EcsIntElement));
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            DynamicBuffer<EcsIntElement> buffer = cmds.AddBuffer<EcsIntElement>(e);
            buffer.CopyFrom(new EcsIntElement[] { 1, 2, 3 });
            cmds.Playback(m_Manager);
            VerifySingleBuffer(3);
            cmds.Dispose();
        }

        [Test]
        public void AddBuffer_BufferExists_NewValueWins()
        {
            var e = m_Manager.CreateEntity(typeof(EcsIntElement));
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            DynamicBuffer<EcsIntElement> bufferOld = cmds.AddBuffer<EcsIntElement>(e);
            DynamicBuffer<EcsIntElement> bufferNew = cmds.AddBuffer<EcsIntElement>(e);
            bufferNew.CopyFrom(new EcsIntElement[] {1, 2, 3});
            // Writes to old buffer at this point are still valid, but will be ignored.
            Assert.DoesNotThrow(() => { bufferOld.CopyFrom(new EcsIntElement[] {4, 5, 6}); });
            cmds.Playback(m_Manager);
            VerifySingleBuffer(3);
            cmds.Dispose();
        }

        [Test, Ignore("Leaks string allocs in Burst")]
        public void AddBuffer_OnEntityFromOtherWorld_Fails()
        {
            var e = m_Manager.CreateEntity();
            using (var cmds = new EntityCommandBuffer(Allocator.TempJob))
            {
                DynamicBuffer<EcsIntElement> buffer = cmds.AddBuffer<EcsIntElement>(e);
                buffer.CopyFrom(new EcsIntElement[] { 1, 2, 3 });
                Assert.Throws<InvalidOperationException>(() => cmds.Playback(m_Manager2));

                using (var allEntities = m_Manager.GetAllEntities())
                {
                    Assert.AreEqual(1, allEntities.Length);
                    Assert.IsFalse(m_Manager.HasComponent<EcsIntElement>(e));
                }
            }
        }

        [Test]
        public void AddBuffer_AfterDispose_WithoutPlayback_Throws()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);

            var e = cmds.CreateEntity();
            var buffer = cmds.AddBuffer<EcsIntElement>(e);

            Assert.DoesNotThrow(() => buffer.CopyFrom(new EcsIntElement[] { 1, 2, 3, 4, 5 }));

            cmds.Dispose();

            Assert.Throws<InvalidOperationException>(() => buffer.CopyFrom(new EcsIntElement[] { 1, 2, 3, 4, 5, 6, 7, 8, 9, 10 }));
        }

        [Test]
        public void NoConcurrentOnMainThread()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var c = cmds.ToConcurrent();
            Assert.Throws<InvalidOperationException>(() => c.CreateEntity(0));
            cmds.Dispose();
        }

        [BurstCompile(CompileSynchronously = true)]
        struct DeterminismTestJob : IJobParallelFor
        {
            public EntityCommandBuffer.Concurrent Cmds;

            public void Execute(int index)
            {
                Entity e = Cmds.CreateEntity(index);
                Cmds.AddComponent(index, e, new EcsTestData { value = index });
            }
        }

        [Test]
        public void DeterminismTest()
        {
            const int kRepeat = 10000;
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var e = cmds.CreateEntity(); // implicitly, sortIndex=Int32.MaxValue on the main thread
            cmds.AddComponent(e, new EcsTestData { value = kRepeat });
            new DeterminismTestJob { Cmds = cmds.ToConcurrent() }.Schedule(kRepeat, 64).Complete();
            cmds.Playback(m_Manager);
            cmds.Dispose();

            var allEntities = m_Manager.GetAllEntities();
            int count = allEntities.Length;
            Assert.AreEqual(kRepeat + 1, count);
            for (int i = 0; i < count; ++i)
            {
                var data = m_Manager.GetComponentData<EcsTestData>(allEntities[i]);
                Assert.AreEqual(i, data.value);
            }
            allEntities.Dispose();
        }

        [Test]
        public void NoTempAllocatorInConcurrent()
        {
            var cmds = new EntityCommandBuffer(Allocator.Temp);
#pragma warning disable 0219 // assigned but its value is never used
            Assert.Throws<InvalidOperationException>(() => { EntityCommandBuffer.Concurrent c = cmds.ToConcurrent(); });
#pragma warning restore 0219
            cmds.Dispose();
        }

        private void VerifySingleBuffer(int length)
        {
            VerifySingleBuffer(m_Manager, length);
        }

        private static void VerifySingleBuffer(EntityManager manager, int length)
        {
            var allEntities = manager.GetAllEntities();
            Assert.AreEqual(1, allEntities.Length);
            var resultBuffer = manager.GetBuffer<EcsIntElement>(allEntities[0]);
            Assert.AreEqual(length, resultBuffer.Length);

            for (int i = 0; i < length; ++i)
            {
                Assert.AreEqual(i + 1, resultBuffer[i].Value);
            }
            allEntities.Dispose();
        }

        private void VerifyEcsTestData(int length, int expectedValue)
        {
            var allEntities = m_Manager.GetAllEntities();
            Assert.AreEqual(length, allEntities.Length);

            for (int i = 0; i < length; ++i)
            {
                Assert.AreEqual(expectedValue, m_Manager.GetComponentData<EcsTestData>(allEntities[i]).value);
            }
            allEntities.Dispose();
        }

        [BurstCompile(CompileSynchronously = true)]
        struct BufferCopyFromNativeArrayJob : IJobParallelFor
        {
            public EntityCommandBuffer.Concurrent CommandBuffer;
            public NativeArray<Entity> Entities;
            [ReadOnly]
            public NativeArray<EcsIntElement> sourceArray;
            public void Execute(int index)
            {
                var buffer = CommandBuffer.AddBuffer<EcsIntElement>(index, Entities[index]);

                buffer.CopyFrom(sourceArray);
            }
        }

        [BurstCompile(CompileSynchronously = true)]
        struct BufferCopyFromNativeSliceJob : IJobParallelFor
        {
            public EntityCommandBuffer.Concurrent CommandBuffer;
            public NativeArray<Entity> Entities;
            [ReadOnly]
            public NativeSlice<EcsIntElement> sourceSlice;
            public void Execute(int index)
            {
                var buffer = CommandBuffer.AddBuffer<EcsIntElement>(index, Entities[index]);

                buffer.CopyFrom(sourceSlice);
            }
        }

#if !UNITY_DOTSPLAYER_IL2CPP
        // https://unity3d.atlassian.net/browse/DOTSR-1435
        // These tests cause crashes in the IL2CPP runner. Cause not yet debugged.
        // Only fails in Multi-Threaded
        [Test]
        public void BufferCopyFromNativeArrayDoesNotThrowInJob()
        {
            var archetype = m_Manager.CreateArchetype(ComponentType.ReadWrite<EcsTestData>());
            var entities = new NativeArray<Entity>(100, Allocator.Persistent);
            m_Manager.CreateEntity(archetype, entities);

            var testArray = new NativeArray<EcsIntElement>(100, Allocator.TempJob);

            for (int i = 0; i < testArray.Length; i++)
            {
                testArray[i] = i;
            }

            EntityCommandBuffer cb = new EntityCommandBuffer(Allocator.Persistent);
            var handle = new BufferCopyFromNativeArrayJob()
            {
                CommandBuffer = cb.ToConcurrent(),
                Entities = entities,
                sourceArray = testArray
            }.Schedule(100, 1);

            handle.Complete();
            cb.Playback(m_Manager);

            for (var i = 0; i < 100; ++i)
            {
                var buffer = m_Manager.GetBuffer<EcsIntElement>(entities[i]);
                Assert.AreEqual(100, buffer.Length);
                for (int j = 0; j < buffer.Length; j++)
                {
                    Assert.AreEqual(testArray[j].Value, buffer[j].Value);
                }
            }

            testArray.Dispose();
            cb.Dispose();
            entities.Dispose();
        }

        [Test]
        public void BufferCopyFromNativeSliceDoesNotThrowInJob()
        {
            var archetype = m_Manager.CreateArchetype(ComponentType.ReadWrite<EcsTestData>());
            var entities = new NativeArray<Entity>(100, Allocator.Persistent);
            m_Manager.CreateEntity(archetype, entities);
            var testArray = new NativeArray<EcsIntElement>(100, Allocator.TempJob);

            for (int i = 0; i < testArray.Length; i++)
            {
                testArray[i] = i;
            }

            var testSlice = new NativeSlice<EcsIntElement>(testArray);

            EntityCommandBuffer cb = new EntityCommandBuffer(Allocator.Persistent);
            var handle = new BufferCopyFromNativeSliceJob()
            {
                CommandBuffer = cb.ToConcurrent(),
                Entities = entities,
                sourceSlice = testSlice
            }.Schedule(100, 1);
            handle.Complete();
            cb.Playback(m_Manager);

            for (var i = 0; i < 100; ++i)
            {
                var buffer = m_Manager.GetBuffer<EcsIntElement>(entities[i]);
                Assert.AreEqual(100, buffer.Length);
                for (int j = 0; j < buffer.Length; j++)
                {
                    Assert.AreEqual(testSlice[j].Value, buffer[j].Value);
                }
            }

            testArray.Dispose();
            cb.Dispose();
            entities.Dispose();
        }

#endif

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        [Test, Ignore("Leaks string allocs in Burst")]
        public void EntityCommandBufferSystemPlaybackExceptionIsolation()
        {
            var entityCommandBufferSystem = World.GetOrCreateSystem<EndSimulationEntityCommandBufferSystem>();

            var buf1 = entityCommandBufferSystem.CreateCommandBuffer();
            var buf2 = entityCommandBufferSystem.CreateCommandBuffer();

            var e1 = buf1.CreateEntity();
            buf1.AddComponent(e1, new EcsTestData());
            buf1.AddComponent(e1, new EcsTestData());

            var e2 = buf2.CreateEntity();
            buf2.AddComponent(e2, new EcsTestData());
            buf2.AddComponent(Entity.Null, new EcsTestData());

            // We exp both command buffers to execute, and an exception thrown afterwards
            // Essentially we want isolation of two systems that might fail independently.
            Assert.Throws<ArgumentException>(() => { entityCommandBufferSystem.Update(); });
            Assert.AreEqual(2, EmptySystem.GetEntityQuery(typeof(EcsTestData)).CalculateEntityCount());

            // On second run, we expect all buffers to be removed...
            // So no more exceptions thrown.
            entityCommandBufferSystem.Update();

            Assert.AreEqual(2, EmptySystem.GetEntityQuery(typeof(EcsTestData)).CalculateEntityCount());
        }

#endif

        [Test]
        public void AddSharedComponent_WhenComponentHasEntityField_ThrowsArgumentException()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);

            var es = m_Manager.CreateEntity();

            Assert.Throws<ArgumentException>(() =>
            {
                cmds.AddSharedComponent(es, new EcsTestSharedCompEntity(es));
            });

            cmds.Dispose();
        }

        [Test]
        public void AddComponent_WhenDataContainsDeferredEntity_ThrowsOnMultiplePlaybacks()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);

            Entity e0 = cmds.CreateEntity();
            cmds.AddComponent(e0, new EcsTestDataEntity(1, e0));

            Assert.DoesNotThrow(() => cmds.Playback(m_Manager));
            Assert.Throws<InvalidOperationException>(() => cmds.Playback(m_Manager));
            Assert.Throws<InvalidOperationException>(() => cmds.Playback(m_Manager2));
            cmds.Dispose();
        }

        [Test]
        public void AddComponent_WhenDataContainsDeferredEntity_DeferredEntityIsResolved()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);

            Entity e0 = cmds.CreateEntity();
            cmds.AddComponent(e0, new EcsTestDataEntity(1, e0));

            cmds.Playback(m_Manager);
            cmds.Dispose();

            {
                var group = m_Manager.CreateEntityQuery(typeof(EcsTestDataEntity));
                var arr = group.ToComponentDataArray<EcsTestDataEntity>(Allocator.TempJob);

                Assert.AreEqual(1, arr.Length);
                var e0real = arr[0].value1;
                EcsTestDataEntity v0 = m_Manager.GetComponentData<EcsTestDataEntity>(e0real);
                Assert.AreEqual(v0.value1, e0real);

                arr.Dispose();
                group.Dispose();
            }
        }

        [Test]
        public void EntityCommands_WithManyDeferredEntities_PerformAsExpected()
        {
            EntityCommandBuffer cmds = new EntityCommandBuffer(Allocator.Persistent);

            // Slow in IL2CPP: https://unity3d.atlassian.net/browse/DOTSR-1434
#if UNITY_DOTSPLAYER_IL2CPP
            const int step = 100;
#else
            const int step = 1;
#endif

            for (int i = 0; i < 250000; i += step)
            {
                Entity e = cmds.CreateEntity();
                cmds.AddComponent(e, new EcsTestData(i));
                cmds.SetComponent(e, new EcsTestData(i + 1));
                cmds.AddBuffer<EcsIntElement>(e);
                cmds.SetBuffer<EcsIntElement>(e);
                cmds.DestroyEntity(e);
            }
            cmds.Playback(m_Manager);
            cmds.Dispose();

            var allEntities = m_Manager.GetAllEntities();
            Assert.AreEqual(0, allEntities.Length);
            allEntities.Dispose();
        }

        [Test]
        public void InstantiateEntity_BatchMode_DisabledIfEntityDirty()
        {
            EntityCommandBuffer cmds = new EntityCommandBuffer(Allocator.Persistent);
            Entity esrc = m_Manager.CreateEntity();

            Entity edst0 = cmds.Instantiate(esrc);
            cmds.AddComponent(esrc, new EcsTestData2(12));
            Entity edst1 = cmds.Instantiate(esrc);
            cmds.AddComponent(esrc, new EcsTestDataEntity(33, edst1));

            cmds.Playback(m_Manager);
            cmds.Dispose();

            var realDst1 = m_Manager.GetComponentData<EcsTestDataEntity>(esrc).value1;
            Assert.AreEqual(12, m_Manager.GetComponentData<EcsTestData2>(realDst1).value1);
        }

#if !UNITY_PORTABLE_TEST_RUNNER
        // The portable test runner can't handle the return value from Assert.Throws
        [Test]
        public void UninitializedEntityCommandBufferThrows()
        {
            EntityCommandBuffer cmds = new EntityCommandBuffer();
            var exception = Assert.Throws<NullReferenceException>(() => cmds.CreateEntity());
            Assert.AreEqual("The EntityCommandBuffer has not been initialized!", exception.Message);
        }

        [Test]
        public void UninitializedConcurrentEntityCommandBufferThrows()
        {
            EntityCommandBuffer.Concurrent cmds = new EntityCommandBuffer.Concurrent();
            var exception = Assert.Throws<NullReferenceException>(() => cmds.CreateEntity(0));
            Assert.AreEqual("The EntityCommandBuffer has not been initialized!", exception.Message);
        }

#endif

        [Test]
        public void AddOrSetBufferWithEntity_NeedsFixup_ThrowsOnMultiplePlayback([Values(true, false)] bool setBuffer)
        {
            EntityCommandBuffer cmds = new EntityCommandBuffer(Allocator.TempJob);

            Entity e0 = m_Manager.CreateEntity();
            Entity e1 = m_Manager.CreateEntity();
            Entity e2 = m_Manager.CreateEntity();

            if (setBuffer)
                m_Manager.AddComponent(e1, typeof(EcsComplexEntityRefElement));

            var deferred0 = cmds.CreateEntity();
            var deferred1 = cmds.CreateEntity();
            var deferred2 = cmds.CreateEntity();

            cmds.AddComponent(e0, new EcsTestDataEntity() { value1 = deferred0 });
            cmds.AddComponent(e1, new EcsTestDataEntity() { value1 = deferred1 });
            cmds.AddComponent(e2, new EcsTestDataEntity() { value1 = deferred2 });

            var buf = setBuffer ? cmds.SetBuffer<EcsComplexEntityRefElement>(e1) : cmds.AddBuffer<EcsComplexEntityRefElement>(e1);
            buf.Add(new EcsComplexEntityRefElement() {Entity = e0});
            buf.Add(new EcsComplexEntityRefElement() {Entity = deferred1});
            buf.Add(new EcsComplexEntityRefElement() {Entity = deferred2});
            buf.Add(new EcsComplexEntityRefElement() {Entity = deferred0});

            Assert.DoesNotThrow(() => cmds.Playback(m_Manager));
            Assert.Throws<InvalidOperationException>(() => cmds.Playback(m_Manager));
            Assert.Throws<InvalidOperationException>(() => cmds.Playback(m_Manager2));
            cmds.Dispose();
        }

        [Test]
        public void AddOrSetBufferWithEntity_NeedsFixup_ContainsRealizedEntity([Values(true, false)] bool setBuffer)
        {
            EntityCommandBuffer cmds = new EntityCommandBuffer(Allocator.TempJob);

            Entity e0 = m_Manager.CreateEntity();
            Entity e1 = m_Manager.CreateEntity();
            Entity e2 = m_Manager.CreateEntity();

            if (setBuffer)
                m_Manager.AddComponent(e1, typeof(EcsComplexEntityRefElement));

            {
                var deferred0 = cmds.CreateEntity();
                var deferred1 = cmds.CreateEntity();
                var deferred2 = cmds.CreateEntity();

                cmds.AddComponent(e0, new EcsTestDataEntity() { value1 = deferred0 });
                cmds.AddComponent(e1, new EcsTestDataEntity() { value1 = deferred1 });
                cmds.AddComponent(e2, new EcsTestDataEntity() { value1 = deferred2 });

                var buf = setBuffer ? cmds.SetBuffer<EcsComplexEntityRefElement>(e1) : cmds.AddBuffer<EcsComplexEntityRefElement>(e1);
                buf.Add(new EcsComplexEntityRefElement() {Entity = e0});
                buf.Add(new EcsComplexEntityRefElement() {Entity = deferred1});
                buf.Add(new EcsComplexEntityRefElement() {Entity = deferred2});
                buf.Add(new EcsComplexEntityRefElement() {Entity = deferred0});
                cmds.Playback(m_Manager);
                cmds.Dispose();
            }
            {
                var outbuf = m_Manager.GetBuffer<EcsComplexEntityRefElement>(e1);
                Assert.AreEqual(4, outbuf.Length);
                var expect0 = m_Manager.GetComponentData<EcsTestDataEntity>(e0).value1;
                var expect1 = m_Manager.GetComponentData<EcsTestDataEntity>(e1).value1;
                var expect2 = m_Manager.GetComponentData<EcsTestDataEntity>(e2).value1;
                Assert.AreEqual(e0, outbuf[0].Entity);
                Assert.AreEqual(expect1, outbuf[1].Entity);
                Assert.AreEqual(expect2, outbuf[2].Entity);
                Assert.AreEqual(expect0, outbuf[3].Entity);
            }
        }

        [Test]
        public void BufferWithEntity_DelayedFixup_ContainsRealizedEntity()
        {
            int kNumOfBuffers = 12; // Must be > 2
            int kNumOfDeferredEntities = 12;

            EntityCommandBuffer cmds = new EntityCommandBuffer(Allocator.TempJob);

            Entity[] e = new Entity[kNumOfBuffers];

            for (int n = 0; n < kNumOfBuffers; n++)
            {
                e[n] = m_Manager.CreateEntity();
                var buf = cmds.AddBuffer<EcsComplexEntityRefElement>(e[n]);
                for (int i = 0; i < kNumOfDeferredEntities; i++)
                    buf.Add(new EcsComplexEntityRefElement() {Entity = cmds.CreateEntity()});
            }

            cmds.RemoveComponent<EcsComplexEntityRefElement>(e[0]);
            cmds.DestroyEntity(e[1]);

            cmds.Playback(m_Manager);
            cmds.Dispose();

            Assert.IsFalse(m_Manager.HasComponent<EcsComplexEntityRefElement>(e[0]));
            Assert.IsFalse(m_Manager.Exists(e[1]));

            for (int n = 2; n < kNumOfBuffers; n++)
            {
                var outbuf = m_Manager.GetBuffer<EcsComplexEntityRefElement>(e[n]);
                Assert.AreEqual(kNumOfDeferredEntities, outbuf.Length);
                for (int i = 0; i < outbuf.Length; i++)
                {
                    Assert.IsTrue(m_Manager.Exists(outbuf[i].Entity));
                }
            }
        }

#if !UNITY_PORTABLE_TEST_RUNNER
// https://unity3d.atlassian.net/browse/DOTSR-1432
        void VerifyCommand_Or_CheckThatItThrowsIfEntityIsDeferred(bool shouldThrow, TestDelegate code)
        {
            if (shouldThrow)
            {
                var ex = Assert.Throws<ArgumentException>(code);
                Assert.IsTrue(ex.Message.Contains("deferred"));
            }
            else
            {
                code();
            }
        }

        void RunDeferredTest(Entity entity)
        {
            bool isDeferredEntity = entity.Index < 0;

            VerifyCommand_Or_CheckThatItThrowsIfEntityIsDeferred(
                isDeferredEntity,
                () => m_Manager.AddComponent(entity, typeof(EcsTestData)));
            VerifyCommand_Or_CheckThatItThrowsIfEntityIsDeferred(
                isDeferredEntity,
                () => m_Manager.RemoveComponent(entity, typeof(EcsTestData)));
            VerifyCommand_Or_CheckThatItThrowsIfEntityIsDeferred(
                isDeferredEntity,
                () => m_Manager.AddComponentData(entity, new EcsTestData()));
            VerifyCommand_Or_CheckThatItThrowsIfEntityIsDeferred(
                isDeferredEntity,
                () => m_Manager.SetComponentData(entity, new EcsTestData()));
            VerifyCommand_Or_CheckThatItThrowsIfEntityIsDeferred(
                isDeferredEntity,
                () => m_Manager.GetComponentData<EcsTestData>(entity));

            VerifyCommand_Or_CheckThatItThrowsIfEntityIsDeferred(
                isDeferredEntity,
                () => m_Manager.AddSharedComponentData(entity, new EcsTestSharedComp()));
            VerifyCommand_Or_CheckThatItThrowsIfEntityIsDeferred(
                isDeferredEntity,
                () => m_Manager.SetSharedComponentData(entity, new EcsTestSharedComp()));
            VerifyCommand_Or_CheckThatItThrowsIfEntityIsDeferred(
                isDeferredEntity,
                () => m_Manager.GetSharedComponentData<EcsTestSharedComp>(entity));
            VerifyCommand_Or_CheckThatItThrowsIfEntityIsDeferred(
                isDeferredEntity,
                () => m_Manager.RemoveComponent(entity, typeof(EcsTestSharedComp)));

            VerifyCommand_Or_CheckThatItThrowsIfEntityIsDeferred(
                isDeferredEntity,
                () => m_Manager.AddBuffer<EcsIntElement>(entity));
            VerifyCommand_Or_CheckThatItThrowsIfEntityIsDeferred(
                isDeferredEntity,
                () => m_Manager.GetBuffer<EcsIntElement>(entity));
            VerifyCommand_Or_CheckThatItThrowsIfEntityIsDeferred(
                isDeferredEntity,
                () => m_Manager.Exists(entity));
            VerifyCommand_Or_CheckThatItThrowsIfEntityIsDeferred(
                isDeferredEntity,
                () => m_Manager.HasComponent(entity, typeof(EcsTestData2)));
            VerifyCommand_Or_CheckThatItThrowsIfEntityIsDeferred(
                isDeferredEntity,
                () => m_Manager.GetComponentCount(entity));

            VerifyCommand_Or_CheckThatItThrowsIfEntityIsDeferred(
                isDeferredEntity,
                () => m_Manager.DestroyEntity(entity));
        }

        [Test]
        public void DeferredEntities_UsedInTheEntityManager_ShouldThrow()
        {
            using (EntityCommandBuffer cmds = new EntityCommandBuffer(Allocator.TempJob))
            {
                var deferred = cmds.CreateEntity();
                cmds.AddComponent(cmds.CreateEntity(), new EcsTestDataEntity()
                {
                    value1 = deferred
                });

                RunDeferredTest(deferred);

                cmds.Playback(m_Manager);
                using (var group = m_Manager.CreateEntityQuery(typeof(EcsTestDataEntity)))
                using (var arr = group.ToComponentDataArray<EcsTestDataEntity>(Allocator.TempJob))
                {
                    RunDeferredTest(arr[0].value1);
                }
            }
        }

#endif

        [Test]
        public void AddSharedComponent_WithEntityQuery_ThatHasNoMatch_WillNotCorruptInternalState()
        {
            using (EntityCommandBuffer cmds = new EntityCommandBuffer(Allocator.TempJob))
            {
                var entityQuery = m_Manager.CreateEntityQuery(typeof(EcsTestData));
                cmds.AddSharedComponent(entityQuery, new EcsTestSharedComp(1));
                cmds.Playback(m_Manager);
            }
            m_Manager.Debug.CheckInternalConsistency();
        }

#if !UNITY_DISABLE_MANAGED_COMPONENTS
        private void VerifyEcsTestManagedComponent(int length, string expectedValue)
        {
            var allEntities = m_Manager.GetAllEntities();
            Assert.AreEqual(length, allEntities.Length);

            for (int i = 0; i < length; ++i)
            {
                var component = m_Manager.GetComponentData<EcsTestManagedComponent>(allEntities[i]);
                Assert.AreEqual(expectedValue, component.value);
            }
            allEntities.Dispose();
        }

        [Test]
        public void CreateEntity_ManagedComponents()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.MultiPlayback);
            var e = cmds.CreateEntity();
            cmds.AddComponent(e, new EcsTestManagedComponent { value = "SomeString" });
            cmds.Playback(m_Manager);
            cmds.Playback(m_Manager2);
            cmds.Dispose();

            {
                var group = m_Manager.CreateEntityQuery(typeof(EcsTestManagedComponent));
                var arr = group.ToComponentDataArray<EcsTestManagedComponent>();
                Assert.AreEqual(1, arr.Length);
                Assert.AreEqual("SomeString", arr[0].value);
                group.Dispose();
            }
            {
                var group = m_Manager2.CreateEntityQuery(typeof(EcsTestManagedComponent));
                var arr = group.ToComponentDataArray<EcsTestManagedComponent>();
                Assert.AreEqual(1, arr.Length);
                Assert.AreEqual("SomeString", arr[0].value);
                group.Dispose();
            }
        }

        [Test]
        public void CreateEntityWithArchetype_ManagedComponents()
        {
            var a = m_Manager.CreateArchetype(typeof(EcsTestManagedComponent));

            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var e = cmds.CreateEntity(a);
            cmds.SetComponent(e, new EcsTestManagedComponent { value = "SomeString" });
            cmds.Playback(m_Manager);
            cmds.Dispose();

            var group = m_Manager.CreateEntityQuery(typeof(EcsTestManagedComponent));
            var arr = group.ToComponentDataArray<EcsTestManagedComponent>();
            Assert.AreEqual(1, arr.Length);
            Assert.AreEqual("SomeString", arr[0].value);
            group.Dispose();
        }

        [Test]
        public void CreateTwoComponents_ManagedComponents()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob, PlaybackPolicy.MultiPlayback);
            var e = cmds.CreateEntity();
            cmds.AddComponent(e, new EcsTestManagedComponent { value = "SomeString" });
            cmds.AddComponent(e, new EcsTestManagedComponent2 { value = "SomeString", value2 = "SomeOtherString" });
            cmds.Playback(m_Manager);
            cmds.Playback(m_Manager2);
            cmds.Dispose();

            {
                var group = m_Manager.CreateEntityQuery(typeof(EcsTestManagedComponent));
                var arr = group.ToComponentDataArray<EcsTestManagedComponent>();
                Assert.AreEqual(1, arr.Length);
                Assert.AreEqual("SomeString", arr[0].value);
                group.Dispose();
            }

            {
                var group = m_Manager.CreateEntityQuery(typeof(EcsTestManagedComponent2));
                var arr = group.ToComponentDataArray<EcsTestManagedComponent2>();
                Assert.AreEqual(1, arr.Length);
                Assert.AreEqual("SomeString", arr[0].value);
                Assert.AreEqual("SomeOtherString", arr[0].value2);
                group.Dispose();
            }
            {
                var group = m_Manager2.CreateEntityQuery(typeof(EcsTestManagedComponent));
                var arr = group.ToComponentDataArray<EcsTestManagedComponent>();
                Assert.AreEqual(1, arr.Length);
                Assert.AreEqual("SomeString", arr[0].value);
                group.Dispose();
            }

            {
                var group = m_Manager2.CreateEntityQuery(typeof(EcsTestManagedComponent2));
                var arr = group.ToComponentDataArray<EcsTestManagedComponent2>();
                Assert.AreEqual(1, arr.Length);
                Assert.AreEqual("SomeString", arr[0].value);
                Assert.AreEqual("SomeOtherString", arr[0].value2);
                group.Dispose();
            }
        }

        [Test]
        public void AddComponent_ManagedComponents()
        {
            var e = m_Manager.CreateEntity();

            var cmds = new EntityCommandBuffer(Allocator.TempJob);

            cmds.AddComponent(e, new EcsTestManagedComponent() { value = "SomeString" });
            cmds.AddComponent<EcsTestTag>(e);
            cmds.AddComponent(e, ComponentType.ReadWrite<EcsTestManagedComponent3>());

            cmds.Playback(m_Manager);

            cmds.Dispose();

            Assert.AreEqual("SomeString", m_Manager.GetComponentData<EcsTestManagedComponent>(e).value);
            Assert.IsTrue(m_Manager.HasComponent<EcsTestTag>(e));
            Assert.IsTrue(m_Manager.HasComponent<EcsTestManagedComponent3>(e));
        }

        [Test]
        public void AddComponentToEntityQuery_ManagedComponents()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);

            var entity = cmds.CreateEntity();
            var data1 = new EcsTestManagedComponent();
            cmds.AddComponent(entity, data1);
            cmds.AddComponent(entity, typeof(EcsTestManagedComponent));

            var entityQuery = m_Manager.CreateEntityQuery(typeof(EcsTestManagedComponent));
            cmds.AddComponent(entityQuery, typeof(EcsTestManagedComponent2));

            cmds.Playback(m_Manager);

            var entities = m_Manager.GetAllEntities(Allocator.TempJob);

            Assert.AreEqual(1, entities.Length);
            Assert.IsTrue(m_Manager.HasComponent<EcsTestManagedComponent2>(entities[0]), "The component was not added to the entities within the entity query.");

            cmds.Dispose();
            entityQuery.Dispose();
            m_Manager.DestroyEntity(entities);
            entities.Dispose();
        }

        [Test]
        public void AddComponentToEntityQueryWithFilter_ManagedComponents()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestSharedComp));

            var entity1 = cmds.CreateEntity(archetype);
            var sharedComponent1 = new EcsTestSharedComp { value = 10 };
            cmds.SetSharedComponent(entity1, sharedComponent1);

            var entity2 = cmds.CreateEntity(archetype);
            var sharedComponent2 = new EcsTestSharedComp { value = 130 };
            cmds.SetSharedComponent(entity2, sharedComponent2);

            var entityQuery = m_Manager.CreateEntityQuery(typeof(EcsTestSharedComp));
            entityQuery.SetSharedComponentFilter(sharedComponent2);

            cmds.AddComponent(entityQuery, typeof(EcsTestManagedComponent2));

            cmds.Playback(m_Manager);

            var entities = m_Manager.GetAllEntities(Allocator.TempJob);

            Assert.AreEqual(2, entities.Length);
            Assert.IsTrue(m_Manager.HasComponent<EcsTestManagedComponent2>(entities[1]), "The component was not added to the entities within the entity query.");
            Assert.IsFalse(m_Manager.HasComponent<EcsTestManagedComponent2>(entities[0]), "The component was incorrectly added based on the EntityQueryFilter.");

            cmds.Dispose();
            entityQuery.Dispose();
            m_Manager.DestroyEntity(entities);
            entities.Dispose();
        }

        [Test]
        public void RemoveComponentFromEntityQuery_ManagedComponents()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestSharedComp), typeof(EcsTestManagedComponent));

            var entity = cmds.CreateEntity(archetype);
            var data1 = new EcsTestManagedComponent();
            cmds.SetComponent(entity, data1);

            var entityQuery = m_Manager.CreateEntityQuery(typeof(EcsTestManagedComponent));
            cmds.RemoveComponent(entityQuery, typeof(EcsTestManagedComponent));

            cmds.Playback(m_Manager);

            var entities = m_Manager.GetAllEntities(Allocator.TempJob);

            Assert.AreEqual(1, entities.Length);
            Assert.IsFalse(m_Manager.HasComponent<EcsTestManagedComponent>(entities[0]), "The component was not removed from the entities in the entity query.");

            cmds.Dispose();
            entityQuery.Dispose();
            m_Manager.DestroyEntity(entities);
            entities.Dispose();
        }

        [Test]
        public void RemoveComponentFromEntityQuery_ValidateComponents_ManagedComponents()
        {
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestManagedComponent), typeof(EcsTestManagedComponent2), typeof(EcsTestManagedComponent3), typeof(EcsTestManagedComponent4));

            var entity = m_Manager.CreateEntity(archetype);
            var data1 = new EcsTestManagedComponent() { value = "SomeString" };
            var data2 = new EcsTestManagedComponent2() { value = "SomeOtherString" };
            var data3 = new EcsTestManagedComponent3() { value = "YetAnotherString" };
            var data4 = new EcsTestManagedComponent4() { value = "SoManyStrings" };
            m_Manager.SetComponentData(entity, data1);
            m_Manager.SetComponentData(entity, data2);
            m_Manager.SetComponentData(entity, data3);
            m_Manager.SetComponentData(entity, data4);

            {
                var entities = m_Manager.GetAllEntities(Allocator.TempJob);
                Assert.AreEqual(1, entities.Length);
                Assert.IsTrue(m_Manager.HasComponent<EcsTestManagedComponent>(entities[0]));
                Assert.IsTrue(m_Manager.HasComponent<EcsTestManagedComponent2>(entities[0]));
                Assert.IsTrue(m_Manager.HasComponent<EcsTestManagedComponent3>(entities[0]));
                Assert.IsTrue(m_Manager.HasComponent<EcsTestManagedComponent4>(entities[0]));
                Assert.AreEqual("SomeString", m_Manager.GetComponentData<EcsTestManagedComponent>(entities[0]).value);
                Assert.AreEqual("SomeOtherString", m_Manager.GetComponentData<EcsTestManagedComponent2>(entities[0]).value);
                Assert.AreEqual("YetAnotherString", m_Manager.GetComponentData<EcsTestManagedComponent3>(entities[0]).value);
                Assert.AreEqual("SoManyStrings", m_Manager.GetComponentData<EcsTestManagedComponent4>(entities[0]).value);
                entities.Dispose();
            }

            var entityQuery = m_Manager.CreateEntityQuery(typeof(EcsTestManagedComponent));
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            cmds.RemoveComponent(entityQuery, typeof(EcsTestManagedComponent2));
            cmds.Playback(m_Manager);

            {
                var entities = m_Manager.GetAllEntities(Allocator.TempJob);
                Assert.AreEqual(1, entities.Length);
                Assert.IsTrue(m_Manager.HasComponent<EcsTestManagedComponent>(entities[0]));
                Assert.IsFalse(m_Manager.HasComponent<EcsTestManagedComponent2>(entities[0]));
                Assert.IsTrue(m_Manager.HasComponent<EcsTestManagedComponent3>(entities[0]));
                Assert.IsTrue(m_Manager.HasComponent<EcsTestManagedComponent4>(entities[0]));
                Assert.AreEqual("SomeString", m_Manager.GetComponentData<EcsTestManagedComponent>(entities[0]).value);
                Assert.Throws<ArgumentException>(() => { m_Manager.GetComponentData<EcsTestManagedComponent2>(entities[0]); });
                Assert.AreEqual("YetAnotherString", m_Manager.GetComponentData<EcsTestManagedComponent3>(entities[0]).value);
                Assert.AreEqual("SoManyStrings", m_Manager.GetComponentData<EcsTestManagedComponent4>(entities[0]).value);

                m_Manager.DestroyEntity(entities);
                entities.Dispose();
            }
            cmds.Dispose();
            entityQuery.Dispose();
        }

        [Test]
        public void RemoveComponentFromEntityQueryWithFilter_ManagedComponents()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var archetype = m_Manager.CreateArchetype(typeof(EcsTestSharedComp), typeof(EcsTestManagedComponent));

            var entity1 = cmds.CreateEntity(archetype);
            var sharedComponent1 = new EcsTestSharedComp { value = 10 };
            cmds.SetSharedComponent(entity1, sharedComponent1);

            var entity2 = cmds.CreateEntity(archetype);
            var sharedComponent2 = new EcsTestSharedComp { value = 130 };
            cmds.SetSharedComponent(entity2, sharedComponent2);

            var entityQuery = m_Manager.CreateEntityQuery(typeof(EcsTestSharedComp), typeof(EcsTestManagedComponent));
            entityQuery.SetSharedComponentFilter(sharedComponent2);

            cmds.RemoveComponent(entityQuery, typeof(EcsTestManagedComponent));

            cmds.Playback(m_Manager);

            var entities = m_Manager.GetAllEntities(Allocator.TempJob);

            Assert.AreEqual(2, entities.Length);

            Assert.IsTrue(m_Manager.HasComponent<EcsTestManagedComponent>(entities[0]), "The component was incorrectly removed based on the EntityQueryFilter.");
            Assert.IsFalse(m_Manager.HasComponent<EcsTestManagedComponent>(entities[1]), "The component was not removed from the entities in the entity query.");

            cmds.Dispose();
            entityQuery.Dispose();
            m_Manager.DestroyEntity(entities);
            entities.Dispose();
        }

        [Test]
        public void Instantiate_ManagedComponents()
        {
            var e = m_Manager.CreateEntity();
            var component = new EcsTestManagedComponent() { value = "SomeString" };
            m_Manager.AddComponentData(e, component);

            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            cmds.Instantiate(e);
            cmds.Instantiate(e);
            cmds.Playback(m_Manager);
            cmds.Dispose();

            VerifyEcsTestManagedComponent(3, "SomeString");
        }

        [Test]
        public void InstantiateWithSetComponentDataWorks_ManagedComponents()
        {
            var e = m_Manager.CreateEntity();
            m_Manager.AddComponentData(e, new EcsTestManagedComponent() { value = "SomeString" });

            var cmds = new EntityCommandBuffer(Allocator.TempJob);

            var e1 = cmds.Instantiate(e);
            cmds.SetComponent(e1, new EcsTestManagedComponent() { value = "SomeOtherString" });

            var e2 = cmds.Instantiate(e);
            cmds.SetComponent(e2, new EcsTestManagedComponent() { value = "SomeOtherString" });

            cmds.Playback(m_Manager);
            cmds.Dispose();

            m_Manager.DestroyEntity(e);

            VerifyEcsTestManagedComponent(2, "SomeOtherString");
        }

        [Test]
        public void DestroyEntityTwiceWorks_ManagedComponents()
        {
            var e = m_Manager.CreateEntity();
            m_Manager.AddComponentData(e, new EcsTestManagedComponent());

            var cmds = new EntityCommandBuffer(Allocator.TempJob);

            cmds.DestroyEntity(e);
            cmds.DestroyEntity(e);

            cmds.Playback(m_Manager);
            cmds.Dispose();

            Assert.IsFalse(m_Manager.Exists(e));
        }

        [Test, Ignore("Leaks string allocs in Burst")]
        public void DestroyInvalidEntity_ManagedComponents()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var entityBuffer = new NativeArray<Entity>(1, Allocator.Temp);
            var e = cmds.CreateEntity();
            cmds.AddComponent(e, new EcsTestManagedComponent { value = "SomeString" });
            entityBuffer[0] = e;
            cmds.Playback(m_Manager);
            cmds.Dispose();

            var savedEntity = entityBuffer[0];
            entityBuffer.Dispose();

            var cmds2 = new EntityCommandBuffer(Allocator.TempJob);
            cmds2.DestroyEntity(savedEntity);

            // savedEntity is invalid, so playing back this ECB should throw an exception
            Assert.Throws<System.InvalidOperationException>(() =>
            {
                cmds2.Playback(m_Manager);
            });

            cmds2.Dispose();
        }

        [Test]
        public void PlaybackWithExclusiveEntityTransactionInJob_ManagedComponents()
        {
            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            var job = new TestJob {Buffer = cmds};

            var e = cmds.CreateEntity();
            cmds.AddComponent(e, new EcsTestManagedComponent { value = "SomeString" });

            var jobHandle = job.Schedule();

            var manager = m_Manager.BeginExclusiveEntityTransaction();

            var playbackJob = new EntityCommandBufferPlaybackJob()
            {
                Buffer = cmds,
                Manager = manager
            };

            m_Manager.ExclusiveEntityTransactionDependency = playbackJob.Schedule(jobHandle);
            m_Manager.EndExclusiveEntityTransaction();

            cmds.Dispose();

            var group = m_Manager.CreateEntityQuery(typeof(EcsTestData));
            var arr = group.ToComponentDataArray<EcsTestData>(Allocator.TempJob);
            Assert.AreEqual(1, arr.Length);
            Assert.AreEqual(1, arr[0].value);
            arr.Dispose();
            group.Dispose();

            var managedGroup = m_Manager.CreateEntityQuery(typeof(EcsTestManagedComponent));
            var managedComponentArray = managedGroup.ToComponentDataArray<EcsTestManagedComponent>();
            Assert.AreEqual(1, managedComponentArray.Length);
            Assert.AreEqual("SomeString", managedComponentArray[0].value);
            managedGroup.Dispose();
        }

#endif

        [Test]
        public void AddCommandsAfterPlayback_Throws()
        {
            var archetype = m_Manager.CreateArchetype(ComponentType.ReadOnly<EcsTestData>());
            var e = m_Manager.CreateEntity(archetype);
            var query = m_Manager.CreateEntityQuery(ComponentType.ReadOnly<EcsTestData>());

            var cmds = new EntityCommandBuffer(Allocator.TempJob);
            cmds.Playback(m_Manager);

            Assert.Throws<InvalidOperationException>(() => cmds.CreateEntity());
            Assert.Throws<InvalidOperationException>(() => cmds.CreateEntity(archetype));
            Assert.Throws<InvalidOperationException>(() => cmds.Instantiate(e));
            Assert.Throws<InvalidOperationException>(() => cmds.DestroyEntity(e));
            Assert.Throws<InvalidOperationException>(() => cmds.DestroyEntity(query));
            Assert.Throws<InvalidOperationException>(() => cmds.AddBuffer<EcsIntElement>(e));
            Assert.Throws<InvalidOperationException>(() => cmds.SetBuffer<EcsIntElement>(e));
            Assert.Throws<InvalidOperationException>(() => cmds.AddSharedComponent(e, new EcsTestSharedComp(10)));
            Assert.Throws<InvalidOperationException>(() => cmds.SetSharedComponent(e, new EcsTestSharedComp(10)));
            Assert.Throws<InvalidOperationException>(() => cmds.AddComponent(e, new EcsTestData2(10)));
            Assert.Throws<InvalidOperationException>(() => cmds.SetComponent(e, new EcsTestData2(10)));
            Assert.Throws<InvalidOperationException>(() => cmds.SetComponent(e, new EcsTestData2(10)));
            Assert.Throws<InvalidOperationException>(() => cmds.AddComponent(query, ComponentType.ReadOnly<EcsTestData2>()));
            Assert.Throws<InvalidOperationException>(() => cmds.RemoveComponent<EcsTestData>(e));
            Assert.Throws<InvalidOperationException>(() => cmds.RemoveComponent(query, ComponentType.ReadOnly<EcsTestData>()));
#if !UNITY_DISABLE_MANAGED_COMPONENTS
            Assert.Throws<InvalidOperationException>(() => cmds.AddComponent(e, new EcsTestManagedComponent()));
            Assert.Throws<InvalidOperationException>(() => cmds.SetComponent(e, new EcsTestManagedComponent()));
            Assert.Throws<InvalidOperationException>(() => cmds.RemoveComponent<EcsTestManagedComponent>(e));
#endif
            cmds.Dispose();
            query.Dispose();
        }
    }
}

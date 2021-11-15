using System;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Entities.CodeGeneratedJobForEach;
using Unity.Jobs;
using Unity.Transforms;
using UnityEngine;
#if !UNITY_PORTABLE_TEST_RUNNER
using System.Linq;
#endif

namespace Unity.Entities.Tests.ForEachCodegen
{
    [TestFixture]
    public class ForEachCodegenTests : ECSTestsFixture
    {
        [InternalBufferCapacity(8)]
        public struct TestBufferElement : IBufferElementData
        {
            public static implicit operator int(TestBufferElement e) { return e.Value; }
            public static implicit operator TestBufferElement(int e) { return new TestBufferElement { Value = e }; }
            public int Value;
        }

        private MyTestSystem TestSystem;
        private Entity TestEntity;

        [SetUp]
        public void SetUp()
        {
            TestSystem = World.GetOrCreateSystem<MyTestSystem>();

            var myArch = m_Manager.CreateArchetype(
                ComponentType.ReadWrite<EcsTestData>(),
                ComponentType.ReadWrite<EcsTestData2>(),
                ComponentType.ReadWrite<EcsTestSharedComp>(),
                ComponentType.ReadWrite<TestBufferElement>(),
                ComponentType.ReadWrite<EcsTestTag>());

            TestEntity = m_Manager.CreateEntity(myArch);
            m_Manager.SetComponentData(TestEntity, new EcsTestData() { value = 3});
            m_Manager.SetComponentData(TestEntity, new EcsTestData2() { value0 = 4});
            var buffer = m_Manager.GetBuffer<TestBufferElement>(TestEntity);
            buffer.Add(new TestBufferElement() {Value = 18});
            buffer.Add(new TestBufferElement() {Value = 19});
            m_Manager.SetSharedComponentData(TestEntity, new EcsTestSharedComp() { value = 5 });
        }

        [Test]
        public void SimplestCase()
        {
            TestSystem.SimplestCase().Complete();
            Assert.AreEqual(7, m_Manager.GetComponentData<EcsTestData>(TestEntity).value);
        }

        [Test]
        public void WithTagComponent()
        {
            TestSystem.WithTagComponent().Complete();
            Assert.AreEqual(5, m_Manager.GetComponentData<EcsTestData>(TestEntity).value);
        }

        [Test]
        public void WithTagComponentReadOnly()
        {
            TestSystem.WithTagComponentReadOnly().Complete();
            Assert.AreEqual(5, m_Manager.GetComponentData<EcsTestData>(TestEntity).value);
        }

        [Test]
        public void WithAllSharedComponent()
        {
            TestSystem.WithAllSharedComponentData().Complete();
            Assert.AreEqual(4, m_Manager.GetComponentData<EcsTestData>(TestEntity).value);
        }

        [Test]
        public void WithSharedComponentFilter()
        {
            TestSystem.WithSharedComponentFilter().Complete();
            Assert.AreEqual(4, m_Manager.GetComponentData<EcsTestData>(TestEntity).value);
        }

        [Test]
        public void WithChangeFilter()
        {
            TestSystem.WithChangeFilter().Complete();
            Assert.AreEqual(4, m_Manager.GetComponentData<EcsTestData>(TestEntity).value);
        }

        [Test]
        public void WithJobAndThenEntitiesForEach()
        {
            TestSystem.WithJobAndThenEntitiesForEach().Complete();
            Assert.AreEqual(6, m_Manager.GetComponentData<EcsTestData>(TestEntity).value);
        }

        [Test]
        public void StoresEntityQueryInField()
        {
            var entityCountFromQuery = TestSystem.m_StoredQuery.CalculateEntityCount();
            var entityCountFromJob = TestSystem.StoresEntityQueryInField();
            Assert.AreEqual(entityCountFromQuery, entityCountFromJob);
        }

        [Test]
        public void AddToDynamicBuffer()
        {
            TestSystem.AddToDynamicBuffer().Complete();
            var buffer = m_Manager.GetBuffer<TestBufferElement>(TestEntity);
            Assert.AreEqual(3, buffer.Length);
            CollectionAssert.AreEqual(new[] {18, 19, 4}, buffer.Reinterpret<int>().AsNativeArray());
        }

        [Test]
        public void ModifyDynamicBuffer()
        {
            TestSystem.ModifyDynamicBuffer().Complete();
            var buffer = m_Manager.GetBuffer<TestBufferElement>(TestEntity);
            CollectionAssert.AreEqual(new[] {18 * 2, 19 * 2}, buffer.Reinterpret<int>().AsNativeArray());
        }

        [Test]
        public void IterateExistingDynamicBufferReadOnly()
        {
            TestSystem.IterateExistingDynamicBufferReadOnly().Complete();
            Assert.AreEqual(18 + 19, m_Manager.GetComponentData<EcsTestData>(TestEntity).value);
        }

        [Test]
        public void IterateExistingDynamicBuffer_NoModifier()
        {
            TestSystem.IterateExistingDynamicBuffer_NoModifier().Complete();
            Assert.AreEqual(18 + 19 + 20, m_Manager.GetComponentData<EcsTestData>(TestEntity).value);
        }

        [Test]
        public void WithNone()
        {
            TestSystem.WithNone().Complete();
            AssertNothingChanged();
        }

        [Test]
        public void WithAny_DoesntExecute_OnEntityWithoutThatComponent()
        {
            TestSystem.WithAny_DoesntExecute_OnEntityWithoutThatComponent().Complete();
            AssertNothingChanged();
        }

        [Test]
        public void ExecuteLocalFunctionThatCapturesTest()
        {
            TestSystem.ExecuteLocalFunctionThatCaptures().Complete();
            Assert.AreEqual(9, m_Manager.GetComponentData<EcsTestData>(TestEntity).value);
        }

        [Test]
        public void FirstCapturingSecondNotCapturingTest()
        {
            TestSystem.FirstCapturingSecondNotCapturing().Complete();
            Assert.AreEqual(9, m_Manager.GetComponentData<EcsTestData>(TestEntity).value);
        }

        [Test]
        public void FirstNotCapturingThenCapturingTest()
        {
            TestSystem.FirstNotCapturingThenCapturing().Complete();
            Assert.AreEqual(9, m_Manager.GetComponentData<EcsTestData>(TestEntity).value);
        }

        [Test]
        public void UseEntityIndexTest()
        {
            TestSystem.UseEntityIndex();
            Assert.AreEqual(1234, m_Manager.GetComponentData<EcsTestData>(TestEntity).value);
        }

        [Test]
        public void InvokeMethodWhoseLocalsLeakTest()
        {
            TestSystem.InvokeMethodWhoseLocalsLeak();
            Assert.AreEqual(9, m_Manager.GetComponentData<EcsTestData>(TestEntity).value);
        }

        [Test]
        public void RunInsideLoopCapturingLoopConditionTest()
        {
            TestSystem.RunInsideLoopCapturingLoopCondition();
            Assert.AreEqual(103, m_Manager.GetComponentData<EcsTestData>(TestEntity).value);
        }

        [Test]
        public void WriteBackToLocalTest()
        {
            Assert.AreEqual(11, TestSystem.WriteBackToLocal());
        }

        internal struct MySharedComponentData : ISharedComponentData
        {
            public int Value;
        }

        [Test]
        public void CaptureAndOperateOnReferenceTypeTest()
        {
            Assert.AreEqual("Hello there Sailor!", TestSystem.CaptureAndOperateOnReferenceType());
        }

#if !UNITY_PORTABLE_TEST_RUNNER
// https://unity3d.atlassian.net/browse/DOTSR-1432
        [Test]
        public void IterateSharedComponentDataTest()
        {
            var entity1 = TestSystem.EntityManager.CreateEntity(ComponentType.ReadWrite<MySharedComponentData>());
            var entity2 = TestSystem.EntityManager.CreateEntity(ComponentType.ReadWrite<MySharedComponentData>());

            TestSystem.EntityManager.SetSharedComponentData(entity1, new MySharedComponentData() {Value = 1});
            TestSystem.EntityManager.SetSharedComponentData(entity2, new MySharedComponentData() {Value = 2});

            var observedDatas = TestSystem.IterateSharedComponentData();

            Assert.AreEqual(2, observedDatas.Count);

            var sorted = observedDatas.OrderBy(o => o.Value).ToArray();
            Assert.AreEqual(1, sorted[0].Value);
            Assert.AreEqual(2, sorted[1].Value);
        }

#endif

        public struct MyBufferElementData : IBufferElementData
        {
            public int Value;
        }

        [Test]
        public void WithBufferElementAsQueryFilterTest()
        {
            var entity1 = TestSystem.EntityManager.CreateEntity(ComponentType.ReadWrite<MyBufferElementData>());
            var entity2 = TestSystem.EntityManager.CreateEntity();

            var observedEntities = TestSystem.BufferElementAsQueryFilter();

            CollectionAssert.Contains(observedEntities, entity2);
            CollectionAssert.DoesNotContain(observedEntities, entity1);
        }

        [Test]
        public void InvokeInstanceMethodWhileCapturingNothingTest()
        {
            var result = TestSystem.InvokeInstanceMethodWhileCapturingNothing();
            Assert.AreEqual(124, result);
        }

        [Test]
        public void CaptureFieldAndLocalNoBurstAndRunTest()
        {
            var result = TestSystem.CaptureFieldAndLocalNoBurstAndRun();
            Assert.AreEqual(124, result);
        }

        [Test]
        public void CaptureFromMultipleScopesAndRunTest()
        {
            TestSystem.CaptureFromMultipleScopesAndRun();
        }

        [Test]
        public void CaptureFromMultipleScopesAndScheduleTest()
        {
            TestSystem.CaptureFromMultipleScopesAndSchedule().Complete();
            Assert.AreEqual(6, m_Manager.GetComponentData<EcsTestData>(TestEntity).value);
        }

        [Test]
        public void CaptureInnerAndOuterStructAndRunTest()
        {
            TestSystem.CaptureInnerAndOuterStructAndRun();
            Assert.AreEqual(9, m_Manager.GetComponentData<EcsTestData>(TestEntity).value);
        }

        [Test]
        public void CaptureInnerAndOuterValueAndScheduleTest()
        {
            TestSystem.CaptureInnerAndOuterValueAndSchedule().Complete();
            Assert.AreEqual(6, m_Manager.GetComponentData<EcsTestData>(TestEntity).value);
        }

#if !UNITY_DISABLE_MANAGED_COMPONENTS
        [Test]
        [DotsRuntimeFixme] // UnsafeUtility.CopyObjectAddressToPtr not implemented
        public void ManyManagedComponents()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponentData(entity, new EcsTestManagedComponent() { value = "SomeString" });
            m_Manager.AddComponentData(entity, new EcsTestManagedComponent2() { value2 = "SomeString2" });
            m_Manager.AddComponentData(entity, new EcsTestManagedComponent3() { value3 = "SomeString3" });
            m_Manager.AddComponentData(entity, new EcsTestManagedComponent4() { value4 = "SomeString4" });
            TestSystem.Many_ManagedComponents();
        }

#endif

#if !UNITY_DOTSPLAYER
        [Test]
        public void UseUnityEngineComponent()
        {
            var entity = m_Manager.CreateEntity();
            m_Manager.AddComponent<Camera>(entity);
            m_Manager.SetComponentObject(entity, ComponentType.ReadWrite<Camera>(), Camera.main);
            (Camera reportedCamera, Entity reportedEntity) = TestSystem.IterateEntitiesWithCameraComponent();
            Assert.AreEqual(Camera.main, reportedCamera);
            Assert.AreEqual(entity, reportedEntity);
        }

#endif

        [Test]
        [DotsRuntimeFixme]
        public void JobDebuggerSafetyThrowsInRun()
        {
            var jobHandle = TestSystem.ScheduleEcsTestData();
            Assert.Throws<InvalidOperationException>(() => { TestSystem.RunEcsTestData(); });
            jobHandle.Complete();
        }

        [Test]
        [DotsRuntimeFixme]
        public void JobDebuggerSafetyThrowsInSchedule()
        {
            var jobHandle = TestSystem.ScheduleEcsTestData();
            Assert.Throws<InvalidOperationException>(() => { TestSystem.ScheduleEcsTestData(); });
            jobHandle.Complete();
        }

        [Test]
        public void ForEachWithCustomDelegateTypeWithMoreThan8Parameters()
        {
            TestSystem.RunForEachWithCustomDelegateTypeWithMoreThan8Parameters();
        }


        class MyTestSystem : TestJobComponentSystem
        {
            public EntityQuery m_StoredQuery;

            public JobHandle SimplestCase()
            {
                //int multiplier = 1;
                return Entities.ForEach((ref EcsTestData e1, in EcsTestData2 e2) => { e1.value += e2.value0;}).Schedule(default);
            }

            public JobHandle WithTagComponent()
            {
                return Entities.ForEach((ref EcsTestData e1, ref EcsTestTag e2) => { e1.value = 5;}).Schedule(default);
            }

            public JobHandle WithTagComponentReadOnly()
            {
                return Entities.ForEach((ref EcsTestData e1, in EcsTestTag e2) => { e1.value = 5;}).Schedule(default);
            }

            public JobHandle WithNone()
            {
                int multiplier = 1;
                return Entities
                    .WithNone<EcsTestData2>()
                    .ForEach((ref EcsTestData e1) => { e1.value += multiplier;})
                    .Schedule(default);
            }

            public JobHandle WithAny_DoesntExecute_OnEntityWithoutThatComponent()
            {
                int multiplier = 1;
                return Entities
                    .WithAny<EcsTestData3>()
                    .ForEach((ref EcsTestData e1) => { e1.value += multiplier;})
                    .Schedule(default);
            }

            public JobHandle WithAllSharedComponentData()
            {
                int multiplier = 1;
                return Entities
                    .WithAll<EcsTestSharedComp>()
                    .ForEach((ref EcsTestData e1) => { e1.value += multiplier;})
                    .Schedule(default);
            }

            public JobHandle WithSharedComponentFilter()
            {
                int multiplier = 1;
                return Entities
                    .WithSharedComponentFilter(new EcsTestSharedComp() { value = 5 })
                    .ForEach((ref EcsTestData e1) => { e1.value += multiplier;})
                    .Schedule(default);
            }

            public JobHandle WithChangeFilter()
            {
                int multiplier = 1;

                // GlobalSystemVersion starts at 1
                // 3 + 1 = 4, bump version number to 1
                Entities
                    .WithChangeFilter<EcsTestData>()
                    .ForEach((ref EcsTestData e1) => { e1.value += multiplier;})
                    .Run();

                AfterUpdateVersioning();  // sets last version to current version (1)
                BeforeUpdateVersioning(); // increments version and sets all queries to last version (1)

                // Shouldn't run, version matches system version (1)
                Entities
                    .WithChangeFilter<EcsTestData>()
                    .ForEach((ref EcsTestData e1) => { e1.value += multiplier;})
                    .Run();

                return default;
            }

            public JobHandle WithJobAndThenEntitiesForEach()
            {
                int multiplier = 1;

                Job.WithCode(() => { multiplier = 3; }).Run();

                return Entities
                    .ForEach((ref EcsTestData e1) => { e1.value += multiplier;})
                    .Schedule(default);
            }

            public int StoresEntityQueryInField()
            {
                int count = 0;

                Entities
                    .WithStoreEntityQueryInField(ref m_StoredQuery)
                    .ForEach((ref EcsTestData e1) => { count++; })
                    .Run();

                return count;
            }

            public JobHandle AddToDynamicBuffer()
            {
                return Entities
                    .ForEach((ref EcsTestData e1, ref DynamicBuffer<TestBufferElement> buf) =>
                {
                    buf.Add(4);
                })
                    .Schedule(default);
            }

            public JobHandle ModifyDynamicBuffer()
            {
                return Entities
                    .ForEach((ref EcsTestData e1, ref DynamicBuffer<TestBufferElement> buf) =>
                {
                    for (int i = 0; i < buf.Length; ++i) buf[i] = buf[i].Value * 2;
                })
                    .Schedule(default);
            }

            public JobHandle IterateExistingDynamicBufferReadOnly()
            {
                return Entities
                    .ForEach((ref EcsTestData e1, in DynamicBuffer<TestBufferElement> buf) =>
                {
                    e1.value = SumOfBufferElements(buf);
                })
                    .Schedule(default);
            }

            public JobHandle IterateExistingDynamicBuffer_NoModifier()
            {
                return Entities
                    .ForEach((DynamicBuffer<TestBufferElement> buf, ref EcsTestData e1) =>
                {
                    buf.Add(20);
                    e1.value = SumOfBufferElements(buf);
                })
                    .Schedule(default);
            }

            private static int SumOfBufferElements(DynamicBuffer<TestBufferElement> buf)
            {
                int total = 0;
                for (int i = 0; i != buf.Length; i++)
                    total += buf[i].Value;
                return total;
            }

            public void CaptureFromMultipleScopesAndRun()
            {
                int scope1 = 1;
                {
                    int scope2 = 2;
                    {
                        int scope3 = 3;
                        Entities
                            .ForEach((ref EcsTestData e1) =>
                        {
                            var sum = scope1 + scope2 + scope3;
                            scope1 = sum;
                            scope2 = -sum;
                            scope3 = 321;
                        })
                            .Run();

                        Assert.AreEqual(-6, scope2);
                        Assert.AreEqual(6, scope1);
                        Assert.AreEqual(321, scope3);
                    }
                }
            }

            public JobHandle CaptureFromMultipleScopesAndSchedule()
            {
                int scope1 = 1;
                {
                    int scope2 = 2;
                    {
                        int scope3 = 3;
                        return Entities
                            .ForEach((ref EcsTestData e1) =>
                        {
                            e1.value = scope1 + scope2 + scope3;
                        })
                            .Schedule(default);
                    }
                }
            }

            public JobHandle ExecuteLocalFunctionThatCaptures()
            {
                int capture_from_outer_scope = 1;
                return Entities
                    .ForEach((ref EcsTestData e1) =>
                {
                    int capture_from_delegate_scope = 8;
                    int MyLocalFunction()
                    {
                        return capture_from_outer_scope + capture_from_delegate_scope;
                    }
                    e1.value = MyLocalFunction();
                })
                    .Schedule(default);
            }

            public JobHandle FirstCapturingSecondNotCapturing()
            {
                int capturedValue = 3;
                var job1 = Entities.ForEach((ref EcsTestData e1) => e1.value = capturedValue).Schedule(default);
                return Entities.ForEach((ref EcsTestData e1) => e1.value *= 3).Schedule(job1);
            }

            public JobHandle FirstNotCapturingThenCapturing()
            {
                int capturedValue = 3;
                var job1 = Entities.ForEach((ref EcsTestData e1) => e1.value = 3).Schedule(default);
                return Entities.ForEach((ref EcsTestData e1) => e1.value *= capturedValue).Schedule(job1);
            }

            public void InvokeMethodWhoseLocalsLeak()
            {
                var normalDelegate = MethodWhoseLocalsLeak();
                Assert.AreEqual(8, normalDelegate());
            }

            public Func<int> MethodWhoseLocalsLeak()
            {
                int capturedValue = 3;
                Entities.ForEach((ref EcsTestData e1) => e1.value *= capturedValue).Schedule(default).Complete();
                int someOtherValue = 8;
                return () => someOtherValue;
            }

            public void UseEntityIndex()
            {
                Entities.ForEach((int entityInQueryIndex, ref EcsTestData etd) =>
                {
                    etd.value = entityInQueryIndex + 1234;
                }).Run();
            }

            public JobHandle ScheduleEcsTestData()
            {
                int multiplier = 1;
                return Entities
                    .ForEach((ref EcsTestData e1) => { e1.value += multiplier;})
                    .Schedule(default);
            }

            public void RunEcsTestData()
            {
                int multiplier = 1;
                Entities
                    .ForEach((ref EcsTestData e1) => { e1.value += multiplier; })
                    .Run();
            }

            public void RunInsideLoopCapturingLoopCondition()
            {
                int variable = 10;
                for (int i = 0; i != variable; i++)
                {
                    Entities
                        .ForEach((ref EcsTestData e1) => { e1.value += variable; })
                        .Run();
                }
            }

            public int WriteBackToLocal()
            {
                int variable = 10;
                Entities.ForEach((ref EcsTestData e1) => { variable++; }).Run();
                return variable;
            }

            public string CaptureAndOperateOnReferenceType()
            {
                string myString = "Hello";
                Entities.WithoutBurst().ForEach((ref EcsTestData e1) => myString += " there Sailor!").Run();
                return myString;
            }

            public List<MySharedComponentData> IterateSharedComponentData()
            {
                var result = new List<MySharedComponentData>();

                Entities.WithoutBurst().ForEach((MySharedComponentData data) => { result.Add(data); }).Run();
                return result;
            }

            public List<Entity> BufferElementAsQueryFilter()
            {
                var result = new List<Entity>();
                Entities
                    .WithoutBurst()
                    .WithNone<MyBufferElementData>()
                    .ForEach((Entity e) => { result.Add(e); })
                    .Run();
                return result;
            }

            void MyInstanceMethod()
            {
                myField++;
            }

            private int myField;
            public int InvokeInstanceMethodWhileCapturingNothing()
            {
                myField = 123;
                Entities.WithoutBurst().ForEach((Entity e) => { MyInstanceMethod(); }).Run();
                return myField;
            }

            private int mySpecialField = 123;
            public int CaptureFieldAndLocalNoBurstAndRun()
            {
                int localValue = 1;
                Entities.WithoutBurst().ForEach((Entity e) => mySpecialField += localValue).Run();
                return mySpecialField;
            }

            struct CaptureStruct
            {
                public int Value;
            }
            public void CaptureInnerAndOuterStructAndRun()
            {
                var outter = new CaptureStruct() { Value = 1 };
                {
                    var inner = new CaptureStruct() {Value = 3};
                    outter.Value = 2;
                    int multiplier = 1;
                    Entities
                        .ForEach((ref EcsTestData e1) =>
                    {
                        e1.value += multiplier + outter.Value + inner.Value;
                    })
                        .Run();
                }
            }

            public JobHandle CaptureInnerAndOuterValueAndSchedule()
            {
                int outerCapure = 3;
                {
                    int innerCapture = 2;
                    return Entities
                        .ForEach((ref EcsTestData testData) =>
                    {
                        testData.value = outerCapure * innerCapture;
                    })
                        .Schedule(default);
                }
            }

#if !UNITY_DISABLE_MANAGED_COMPONENTS
            public void Many_ManagedComponents()
            {
                var counter = 0;
                Entities.WithoutBurst().ForEach(
                    (EcsTestManagedComponent t0, EcsTestManagedComponent2 t1, EcsTestManagedComponent3 t2,
                        in EcsTestManagedComponent4 t3) =>
                    {
                        Assert.AreEqual("SomeString", t0.value);
                        Assert.AreEqual("SomeString2", t1.value2);
                        Assert.AreEqual("SomeString3", t2.value3);
                        Assert.AreEqual("SomeString4", t3.value4);
                        counter++;
                    }).Run();
                Assert.AreEqual(1, counter);
            }

#endif

#if !UNITY_DOTSPLAYER
            public (Camera, Entity) IterateEntitiesWithCameraComponent()
            {
                (Camera camera, Entity entity)result = default;
                Entities.WithoutBurst().ForEach((Camera camera, Entity e) =>
                {
                    result.camera = camera;
                    result.entity = e;
                }).Run();
                return result;
            }

#endif
            public void RunForEachWithCustomDelegateTypeWithMoreThan8Parameters()
            {
                int grabbedData = -1;
                Entities.ForEach((Entity e0, Entity e1, Entity e2, Entity e3, Entity e4, Entity e5, Entity e6, Entity e7, Entity e8, Entity e9, Entity e10, in EcsTestData data) =>
                {
                    grabbedData = data.value;
                }).Run();
                Assert.AreEqual(3,  grabbedData);
            }
        }



        void AssertNothingChanged() => Assert.AreEqual(3, m_Manager.GetComponentData<EcsTestData>(TestEntity).value);
    }

    public class TestJobComponentSystem : JobComponentSystem
    {
        protected override JobHandle OnUpdate(JobHandle inputDeps) => inputDeps;
    }
}


static class BringYourOwnDelegate
{
    [EntitiesForEachCompatible]
    public delegate void CustomForEachDelegate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(T0 t0, T1 t1, T2 t2, T3 t3, T4 t4, T5 t5, T6 t6, T7 t7, T8 t8, T9 t9, T10 t10, in T11 t11);

    public static TDescription ForEach<TDescription, T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11>(this TDescription description, CustomForEachDelegate<T0, T1, T2, T3, T4, T5, T6, T7, T8, T9, T10, T11> codeToRun)
        where TDescription : struct, ISupportForEachWithUniversalDelegate =>
        LambdaForEachDescriptionConstructionMethods.ThrowCodeGenException<TDescription>();
}

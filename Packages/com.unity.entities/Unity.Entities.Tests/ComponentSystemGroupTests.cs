using System;
using System.Collections;
using System.Collections.Generic;
using NUnit.Framework;
using Unity.Jobs;
using UnityEngine;
using UnityEngine.TestTools;

#if !UNITY_PORTABLE_TEST_RUNNER
using System.Text.RegularExpressions;
using System.Linq;
#endif

namespace Unity.Entities.Tests
{
    class ComponentSystemGroupTests : ECSTestsFixture
    {
        class TestGroup : ComponentSystemGroup
        {
        }

#if NET_DOTS
        private class TestSystemBase : ComponentSystem
        {
            protected override void OnUpdate() => throw new System.NotImplementedException();
        }

#else
        private class TestSystemBase : JobComponentSystem
        {
            protected override JobHandle OnUpdate(JobHandle inputDeps) => throw new System.NotImplementedException();
        }
#endif

        [Test]
        public void SortEmptyParentSystem([Values(true, false)] bool legacy)
        {
            var parent = World.CreateSystem<TestGroup>();
            parent.UseLegacySortOrder = legacy;
            Assert.DoesNotThrow(() => { parent.SortSystems(); });
        }

        class TestSystem : TestSystemBase
        {
        }

        [Test]
        public void SortOneChildSystem([Values(true, false)] bool legacy)
        {
            var parent = World.CreateSystem<TestGroup>();
            parent.UseLegacySortOrder = legacy;

            var child = World.CreateSystem<TestSystem>();
            parent.AddSystemToUpdateList(child);
            parent.SortSystems();
            CollectionAssert.AreEqual(new[] {child}, parent.Systems);
        }

        [UpdateAfter(typeof(Sibling2System))]
        class Sibling1System : TestSystemBase
        {
        }
        class Sibling2System : TestSystemBase
        {
        }

        [Test]
        public void SortTwoChildSystems_CorrectOrder([Values(true, false)] bool legacy)
        {
            var parent = World.CreateSystem<TestGroup>();
            parent.UseLegacySortOrder = legacy;
            var child1 = World.CreateSystem<Sibling1System>();
            var child2 = World.CreateSystem<Sibling2System>();
            parent.AddSystemToUpdateList(child1);
            parent.AddSystemToUpdateList(child2);
            parent.SortSystems();
            CollectionAssert.AreEqual(new TestSystemBase[] {child2, child1}, parent.Systems);
        }

        // This test constructs the following system dependency graph:
        // 1 -> 2 -> 3 -> 4 -v
        //           ^------ 5 -> 6
        // The expected results of topologically sorting this graph:
        // - systems 1 and 2 are properly sorted in the system update list.
        // - systems 3, 4, and 5 form a cycle (in that order, or equivalent).
        // - system 6 is not sorted AND is not part of the cycle.
        [UpdateBefore(typeof(Circle2System))]
        class Circle1System : TestSystemBase
        {
        }
        [UpdateBefore(typeof(Circle3System))]
        class Circle2System : TestSystemBase
        {
        }
        [UpdateAfter(typeof(Circle5System))]
        class Circle3System : TestSystemBase
        {
        }
        [UpdateAfter(typeof(Circle3System))]
        class Circle4System : TestSystemBase
        {
        }
        [UpdateAfter(typeof(Circle4System))]
        class Circle5System : TestSystemBase
        {
        }
        [UpdateAfter(typeof(Circle5System))]
        class Circle6System : TestSystemBase
        {
        }

#if !UNITY_PORTABLE_TEST_RUNNER
// https://unity3d.atlassian.net/browse/DOTSR-1432

        [Test]
#if NET_DOTS
        [Ignore("Tiny pre-compiles systems. Many tests will fail if they exist, not just this one.")]
#endif
        public void DetectCircularDependency_Throws([Values(true, false)] bool legacy)
        {
            var parent = World.CreateSystem<TestGroup>();
            parent.UseLegacySortOrder = legacy;
            var child1 = World.CreateSystem<Circle1System>();
            var child2 = World.CreateSystem<Circle2System>();
            var child3 = World.CreateSystem<Circle3System>();
            var child4 = World.CreateSystem<Circle4System>();
            var child5 = World.CreateSystem<Circle5System>();
            var child6 = World.CreateSystem<Circle6System>();
            parent.AddSystemToUpdateList(child3);
            parent.AddSystemToUpdateList(child6);
            parent.AddSystemToUpdateList(child2);
            parent.AddSystemToUpdateList(child4);
            parent.AddSystemToUpdateList(child1);
            parent.AddSystemToUpdateList(child5);
            var e = Assert.Throws<ComponentSystemSorter.CircularSystemDependencyException>(() => parent.SortSystems());
            // Make sure the cycle expressed in e.Chain is the one we expect, even though it could start at any node
            // in the cycle.
            var expectedCycle = new Type[] {typeof(Circle5System), typeof(Circle3System), typeof(Circle4System)};
            var cycle = e.Chain.ToList();
            bool foundCycleMatch = false;
            for (int i = 0; i < cycle.Count; ++i)
            {
                var offsetCycle = new System.Collections.Generic.List<Type>(cycle.Count);
                offsetCycle.AddRange(cycle.GetRange(i, cycle.Count - i));
                offsetCycle.AddRange(cycle.GetRange(0, i));
                Assert.AreEqual(cycle.Count, offsetCycle.Count);
                if (expectedCycle.SequenceEqual(offsetCycle))
                {
                    foundCycleMatch = true;
                    break;
                }
            }
            Assert.IsTrue(foundCycleMatch);
        }

#endif // UNITY_DOTSPLAYER_IL2CPP

        class Unconstrained1System : TestSystemBase
        {
        }
        class Unconstrained2System : TestSystemBase
        {
        }
        class Unconstrained3System : TestSystemBase
        {
        }
        class Unconstrained4System : TestSystemBase
        {
        }

        [Test]
        public void SortUnconstrainedSystems_IsDeterministic([Values(true, false)] bool legacy)
        {
            var parent = World.CreateSystem<TestGroup>();
            parent.UseLegacySortOrder = true;
            var child1 = World.CreateSystem<Unconstrained1System>();
            var child2 = World.CreateSystem<Unconstrained2System>();
            var child3 = World.CreateSystem<Unconstrained3System>();
            var child4 = World.CreateSystem<Unconstrained4System>();
            parent.AddSystemToUpdateList(child2);
            parent.AddSystemToUpdateList(child4);
            parent.AddSystemToUpdateList(child3);
            parent.AddSystemToUpdateList(child1);
            parent.SortSystems();
            CollectionAssert.AreEqual(parent.Systems, new TestSystemBase[] {child1, child2, child3, child4});
        }

        private class UpdateCountingSystemBase : ComponentSystem
        {
            public int CompleteUpdateCount = 0;
            protected override void OnUpdate()
            {
                ++CompleteUpdateCount;
            }
        }
        class NonThrowing1System : UpdateCountingSystemBase
        {
        }
        class NonThrowing2System : UpdateCountingSystemBase
        {
        }
        class ThrowingSystem : UpdateCountingSystemBase
        {
            public string ExceptionMessage = "I should always throw!";
            protected override void OnUpdate()
            {
                if (CompleteUpdateCount == 0)
                {
                    throw new InvalidOperationException(ExceptionMessage);
                }
                base.OnUpdate();
            }
        }

#if !NET_DOTS // Tiny precompiles systems, and lacks a Regex overload for LogAssert.Expect()
        [Test]
        public void SystemInGroupThrows_LaterSystemsRun([Values(true, false)] bool legacy)
        {
            var parent = World.CreateSystem<TestGroup>();
            parent.UseLegacySortOrder = legacy;
            var child1 = World.CreateSystem<NonThrowing1System>();
            var child2 = World.CreateSystem<ThrowingSystem>();
            var child3 = World.CreateSystem<NonThrowing2System>();
            parent.AddSystemToUpdateList(child1);
            parent.AddSystemToUpdateList(child2);
            parent.AddSystemToUpdateList(child3);
            parent.Update();
            LogAssert.Expect(LogType.Exception, new Regex(child2.ExceptionMessage));
            Assert.AreEqual(1, child1.CompleteUpdateCount);
            Assert.AreEqual(0, child2.CompleteUpdateCount);
            Assert.AreEqual(1, child3.CompleteUpdateCount);
        }

#endif

#if !NET_DOTS // Tiny precompiles systems, and lacks a Regex overload for LogAssert.Expect()
        [Test]
        public void SystemThrows_SystemNotRemovedFromUpdate([Values(true, false)] bool legacy)
        {
            var parent = World.CreateSystem<TestGroup>();
            parent.UseLegacySortOrder = legacy;
            var child = World.CreateSystem<ThrowingSystem>();
            parent.AddSystemToUpdateList(child);
            parent.Update();
            LogAssert.Expect(LogType.Exception, new Regex(child.ExceptionMessage));
            parent.Update();
            LogAssert.Expect(LogType.Exception, new Regex(child.ExceptionMessage));

            Assert.AreEqual(0, child.CompleteUpdateCount);
        }

#endif

#if !NET_DOTS // Tiny precompiles systems, and lacks a Regex overload for LogAssert.Expect()
        [UpdateAfter(typeof(NonSibling2System))]
        class NonSibling1System : TestSystemBase
        {
        }
        [UpdateBefore(typeof(NonSibling1System))]
        class NonSibling2System : TestSystemBase
        {
        }

        [Test]
        public void ComponentSystemGroup_UpdateAfterTargetIsNotSibling_LogsWarning([Values(true, false)] bool legacy)
        {
            var parent = World.CreateSystem<TestGroup>();
            parent.UseLegacySortOrder = legacy;
            var child = World.CreateSystem<NonSibling1System>();
            parent.AddSystemToUpdateList(child);
            parent.SortSystems();
            LogAssert.Expect(LogType.Warning, new Regex(@"Ignoring invalid \[UpdateAfter\].+NonSibling1System.+belongs to a different ComponentSystemGroup"));
        }

        [Test]
        public void ComponentSystemGroup_UpdateBeforeTargetIsNotSibling_LogsWarning([Values(true, false)] bool legacy)
        {
            var parent = World.CreateSystem<TestGroup>();
            parent.UseLegacySortOrder = legacy;
            var child = World.CreateSystem<NonSibling2System>();
            parent.AddSystemToUpdateList(child);
            parent.SortSystems();
            LogAssert.Expect(LogType.Warning, new Regex(@"Ignoring invalid \[UpdateBefore\].+NonSibling2System.+belongs to a different ComponentSystemGroup"));
        }

#endif

#if !NET_DOTS
        [UpdateAfter(typeof(NotEvenASystem))]
        class InvalidUpdateAfterSystem : TestSystemBase
        {
        }
        [UpdateBefore(typeof(NotEvenASystem))]
        class InvalidUpdateBeforeSystem : TestSystemBase
        {
        }
        class NotEvenASystem
        {
        }

        [Test]
        public void ComponentSystemGroup_UpdateAfterTargetIsNotSystem_LogsWarning([Values(true, false)] bool legacy)
        {
            var parent = World.CreateSystem<TestGroup>();
            parent.UseLegacySortOrder = legacy;
            var child = World.CreateSystem<InvalidUpdateAfterSystem>();
            parent.AddSystemToUpdateList(child);
            parent.SortSystems();
            LogAssert.Expect(LogType.Warning, new Regex(@"Ignoring invalid \[UpdateAfter\].+InvalidUpdateAfterSystem.+NotEvenASystem is not a subclass of ComponentSystemBase"));
        }

        [Test]
        public void ComponentSystemGroup_UpdateBeforeTargetIsNotSystem_LogsWarning([Values(true, false)] bool legacy)
        {
            var parent = World.CreateSystem<TestGroup>();
            parent.UseLegacySortOrder = legacy;
            var child = World.CreateSystem<InvalidUpdateBeforeSystem>();
            parent.AddSystemToUpdateList(child);
            parent.SortSystems();
            LogAssert.Expect(LogType.Warning, new Regex(@"Ignoring invalid \[UpdateBefore\].+InvalidUpdateBeforeSystem.+NotEvenASystem is not a subclass of ComponentSystemBase"));
        }

        [UpdateAfter(typeof(UpdateAfterSelfSystem))]
        class UpdateAfterSelfSystem : TestSystemBase
        {
        }
        [UpdateBefore(typeof(UpdateBeforeSelfSystem))]
        class UpdateBeforeSelfSystem : TestSystemBase
        {
        }

        [Test]
        public void ComponentSystemGroup_UpdateAfterTargetIsSelf_LogsWarning([Values(true, false)] bool legacy)
        {
            var parent = World.CreateSystem<TestGroup>();
            parent.UseLegacySortOrder = legacy;
            var child = World.CreateSystem<UpdateAfterSelfSystem>();
            parent.AddSystemToUpdateList(child);
            parent.SortSystems();
            LogAssert.Expect(LogType.Warning, new Regex(@"Ignoring invalid \[UpdateAfter\].+UpdateAfterSelfSystem.+cannot be updated after itself."));
        }

        [Test]
        public void ComponentSystemGroup_UpdateBeforeTargetIsSelf_LogsWarning([Values(true, false)] bool legacy)
        {
            var parent = World.CreateSystem<TestGroup>();
            parent.UseLegacySortOrder = legacy;
            var child = World.CreateSystem<UpdateBeforeSelfSystem>();
            parent.AddSystemToUpdateList(child);
            parent.SortSystems();
            LogAssert.Expect(LogType.Warning, new Regex(@"Ignoring invalid \[UpdateBefore\].+UpdateBeforeSelfSystem.+cannot be updated before itself."));
        }

        [Test]
        public void ComponentSystemGroup_AddNullToUpdateList_QuietNoOp([Values(true, false)] bool legacy)
        {
            var parent = World.CreateSystem<TestGroup>();
            parent.UseLegacySortOrder = legacy;
            Assert.DoesNotThrow(() => { parent.AddSystemToUpdateList(null); });
            Assert.IsEmpty(parent.Systems);
        }

        [Test]
        public void ComponentSystemGroup_AddSelfToUpdateList_Throws([Values(true, false)] bool legacy)
        {
            var parent = World.CreateSystem<TestGroup>();
            parent.UseLegacySortOrder = legacy;
            Assert.That(() => { parent.AddSystemToUpdateList(parent); },
                Throws.ArgumentException.With.Message.Contains("to its own update list"));
        }

#endif

        class StartAndStopSystemGroup : ComponentSystemGroup
        {
            public List<int> Operations;
            protected override void OnCreate()
            {
                base.OnCreate();
                Operations = new List<int>(6);
            }

            protected override void OnStartRunning()
            {
                Operations.Add(0);
                base.OnStartRunning();
            }

            protected override void OnUpdate()
            {
                Operations.Add(1);
                base.OnUpdate();
            }

            protected override void OnStopRunning()
            {
                Operations.Add(2);
                base.OnStopRunning();
            }
        }

        class StartAndStopSystemA : ComponentSystem
        {
            private StartAndStopSystemGroup Group;
            protected override void OnCreate()
            {
                base.OnCreate();
                Group = World.GetExistingSystem<StartAndStopSystemGroup>();
            }

            protected override void OnStartRunning()
            {
                Group.Operations.Add(10);
                base.OnStartRunning();
            }

            protected override void OnUpdate()
            {
                Group.Operations.Add(11);
            }

            protected override void OnStopRunning()
            {
                Group.Operations.Add(12);
                base.OnStopRunning();
            }
        }
        class StartAndStopSystemB : ComponentSystem
        {
            private StartAndStopSystemGroup Group;
            protected override void OnCreate()
            {
                base.OnCreate();
                Group = World.GetExistingSystem<StartAndStopSystemGroup>();
            }

            protected override void OnStartRunning()
            {
                Group.Operations.Add(20);
                base.OnStartRunning();
            }

            protected override void OnUpdate()
            {
                Group.Operations.Add(21);
            }

            protected override void OnStopRunning()
            {
                Group.Operations.Add(22);
                base.OnStopRunning();
            }
        }
        class StartAndStopSystemC : ComponentSystem
        {
            private StartAndStopSystemGroup Group;
            protected override void OnCreate()
            {
                base.OnCreate();
                Group = World.GetExistingSystem<StartAndStopSystemGroup>();
            }

            protected override void OnStartRunning()
            {
                Group.Operations.Add(30);
                base.OnStartRunning();
            }

            protected override void OnUpdate()
            {
                Group.Operations.Add(31);
            }

            protected override void OnStopRunning()
            {
                Group.Operations.Add(32);
                base.OnStopRunning();
            }
        }

        [Test]
        public void ComponentSystemGroup_OnStartRunningOnStopRunning_Recurses([Values(true, false)] bool legacy)
        {
            var parent = World.CreateSystem<StartAndStopSystemGroup>();
            parent.UseLegacySortOrder = legacy;
            var childA = World.CreateSystem<StartAndStopSystemA>();
            var childB = World.CreateSystem<StartAndStopSystemB>();
            var childC = World.CreateSystem<StartAndStopSystemC>();
            parent.AddSystemToUpdateList(childA);
            parent.AddSystemToUpdateList(childB);
            parent.AddSystemToUpdateList(childC);
            // child C is always disabled; make sure enabling/disabling the parent doesn't change that
            childC.Enabled = false;

            // first update
            parent.Update();
            CollectionAssert.AreEqual(parent.Operations, new[] {0, 1, 10, 11, 20, 21});
            parent.Operations.Clear();

            // second update with no new enabled/disabled
            parent.Update();
            CollectionAssert.AreEqual(parent.Operations, new[] {1, 11, 21});
            parent.Operations.Clear();

            // parent is disabled
            parent.Enabled = false;
            parent.Update();
            CollectionAssert.AreEqual(parent.Operations, new[] {2, 12, 22});
            parent.Operations.Clear();

            // parent is re-enabled
            parent.Enabled = true;
            parent.Update();
            CollectionAssert.AreEqual(parent.Operations, new[] {0, 1, 10, 11, 20, 21});
            parent.Operations.Clear();
        }

        class TrackUpdatedSystem : JobComponentSystem
        {
            public List<ComponentSystemBase> Updated;

            protected override JobHandle OnUpdate(JobHandle inputDeps)
            {
                Updated.Add(this);
                return inputDeps;
            }
        }

        [Test]
        public void AddAndRemoveTakesEffectBeforeUpdate([Values(true, false)] bool legacy)
        {
            var parent = World.CreateSystem<TestGroup>();
            parent.UseLegacySortOrder = legacy;
            var childa = World.CreateSystem<TrackUpdatedSystem>();
            var childb = World.CreateSystem<TrackUpdatedSystem>();

            var updates = new List<ComponentSystemBase>();
            childa.Updated = updates;
            childb.Updated = updates;

            // Add 2 systems & validate Update calls
            parent.AddSystemToUpdateList(childa);
            parent.AddSystemToUpdateList(childb);
            parent.Update();

            // Order is not guaranteed
            Assert.IsTrue(updates.Count == 2 && updates.Contains(childa) && updates.Contains(childb));

            // Remove system & validate Update calls
            updates.Clear();
            parent.RemoveSystemFromUpdateList(childa);
            parent.Update();
            Assert.AreEqual(new ComponentSystemBase[] {childb}, updates.ToArray());
        }

        [UpdateInGroup(typeof(TestGroup), OrderFirst = true)]
        public class OFL_A : EmptySystem
        {
        }

        [UpdateInGroup(typeof(TestGroup), OrderFirst = true)]
        public class OFL_B : EmptySystem
        {
        }

        public class OFL_C : EmptySystem
        {
        }

        [UpdateInGroup(typeof(TestGroup), OrderLast = true)]
        public class OFL_D : EmptySystem
        {
        }

        [UpdateInGroup(typeof(TestGroup), OrderLast = true)]
        public class OFL_E : EmptySystem
        {
        }

#if !NET_DOTS
        [Test]
        public void OrderFirstLastWorks([Values(0, 1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12, 13, 14, 15, 16, 17, 18, 19, 20, 21, 22, 23, 24, 25, 26, 27, 30, 31)] int bits, [Values(true, false)] bool legacyMode)
        {
            var parent = World.CreateSystem<TestGroup>();
            parent.UseLegacySortOrder = legacyMode;

            // Add in reverse prder
            if (0 != (bits & (1 << 4))) { parent.AddSystemToUpdateList(World.CreateSystem<OFL_E>()); }
            if (0 != (bits & (1 << 3))) { parent.AddSystemToUpdateList(World.CreateSystem<OFL_D>()); }
            if (0 != (bits & (1 << 2))) { parent.AddSystemToUpdateList(World.CreateSystem<OFL_C>()); }
            if (0 != (bits & (1 << 1))) { parent.AddSystemToUpdateList(World.CreateSystem<OFL_B>()); }
            if (0 != (bits & (1 << 0))) { parent.AddSystemToUpdateList(World.CreateSystem<OFL_A>()); }

            parent.SortSystems();

            // Ensure they are always in alphabetical order
            string prev = null;
            foreach (var sys in parent.Systems)
            {
                var curr = sys.GetType().Name;
                Assert.IsTrue(prev == null || prev.CompareTo(curr) < 0);
                prev = curr;
            }
        }

        [UpdateAfter(typeof(TestSystem))]
        struct MyUnmanagedSystem : ISystemBase
        {
            public void OnCreate(ref SystemState state)
            {
            }

            public void OnDestroy(ref SystemState state)
            {
            }

            public void OnUpdate(ref SystemState state)
            {
            }
        }

        [Test]
        public void LegacySortDoesNotWorkWithUnmanagedSystems()
        {
            var parent = World.CreateSystem<TestGroup>();
            parent.UseLegacySortOrder = true;

            var sys = World.AddSystem<MyUnmanagedSystem>();
            Assert.Throws<InvalidOperationException>(() => parent.AddSystemToUpdateList(sys));
        }

        [Test]
        public void NewSortWorksWithBoth()
        {
            var parent = World.CreateSystem<TestGroup>();
            parent.UseLegacySortOrder = false;
            var sys = World.AddSystem<MyUnmanagedSystem>();
            var s1 = World.GetOrCreateSystem<TestSystem>();

            parent.AddSystemToUpdateList(sys);
            parent.AddSystemToUpdateList(s1);

            parent.SortSystems();

#pragma warning disable 618
            Assert.Throws<InvalidOperationException>(() => parent.SortSystemUpdateList());
#pragma warning restore 618
        }
#endif
    }
}

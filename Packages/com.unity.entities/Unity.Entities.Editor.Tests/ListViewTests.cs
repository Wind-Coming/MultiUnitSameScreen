using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Entities.Tests;
using UnityEditor.IMGUI.Controls;

namespace Unity.Entities.Editor.Tests
{
    class ListViewTests : ECSTestsFixture
    {
        private static void SetEntitySelection(Entity s)
        {
        }

        private World GetWorldSelection()
        {
            return World;
        }

        private static void SetComponentGroupSelection(EntityListQuery query)
        {
        }

        private static void SetSystemSelection(ComponentSystemBase system, World world)
        {
        }

        private EntityListQuery AllQuery => new EntityListQuery(new EntityQueryDesc(){All = new ComponentType[0], Any = new ComponentType[0], None = new ComponentType[0]});

        private World World2;

        public override void Setup()
        {
            base.Setup();

            ScriptBehaviourUpdateOrder.UpdatePlayerLoop(World.DefaultGameObjectInjectionWorld);

            World2 = new World("Test World 2");
            var emptySys = World2.GetOrCreateSystem<EmptySystem>();
            var simGroup = World.GetOrCreateSystem<SimulationSystemGroup>();
            simGroup.AddSystemToUpdateList(emptySys);
            simGroup.SortSystems();
        }

        public override void TearDown()
        {
            World2.Dispose();
            World2 = null;

            base.TearDown();

            ScriptBehaviourUpdateOrder.UpdatePlayerLoop(m_PreviousWorld);
        }

        [Test]
        public void EntityListView_ShowNothingWithoutWorld()
        {
            m_Manager.CreateEntity();
            var emptySystem = World.GetOrCreateSystem<EmptySystem>();
            ComponentSystemBase currentSystem = null;

            using (var listView = new EntityListView(new TreeViewState(), null, SetEntitySelection, () => null,
                () => currentSystem, x => {}))
            {
                currentSystem = emptySystem;
                listView.SelectedEntityQuery = null;
                Assert.IsFalse(listView.ShowingSomething);

                currentSystem = null;
                listView.SelectedEntityQuery = null;
                Assert.IsFalse(listView.ShowingSomething);

                currentSystem = null;
                listView.SelectedEntityQuery = AllQuery;
                Assert.IsFalse(listView.ShowingSomething);
            }
        }

        [Test]
        public void EntityListView_ShowEntitiesFromWorld()
        {
            m_Manager.CreateEntity();
            var emptySystem = World.GetOrCreateSystem<EmptySystem>();
            var selectedWorld = World;
            ComponentSystemBase currentSystem = null;

            using (var listView = new EntityListView(new TreeViewState(), null, SetEntitySelection, () => selectedWorld,
                () => currentSystem, x => {}))
            {
                // TODO EntityManager is no longer a system
                /*
                currentSystem = World.Active.EntityManager;
                listView.SelectedEntityQuery = AllQuery;
                Assert.IsTrue(listView.ShowingSomething);
                Assert.AreEqual(1, listView.GetRows().Count);

                currentSystem = World.Active.EntityManager;
                listView.SelectedEntityQuery = null;
                Assert.IsTrue(listView.ShowingSomething);
                Assert.AreEqual(1, listView.GetRows().Count);
                */

                currentSystem = emptySystem;
                listView.SelectedEntityQuery = null;
                Assert.IsFalse(listView.ShowingSomething);
            }
        }

        [Test]
        public void EntityListView_ShowNothingWithNoEntityManager()
        {
            using (var incompleteWorld = new World("test 2"))
            {
                using (var listView = new EntityListView(new TreeViewState(), null, SetEntitySelection, () => incompleteWorld,
                    () => null, x => {}))
                {
                    listView.SelectedEntityQuery = null;
                    Assert.AreEqual(0, listView.GetRows().Count);
                }
            }
        }

        [Test]
        public void ComponentGroupListView_CanSetNullSystem()
        {
            var listView = new EntityQueryListView(new TreeViewState(), EmptySystem, SetComponentGroupSelection, GetWorldSelection);

            Assert.DoesNotThrow(() => listView.SelectedSystem = null);
        }

        [Test]
        public void ComponentGroupListView_SortOrderExpected()
        {
            var typeList = new List<ComponentType>();
            var subtractive = ComponentType.Exclude<EcsTestData>();
            var readWrite = ComponentType.ReadWrite<EcsTestData2>();
            var readOnly = ComponentType.ReadOnly<EcsTestData3>();

            typeList.Add(subtractive);
            typeList.Add(readOnly);
            typeList.Add(readWrite);
            typeList.Sort(EntityQueryGUI.CompareTypes);

            Assert.AreEqual(readOnly, typeList[0]);
            Assert.AreEqual(readWrite, typeList[1]);
            Assert.AreEqual(subtractive, typeList[2]);
        }

        [Test]
        public void SystemListView_CanCreateWithNullWorld()
        {
            SystemListView listView;
            var states = new List<TreeViewState>();
            var stateNames = new List<string>();
            Assert.DoesNotThrow(() =>
            {
                listView = SystemListView.CreateList(states, stateNames, SetSystemSelection, () => null, () => true);
                listView.Reload();
            });
        }

        [Test]
        public void SystemListView_ShowExactlyWorldSystems()
        {
            var listView = new SystemListView(
                new TreeViewState(),
                new MultiColumnHeader(SystemListView.GetHeaderState()),
                (system, world) => {},
                () => World2,
                () => true);
            var systemItems = listView.GetRows().Where(x => listView.systemsById.ContainsKey(x.id)).Select(x => listView.systemsById[x.id]);
            var systemList = systemItems.ToList();

            var world2Systems = new List<ComponentSystemBase>();
            foreach (var system in World2.Systems)
            {
                world2Systems.Add(system);
            }

            Assert.That(world2Systems, Is.EquivalentTo(systemList.Intersect(world2Systems)));
        }

        [Test]
        public void SystemListView_NullWorldShowsAllSystems()
        {
            var listView = new SystemListView(
                new TreeViewState(),
                new MultiColumnHeader(SystemListView.GetHeaderState()),
                (system, world) => {},
                () => null,
                () => true);
            var systemItems = listView.GetRows().Where(x => listView.systemsById.ContainsKey(x.id)).Select(x => listView.systemsById[x.id]);
            var allSystems = new List<ComponentSystemBase>();
            foreach (var system in World.Systems)
            {
                allSystems.Add(system);
            }
            foreach (var system in World2.Systems)
            {
                allSystems.Add(system);
            }
            var systemList = systemItems.ToList();
            Assert.AreEqual(allSystems.Count(x => !(x is ComponentSystemGroup)), allSystems.Intersect(systemList).Count());
        }
    }
}

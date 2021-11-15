using System;
using Unity.Collections;
using NUnit.Framework;
using System.Collections.Generic;

namespace Unity.Entities.Tests
{
    public struct Issue149Data : IComponentData
    {
        public int a;
        public int b;
    }

    public struct Issue476Data : IComponentData
    {
        public int a;
        public int b;
    }

    class Bug149 : ECSTestsFixture
    {
        EntityArchetype m_Archetype;

        const int kBatchCount = 512;

        class EntityBag
        {
            public NativeArray<Entity> Entities;
            public int ValidVersion;
        }

        EntityBag[] Bags = new EntityBag[3];

        [Test]
        public void TestIssue149()
        {
            m_OurTypes = new ComponentType[] {typeof(Issue149Data)};

            m_Archetype = m_Manager.CreateArchetype(typeof(Issue149Data));

            for (int i = 0; i < Bags.Length; ++i)
            {
                Bags[i] = new EntityBag();
            }

            var a = Bags[0];
            var b = Bags[1];
            var c = Bags[2];

            try
            {
                RecycleEntities(a);
                RecycleEntities(b);
                RecycleEntities(c);
                RecycleEntities(a);
                RecycleEntities(b);
                RecycleEntities(a);
                RecycleEntities(c);
                RecycleEntities(a);
                RecycleEntities(a);
                RecycleEntities(b);
                RecycleEntities(a);
                RecycleEntities(c);
                RecycleEntities(a);
            }
            finally
            {
                // To get rid of leak errors in the log when the test fails.
                a.Entities.Dispose();
                b.Entities.Dispose();
                c.Entities.Dispose();
            }
        }

        void RecycleEntities(EntityBag bag)
        {
            if (bag.Entities.Length > 0)
            {
                m_Manager.DestroyEntity(bag.Entities);
                bag.Entities.Dispose();
            }

            bag.ValidVersion++;

            // Sanity check all arrays.
            SanityCheckVersions();

            bag.Entities = new NativeArray<Entity>(kBatchCount, Allocator.Persistent);

            for (int i = 0; i < bag.Entities.Length; ++i)
            {
                bag.Entities[i] = m_Manager.CreateEntity(m_Archetype);
            }
        }

        private ComponentType[] m_OurTypes;

        // Walk all accessible entity data and check that the versions match what we
        // believe the generation numbers should be.
        private void SanityCheckVersions()
        {
            var group = m_Manager.CreateEntityQuery(new EntityQueryDesc
            {
                Any = Array.Empty<ComponentType>(),
                None = Array.Empty<ComponentType>(),
                All = m_OurTypes,
            });
            var chunks = group.CreateArchetypeChunkArray(Allocator.TempJob);
            group.Dispose();

            ArchetypeChunkEntityType entityType = m_Manager.GetArchetypeChunkEntityType();

            for (int i = 0; i < chunks.Length; ++i)
            {
                ArchetypeChunk chunk = chunks[i];
                var entitiesInChunk = chunk.GetNativeArray(entityType);

                for (int k = 0; k < chunk.Count; ++k)
                {
                    Entity e = entitiesInChunk[k];
                    int index = e.Index;
                    int version = e.Version;

                    int ourArray = index / kBatchCount;
                    int ourVersion = Bags[ourArray].ValidVersion;

                    Assert.IsTrue(ourVersion == version);
                }
            }

            chunks.Dispose();
        }
    }

    class Bug1294 : ECSTestsFixture
    {
        [Test]
        public unsafe void EntityInChunkCompareTo_Low32BitsMatch_ComparesCorrectly()
        {
            // Create a valid Entity and get its EntityInChunk:
            var ent1 = m_Manager.CreateEntity(typeof(EcsTestData));
            var eic1 = m_Manager.GetCheckedEntityDataAccess()->EntityComponentStore->GetEntityInChunk(ent1);
            // Construct an artificial EntityInChunk for a hypothetical entity in a hypothetical
            // Chunk, exactly 2^32 bytes higher than ent1's (such that comparing only 32 bits of
            // the chunk pointer would fail):
            var eic2 = new EntityInChunk
            {
                Chunk = (Chunk*)((ulong)eic1.Chunk + (1ul << 32)),
                IndexInChunk = eic1.IndexInChunk,
            };
            Assert.Greater((ulong)eic2.Chunk, (ulong)eic1.Chunk);
            Assert.AreEqual((int)eic2.Chunk, (int)eic1.Chunk);
            // Make sure the EntityInChunks sort correctly (eic2 > eic1):
            Assert.Greater(eic2.CompareTo(eic1), 0);
            Assert.Less(eic1.CompareTo(eic2), 0);
        }

        [Test]
        public unsafe void EntityInChunkCompareTo_DifferenceOverflows32Bits_ComparesCorrectly()
        {
            // Create a valid Entity and get its EntityInChunk:
            var ent1 = m_Manager.CreateEntity(typeof(EcsTestData));
            var eic1 = m_Manager.GetCheckedEntityDataAccess()->EntityComponentStore->GetEntityInChunk(ent1);
            // Construct an artificial EntityInChunk for a hypothetical entity in a hypothetical
            // Chunk, whose pointer is just above 2^31 larger than ent1's (such that using lhs-rhs as the
            // CompareTo result would overflow a signed int and give an incorrect result):
            var eic2 = new EntityInChunk
            {
                Chunk = (Chunk*)((ulong)eic1.Chunk + (1ul << 31) + 65536),
                IndexInChunk = eic1.IndexInChunk,
            };
            Assert.Greater((ulong)eic2.Chunk, (ulong)eic1.Chunk);
            Assert.Less((int)((ulong)eic2.Chunk - (ulong)eic1.Chunk), 0);
            // Make sure the EntityInChunks sort correctly (eic2 > eic1):
            Assert.Greater(eic2.CompareTo(eic1), 0);
            Assert.Less(eic1.CompareTo(eic2), 0);
        }
    }

    class Bug476 : ECSTestsFixture
    {
        [Test]
        public void EntityArchetypeQueryMembersHaveSensibleDefaults()
        {
            ComponentType[] types = {typeof(Issue476Data)};
            var group = m_Manager.CreateEntityQuery(types);
            var temp = group.CreateArchetypeChunkArray(Allocator.TempJob);
            group.Dispose();
            temp.Dispose();
        }
    }

    class Bug148 : ECSTestsFixture
    {
        [Test]
        public void Test1()
        {
            World w = new World("TestWorld");
            World.DefaultGameObjectInjectionWorld = w;
            EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;
            List<Entity> remember = new List<Entity>();
            for (int i = 0; i < 5; i++)
            {
                remember.Add(em.CreateEntity());
            }

            var allEnt = em.GetAllEntities(Allocator.Temp);
            allEnt.Dispose();
            foreach (Entity e in remember)
            {
                Assert.IsTrue(em.Exists(e));
            }

            foreach (Entity e in remember)
            {
                em.DestroyEntity(e);
            }

            w.Dispose();
        }

        [Test]
        public void Test2()
        {
            World w = new World("TestWorld");
            World.DefaultGameObjectInjectionWorld = w;
            EntityManager em = World.DefaultGameObjectInjectionWorld.EntityManager;

            List<Entity> remember = new List<Entity>();
            for (int i = 0; i < 5; i++)
            {
                remember.Add(em.CreateEntity());
            }

            w.Dispose();
            w = null;

            w = new World("TestWorld2");
            World.DefaultGameObjectInjectionWorld = w;
            em = World.DefaultGameObjectInjectionWorld.EntityManager;
            var allEnt = em.GetAllEntities(Allocator.Temp);
            Assert.AreEqual(0, allEnt.Length);
            allEnt.Dispose();

            foreach (Entity e in remember)
            {
                bool exists = em.Exists(e);
                Assert.IsFalse(exists);
            }

            foreach (Entity e in remember)
            {
                if (em.Exists(e))
                {
                    em.DestroyEntity(e);
                }
            }

            w.Dispose();
        }
    }
}

using System;
using NUnit.Framework;
using Unity.Collections;
using Unity.Entities;
using Unity.Entities.Tests;
using Unity.Scenes;
using UnityEditor;
using UnityEngine;

public class PatchingEndToEndWithSerialization : EntityDifferTestFixture
{
    public struct ComponentWithObjectRef : ISharedComponentData, IEquatable<ComponentWithObjectRef>
    {
        public Material Material;

        public bool Equals(ComponentWithObjectRef other)
        {
            return Material == other.Material;
        }

        public override int GetHashCode()
        {
            return ReferenceEquals(Material, null) ? 0 : Material.GetHashCode();
        }
    }

    static LiveLinkChangeSet SerializeAndDeserialize(LiveLinkChangeSet srcChange)
    {
        var resolver = new GlobalAssetObjectResolver();

        var bytes = srcChange.Serialize();

        // Serialize Changes
        var resourcePacket = new EntityChangeSetSerialization.ResourcePacket(bytes);
        foreach (var asset in resourcePacket.GlobalObjectIds)
        {
            var manifest = ScriptableObject.CreateInstance<AssetObjectManifest>();
            AssetObjectManifestBuilder.BuildManifest(asset.AssetGUID, manifest);

            resolver.AddAsset(asset.AssetGUID, new Unity.Entities.Hash128(), manifest, null);
        }

        // Deserialize
        LiveLinkChangeSet changeSet;
        try
        {
            changeSet = LiveLinkChangeSet.Deserialize(resourcePacket, resolver);
        }
        finally
        {
            resourcePacket.Dispose();
            resolver.DisposeObjectManifests();
        }

        return changeSet;
    }

    static void AssertChangeSetsAreEqual(LiveLinkChangeSet srcChange, LiveLinkChangeSet dstChange)
    {
        Assert.AreEqual(srcChange.SceneName, dstChange.SceneName);
        Assert.AreEqual(srcChange.UnloadAllPreviousEntities, dstChange.UnloadAllPreviousEntities);
        Assert.AreEqual(srcChange.SceneGUID, dstChange.SceneGUID);

        Assert.AreEqual(srcChange.Changes.DestroyedEntityCount, dstChange.Changes.DestroyedEntityCount);
        Assert.AreEqual(srcChange.Changes.CreatedEntityCount, dstChange.Changes.CreatedEntityCount);
        Assert.AreEqual(srcChange.Changes.AddComponents.Length, dstChange.Changes.AddComponents.Length);
        Assert.AreEqual(srcChange.Changes.SetComponents.Length, dstChange.Changes.SetComponents.Length);
        Assert.AreEqual(srcChange.Changes.SetSharedComponents.Length, dstChange.Changes.SetSharedComponents.Length);
        Assert.AreEqual(srcChange.Changes.SetManagedComponents.Length, dstChange.Changes.SetManagedComponents.Length);
    }

    [Test]
    public void PatchWithObjectReferenceResolving()
    {
        var material = AssetDatabase.LoadAssetAtPath<Material>("Packages/com.unity.entities/Unity.Scenes.Hybrid.Tests/Test.mat");

        using (var differ = new EntityManagerDiffer(SrcWorld.EntityManager, Allocator.TempJob))
        {
            var entityGuid = CreateEntityGuid();

            var entity = SrcEntityManager.CreateEntity();
            SrcEntityManager.AddComponentData(entity, entityGuid);
            SrcEntityManager.AddSharedComponentData(entity, new ComponentWithObjectRef {Material = material});
            SrcEntityManager.AddComponentData(entity, new EcsTestData(5));

            var options = EntityManagerDifferOptions.IncludeForwardChangeSet | EntityManagerDifferOptions.FastForwardShadowWorld;

            using (var changes = differ.GetChanges(options, Allocator.TempJob))
            {
                var srcChange = new LiveLinkChangeSet
                {
                    Changes = changes.ForwardChangeSet,
                    SceneName = "Boing",
                    UnloadAllPreviousEntities = true,
                    SceneGUID = new Unity.Entities.Hash128(),
                    FramesToRetainBlobAssets = 1
                };

                var changeSet = SerializeAndDeserialize(srcChange);
                AssertChangeSetsAreEqual(srcChange, changeSet);

                EntityPatcher.ApplyChangeSet(DstEntityManager, changeSet.Changes);

                changeSet.Dispose();
            }

            var dstMaterial = GetSharedComponentData<ComponentWithObjectRef>(DstEntityManager, entityGuid).Material;
            Assert.AreEqual(material, dstMaterial);
            Assert.AreEqual(5, GetComponentData<EcsTestData>(DstEntityManager, entityGuid).value);
        }
    }

    [Test]
    public void DeserializeWithDifferentDataLayoutThrows()
    {
        using (var differ = new EntityManagerDiffer(SrcWorld.EntityManager, Allocator.TempJob))
        {
            var entity = SrcEntityManager.CreateEntity();
            SrcEntityManager.AddComponentData(entity, CreateEntityGuid());
            SrcEntityManager.AddComponentData(entity, new EcsTestData(5));

            var options = EntityManagerDifferOptions.IncludeForwardChangeSet | EntityManagerDifferOptions.FastForwardShadowWorld;

            using (var changes = differ.GetChanges(options, Allocator.TempJob))
            {
                var typeHashes = changes.ForwardChangeSet.TypeHashes;
                var modifiedTypeHash = typeHashes[0];
                modifiedTypeHash.StableTypeHash += 1;
                typeHashes[0] = modifiedTypeHash;

                var srcChange = new LiveLinkChangeSet
                {
                    Changes = changes.ForwardChangeSet
                };

                Assert.Throws<ArgumentException>(() => SerializeAndDeserialize(srcChange));
            }
        }
    }

#if !UNITY_DISABLE_MANAGED_COMPONENTS
    [Test]
    public void CanSerializeAndDeserializeManagedComponents()
    {
        using (var differ = new EntityManagerDiffer(SrcWorld.EntityManager, Allocator.TempJob))
        {
            var entityGuid = CreateEntityGuid();
            var entity = SrcEntityManager.CreateEntity();

            var srcManagedComponent = new EcsTestManagedDataEntity
            {
                value0 = "Hello",
                value2 = 42,
                nullField = new ClassWithClassFields
                {
                    ClassWithString = new ClassWithString
                    {
                        String = "World"
                    }
                }
            };

            SrcEntityManager.AddComponentData(entity, entityGuid);
            SrcEntityManager.AddComponentData(entity, srcManagedComponent);
            SrcEntityManager.AddComponentData(entity, (EcsTestManagedComponent2)null);

            var options = EntityManagerDifferOptions.IncludeForwardChangeSet | EntityManagerDifferOptions.FastForwardShadowWorld;

            using (var changes = differ.GetChanges(options, Allocator.TempJob))
            {
                var srcChange = new LiveLinkChangeSet
                {
                    Changes = changes.ForwardChangeSet,
                    SceneName = "Boing",
                    UnloadAllPreviousEntities = true,
                    SceneGUID = new Unity.Entities.Hash128(),
                    FramesToRetainBlobAssets = 1
                };

                var changeSet = SerializeAndDeserialize(srcChange);
                AssertChangeSetsAreEqual(srcChange, changeSet);
                EntityPatcher.ApplyChangeSet(DstEntityManager, changeSet.Changes);
                changeSet.Dispose();
            }

            var dstManagedComponent = GetManagedComponentData<EcsTestManagedDataEntity>(DstEntityManager, entityGuid);

            Assert.That(dstManagedComponent, Is.Not.Null);
            Assert.That(dstManagedComponent, Is.Not.SameAs(srcManagedComponent));
            Assert.That(dstManagedComponent.value0, Is.EqualTo(srcManagedComponent.value0));
            Assert.That(dstManagedComponent.value1, Is.EqualTo(srcManagedComponent.value1));
            Assert.That(dstManagedComponent.value2, Is.EqualTo(srcManagedComponent.value2));
            Assert.That(dstManagedComponent.nullField, Is.Not.SameAs(srcManagedComponent.nullField));
            Assert.That(dstManagedComponent.nullField.ClassWithString, Is.Not.SameAs(srcManagedComponent.nullField.ClassWithString));
            Assert.That(dstManagedComponent.nullField.ClassWithString.String, Is.EqualTo(srcManagedComponent.nullField.ClassWithString.String));

            Assert.That(GetManagedComponentData<EcsTestManagedComponent2>(DstEntityManager, entityGuid), Is.Null);
        }
    }

#endif
}

using System;
using System.Runtime.CompilerServices;
using UnityEngine;

// Temporarily needed to upgrade RenderMesh so it adds local to world
[assembly: InternalsVisibleTo("Unity.Rendering.Hybrid")]

namespace Unity.Entities
{
    sealed class WrappedComponentDataAttribute : PropertyAttribute
    {
    }


    [ExecuteAlways]
    [RequireComponent(typeof(GameObjectEntity))]
    [Obsolete("ComponentDataProxyBase has been deprecated. Please use the new GameObject-to-entity conversion workflows instead. (RemovedAfter 2020-07-03).")]
    public abstract class ComponentDataProxyBase : MonoBehaviour, ISerializationCallbackReceiver
    {
        internal abstract ComponentType GetComponentType();
        internal abstract void UpdateComponentData(EntityManager manager, Entity entity);
        internal abstract void UpdateSerializedData(EntityManager manager, Entity entity);

        internal abstract int InsertSharedComponent(EntityManager manager);
        internal abstract void UpdateSerializedData(EntityManager manager, int sharedComponentIndex);

        internal abstract void ValidateSerializedData();

        protected virtual void OnEnable()
        {
            EntityManager entityManager;
            Entity entity;
            if (
                World.DefaultGameObjectInjectionWorld != null
                && TryGetEntityAndManager(out entityManager, out entity)
                && !entityManager.HasComponent(entity, GetComponentType()) // in case GameObjectEntity already added
            )
                entityManager.AddComponent(entity, GetComponentType());
        }

        protected virtual void OnDisable()
        {
            if (!gameObject.activeInHierarchy) // GameObjectEntity will handle removal when Entity is destroyed
                return;
            EntityManager entityManager;
            Entity entity;
            if (CanSynchronizeWithEntityManager(out entityManager, out entity))
                entityManager.RemoveComponent(entity, GetComponentType());
        }

        internal bool TryGetEntityAndManager(out EntityManager entityManager, out Entity entity)
        {
            entityManager = default;
            entity = Entity.Null;
            // gameObject is not initialized yet in native when OnBeforeSerialized() is called via SmartReset()
            if (gameObject == null)
                return false;
            var gameObjectEntity = GetComponent<GameObjectEntity>();
            if (gameObjectEntity == null)
                return false;
            var world = gameObjectEntity.World;
            if (world == null || !world.IsCreated)
                return false;

            var em = world.EntityManager;
            if (!em.Exists(gameObjectEntity.Entity))
                return false;
            entityManager = em;
            entity = gameObjectEntity.Entity;
            return true;
        }

        internal bool CanSynchronizeWithEntityManager(out EntityManager entityManager, out Entity entity)
        {
            return TryGetEntityAndManager(out entityManager, out entity)
                && entityManager.HasComponent(entity, GetComponentType());
        }

        void OnValidate()
        {
            ValidateSerializedData();
            EntityManager entityManager;
            Entity entity;
            if (CanSynchronizeWithEntityManager(out entityManager, out entity))
                UpdateComponentData(entityManager, entity);
        }

        void Reset()
        {
            OnValidate();
        }

        void ISerializationCallbackReceiver.OnBeforeSerialize()
        {
            EntityManager entityManager;
            Entity entity;
            if (CanSynchronizeWithEntityManager(out entityManager, out entity))
                UpdateSerializedData(entityManager, entity);
        }

        void ISerializationCallbackReceiver.OnAfterDeserialize() {}
    }
}

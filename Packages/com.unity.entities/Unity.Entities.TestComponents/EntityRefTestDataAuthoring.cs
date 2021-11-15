using System.Collections.Generic;
using Unity.Entities;
using Unity.Entities.Conversion;
using UnityEngine;

namespace Unity.Entities.Tests
{
    public struct EntityRefTestData : IComponentData
    {
        public Entity Value;

        public EntityRefTestData(Entity value) => Value = value;

        public override string ToString() => Value.ToString();
    }

    [AddComponentMenu("")]
    [ConverterVersion("joe", 1)]
    public class EntityRefTestDataAuthoring : MonoBehaviour, IConvertGameObjectToEntity, IDeclareReferencedPrefabs
    {
        public GameObject Value;
        public int        AdditionalEntityCount;
        public bool       DeclareLinkedEntityGroup;

        public void Convert(Entity entity, EntityManager dstManager, GameObjectConversionSystem conversionSystem)
        {
            dstManager.AddComponentData(entity, new EntityRefTestData {Value = conversionSystem.GetPrimaryEntity(Value)});

            for (int i = 0; i != AdditionalEntityCount; i++)
            {
                var additional = conversionSystem.CreateAdditionalEntity(this);
                dstManager.AddComponentData(additional, new EntityRefTestData {Value = conversionSystem.GetPrimaryEntity(Value)});
            }

            if (DeclareLinkedEntityGroup)
                conversionSystem.DeclareLinkedEntityGroup(gameObject);
        }

        public void DeclareReferencedPrefabs(List<GameObject> referencedPrefabs)
        {
            if (Value != null && Value.IsPrefab())
                referencedPrefabs.Add(Value);
        }

        // Empty Update function makes it so that unity shows the UI for the checkbox.
        // We use it for testing stripping of components.
        // ReSharper disable once Unity.RedundantEventFunction
        void Update() {}
    }
}

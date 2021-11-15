using System;
using System.Collections.Generic;
using Unity.Collections;

namespace Unity.Entities
{
    public unsafe partial struct EntityManager
    {
        // ----------------------------------------------------------------------------------------------------------
        // PUBLIC
        // ----------------------------------------------------------------------------------------------------------

        /// <summary>
        /// Gets the dynamic type object required to access a chunk component of type T.
        /// </summary>
        /// <remarks>
        /// To access a component stored in a chunk, you must have the type registry information for the component.
        /// This function provides that information. Use the returned <see cref="ArchetypeChunkComponentType{T}"/>
        /// object with the functions of an <see cref="ArchetypeChunk"/> object to get information about the components
        /// in that chunk and to access the component values.
        /// </remarks>
        /// <param name="isReadOnly">Specify whether the access to the component through this object is read only
        /// or read and write. For managed components isReadonly will always be treated as false.</param>
        /// <typeparam name="T">The compile-time type of the component.</typeparam>
        /// <returns>The run-time type information of the component.</returns>
        public ArchetypeChunkComponentType<T> GetArchetypeChunkComponentType<T>(bool isReadOnly)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var access = GetCheckedEntityDataAccess();
            var typeIndex = TypeManager.GetTypeIndex<T>();
            return new ArchetypeChunkComponentType<T>(
                access->DependencyManager->Safety.GetSafetyHandleForArchetypeChunkComponentType(typeIndex, isReadOnly), isReadOnly,
                GlobalSystemVersion);
#else
            return new ArchetypeChunkComponentType<T>(isReadOnly, GlobalSystemVersion);
#endif
        }

        /// <summary>
        /// Gets the dynamic type object required to access a chunk component of dynamic type acquired from reflection.
        /// </summary>
        /// <remarks>
        /// To access a component stored in a chunk, you must have the type registry information for the component.
        /// This function provides that information. Use the returned <see cref="ArchetypeChunkComponentTypeDynamic"/>
        /// object with the functions of an <see cref="ArchetypeChunk"/> object to get information about the components
        /// in that chunk and to access the component values.
        /// </remarks>
        /// <param name="componentType">Type of the component</param>
        /// <returns>The run-time type information of the component.</returns>
        public ArchetypeChunkComponentTypeDynamic GetArchetypeChunkComponentTypeDynamic(ComponentType componentType)
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var access = GetCheckedEntityDataAccess();
            return new ArchetypeChunkComponentTypeDynamic(componentType,
                access->DependencyManager->Safety.GetSafetyHandleForArchetypeChunkComponentTypeDynamic(componentType.TypeIndex, componentType.AccessModeType == ComponentType.AccessMode.ReadOnly),
                GlobalSystemVersion);
#else
            return new ArchetypeChunkComponentTypeDynamic(componentType, GlobalSystemVersion);
#endif
        }

        /// <summary>
        /// Gets the dynamic type object required to access a chunk buffer containing elements of type T.
        /// </summary>
        /// <remarks>
        /// To access a component stored in a chunk, you must have the type registry information for the component.
        /// This function provides that information for buffer components. Use the returned
        /// <see cref="ArchetypeChunkComponentType{T}"/> object with the functions of an <see cref="ArchetypeChunk"/>
        /// object to get information about the components in that chunk and to access the component values.
        /// </remarks>
        /// <param name="isReadOnly">Specify whether the access to the component through this object is read only
        /// or read and write. </param>
        /// <typeparam name="T">The compile-time type of the buffer elements.</typeparam>
        /// <returns>The run-time type information of the buffer component.</returns>
        public ArchetypeChunkBufferType<T> GetArchetypeChunkBufferType<T>(bool isReadOnly)
            where T : struct, IBufferElementData
        {
            return GetCheckedEntityDataAccess()->GetArchetypeChunkBufferType<T>(isReadOnly);
        }

        /// <summary>
        /// Gets the dynamic type object required to access a shared component of type T.
        /// </summary>
        /// <remarks>
        /// To access a component stored in a chunk, you must have the type registry information for the component.
        /// This function provides that information for shared components. Use the returned
        /// <see cref="ArchetypeChunkComponentType{T}"/> object with the functions of an <see cref="ArchetypeChunk"/>
        /// object to get information about the components in that chunk and to access the component values.
        /// </remarks>
        /// <typeparam name="T">The compile-time type of the shared component.</typeparam>
        /// <returns>The run-time type information of the shared component.</returns>
        public ArchetypeChunkSharedComponentType<T> GetArchetypeChunkSharedComponentType<T>()
            where T : struct, ISharedComponentData
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var typeIndex = TypeManager.GetTypeIndex<T>();
            var access = GetCheckedEntityDataAccess();
            return new ArchetypeChunkSharedComponentType<T>(access->DependencyManager->Safety.GetSafetyHandleForArchetypeChunkSharedComponentType(typeIndex));
#else
            return new ArchetypeChunkSharedComponentType<T>(false);
#endif
        }

        /// <summary>
        /// Gets the dynamic type object required to access the <see cref="Entity"/> component of a chunk.
        /// </summary>
        /// <remarks>
        /// All chunks have an implicit <see cref="Entity"/> component referring to the entities in that chunk.
        ///
        /// To access any component stored in a chunk, you must have the type registry information for the component.
        /// This function provides that information for the implicit <see cref="Entity"/> component. Use the returned
        /// <see cref="ArchetypeChunkComponentType{T}"/> object with the functions of an <see cref="ArchetypeChunk"/>
        /// object to access the component values.
        /// </remarks>
        /// <returns>The run-time type information of the Entity component.</returns>
        public ArchetypeChunkEntityType GetArchetypeChunkEntityType()
        {
#if ENABLE_UNITY_COLLECTIONS_CHECKS
            var access = GetCheckedEntityDataAccess();
            return new ArchetypeChunkEntityType(
                access->DependencyManager->Safety.GetSafetyHandleForArchetypeChunkEntityType());
#else
            return new ArchetypeChunkEntityType(false);
#endif
        }

        /// <summary>
        /// Gets an entity's component types.
        /// </summary>
        /// <param name="entity">The entity.</param>
        /// <param name="allocator">The type of allocation for creating the NativeArray to hold the ComponentType
        /// objects.</param>
        /// <returns>An array of ComponentType containing all the types of components associated with the entity.</returns>
        public NativeArray<ComponentType> GetComponentTypes(Entity entity, Allocator allocator = Allocator.Temp)
        {
            var access = GetCheckedEntityDataAccess();
            var ecs = access->EntityComponentStore;

            ecs->AssertEntitiesExist(&entity, 1);
            var archetype = new EntityArchetype { Archetype = ecs->GetArchetype(entity) };
            return archetype.GetComponentTypes(allocator);
        }

        /// <summary>
        /// Gets a list of the types of components that can be assigned to the specified component.
        /// </summary>
        /// <remarks>Assignable components include those with the same compile-time type and those that
        /// inherit from the same compile-time type.</remarks>
        /// <param name="interfaceType">The type to check.</param>
        /// <param name="listOut">The list to receive the output.</param>
        /// <returns>The list that was passed in, containing the System.Types that can be assigned to `interfaceType`.</returns>
        public List<Type> GetAssignableComponentTypes(Type interfaceType, List<Type> listOut)
        {
            // #todo Cache this. It only can change when TypeManager.GetTypeCount() changes
            var componentTypeCount = TypeManager.GetTypeCount();
            for (var i = 0; i < componentTypeCount; i++)
            {
                var type = TypeManager.GetType(i);
                if (interfaceType.IsAssignableFrom(type)) listOut.Add(type);
            }

            return listOut;
        }

        /// <summary>
        /// Gets a list of the types of components that can be assigned to the specified component.
        /// </summary>
        /// <remarks>Assignable components include those with the same compile-time type and those that
        /// inherit from the same compile-time type.</remarks>
        /// <param name="interfaceType">The type to check.</param>
        /// <returns>A new List object containing the System.Types that can be assigned to `interfaceType`.</returns>
        public List<Type> GetAssignableComponentTypes(Type interfaceType)
            => GetAssignableComponentTypes(interfaceType, new List<Type>());

        // ----------------------------------------------------------------------------------------------------------
        // INTERNAL
        // ----------------------------------------------------------------------------------------------------------

        internal int GetComponentTypeIndex(Entity entity, int index)
        {
            var access = GetCheckedEntityDataAccess();
            var ecs = access->EntityComponentStore;

            ecs->AssertEntitiesExist(&entity, 1);
            var archetype = ecs->GetArchetype(entity);

            if ((uint)index >= archetype->TypesCount) return -1;

            return archetype->Types[index + 1].TypeIndex;
        }
    }
}

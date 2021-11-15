using System;
using System.ComponentModel;

namespace Unity.Entities
{
    public struct ExcludeComponent<T>
    {
    }

    public partial struct ComponentType : IEquatable<ComponentType>
    {
        public enum AccessMode
        {
            ReadWrite,
            ReadOnly,
            Exclude
        }

        public int TypeIndex;
        public AccessMode AccessModeType;

        public bool IsBuffer => (TypeIndex & TypeManager.BufferComponentTypeFlag) != 0;
        public bool IsSystemStateComponent => (TypeIndex & TypeManager.SystemStateTypeFlag) != 0;
        public bool IsSystemStateSharedComponent => (TypeIndex & TypeManager.SystemStateSharedComponentTypeFlag) == TypeManager.SystemStateSharedComponentTypeFlag;
        public bool IsSharedComponent => (TypeIndex & TypeManager.SharedComponentTypeFlag) != 0;
        public bool IsManagedComponent => TypeManager.IsManagedComponent(TypeIndex);
        public bool IsZeroSized => (TypeIndex & TypeManager.ZeroSizeInChunkTypeFlag) != 0;
        public bool IsChunkComponent => (TypeIndex & TypeManager.ChunkComponentTypeFlag) != 0;
        public bool HasEntityReferences => (TypeIndex & TypeManager.HasNoEntityReferencesFlag) == 0;

        public static ComponentType ReadWrite<T>()
        {
            return FromTypeIndex(TypeManager.GetTypeIndex<T>());
        }

        public static ComponentType ReadWrite(Type type)
        {
            return FromTypeIndex(TypeManager.GetTypeIndex(type));
        }

        public static ComponentType ReadWrite(int typeIndex)
        {
            return FromTypeIndex(typeIndex);
        }

        public static ComponentType FromTypeIndex(int typeIndex)
        {
            ComponentType type;
            type.TypeIndex = typeIndex;
            type.AccessModeType = AccessMode.ReadWrite;
            return type;
        }

        public static ComponentType ReadOnly(Type type)
        {
            ComponentType t = FromTypeIndex(TypeManager.GetTypeIndex(type));
            t.AccessModeType = AccessMode.ReadOnly;
            return t;
        }

        public static ComponentType ReadOnly(int typeIndex)
        {
            ComponentType t = FromTypeIndex(typeIndex);
            t.AccessModeType = AccessMode.ReadOnly;
            return t;
        }

        public static ComponentType ReadOnly<T>()
        {
            ComponentType t = ReadWrite<T>();
            t.AccessModeType = AccessMode.ReadOnly;
            return t;
        }

        public static ComponentType ChunkComponent(Type type)
        {
            var typeIndex = TypeManager.MakeChunkComponentTypeIndex(TypeManager.GetTypeIndex(type));
            return FromTypeIndex(typeIndex);
        }

        public static ComponentType ChunkComponent<T>()
        {
            var typeIndex = TypeManager.MakeChunkComponentTypeIndex(TypeManager.GetTypeIndex<T>());
            return FromTypeIndex(typeIndex);
        }

        public static ComponentType ChunkComponentReadOnly<T>()
        {
            var typeIndex = TypeManager.MakeChunkComponentTypeIndex(TypeManager.GetTypeIndex<T>());
            return ReadOnly(typeIndex);
        }

        public static ComponentType ChunkComponentReadOnly(Type type)
        {
            var typeIndex = TypeManager.MakeChunkComponentTypeIndex(TypeManager.GetTypeIndex(type));
            return ReadOnly(typeIndex);
        }

        public static ComponentType ChunkComponentExclude<T>()
        {
            var typeIndex = TypeManager.MakeChunkComponentTypeIndex(TypeManager.GetTypeIndex<T>());
            return Exclude(typeIndex);
        }

        public static ComponentType ChunkComponentExclude(Type type)
        {
            var typeIndex = TypeManager.MakeChunkComponentTypeIndex(TypeManager.GetTypeIndex(type));
            return Exclude(typeIndex);
        }

        public static ComponentType Exclude(Type type)
        {
            return Exclude(TypeManager.GetTypeIndex(type));
        }

        public static ComponentType Exclude(int typeIndex)
        {
            ComponentType t = FromTypeIndex(typeIndex);
            t.AccessModeType = AccessMode.Exclude;
            return t;
        }

        public static ComponentType Exclude<T>()
        {
            return Exclude(TypeManager.GetTypeIndex<T>());
        }

        public ComponentType(Type type, AccessMode accessModeType = AccessMode.ReadWrite)
        {
            TypeIndex = TypeManager.GetTypeIndex(type);
            AccessModeType = accessModeType;
        }

        public Type GetManagedType()
        {
            return TypeManager.GetType(TypeIndex);
        }

        public static implicit operator ComponentType(Type type)
        {
            return new ComponentType(type, AccessMode.ReadWrite);
        }

        public static bool operator<(ComponentType lhs, ComponentType rhs)
        {
            if (lhs.TypeIndex == rhs.TypeIndex)
                return lhs.AccessModeType < rhs.AccessModeType;

            return lhs.TypeIndex < rhs.TypeIndex;
        }

        public static bool operator>(ComponentType lhs, ComponentType rhs)
        {
            return rhs < lhs;
        }

        public static bool operator==(ComponentType lhs, ComponentType rhs)
        {
            return lhs.TypeIndex == rhs.TypeIndex && lhs.AccessModeType == rhs.AccessModeType;
        }

        public static bool operator!=(ComponentType lhs, ComponentType rhs)
        {
            return lhs.TypeIndex != rhs.TypeIndex || lhs.AccessModeType != rhs.AccessModeType;
        }

        internal static unsafe bool CompareArray(ComponentType* type1, int typeCount1, ComponentType* type2,
            int typeCount2)
        {
            if (typeCount1 != typeCount2)
                return false;
            for (var i = 0; i < typeCount1; ++i)
                if (type1[i] != type2[i])
                    return false;
            return true;
        }

#if ENABLE_UNITY_COLLECTIONS_CHECKS
        public override string ToString()
        {
#if NET_DOTS
            var name = TypeManager.GetTypeInfo(TypeIndex).Debug.TypeName;
#else
            var name = GetManagedType().Name;
            if (IsBuffer)
                return $"{name} [B]";
            if (AccessModeType == AccessMode.Exclude)
                return $"{name} [S]";
            if (AccessModeType == AccessMode.ReadOnly)
                return $"{name} [RO]";
            if (TypeIndex == 0)
                return "None";
#endif
            return name;
        }

#endif

        public bool Equals(ComponentType other)
        {
            return TypeIndex == other.TypeIndex;
        }

        public override bool Equals(object obj)
        {
            return obj is ComponentType && (ComponentType)obj == this;
        }

        public override int GetHashCode()
        {
            return (TypeIndex * 5813);
        }
    }
}

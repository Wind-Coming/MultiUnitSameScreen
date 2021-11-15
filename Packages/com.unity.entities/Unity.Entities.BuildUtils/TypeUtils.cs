#if !UNITY_EDITOR

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using Mono.Cecil;
using Unity.Cecil.Awesome;
using Unity.Cecil.Awesome.Comparers;

namespace Unity.Entities.BuildUtils
{
    public static class TypeUtils
    {
        public struct AlignAndSize
        {
            public readonly int align;
            public readonly int size;
            public readonly int offset;
            public readonly bool empty;

            public AlignAndSize(int single)
            {
                align = size = single;
                offset = 0;
                empty = false;
            }

            public AlignAndSize(int a, int s)
            {
                align = a;
                size = s;
                offset = 0;
                empty = false;
            }

            public AlignAndSize(int a, int s, int o)
            {
                align = a;
                size = s;
                offset = o;
                empty = false;
            }

            public AlignAndSize(int a, int s, int o, bool e)
            {
                align = a;
                size = s;
                offset = o;
                empty = e;
            }

            public static readonly AlignAndSize Zero = new AlignAndSize(0);
            public static readonly AlignAndSize One = new AlignAndSize(1);
            public static readonly AlignAndSize Two = new AlignAndSize(2);
            public static readonly AlignAndSize Four = new AlignAndSize(4);
            public static readonly AlignAndSize Eight = new AlignAndSize(8);
            public static readonly AlignAndSize Pointer2_32 = new AlignAndSize(4, 8);
            public static readonly AlignAndSize Pointer2_64 = new AlignAndSize(8, 16);
            public static readonly AlignAndSize Pointer3_32 = new AlignAndSize(4, 12);
            public static readonly AlignAndSize Pointer3_64 = new AlignAndSize(8, 24);
            public static readonly AlignAndSize Pointer4_32 = new AlignAndSize(4, 16);
            public static readonly AlignAndSize Pointer4_64 = new AlignAndSize(8, 32);
            public static readonly AlignAndSize Sentinel = new AlignAndSize(-1);

            public static AlignAndSize Pointer(int bits) => (bits == 32 || bits == 0) ? Four : Eight;
            public static AlignAndSize DynamicArray(int bits) => (bits == 32 || bits == 0) ? new AlignAndSize(4, 12) : new AlignAndSize(8, 16);
            public static AlignAndSize NativeString(int bits) => (bits == 32 || bits == 0) ? new AlignAndSize(4, 8) : new AlignAndSize(8, 16); // 64-bit has 4 bytes of wasted space to make the alignment work

            public bool IsSentinel => size == -1;

            public override string ToString()
            {
                return String.Format("[{0};{1}]", align, size);
            }
        }

        private static Dictionary<TypeReference, AlignAndSize>[] ValueTypeAlignment =
        {
            new Dictionary<TypeReference, AlignAndSize>(new TypeReferenceEqualityComparer()), new Dictionary<TypeReference, AlignAndSize>(new TypeReferenceEqualityComparer())
        };

        private static Dictionary<FieldReference, AlignAndSize>[] StructFieldAlignment =
        {
            new Dictionary<FieldReference, AlignAndSize>(new FieldReferenceComparer()), new Dictionary<FieldReference, AlignAndSize>(new FieldReferenceComparer())
        };

        internal static Dictionary<TypeReference, bool>[] ValueTypeIsComplex =
        {
            new Dictionary<TypeReference, bool>(new TypeReferenceEqualityComparer()), new Dictionary<TypeReference, bool>(new TypeReferenceEqualityComparer())
        };

        public static AlignAndSize AlignAndSizeOfType(MetadataType mtype, int bits)
        {
            if (mtype == MetadataType.Boolean || mtype == MetadataType.Byte || mtype == MetadataType.SByte) return AlignAndSize.One;
            if (mtype == MetadataType.Int16 || mtype == MetadataType.UInt16 || mtype == MetadataType.Char) return AlignAndSize.Two;
            if (mtype == MetadataType.Int32 || mtype == MetadataType.UInt32 || mtype == MetadataType.Single) return AlignAndSize.Four;
            if (mtype == MetadataType.Int64 || mtype == MetadataType.UInt64 || mtype == MetadataType.Double) return AlignAndSize.Eight;
            if (mtype == MetadataType.IntPtr || mtype == MetadataType.UIntPtr) return AlignAndSize.Pointer(bits);
            if (mtype == MetadataType.String) return AlignAndSize.NativeString(bits);

            throw new ArgumentException($"Metadata type {mtype} is a special type which is not supported");
        }

        public static AlignAndSize AlignAndSizeOfType(TypeReference typeRef, int bits)
        {
            // This is a gross hack and i'm not proud of it; we use bits as an array index,
            // and we call this method recursively.
            if (bits == 32) bits = 0;
            else if (bits == 64) bits = 1;

            if (typeRef.IsPointer)
                return AlignAndSize.Pointer(bits);

            TypeDefinition fixedSpecialType = typeRef.FixedSpecialType();
            if (fixedSpecialType != null)
            {
                return AlignAndSizeOfType(fixedSpecialType.MetadataType, bits);
            }

            var type = typeRef.Resolve();

            // Handle the case where we have a fixed buffer. Cecil will name it: "<MyMemberName>e_FixedBuffer"
            if (type.ClassSize != -1 && typeRef.Name.Contains(">e__FixedBuffer"))
            {
                // Fixed buffers can only be of primitive types so inspect the fields of the buffer (there should only be one)
                // and determine the packing requirement for the type
                if (type.Fields.Count() != 1)
                    throw new ArgumentException("A FixedBuffer type contains more than one field, this should not happen");

                var fieldAlignAndSize = AlignAndSizeOfType(type.Fields[0].FieldType.MetadataType, bits);
                return new AlignAndSize(fieldAlignAndSize.align, type.ClassSize);
            }
            else if (type.IsExplicitLayout && type.ClassSize > 0 && type.PackingSize > 0)
            {
                return new AlignAndSize(type.PackingSize, type.ClassSize);
            }

            if (ValueTypeAlignment[bits].ContainsKey(typeRef))
            {
                var sz = ValueTypeAlignment[bits][typeRef];

                if (sz.IsSentinel)
                    throw new ArgumentException($"Type {typeRef} triggered sentinel; recursive value type definition");

                return sz;
            }

            if (typeRef.IsArray)
                throw new ArgumentException($"Can't represent {typeRef}: C# array types cannot be represented directly, use DynamicArray<T>");

            if (typeRef.IsDynamicArray())
            {
                var elementType = typeRef.DynamicArrayElementType();
                //Console.WriteLine($"{nts.TypeArguments[0]}");
                // call this just for the side effect checks
                if (AlignAndSizeOfType(elementType, bits).size == 0)
                    throw new Exception("Unexpected type with size 0: " + elementType);

                // arrays match std::array, for emscripten at least
                return AlignAndSize.DynamicArray(bits);
            }

            if (type.IsEnum)
            {
                // Inspect the __value member to determine the underlying type size
                var enumBaseType = type.Fields.First(f => f.Name == "value__").FieldType;
                return AlignAndSizeOfType(enumBaseType, bits);
            }

            if (!type.IsValueType)
            {
                // Why not throw? Really should expect this:
                //throw new ArgumentException($"Type {type} ({type.Name}) was expected to be a value type");
                // However, the DisposeSentinel is a ManagedType that sits in the NativeContainers.
                // Treat it as an opaque pointer and skip over it.
                return AlignAndSizeOfType(MetadataType.IntPtr, bits);
            }

            ValueTypeAlignment[bits].Add(typeRef, AlignAndSize.Sentinel);
            PreprocessTypeFields(typeRef, bits);
            return ValueTypeAlignment[bits][typeRef];
        }

        public static AlignAndSize AlignAndSizeOfField(FieldReference fieldRef, int bits)
        {
            if (bits == 32) bits = 0;
            else if (bits == 64) bits = 1;

            if (!StructFieldAlignment[bits].ContainsKey(fieldRef))
            {
                PreprocessTypeFields(fieldRef.DeclaringType, bits);
            }
            return StructFieldAlignment[bits][fieldRef];
        }

        public static int AlignUp(int sz, int align)
        {
            if (align == 0)
                return sz;
            int k = (sz + align - 1);
            return k - k % align;
        }

        public static bool HasNestedDynamicArrayType(TypeReference type)
        {
            if (type.IsPrimitive || type.Resolve().IsEnum || type.MetadataType == MetadataType.String) return false;
            if (type.IsDynamicArray()) return true;

            foreach (var field in type.Resolve().Fields)
            {
                if (field.IsNotSerialized) continue;
                if (HasNestedDynamicArrayType(field.FieldType))
                    return true;
            }

            return false;
        }

        private static HashSet<TypeDefinition> AlreadyValidated = new HashSet<TypeDefinition>();

        public static void ValidateAllowedObjectType(TypeReference typeRef)
        {
            TypeDefinition type = typeRef.Resolve();
            if (typeRef.IsPrimitive || type.IsEnum || typeRef.MetadataType == MetadataType.String)
                return;

            if (AlreadyValidated.Contains(type))
                return;

            AlreadyValidated.Add(type);

            var typeResolver = TypeResolver.For(type);
            foreach (var field in type.Fields)
                ValidateAllowedObjectType(typeResolver.Resolve(field.FieldType));
        }

        public static void PreprocessTypeFields(TypeReference valuetype, int bits)
        {
            if (bits == 32) bits = 0;
            else if (bits == 64) bits = 1;

            int size = 0;
            int highestFieldAlignment = 0;
            bool isComplex = false;

            // have we already preprocessed this?
            if (ValueTypeAlignment[bits].ContainsKey(valuetype) &&
                !ValueTypeAlignment[bits][valuetype].IsSentinel)
            {
                return;
            }

            // For each field, calculate its layout as if it was a C++ struct
            //Console.WriteLine($"Type {valuetype}");
            var typeResolver = TypeResolver.For(valuetype);
            var typeDef = valuetype.Resolve();
            foreach (var fs in typeDef.Fields)
            {
                if (fs.IsStatic)
                    continue;

                var fieldType = typeResolver.Resolve(fs.FieldType);

                var sz = AlignAndSizeOfType(fieldType, bits);
                isComplex = isComplex || fieldType.IsComplex();

                // In C++, all members of a struct must have their own address.
                // If we have a "struct {}" as a member, treat its size as at least one byte.
                sz = new AlignAndSize(sz.align, Math.Max(sz.size, 1));
                highestFieldAlignment = Math.Max(highestFieldAlignment, sz.align);
                size = AlignUp(size, sz.align);
                //Console.WriteLine($"  Field: {fs.Name} ({fs.GetType()}) - offset: {size} alignment {sz.align} sz {sz.size}");
                StructFieldAlignment[bits].Add(typeResolver.Resolve(fs) /*VERIFY*/, new AlignAndSize(sz.align, sz.size, size));

                int offset = fs.Offset;
                if (offset >= 0)
                    size = offset + sz.size;
                else
                    size += sz.size;
            }
            // same min size for outer struct
            size = Math.Max(size, 1);

            // C++ aligns struct sizes up to the highest alignment required
            size = AlignUp(size, highestFieldAlignment);

            // If an explicit size have been provided use that instead
            if (typeDef.IsExplicitLayout && typeDef.ClassSize > 0)
                size = typeDef.ClassSize;

            // Alignment requirements are > 0
            highestFieldAlignment = Math.Max(highestFieldAlignment, 1);

            // If an explict alignment has been provided use that instead
            if (typeDef.IsExplicitLayout && typeDef.PackingSize > 0)
                size = typeDef.PackingSize;

            ValueTypeAlignment[bits].Remove(valuetype);
            ValueTypeAlignment[bits].Add(valuetype, new AlignAndSize(highestFieldAlignment, size, 0, typeDef.Fields.Count == 0));
            //Console.WriteLine($"ValueType: {valuetype.Name} ({valuetype.GetType()}) - alignment {highestFieldAlignment} sz {size}");
            ValueTypeIsComplex[bits].Add(valuetype, isComplex);
        }

        internal static void GetFieldOffsetsOfRecurse(Func<FieldReference, TypeReference, bool> match, int offset, TypeReference type, List<int> list, int bits)
        {
            int in_type_offset = 0;
            var typeResolver = TypeResolver.For(type);
            foreach (var f in type.Resolve().Fields)
            {
                if (f.IsStatic)
                    continue;

                uint alignUp(uint a, uint align) => (a + ((align - a) % align));
                int valueOr1(int v) => Math.Max(v, 1);

                TypeUtils.AlignAndSize resize(TypeUtils.AlignAndSize s) => new TypeUtils.AlignAndSize(
                    valueOr1(s.align),
                    valueOr1(s.size));

                var fieldReference = typeResolver.Resolve(f);
                var fieldType = typeResolver.Resolve(f.FieldType);

                var tinfo = resize(TypeUtils.AlignAndSizeOfType(fieldType, bits));
                if (f.Offset != -1)
                    in_type_offset = f.Offset;
                else
                    in_type_offset = (int)alignUp((uint)in_type_offset, (uint)tinfo.align);

                if (fieldType.IsDynamicArray() && match(fieldReference, fieldType.DynamicArrayElementType()))
                {
                    // +1 so that we have a way to indicate an Array<Entity> at position 0
                    // fixup code subtracts 1
                    list.Add(-(offset + in_type_offset + 1));
                }
                else if (match(fieldReference, fieldType))
                {
                    list.Add(offset + in_type_offset);
                }
                else if (fieldType.IsValueType && !fieldType.IsPrimitive)
                {
                    GetFieldOffsetsOfRecurse(match, offset + in_type_offset, fieldType, list, bits);
                }

                in_type_offset += tinfo.size;
            }
        }

        public static List<int> GetFieldOffsetsOf(TypeReference typeToFind, TypeReference typeToLookIn, int archBits)
        {
            var offsets = new List<int>();

            if (typeToLookIn != null)
            {
                GetFieldOffsetsOfRecurse((_, typeRef) => TypeReferenceEqualityComparer.AreEqual(typeRef, typeToFind), 0, typeToLookIn, offsets, archBits);
            }

            return offsets;
        }

        /* match: 1st param is the Field, 2nd param is the FieldType */
        public static List<int> GetFieldOffsetsOf(Func<FieldReference, TypeReference, bool> match, TypeReference typeToLookIn, int archBits)
        {
            var offsets = new List<int>();

            if (typeToLookIn != null)
            {
                GetFieldOffsetsOfRecurse(match, 0, typeToLookIn, offsets, archBits);
            }

            return offsets;
        }

        public static List<int> GetFieldOffsetsOf(string fieldTypeName, TypeReference typeToLookIn, int archBits)
        {
            var offsets = new List<int>();

            if (typeToLookIn != null)
            {
                GetFieldOffsetsOfRecurse((_, typeRef) => typeRef.Resolve().FullName == fieldTypeName, 0, typeToLookIn, offsets, archBits);
            }

            return offsets;
        }

        public static List<int> GetFieldOffsetsOfByFieldName(string fieldName, TypeReference typeToLookIn, int archBits)
        {
            var offsets = new List<int>();

            if (typeToLookIn != null)
            {
                GetFieldOffsetsOfRecurse((fieldRef, typeRef) => fieldRef.Name == fieldName, 0, typeToLookIn, offsets, archBits);
            }

            return offsets;
        }

        public static List<int> GetEntityFieldOffsets(TypeReference type, int archBits)
        {
            return GetFieldOffsetsOf("Unity.Entities.Entity", type, archBits);
        }
    }
}

#endif

using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Collections.Generic;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;

namespace Unity.Entities.CodeGen
{
    static class InjectAndInitializeEntityQueryField
    {
        public static FieldDefinition InjectAndInitialize(MethodDefinition methodToAnalyze, LambdaJobDescriptionConstruction descriptionConstruction, Collection<ParameterDefinition> closureParameters)
        {
            /* We're going to generate this code:
             *
             * protected void override OnCreate()
             * {
             *     _entityQuery = GetEntityQuery_ForMyJob_From(this);
             * }
             *
             * static void GetEntityQuery_ForMyJob_From(ComponentSystem componentSystem)
             * {
             *     var result = componentSystem.GetEntityQuery(new[] { new EntityQueryDesc() {
             *         All = new[] { ComponentType.ReadWrite<Position>(), ComponentType.ReadOnly<Velocity>() },
             *         None = new[] { ComponentType.ReadWrite<IgnoreTag>() }
             *     }});
             *     result.SetChangedFilter(new[] { ComponentType.ReadOnly<Position>() } );
             * }
             */

            var module = methodToAnalyze.Module;

            var entityQueryField = new FieldDefinition($"<>{descriptionConstruction.LambdaJobName}_entityQuery",
                FieldAttributes.Private, module.ImportReference(typeof(EntityQuery)));
            var userSystemType = methodToAnalyze.DeclaringType;
            userSystemType.Fields.Add(entityQueryField);

            var getEntityQueryFromMethod = AddGetEntityQueryFromMethod(descriptionConstruction, closureParameters.ToArray(), methodToAnalyze.DeclaringType);

            List<Instruction> instructionsToInsert = new List<Instruction>();
            instructionsToInsert.Add(
                new[]
                {
                    Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Ldarg_0),
                    Instruction.Create(OpCodes.Call, getEntityQueryFromMethod),
                    Instruction.Create(OpCodes.Stfld, entityQueryField)
                }
            );

            // Store our generated query in a user-specified field if one was given
            if (descriptionConstruction.StoreQueryInField != null)
            {
                instructionsToInsert.Add(
                    new[]
                    {
                        Instruction.Create(OpCodes.Ldarg_0),
                        Instruction.Create(OpCodes.Ldarg_0),
                        Instruction.Create(OpCodes.Ldfld, entityQueryField),
                        Instruction.Create(OpCodes.Stfld, descriptionConstruction.StoreQueryInField),
                    });
            }
            InsertIntoOnCreateForCompilerMethod(userSystemType, instructionsToInsert.ToArray());

            return entityQueryField;
        }

        public static void InjectAndInitialize(TypeDefinition userSystemType, FieldDefinition entityQueryField, TypeReference singletonType, bool asReadOnly)
        {
            userSystemType.Fields.Add(entityQueryField);

            var getEntityQueryMethod = userSystemType.Module.ImportReference(typeof(ComponentSystemBase)
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Single(m =>
                    m.Name == "GetEntityQuery" && m.GetParameters().Length == 1 &&
                    m.GetParameters().Single().ParameterType == typeof(ComponentType[])));

            var componentTypeReference = userSystemType.Module.ImportReference(typeof(ComponentType));
            List<Instruction> instructionsToInsert = new List<Instruction>();

            instructionsToInsert.Add(Instruction.Create(OpCodes.Ldarg_0));
            instructionsToInsert.Add(Instruction.Create(OpCodes.Ldarg_0));
            foreach (var instruction in InstructionsToPutArrayOfComponentTypesOnStack(
                new(TypeReference typeReference, bool readOnly)[] {(singletonType, asReadOnly)}, componentTypeReference))
                instructionsToInsert.Add(instruction);
            instructionsToInsert.Add(Instruction.Create(OpCodes.Call, getEntityQueryMethod));
            instructionsToInsert.Add(Instruction.Create(OpCodes.Stfld, entityQueryField));

            InsertIntoOnCreateForCompilerMethod(userSystemType, instructionsToInsert.ToArray());
        }

        public static void InsertIntoOnCreateForCompilerMethod(TypeDefinition userSystemType, Instruction[] instructions)
        {
            var methodBody = EntitiesILHelpers.GetOrMakeOnCreateForCompilerMethodFor(userSystemType).Body.GetILProcessor().Body;
            methodBody.GetILProcessor().InsertBefore(methodBody.Instructions.Last(), instructions);
        }

        static MethodDefinition AddGetEntityQueryFromMethod(LambdaJobDescriptionConstruction descriptionConstruction, ParameterDefinition[] closureParameters,
            TypeDefinition typeToInjectIn)
        {
            var moduleDefinition = typeToInjectIn.Module;
            var typeDefinition = typeToInjectIn;
            var getEntityQueryFromMethod =
                new MethodDefinition($"<>GetEntityQuery_For{descriptionConstruction.LambdaJobName}_From",
                    MethodAttributes.Public | MethodAttributes.Static,
                    moduleDefinition.ImportReference(typeof(EntityQuery)))
            {
                DeclaringType = typeDefinition,
                HasThis = false,
                Parameters =
                {
                    new ParameterDefinition("componentSystem", ParameterAttributes.None, moduleDefinition.ImportReference(typeof(ComponentSystemBase)))
                }
            };

            typeDefinition.Methods.Add(getEntityQueryFromMethod);
            var body = getEntityQueryFromMethod.Body;
            body.InitLocals = true; // initlocals must be set for verifiable methods with one or more local variables

            var getEntityQueryMethod = moduleDefinition.ImportReference(typeof(ComponentSystemBase)
                .GetMethods(BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public).Single(m =>
                    m.Name == "GetEntityQuery" && m.GetParameters().Length == 1 &&
                    m.GetParameters().Single().ParameterType == typeof(EntityQueryDesc[])));

            var entityQueryDescConstructor = moduleDefinition.ImportReference(typeof(EntityQueryDesc).GetConstructor(Array.Empty<Type>()));
            var componentTypeReference = moduleDefinition.ImportReference(typeof(ComponentType));

            var withNoneTypes = AllTypeArgumentsOfMethod(moduleDefinition, descriptionConstruction, nameof(LambdaJobQueryConstructionMethods.WithNone));
            var withAllTypes = AllTypeArgumentsOfMethod(moduleDefinition, descriptionConstruction, nameof(LambdaJobQueryConstructionMethods.WithAll));
            var withSharedComponentFilterTypes = AllTypeArgumentsOfMethod(moduleDefinition, descriptionConstruction, nameof(LambdaJobQueryConstructionMethods.WithSharedComponentFilter));
            foreach (var allType in withAllTypes.Where(t => withSharedComponentFilterTypes.Contains(t)))
            {
                UserError.DC0026(allType.Name, descriptionConstruction.ContainingMethod,
                    descriptionConstruction.InvokedConstructionMethods.First().InstructionInvokingMethod).Throw();
            }
            var withAllAndSharedComponentFilterTypes = withAllTypes.Concat(withSharedComponentFilterTypes);

            var withAnyTypes = AllTypeArgumentsOfMethod(moduleDefinition, descriptionConstruction, nameof(LambdaJobQueryConstructionMethods.WithAny));
            var withChangeFilterTypes = AllTypeArgumentsOfMethod(moduleDefinition, descriptionConstruction, nameof(LambdaJobQueryConstructionMethods.WithChangeFilter));

            var arrayOfSingleEQDVariable = new VariableDefinition(moduleDefinition.ImportReference(typeof(EntityQueryDesc[])));
            var localVarOfEQD = new VariableDefinition(moduleDefinition.ImportReference(typeof(EntityQueryDesc)));
            var localVarOfResult = new VariableDefinition(moduleDefinition.ImportReference(typeof(EntityQuery)));

            foreach (var closureParameter in closureParameters)
            {
                var parameterElementType = closureParameter.ParameterType.GetElementType();
                if (parameterElementType.IsGenericInstance || parameterElementType.IsGenericParameter ||
                    (parameterElementType.HasGenericParameters && !parameterElementType.IsDynamicBufferOfT()))
                {
                    UserError.DC0025($"Type {closureParameter.ParameterType.Name} cannot be used as an Entities.ForEach parameter as generic types and generic parameters are not supported in Entities.ForEach",
                        descriptionConstruction.ContainingMethod, descriptionConstruction.InvokedConstructionMethods.First().InstructionInvokingMethod).Throw();
                }
            }

            body.Variables.Add(arrayOfSingleEQDVariable);
            body.Variables.Add(localVarOfEQD);
            body.Variables.Add(localVarOfResult);

            var combinedWithAllTypes = withAllAndSharedComponentFilterTypes.Select(typeReference => (typeReference, true))
                .Concat(closureParameters.Select(WithAllTypeArgumentForLambdaParameter).Where(t => t.typeReference != null))
                .ToArray();

            foreach (var noneType in withNoneTypes)
            {
                if (combinedWithAllTypes.Select(c => c.typeReference).Any(allType => allType.TypeReferenceEquals(noneType)))
                    UserError.DC0015(noneType.Name, descriptionConstruction.ContainingMethod, descriptionConstruction.InvokedConstructionMethods.First().InstructionInvokingMethod).Throw();

                if (withAnyTypes.Any(anyType => anyType.TypeReferenceEquals(noneType)))
                    UserError.DC0016(noneType.Name, descriptionConstruction.ContainingMethod, descriptionConstruction.InvokedConstructionMethods.First().InstructionInvokingMethod).Throw();
            }

            var instructions = new List<Instruction>()
            {
                //var arrayOfSingleEQDVariable = new EnityQueryDesc[1];
                Instruction.Create(OpCodes.Ldc_I4_1),
                Instruction.Create(OpCodes.Newarr, moduleDefinition.ImportReference(typeof(EntityQueryDesc))),
                Instruction.Create(OpCodes.Stloc, arrayOfSingleEQDVariable),

                //var localVarOfEQD = new EntityQuery();
                Instruction.Create(OpCodes.Newobj, entityQueryDescConstructor),
                Instruction.Create(OpCodes.Stloc, localVarOfEQD),

                // arrayOfSingleEQDVariable[0] = localVarOfEQD;
                Instruction.Create(OpCodes.Ldloc, arrayOfSingleEQDVariable),
                Instruction.Create(OpCodes.Ldc_I4_0),
                Instruction.Create(OpCodes.Ldloc, localVarOfEQD),
                Instruction.Create(OpCodes.Stelem_Any, moduleDefinition.ImportReference(typeof(EntityQueryDesc))),

                InstructionsToSetEntityQueryDescriptionField(nameof(EntityQueryDesc.All), combinedWithAllTypes, componentTypeReference,
                    localVarOfEQD, entityQueryDescConstructor),
                InstructionsToSetEntityQueryDescriptionField(nameof(EntityQueryDesc.None), withNoneTypes.Select(t => (t, false)).ToArray(), componentTypeReference,
                    localVarOfEQD, entityQueryDescConstructor),
                InstructionsToSetEntityQueryDescriptionField(nameof(EntityQueryDesc.Any), withAnyTypes.Select(t => (t, false)).ToArray(), componentTypeReference,
                    localVarOfEQD, entityQueryDescConstructor),

                InstructionsToSetEntityQueryDescriptionOptions(localVarOfEQD, entityQueryDescConstructor, descriptionConstruction),

                Instruction.Create(OpCodes.Ldarg_0), //the this for this.GetEntityQuery()
                Instruction.Create(OpCodes.Ldloc, arrayOfSingleEQDVariable),
                Instruction.Create(OpCodes.Call, getEntityQueryMethod),

                Instruction.Create(OpCodes.Stloc, localVarOfResult),

                InstructionsToSetChangedVersionFilterFor(moduleDefinition, withChangeFilterTypes, localVarOfResult, componentTypeReference),

                Instruction.Create(OpCodes.Ldloc, localVarOfResult),
                Instruction.Create(OpCodes.Ret),
            };

            var ilProcessor = getEntityQueryFromMethod.Body.GetILProcessor();
            ilProcessor.Append(instructions);

            return getEntityQueryFromMethod;
        }

        static IEnumerable<Instruction> InstructionsToCreateComponentTypeFor(TypeReference typeReference, bool isReadOnly, int arrayIndex,
            TypeReference componentTypeReference)
        {
            yield return Instruction.Create(OpCodes.Dup); //put the array on the stack again
            yield return Instruction.Create(OpCodes.Ldc_I4, arrayIndex);

            var componentTypeDefinition = componentTypeReference.Resolve();

            MethodReference ComponentTypeMethod(string name) =>
                typeReference.Module.ImportReference(
                    componentTypeDefinition.Methods.Single(m => m.Name == name && m.Parameters.Count == 0));

            var readOnlyMethod = ComponentTypeMethod(nameof(ComponentType.ReadOnly));
            var readWriteMethod = ComponentTypeMethod(nameof(ComponentType.ReadWrite));

            var method = isReadOnly ? readOnlyMethod : readWriteMethod;
            yield return Instruction.Create(OpCodes.Call, method.MakeGenericInstanceMethod(typeReference.GetElementType()));
            yield return Instruction.Create(OpCodes.Stelem_Any, componentTypeReference);
        }

        static IEnumerable<Instruction> InstructionsToPutArrayOfComponentTypesOnStack((TypeReference typeReference, bool readOnly)[] typeReferences,
            TypeReference componentTypeReference)
        {
            yield return Instruction.Create(OpCodes.Ldc_I4, typeReferences.Length);
            yield return Instruction.Create(OpCodes.Newarr, componentTypeReference);

            for (int i = 0; i != typeReferences.Length; i++)
            {
                foreach (var instruction in InstructionsToCreateComponentTypeFor(typeReferences[i].typeReference, typeReferences[i].readOnly, i,
                    componentTypeReference))
                {
                    yield return instruction;
                }
            }
        }

        static List<TypeReference> AllTypeArgumentsOfMethod(ModuleDefinition moduleDefinition, LambdaJobDescriptionConstruction descriptionConstruction, string methodName)
        {
            var invokedConstructionMethods = descriptionConstruction.InvokedConstructionMethods.Where(m => m.MethodName == methodName);
            var result = new List<TypeReference>();

            foreach (var m in invokedConstructionMethods)
            {
                foreach (var argumentType in m.TypeArguments)
                {
                    if (argumentType.IsGenericParameter || argumentType.IsGenericInstance)
                        UserError.DC0025($"Type {argumentType.Name} cannot be used with {m.MethodName} as generic types and parameters are not allowed", descriptionConstruction.ContainingMethod, m.InstructionInvokingMethod).Throw();
                    var argumentTypeDefinition = argumentType.Resolve();
                    if (!LambdaParamaterValueProviderInformation.IsTypeValidForEntityQuery(argumentTypeDefinition))
                        UserError.DC0025($"Type {argumentType.Name} cannot be used with {m.MethodName} as it is not a supported component type", descriptionConstruction.ContainingMethod, m.InstructionInvokingMethod).Throw();

                    result.Add(moduleDefinition.ImportReference(argumentType));
                }
            }

            return result;
        }

        static IEnumerable<Instruction> InstructionsToSetChangedVersionFilterFor(ModuleDefinition moduleDefinition, List<TypeReference> typeReferences,
            VariableDefinition localVarOfResult, TypeReference componentTypeReference)
        {
            if (typeReferences.Count == 0)
                yield break;

            yield return Instruction.Create(OpCodes.Ldloca, localVarOfResult); //<- target of the SetChangedFilter call

            //create the array for the first argument:   new[] { ComponentType.ReadOnly<Position>(), ComponentType>.ReadOnly<Velocity>() }
            foreach (var instruction in InstructionsToPutArrayOfComponentTypesOnStack(typeReferences.Select(t => (t, false)).ToArray(), componentTypeReference))
                yield return instruction;

            EntityQuery eq;
            var setChangedVersionFilter = moduleDefinition.ImportReference(
                typeof(EntityQuery).GetMethod(nameof(eq.SetChangedVersionFilter), new[] {typeof(ComponentType[])}));

            //and do the actual invocation
            yield return Instruction.Create(OpCodes.Call, setChangedVersionFilter);
        }

        static IEnumerable<Instruction> InstructionsToSetEntityQueryDescriptionField(string fieldName,
            (TypeReference typeReference, bool readOnly)[] typeReferences, TypeReference componentTypeReference, VariableDefinition localVarOfEQD,
            MethodReference entityQueryDescConstructor)
        {
            if (typeReferences.Length == 0)
                yield break;

            yield return Instruction.Create(OpCodes.Ldloc, localVarOfEQD);
            foreach (var instruction in InstructionsToPutArrayOfComponentTypesOnStack(typeReferences, componentTypeReference))
                yield return instruction;
            var fieldReference = new FieldReference(fieldName, componentTypeReference.Module.ImportReference(typeof(ComponentType[])),
                entityQueryDescConstructor.DeclaringType);
            yield return Instruction.Create(OpCodes.Stfld, fieldReference);
        }

        static IEnumerable<Instruction> InstructionsToSetEntityQueryDescriptionOptions(VariableDefinition localVarOfEQD,
            MethodReference entityQueryDescConstructor, LambdaJobDescriptionConstruction descriptionConstruction)
        {
            var withOptionsInvocation = descriptionConstruction.InvokedConstructionMethods.FirstOrDefault(m =>
                m.MethodName == nameof(LambdaJobQueryConstructionMethods.WithEntityQueryOptions));
            if (withOptionsInvocation == null)
                yield break;

            yield return Instruction.Create(OpCodes.Ldloc, localVarOfEQD);
            yield return Instruction.Create(OpCodes.Ldc_I4, (int)withOptionsInvocation.Arguments.Single());
            var fieldReference = new FieldReference(nameof(EntityQueryDesc.Options), entityQueryDescConstructor.Module.ImportReference(typeof(EntityQueryOptions)),
                entityQueryDescConstructor.DeclaringType);
            yield return Instruction.Create(OpCodes.Stfld, fieldReference);
        }

        static (TypeReference typeReference, bool readOnly) WithAllTypeArgumentForLambdaParameter(ParameterDefinition p)
        {
            var isMarkedReadOnly = p.HasCompilerServicesIsReadOnlyAttribute();
            var isMarkedAsRef = p.ParameterType.IsByReference && !isMarkedReadOnly;

            var type = p.ParameterType.Resolve();
            if (type.IsIComponentDataStruct() || type.IsISharedComponentData())
                return (p.ParameterType.GetElementType(), !isMarkedAsRef);

            if (type.IsIComponentDataClass() || type.IsUnityEngineObject())
                return (p.ParameterType.GetElementType(), isMarkedReadOnly);

            if (type.IsDynamicBufferOfT())
            {
                var typeReference = ((GenericInstanceType)p.ParameterType.StripRef()).GenericArguments.Single();
                return (typeReference, isMarkedReadOnly);
            }

            return (null, true);
        }
    }
}

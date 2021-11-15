using System;
using System.Linq;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Mono.Cecil.Rocks;
using Unity.Entities.CodeGeneratedJobForEach;

namespace Unity.Entities.CodeGen
{
    class LambdaParamaterValueProviderInformation
    {
        public readonly TypeReference Provider;
        public readonly MethodReference ProviderScheduleTimeInitializeMethod;
        public readonly MethodReference ProviderPrepareToExecuteOnEntitiesIn;
        public readonly MethodReference PrepareToExecuteWithStructuralChanges;
        public readonly TypeReference ProviderRuntime;
        public readonly MethodReference RuntimeForMethod;
        public readonly TypeReference RuntimeForMethodReturnType;
        public readonly MethodReference RuntimeWriteBackMethod;
        public readonly MethodReference ProviderFinishExecuteOnEntities;
        public readonly bool IsReadOnly;
        public readonly bool WithStructuralChanges;
        public readonly string Name;

        LambdaParamaterValueProviderInformation(TypeReference provider, TypeReference providerRuntime, bool readOnly,
                                                string name, TypeReference runtimeForMethodReturnType = null, bool withStructuralChanges = false)
        {
            Provider = provider;
            Name = name;

            ProviderRuntime = providerRuntime;
            (RuntimeForMethod, RuntimeForMethodReturnType) = MethodReferenceFor("For", ProviderRuntime);
            if (runtimeForMethodReturnType != null)
                RuntimeForMethodReturnType = runtimeForMethodReturnType;

            if (providerRuntime.DeclaringType.Name == typeof(LambdaParameterValueProvider_IComponentData<>).Name && withStructuralChanges)
                (RuntimeWriteBackMethod, _) = MethodReferenceFor("WriteBack", ProviderRuntime);
            (PrepareToExecuteWithStructuralChanges, _) = MethodReferenceFor(nameof(LambdaParameterValueProvider_Entity.PrepareToExecuteWithStructuralChanges), Provider);
            (ProviderScheduleTimeInitializeMethod, _) = MethodReferenceFor(nameof(LambdaParameterValueProvider_Entity.ScheduleTimeInitialize), Provider);
            (ProviderPrepareToExecuteOnEntitiesIn, _) = MethodReferenceFor(nameof(LambdaParameterValueProvider_Entity.PrepareToExecuteOnEntitiesIn), Provider);

            IsReadOnly = readOnly;
            WithStructuralChanges = withStructuralChanges;
        }

        static (MethodReference methodReference, TypeReference specializedReturnType) MethodReferenceFor(string methodName, TypeReference typeReference)
        {
            var resolvedMethod = typeReference.Module.ImportReference(typeReference.Resolve().Methods.Single(m => m.Name == methodName));

            var resolvedMethod2 = typeReference.Module.ImportReference(typeReference.Resolve().Methods.Single(m => m.Name == methodName));
            var specializedReturnType = resolvedMethod2.ReturnType;
            //I tried coding this up in a totally generic way where we can correctly figure out the "specialized" type of the returntype, but our case
            //with ElementProvider_DynamicBuffer<Something>.Runtime takes a better person than me apparently. No worries! this code only needs to deal
            //with this one generic weirdo case correctly, so let's just make a specialized codepath for it, and do the thing that we know is correct
            //just for that once case.
            if (methodName == "For" && typeReference.DeclaringType.Name == typeof(LambdaParameterValueProvider_DynamicBuffer<>).Name)
            {
                ((GenericInstanceType)specializedReturnType).GenericArguments[0] =  ((GenericInstanceType)typeReference).GenericArguments.Single();
            }
            else if (methodName == "For" &&
                     (typeReference.DeclaringType.Name == typeof(LambdaParameterValueProvider_IComponentData<>).Name ||
                      typeReference.DeclaringType.Name == typeof(LambdaParameterValueProvider_IComponentData_Tag<>).Name))
            {
                specializedReturnType = ((GenericInstanceType)typeReference).GenericArguments.Single();
            }

            var result = new MethodReference(resolvedMethod.Name, resolvedMethod.ReturnType, typeReference)
            {
                HasThis = resolvedMethod.HasThis,
            };
            foreach (var pd in resolvedMethod.Parameters)
                result.Parameters.Add(pd);
            return (result, specializedReturnType);
        }

        public bool PrepareToExecuteOnEntitiesTakesJustAChunkParameter => ProviderPrepareToExecuteOnEntitiesIn.Parameters.Count() == 1;


        static public bool IsTypeValidForEntityQuery(TypeDefinition t)
        {
            if (t.IsIComponentDataStruct())
                return true;
            if (t.IsIComponentDataClass() || t.IsUnityEngineObject())
                return true;
            if (t.IsIBufferElementData())
                return true;
            if (t.IsISharedComponentData())
                return true;
            return false;
        }

        public static LambdaParamaterValueProviderInformation ElementProviderInformationFor(
            LambdaJobDescriptionConstruction lambdaJobDescriptionConstruction, ParameterDefinition parameter, bool withStructuralChanges)
        {
            var moduleDefinition = lambdaJobDescriptionConstruction.ContainingMethod.Module;

            (TypeReference provider, TypeReference providerRuntime) ImportReferencesFor(Type providerType, Type runtimeType, TypeReference typeOfT)
            {
                var provider = moduleDefinition
                    .ImportReference(providerType)
                    .MakeGenericInstanceType(typeOfT);
                var providerRuntime = moduleDefinition.ImportReference(runtimeType).MakeGenericInstanceType(typeOfT);

                return (provider, providerRuntime);
            }

            var parameterType = parameter.ParameterType;
            var resolvedParameterType = parameterType.Resolve();

            // IComponentData
            if (resolvedParameterType.IsIComponentDataStruct())
            {
                var readOnly = !parameter.ParameterType.IsByReference || parameter.HasCompilerServicesIsReadOnlyAttribute();

                if (resolvedParameterType.IsTagComponentDataStruct())
                {
                    var(provider, providerRuntime) =
                        ImportReferencesFor(
                            typeof(LambdaParameterValueProvider_IComponentData_Tag<>),
                            withStructuralChanges
                            ? typeof(LambdaParameterValueProvider_IComponentData_Tag<>.StructuralChangeRuntime)
                            : typeof(LambdaParameterValueProvider_IComponentData_Tag<>.Runtime), parameter.ParameterType.GetElementType());
                    return new LambdaParamaterValueProviderInformation(provider, providerRuntime, readOnly, parameter.Name, null, false);
                }
                else
                {
                    var(provider, providerRuntime) =
                        ImportReferencesFor(
                            typeof(LambdaParameterValueProvider_IComponentData<>),
                            withStructuralChanges
                            ? typeof(LambdaParameterValueProvider_IComponentData<>.StructuralChangeRuntime)
                            : typeof(LambdaParameterValueProvider_IComponentData<>.Runtime), parameter.ParameterType.GetElementType());
                    return new LambdaParamaterValueProviderInformation(provider, providerRuntime, readOnly, parameter.Name, null, withStructuralChanges);
                }
            }

            // class IComponentData / UnityEngine.Object
            if (resolvedParameterType.IsIComponentDataClass() || resolvedParameterType.IsUnityEngineObject())
            {
                if (lambdaJobDescriptionConstruction.UsesBurst ||
                    lambdaJobDescriptionConstruction.ExecutionMode == ExecutionMode.Schedule ||
                    lambdaJobDescriptionConstruction.ExecutionMode == ExecutionMode.ScheduleParallel)
                    UserError.DC0023(lambdaJobDescriptionConstruction.ContainingMethod, parameterType, lambdaJobDescriptionConstruction.WithCodeInvocationInstruction).Throw();

                bool readOnly = false;
                if (parameter.ParameterType.IsByReference)
                {
                    if (parameter.HasCompilerServicesIsReadOnlyAttribute())
                        readOnly = true;
                    else
                        UserError.DC0024(lambdaJobDescriptionConstruction.ContainingMethod, parameterType, lambdaJobDescriptionConstruction.WithCodeInvocationInstruction).Throw();
                }

                var(provider, providerRuntime) = ImportReferencesFor(typeof(LambdaParameterValueProvider_ManagedComponentData<>),
                    withStructuralChanges
                    ? typeof(LambdaParameterValueProvider_ManagedComponentData<>.StructuralChangeRuntime)
                    : typeof(LambdaParameterValueProvider_ManagedComponentData<>.Runtime), parameter.ParameterType.GetElementType());
                return new LambdaParamaterValueProviderInformation(provider, providerRuntime, readOnly, parameter.Name, parameter.ParameterType.GetElementType());
            }

            // DynamicBuffer<T>
            if (resolvedParameterType.IsDynamicBufferOfT())
            {
                TypeReference typeRef = parameterType;
                if (parameterType is ByReferenceType referenceType)
                    typeRef = referenceType.ElementType;

                var readOnly = parameter.HasCompilerServicesIsReadOnlyAttribute();
                GenericInstanceType bufferOfT = (GenericInstanceType)typeRef;
                TypeReference bufferElementType = bufferOfT.GenericArguments[0];
                var(provider, providerRuntime) = ImportReferencesFor(typeof(LambdaParameterValueProvider_DynamicBuffer<>),
                    withStructuralChanges
                    ? typeof(LambdaParameterValueProvider_DynamicBuffer<>.StructuralChangeRuntime)
                    : typeof(LambdaParameterValueProvider_DynamicBuffer<>.Runtime), bufferElementType);
                return new LambdaParamaterValueProviderInformation(provider, providerRuntime, readOnly, parameter.Name);
            }

            // ISharedComponent
            if (resolvedParameterType.IsISharedComponentData())
            {
                if (lambdaJobDescriptionConstruction.ExecutionMode != ExecutionMode.Run || lambdaJobDescriptionConstruction.UsesBurst)
                    UserError.DC0019(lambdaJobDescriptionConstruction.ContainingMethod, parameter.ParameterType.GetElementType(), lambdaJobDescriptionConstruction.WithCodeInvocationInstruction).Throw();

                if (!parameter.HasCompilerServicesIsReadOnlyAttribute() && parameter.ParameterType.IsByReference)
                {
                    UserError.DC0020(lambdaJobDescriptionConstruction.ContainingMethod, parameter.ParameterType.GetElementType(), lambdaJobDescriptionConstruction.WithCodeInvocationInstruction).Throw();
                }

                var(provider, providerRuntime) = ImportReferencesFor(typeof(LambdaParameterValueProvider_ISharedComponentData<>),
                    withStructuralChanges
                    ? typeof(LambdaParameterValueProvider_ISharedComponentData<>.StructuralChangeRuntime)
                    : typeof(LambdaParameterValueProvider_ISharedComponentData<>.Runtime), parameter.ParameterType.GetElementType());
                var newProvider = new LambdaParamaterValueProviderInformation(provider, providerRuntime, false, parameter.Name, parameter.ParameterType.GetElementType(), withStructuralChanges);

                return newProvider;
            }

            if (resolvedParameterType.TypeReferenceEquals(moduleDefinition.ImportReference(typeof(Entity))))
            {
                var provider = moduleDefinition.ImportReference(typeof(LambdaParameterValueProvider_Entity));
                var runtime = withStructuralChanges
                    ? moduleDefinition.ImportReference(typeof(LambdaParameterValueProvider_Entity.StructuralChangeRuntime))
                    : moduleDefinition.ImportReference(typeof(LambdaParameterValueProvider_Entity.Runtime));

                return new LambdaParamaterValueProviderInformation(provider, runtime, true, parameter.Name, null, withStructuralChanges);
            }

            if (resolvedParameterType.FullName == moduleDefinition.TypeSystem.Int32.FullName)
            {
                var allNames = new[] {"entityInQueryIndex", "nativeThreadIndex"};
                string entityInQueryIndexName = allNames[0];
                string nativeThreadIndexName = allNames[1];

                if (parameter.Name == entityInQueryIndexName)
                {
                    var provider = moduleDefinition.ImportReference(typeof(LambdaParameterValueProvider_EntityInQueryIndex));
                    var runtime = withStructuralChanges
                        ? moduleDefinition.ImportReference(typeof(LambdaParameterValueProvider_EntityInQueryIndex.StructuralChangeRuntime))
                        : moduleDefinition.ImportReference(typeof(LambdaParameterValueProvider_EntityInQueryIndex.Runtime));
                    return new LambdaParamaterValueProviderInformation(provider, runtime, true, parameter.Name);
                }

                if (parameter.Name == nativeThreadIndexName)
                {
                    var provider = moduleDefinition.ImportReference(typeof(LambdaParameterValueProvider_NativeThreadIndex));
                    var runtime = moduleDefinition.ImportReference(typeof(LambdaParameterValueProvider_NativeThreadIndex.Runtime));
#if !UNITY_DOTSPLAYER
                    var isReadonly = true;
#else
                    // Tiny's Job System currently will set the NativeThreadIndex at the beginning of the function to be Bursted.
                    // This will make Burst Compilation fail due to the NativeThreadIndex being marked as Readonly here. So for now
                    // until we workaround this issue in the Tiny Job System, we disable marking NativeThreadIndex as Readonly.
                    var isReadonly = false;
#endif
                    return new LambdaParamaterValueProviderInformation(provider, runtime, isReadonly, parameter.Name);
                }

                UserError.DC0014(lambdaJobDescriptionConstruction.ContainingMethod, lambdaJobDescriptionConstruction.WithCodeInvocationInstruction, parameter, allNames).Throw();
            }

            if (resolvedParameterType.IsIBufferElementData())
                UserError.DC0033(lambdaJobDescriptionConstruction.ContainingMethod, parameter.Name, parameter.ParameterType.GetElementType(), lambdaJobDescriptionConstruction.WithCodeInvocationInstruction).Throw();

            if (!resolvedParameterType.GetElementType().IsPrimitive && resolvedParameterType.GetElementType().IsValueType())
                UserError.DC0021(lambdaJobDescriptionConstruction.ContainingMethod, parameter.Name, parameter.ParameterType.GetElementType(), lambdaJobDescriptionConstruction.WithCodeInvocationInstruction).Throw();

            UserError.DC0005(lambdaJobDescriptionConstruction.ContainingMethod, lambdaJobDescriptionConstruction.WithCodeInvocationInstruction, parameter).Throw();
            return null;
        }
    }
}

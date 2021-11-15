using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.Burst;
using Unity.Collections;
using Unity.Collections.LowLevel.Unsafe;
using Unity.Entities.CodeGeneratedJobForEach;
using FieldAttributes = Mono.Cecil.FieldAttributes;
using MethodAttributes = Mono.Cecil.MethodAttributes;
using ParameterAttributes = Mono.Cecil.ParameterAttributes;
using TypeAttributes = Mono.Cecil.TypeAttributes;

namespace Unity.Entities.CodeGen
{
    internal class LambdaParameterValueInformations
    {
        private LambdaParamaterValueProviderInformation[] _providers;

        Dictionary<string, LambdaParamaterValueProviderInformation> _parameterTypeNameToProvider;
        readonly Dictionary<LambdaParamaterValueProviderInformation, (FieldDefinition elementField, FieldDefinition runtimeField)> _elementProviderToFields = new Dictionary<LambdaParamaterValueProviderInformation, (FieldDefinition elementField, FieldDefinition runtimeField)>();
        TypeDefinition _jobChunkType;
        MethodDefinition _scheduleTimeInitializeMethod;
        TypeDefinition _lambdaParameterValueProvidersType;
        public TypeDefinition RuntimesType { get; private set; }
        MethodDefinition _prepareToExecute;
        FieldDefinition _elementsField;
        public FieldDefinition _runtimesField;
        public FieldDefinition _entityProviderField;
        public LambdaJobDescriptionConstruction _lambdaJobDescriptionConstruction;
        public bool _withStructuralChanges;

        private LambdaParameterValueInformations()
        {
        }

        void InjectIntoJobChunkType()
        {
            _lambdaParameterValueProvidersType = new TypeDefinition("", "LambdaParameterValueProviders", TypeAttributes.NestedPrivate | TypeAttributes.SequentialLayout | TypeAttributes.Sealed, _jobChunkType.Module.ImportReference(typeof(ValueType))) {DeclaringType = _jobChunkType};
            _jobChunkType.NestedTypes.Add(_lambdaParameterValueProvidersType);

            _elementsField = new FieldDefinition("_lambdaParameterValueProviders", FieldAttributes.Private, _lambdaParameterValueProvidersType);
            _jobChunkType.Fields.Add(_elementsField);

            RuntimesType = new TypeDefinition("", "Runtimes", TypeAttributes.NestedPublic | TypeAttributes.SequentialLayout | TypeAttributes.Sealed, _jobChunkType.Module.ImportReference(typeof(ValueType))) { DeclaringType = _lambdaParameterValueProvidersType};
            if (_lambdaJobDescriptionConstruction.UsesBurst && LambdaJobDescriptionConstruction.UsesNoAlias)
                RuntimesType.AddNoAliasAttribute();
            _lambdaParameterValueProvidersType.NestedTypes.Add(RuntimesType);

            _runtimesField = new FieldDefinition("_runtimes", FieldAttributes.Private,
                new PointerType(RuntimesType))
            {
                CustomAttributes =
                {
                    new CustomAttribute(JobStructForLambdaJob.AttributeConstructorReferenceFor(typeof(NativeDisableUnsafePtrRestrictionAttribute), ModuleDefinition))
                }
            };
            _jobChunkType.Fields.Add(_runtimesField);

            if (_withStructuralChanges)
            {
                // We need an entity provider to handle iterating through entities during structural changes
                _entityProviderField = new FieldDefinition("_entityProvider", FieldAttributes.Public, _jobChunkType.Module.ImportReference(typeof(StructuralChangeEntityProvider)));
                RuntimesType.Fields.Add(_entityProviderField);
            }

            int counter = 0;
            foreach (var provider in _providers)
            {
                var parameterFieldDefinition = new FieldDefinition($"forParameter_{provider.Name}", FieldAttributes.Private, provider.Provider);
                if (provider.IsReadOnly)
                {
                    var readOnlyAttributeConstructor = JobStructForLambdaJob.AttributeConstructorReferenceFor(typeof(ReadOnlyAttribute), _jobChunkType.Module);
                    parameterFieldDefinition.CustomAttributes.Add(new CustomAttribute(readOnlyAttributeConstructor));
                }

                _lambdaParameterValueProvidersType.Fields.Add(parameterFieldDefinition);
                bool applyBurstAliasAttribute = _lambdaJobDescriptionConstruction.UsesBurst && LambdaJobDescriptionConstruction.UsesNoAlias;
                if (applyBurstAliasAttribute)
                    parameterFieldDefinition.AddNoAliasAttribute();

                var runtimeFieldDefinition = new FieldDefinition($"runtime_{provider.Name}", FieldAttributes.Public, provider.ProviderRuntime);
                RuntimesType.Fields.Add(runtimeFieldDefinition);
                if (_lambdaJobDescriptionConstruction.UsesBurst && LambdaJobDescriptionConstruction.UsesNoAlias)
                    runtimeFieldDefinition.AddNoAliasAttribute();

                _elementProviderToFields.Add(provider, (parameterFieldDefinition, runtimeFieldDefinition));

                counter++;
            }


            MakeScheduleTimeInitializeMethod();

            if (_withStructuralChanges)
                MakePrepareForStructuralChangesMethod();
            else
                MakePrepareToExecuteOnEntitiesInMethod();
        }

        void MakePrepareForStructuralChangesMethod()
        {
            MethodReference mimickParametersFrom = ModuleDefinition.ImportReference(
                typeof(StructuralChangeEntityProvider).GetMethod(nameof(StructuralChangeEntityProvider.PrepareToExecuteWithStructuralChanges),
                    BindingFlags.Public | BindingFlags.Instance));

            _prepareToExecute = new MethodDefinition("PrepareToExecuteWithStructuralChanges", MethodAttributes.Public, RuntimesType)
            {
                HasThis = true,
            };
            int counter = 0;
            foreach (var parameter in mimickParametersFrom.Parameters)
                _prepareToExecute.Parameters.Add(new ParameterDefinition($"p{counter++}", parameter.Attributes, parameter.ParameterType));

            _lambdaParameterValueProvidersType.Methods.Add(_prepareToExecute);

            var il = _prepareToExecute.Body.GetILProcessor();
            var resultVariable = new VariableDefinition(RuntimesType);
            _prepareToExecute.Body.Variables.Add(resultVariable);
            _prepareToExecute.Body.InitLocals = true; // initlocals must be set for verifiable methods with one or more local variables

            // Initialize the result object.
            il.Emit(OpCodes.Ldloca, resultVariable);
            il.Emit(OpCodes.Initobj, RuntimesType);

            // Prepare our StructuralChangeEntityProvider instance
            il.Emit(OpCodes.Ldloca, resultVariable);
            il.Emit(OpCodes.Ldflda, _entityProviderField);
            il.Emit(OpCodes.Ldarg_1);
            il.Emit(OpCodes.Ldarg_2);
            il.Emit(OpCodes.Call,
                ModuleDefinition.ImportReference(typeof(StructuralChangeEntityProvider).GetMethod(nameof(StructuralChangeEntityProvider.PrepareToExecuteWithStructuralChanges))));

            // Prepare all parameter providers
            foreach (var providerInformation in _providers)
            {
                il.Emit(OpCodes.Ldloca, resultVariable);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldflda, _elementProviderToFields[providerInformation].elementField);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(OpCodes.Ldarg_2);

                il.Emit(OpCodes.Call, providerInformation.PrepareToExecuteWithStructuralChanges);

                // Store in the runtime struct that was already on the stack
                il.Emit(OpCodes.Stfld, _elementProviderToFields[providerInformation].runtimeField);
            }

            il.Emit(OpCodes.Ldloc, resultVariable);
            il.Emit(OpCodes.Ret);
        }

        void MakePrepareToExecuteOnEntitiesInMethod()
        {
            MethodReference mimickParametersFrom = ModuleDefinition.ImportReference(
                typeof(LambdaParameterValueProvider_EntityInQueryIndex).GetMethod(
                    nameof(LambdaParameterValueProvider_EntityInQueryIndex.PrepareToExecuteOnEntitiesIn),
                    BindingFlags.Public | BindingFlags.Instance));

            _prepareToExecute = new MethodDefinition("PrepareToExecuteOnEntitiesInMethod", MethodAttributes.Public, RuntimesType)
            {
                HasThis = true,
            };
            int counter = 0;
            foreach (var parameter in mimickParametersFrom.Parameters)
                _prepareToExecute.Parameters.Add(new ParameterDefinition($"p{counter++}", parameter.Attributes, parameter.ParameterType));

            _lambdaParameterValueProvidersType.Methods.Add(_prepareToExecute);

            var il = _prepareToExecute.Body.GetILProcessor();
            var resultVariable = new VariableDefinition(RuntimesType);
            _prepareToExecute.Body.Variables.Add(resultVariable);
            _prepareToExecute.Body.InitLocals = true; // initlocals must be set for verifiable methods with one or more local variables

            //initialize the result object.
            il.Emit(OpCodes.Ldloca, resultVariable);
            il.Emit(OpCodes.Initobj, RuntimesType);

            foreach (var providerInformation in _providers)
            {
                il.Emit(OpCodes.Ldloca, resultVariable);
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldflda, _elementProviderToFields[providerInformation].elementField);
                il.Emit(OpCodes.Ldarg_1);

                //most PrepareToExecuteOnEntitiesIn take just the chunk, but some take the chunkIndex and firstEntityIndex as well.
                if (!providerInformation.PrepareToExecuteOnEntitiesTakesJustAChunkParameter)
                {
                    il.Emit(OpCodes.Ldarg_2);
                    il.Emit(OpCodes.Ldarg_3);
                }

                il.Emit(OpCodes.Call, providerInformation.ProviderPrepareToExecuteOnEntitiesIn);

                //store in the runtime struct that was already on the stack
                il.Emit(OpCodes.Stfld, _elementProviderToFields[providerInformation].runtimeField);
            }

            il.Emit(OpCodes.Ldloc, resultVariable);
            il.Emit(OpCodes.Ret);
        }

        private void MakeScheduleTimeInitializeMethod()
        {
            _scheduleTimeInitializeMethod = new MethodDefinition("ScheduleTimeInitialize", MethodAttributes.Public, ModuleDefinition.TypeSystem.Void)
            {
                HasThis = true,
                Parameters =
                {
                    new ParameterDefinition("componentSystem", ParameterAttributes.None, _lambdaJobDescriptionConstruction.ContainingMethod.DeclaringType),
                }
            };
            _lambdaParameterValueProvidersType.Methods.Add(_scheduleTimeInitializeMethod);

            var il = _scheduleTimeInitializeMethod.Body.GetILProcessor();
            foreach (var providerInformation in _providers)
            {
                il.Emit(OpCodes.Ldarg_0);
                il.Emit(OpCodes.Ldflda, _elementProviderToFields[providerInformation].elementField);
                il.Emit(OpCodes.Ldarg_1);
                il.Emit(providerInformation.IsReadOnly ? OpCodes.Ldc_I4_1 : OpCodes.Ldc_I4_0);
                il.Emit(OpCodes.Call, providerInformation.ProviderScheduleTimeInitializeMethod);
            }

            il.Emit(OpCodes.Ret);
        }

        private ModuleDefinition ModuleDefinition => _jobChunkType.Module;


        public static LambdaParameterValueInformations For(JobStructForLambdaJob generatedJobStruct, bool withStructuralChanges)
        {
            var lambdaJobDescriptionConstruction = generatedJobStruct.LambdaJobDescriptionConstruction;

            var parameterDefinitions = lambdaJobDescriptionConstruction.MethodLambdaWasEmittedAs.Parameters.ToArray();

            var providerInformations = parameterDefinitions.Select(p =>
                LambdaParamaterValueProviderInformation.ElementProviderInformationFor(lambdaJobDescriptionConstruction, p, withStructuralChanges)).ToList();

            var parameterTypeNameToProvider = new Dictionary<string, LambdaParamaterValueProviderInformation>();
            for (int left = 0; left != providerInformations.Count; left++)
            {
                for (int right = left + 1; right != providerInformations.Count; right++)
                {
                    var leftObject = providerInformations[left];
                    var rightObject = providerInformations[right];

                    if (leftObject == rightObject)
                        continue;

                    if (leftObject.Provider.FullName != rightObject.Provider.FullName)
                        continue;

                    if (!rightObject.IsReadOnly)
                        providerInformations[left] = rightObject;
                    else
                        providerInformations[right] = leftObject;
                }
                parameterTypeNameToProvider[parameterDefinitions[left].ParameterType.FullName] = providerInformations[left];
            }

            var result = new LambdaParameterValueInformations()
            {
                _providers = providerInformations.Distinct().ToArray(),
                _parameterTypeNameToProvider = parameterTypeNameToProvider,
                _jobChunkType = generatedJobStruct.TypeDefinition,
                _lambdaJobDescriptionConstruction = lambdaJobDescriptionConstruction,
                _withStructuralChanges = withStructuralChanges
            };
            result.InjectIntoJobChunkType();
            return result;
        }

        public VariableDefinition EmitInvocationToPrepareToRunOnEntitiesInIntoJobChunkExecute(MethodDefinition executeMethod)
        {
            if (executeMethod.DeclaringType != _jobChunkType)
                throw new ArgumentException();
            var il = executeMethod.Body.GetILProcessor();
            il.Emit(OpCodes.Ldarg_0); //<-- for the stdfld at the end
            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldflda, _elementsField);

            if (_withStructuralChanges)
            {
                il.Emit(OpCodes.Ldarg, 1); // ComponentSystem
                il.Emit(OpCodes.Ldarg, 2); // EntityQuery
            }
            else
            {
                il.Emit(OpCodes.Ldarga, 1); // <- the chunk goes by ref
                il.Emit(OpCodes.Ldarg, 2);
                il.Emit(OpCodes.Ldarg, 3);
            }

            il.Emit(OpCodes.Call, _prepareToExecute);

            var runtimesVariable = new VariableDefinition(RuntimesType);
            il.Body.Variables.Add(runtimesVariable);
            executeMethod.Body.InitLocals = true; // initlocals must be set for verifiable methods with one or more local variables

            il.Emit(OpCodes.Stloc, runtimesVariable);
            il.Emit(OpCodes.Ldloca, runtimesVariable);
            il.Emit(OpCodes.Conv_U);
            il.Emit(OpCodes.Stfld, _runtimesField);

            return runtimesVariable;
        }

        public void EmitInvocationToScheduleTimeInitializeIntoJobChunkScheduleTimeInitialize(MethodDefinition jobChunkScheduleTimeInitialize)
        {
            var il = jobChunkScheduleTimeInitialize.Body.GetILProcessor();

            il.Emit(OpCodes.Ldarg_0);
            il.Emit(OpCodes.Ldflda, _elementsField);

            il.Emit(OpCodes.Ldarg, 1);
            il.Emit(OpCodes.Call, _scheduleTimeInitializeMethod);
        }

        public void EmitILToLoadValueForParameterOnStack(ParameterDefinition parameterDefinition, ILProcessor ilProcessor, VariableDefinition loopCounter)
        {
            var provider = _parameterTypeNameToProvider[parameterDefinition.ParameterType.FullName];

            ilProcessor.Emit(OpCodes.Ldarg_2);

            ilProcessor.Emit(OpCodes.Ldflda, _elementProviderToFields[provider].runtimeField);

            ilProcessor.Emit(OpCodes.Ldloc, loopCounter);
            ilProcessor.Emit(OpCodes.Call, provider.RuntimeForMethod);

            if (provider.RuntimeForMethod.ReturnType.IsByReference &&
                !parameterDefinition.ParameterType.IsByReference)
                ilProcessor.Emit(OpCodes.Ldobj, parameterDefinition.ParameterType);

            if (!provider.RuntimeForMethod.ReturnType.IsByReference && parameterDefinition.ParameterType.IsByReference)
            {
                var tempVariable = new VariableDefinition(provider.RuntimeForMethodReturnType);
                ilProcessor.Body.Variables.Add(tempVariable);
                ilProcessor.Body.InitLocals = true; // initlocals must be set for verifiable methods with one or more local variables
                ilProcessor.Emit(OpCodes.Stloc, tempVariable);
                ilProcessor.Emit(OpCodes.Ldloca, tempVariable);
            }
        }

        public (VariableDefinition, VariableDefinition) EmitILToCreateCopyForParameter_WithStructuralChanges(VariableDefinition runtimesVariable,
            ParameterDefinition parameterDefinition, ILProcessor ilProcessor)
        {
            VariableDefinition addressVariable = null;

            var provider = _parameterTypeNameToProvider[parameterDefinition.ParameterType.FullName];

            ilProcessor.Emit(OpCodes.Ldloc, runtimesVariable);
            ilProcessor.Emit(OpCodes.Ldflda, _elementProviderToFields[provider].runtimeField);

            ilProcessor.Emit(OpCodes.Ldarg_2);

            // In the case where we are getting an IComponentData, we also need to store a copy of the address (to check for changes)
            if (provider.RuntimeForMethod.Parameters.Count == 2)
            {
                addressVariable = new VariableDefinition(provider.RuntimeForMethodReturnType);
                ilProcessor.Body.Variables.Add(addressVariable);
                ilProcessor.Emit(OpCodes.Ldloca, addressVariable);
            }

            ilProcessor.Emit(OpCodes.Call, provider.RuntimeForMethod);

            var tempVariable = new VariableDefinition(provider.RuntimeForMethodReturnType);
            ilProcessor.Body.Variables.Add(tempVariable);
            ilProcessor.Body.InitLocals = true; // initlocals must be set for verifiable methods with one or more local variables
            ilProcessor.Emit(OpCodes.Stloc, tempVariable);

            return (tempVariable, addressVariable);
        }

        public void EmitILToWriteBackParameter_WithStructuralChanges(VariableDefinition runtimesVariable, ParameterDefinition parameterDefinition, ILProcessor ilProcessor,
            VariableDefinition parameterCopyVariableDefinition, VariableDefinition parameterAddressVariableDefinition)
        {
            var provider = _parameterTypeNameToProvider[parameterDefinition.ParameterType.FullName];

            // Only write-back providers that are setup for structural changes
            // and are not ReadOnly
            if (!provider.WithStructuralChanges || provider.RuntimeWriteBackMethod == null || provider.IsReadOnly)
                return;

            ilProcessor.Emit(OpCodes.Ldloc, runtimesVariable);
            ilProcessor.Emit(OpCodes.Ldflda, _elementProviderToFields[provider].runtimeField);

            ilProcessor.Emit(OpCodes.Ldarg_2);

            ilProcessor.Emit(OpCodes.Ldloca, parameterCopyVariableDefinition);

            if (parameterAddressVariableDefinition != null)
                ilProcessor.Emit(OpCodes.Ldloca, parameterAddressVariableDefinition);

            ilProcessor.Emit(OpCodes.Call, provider.RuntimeWriteBackMethod);
        }
    }
}

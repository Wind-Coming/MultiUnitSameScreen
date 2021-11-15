using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using Mono.Cecil;
using Mono.Cecil.Cil;

#if UNITY_DOTSPLAYER
namespace Unity.Entities.CodeGen
{
    /// <summary> Generates code need to run unit tests in ILPP targets without reflection.</summary>
    /// <remarks>
    /// The NUnit test runner runs DOTS-Runtime in the full dotnet framework in order to use reflection
    /// to find and run tests. In any IL2CPP build (windows, mac, mobile, or web) this straight up doesn't
    /// work since there isn't full dotnet support, only the minimal profile.
    ///
    /// This PostProcessor will detect a test framework assembly, scan it, and code-gen calls to test
    /// cases. It is compatible with NUnit, although it only implements a subset of the NUnit functionality.
    ///
    /// </remarks>

    class TestCaseILPP : EntitiesILPostProcessor
    {
        MethodDefinition m_TestRunner;
        MethodReference m_WriteLine;
        FieldDefinition m_TestsRanFld;
        FieldDefinition m_TestsIgnoredFld;
        FieldDefinition m_TestsSkippedFld;
        FieldDefinition m_TestsUnsupportedFld;
        FieldDefinition m_TestsPartiallySupportedFld;

        enum TestStatus
        {
            Okay,
            Ignored,            // [Ignored] in the code, equivalent to skipped.
            Limitation,         // Limitation of test suite.
            NotSupported,       // Test uses unsupported, not cross-platform feature.
            PartiallySupported  // Test case can be run, but not all the asserts in the test case are executed.
        }

        protected override bool PostProcessImpl(TypeDefinition[] componentSystemTypes)
        {
            m_TestRunner = FindCallerMethod();
            if (m_TestRunner == null)
                return false;

            m_WriteLine = AssemblyDefinition.MainModule.ImportReference(typeof(Console).GetMethod("WriteLine", new[] {typeof(string)}));

            // Initially set up the caller.
            m_TestRunner.Body.Instructions.Clear();
            m_TestRunner.Body.InitLocals = true;

            try
            {
                var nUnit = AssemblyDefinition.MainModule.Types.First(t => t.FullName == "NUnit.Framework.Assert");
                m_TestsRanFld = nUnit.Fields.First(f => f.Name == "testsRan");
                m_TestsIgnoredFld = nUnit.Fields.First(f => f.Name == "testsIgnored");
                m_TestsSkippedFld = nUnit.Fields.First(f => f.Name == "testsLimitation");
                m_TestsUnsupportedFld = nUnit.Fields.First(f => f.Name == "testsNotSupported");
                m_TestsPartiallySupportedFld = nUnit.Fields.First(f => f.Name == "testsPartiallySupported");
            }
            catch
            {
                Console.WriteLine($"Failed to find required fields (testsRan, testsIgnored, etc.) of NUnitFrameWork.Assert in the runtime package.");
            }

            foreach (var t in AssemblyDefinition.MainModule.Types)
            {
                if (t.IsClass)
                    ProcessClass(t);
            }

            ILProcessor il = m_TestRunner.Body.GetILProcessor();
            il.Emit(OpCodes.Ret);
            return true;
        }

        protected override bool PostProcessUnmanagedImpl(TypeDefinition[] unmanagedComponentSystemTypes)
        {
            return false;
        }

        MethodDefinition FindCallerMethod()
        {
            TypeDefinition runner = AssemblyDefinition.MainModule.Types.FirstOrDefault(t => t.FullName == "NUnit.Framework.UnitTestRunner");
            MethodDefinition caller = runner?.Methods.First(m => m.Name == "Run");
            return caller;
        }

        static bool HasCustomAttribute(MethodDefinition m, string attributeName)
        {
            var fullAttrName = attributeName + "Attribute";
            var attributes = m.Resolve().CustomAttributes;
            return attributes.FirstOrDefault(ca =>
                ca.AttributeType.FullName == attributeName || ca.AttributeType.FullName == fullAttrName) != null;
        }

        void ProcessClass(TypeDefinition clss)
        {
            List<MethodDefinition> tests = new List<MethodDefinition>();

            foreach (var m in clss.Methods)
            {
                if (HasCustomAttribute(m, "NUnit.Framework.Test"))
                {
                    if (m.ReturnType.MetadataType != MetadataType.Void)
                        throw new Exception($"Test case '{m.FullName}' has non-void return type.");
                    tests.Add(m);
                }
            }

            if (tests.Count > 0)
            {
                var setups = FindSetupTeardown(true, clss);
                var teardowns = FindSetupTeardown(false, clss);

                EmitTestCalls(clss, tests, setups, teardowns);
            }
        }

        // Walks the code to look for [NotSupported]/[PartiallySupported] method calls.
        // Recursive, but some of the Asserts are "buried deep" and hard to find,
        // so the use of Ignore may still be required.
        bool HasTaggedCodeRecursive(MethodReference method, out string msg, HashSet<string> visited, int recDepth, string attr)
        {
            msg = "";

            if (recDepth >= 3)
                return false;

            MethodDefinition methodDefinition = method.Resolve();
            if (methodDefinition == null || methodDefinition.Body == null)
                return false;

            foreach (var bc in methodDefinition.Body.Instructions)
            {
                if (bc.OpCode == OpCodes.Call)
                {
                    MethodReference mr = (MethodReference)bc.Operand;
                    MethodDefinition md = mr.Resolve();

                    if (md != null)
                    {
                        CustomAttribute ca = GetAttributeByFullName(md, attr, out msg);
                        if (ca != null)
                        {
                            return true;
                        }

                        // Limit the search to this module; this search doesn't always work (and in [Ignore] needs
                        // to be manually added) so there's benefit to be faster if only mostly correct.
                        if (method.Module == mr.Module && !visited.Contains(mr.FullName))
                        {
                            visited.Add(mr.FullName);
                            bool result = HasTaggedCodeRecursive(mr, out msg, visited, recDepth + 1, attr);
                            if (result)
                                return true;
                        }
                    }
                }
            }
            return false;
        }

        CustomAttribute GetNotSupportedAttribute(MethodDefinition method, out string msg)
        {
            return GetAttributeByFullName(method, "NUnit.Framework.NotSupportedAttribute", out msg);
        }

        CustomAttribute GetAttributeByFullName(MethodDefinition method, string attr, out string msg)
        {
            msg = "";
            if (method.HasCustomAttributes)
            {
                CustomAttribute ca = method.CustomAttributes.FirstOrDefault(a => a.AttributeType.FullName == attr);
                if (ca != null)
                {
                    msg = (string)ca.ConstructorArguments[0].Value;
                    return ca;
                }
            }

            return null;
        }

        bool HasNotSupportedCode(MethodDefinition method, out string msg)
        {
            msg = "";
            HashSet<string> visited = new HashSet<string>();
            return HasTaggedCodeRecursive(method, out msg, visited, 0, "NUnit.Framework.NotSupportedAttribute");
        }

        bool HasPartiallySupportedCode(MethodDefinition method, out string msg)
        {
            msg = "";
            HashSet<string> visited = new HashSet<string>();
            return HasTaggedCodeRecursive(method, out msg, visited, 0, "NUnit.Framework.PartiallySupportedAttribute");
        }

        bool IsIgnored(MethodDefinition method)
        {
            foreach (var attr in method.CustomAttributes)
            {
                var type = attr.AttributeType;
                while (type != null)
                {
                    if (type.Name == "IgnoreAttribute")
                        return true;

                    // TODO: may choose to support in the future.
                    // But for now - since there is no command line interface yet - same as [Ignore]
                    if (type.Name == "ExplicitAttribute")
                        return true;

                    type = type.Resolve().BaseType;
                }
            }

            return false;
        }

        void EmitIncStaticFld(ILProcessor il, FieldDefinition fld)
        {
            il.Emit(OpCodes.Ldsfld, fld);
            il.Emit(OpCodes.Ldc_I4_1);
            il.Emit(OpCodes.Add);
            il.Emit(OpCodes.Stsfld, fld);
        }

        // DeconstructParameters walks the [Values] attribute on the test arguments, and
        // pulls out all the possible values. For example:
        //
        // Test([Values(0, 1) int a, [Values] bool b) {}
        //
        // Would create this list of objects:
        // [ [0, 1], [true, false]
        //
        // ParametersToILRecursive then converts the List<List<object>> into IL code.
        List<List<object>> DeconstructParameters(MethodDefinition testMethod)
        {
            List<List<object>> paramList = new List<List<object>>();

            foreach (var p in testMethod.Parameters)
            {
                var list = new List<object>();
                paramList.Add(list);

                var customAttributeCollection = p.CustomAttributes;
                CustomAttribute customAttribute = customAttributeCollection[0];
                if (customAttribute.HasConstructorArguments)
                {
                    var ctorAttr = customAttribute.ConstructorArguments[0];
                    if (ctorAttr.Type.IsArray)
                    {
                        CustomAttributeArgument[] caa = (CustomAttributeArgument[])ctorAttr.Value;
                        foreach (var arg in caa)
                        {
                            list.Add(arg.Value);
                        }
                    }
                }
                else if (p.ParameterType.MetadataType == MetadataType.Boolean)
                {
                    list.Add(true);
                    list.Add(false);
                }
            }

            return paramList;
        }

        // See DeconstructParameters.
        // Given a List<List<object>>, convert that to IL code that is all the parameter combinations
        // for a given test method: basically, it is a List of LDC_I4 in every possible combination.
        void ParametersToILRecursive(int depth, List<List<object>> paramList,
            List<List<Instruction>> instructions, List<Instruction> instructionStack,
            List<string> logs, List<string> logStack)
        {
            if (depth == paramList.Count)
            {
                instructions.Add(new List<Instruction>(instructionStack));

                StringBuilder builder = new StringBuilder();
                builder.Append("(");
                for(int i=0; i<logStack.Count; ++i)
                {
                    if (i > 0)
                        builder.Append(", ");
                    builder.Append(logStack[i]);
                }

                builder.Append(")");
                logs.Add(builder.ToString());

                return;
            }

            foreach (var obj in paramList[depth])
            {
                switch (obj)
                {
                    case byte ui1:
                    case char i1:
                    case short i2:
                    case ushort ui2:
                    case int i4:
                    case uint ui4:
                        instructionStack.Add(Instruction.Create(OpCodes.Ldc_I4, (int)obj));
                        break;

                    case long i8:
                    case ulong ui8:
                        instructionStack.Add(Instruction.Create(OpCodes.Ldc_I8, (uint)obj));
                        break;

                    case float f4:
                        instructionStack.Add(Instruction.Create(OpCodes.Ldc_R4, f4));
                        break;

                    case double f8:
                        instructionStack.Add(Instruction.Create(OpCodes.Ldc_R8, f8));
                        break;

                    case bool b:
                        instructionStack.Add(Instruction.Create(OpCodes.Ldc_I4, b ? 1 : 0));
                        break;

                    default:
                        // The name of the test case is prepended in the log.
                        throw new ArgumentException($"The portable test runner is missing support for {obj.GetType()} [Values] attribute.");
                }

                logStack.Add(obj.ToString());

                ParametersToILRecursive(depth + 1, paramList, instructions, instructionStack, logs, logStack);
                instructionStack.RemoveAt(instructionStack.Count - 1);
                logStack.RemoveAt(logStack.Count - 1);
            }
        }

        // For a give test method, determine all the combinations of [Values] that are present, and generate
        // the IL to push onto the stack before the call.
        // If the method has no [Value] attribute, empty IL and logs.
        void GenerateParameterIL(MethodDefinition method, List<List<Instruction>> instructions, List<string> logs)
        {
            if (method.Parameters.Count == 0)
            {
                instructions.Add(new List<Instruction>());
                logs.Add("");
            }
            else
            {
                List<List<object>> values = DeconstructParameters(method);
                List<string> logStack = new List<string>();
                List<Instruction> instructionStack = new List<Instruction>();
                ParametersToILRecursive(0, values, instructions, instructionStack, logs, logStack);
            }
        }

        bool RunTest(TestStatus status)
        {
            return status == TestStatus.Okay || status == TestStatus.PartiallySupported;
        }

        void EmitTestCalls(TypeDefinition clss, List<MethodDefinition> tests, List<MethodDefinition> setup, List<MethodDefinition> teardown)
        {
            var ctor = clss.Methods.FirstOrDefault(m => m.Name == ".ctor" && m.Parameters.Count == 0);
            if (ctor == null)
                throw new Exception($"Test class '{clss.FullName}' doesn't have a default constructor.");

            var classLocal = new VariableDefinition(AssemblyDefinition.MainModule.ImportReference(clss));
            m_TestRunner.Body.Variables.Add(classLocal);
            var il = m_TestRunner.Body.GetILProcessor();

            // Create the test suite
            il.Emit(OpCodes.Newobj, ctor);
            il.Emit(OpCodes.Stloc, classLocal);

            foreach(var testMethod in tests)
            {
                string skipMsg = null;
                string msg = "";
                TestStatus status = TestStatus.Okay;

                var valueILList = new List<List<Instruction>>();
                var valueLogList = new List<string>();
                try
                {
                    GenerateParameterIL(testMethod, valueILList, valueLogList);
                }
                catch (Exception e)
                {
                    status = TestStatus.Limitation;
                    skipMsg = e.ToString();
                }

                // Note that the order of these 'if' cases is important; NotSupported skips the tests,
                // while PartiallySupported can still run it.
                //
                if (status != TestStatus.Okay)
                {
                    // Pre-processing failed; do no more.
                }
                else if (GetNotSupportedAttribute(testMethod, out msg) != null)
                {
                    skipMsg = msg;
                    status = TestStatus.NotSupported;
                }
                else if (HasNotSupportedCode(testMethod, out msg))
                {
                    skipMsg = msg;
                    status = TestStatus.NotSupported;
                }
                else if (IsIgnored(testMethod))
                {
                    skipMsg = "";
                    status = TestStatus.Ignored;
                }
                else if (HasPartiallySupportedCode(testMethod, out msg))
                {
                    // Make sure "partial" is the last check - check first that we aren't unsupported, etc.
                    skipMsg = msg;
                    status = TestStatus.PartiallySupported;
                }

                switch (status)
                {
                    case TestStatus.Limitation:
                        il.Emit(OpCodes.Ldstr, $"[Limitation]   '{testMethod.FullName}' {skipMsg}");
                        il.Emit(OpCodes.Call, m_WriteLine);
                        EmitIncStaticFld(il, m_TestsSkippedFld);
                        break;

                    case TestStatus.NotSupported:
                        il.Emit(OpCodes.Ldstr, $"[NotSupported] '{testMethod.FullName}' {skipMsg}");
                        il.Emit(OpCodes.Call, m_WriteLine);
                        EmitIncStaticFld(il, m_TestsUnsupportedFld);
                        break;

                    case TestStatus.Ignored:
                        il.Emit(OpCodes.Ldstr, $"[Ignored]      '{testMethod.FullName}' {skipMsg}");
                        il.Emit(OpCodes.Call, m_WriteLine);
                        EmitIncStaticFld(il, m_TestsIgnoredFld);
                        break;

                    case TestStatus.PartiallySupported:
                        EmitIncStaticFld(il, m_TestsPartiallySupportedFld);
                        break;

                    default:
                        break;
                }

                if (RunTest(status))
                {
                    const int RETURN_HEADER_LEN = 12;
                    string logMsg = testMethod.FullName.Substring(RETURN_HEADER_LEN);

                    if (status == TestStatus.PartiallySupported)
                        logMsg = "[Partial]      " + logMsg + " " + skipMsg;

                    for (int i = 0; i < valueILList.Count; i++)
                    {
                        var valueIL = valueILList[i];
                        var valueLog = valueLogList[i];

                        il.Emit(OpCodes.Ldstr, logMsg + valueLog);
                        il.Emit(OpCodes.Call, m_WriteLine);

                        foreach (var setupCall in setup)
                        {
                            il.Emit(OpCodes.Ldloc, classLocal);
                            il.Emit(OpCodes.Call, setupCall);
                        }

                        il.Emit(OpCodes.Ldloc, classLocal);

                        il.Append(valueIL);
                        il.Emit(OpCodes.Call, testMethod);

                        foreach (var teardownCall in teardown)
                        {
                            il.Emit(OpCodes.Ldloc, classLocal);
                            il.Emit(OpCodes.Call, teardownCall);
                        }

                        EmitIncStaticFld(il, m_TestsRanFld);
                    }
                }
            }
        }


        /// <summary>
        /// NUnit uses [SetUp] and [TearDown] attributes to denote methods that should run before and after methods with a [Test] attribute.
        /// NUnit does a few things that we need to specifically handle:
        ///  1. NUnit treats [SetUp]/[TearDown] attributes as if they are inherited. This means a child class can override a parent method without explicitly adding either attribute.
        ///     Importantly since the user must override the method explicitly, we do not call SetUp/TearDown methods twice in this case
        ///  2. NUnit allows more than one SetUp/TearDown method, and will invoke such methods in the following order:
        ///     {BaseSetups}, {DerivedSetups}, {TestMethod}, {DerivedTearDowns}, {BaseTearDowns}
        ///     Within a given set, the order of Setup/Teardown methods can be random.
        /// </summary>
        /// <param name="setup"></param>
        /// <param name="clss"></param>
        /// <returns></returns>
        List<MethodDefinition> FindSetupTeardown(bool setup, TypeDefinition clss)
        {
            var list = new List<MethodDefinition>();
            var methodSet = new HashSet<MethodDefinition>();
            var overriddenMethods = new Dictionary<string, MethodDefinition>();

            while (clss != null)
            {
                foreach (var method in clss.Methods)
                {
                    // TearDown and SetupMethods must take no parameters
                    // Todo: We don't support static setup methods (NUnit 2.5 feature)
                    if (method.HasParameters || method.IsStatic)
                        continue;

                    if (HasCustomAttribute(method, setup ? "NUnit.Framework.SetUp" : "NUnit.Framework.TearDown"))
                    {
                        if (overriddenMethods.TryGetValue(method.Name, out var methodOverride))
                            methodSet.Add(methodOverride);
                        else
                            methodSet.Add(method);
                    }

                    if (method.IsVirtual && !method.IsNewSlot)
                    {
                        overriddenMethods.Add(method.Name, method);
                    }
                }

                clss = clss.BaseType?.Resolve();
            }

            list = methodSet.ToList();

            if (setup)
            {
                // SetUp: Base first, then derived.
                // TearDown: Derived first, then base
                list.Reverse();
            }

            return list;
        }
    }
}

#endif // UNITY_DOTSPLAYER

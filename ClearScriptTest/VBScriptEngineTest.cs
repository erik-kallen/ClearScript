﻿// 
// Copyright © Microsoft Corporation. All rights reserved.
// 
// Microsoft Public License (MS-PL)
// 
// This license governs use of the accompanying software. If you use the
// software, you accept this license. If you do not accept the license, do not
// use the software.
// 
// 1. Definitions
// 
//   The terms "reproduce," "reproduction," "derivative works," and
//   "distribution" have the same meaning here as under U.S. copyright law. A
//   "contribution" is the original software, or any additions or changes to
//   the software. A "contributor" is any person that distributes its
//   contribution under this license. "Licensed patents" are a contributor's
//   patent claims that read directly on its contribution.
// 
// 2. Grant of Rights
// 
//   (A) Copyright Grant- Subject to the terms of this license, including the
//       license conditions and limitations in section 3, each contributor
//       grants you a non-exclusive, worldwide, royalty-free copyright license
//       to reproduce its contribution, prepare derivative works of its
//       contribution, and distribute its contribution or any derivative works
//       that you create.
// 
//   (B) Patent Grant- Subject to the terms of this license, including the
//       license conditions and limitations in section 3, each contributor
//       grants you a non-exclusive, worldwide, royalty-free license under its
//       licensed patents to make, have made, use, sell, offer for sale,
//       import, and/or otherwise dispose of its contribution in the software
//       or derivative works of the contribution in the software.
// 
// 3. Conditions and Limitations
// 
//   (A) No Trademark License- This license does not grant you rights to use
//       any contributors' name, logo, or trademarks.
// 
//   (B) If you bring a patent claim against any contributor over patents that
//       you claim are infringed by the software, your patent license from such
//       contributor to the software ends automatically.
// 
//   (C) If you distribute any portion of the software, you must retain all
//       copyright, patent, trademark, and attribution notices that are present
//       in the software.
// 
//   (D) If you distribute any portion of the software in source code form, you
//       may do so only under this license by including a complete copy of this
//       license with your distribution. If you distribute any portion of the
//       software in compiled or object code form, you may only do so under a
//       license that complies with this license.
// 
//   (E) The software is licensed "as-is." You bear the risk of using it. The
//       contributors give no express warranties, guarantees or conditions. You
//       may have additional consumer rights under your local laws which this
//       license cannot change. To the extent permitted under your local laws,
//       the contributors exclude the implied warranties of merchantability,
//       fitness for a particular purpose and non-infringement.
//       

using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.ClearScript.Util;
using Microsoft.ClearScript.Windows;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Microsoft.ClearScript.Test
{
    [TestClass]
    [SuppressMessage("Microsoft.Design", "CA1001:TypesThatOwnDisposableFieldsShouldBeDisposable", Justification = "Test classes use TestCleanupAttribute for deterministic teardown.")]
    public class VBScriptEngineTest : ClearScriptTest
    {
        #region setup / teardown

        private VBScriptEngine engine;

        [TestInitialize]
        public void TestInitialize()
        {
            engine = new VBScriptEngine(WindowsScriptEngineFlags.EnableDebugging);
            engine.Execute("function pi : pi = 4 * atn(1) : end function");
            engine.Execute("function e : e = exp(1) : end function");
        }

        [TestCleanup]
        public void TestCleanup()
        {
            engine.Dispose();
        }

        #endregion

        #region test methods

        // ReSharper disable InconsistentNaming

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_AddHostObject()
        {
            var host = new HostFunctions();
            engine.AddHostObject("host", host);
            Assert.AreSame(host, engine.Evaluate("host"));
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        [ExpectedException(typeof(InvalidOperationException))]
        public void VBScriptEngine_AddHostObject_Scalar()
        {
            engine.AddHostObject("value", 123);
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_AddHostObject_Enum()
        {
            const DayOfWeek value = DayOfWeek.Wednesday;
            engine.AddHostObject("value", value);
            Assert.AreEqual(value, engine.Evaluate("value"));
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_AddHostObject_Struct()
        {
            var date = new DateTime(2007, 5, 22, 6, 15, 43);
            engine.AddHostObject("date", date);
            Assert.AreEqual(date, engine.Evaluate("date"));
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_AddHostObject_GlobalMembers()
        {
            var host = new HostFunctions();
            engine.AddHostObject("host", HostItemFlags.GlobalMembers, host);
            Assert.IsInstanceOfType(engine.Evaluate("newObj()"), typeof(PropertyBag));
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        [ExpectedException(typeof(ExternalException))]
        public void VBScriptEngine_AddHostObject_DefaultAccess()
        {
            engine.AddHostObject("test", this);
            engine.Execute("test.PrivateMethod()");
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_AddHostObject_PrivateAccess()
        {
            engine.AddHostObject("test", HostItemFlags.PrivateAccess, this);
            engine.Execute("test.PrivateMethod()");
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_AddHostType()
        {
            engine.AddHostObject("host", new HostFunctions());
            engine.AddHostType("Random", typeof(Random));
            Assert.IsInstanceOfType(engine.Evaluate("host.newObj(Random)"), typeof(Random));
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_AddHostType_GlobalMembers()
        {
            engine.AddHostType("Guid", HostItemFlags.GlobalMembers, typeof(Guid));
            Assert.IsInstanceOfType(engine.Evaluate("NewGuid()"), typeof(Guid));
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        [ExpectedException(typeof(ExternalException))]
        public void VBScriptEngine_AddHostType_DefaultAccess()
        {
            engine.AddHostType("Test", GetType());
            engine.Execute("Test.PrivateStaticMethod()");
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_AddHostType_PrivateAccess()
        {
            engine.AddHostType("Test", HostItemFlags.PrivateAccess, GetType());
            engine.Execute("Test.PrivateStaticMethod()");
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_AddHostType_Static()
        {
            engine.AddHostType("Enumerable", typeof(Enumerable));
            Assert.IsInstanceOfType(engine.Evaluate("Enumerable.Range(0, 5).ToArray()"), typeof(int[]));
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_AddHostType_OpenGeneric()
        {
            engine.AddHostObject("host", new HostFunctions());
            engine.AddHostType("List", typeof(List<>));
            engine.AddHostType("Guid", typeof(Guid));
            Assert.IsInstanceOfType(engine.Evaluate("host.newObj(List(Guid))"), typeof(List<Guid>));
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_AddHostType_ByName()
        {
            engine.AddHostObject("host", new HostFunctions());
            engine.AddHostType("Random", "System.Random");
            Assert.IsInstanceOfType(engine.Evaluate("host.newObj(Random)"), typeof(Random));
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_AddHostType_ByNameWithAssembly()
        {
            engine.AddHostType("Enumerable", "System.Linq.Enumerable", "System.Core");
            Assert.IsInstanceOfType(engine.Evaluate("Enumerable.Range(0, 5).ToArray()"), typeof(int[]));
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_AddHostType_ByNameWithTypeArgs()
        {
            engine.AddHostObject("host", new HostFunctions());
            engine.AddHostType("Dictionary", "System.Collections.Generic.Dictionary", typeof(string), typeof(int));
            Assert.IsInstanceOfType(engine.Evaluate("host.newObj(Dictionary)"), typeof(Dictionary<string, int>));
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_Evaluate()
        {
            Assert.AreEqual(Math.E * Math.PI, engine.Evaluate("e * pi"));
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_Evaluate_Array()
        {
            // ReSharper disable ImplicitlyCapturedClosure

            var lengths = new[] { 3, 5, 7 };
            var formatParams = string.Join(", ", Enumerable.Range(0, lengths.Length).Select(position => "{" + position + "}"));

            var hosts = Array.CreateInstance(typeof(object), lengths);
            hosts.Iterate(indices => hosts.SetValue(new HostFunctions(), indices));
            engine.AddHostObject("hostArray", hosts);

            engine.Execute(MiscHelpers.FormatInvariant("dim hosts(" + formatParams + ")", lengths.Select(length => (object)(length - 1)).ToArray()));
            hosts.Iterate(indices => engine.Execute(MiscHelpers.FormatInvariant("set hosts(" + formatParams + ") = hostArray.GetValue(" + formatParams + ")", indices.Select(index => (object)index).ToArray())));
            hosts.Iterate(indices => Assert.AreSame(hosts.GetValue(indices), engine.Evaluate(MiscHelpers.FormatInvariant("hosts(" + formatParams + ")", indices.Select(index => (object)index).ToArray()))));

            var result = engine.Evaluate("hosts");
            Assert.IsInstanceOfType(result, typeof(object[,,]));
            var hostArray = (object[,,])result;
            hosts.Iterate(indices => Assert.AreSame(hosts.GetValue(indices), hostArray.GetValue(indices)));

            // ReSharper restore ImplicitlyCapturedClosure
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_Evaluate_WithDocumentName()
        {
            const string documentName = "DoTheMath";
            Assert.AreEqual(Math.E * Math.PI, engine.Evaluate(documentName, "e * pi"));
            Assert.IsFalse(engine.GetDebugDocumentNames().Any(name => name.StartsWith(documentName, StringComparison.Ordinal)));
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_Evaluate_DiscardDocument()
        {
            const string documentName = "DoTheMath";
            Assert.AreEqual(Math.E * Math.PI, engine.Evaluate(documentName, true, "e * pi"));
            Assert.IsFalse(engine.GetDebugDocumentNames().Any(name => name.StartsWith(documentName, StringComparison.Ordinal)));
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_Evaluate_RetainDocument()
        {
            const string documentName = "DoTheMath";
            Assert.AreEqual(Math.E * Math.PI, engine.Evaluate(documentName, false, "e * pi"));
            Assert.IsTrue(engine.GetDebugDocumentNames().Any(name => name.StartsWith(documentName, StringComparison.Ordinal)));
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_Execute()
        {
            engine.Execute("epi = e * pi");
            Assert.AreEqual(Math.E * Math.PI, engine.Script.epi);
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_Execute_WithDocumentName()
        {
            const string documentName = "DoTheMath";
            engine.Execute(documentName, "epi = e * pi");
            Assert.AreEqual(Math.E * Math.PI, engine.Script.epi);
            Assert.IsTrue(engine.GetDebugDocumentNames().Any(name => name.StartsWith(documentName, StringComparison.Ordinal)));
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_Execute_DiscardDocument()
        {
            const string documentName = "DoTheMath";
            engine.Execute(documentName, true, "epi = e * pi");
            Assert.AreEqual(Math.E * Math.PI, engine.Script.epi);
            Assert.IsFalse(engine.GetDebugDocumentNames().Any(name => name.StartsWith(documentName, StringComparison.Ordinal)));
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_Execute_RetainDocument()
        {
            const string documentName = "DoTheMath";
            engine.Execute(documentName, false, "epi = e * pi");
            Assert.AreEqual(Math.E * Math.PI, engine.Script.epi);
            Assert.IsTrue(engine.GetDebugDocumentNames().Any(name => name.StartsWith(documentName, StringComparison.Ordinal)));
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_ExecuteCommand_EngineConvert()
        {
            Assert.AreEqual("[ScriptObject:EngineInternalImpl]", engine.ExecuteCommand("eval EngineInternal"));
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_ExecuteCommand_HostConvert()
        {
            var dateHostItem = HostItem.Wrap(engine, new DateTime(2007, 5, 22, 6, 15, 43));
            engine.AddHostObject("date", dateHostItem);
            Assert.AreEqual(dateHostItem.ToString(), engine.ExecuteCommand("eval date"));
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_Interrupt()
        {
            var checkpoint = new ManualResetEvent(false);
            ThreadPool.QueueUserWorkItem(state =>
            {
                checkpoint.WaitOne();
                engine.Interrupt();
            });

            engine.AddHostObject("checkpoint", checkpoint);

            var gotException = false;
            try
            {
                engine.Execute("call checkpoint.Set() : while true : foo = \"hello\" : wend");
            }
            catch (OperationCanceledException)
            {
                gotException = true;
            }

            Assert.IsTrue(gotException);
            Assert.AreEqual(Math.E * Math.PI, engine.Evaluate("e * pi"));
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        [ExpectedException(typeof(ExternalException))]
        public void VBScriptEngine_AccessContext_Default()
        {
            engine.AddHostObject("test", this);
            engine.Execute("test.PrivateMethod()");
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_AccessContext_Private()
        {
            engine.AddHostObject("test", this);
            engine.AccessContext = GetType();
            engine.Execute("test.PrivateMethod()");
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_ContinuationCallback()
        {
            engine.ContinuationCallback = () => false;

            var gotException = false;
            try
            {
                engine.Execute("while true : foo = \"hello\" : wend");
            }
            catch (OperationCanceledException)
            {
                gotException = true;
            }

            Assert.IsTrue(gotException);

            engine.ContinuationCallback = null;
            Assert.AreEqual(Math.E * Math.PI, engine.Evaluate("e * pi"));
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_FileNameExtension()
        {
            Assert.AreEqual("vbs", engine.FileNameExtension);
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_Script_Variable()
        {
            var host = new HostFunctions();
            engine.Script.host = host;
            Assert.AreSame(host, engine.Script.host);
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_Script_Variable_Scalar()
        {
            const int value = 123;
            engine.Script.value = value;
            Assert.AreEqual(value, engine.Script.value);
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_Script_Variable_Enum()
        {
            const DayOfWeek value = DayOfWeek.Wednesday;
            engine.Script.value = value;
            Assert.AreEqual(value, engine.Script.value);
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_Script_Array()
        {
            // ReSharper disable ImplicitlyCapturedClosure

            var lengths = new[] { 3, 5, 7 };
            var formatParams = string.Join(", ", Enumerable.Range(0, lengths.Length).Select(position => "{" + position + "}"));

            var hosts = Array.CreateInstance(typeof(object), lengths);
            hosts.Iterate(indices => hosts.SetValue(new HostFunctions(), indices));
            engine.Script.hostArray = hosts;

            engine.Execute(MiscHelpers.FormatInvariant("dim hosts(" + formatParams + ")", lengths.Select(length => (object)(length - 1)).ToArray()));
            hosts.Iterate(indices => engine.Execute(MiscHelpers.FormatInvariant("set hosts(" + formatParams + ") = hostArray.GetValue(" + formatParams + ")", indices.Select(index => (object)index).ToArray())));
            hosts.Iterate(indices => Assert.AreSame(hosts.GetValue(indices), engine.Script.hosts.GetValue(indices)));

            var result = engine.Script.hosts;
            Assert.IsInstanceOfType(result, typeof(object[,,]));
            var hostArray = (object[,,])result;
            hosts.Iterate(indices => Assert.AreSame(hosts.GetValue(indices), hostArray.GetValue(indices)));

            // ReSharper restore ImplicitlyCapturedClosure
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_Script_Variable_Struct()
        {
            var stamp = new DateTime(2007, 5, 22, 6, 15, 43);
            engine.Script.stamp = stamp;
            Assert.AreEqual(stamp, engine.Script.stamp);
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_Script_Function()
        {
            engine.Execute("function test(x, y) : test = x * y : end function");
            Assert.AreEqual(Math.E * Math.PI, engine.Script.test(Math.E, Math.PI));
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_Script_Sub()
        {
            var callbackInvoked = false;
            Action callback = () => callbackInvoked = true;
            engine.Execute("sub test(x) : call x() : end sub");
            engine.Script.test(callback);
            Assert.IsTrue(callbackInvoked);
        }

        [TestMethod, TestCategory("VBScriptEngine")]
        public void VBScriptEngine_General()
        {
            using (var console = new StringWriter())
            {
                var clr = new HostTypeCollection(type => type != typeof(Console), "mscorlib", "System", "System.Core");
                clr.GetNamespaceNode("System").SetPropertyNoCheck("Console", console);

                engine.AddHostObject("host", new ExtendedHostFunctions());
                engine.AddHostObject("clr", clr);

                engine.Execute(generalScript);
                Assert.AreEqual(MiscHelpers.FormatCode(generalScriptOutput), console.ToString().Replace("\r\n", "\n"));
            }
        }

        // ReSharper restore InconsistentNaming

        #endregion

        #region miscellaneous

        private const string generalScript =
        @"
            set System = clr.System

            set TestObject = host.type(""Microsoft.ClearScript.Test.GeneralTestObject"", ""ClearScriptTest"")
            set tlist = host.newObj(System.Collections.Generic.List(TestObject))
            call tlist.Add(host.newObj(TestObject, ""Eóin"", 20))
            call tlist.Add(host.newObj(TestObject, ""Shane"", 16))
            call tlist.Add(host.newObj(TestObject, ""Cillian"", 8))
            call tlist.Add(host.newObj(TestObject, ""Sasha"", 6))
            call tlist.Add(host.newObj(TestObject, ""Brian"", 3))

            class VBTestObject
               public name
               public age
            end class

            function createTestObject(name, age)
               dim testObject
               set testObject = new VBTestObject
               testObject.name = name
               testObject.age = age
               set createTestObject = testObject
            end function

            set olist = host.newObj(System.Collections.Generic.List(System.Object))
            call olist.Add(createTestObject(""Brian"", 3))
            call olist.Add(createTestObject(""Sasha"", 6))
            call olist.Add(createTestObject(""Cillian"", 8))
            call olist.Add(createTestObject(""Shane"", 16))
            call olist.Add(createTestObject(""Eóin"", 20))

            set dict = host.newObj(System.Collections.Generic.Dictionary(System.String, System.String))
            call dict.Add(""foo"", ""bar"")
            call dict.Add(""baz"", ""qux"")
            set value = host.newVar(System.String)
            result = dict.TryGetValue(""foo"", value.out)

            set expando = host.newObj(System.Dynamic.ExpandoObject)
            set expandoCollection = host.cast(System.Collections.Generic.ICollection(System.Collections.Generic.KeyValuePair(System.String, System.Object)), expando)

            set onEventRef = GetRef(""onEvent"")
            sub onEvent(s, e)
                call System.Console.WriteLine(""Property changed: {0}; new value: {1}"", e.PropertyName, eval(""s."" + e.PropertyName))
            end sub

            set onStaticEventRef = GetRef(""onStaticEvent"")
            sub onStaticEvent(s, e)
                call System.Console.WriteLine(""Property changed: {0}; new value: {1} (static event)"", e.PropertyName, e.PropertyValue)
            end sub

            set eventCookie = tlist.Item(0).Change.connect(onEventRef)
            set staticEventCookie = TestObject.StaticChange.connect(onStaticEventRef)
            tlist.Item(0).Name = ""Jerry""
            tlist.Item(1).Name = ""Ellis""
            tlist.Item(0).Name = ""Eóin""
            tlist.Item(1).Name = ""Shane""

            call eventCookie.disconnect()
            call staticEventCookie.disconnect()
            tlist.Item(0).Name = ""Jerry""
            tlist.Item(1).Name = ""Ellis""
            tlist.Item(0).Name = ""Eóin""
            tlist.Item(1).Name = ""Shane""
        ";

        private const string generalScriptOutput =
        @"
            Property changed: Name; new value: Jerry
            Property changed: Name; new value: Jerry (static event)
            Property changed: Name; new value: Ellis (static event)
            Property changed: Name; new value: Eóin
            Property changed: Name; new value: Eóin (static event)
            Property changed: Name; new value: Shane (static event)
        ";

        // ReSharper disable UnusedMember.Local

        private void PrivateMethod()
        {
        }

        private static void PrivateStaticMethod()
        {
        }

        // ReSharper restore UnusedMember.Local

        #endregion
    }
}

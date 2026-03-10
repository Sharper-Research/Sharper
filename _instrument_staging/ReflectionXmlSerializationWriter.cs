// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Reflection;
using System.Text;
using System.Xml.Schema;

namespace System.Xml.Serialization
{
    [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
    [RequiresDynamicCode(XmlSerializer.AotSerializationWarning)]
    internal sealed class ReflectionXmlSerializationWriter : XmlSerializationWriter
    {
        private readonly XmlMapping _mapping;

        public ReflectionXmlSerializationWriter(XmlMapping xmlMapping, XmlWriter xmlWriter, XmlSerializerNamespaces namespaces, string? encodingStyle, string? id)
        {
global::AutoRepairTracing.TraceLogger.Log("9cc6f3ec-8841-4515-a12b-529da051eadf", "ReflectionXmlSerializationWriter.cs", 21, "Enter Method 'ReflectionXmlSerializationWriter'");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("6d6816d7-d73e-425b-852e-d43fa783a61b", "ReflectionXmlSerializationWriter.cs", 23, "Init", 0, xmlWriter, "xmlWriter");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("41957445-c009-4981-92af-471b96ba344f", "ReflectionXmlSerializationWriter.cs", 23, "Init", 1, namespaces, "namespaces");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("c223f801-7429-49db-b3c3-d4daf23c6fa5", "ReflectionXmlSerializationWriter.cs", 23, "Init", 2, encodingStyle, "encodingStyle");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("e5f9eec5-8278-43db-96bc-5de7c769d44b", "ReflectionXmlSerializationWriter.cs", 23, "Init", 3, id, "id");
            Init(xmlWriter, namespaces, encodingStyle, id);
}
            if (!xmlMapping.IsWriteable || !xmlMapping.GenerateSerializer)
            {
global::AutoRepairTracing.TraceLogger.Log("07d54f07-85ab-4b93-91f8-30f0a83e17c1", "ReflectionXmlSerializationWriter.cs", 26, "If-True Branch");
                throw new ArgumentException(SR.XmlInternalError);
            }

            if (xmlMapping is XmlTypeMapping || xmlMapping is XmlMembersMapping)
            {
global::AutoRepairTracing.TraceLogger.Log("25aaf29b-11a1-4b22-a4e5-8907d2330d6f", "ReflectionXmlSerializationWriter.cs", 31, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("d7f453c4-bd96-4ab5-9228-73f33561532d", "ReflectionXmlSerializationWriter.cs", 39, "Variable Update", _mapping = xmlMapping, "_mapping");
            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("1560a18f-acf9-4c7e-9edf-ab66fcbaf560", "ReflectionXmlSerializationWriter.cs", 35, "Else Branch");
                throw new ArgumentException(SR.XmlInternalError);
            }
        }

        protected override void InitCallbacks()
{global::AutoRepairTracing.TraceLogger.Log("45c37fb8-a7b0-40a4-89c3-ccb7afdb66ad", "ReflectionXmlSerializationWriter.cs", 40, "Enter Method 'InitCallbacks'");
try        {
            TypeScope scope = _mapping.Scope!;
global::AutoRepairTracing.TraceLogger.Log("353639ec-878d-4897-9755-a781e7fdf76b", "ReflectionXmlSerializationWriter.cs", 42, "Variable Update", scope, "scope");
            foreach (TypeMapping mapping in scope.TypeMappings)
            {
global::AutoRepairTracing.TraceLogger.Log("3d43705a-ac52-4c9c-9b52-5bcff77b74c0", "ReflectionXmlSerializationWriter.cs", 43, "ForEach Loop");
                if (mapping.IsSoap &&
                    (mapping is StructMapping || mapping is EnumMapping) &&
                    !mapping.TypeDesc!.IsRoot)
                {
global::AutoRepairTracing.TraceLogger.Log("112f0c54-4347-4725-a902-aead2339e0d3", "ReflectionXmlSerializationWriter.cs", 48, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("ee18dcf7-498c-4382-93fb-f568a5993093", "ReflectionXmlSerializationWriter.cs", 49, "AddWriteCallback", 0, mapping.TypeDesc.Type!, "mapping.TypeDesc.Type!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("e2223d34-9899-40c1-985a-1d7479f8029f", "ReflectionXmlSerializationWriter.cs", 49, "AddWriteCallback", 1, mapping.TypeName!, "mapping.TypeName!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("be6fa568-bf00-449a-8b47-ca769423c3eb", "ReflectionXmlSerializationWriter.cs", 49, "AddWriteCallback", 2, mapping.Namespace, "mapping.Namespace");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("44998ceb-ab2e-41cc-a16e-23b439412607", "ReflectionXmlSerializationWriter.cs", 49, "AddWriteCallback", 3, CreateXmlSerializationWriteCallback(mapping, mapping.TypeName!, mapping.Namespace, mapping.TypeDesc.IsNullable), "CreateXmlSerializationWriteCallback(mapping, mapping.TypeName!, mapping.Namespace, mapping.TypeDesc.IsNullable)");
                    AddWriteCallback(
                        mapping.TypeDesc.Type!,
                        mapping.TypeName!,
                        mapping.Namespace,
                        CreateXmlSerializationWriteCallback(mapping, mapping.TypeName!, mapping.Namespace, mapping.TypeDesc.IsNullable)
                    );
}                }
            }
        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("8c8385b2-d93e-401d-bdf2-23b5f472bb6b", "ReflectionXmlSerializationWriter.cs", 40, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("5449526f-5eef-4f3d-9058-4b6d07017c02", "ReflectionXmlSerializationWriter.cs", 58, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("c7f58e38-2fc3-4931-8157-41aef3970ee0", "ReflectionXmlSerializationWriter.cs", 40, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("3c2f9921-00ff-4e84-a991-0b71f9a53235", "ReflectionXmlSerializationWriter.cs", 40, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("bc5995c8-3ddc-42aa-a3b9-b71391032111", "ReflectionXmlSerializationWriter.cs", 40, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        public void WriteObject(object? o)
{global::AutoRepairTracing.TraceLogger.Log("71317f07-642c-4b97-b65e-b08c1e0e67af", "ReflectionXmlSerializationWriter.cs", 59, "Enter Method 'WriteObject'");
try        {
            XmlMapping xmlMapping = _mapping;
global::AutoRepairTracing.TraceLogger.Log("e3793c58-b02a-4b7a-9a0d-4db59f2302ec", "ReflectionXmlSerializationWriter.cs", 61, "Variable Update", xmlMapping, "xmlMapping");
            if (xmlMapping is XmlTypeMapping xmlTypeMapping)
            {
global::AutoRepairTracing.TraceLogger.Log("4713fa67-e4f8-4956-be80-8b6441ec52ec", "ReflectionXmlSerializationWriter.cs", 63, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("664d2aa8-ec2a-435f-85a6-26951a99af30", "ReflectionXmlSerializationWriter.cs", 64, "WriteObjectOfTypeElement", 0, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("1aa282da-2943-481f-ad86-856c35b2efd6", "ReflectionXmlSerializationWriter.cs", 64, "WriteObjectOfTypeElement", 1, xmlTypeMapping, "xmlTypeMapping");
                WriteObjectOfTypeElement(o, xmlTypeMapping);
}            }
            else {global::AutoRepairTracing.TraceLogger.Log("08d4bc87-23da-4700-8449-458d5eb5f691", "ReflectionXmlSerializationWriter.cs", 66, "Else Branch");
if (xmlMapping is XmlMembersMapping xmlMembersMapping)
            {
global::AutoRepairTracing.TraceLogger.Log("20933d81-883a-4506-b38b-cae9e7419f77", "ReflectionXmlSerializationWriter.cs", 67, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("09a75f82-a418-49f0-a703-f8602381d318", "ReflectionXmlSerializationWriter.cs", 68, "GenerateMembersElement", 0, o!, "o!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("35e33001-7a5e-41f1-a7e3-656f0ccc2fab", "ReflectionXmlSerializationWriter.cs", 68, "GenerateMembersElement", 1, xmlMembersMapping, "xmlMembersMapping");
                GenerateMembersElement(o!, xmlMembersMapping);
}            }
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("e47ea67d-a3c7-47fc-853c-a2a50e10a406", "ReflectionXmlSerializationWriter.cs", 59, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("0c2014d9-f65d-489d-b04b-0b3cca6adf51", "ReflectionXmlSerializationWriter.cs", 72, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("37474b25-b564-4120-86ac-b067b64fd029", "ReflectionXmlSerializationWriter.cs", 59, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("60414db3-28b6-4c5d-a3ed-33adaccad18f", "ReflectionXmlSerializationWriter.cs", 59, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("f9d3656d-28b7-4daa-89f3-875a4864caaf", "ReflectionXmlSerializationWriter.cs", 59, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private void WriteObjectOfTypeElement(object? o, XmlTypeMapping mapping)
{global::AutoRepairTracing.TraceLogger.Log("4edb33a3-2bbd-451e-afd9-419ce3419c89", "ReflectionXmlSerializationWriter.cs", 72, "Enter Method 'WriteObjectOfTypeElement'");
try        {
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("03bb70b8-8b59-4cd1-aa46-17ac2bc74dc1", "ReflectionXmlSerializationWriter.cs", 74, "GenerateTypeElement", 0, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("21e6c55d-cb42-4309-be3b-2a06c306a7c0", "ReflectionXmlSerializationWriter.cs", 74, "GenerateTypeElement", 1, mapping, "mapping");
            GenerateTypeElement(o, mapping);
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("d1009d9a-180e-48e4-8586-a5c528ba8d65", "ReflectionXmlSerializationWriter.cs", 72, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("78b7cced-98cd-45e4-a061-12cace2f5159", "ReflectionXmlSerializationWriter.cs", 78, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("0fc0915a-fec8-40f6-984b-293b6768ca2f", "ReflectionXmlSerializationWriter.cs", 72, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("5bdece12-871d-4bf8-8307-b416911c92a5", "ReflectionXmlSerializationWriter.cs", 72, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("aa273d56-fcc9-426c-8683-6c6032dddb31", "ReflectionXmlSerializationWriter.cs", 72, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private void GenerateTypeElement(object? o, XmlTypeMapping xmlMapping)
{global::AutoRepairTracing.TraceLogger.Log("b5cac19b-ee55-4b64-8dde-641e3dd516b8", "ReflectionXmlSerializationWriter.cs", 77, "Enter Method 'GenerateTypeElement'");
try        {
            ElementAccessor element = xmlMapping.Accessor;
global::AutoRepairTracing.TraceLogger.Log("5e0fb405-4d3e-43df-896d-cc0d76cec77d", "ReflectionXmlSerializationWriter.cs", 79, "Variable Update", element, "element");
            TypeMapping mapping = element.Mapping!;
global::AutoRepairTracing.TraceLogger.Log("6fc1632c-ff08-430e-b742-520465fc8848", "ReflectionXmlSerializationWriter.cs", 80, "Variable Update", mapping, "mapping");

            WriteStartDocument();
            if (o == null)
            {
global::AutoRepairTracing.TraceLogger.Log("c3990d90-e34f-494f-95a6-5804b958fe09", "ReflectionXmlSerializationWriter.cs", 84, "If-True Branch");
                string? ns = (element.Form == XmlSchemaForm.Qualified ? element.Namespace : string.Empty);
global::AutoRepairTracing.TraceLogger.Log("2b5e03b0-88c1-4498-b96a-5d1e41171029", "ReflectionXmlSerializationWriter.cs", 85, "Variable Update", ns, "ns");
                if (element.IsNullable)
                {
global::AutoRepairTracing.TraceLogger.Log("4b03f2ed-aff1-41f4-aa1d-f225acdbe164", "ReflectionXmlSerializationWriter.cs", 87, "If-True Branch");
                    if (mapping.IsSoap)
                    {
global::AutoRepairTracing.TraceLogger.Log("c71d1557-8700-42f5-a809-532913c1248c", "ReflectionXmlSerializationWriter.cs", 89, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("1fbef766-7216-4fc4-8364-ece8681a8a43", "ReflectionXmlSerializationWriter.cs", 90, "WriteNullTagEncoded", 0, element.Name, "element.Name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("a557b9f1-4099-477d-97e9-81bcf8e8b56e", "ReflectionXmlSerializationWriter.cs", 90, "WriteNullTagEncoded", 1, ns, "ns");
                        WriteNullTagEncoded(element.Name, ns);
}                    }
                    else
                    {
global::AutoRepairTracing.TraceLogger.Log("ae584eb3-8058-420d-b2a0-9581c7bcbdc9", "ReflectionXmlSerializationWriter.cs", 93, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("e60fd68b-4ba8-4b6e-be18-1b4a72f92db1", "ReflectionXmlSerializationWriter.cs", 94, "WriteNullTagLiteral", 0, element.Name, "element.Name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("2e28f806-9f65-4275-9a93-2e76bfe50529", "ReflectionXmlSerializationWriter.cs", 94, "WriteNullTagLiteral", 1, ns, "ns");
                        WriteNullTagLiteral(element.Name, ns);
}                    }
                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("5eb28a03-7531-4495-ab8e-8b4df3127d81", "ReflectionXmlSerializationWriter.cs", 98, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("94c6f032-a553-4b63-b765-9ac20caa5301", "ReflectionXmlSerializationWriter.cs", 99, "WriteEmptyTag", 0, element.Name, "element.Name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("8d6d9967-e621-4b04-9731-e2f7f6ffb015", "ReflectionXmlSerializationWriter.cs", 99, "WriteEmptyTag", 1, ns, "ns");
                    WriteEmptyTag(element.Name, ns);
}                }
{global::AutoRepairTracing.TraceLogger.Log("e52515c9-cac1-4027-8644-0e1b736f3895", "ReflectionXmlSerializationWriter.cs", 102, "Return");

                return;
}            }

            if (!mapping.TypeDesc!.IsValueType && !mapping.TypeDesc.Type!.IsPrimitive)
            {
global::AutoRepairTracing.TraceLogger.Log("b279bce1-ef17-4f94-82fd-141cda8d5040", "ReflectionXmlSerializationWriter.cs", 106, "If-True Branch");
                TopLevelElement();
            }
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("fc8df5fa-9e9e-4eaf-86b5-7d14fe08b11b", "ReflectionXmlSerializationWriter.cs", 110, "WriteMember", 0, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("031a159b-5c14-498c-9065-ebd4177f6064", "ReflectionXmlSerializationWriter.cs", 110, "WriteMember", 1, null, "null");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("4e0f7ac1-47a2-4c49-9b19-057c04480594", "ReflectionXmlSerializationWriter.cs", 110, "WriteMember", 2, new ElementAccessor[] { element }, "new ElementAccessor[] { element }");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("98b6d21b-f821-4626-9b32-2f56313cc856", "ReflectionXmlSerializationWriter.cs", 110, "WriteMember", 3, null, "null");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("ebd53fec-1866-4550-b396-48c6a1e62a05", "ReflectionXmlSerializationWriter.cs", 110, "WriteMember", 4, null, "null");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("01153f26-9e95-4e1d-93e3-d8776c414469", "ReflectionXmlSerializationWriter.cs", 110, "WriteMember", 5, mapping.TypeDesc, "mapping.TypeDesc");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("9f9a0040-5aa1-4a09-88c7-e4c32176da14", "ReflectionXmlSerializationWriter.cs", 110, "WriteMember", 6, !element.IsSoap, "!element.IsSoap");

            WriteMember(o, null, new ElementAccessor[] { element }, null, null, mapping.TypeDesc, !element.IsSoap);
}            if (mapping.IsSoap)
            {
global::AutoRepairTracing.TraceLogger.Log("b1a00bc5-d461-4688-9b53-c1a88136361f", "ReflectionXmlSerializationWriter.cs", 112, "If-True Branch");
                WriteReferencedElements();
            }
        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("5dbafe21-9893-43ae-8d55-8fedc5bf1e78", "ReflectionXmlSerializationWriter.cs", 77, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("e58d39de-7868-40a9-b154-7a14788163b4", "ReflectionXmlSerializationWriter.cs", 119, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("bc853e7a-d56d-4abe-8934-cfdaa727c8f6", "ReflectionXmlSerializationWriter.cs", 77, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("ea91ce82-5019-40c0-b609-479325eeb92b", "ReflectionXmlSerializationWriter.cs", 77, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("a6db35da-0b73-481c-a16b-f8ac64db5a39", "ReflectionXmlSerializationWriter.cs", 77, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private void WriteMember(object? o, object? choiceSource, ElementAccessor[] elements, TextAccessor? text, ChoiceIdentifierAccessor? choice, TypeDesc memberTypeDesc, bool writeAccessors)
{global::AutoRepairTracing.TraceLogger.Log("f6dcccd6-4a1b-4be9-9421-2e8fe543c540", "ReflectionXmlSerializationWriter.cs", 117, "Enter Method 'WriteMember'");
global::AutoRepairTracing.TraceLogger.LogArray("f2baa1b5-5b73-4f04-bd87-0f88f689759b", "ReflectionXmlSerializationWriter.cs", 117, "Array Parameter", elements, "WriteMember#arg2:elements");
global::AutoRepairTracing.TraceLogger.LogArrayAliasParameter("62ab1a64-23bb-44f6-b744-fa952b2882b7", "ReflectionXmlSerializationWriter.cs", 117, "WriteMember", 2, elements, "elements");
try        {
            if (memberTypeDesc.IsArrayLike &&
                !(elements.Length == 1 && elements[0].Mapping is ArrayMapping))
            {
global::AutoRepairTracing.TraceLogger.Log("26ccfd9b-b23b-4366-84a2-ca2531444a29", "ReflectionXmlSerializationWriter.cs", 121, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d2b63cea-ed39-448a-8e3e-b38c7445c636", "ReflectionXmlSerializationWriter.cs", 122, "WriteArray", 0, o!, "o!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("0d464d92-f9db-4dc5-ad30-ca5f4068803f", "ReflectionXmlSerializationWriter.cs", 122, "WriteArray", 1, choiceSource, "choiceSource");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("785334a7-ca5a-41dd-8bff-b0a419658115", "ReflectionXmlSerializationWriter.cs", 122, "WriteArray", 2, elements, "elements");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("a1003eb3-e155-4b5c-b762-262b884643a9", "ReflectionXmlSerializationWriter.cs", 122, "WriteArray", 3, text, "text");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("94f5b385-b33a-4a65-bc79-51d5d2fd1837", "ReflectionXmlSerializationWriter.cs", 122, "WriteArray", 4, choice, "choice");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("47f12e09-2139-438c-a5b2-7fb9900bc4c9", "ReflectionXmlSerializationWriter.cs", 122, "WriteArray", 5, memberTypeDesc, "memberTypeDesc");
                WriteArray(o!, choiceSource, elements, text, choice, memberTypeDesc);
}            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("7f1d263b-d4a8-4440-840e-373f0a050465", "ReflectionXmlSerializationWriter.cs", 125, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("cad1e372-04cb-4b3b-aac0-ae64ca4b0443", "ReflectionXmlSerializationWriter.cs", 126, "WriteElements", 0, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("118cf391-dbae-4325-b194-ad31ec72cad9", "ReflectionXmlSerializationWriter.cs", 126, "WriteElements", 1, choiceSource, "choiceSource");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("b5073d7a-6f5d-4a37-aad9-06d8565bb7bf", "ReflectionXmlSerializationWriter.cs", 126, "WriteElements", 2, elements, "elements");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("35bd3624-9cef-4a75-a209-0e21fb467b17", "ReflectionXmlSerializationWriter.cs", 126, "WriteElements", 3, text, "text");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("3491e8b4-4c13-4f7c-b80c-bb75271715f7", "ReflectionXmlSerializationWriter.cs", 126, "WriteElements", 4, choice, "choice");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("f95c41f1-e98e-4319-ae52-bb0b7d511b55", "ReflectionXmlSerializationWriter.cs", 126, "WriteElements", 5, writeAccessors, "writeAccessors");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("568a991c-3ded-4132-96ef-ad2d795837ab", "ReflectionXmlSerializationWriter.cs", 126, "WriteElements", 6, memberTypeDesc.IsNullable, "memberTypeDesc.IsNullable");
                WriteElements(o, choiceSource, elements, text, choice, writeAccessors, memberTypeDesc.IsNullable);
}            }
        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("ee428223-0e46-4e64-97c0-2280cfc8b5f9", "ReflectionXmlSerializationWriter.cs", 117, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("ff72a5a2-e47c-4680-b637-bef54b7c2614", "ReflectionXmlSerializationWriter.cs", 133, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("bb3c594f-e6f9-4cb6-aba0-459724445e93", "ReflectionXmlSerializationWriter.cs", 117, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("7ece278e-8e57-4b15-8b26-ac1a32426b30", "ReflectionXmlSerializationWriter.cs", 117, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("0dc88e05-fae5-4f74-8ae4-f02a31c0a399", "ReflectionXmlSerializationWriter.cs", 117, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private void WriteArray(object o, object? choiceSource, ElementAccessor[] elements, TextAccessor? text, ChoiceIdentifierAccessor? choice, TypeDesc arrayTypeDesc)
{global::AutoRepairTracing.TraceLogger.Log("919c1cea-b956-4073-8325-3d278502d0c4", "ReflectionXmlSerializationWriter.cs", 130, "Enter Method 'WriteArray'");
global::AutoRepairTracing.TraceLogger.LogArray("29b6a2c3-11ea-462d-9fb8-7fadca742ac0", "ReflectionXmlSerializationWriter.cs", 130, "Array Parameter", elements, "WriteArray#arg2:elements");
global::AutoRepairTracing.TraceLogger.LogArrayAliasParameter("bcd8f31b-d71d-48f0-b392-624ebdfb6fc2", "ReflectionXmlSerializationWriter.cs", 130, "WriteArray", 2, elements, "elements");
try        {
            if (elements.Length == 0 && text == null)
            {
global::AutoRepairTracing.TraceLogger.Log("efdf6987-1bb6-4eb8-81cf-cd3caabeb5d0", "ReflectionXmlSerializationWriter.cs", 133, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("e3d0df6c-dd53-43dc-9037-ab6ab2b09ef7", "ReflectionXmlSerializationWriter.cs", 134, "Return");
                return;
}            }

            if (arrayTypeDesc.IsNullable && o == null)
            {
global::AutoRepairTracing.TraceLogger.Log("c6b59ffd-1da3-4010-9249-63b30f83017a", "ReflectionXmlSerializationWriter.cs", 138, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("fe311801-82eb-4422-881c-b21f4c8335ee", "ReflectionXmlSerializationWriter.cs", 139, "Return");
                return;
}            }

            if (choice != null)
            {
global::AutoRepairTracing.TraceLogger.Log("d47276d7-0f8e-4945-b205-1ea65f2c3b9a", "ReflectionXmlSerializationWriter.cs", 143, "If-True Branch");
                if (choiceSource == null || ((Array)global::AutoRepairTracing.TraceLogger.LogCastSource("73b07d59-cd7e-46ca-baba-b36cbc67b3f2", "ReflectionXmlSerializationWriter.cs", 233, (object)(choiceSource), "Array")).Length < ((Array)global::AutoRepairTracing.TraceLogger.LogCastSource("61459dde-59cc-454a-81d4-518c07ff8e36", "ReflectionXmlSerializationWriter.cs", 233, (object)(o), "Array")).Length)
                {
global::AutoRepairTracing.TraceLogger.Log("0b57e4f4-6b9c-4296-9d6c-870bcd4b3541", "ReflectionXmlSerializationWriter.cs", 145, "If-True Branch");
                    throw CreateInvalidChoiceIdentifierValueException(choice.Mapping!.TypeDesc!.FullName, choice.MemberName!);
                }
            }
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("3d5e0b89-1882-4075-ae02-d1551b94f350", "ReflectionXmlSerializationWriter.cs", 150, "WriteArrayItems", 0, elements, "elements");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("92677748-2277-4cab-b510-3c6f47f2f8a6", "ReflectionXmlSerializationWriter.cs", 150, "WriteArrayItems", 1, text, "text");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("c8ea1f03-f92a-4af1-8cf9-48738b197601", "ReflectionXmlSerializationWriter.cs", 150, "WriteArrayItems", 2, choice, "choice");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("79b3b776-6f65-43c9-a68a-cb5141ade928", "ReflectionXmlSerializationWriter.cs", 150, "WriteArrayItems", 3, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("feb421cd-6d43-47e8-b624-3cea415c85ff", "ReflectionXmlSerializationWriter.cs", 150, "WriteArrayItems", 4, choiceSource, "choiceSource");

            WriteArrayItems(elements, text, choice, o, choiceSource);
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("89e2b663-26a0-41c5-b831-e338bd3c08cc", "ReflectionXmlSerializationWriter.cs", 130, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("6a448972-cfa4-4faf-ab50-d16afcb6e559", "ReflectionXmlSerializationWriter.cs", 157, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("64f15da6-9a1a-4579-b412-24147af17728", "ReflectionXmlSerializationWriter.cs", 130, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("9dc66e60-33f3-431c-a21a-03ab311bddbb", "ReflectionXmlSerializationWriter.cs", 130, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("45b829ab-52d7-42cf-8113-fdd9432c2274", "ReflectionXmlSerializationWriter.cs", 130, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private void WriteArrayItems(ElementAccessor[] elements, TextAccessor? text, ChoiceIdentifierAccessor? choice, object o, object? choiceSources)
{global::AutoRepairTracing.TraceLogger.Log("b233978f-f3e3-4067-a3dc-5b420aa91bbc", "ReflectionXmlSerializationWriter.cs", 153, "Enter Method 'WriteArrayItems'");
global::AutoRepairTracing.TraceLogger.LogArray("3d37ca29-63b1-4abd-ad91-f602dcd927db", "ReflectionXmlSerializationWriter.cs", 153, "Array Parameter", elements, "WriteArrayItems#arg0:elements");
global::AutoRepairTracing.TraceLogger.LogArrayAliasParameter("787de54d-3ea8-4bde-81a9-138eebff94b0", "ReflectionXmlSerializationWriter.cs", 153, "WriteArrayItems", 0, elements, "elements");
try        {
            var arr = global::AutoRepairTracing.TraceLogger.LogAsCast<IList>("fbac6cfb-95f0-479f-8471-80275c9f0c27", "ReflectionXmlSerializationWriter.cs", 255, (object)(o), "IList");
global::AutoRepairTracing.TraceLogger.Log("1c341a22-d169-45b1-afa7-9a01ccb43fac", "ReflectionXmlSerializationWriter.cs", 155, "Variable Update", arr, "arr");

            if (arr != null)
            {
global::AutoRepairTracing.TraceLogger.Log("99da4f69-0981-4f78-a5fd-ebe5167dff06", "ReflectionXmlSerializationWriter.cs", 158, "If-True Branch");
{int __loopCounter_89ea3031 = 0;global::AutoRepairTracing.TraceLogger.Log("c761fe12-357c-4deb-8fbf-a11fa609ff60", "ReflectionXmlSerializationWriter.cs", 255, "Variable Update", __loopCounter_89ea3031, "__loopCounter_89ea3031");
                for (int i = 0; i < arr.Count; global::AutoRepairTracing.TraceLogger.LogValue("8f420dc8-ecab-4e02-8737-32ac324da42d", "ReflectionXmlSerializationWriter.cs", 255, "Variable Update", i++, "i"))
                {
global::AutoRepairTracing.TraceLogger.Log("18c2511e-a631-4534-a69d-1fe4a8194abb", "ReflectionXmlSerializationWriter.cs", 159, "For Loop");
global::AutoRepairTracing.TraceLogger.LogValue("9b33e81c-4247-494e-824b-cff5ddfc8d9f", "ReflectionXmlSerializationWriter.cs", 258, "Variable Update", __loopCounter_89ea3031++, "__loopCounter_89ea3031");if(__loopCounter_89ea3031 > 10000){global::AutoRepairTracing.TraceLogger.Log("c4a99b65-b121-419f-9a64-a68010885f99", "ReflectionXmlSerializationWriter.cs", 167, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("89ea3031-bb0a-42ab-8bdc-fdde51445225", "ReflectionXmlSerializationWriter.cs", 159, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_89ea3031, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("9fa4f7d5-f82d-42d8-b4ba-21073263f3f1", "ReflectionXmlSerializationWriter.cs", 167, "Break");
break;}}                    object? ai = arr[i];
global::AutoRepairTracing.TraceLogger.Log("85e66c3c-3720-4f5f-b359-24f333c31abd", "ReflectionXmlSerializationWriter.cs", 161, "Variable Update", ai, "ai");
                    var choiceSource = ((Array?)global::AutoRepairTracing.TraceLogger.LogCastSource("e044b735-256d-4052-8401-44f8434eec89", "ReflectionXmlSerializationWriter.cs", 269, (object)(choiceSources), "Array?"))?.GetValue(i);
global::AutoRepairTracing.TraceLogger.Log("2c42b63c-2702-4960-812e-7734edd0dbc8", "ReflectionXmlSerializationWriter.cs", 162, "Variable Update", choiceSource, "choiceSource");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("a51eac85-441f-4191-aa36-2962508af567", "ReflectionXmlSerializationWriter.cs", 163, "WriteElements", 0, ai, "ai");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("c8b8eec7-c2dc-4eb9-8bcc-4b6b09d8f901", "ReflectionXmlSerializationWriter.cs", 163, "WriteElements", 1, choiceSource, "choiceSource");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("093c7d02-2222-4f73-baf8-c3502b353237", "ReflectionXmlSerializationWriter.cs", 163, "WriteElements", 2, elements, "elements");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("fabcf317-f756-41f3-a7ec-a036591ecd19", "ReflectionXmlSerializationWriter.cs", 163, "WriteElements", 3, text, "text");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("3f4d19f8-71d0-4d59-90c2-e2939148781f", "ReflectionXmlSerializationWriter.cs", 163, "WriteElements", 4, choice, "choice");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("71240a0f-762e-482b-961b-a6be37c25ac6", "ReflectionXmlSerializationWriter.cs", 163, "WriteElements", 5, true, "true");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("b827ea4a-b2b3-490a-a12d-0f1a941a1594", "ReflectionXmlSerializationWriter.cs", 163, "WriteElements", 6, true, "true");
                    WriteElements(ai, choiceSource, elements, text, choice, true, true);
}                }
}            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("58415126-1d35-476e-b100-60726d4a8456", "ReflectionXmlSerializationWriter.cs", 167, "Else Branch");
                var a = global::AutoRepairTracing.TraceLogger.LogAsCast<IEnumerable>("9be756ec-7bf0-441b-a591-e69aa5938a45", "ReflectionXmlSerializationWriter.cs", 284, (object)(o), "IEnumerable");
global::AutoRepairTracing.TraceLogger.Log("358009d0-1df3-4e38-b2df-fbaaf13e6a37", "ReflectionXmlSerializationWriter.cs", 168, "Variable Update", a, "a");
                Debug.Assert(a != null);

                IEnumerator e = a.GetEnumerator();
global::AutoRepairTracing.TraceLogger.Log("9b59ce8f-f931-434f-bcfb-efe4bc062489", "ReflectionXmlSerializationWriter.cs", 171, "Variable Update", e, "e");
                if (e != null)
                {
global::AutoRepairTracing.TraceLogger.Log("6860f4d2-ea06-474b-a2f5-050a8ad3d595", "ReflectionXmlSerializationWriter.cs", 173, "If-True Branch");
                    int c = 0;
global::AutoRepairTracing.TraceLogger.Log("b13e6d2d-c453-405c-8562-edba34a54b17", "ReflectionXmlSerializationWriter.cs", 174, "Variable Update", c, "c");
{int __loopCounter_efe60384 = 0;global::AutoRepairTracing.TraceLogger.Log("57aa7e2e-6a5b-4e32-9722-be45dab53558", "ReflectionXmlSerializationWriter.cs", 283, "Variable Update", __loopCounter_efe60384, "__loopCounter_efe60384");
                    while (e.MoveNext())
                    {
global::AutoRepairTracing.TraceLogger.Log("67cb2f55-a0e9-44cc-ad0f-b290fef2bd69", "ReflectionXmlSerializationWriter.cs", 175, "While Loop");
global::AutoRepairTracing.TraceLogger.LogValue("d611fe1c-71da-429e-ae2e-bf3c38a6558a", "ReflectionXmlSerializationWriter.cs", 286, "Variable Update", __loopCounter_efe60384++, "__loopCounter_efe60384");if(__loopCounter_efe60384 > 10000){global::AutoRepairTracing.TraceLogger.Log("7d76f952-35e3-4a05-8d20-481dd0733acd", "ReflectionXmlSerializationWriter.cs", 183, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("efe60384-4c65-41f9-abf1-ae8fe467bf5d", "ReflectionXmlSerializationWriter.cs", 175, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_efe60384, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("85c41385-1490-4373-a23d-d72e1fd3ae43", "ReflectionXmlSerializationWriter.cs", 183, "Break");
break;}}                        object ai = e.Current;
global::AutoRepairTracing.TraceLogger.Log("ffc480bc-bd59-4305-a69a-a492777b37a7", "ReflectionXmlSerializationWriter.cs", 177, "Variable Update", ai, "ai");
                        var choiceSource = ((Array?)global::AutoRepairTracing.TraceLogger.LogCastSource("da1c9179-318b-4c76-9f52-7b8d1381b479", "ReflectionXmlSerializationWriter.cs", 303, (object)(choiceSources), "Array?"))?.GetValue(global::AutoRepairTracing.TraceLogger.LogValue("87f14605-24c5-442c-b9c5-67c93b89ce7d", "ReflectionXmlSerializationWriter.cs", 289, "Variable Update", c++, "c"));
global::AutoRepairTracing.TraceLogger.Log("a51aad29-4140-4b8f-ad6b-40b293ede703", "ReflectionXmlSerializationWriter.cs", 178, "Variable Update", choiceSource, "choiceSource");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("305968b0-d12b-4041-9793-4fabb448ff4e", "ReflectionXmlSerializationWriter.cs", 179, "WriteElements", 0, ai, "ai");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("a132adbc-c37d-49b3-b9e8-120ef3a3b9f8", "ReflectionXmlSerializationWriter.cs", 179, "WriteElements", 1, choiceSource, "choiceSource");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("b2726b51-6c12-4a4b-8d41-81267dea1f68", "ReflectionXmlSerializationWriter.cs", 179, "WriteElements", 2, elements, "elements");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("1e1b7263-099f-40f1-8232-20cb2c59fa9c", "ReflectionXmlSerializationWriter.cs", 179, "WriteElements", 3, text, "text");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("eed54733-9a99-4073-a97f-ce898c70f148", "ReflectionXmlSerializationWriter.cs", 179, "WriteElements", 4, choice, "choice");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("018b80e4-ab53-4676-81ec-b200cbed4179", "ReflectionXmlSerializationWriter.cs", 179, "WriteElements", 5, true, "true");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("cd3f9cdf-8b66-496e-8ca1-4a98cccc0626", "ReflectionXmlSerializationWriter.cs", 179, "WriteElements", 6, true, "true");
                        WriteElements(ai, choiceSource, elements, text, choice, true, true);
}                    }
}                }
            }
        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("36bd2722-c0ef-4e8e-989b-674f854c694f", "ReflectionXmlSerializationWriter.cs", 153, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("96fcc2ec-e8c5-4e9b-b409-df7dab4d6f30", "ReflectionXmlSerializationWriter.cs", 190, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("75221396-b15d-4d8f-b424-d5980afccfb4", "ReflectionXmlSerializationWriter.cs", 153, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("9e6cf097-26eb-4207-9776-c63926c2c370", "ReflectionXmlSerializationWriter.cs", 153, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("8edff821-d149-49ac-af3f-f59d27c79bbe", "ReflectionXmlSerializationWriter.cs", 153, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private void WriteElements(object? o, object? choiceSource, ElementAccessor[] elements, TextAccessor? text, ChoiceIdentifierAccessor? choice, bool writeAccessors, bool isNullable)
{global::AutoRepairTracing.TraceLogger.Log("8b2f7e67-fe7f-44bb-a230-0d38f3035db6", "ReflectionXmlSerializationWriter.cs", 185, "Enter Method 'WriteElements'");
global::AutoRepairTracing.TraceLogger.LogArray("a0606251-d132-49b4-9508-8c4e56770939", "ReflectionXmlSerializationWriter.cs", 185, "Array Parameter", elements, "WriteElements#arg2:elements");
global::AutoRepairTracing.TraceLogger.LogArrayAliasParameter("056f0a8e-ed9a-4f46-9817-15ff3b6583b8", "ReflectionXmlSerializationWriter.cs", 185, "WriteElements", 2, elements, "elements");
try        {
            if (elements.Length == 0 && text == null)
{global::AutoRepairTracing.TraceLogger.Log("c9a9cb56-a0c3-4e43-8459-6c82013d2558", "ReflectionXmlSerializationWriter.cs", 188, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("12e46efb-80b0-4426-9dc1-f13942d1eb78", "ReflectionXmlSerializationWriter.cs", 188, "Return");
                return;
}
            if (elements.Length == 1 && text == null)
            {
global::AutoRepairTracing.TraceLogger.Log("f2d7cc12-f6e4-4497-a4a6-f5cb6f18e5bf", "ReflectionXmlSerializationWriter.cs", 191, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("78eb50d6-d432-4e09-bd19-9e6d95352239", "ReflectionXmlSerializationWriter.cs", 192, "WriteElement", 0, o, "o");
{global::AutoRepairTracing.TraceLogger.Log("2ad9edb7-77be-419f-9b8a-686f0e4c417f", "ReflectionXmlSerializationWriter.cs", 319, "Variable Update", 0, "index:elements");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("14ea4fbf-7049-48cf-a1c0-ae431ea4250a", "ReflectionXmlSerializationWriter.cs", 192, "WriteElement", 1, elements[0], "elements[0]");
}global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("39536576-edf9-493c-9aef-b6455297ea49", "ReflectionXmlSerializationWriter.cs", 192, "WriteElement", 2, writeAccessors, "writeAccessors");
{global::AutoRepairTracing.TraceLogger.Log("d3400884-f80c-4afb-a7b8-a521bae65a54", "ReflectionXmlSerializationWriter.cs", 192, "Variable Update", 0, "index:elements");
                WriteElement(o, elements[0], writeAccessors);
}}            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("53a602b9-5068-4faa-b970-ef4eb573ddfb", "ReflectionXmlSerializationWriter.cs", 195, "Else Branch");
                if (isNullable && choice == null && o == null)
                {
global::AutoRepairTracing.TraceLogger.Log("55641b17-259f-4ba9-82c9-2f289ddb2df1", "ReflectionXmlSerializationWriter.cs", 197, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("7c7da05a-29db-4e80-ae92-7fb69c8d519b", "ReflectionXmlSerializationWriter.cs", 198, "Return");
                    return;
}                }

                int anyCount = 0;
global::AutoRepairTracing.TraceLogger.Log("b0fc3c60-4d4a-441f-b656-8c5dced4710f", "ReflectionXmlSerializationWriter.cs", 201, "Variable Update", anyCount, "anyCount");
                var namedAnys = new List<ElementAccessor>();
global::AutoRepairTracing.TraceLogger.Log("e0e6b06e-87bc-4e1c-9104-73c272553611", "ReflectionXmlSerializationWriter.cs", 202, "Variable Update", namedAnys, "namedAnys");
                ElementAccessor? unnamedAny = null; // can only have one
global::AutoRepairTracing.TraceLogger.Log("d48e8413-ac13-4e74-9b96-bf85548ae335", "ReflectionXmlSerializationWriter.cs", 203, "Variable Update", unnamedAny, "unnamedAny");
global::AutoRepairTracing.TraceLogger.Log("b220833d-8a85-4e31-8166-5b4048fd8838", "ReflectionXmlSerializationWriter.cs", 203, "Null Assignment Warning", "null", "unnamedAny");{int __loopCounter_c4b25d00 = 0;global::AutoRepairTracing.TraceLogger.Log("2185c095-754d-47ba-a4df-3e77ce193096", "ReflectionXmlSerializationWriter.cs", 336, "Variable Update", __loopCounter_c4b25d00, "__loopCounter_c4b25d00");

                for (int i = 0; i < elements.Length; global::AutoRepairTracing.TraceLogger.LogValue("389f152c-3e48-4205-b881-99fd47117261", "ReflectionXmlSerializationWriter.cs", 337, "Variable Update", i++, "i"))
                {
global::AutoRepairTracing.TraceLogger.Log("7ccbe523-9c00-4282-8c14-d1835c2fa363", "ReflectionXmlSerializationWriter.cs", 205, "For Loop");
global::AutoRepairTracing.TraceLogger.LogValue("f81d572f-272d-4ad6-953a-f3c008f79c75", "ReflectionXmlSerializationWriter.cs", 340, "Variable Update", __loopCounter_c4b25d00++, "__loopCounter_c4b25d00");if(__loopCounter_c4b25d00 > 10000){global::AutoRepairTracing.TraceLogger.Log("53f7f604-d1f6-4ec5-86fe-d6a4e487fadf", "ReflectionXmlSerializationWriter.cs", 214, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("c4b25d00-15a8-49e5-b6bf-51d679f5cc69", "ReflectionXmlSerializationWriter.cs", 205, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_c4b25d00, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("d6d82203-7e22-4e94-8308-4e49b6b15501", "ReflectionXmlSerializationWriter.cs", 214, "Break");
break;}}                    ElementAccessor element = elements[i];
global::AutoRepairTracing.TraceLogger.Log("bf19ac3f-5758-4271-b672-9b1036a2d07c", "ReflectionXmlSerializationWriter.cs", 207, "Variable Update", element, "element");

                    if (element.Any)
                    {
global::AutoRepairTracing.TraceLogger.Log("503e0cb6-fb7c-409f-9a3a-aef137a45efa", "ReflectionXmlSerializationWriter.cs", 210, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("24f1b430-e6fc-4a5a-9de9-da2b57ceb4d2", "ReflectionXmlSerializationWriter.cs", 347, "Variable Update", anyCount++, "anyCount");
                        if (element.Name != null && element.Name.Length > 0)
                        {
global::AutoRepairTracing.TraceLogger.Log("44b3dda6-932c-4fd9-8486-d4a5a86981dc", "ReflectionXmlSerializationWriter.cs", 213, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("46145d36-fb90-45dc-8e86-9efd2bee3abe", "ReflectionXmlSerializationWriter.cs", 214, "Add", 0, element, "element");
                            namedAnys.Add(element);
}                        }
                        else
                        {
global::AutoRepairTracing.TraceLogger.Log("a13fa472-cdea-403b-af7c-c5cd2d6a935d", "ReflectionXmlSerializationWriter.cs", 217, "Else Branch");
global::AutoRepairTracing.TraceLogger.LogValue("715e1486-fd6e-4116-8f6f-e8e81ea8f8d4", "ReflectionXmlSerializationWriter.cs", 357, "Variable Update", unnamedAny ??= element, "unnamedAny");
                        }
                    }
                    else {global::AutoRepairTracing.TraceLogger.Log("f1322f9a-4156-4039-9b52-d08501eb858c", "ReflectionXmlSerializationWriter.cs", 221, "Else Branch");
if (choice != null)
                    {
global::AutoRepairTracing.TraceLogger.Log("2a7e94c9-6ed7-4c04-8ea8-6d208ff8f6b1", "ReflectionXmlSerializationWriter.cs", 222, "If-True Branch");
                        // This looks heavy - getting names of enums in string form for comparison rather than just comparing values.
                        // But this faithfully mimics NetFx, and is necessary to prevent confusion between different enum types.
                        // ie EnumType.ValueX could == 1, but TotallyDifferentEnumType.ValueY could also == 1.
                        TypeDesc td = element.Mapping!.TypeDesc!;
global::AutoRepairTracing.TraceLogger.Log("96480732-4e6f-45ef-8ae7-cdd6d4bf1ff0", "ReflectionXmlSerializationWriter.cs", 226, "Variable Update", td, "td");
                        bool enumUseReflection = choice.Mapping!.TypeDesc!.UseReflection;
global::AutoRepairTracing.TraceLogger.Log("49b38153-6829-40ff-9736-2a0bc81ada1d", "ReflectionXmlSerializationWriter.cs", 227, "Variable Update", enumUseReflection, "enumUseReflection");
                        string enumTypeName = choice.Mapping!.TypeDesc!.FullName;
global::AutoRepairTracing.TraceLogger.Log("718422a7-f6bc-45c3-bb36-4f35d0a69407", "ReflectionXmlSerializationWriter.cs", 228, "Variable Update", enumTypeName, "enumTypeName");
                        string enumFullName = (enumUseReflection ? "" : enumTypeName + ".@") + FindChoiceEnumValue(element, (EnumMapping)global::AutoRepairTracing.TraceLogger.LogCastSource("01c31fb0-68f6-4753-b8e2-d53a2c1f7153", "ReflectionXmlSerializationWriter.cs", 395, (object)(choice.Mapping), "EnumMapping"), enumUseReflection);
global::AutoRepairTracing.TraceLogger.Log("3a21d254-72e1-49ec-a8ab-e1d26bd53b4c", "ReflectionXmlSerializationWriter.cs", 229, "Variable Update", enumFullName, "enumFullName");
                        string choiceFullName = global::AutoRepairTracing.TraceLogger.LogCheckedBinary("bf3c0933-dfe1-468b-8ee3-d49a73eea7b8", "ReflectionXmlSerializationWriter.cs", 397, (enumUseReflection ? "" : choiceSource!.GetType().FullName + ".@"), choiceSource!.ToString(), "+");
global::AutoRepairTracing.TraceLogger.Log("32370388-dbda-4daa-a138-a2670472f0a3", "ReflectionXmlSerializationWriter.cs", 230, "Variable Update", choiceFullName, "choiceFullName");

                        if (choiceFullName == enumFullName)
                        {
global::AutoRepairTracing.TraceLogger.Log("e1046fd3-d5e1-425c-a0d8-0d62127b6313", "ReflectionXmlSerializationWriter.cs", 233, "If-True Branch");
                            // Object is either non-null, or it is allowed to be null
                            if (o != null || (!isNullable || element.IsNullable))
                            {
global::AutoRepairTracing.TraceLogger.Log("091aa651-5574-4375-af68-79f14ae217bd", "ReflectionXmlSerializationWriter.cs", 236, "If-True Branch");
                                // But if Object is non-null, it's got to match types
                                if (o != null && !td.Type!.IsAssignableFrom(o!.GetType()))
                                {
global::AutoRepairTracing.TraceLogger.Log("d88715d7-b7de-41c9-94a8-57aa729a4b73", "ReflectionXmlSerializationWriter.cs", 239, "If-True Branch");
                                    throw CreateMismatchChoiceException(td.FullName, choice.MemberName!, enumFullName);
                                }
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("74c84303-3909-4108-ab29-9ea3f2abeef5", "ReflectionXmlSerializationWriter.cs", 243, "WriteElement", 0, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("226cb75d-6d0b-4d2a-9acf-904c9995ee3b", "ReflectionXmlSerializationWriter.cs", 243, "WriteElement", 1, element, "element");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("9a6ffa3b-457b-49a9-8341-c8558bbca8d9", "ReflectionXmlSerializationWriter.cs", 243, "WriteElement", 2, writeAccessors, "writeAccessors");

                                WriteElement(o, element, writeAccessors);
}{global::AutoRepairTracing.TraceLogger.Log("8b9acd67-92f2-4c62-adcb-fabe051ad36e", "ReflectionXmlSerializationWriter.cs", 244, "Return");
                                return;
}                            }
                        }
                    }
                    else
                    {
global::AutoRepairTracing.TraceLogger.Log("2923eeec-0831-4ee6-a18d-05b18243ef4f", "ReflectionXmlSerializationWriter.cs", 249, "Else Branch");
                        TypeDesc td = element.IsUnbounded ? element.Mapping!.TypeDesc!.CreateArrayTypeDesc() : element.Mapping!.TypeDesc!;
global::AutoRepairTracing.TraceLogger.Log("cb8791f0-1b39-48a8-adef-482b9fb5e9c1", "ReflectionXmlSerializationWriter.cs", 250, "Variable Update", td, "td");
                        if (td.Type!.IsAssignableFrom(o!.GetType()))
                        {
global::AutoRepairTracing.TraceLogger.Log("d63ff9fe-9fd8-41ea-99f1-9737eac1a499", "ReflectionXmlSerializationWriter.cs", 252, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("bbaf5d78-8c0f-427b-9dd1-e97e025fd95e", "ReflectionXmlSerializationWriter.cs", 253, "WriteElement", 0, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d5801b86-5b5c-41d7-8c13-85e574adc94d", "ReflectionXmlSerializationWriter.cs", 253, "WriteElement", 1, element, "element");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("30683723-4855-471b-bd0a-8e5e654c0a0c", "ReflectionXmlSerializationWriter.cs", 253, "WriteElement", 2, writeAccessors, "writeAccessors");
                            WriteElement(o, element, writeAccessors);
}{global::AutoRepairTracing.TraceLogger.Log("3e2ca4eb-530b-42a3-8173-9599161e5ed8", "ReflectionXmlSerializationWriter.cs", 254, "Return");
                            return;
}                        }
                    }
}                }
}
                if (anyCount > 0)
                {
global::AutoRepairTracing.TraceLogger.Log("5e4268ca-2fe9-4550-b7fd-0924df91cc64", "ReflectionXmlSerializationWriter.cs", 260, "If-True Branch");
                    if (o is XmlElement elem)
                    {
global::AutoRepairTracing.TraceLogger.Log("9b97b898-5463-48b1-a2d0-fc4821b8581f", "ReflectionXmlSerializationWriter.cs", 262, "If-True Branch");
                        foreach (ElementAccessor element in namedAnys)
                        {
global::AutoRepairTracing.TraceLogger.Log("761a6027-2f0a-4197-a1bc-465549125476", "ReflectionXmlSerializationWriter.cs", 263, "ForEach Loop");
                            if (element.Name == elem.Name && element.Namespace == elem.NamespaceURI)
                            {
global::AutoRepairTracing.TraceLogger.Log("b3a6b774-ad2e-46ae-a3f2-ee1ebfd51f6e", "ReflectionXmlSerializationWriter.cs", 266, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("71193d6a-d34b-4f8f-8cc6-c620ff120ee7", "ReflectionXmlSerializationWriter.cs", 267, "WriteElement", 0, elem, "elem");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("da707e5e-3f2d-45a5-a394-fabfc83b53cf", "ReflectionXmlSerializationWriter.cs", 267, "WriteElement", 1, element, "element");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("28ad8b62-4d29-4654-93a2-fcb5b67726ce", "ReflectionXmlSerializationWriter.cs", 267, "WriteElement", 2, writeAccessors, "writeAccessors");
                                WriteElement(elem, element, writeAccessors);
}{global::AutoRepairTracing.TraceLogger.Log("3fb0de2a-02bd-4965-ad41-9eb848f12212", "ReflectionXmlSerializationWriter.cs", 268, "Return");
                                return;
}                            }
                        }

                        if (choice != null)
                        {
global::AutoRepairTracing.TraceLogger.Log("04775726-89db-4769-ab26-10584589266a", "ReflectionXmlSerializationWriter.cs", 273, "If-True Branch");
                            throw CreateChoiceIdentifierValueException(choice.Mapping!.TypeDesc!.FullName, choice.MemberName!, elem.Name, elem.NamespaceURI);
                        }

                        if (unnamedAny != null)
                        {
global::AutoRepairTracing.TraceLogger.Log("9dcb2378-7ce4-4a4d-b66c-ab94d25384df", "ReflectionXmlSerializationWriter.cs", 278, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("8155bb55-1389-45cf-ba6a-0f5ccff5f03b", "ReflectionXmlSerializationWriter.cs", 279, "WriteElement", 0, elem, "elem");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d19a1b5d-8ca2-4894-b50d-8e2246a4fe52", "ReflectionXmlSerializationWriter.cs", 279, "WriteElement", 1, unnamedAny, "unnamedAny");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("cd9b36fc-c125-4ce6-948f-cb0544ba2798", "ReflectionXmlSerializationWriter.cs", 279, "WriteElement", 2, writeAccessors, "writeAccessors");
                            WriteElement(elem, unnamedAny, writeAccessors);
}{global::AutoRepairTracing.TraceLogger.Log("fcc77a63-66f3-4850-ac6a-a720d47c6957", "ReflectionXmlSerializationWriter.cs", 280, "Return");
                            return;
}                        }

                        throw CreateUnknownAnyElementException(elem.Name, elem.NamespaceURI);
                    }
                }

                if (text != null)
                {
global::AutoRepairTracing.TraceLogger.Log("8b73fc37-64e2-4f6c-b1cc-ecf8cdc2abcc", "ReflectionXmlSerializationWriter.cs", 288, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("f0929c59-5cbd-45c9-b641-ed31dfdf67d2", "ReflectionXmlSerializationWriter.cs", 289, "WriteText", 0, o!, "o!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("ae7a1080-e3a5-4b49-85f6-ca0c024a6914", "ReflectionXmlSerializationWriter.cs", 289, "WriteText", 1, text, "text");
                    WriteText(o!, text);
}{global::AutoRepairTracing.TraceLogger.Log("d564c1ca-f7e6-4a98-a910-fe6d911090b2", "ReflectionXmlSerializationWriter.cs", 290, "Return");
                    return;
}                }

                if (elements.Length > 0 && o != null)
                {
global::AutoRepairTracing.TraceLogger.Log("74521fd1-cec7-4137-9a23-5a51b4740474", "ReflectionXmlSerializationWriter.cs", 294, "If-True Branch");
                    throw CreateUnknownTypeException(o);
                }
            }
        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("c31cf5c6-d08e-4815-bbd2-bde69d2ff313", "ReflectionXmlSerializationWriter.cs", 185, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("c6752175-75a4-42d8-a103-2d52ad253553", "ReflectionXmlSerializationWriter.cs", 306, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("d4f7081c-f429-443b-a709-7131bc0dbf0b", "ReflectionXmlSerializationWriter.cs", 185, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("186cc2c5-d1cf-4300-9812-a39fbe21e341", "ReflectionXmlSerializationWriter.cs", 185, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("13d64312-a19c-45cf-8529-73bf2b571cbb", "ReflectionXmlSerializationWriter.cs", 185, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private static string FindChoiceEnumValue(ElementAccessor element, EnumMapping choiceMapping, bool useReflection)
{global::AutoRepairTracing.TraceLogger.Log("71cb0256-4cbe-4a59-a15c-3fecfcffa5e4", "ReflectionXmlSerializationWriter.cs", 300, "Enter Method 'FindChoiceEnumValue'");
try        {
            string? enumValue = null;
global::AutoRepairTracing.TraceLogger.Log("d89adead-5d64-4d7b-935e-aea8de2bbce3", "ReflectionXmlSerializationWriter.cs", 302, "Variable Update", enumValue, "enumValue");
global::AutoRepairTracing.TraceLogger.Log("1a353f59-81e5-4fa3-b567-98ff91b52bfd", "ReflectionXmlSerializationWriter.cs", 302, "Null Assignment Warning", "null", "enumValue");{int __loopCounter_dbb2fd54 = 0;global::AutoRepairTracing.TraceLogger.Log("7fa1abbb-1270-4d1d-9aee-fc29bdeab918", "ReflectionXmlSerializationWriter.cs", 479, "Variable Update", __loopCounter_dbb2fd54, "__loopCounter_dbb2fd54");

            for (int i = 0; i < choiceMapping.Constants!.Length; global::AutoRepairTracing.TraceLogger.LogValue("ea60b0a8-bf14-4e49-9e6e-74d5c8cbf8d8", "ReflectionXmlSerializationWriter.cs", 480, "Variable Update", i++, "i"))
            {
global::AutoRepairTracing.TraceLogger.Log("18eecd0a-2d4c-40eb-8051-955398971841", "ReflectionXmlSerializationWriter.cs", 304, "For Loop");
global::AutoRepairTracing.TraceLogger.LogValue("270f462e-48e7-4591-8dc0-15f49cdbe637", "ReflectionXmlSerializationWriter.cs", 483, "Variable Update", __loopCounter_dbb2fd54++, "__loopCounter_dbb2fd54");if(__loopCounter_dbb2fd54 > 10000){global::AutoRepairTracing.TraceLogger.Log("74f840af-9a5d-405a-a925-7eae0ab10e46", "ReflectionXmlSerializationWriter.cs", 314, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("dbb2fd54-4992-4c82-9a0c-b68ae810171c", "ReflectionXmlSerializationWriter.cs", 304, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_dbb2fd54, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("9d3d0589-639a-45d2-bde0-d8aede2a9db4", "ReflectionXmlSerializationWriter.cs", 314, "Break");
break;}}                string xmlName = choiceMapping.Constants[i].XmlName;
global::AutoRepairTracing.TraceLogger.Log("91ff4ad3-71ef-4aae-a1db-ea03f07a6662", "ReflectionXmlSerializationWriter.cs", 306, "Variable Update", xmlName, "xmlName");

                if (element.Any && element.Name.Length == 0)
                {
global::AutoRepairTracing.TraceLogger.Log("0a001168-7523-4da6-8329-3e303aa200f4", "ReflectionXmlSerializationWriter.cs", 309, "If-True Branch");
                    if (xmlName == "##any:")
                    {
global::AutoRepairTracing.TraceLogger.Log("1b71126e-d26c-4389-92e9-e333b701894e", "ReflectionXmlSerializationWriter.cs", 311, "If-True Branch");
                        if (useReflection)
{global::AutoRepairTracing.TraceLogger.Log("9afed9aa-f3d6-43cd-b0c8-7667e9f2f9db", "ReflectionXmlSerializationWriter.cs", 313, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("9f0edb15-2cbf-406a-8c38-13675142b4f6", "ReflectionXmlSerializationWriter.cs", 313, "Variable Update", i, "index:choiceMapping.Constants");
global::AutoRepairTracing.TraceLogger.LogValue("3ee63730-3054-4076-8b64-56af3a039bcc", "ReflectionXmlSerializationWriter.cs", 495, "Variable Update", enumValue = choiceMapping.Constants[i].Value.ToString(CultureInfo.InvariantCulture), "enumValue");
}}                        else
{global::AutoRepairTracing.TraceLogger.Log("9df6326f-92c2-4a74-902a-6a0f0d437891", "ReflectionXmlSerializationWriter.cs", 315, "Else Branch");
{global::AutoRepairTracing.TraceLogger.Log("0fb2448f-609e-45ad-bbd5-5bd86f50ed29", "ReflectionXmlSerializationWriter.cs", 315, "Variable Update", i, "index:choiceMapping.Constants");
global::AutoRepairTracing.TraceLogger.LogValue("761d7ad5-edf7-4743-b40b-8277fa821e72", "ReflectionXmlSerializationWriter.cs", 498, "Variable Update", enumValue = choiceMapping.Constants[i].Name, "enumValue");
}}{global::AutoRepairTracing.TraceLogger.Log("5b5327a7-431f-4b7b-adb7-bcf5add9a6de", "ReflectionXmlSerializationWriter.cs", 316, "Break");
                        break;
}                    }
{global::AutoRepairTracing.TraceLogger.Log("42cf49cb-f87a-4b09-9974-0a0d083507de", "ReflectionXmlSerializationWriter.cs", 318, "Continue");
                    continue;
}                }
                int colon = xmlName.LastIndexOf(':');
global::AutoRepairTracing.TraceLogger.Log("cd13338f-2233-4d42-a96f-936a928346c5", "ReflectionXmlSerializationWriter.cs", 320, "Variable Update", colon, "colon");
                string? choiceNs = colon < 0 ? choiceMapping.Namespace : xmlName.Substring(0, colon);
global::AutoRepairTracing.TraceLogger.Log("a4e5acf4-42dd-4b90-836d-64f4f2a71130", "ReflectionXmlSerializationWriter.cs", 321, "Variable Update", choiceNs, "choiceNs");
                string choiceName = colon < 0 ? xmlName : xmlName.Substring(global::AutoRepairTracing.TraceLogger.LogCheckedBinary("d79c6d49-88e3-4c7e-bc19-18510f71cc86", "ReflectionXmlSerializationWriter.cs", 542, colon, 1, "+"));
global::AutoRepairTracing.TraceLogger.Log("3b92be3c-db3f-44ad-9f3b-b69185eee7bf", "ReflectionXmlSerializationWriter.cs", 322, "Variable Update", choiceName, "choiceName");

                if (element.Name == choiceName)
                {
global::AutoRepairTracing.TraceLogger.Log("f92d3b7c-e17d-4b63-821a-741b2a787ecf", "ReflectionXmlSerializationWriter.cs", 325, "If-True Branch");
                    if ((element.Form == XmlSchemaForm.Unqualified && string.IsNullOrEmpty(choiceNs)) || element.Namespace == choiceNs)
                    {
global::AutoRepairTracing.TraceLogger.Log("20d8a0e6-1486-4d12-b0c6-04ac1bc9c8d1", "ReflectionXmlSerializationWriter.cs", 327, "If-True Branch");
                        if (useReflection)
{global::AutoRepairTracing.TraceLogger.Log("945fee8d-baeb-4dcd-ac4c-0a60322f2ded", "ReflectionXmlSerializationWriter.cs", 329, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("d44e4df4-0d0c-47f1-bc5f-552f85899bdb", "ReflectionXmlSerializationWriter.cs", 329, "Variable Update", i, "index:choiceMapping.Constants");
global::AutoRepairTracing.TraceLogger.LogValue("c6309b1d-40a1-4d3d-ae38-2b3dc3dafff9", "ReflectionXmlSerializationWriter.cs", 517, "Variable Update", enumValue = choiceMapping.Constants[i].Value.ToString(CultureInfo.InvariantCulture), "enumValue");
}}                        else
{global::AutoRepairTracing.TraceLogger.Log("98c876d9-61e5-422d-a1e7-3a8c003d2fb1", "ReflectionXmlSerializationWriter.cs", 331, "Else Branch");
{global::AutoRepairTracing.TraceLogger.Log("5bd5349a-f631-4d0a-9d77-55db64e50852", "ReflectionXmlSerializationWriter.cs", 331, "Variable Update", i, "index:choiceMapping.Constants");
global::AutoRepairTracing.TraceLogger.LogValue("5f3fa166-f66c-47a0-96f5-2b76a00f7729", "ReflectionXmlSerializationWriter.cs", 520, "Variable Update", enumValue = choiceMapping.Constants[i].Name, "enumValue");
}}{global::AutoRepairTracing.TraceLogger.Log("18a14c55-e26d-40dd-907b-bd6408e0ace7", "ReflectionXmlSerializationWriter.cs", 332, "Break");
                        break;
}                    }
                }
            }
}
            if (string.IsNullOrEmpty(enumValue))
            {
global::AutoRepairTracing.TraceLogger.Log("9223ff05-7049-4532-8341-a17ea1b158a9", "ReflectionXmlSerializationWriter.cs", 338, "If-True Branch");
                if (element.Any && element.Name.Length == 0)
                {
global::AutoRepairTracing.TraceLogger.Log("738fa345-4d6d-42d0-a585-a4d6532abae6", "ReflectionXmlSerializationWriter.cs", 340, "If-True Branch");
                    // Type {0} is missing enumeration value '##any' for XmlAnyElementAttribute.
                    throw new InvalidOperationException(SR.Format(SR.XmlChoiceMissingAnyValue, choiceMapping.TypeDesc!.FullName));
                }
                // Type {0} is missing value for '{1}'.
                throw new InvalidOperationException(SR.Format(SR.XmlChoiceMissingValue, choiceMapping.TypeDesc!.FullName, global::AutoRepairTracing.TraceLogger.LogCheckedBinary("4312942d-1697-4034-a06e-84b47f78df20", "ReflectionXmlSerializationWriter.cs", 575, element.Namespace + ":", element.Name, "+"), element.Name, element.Namespace));
            }
            if (!useReflection)
{global::AutoRepairTracing.TraceLogger.Log("f683142d-0fb5-4111-aadd-73649783cf9b", "ReflectionXmlSerializationWriter.cs", 348, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("ae1b5d0a-d753-43d2-a8f9-422ce0a474b6", "ReflectionXmlSerializationWriter.cs", 348, "CheckValidIdentifier", 0, enumValue, "enumValue");
                CodeIdentifier.CheckValidIdentifier(enumValue);
}{global::AutoRepairTracing.TraceLogger.Log("8f268931-1245-4ef8-9c64-5d3244a53749", "ReflectionXmlSerializationWriter.cs", 349, "Return");
            return enumValue;
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("92705d00-63a1-414c-a164-099494ebc583", "ReflectionXmlSerializationWriter.cs", 300, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("43c03682-7120-46d3-9cd6-b38f31168bc6", "ReflectionXmlSerializationWriter.cs", 359, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("aaef4eb3-db8d-48c3-a54b-97b2c042de0b", "ReflectionXmlSerializationWriter.cs", 300, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("d64134d3-4014-497a-86c1-6f85769e04b0", "ReflectionXmlSerializationWriter.cs", 300, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("e28ebf2e-a678-4c50-9c09-0fddecadbccb", "ReflectionXmlSerializationWriter.cs", 300, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private void WriteText(object o, TextAccessor text)
{global::AutoRepairTracing.TraceLogger.Log("e8588dd6-9b03-4ee9-ad40-dc9ee2c3a759", "ReflectionXmlSerializationWriter.cs", 352, "Enter Method 'WriteText'");
try        {
            if (text.Mapping is PrimitiveMapping primitiveMapping)
            {
global::AutoRepairTracing.TraceLogger.Log("bad28ee5-79a9-4971-8df8-7e4f5b8fecb5", "ReflectionXmlSerializationWriter.cs", 355, "If-True Branch");
                string? stringValue;
                if (text.Mapping is EnumMapping enumMapping)
                {
global::AutoRepairTracing.TraceLogger.Log("c4b2ef2b-b27b-4ff6-b834-6eba024974cd", "ReflectionXmlSerializationWriter.cs", 358, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("427e32bc-297a-4d1c-b6cb-0db74cb8f419", "ReflectionXmlSerializationWriter.cs", 559, "Variable Update", stringValue = WriteEnumMethod(enumMapping, o), "stringValue");
                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("acd32d96-6022-48c6-afc2-fe9305162588", "ReflectionXmlSerializationWriter.cs", 362, "Else Branch");
                    if (!WritePrimitiveValue(primitiveMapping.TypeDesc!, o, out stringValue))
                    {
global::AutoRepairTracing.TraceLogger.Log("dcc823ee-3cbb-45d7-a0e5-7b293af394c4", "ReflectionXmlSerializationWriter.cs", 364, "If-True Branch");
                        Debug.Assert(o is byte[]);
                    }
                }

                if (o is byte[] byteArray)
                {
global::AutoRepairTracing.TraceLogger.Log("8feaa1e5-3d04-4820-bde1-b4fb148d348f", "ReflectionXmlSerializationWriter.cs", 370, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("dd190f05-3869-4271-a42b-a822f998da89", "ReflectionXmlSerializationWriter.cs", 371, "WriteValue", 0, byteArray, "byteArray");
                    WriteValue(byteArray);
}                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("38d5c2e3-40b2-4e6c-855c-549790a5d520", "ReflectionXmlSerializationWriter.cs", 374, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("4db2048c-1fbf-4225-a4b7-717bb8bdcd50", "ReflectionXmlSerializationWriter.cs", 375, "WriteValue", 0, stringValue, "stringValue");
                    WriteValue(stringValue);
}                }
            }
            else {global::AutoRepairTracing.TraceLogger.Log("4b48b374-e554-4bc5-9f35-e0c21ab36b60", "ReflectionXmlSerializationWriter.cs", 378, "Else Branch");
if (text.Mapping is SpecialMapping specialMapping)
            {
global::AutoRepairTracing.TraceLogger.Log("187dcc73-f5a0-4732-83d0-2619a1cca875", "ReflectionXmlSerializationWriter.cs", 379, "If-True Branch");
                switch (specialMapping.TypeDesc!.Kind)
                {
                    case TypeKind.Node:
global::AutoRepairTracing.TraceLogger.Log("d741b660-b87c-49a7-a4d7-7926505cecb0", "ReflectionXmlSerializationWriter.cs", 391, "Switch Case [case TypeKind.Node:]");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("6fe22084-e52f-4239-b871-6619f8919af0", "ReflectionXmlSerializationWriter.cs", 383, "WriteTo", 0, Writer, "Writer");
                        ((XmlNode)global::AutoRepairTracing.TraceLogger.LogCastSource("80980f1c-f56e-4e29-a77f-01752622bc22", "ReflectionXmlSerializationWriter.cs", 631, (object)(o), "XmlNode")).WriteTo(Writer);
}{global::AutoRepairTracing.TraceLogger.Log("fc8af298-920f-4986-9ea8-a17cc191c962", "ReflectionXmlSerializationWriter.cs", 384, "Break");
                        break;
}                    default:
global::AutoRepairTracing.TraceLogger.Log("8e4e71ee-6730-499e-b614-b41745bd33ac", "ReflectionXmlSerializationWriter.cs", 394, "Switch Case [default:]");
                        throw new InvalidOperationException(SR.XmlInternalError);
                }
            }
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("c4e76acb-d300-425a-941b-ce82905a00c1", "ReflectionXmlSerializationWriter.cs", 352, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("d8fb8452-d206-41a7-ad60-c1bb8d084f55", "ReflectionXmlSerializationWriter.cs", 399, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("c4080245-27e9-4cfd-905d-76b10bddb6bc", "ReflectionXmlSerializationWriter.cs", 352, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("d148358d-e26b-4791-a199-d5954fd141a2", "ReflectionXmlSerializationWriter.cs", 352, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("b49d790f-c0af-4ef5-a750-95849036a1fb", "ReflectionXmlSerializationWriter.cs", 352, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private void WriteElement(object? o, ElementAccessor element, bool writeAccessor)
{global::AutoRepairTracing.TraceLogger.Log("a1be47b7-3f71-4e4d-ad85-ed5d338b1d89", "ReflectionXmlSerializationWriter.cs", 391, "Enter Method 'WriteElement'");
try        {
            string name = writeAccessor ? element.Name : element.Mapping!.TypeName!;
global::AutoRepairTracing.TraceLogger.Log("07c8b38f-4cbb-476e-963c-1872492b19ec", "ReflectionXmlSerializationWriter.cs", 393, "Variable Update", name, "name");
            string? ns = element.Any && element.Name.Length == 0 ? null : (element.Form == XmlSchemaForm.Qualified ? (writeAccessor ? element.Namespace : element.Mapping!.Namespace) : string.Empty);
global::AutoRepairTracing.TraceLogger.Log("78e9b5cb-d3c3-4837-965e-28630bb034d1", "ReflectionXmlSerializationWriter.cs", 394, "Variable Update", ns, "ns");

            if (element.Mapping is NullableMapping nullableMapping)
            {
global::AutoRepairTracing.TraceLogger.Log("14496a7a-f283-4bfd-b9db-86f510337222", "ReflectionXmlSerializationWriter.cs", 397, "If-True Branch");
                if (o != null)
                {
global::AutoRepairTracing.TraceLogger.Log("c7882764-c019-4009-83ba-abf4a421708e", "ReflectionXmlSerializationWriter.cs", 399, "If-True Branch");
                    ElementAccessor e = element.Clone();
global::AutoRepairTracing.TraceLogger.Log("1bf92783-e944-43a3-8ba0-358b3f11c89b", "ReflectionXmlSerializationWriter.cs", 400, "Variable Update", e, "e");
global::AutoRepairTracing.TraceLogger.LogValue("00dfa795-837e-40ee-97d6-8a461fe74335", "ReflectionXmlSerializationWriter.cs", 618, "Variable Update", e.Mapping = nullableMapping.BaseMapping, "e.Mapping");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("71121ef2-c12f-4fc4-84f5-05bb181e34f7", "ReflectionXmlSerializationWriter.cs", 402, "WriteElement", 0, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("60436698-8d2b-4eee-a511-6b7f701a426a", "ReflectionXmlSerializationWriter.cs", 402, "WriteElement", 1, e, "e");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("4543ad35-da77-4e7a-95e0-eb6f93cea586", "ReflectionXmlSerializationWriter.cs", 402, "WriteElement", 2, writeAccessor, "writeAccessor");
                    WriteElement(o, e, writeAccessor);
}                }
                else {global::AutoRepairTracing.TraceLogger.Log("6c69e5d0-12b4-4ca6-af4a-9ed2ef48af15", "ReflectionXmlSerializationWriter.cs", 404, "Else Branch");
if (element.IsNullable)
                {
global::AutoRepairTracing.TraceLogger.Log("2c4a2188-51fc-4ac4-bbaf-73c164e433fa", "ReflectionXmlSerializationWriter.cs", 405, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("360361c2-98f9-4517-9969-28ed70c8f273", "ReflectionXmlSerializationWriter.cs", 406, "WriteNullTagLiteral", 0, element.Name, "element.Name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("bd021a65-0e8d-4f6d-acfc-016529732964", "ReflectionXmlSerializationWriter.cs", 406, "WriteNullTagLiteral", 1, ns, "ns");
                    WriteNullTagLiteral(element.Name, ns);
}                }
}            }
            else {global::AutoRepairTracing.TraceLogger.Log("2a098094-61e8-4e04-a59e-32533307b610", "ReflectionXmlSerializationWriter.cs", 409, "Else Branch");
if (element.Mapping is ArrayMapping)
            {
global::AutoRepairTracing.TraceLogger.Log("a1c1bcb7-d895-4f4a-ac2d-536b61e028c2", "ReflectionXmlSerializationWriter.cs", 410, "If-True Branch");
                var mapping = global::AutoRepairTracing.TraceLogger.LogAsCast<ArrayMapping>("4e6f85e0-92f6-4d3f-994f-7a3360fd7f6a", "ReflectionXmlSerializationWriter.cs", 678, (object)(element.Mapping), "ArrayMapping");
global::AutoRepairTracing.TraceLogger.Log("8a1f70ad-c267-4974-97d4-4580bbb8d22c", "ReflectionXmlSerializationWriter.cs", 411, "Variable Update", mapping, "mapping");
                if (element.IsNullable && o == null)
                {
global::AutoRepairTracing.TraceLogger.Log("8f6483d9-d141-4988-8271-738a2e932d84", "ReflectionXmlSerializationWriter.cs", 413, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("2bf5d322-c0f1-40a4-94e0-9a8ca0b348cf", "ReflectionXmlSerializationWriter.cs", 414, "WriteNullTagLiteral", 0, element.Name, "element.Name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("ffc0fcb3-6fe1-45e9-bd12-af486e51447c", "ReflectionXmlSerializationWriter.cs", 414, "WriteNullTagLiteral", 1, element.Form == XmlSchemaForm.Qualified ? element.Namespace : string.Empty, "element.Form == XmlSchemaForm.Qualified ? element.Namespace : string.Empty");
                    WriteNullTagLiteral(element.Name, element.Form == XmlSchemaForm.Qualified ? element.Namespace : string.Empty);
}                }
                else {global::AutoRepairTracing.TraceLogger.Log("ec3a56d8-0828-4da3-b398-755d12447819", "ReflectionXmlSerializationWriter.cs", 416, "Else Branch");
if (mapping!.IsSoap)
                {
global::AutoRepairTracing.TraceLogger.Log("1afb52f9-f743-4c27-9ba1-c79ebba02a85", "ReflectionXmlSerializationWriter.cs", 417, "If-True Branch");
                    if (mapping.Elements == null || mapping.Elements.Length != 1)
                    {
global::AutoRepairTracing.TraceLogger.Log("1ad3141b-fd98-4830-9bd4-fe03a9348162", "ReflectionXmlSerializationWriter.cs", 419, "If-True Branch");
                        throw new InvalidOperationException(SR.XmlInternalError);
                    }

                    if (!writeAccessor)
                    {
global::AutoRepairTracing.TraceLogger.Log("3e6954bb-0559-48bd-af80-db7f397b6d0a", "ReflectionXmlSerializationWriter.cs", 424, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("02a625fa-f9ec-4f38-8423-90bf868f382f", "ReflectionXmlSerializationWriter.cs", 425, "WritePotentiallyReferencingElement", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("c0b122e2-b359-46b5-a261-bf0dd30584eb", "ReflectionXmlSerializationWriter.cs", 425, "WritePotentiallyReferencingElement", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("5a7ef4cd-49b9-464e-a42f-1add185ad0d5", "ReflectionXmlSerializationWriter.cs", 425, "WritePotentiallyReferencingElement", 2, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("e8f8d677-954c-4e5f-a2f9-b2023f344c6d", "ReflectionXmlSerializationWriter.cs", 425, "WritePotentiallyReferencingElement", 3, mapping.TypeDesc!.Type, "mapping.TypeDesc!.Type");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("917daeb4-136a-4a66-8d7c-fb90ab6a1fab", "ReflectionXmlSerializationWriter.cs", 425, "WritePotentiallyReferencingElement", 4, true, "true");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("b89645d5-983c-4102-a1d4-9be5af32d93d", "ReflectionXmlSerializationWriter.cs", 425, "WritePotentiallyReferencingElement", 5, element.IsNullable, "element.IsNullable");
                        WritePotentiallyReferencingElement(name, ns, o, mapping.TypeDesc!.Type, true, element.IsNullable);
}                    }
                    else
                    {
global::AutoRepairTracing.TraceLogger.Log("bd5c6d5f-594e-4517-b9bc-7a9572f500c5", "ReflectionXmlSerializationWriter.cs", 428, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("5a0b4a4a-82e1-47ec-86ff-c3ddbc05b4e4", "ReflectionXmlSerializationWriter.cs", 429, "WritePotentiallyReferencingElement", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("8e6b2a7d-fe0f-4dcd-8f1f-3027252a64f4", "ReflectionXmlSerializationWriter.cs", 429, "WritePotentiallyReferencingElement", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("00bcc56b-9908-4c47-a8db-3193be0f32fa", "ReflectionXmlSerializationWriter.cs", 429, "WritePotentiallyReferencingElement", 2, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("0caa3df9-f079-4415-a8ab-8ab419253b59", "ReflectionXmlSerializationWriter.cs", 429, "WritePotentiallyReferencingElement", 3, null, "null");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("49725abf-294a-4bc4-8e0d-37f9fe220fdf", "ReflectionXmlSerializationWriter.cs", 429, "WritePotentiallyReferencingElement", 4, false, "false");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("9108f8c9-a5f2-4310-a51b-58de9242b73c", "ReflectionXmlSerializationWriter.cs", 429, "WritePotentiallyReferencingElement", 5, element.IsNullable, "element.IsNullable");
                        WritePotentiallyReferencingElement(name, ns, o, null, false, element.IsNullable);
}                    }
                }
                else {global::AutoRepairTracing.TraceLogger.Log("1a6342a7-0842-46b1-84a6-88467da88b49", "ReflectionXmlSerializationWriter.cs", 432, "Else Branch");
if (element.IsUnbounded)
                {
global::AutoRepairTracing.TraceLogger.Log("21ac5242-6a59-415b-a689-aafbf5cc55f4", "ReflectionXmlSerializationWriter.cs", 433, "If-True Branch");
                    var enumerable = (IEnumerable)global::AutoRepairTracing.TraceLogger.LogCastSource("ae955831-ffb9-4171-86ae-c5f659d2ad2c", "ReflectionXmlSerializationWriter.cs", 724, (object)(o!), "IEnumerable");
global::AutoRepairTracing.TraceLogger.Log("a6bdaa74-a96c-43b4-ba72-8fc2583ffebf", "ReflectionXmlSerializationWriter.cs", 434, "Variable Update", enumerable, "enumerable");
                    foreach (var e in enumerable)
                    {
global::AutoRepairTracing.TraceLogger.Log("7b42ee43-e0f7-4fb4-8fe7-b3fd27a38789", "ReflectionXmlSerializationWriter.cs", 435, "ForEach Loop");
global::AutoRepairTracing.TraceLogger.LogValue("376bd04e-e4c0-4950-bf03-0f0d33e11fa7", "ReflectionXmlSerializationWriter.cs", 686, "Variable Update", element.IsUnbounded = false, "element.IsUnbounded");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("f3da9b04-d5c1-4ded-86d0-fe8935de2ed4", "ReflectionXmlSerializationWriter.cs", 438, "WriteElement", 0, e, "e");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d7d6fcf9-ea9c-4672-8613-922642e26753", "ReflectionXmlSerializationWriter.cs", 438, "WriteElement", 1, element, "element");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("852dadc0-6474-4282-a283-4297e1c301d6", "ReflectionXmlSerializationWriter.cs", 438, "WriteElement", 2, writeAccessor, "writeAccessor");
                        WriteElement(e, element, writeAccessor);
}global::AutoRepairTracing.TraceLogger.LogValue("bc21686e-d565-497d-92ec-5a1b254da97a", "ReflectionXmlSerializationWriter.cs", 691, "Variable Update", element.IsUnbounded = true, "element.IsUnbounded");
                    }
                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("c0104b83-49d4-4188-989b-402a95eabf42", "ReflectionXmlSerializationWriter.cs", 443, "Else Branch");
                    if (o != null)
                    {
global::AutoRepairTracing.TraceLogger.Log("4f33bae0-6420-45e8-aa0d-8484a71c929d", "ReflectionXmlSerializationWriter.cs", 445, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("5434aa93-4124-4898-a26c-10d758e2e969", "ReflectionXmlSerializationWriter.cs", 446, "WriteStartElement", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("7900d8f6-f0a5-4263-9c3e-48ef76b3e5e2", "ReflectionXmlSerializationWriter.cs", 446, "WriteStartElement", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("9541e1f1-63e3-4511-82b3-37462edf42c8", "ReflectionXmlSerializationWriter.cs", 446, "WriteStartElement", 2, false, "false");
                        WriteStartElement(name, ns, false);
}{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("05d1beff-4a19-4fd4-a809-ddd998dd6045", "ReflectionXmlSerializationWriter.cs", 447, "WriteArrayItems", 0, mapping.ElementsSortedByDerivation!, "mapping.ElementsSortedByDerivation!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("3c1fbb37-b5f9-4cd4-a8ae-d4da9ff7a5da", "ReflectionXmlSerializationWriter.cs", 447, "WriteArrayItems", 1, null, "null");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("dd763f30-0909-407c-acf3-a6b94fe3ef17", "ReflectionXmlSerializationWriter.cs", 447, "WriteArrayItems", 2, null, "null");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("0473339a-50fa-4edd-9559-1c6e35998304", "ReflectionXmlSerializationWriter.cs", 447, "WriteArrayItems", 3, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("ae308942-7aeb-4f4d-b638-87f4fe39125b", "ReflectionXmlSerializationWriter.cs", 447, "WriteArrayItems", 4, null, "null");
                        WriteArrayItems(mapping.ElementsSortedByDerivation!, null, null, o, null);
}                        WriteEndElement();
                    }
                }
}}            }
            else {global::AutoRepairTracing.TraceLogger.Log("d1a5e7a2-018a-4d66-86b2-31e31beeb70f", "ReflectionXmlSerializationWriter.cs", 452, "Else Branch");
if (element.Mapping is EnumMapping)
            {
global::AutoRepairTracing.TraceLogger.Log("d9341839-150e-437c-b6a9-05b25f2ee126", "ReflectionXmlSerializationWriter.cs", 453, "If-True Branch");
                if (element.Mapping.IsSoap)
                {
global::AutoRepairTracing.TraceLogger.Log("6faf2794-8cc9-423b-9706-9ff24df6329d", "ReflectionXmlSerializationWriter.cs", 455, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("2b05f982-52ab-4fb0-9da5-33c45b4bd0d1", "ReflectionXmlSerializationWriter.cs", 456, "WriteStartElement", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d46e33f9-a7c3-4696-85be-954dce0bfe84", "ReflectionXmlSerializationWriter.cs", 456, "WriteStartElement", 1, ns, "ns");
                    Writer.WriteStartElement(name, ns);
}{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("8812d142-379a-4de2-a972-73e98b9d50f9", "ReflectionXmlSerializationWriter.cs", 457, "WriteEnumMethod", 0, (EnumMapping)global::AutoRepairTracing.TraceLogger.LogCastSource("99235191-7a4b-4745-b5b1-6fdb70d997f4", "ReflectionXmlSerializationWriter.cs", 767, (object)(element.Mapping), "EnumMapping"), "(EnumMapping)element.Mapping");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("c78135ea-847b-4e2c-ac7f-9e4b90d05828", "ReflectionXmlSerializationWriter.cs", 457, "WriteEnumMethod", 1, o!, "o!");
                    WriteEnumMethod((EnumMapping)global::AutoRepairTracing.TraceLogger.LogCastSource("dac6890b-dc16-48aa-b8d4-59439cb58902", "ReflectionXmlSerializationWriter.cs", 769, (object)(element.Mapping), "EnumMapping"), o!);
}                    WriteEndElement();
                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("222c7bd7-6462-4b00-8c6c-a618dcf892fd", "ReflectionXmlSerializationWriter.cs", 461, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("df548728-049c-4fa2-843e-73795779fe99", "ReflectionXmlSerializationWriter.cs", 462, "WritePrimitive", 0, WritePrimitiveMethodRequirement.WriteElementString, "WritePrimitiveMethodRequirement.WriteElementString");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("57de794d-2547-42a0-8051-2e09ca86147b", "ReflectionXmlSerializationWriter.cs", 462, "WritePrimitive", 1, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("7e28403c-6fda-440c-a476-34b406799e30", "ReflectionXmlSerializationWriter.cs", 462, "WritePrimitive", 2, ns!, "ns!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("a206afe7-14a9-452f-ab14-5004f91ec8ef", "ReflectionXmlSerializationWriter.cs", 462, "WritePrimitive", 3, element.Default, "element.Default");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("e881d0ed-17e3-46a1-8643-e951b38b58fa", "ReflectionXmlSerializationWriter.cs", 462, "WritePrimitive", 4, o!, "o!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d4346d3a-4cf7-471a-b461-7eaf767bc5fe", "ReflectionXmlSerializationWriter.cs", 462, "WritePrimitive", 5, element.Mapping, "element.Mapping");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("e3319d5e-0e73-4702-816e-97d0c7704a54", "ReflectionXmlSerializationWriter.cs", 462, "WritePrimitive", 6, false, "false");
                    WritePrimitive(WritePrimitiveMethodRequirement.WriteElementString, name, ns!, element.Default, o!, element.Mapping, false);
}                }
            }
            else {global::AutoRepairTracing.TraceLogger.Log("f1f2433a-2339-4e8e-aabe-3c054c3d8733", "ReflectionXmlSerializationWriter.cs", 465, "Else Branch");
if (element.Mapping is PrimitiveMapping)
            {
global::AutoRepairTracing.TraceLogger.Log("4def116f-0de4-4606-b746-213e4f695d69", "ReflectionXmlSerializationWriter.cs", 466, "If-True Branch");
                var mapping = global::AutoRepairTracing.TraceLogger.LogAsCast<PrimitiveMapping>("6448fa6d-7da6-4e8c-9be1-ff5c5f617d5b", "ReflectionXmlSerializationWriter.cs", 789, (object)(element.Mapping), "PrimitiveMapping");
global::AutoRepairTracing.TraceLogger.Log("ce31ff85-d170-401d-8230-c6f3fac3a39a", "ReflectionXmlSerializationWriter.cs", 467, "Variable Update", mapping, "mapping");
                if (mapping!.TypeDesc == ReflectionXmlSerializationReader.QnameTypeDesc)
                {
global::AutoRepairTracing.TraceLogger.Log("69800429-884b-4fd9-9b2b-55f17b1074ae", "ReflectionXmlSerializationWriter.cs", 469, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("b234070c-b7b4-4394-a928-ee0ded1fa959", "ReflectionXmlSerializationWriter.cs", 470, "WriteQualifiedNameElement", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("dc2318d0-beee-4bc9-affe-80a65bdf7ae0", "ReflectionXmlSerializationWriter.cs", 470, "WriteQualifiedNameElement", 1, ns!, "ns!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("86d95c1a-2c7b-4295-bb7a-9327808a6241", "ReflectionXmlSerializationWriter.cs", 470, "WriteQualifiedNameElement", 2, element.Default, "element.Default");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("6afd708f-aaa4-4165-a5cd-0c4700d9a659", "ReflectionXmlSerializationWriter.cs", 470, "WriteQualifiedNameElement", 3, (XmlQualifiedName)global::AutoRepairTracing.TraceLogger.LogCastSource("2ddf9db5-08b8-45af-a0f1-9bc6111eaa4c", "ReflectionXmlSerializationWriter.cs", 797, (object)(o!), "XmlQualifiedName"), "(XmlQualifiedName)o!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("1f736f42-3eed-419f-8d6f-069c4afc907c", "ReflectionXmlSerializationWriter.cs", 470, "WriteQualifiedNameElement", 4, element.IsNullable, "element.IsNullable");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("05f11dda-b238-4f16-9c4a-21fd8942e4e5", "ReflectionXmlSerializationWriter.cs", 470, "WriteQualifiedNameElement", 5, mapping.IsSoap, "mapping.IsSoap");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("88c81a43-b495-4f53-ac84-b77bd830efea", "ReflectionXmlSerializationWriter.cs", 470, "WriteQualifiedNameElement", 6, mapping, "mapping");
                    WriteQualifiedNameElement(name, ns!, element.Default, (XmlQualifiedName)global::AutoRepairTracing.TraceLogger.LogCastSource("b8d4d257-bfe6-4ddd-a2d5-593dca4facee", "ReflectionXmlSerializationWriter.cs", 801, (object)(o!), "XmlQualifiedName"), element.IsNullable, mapping.IsSoap, mapping);
}                }
                else {global::AutoRepairTracing.TraceLogger.Log("3c1bb03a-f5b6-4750-b373-18e09e13e01a", "ReflectionXmlSerializationWriter.cs", 472, "Else Branch");
if (o == null && element.IsNullable)
                {
global::AutoRepairTracing.TraceLogger.Log("b9299a0e-d4f1-4747-9c3a-046e8f0cf746", "ReflectionXmlSerializationWriter.cs", 473, "If-True Branch");
                    if (mapping.IsSoap)
                    {
global::AutoRepairTracing.TraceLogger.Log("cae05a22-433f-4b2c-85c0-bb11bbe6eefe", "ReflectionXmlSerializationWriter.cs", 475, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("1af0d1c0-227e-41dd-92c6-8f3cc6942f15", "ReflectionXmlSerializationWriter.cs", 476, "WriteNullTagEncoded", 0, element.Name, "element.Name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("8894fc75-8474-48b5-86c3-36b8d83de978", "ReflectionXmlSerializationWriter.cs", 476, "WriteNullTagEncoded", 1, ns, "ns");
                        WriteNullTagEncoded(element.Name, ns);
}                    }
                    else
                    {
global::AutoRepairTracing.TraceLogger.Log("cbc50f0f-6e18-4034-b6fb-966770763eb6", "ReflectionXmlSerializationWriter.cs", 479, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("31b43055-1329-4214-bc4f-62565e93d8a0", "ReflectionXmlSerializationWriter.cs", 480, "WriteNullTagLiteral", 0, element.Name, "element.Name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("911d2793-69e7-42d8-a189-d82d73e3c95b", "ReflectionXmlSerializationWriter.cs", 480, "WriteNullTagLiteral", 1, ns, "ns");
                        WriteNullTagLiteral(element.Name, ns);
}                    }
                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("418dc905-53be-4be0-ba36-b3154d7a225a", "ReflectionXmlSerializationWriter.cs", 484, "Else Branch");
                    WritePrimitiveMethodRequirement suffixNullable = mapping.IsSoap ? WritePrimitiveMethodRequirement.Encoded : WritePrimitiveMethodRequirement.None;
global::AutoRepairTracing.TraceLogger.Log("57a29cfe-4069-41c3-9eac-65c97edafd9a", "ReflectionXmlSerializationWriter.cs", 485, "Variable Update", suffixNullable, "suffixNullable");
                    WritePrimitiveMethodRequirement suffixRaw = mapping.TypeDesc!.XmlEncodingNotRequired ? WritePrimitiveMethodRequirement.Raw : WritePrimitiveMethodRequirement.None;
global::AutoRepairTracing.TraceLogger.Log("eeccaaed-1a8c-41dd-8feb-b3e60a517e72", "ReflectionXmlSerializationWriter.cs", 486, "Variable Update", suffixRaw, "suffixRaw");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("c4cf6e97-9247-4439-8246-eca7aeb06525", "ReflectionXmlSerializationWriter.cs", 487, "WritePrimitive", 0, element.IsNullable
                        ? WritePrimitiveMethodRequirement.WriteNullableStringLiteral | suffixNullable | suffixRaw
                        : WritePrimitiveMethodRequirement.WriteElementString | suffixRaw, "element.IsNullable
                        ? WritePrimitiveMethodRequirement.WriteNullableStringLiteral | suffixNullable | suffixRaw
                        : WritePrimitiveMethodRequirement.WriteElementString | suffixRaw");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("078dc775-3cb8-45bf-a821-e39e9008df27", "ReflectionXmlSerializationWriter.cs", 487, "WritePrimitive", 1, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("c964d8be-9dde-4920-8644-968d4efa6b21", "ReflectionXmlSerializationWriter.cs", 487, "WritePrimitive", 2, ns!, "ns!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("a99e5bc9-b91b-4390-886a-88fef7caf457", "ReflectionXmlSerializationWriter.cs", 487, "WritePrimitive", 3, element.Default, "element.Default");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("e3371e00-e41d-41a2-a89b-4c1bd1b068d6", "ReflectionXmlSerializationWriter.cs", 487, "WritePrimitive", 4, o!, "o!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("e99068a9-8ac9-474c-bc0a-7ec50e1a1f6f", "ReflectionXmlSerializationWriter.cs", 487, "WritePrimitive", 5, mapping, "mapping");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("3a12bfb9-8be7-4b41-a32b-962c3522b8c3", "ReflectionXmlSerializationWriter.cs", 487, "WritePrimitive", 6, mapping.IsSoap, "mapping.IsSoap");
                    WritePrimitive(element.IsNullable
                        ? WritePrimitiveMethodRequirement.WriteNullableStringLiteral | suffixNullable | suffixRaw
                        : WritePrimitiveMethodRequirement.WriteElementString | suffixRaw,
                        name, ns!, element.Default, o!, mapping, mapping.IsSoap);
}                }
}            }
            else {global::AutoRepairTracing.TraceLogger.Log("3062d79a-b8d8-4a1b-9728-e0b71bc99f55", "ReflectionXmlSerializationWriter.cs", 493, "Else Branch");
if (element.Mapping is StructMapping)
            {
global::AutoRepairTracing.TraceLogger.Log("d0c1adf4-4655-4d8e-ae77-f99e8e7b6a36", "ReflectionXmlSerializationWriter.cs", 494, "If-True Branch");
                var mapping = global::AutoRepairTracing.TraceLogger.LogAsCast<StructMapping>("c61ab4b5-d837-4eca-aec0-bb95c43d0116", "ReflectionXmlSerializationWriter.cs", 850, (object)(element.Mapping), "StructMapping");
global::AutoRepairTracing.TraceLogger.Log("9528221b-ef91-4acc-903b-384a55bdfa9f", "ReflectionXmlSerializationWriter.cs", 495, "Variable Update", mapping, "mapping");
                if (mapping!.IsSoap)
                {
global::AutoRepairTracing.TraceLogger.Log("48aed4e7-b4df-4a5d-af7b-d323877f8d32", "ReflectionXmlSerializationWriter.cs", 497, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("c4a36856-71d3-4471-8147-521bcff1ba58", "ReflectionXmlSerializationWriter.cs", 498, "WritePotentiallyReferencingElement", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("ac6fe872-439f-4296-b307-a7ee01af72bd", "ReflectionXmlSerializationWriter.cs", 498, "WritePotentiallyReferencingElement", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("bb0bfa5e-ef6c-4b45-b1ab-662a0d6367c0", "ReflectionXmlSerializationWriter.cs", 498, "WritePotentiallyReferencingElement", 2, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("9a00476e-f62e-4c73-83f7-c1bfabb2d851", "ReflectionXmlSerializationWriter.cs", 498, "WritePotentiallyReferencingElement", 3, !writeAccessor ? mapping.TypeDesc!.Type : null, "!writeAccessor ? mapping.TypeDesc!.Type : null");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("73dcb034-3f50-4c2e-b083-dfea6074c573", "ReflectionXmlSerializationWriter.cs", 498, "WritePotentiallyReferencingElement", 4, !writeAccessor, "!writeAccessor");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("09727d3c-2c15-44d4-b280-9b8f30cfa691", "ReflectionXmlSerializationWriter.cs", 498, "WritePotentiallyReferencingElement", 5, element.IsNullable, "element.IsNullable");
                    WritePotentiallyReferencingElement(name, ns, o, !writeAccessor ? mapping.TypeDesc!.Type : null, !writeAccessor, element.IsNullable);
}                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("2c12ddb8-3c88-4836-9c83-23d6d7b16246", "ReflectionXmlSerializationWriter.cs", 501, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("ee014650-055f-4614-a854-2660daf2405e", "ReflectionXmlSerializationWriter.cs", 502, "WriteStructMethod", 0, mapping, "mapping");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("e3ccacb4-f7fa-4250-ad69-18857e1f314f", "ReflectionXmlSerializationWriter.cs", 502, "WriteStructMethod", 1, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("b15c9e46-cfa4-40be-b54e-3d089503f1df", "ReflectionXmlSerializationWriter.cs", 502, "WriteStructMethod", 2, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("aecd2a9f-c614-42b7-a1ca-d78587f65de8", "ReflectionXmlSerializationWriter.cs", 502, "WriteStructMethod", 3, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("6d98c422-5e77-43ad-b896-52ab867143b9", "ReflectionXmlSerializationWriter.cs", 502, "WriteStructMethod", 4, element.IsNullable, "element.IsNullable");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("58f1f926-da6b-4da0-8d41-6087ce9dd571", "ReflectionXmlSerializationWriter.cs", 502, "WriteStructMethod", 5, false, "false");
                    WriteStructMethod(mapping, name, ns, o, element.IsNullable, needType: false);
}                }
            }
            else {global::AutoRepairTracing.TraceLogger.Log("59ed0162-1030-45c6-a766-7fe525aa1117", "ReflectionXmlSerializationWriter.cs", 505, "Else Branch");
if (element.Mapping is SpecialMapping)
            {
global::AutoRepairTracing.TraceLogger.Log("cdf09791-b9f1-43a9-998a-ecc22408da46", "ReflectionXmlSerializationWriter.cs", 506, "If-True Branch");
                if (element.Mapping is SerializableMapping)
                {
global::AutoRepairTracing.TraceLogger.Log("7afefdde-d87b-4e58-941c-07a55a80d34c", "ReflectionXmlSerializationWriter.cs", 508, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("368dcb38-120a-40aa-be72-0c00141e41ef", "ReflectionXmlSerializationWriter.cs", 509, "WriteSerializable", 0, (IXmlSerializable)global::AutoRepairTracing.TraceLogger.LogCastSource("a606702b-5ce8-4017-856b-1c175af8fedf", "ReflectionXmlSerializationWriter.cs", 882, (object)(o!), "IXmlSerializable"), "(IXmlSerializable)o!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("41170c95-864c-4992-a6df-74ee89420034", "ReflectionXmlSerializationWriter.cs", 509, "WriteSerializable", 1, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("a1e0a9c6-2209-4e00-857a-345738e36d79", "ReflectionXmlSerializationWriter.cs", 509, "WriteSerializable", 2, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("94e0bd85-8f0e-422b-af20-36a21bb0c3d7", "ReflectionXmlSerializationWriter.cs", 509, "WriteSerializable", 3, element.IsNullable, "element.IsNullable");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("049ea815-e7ff-46a6-84cf-e975e26eeec3", "ReflectionXmlSerializationWriter.cs", 509, "WriteSerializable", 4, !element.Any, "!element.Any");
                    WriteSerializable((IXmlSerializable)global::AutoRepairTracing.TraceLogger.LogCastSource("0510aa46-c7ce-4174-9b3e-441761a122a2", "ReflectionXmlSerializationWriter.cs", 887, (object)(o!), "IXmlSerializable"), name, ns, element.IsNullable, !element.Any);
}                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("c3c391d2-d911-402e-83ef-31b6060e4212", "ReflectionXmlSerializationWriter.cs", 512, "Else Branch");
                    // XmlNode, XmlElement
                    if (o is XmlNode node)
                    {
global::AutoRepairTracing.TraceLogger.Log("ce2ce8c5-ba6c-4c91-80a5-54250b0147f7", "ReflectionXmlSerializationWriter.cs", 515, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("10914603-4d87-423a-b873-ebaf7c8ad3cf", "ReflectionXmlSerializationWriter.cs", 516, "WriteElementLiteral", 0, node, "node");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("2a52a92f-10e4-4ade-b57f-98e4fbd722d5", "ReflectionXmlSerializationWriter.cs", 516, "WriteElementLiteral", 1, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("13d0f5c9-d990-4554-8187-708d75fd5566", "ReflectionXmlSerializationWriter.cs", 516, "WriteElementLiteral", 2, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("38f05f6a-2aa0-497e-8ba2-467df72ca708", "ReflectionXmlSerializationWriter.cs", 516, "WriteElementLiteral", 3, element.IsNullable, "element.IsNullable");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("b20c10fc-b7fe-4c73-95aa-27a6d17b03b0", "ReflectionXmlSerializationWriter.cs", 516, "WriteElementLiteral", 4, element.Any, "element.Any");
                        WriteElementLiteral(node, name, ns, element.IsNullable, element.Any);
}                    }
                    else
                    {
global::AutoRepairTracing.TraceLogger.Log("b6ab2021-a55c-4565-9802-74f4ce82855d", "ReflectionXmlSerializationWriter.cs", 519, "Else Branch");
                        throw CreateInvalidAnyTypeException(o!);
                    }
                }
            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("4c704c1d-5669-40b0-ad86-15c798ea160b", "ReflectionXmlSerializationWriter.cs", 525, "Else Branch");
                throw new InvalidOperationException(SR.XmlInternalError);
            }
}}}}}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("07339e81-7b55-44a4-9de9-bfa4fde1d592", "ReflectionXmlSerializationWriter.cs", 391, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("8333271e-7eac-41a9-8faa-941307a65fa5", "ReflectionXmlSerializationWriter.cs", 539, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("4a936eb7-fac9-4702-97c8-cb709fb4256c", "ReflectionXmlSerializationWriter.cs", 391, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("cc7cc85e-3eea-4439-b9e8-c8135d688b69", "ReflectionXmlSerializationWriter.cs", 391, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("4725721c-4321-4b0b-baf1-2d8781d76f6c", "ReflectionXmlSerializationWriter.cs", 391, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private XmlSerializationWriteCallback CreateXmlSerializationWriteCallback(TypeMapping mapping, string name, string? ns, bool isNullable)
{global::AutoRepairTracing.TraceLogger.Log("25020612-4aad-4f5c-a259-d905b0f97860", "ReflectionXmlSerializationWriter.cs", 530, "Enter Method 'CreateXmlSerializationWriteCallback'");
try        {
            if (mapping is StructMapping structMapping)
            {
global::AutoRepairTracing.TraceLogger.Log("7c11911c-782e-4e4e-b0db-342171dc3221", "ReflectionXmlSerializationWriter.cs", 533, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("43f154ea-7d4e-4092-9944-3f755b366bae", "ReflectionXmlSerializationWriter.cs", 534, "Return");
                return (o) =>
                {
                    WriteStructMethod(structMapping, name, ns, o, isNullable, needType: false);
                };
}            }
            else {global::AutoRepairTracing.TraceLogger.Log("9fa90441-8501-448d-9f49-ffe76d66ba4c", "ReflectionXmlSerializationWriter.cs", 539, "Else Branch");
if (mapping is EnumMapping enumMapping)
            {
global::AutoRepairTracing.TraceLogger.Log("90ea544c-037a-47f5-8742-2d9b6ce257bb", "ReflectionXmlSerializationWriter.cs", 540, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("74edb4dc-71b7-4f51-98a1-7d65d004e8aa", "ReflectionXmlSerializationWriter.cs", 541, "Return");
                return (o) =>
                {
                    WriteEnumMethod(enumMapping, o);
                };
}            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("949e318b-53bc-4886-80d2-cf3b4044c6b9", "ReflectionXmlSerializationWriter.cs", 547, "Else Branch");
                throw new InvalidOperationException(SR.XmlInternalError);
            }
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("46a00973-acb2-4457-b85c-dbcc7cc8d8b1", "ReflectionXmlSerializationWriter.cs", 530, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("4e91633a-984f-4b33-9bbb-d044509bc9b0", "ReflectionXmlSerializationWriter.cs", 562, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("75953027-03d8-48ad-a837-5970a621fc58", "ReflectionXmlSerializationWriter.cs", 530, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("4d3d8bdd-8abb-41ec-9ad2-e37ec3ff4a26", "ReflectionXmlSerializationWriter.cs", 530, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("fac2feb4-96e5-4b50-b918-e55f4dd17765", "ReflectionXmlSerializationWriter.cs", 530, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private void WriteQualifiedNameElement(string name, string ns, object? defaultValue, XmlQualifiedName o, bool nullable, bool isSoap, PrimitiveMapping mapping)
{global::AutoRepairTracing.TraceLogger.Log("2681b94e-7211-44af-97b5-addba10f77d7", "ReflectionXmlSerializationWriter.cs", 552, "Enter Method 'WriteQualifiedNameElement'");
try        {
            bool hasDefault = defaultValue != null && defaultValue != DBNull.Value && mapping.TypeDesc!.HasDefaultSupport;
global::AutoRepairTracing.TraceLogger.Log("60ec16e1-4322-4b44-8c0b-9b33ffb74ee0", "ReflectionXmlSerializationWriter.cs", 554, "Variable Update", hasDefault, "hasDefault");
            if (hasDefault && IsDefaultValue(o, defaultValue!))
{global::AutoRepairTracing.TraceLogger.Log("11af5ba5-d0e3-4852-9d83-26ce9800fdd3", "ReflectionXmlSerializationWriter.cs", 556, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("6bb139df-b42c-46e2-a4a6-bd97eea4074d", "ReflectionXmlSerializationWriter.cs", 556, "Return");
                return;
}
            if (isSoap)
            {
global::AutoRepairTracing.TraceLogger.Log("250dac66-3aed-470f-b87d-2e8c8706d916", "ReflectionXmlSerializationWriter.cs", 559, "If-True Branch");
                if (nullable)
                {
global::AutoRepairTracing.TraceLogger.Log("e8df0529-f6bc-4dc0-bd4c-c8133b24c27a", "ReflectionXmlSerializationWriter.cs", 561, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("bb62265f-6d81-431d-a449-c1c377e5ddf3", "ReflectionXmlSerializationWriter.cs", 562, "WriteNullableQualifiedNameEncoded", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("ad9a3b18-9d1e-42f6-85f1-90d049c0edf4", "ReflectionXmlSerializationWriter.cs", 562, "WriteNullableQualifiedNameEncoded", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("0da93c01-3901-4379-933f-9108511ee8e6", "ReflectionXmlSerializationWriter.cs", 562, "WriteNullableQualifiedNameEncoded", 2, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("b8688461-5fd6-49a1-ae82-cdbdbe982eea", "ReflectionXmlSerializationWriter.cs", 562, "WriteNullableQualifiedNameEncoded", 3, new XmlQualifiedName(mapping.TypeName, mapping.Namespace), "new XmlQualifiedName(mapping.TypeName, mapping.Namespace)");
                    WriteNullableQualifiedNameEncoded(name, ns, o, new XmlQualifiedName(mapping.TypeName, mapping.Namespace));
}                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("489b3b39-dcc0-4d6f-bb8c-159013d664dd", "ReflectionXmlSerializationWriter.cs", 565, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("45713e08-df66-4d0d-99b1-d331acb80d5e", "ReflectionXmlSerializationWriter.cs", 566, "WriteElementQualifiedName", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("abc89417-34b5-4740-b99a-91c6945448b8", "ReflectionXmlSerializationWriter.cs", 566, "WriteElementQualifiedName", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("91c8e02b-69b9-4a1e-bce8-4dd7872f53db", "ReflectionXmlSerializationWriter.cs", 566, "WriteElementQualifiedName", 2, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("b7e5390b-33bb-426b-a466-8e4911e3dd70", "ReflectionXmlSerializationWriter.cs", 566, "WriteElementQualifiedName", 3, new XmlQualifiedName(mapping.TypeName, mapping.Namespace), "new XmlQualifiedName(mapping.TypeName, mapping.Namespace)");
                    WriteElementQualifiedName(name, ns, o, new XmlQualifiedName(mapping.TypeName, mapping.Namespace));
}                }
            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("81aa2c78-2c8e-40c5-b13b-4579c6427887", "ReflectionXmlSerializationWriter.cs", 570, "Else Branch");
                if (nullable)
                {
global::AutoRepairTracing.TraceLogger.Log("e6462934-0dd8-4daf-bc77-74cb1061a65b", "ReflectionXmlSerializationWriter.cs", 572, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("94a7fccb-4ab0-4c89-9fda-d5b20a4f9950", "ReflectionXmlSerializationWriter.cs", 573, "WriteNullableQualifiedNameLiteral", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("7e3d868d-6e82-4c31-8193-65f006cf57ba", "ReflectionXmlSerializationWriter.cs", 573, "WriteNullableQualifiedNameLiteral", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("37a4a59c-c771-43f4-8bda-625e96aef6a9", "ReflectionXmlSerializationWriter.cs", 573, "WriteNullableQualifiedNameLiteral", 2, o, "o");
                    WriteNullableQualifiedNameLiteral(name, ns, o);
}                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("805ccf9a-049f-49dc-b272-62a09f206ab5", "ReflectionXmlSerializationWriter.cs", 576, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("3b66c595-63f6-4f57-9be0-0ba5561efa53", "ReflectionXmlSerializationWriter.cs", 577, "WriteElementQualifiedName", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("11f06a75-3677-40fb-aad0-f5ff0b97b628", "ReflectionXmlSerializationWriter.cs", 577, "WriteElementQualifiedName", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("b34a12ea-444e-4de0-a780-19e39de54b04", "ReflectionXmlSerializationWriter.cs", 577, "WriteElementQualifiedName", 2, o, "o");
                    WriteElementQualifiedName(name, ns, o);
}                }
            }
        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("1582a14f-2708-4ea4-80f3-dbb9576de318", "ReflectionXmlSerializationWriter.cs", 552, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("264c6f36-8f3e-4a80-bee6-830136eb2b12", "ReflectionXmlSerializationWriter.cs", 593, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("af281d4a-9cf9-474a-81aa-1d42d92bc487", "ReflectionXmlSerializationWriter.cs", 552, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("5af95b45-09d4-4360-a856-53cd83c2aa44", "ReflectionXmlSerializationWriter.cs", 552, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("3a2e7db5-ecd5-4f8e-be49-b86fd6367b84", "ReflectionXmlSerializationWriter.cs", 552, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private void WriteStructMethod(StructMapping mapping, string n, string? ns, object? o, bool isNullable, bool needType)
{global::AutoRepairTracing.TraceLogger.Log("43e22a28-b5f5-45ea-b3b0-015990bdf401", "ReflectionXmlSerializationWriter.cs", 582, "Enter Method 'WriteStructMethod'");
try        {
            if (mapping.IsSoap && mapping.TypeDesc!.IsRoot) {global::AutoRepairTracing.TraceLogger.Log("efc9900a-8b8b-4f56-9e85-e3e8ad21625d", "ReflectionXmlSerializationWriter.cs", 584, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("d00eec84-80c5-4d0f-ba72-1acd88d152a2", "ReflectionXmlSerializationWriter.cs", 584, "Return");
return;
}
            if (!mapping.IsSoap)
            {
global::AutoRepairTracing.TraceLogger.Log("897bb38f-fd41-4d8f-b8f5-ae913029c51d", "ReflectionXmlSerializationWriter.cs", 587, "If-True Branch");
                if (o == null)
                {
global::AutoRepairTracing.TraceLogger.Log("7e8f3116-ce72-4804-a68c-dd1f858d9438", "ReflectionXmlSerializationWriter.cs", 589, "If-True Branch");
                    if (isNullable) {global::AutoRepairTracing.TraceLogger.Log("2b269d24-55b2-4104-a586-81a3334ac464", "ReflectionXmlSerializationWriter.cs", 590, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("0269a544-8839-41fb-99a9-e215622f5619", "ReflectionXmlSerializationWriter.cs", 590, "WriteNullTagLiteral", 0, n, "n");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("cd7fd3c3-1e40-4a7f-997c-cd495b6d73c6", "ReflectionXmlSerializationWriter.cs", 590, "WriteNullTagLiteral", 1, ns, "ns");
WriteNullTagLiteral(n, ns);
}{global::AutoRepairTracing.TraceLogger.Log("50afed43-211c-4f2b-81d6-493fa336ad53", "ReflectionXmlSerializationWriter.cs", 591, "Return");
                    return;
}                }

                if (!needType
                 && o.GetType() != mapping.TypeDesc!.Type)
                {
global::AutoRepairTracing.TraceLogger.Log("a1dbb9b8-d6fe-4b3b-8f3a-2b7f6844a7f5", "ReflectionXmlSerializationWriter.cs", 596, "If-True Branch");
                    if (WriteDerivedTypes(mapping, n, ns, o, isNullable))
                    {
global::AutoRepairTracing.TraceLogger.Log("8a8369f7-fc7a-4dcb-91c7-09b79d32fd2f", "ReflectionXmlSerializationWriter.cs", 598, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("251db80a-7ae9-491e-a215-0305c8f9d46f", "ReflectionXmlSerializationWriter.cs", 599, "Return");
                        return;
}                    }

                    if (mapping.TypeDesc.IsRoot)
                    {
global::AutoRepairTracing.TraceLogger.Log("47313c3d-4603-4d0c-b11f-0e13fd946bcc", "ReflectionXmlSerializationWriter.cs", 603, "If-True Branch");
                        if (WriteEnumAndArrayTypes(o, n!, ns))
                        {
global::AutoRepairTracing.TraceLogger.Log("167863f2-f8be-4709-9229-e999e63eb871", "ReflectionXmlSerializationWriter.cs", 605, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("966d978e-b177-4033-8a38-1a203d09c2df", "ReflectionXmlSerializationWriter.cs", 606, "Return");
                            return;
}                        }
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("19748396-4d0a-43bb-9d64-decb664eef01", "ReflectionXmlSerializationWriter.cs", 609, "WriteTypedPrimitive", 0, n, "n");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("92c271da-9794-4ea5-b318-99ac72933472", "ReflectionXmlSerializationWriter.cs", 609, "WriteTypedPrimitive", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d94c5d47-1156-481a-9ef0-dd535417dade", "ReflectionXmlSerializationWriter.cs", 609, "WriteTypedPrimitive", 2, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("ffa3df90-2586-41f6-bacc-61ba51849543", "ReflectionXmlSerializationWriter.cs", 609, "WriteTypedPrimitive", 3, true, "true");

                        WriteTypedPrimitive(n, ns, o, true);
}{global::AutoRepairTracing.TraceLogger.Log("c48dfc73-e5d1-43fd-9323-71b4bb0ba680", "ReflectionXmlSerializationWriter.cs", 610, "Return");
                        return;
}                    }

                    throw CreateUnknownTypeException(o);
                }
            }

            if (!mapping.TypeDesc!.IsAbstract)
            {
global::AutoRepairTracing.TraceLogger.Log("209ed0d3-eac6-46a7-b6ec-f5951e612594", "ReflectionXmlSerializationWriter.cs", 618, "If-True Branch");
                if (mapping.TypeDesc.Type != null && typeof(XmlSchemaObject).IsAssignableFrom(mapping.TypeDesc.Type))
                {
global::AutoRepairTracing.TraceLogger.Log("39a6bad4-c502-4e95-bdcd-e8c325486144", "ReflectionXmlSerializationWriter.cs", 620, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("5ed54ace-bb82-43ec-97c6-c0cec70321db", "ReflectionXmlSerializationWriter.cs", 1019, "Variable Update", EscapeName = false, "EscapeName");
                }

                XmlSerializerNamespaces? xmlnsSource = null;
global::AutoRepairTracing.TraceLogger.Log("c34335e8-e295-44cc-8730-cd76e5b2be8d", "ReflectionXmlSerializationWriter.cs", 624, "Variable Update", xmlnsSource, "xmlnsSource");
global::AutoRepairTracing.TraceLogger.Log("2fca0937-2338-4ae1-9a09-42c3580bcc70", "ReflectionXmlSerializationWriter.cs", 624, "Null Assignment Warning", "null", "xmlnsSource");                MemberMapping[] members = TypeScope.GetAllMembers(mapping);
global::AutoRepairTracing.TraceLogger.Log("f05baa9e-07fd-43cf-81a4-bb1e318eecf7", "ReflectionXmlSerializationWriter.cs", 625, "Heap State: members (MemberMapping[])");global::AutoRepairTracing.TraceLogger.Log("31dbea0c-aa6e-4f43-b958-cad9415357f2", "ReflectionXmlSerializationWriter.cs", 625, "Variable Update", members, "members");
                int xmlnsMember = FindXmlnsIndex(members);
global::AutoRepairTracing.TraceLogger.Log("9d1d17d5-b5a4-4e87-b729-a62b0f0a95da", "ReflectionXmlSerializationWriter.cs", 626, "Variable Update", xmlnsMember, "xmlnsMember");
                if (xmlnsMember >= 0)
                {
global::AutoRepairTracing.TraceLogger.Log("19ea5749-bba6-4336-a450-814b529d41f5", "ReflectionXmlSerializationWriter.cs", 628, "If-True Branch");
                    MemberMapping member = members[xmlnsMember];
global::AutoRepairTracing.TraceLogger.Log("5790e2e3-3d58-41c5-a73e-bd358c65ed07", "ReflectionXmlSerializationWriter.cs", 629, "Variable Update", member, "member");
global::AutoRepairTracing.TraceLogger.LogValue("65e18298-e97a-4be1-bfeb-bdda2a591ae0", "ReflectionXmlSerializationWriter.cs", 1029, "Variable Update", xmlnsSource = (XmlSerializerNamespaces?)global::AutoRepairTracing.TraceLogger.LogCastSource("f10f0226-c631-4f23-8cc5-d91bb5114383", "ReflectionXmlSerializationWriter.cs", 1081, (object)(GetMemberValue(o!, member.Name)), "XmlSerializerNamespaces?"), "xmlnsSource");
                }

                if (!mapping.IsSoap)
                {
global::AutoRepairTracing.TraceLogger.Log("cf43862f-1b35-436f-9a53-2adfa710a906", "ReflectionXmlSerializationWriter.cs", 634, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("bbab5670-6dd9-4e98-a100-41afb762dda0", "ReflectionXmlSerializationWriter.cs", 635, "WriteStartElement", 0, n, "n");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("a394260c-50a8-4210-896f-12eeb6f14677", "ReflectionXmlSerializationWriter.cs", 635, "WriteStartElement", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("42ce98ba-afc8-4a8b-beab-8bb27aee7c5c", "ReflectionXmlSerializationWriter.cs", 635, "WriteStartElement", 2, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("969a67a4-b612-4eff-ad57-ce5ba9e8f954", "ReflectionXmlSerializationWriter.cs", 635, "WriteStartElement", 3, false, "false");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("ac00222d-ff92-4ea0-97d5-a42e7fedb66e", "ReflectionXmlSerializationWriter.cs", 635, "WriteStartElement", 4, xmlnsSource, "xmlnsSource");
                    WriteStartElement(n, ns, o, false, xmlnsSource);
}
                    if (!mapping.TypeDesc.IsRoot)
                    {
global::AutoRepairTracing.TraceLogger.Log("2a50b4dc-22ac-49e9-9e4b-672396ef83d6", "ReflectionXmlSerializationWriter.cs", 638, "If-True Branch");
                        if (needType)
                        {
global::AutoRepairTracing.TraceLogger.Log("edce9982-41bb-4f25-87e7-9cfa4e6d157c", "ReflectionXmlSerializationWriter.cs", 640, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("70b9dc08-c7a5-48f4-8c8f-889bf2ef14aa", "ReflectionXmlSerializationWriter.cs", 641, "WriteXsiType", 0, mapping.TypeName!, "mapping.TypeName!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("ca970969-2739-48f8-9450-3c110faf721a", "ReflectionXmlSerializationWriter.cs", 641, "WriteXsiType", 1, mapping.Namespace, "mapping.Namespace");
                            WriteXsiType(mapping.TypeName!, mapping.Namespace);
}                        }
                    }
                }
                else {global::AutoRepairTracing.TraceLogger.Log("553bcb96-da84-4bda-8204-0d37b4a8ef2d", "ReflectionXmlSerializationWriter.cs", 645, "Else Branch");
if (xmlnsSource != null)
                {
global::AutoRepairTracing.TraceLogger.Log("805a7f3d-18d8-4c9c-bb12-a2a8c99b7ba6", "ReflectionXmlSerializationWriter.cs", 646, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("a4300990-762c-4e05-aeed-3072a3538506", "ReflectionXmlSerializationWriter.cs", 647, "WriteNamespaceDeclarations", 0, xmlnsSource, "xmlnsSource");
                    WriteNamespaceDeclarations(xmlnsSource);
}                }
}{int __loopCounter_b5e535af = 0;global::AutoRepairTracing.TraceLogger.Log("7d995b32-4633-4a45-a5b7-8137dce126e3", "ReflectionXmlSerializationWriter.cs", 1061, "Variable Update", __loopCounter_b5e535af, "__loopCounter_b5e535af");

                for (int i = 0; i < members.Length; global::AutoRepairTracing.TraceLogger.LogValue("268630f4-7fd7-49e0-81a6-5c3991b74382", "ReflectionXmlSerializationWriter.cs", 1062, "Variable Update", i++, "i"))
                {
global::AutoRepairTracing.TraceLogger.Log("48c0a06b-32d3-435d-9273-2f52e8a7dfed", "ReflectionXmlSerializationWriter.cs", 650, "For Loop");
global::AutoRepairTracing.TraceLogger.LogValue("7bcbff17-20fb-450d-a7ec-4c83c77159c5", "ReflectionXmlSerializationWriter.cs", 1065, "Variable Update", __loopCounter_b5e535af++, "__loopCounter_b5e535af");if(__loopCounter_b5e535af > 10000){global::AutoRepairTracing.TraceLogger.Log("0d2ccfea-a226-439d-841d-bbc627283b4e", "ReflectionXmlSerializationWriter.cs", 665, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("b5e535af-d8d5-4163-a0b4-38b0f79aaac4", "ReflectionXmlSerializationWriter.cs", 650, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_b5e535af, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("e20e0810-47ff-4572-93d7-c49c35504f43", "ReflectionXmlSerializationWriter.cs", 665, "Break");
break;}}                    MemberMapping m = members[i];
global::AutoRepairTracing.TraceLogger.Log("da7d828a-2172-4682-a2cb-3427749dc9d1", "ReflectionXmlSerializationWriter.cs", 652, "Variable Update", m, "m");

                    bool isSpecified = true;
global::AutoRepairTracing.TraceLogger.Log("440214bc-1535-451b-9f1b-df95f08314fb", "ReflectionXmlSerializationWriter.cs", 654, "Variable Update", isSpecified, "isSpecified");
                    bool shouldPersist = true;
global::AutoRepairTracing.TraceLogger.Log("bd95997a-ec76-41e4-ac8f-2ef70d498abf", "ReflectionXmlSerializationWriter.cs", 655, "Variable Update", shouldPersist, "shouldPersist");
                    if (m.CheckSpecified != SpecifiedAccessor.None)
                    {
global::AutoRepairTracing.TraceLogger.Log("81af08ec-5971-40b1-a645-f56a453e45da", "ReflectionXmlSerializationWriter.cs", 657, "If-True Branch");
                        string specifiedMemberName = $"{m.Name}Specified";
global::AutoRepairTracing.TraceLogger.Log("d6369f89-af9c-4e5c-811e-3b3a09cc6765", "ReflectionXmlSerializationWriter.cs", 658, "Variable Update", specifiedMemberName, "specifiedMemberName");
global::AutoRepairTracing.TraceLogger.LogValue("206fb1a9-fddd-4458-8351-023dba38b7c9", "ReflectionXmlSerializationWriter.cs", 1075, "Variable Update", isSpecified = (bool)GetMemberValue(o!, specifiedMemberName)!, "isSpecified");
                    }

                    if (m.CheckShouldPersist)
                    {
global::AutoRepairTracing.TraceLogger.Log("39be175d-d159-43a4-ae2a-7cfda0d77007", "ReflectionXmlSerializationWriter.cs", 663, "If-True Branch");
                        string methodInvoke = $"ShouldSerialize{m.Name}";
global::AutoRepairTracing.TraceLogger.Log("0dcbee39-4169-49d4-a2ec-80070b0314c7", "ReflectionXmlSerializationWriter.cs", 664, "Variable Update", methodInvoke, "methodInvoke");
                        MethodInfo method = o!.GetType().GetMethod(methodInvoke, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)!;
global::AutoRepairTracing.TraceLogger.Log("8b609000-ca9f-4940-86e8-4ac3b1280d84", "ReflectionXmlSerializationWriter.cs", 665, "Variable Update", method, "method");
global::AutoRepairTracing.TraceLogger.LogValue("750d3f22-9f77-4092-9e8b-69de2876a052", "ReflectionXmlSerializationWriter.cs", 1083, "Variable Update", shouldPersist = (bool)method.Invoke(o, Array.Empty<object>())!, "shouldPersist");
                    }

                    if (m.Attribute != null)
                    {
global::AutoRepairTracing.TraceLogger.Log("7140744f-0bde-4e80-bfef-69b34fe2e88f", "ReflectionXmlSerializationWriter.cs", 670, "If-True Branch");
                        if (isSpecified && shouldPersist)
                        {
global::AutoRepairTracing.TraceLogger.Log("28238672-c16f-40dd-a7dd-ed98936c91b5", "ReflectionXmlSerializationWriter.cs", 672, "If-True Branch");
                            object? memberValue = GetMemberValue(o!, m.Name);
global::AutoRepairTracing.TraceLogger.Log("aaea41f6-1ba6-41a8-b15f-1accb4f0693a", "ReflectionXmlSerializationWriter.cs", 673, "Variable Update", memberValue, "memberValue");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("2d72a84f-e8fc-48c9-aa02-27747c11db0b", "ReflectionXmlSerializationWriter.cs", 674, "WriteMember", 0, memberValue, "memberValue");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("eb374edf-6e79-4f9f-913c-e18df9062cdc", "ReflectionXmlSerializationWriter.cs", 674, "WriteMember", 1, m.Attribute, "m.Attribute");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("3bfc4219-d74a-422c-8c1a-ceec4bc99572", "ReflectionXmlSerializationWriter.cs", 674, "WriteMember", 2, m.TypeDesc!, "m.TypeDesc!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("f1bfee57-097d-4299-a55b-c1a7e88b7da3", "ReflectionXmlSerializationWriter.cs", 674, "WriteMember", 3, o, "o");
                            WriteMember(memberValue, m.Attribute, m.TypeDesc!, o);
}                        }
                    }
                }
}{int __loopCounter_9b11ba8f = 0;global::AutoRepairTracing.TraceLogger.Log("ed41fa66-e3c6-4d37-b069-16999b8d66ad", "ReflectionXmlSerializationWriter.cs", 1101, "Variable Update", __loopCounter_9b11ba8f, "__loopCounter_9b11ba8f");

                for (int i = 0; i < members.Length; global::AutoRepairTracing.TraceLogger.LogValue("596ae9c3-8657-4a63-bc9e-045ca65e346d", "ReflectionXmlSerializationWriter.cs", 1102, "Variable Update", i++, "i"))
                {
global::AutoRepairTracing.TraceLogger.Log("92438527-0147-483e-9c80-f63430017b58", "ReflectionXmlSerializationWriter.cs", 679, "For Loop");
global::AutoRepairTracing.TraceLogger.LogValue("a530afe6-add0-4b34-8176-680763c18f18", "ReflectionXmlSerializationWriter.cs", 1105, "Variable Update", __loopCounter_9b11ba8f++, "__loopCounter_9b11ba8f");if(__loopCounter_9b11ba8f > 10000){global::AutoRepairTracing.TraceLogger.Log("9b747cec-a123-4379-987b-c25a637e25a8", "ReflectionXmlSerializationWriter.cs", 694, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("9b11ba8f-6364-4258-8327-97c4229fa583", "ReflectionXmlSerializationWriter.cs", 679, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_9b11ba8f, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("5c943415-92e9-4ea0-8671-871e86ca603b", "ReflectionXmlSerializationWriter.cs", 694, "Break");
break;}}                    MemberMapping m = members[i];
global::AutoRepairTracing.TraceLogger.Log("0370f64f-bdb1-45a9-96e9-719823e0d86d", "ReflectionXmlSerializationWriter.cs", 681, "Variable Update", m, "m");

                    if (m.Xmlns != null)
{global::AutoRepairTracing.TraceLogger.Log("662c6296-0970-43bf-878a-8926cd3582dd", "ReflectionXmlSerializationWriter.cs", 684, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("393e90f4-d00c-4eaf-8faa-54ad937dcf61", "ReflectionXmlSerializationWriter.cs", 684, "Continue");
                        continue;
}
                    bool isSpecified = true;
global::AutoRepairTracing.TraceLogger.Log("9fe165c0-d13b-49d3-bb30-a770a565e84e", "ReflectionXmlSerializationWriter.cs", 686, "Variable Update", isSpecified, "isSpecified");
                    bool shouldPersist = true;
global::AutoRepairTracing.TraceLogger.Log("44921056-40f0-470b-b758-1910051db16a", "ReflectionXmlSerializationWriter.cs", 687, "Variable Update", shouldPersist, "shouldPersist");
                    if (m.CheckSpecified != SpecifiedAccessor.None)
                    {
global::AutoRepairTracing.TraceLogger.Log("dc82ef26-8725-4cea-9c9d-df5890d215bd", "ReflectionXmlSerializationWriter.cs", 689, "If-True Branch");
                        string specifiedMemberName = $"{m.Name}Specified";
global::AutoRepairTracing.TraceLogger.Log("fcb7f266-5341-4002-8b83-9dc1143a5b89", "ReflectionXmlSerializationWriter.cs", 690, "Variable Update", specifiedMemberName, "specifiedMemberName");
global::AutoRepairTracing.TraceLogger.LogValue("bb9c0fae-768c-453e-8460-aa947cb593f1", "ReflectionXmlSerializationWriter.cs", 1120, "Variable Update", isSpecified = (bool)GetMemberValue(o!, specifiedMemberName)!, "isSpecified");
                    }

                    if (m.CheckShouldPersist)
                    {
global::AutoRepairTracing.TraceLogger.Log("15b561ad-13cb-4ccb-9725-2474f0cd6453", "ReflectionXmlSerializationWriter.cs", 695, "If-True Branch");
                        string methodInvoke = $"ShouldSerialize{m.Name}";
global::AutoRepairTracing.TraceLogger.Log("687d4960-ae8b-438f-bc10-21d9a6eedf95", "ReflectionXmlSerializationWriter.cs", 696, "Variable Update", methodInvoke, "methodInvoke");
                        MethodInfo method = o!.GetType().GetMethod(methodInvoke, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)!;
global::AutoRepairTracing.TraceLogger.Log("e8ad4a50-e052-4848-a5d0-dfb0a115f15c", "ReflectionXmlSerializationWriter.cs", 697, "Variable Update", method, "method");
global::AutoRepairTracing.TraceLogger.LogValue("63217828-b01b-4a95-b3dc-8bfb3843eb7f", "ReflectionXmlSerializationWriter.cs", 1128, "Variable Update", shouldPersist = (bool)method.Invoke(o, Array.Empty<object>())!, "shouldPersist");
                    }

                    bool checkShouldPersist = m.CheckShouldPersist && (m.Elements!.Length > 0 || m.Text != null);
global::AutoRepairTracing.TraceLogger.Log("23ec7fd6-21fc-4e12-bab3-d6c24b60820f", "ReflectionXmlSerializationWriter.cs", 701, "Variable Update", checkShouldPersist, "checkShouldPersist");

                    if (!checkShouldPersist)
                    {
global::AutoRepairTracing.TraceLogger.Log("9ec74790-6b4c-487b-bff1-7b60ddc24258", "ReflectionXmlSerializationWriter.cs", 704, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("19cd26b3-3f60-42f7-9bb2-3fb90c487439", "ReflectionXmlSerializationWriter.cs", 1136, "Variable Update", shouldPersist = true, "shouldPersist");
                    }

                    if (isSpecified && shouldPersist)
                    {
global::AutoRepairTracing.TraceLogger.Log("a01f8e23-b1e2-4a5c-b53b-7a87c6e17ab2", "ReflectionXmlSerializationWriter.cs", 709, "If-True Branch");
                        object? choiceSource = null;
global::AutoRepairTracing.TraceLogger.Log("11ca4ca7-dc21-45ce-98d2-c305d592ab5e", "ReflectionXmlSerializationWriter.cs", 710, "Variable Update", choiceSource, "choiceSource");
                        if (m.ChoiceIdentifier != null)
                        {
global::AutoRepairTracing.TraceLogger.Log("f5abb7f9-fd1b-4364-87b6-450dd3187e59", "ReflectionXmlSerializationWriter.cs", 712, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("88b561eb-3bcf-42f7-bc2a-a16c82c7c671", "ReflectionXmlSerializationWriter.cs", 1146, "Variable Update", choiceSource = GetMemberValue(o!, m.ChoiceIdentifier.MemberName!), "choiceSource");
                        }

                        object? memberValue = GetMemberValue(o!, m.Name);
global::AutoRepairTracing.TraceLogger.Log("5ddbed39-24a4-4df7-9c04-949e838e2332", "ReflectionXmlSerializationWriter.cs", 716, "Variable Update", memberValue, "memberValue");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("01869ae7-ebd9-46b8-9295-ae4c8d182672", "ReflectionXmlSerializationWriter.cs", 717, "WriteMember", 0, memberValue, "memberValue");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("af524fd3-7326-47bb-a27c-555337dbc671", "ReflectionXmlSerializationWriter.cs", 717, "WriteMember", 1, choiceSource, "choiceSource");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("53361f15-d024-4a4a-a9ec-85309a6b0e8d", "ReflectionXmlSerializationWriter.cs", 717, "WriteMember", 2, m.ElementsSortedByDerivation!, "m.ElementsSortedByDerivation!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("f6fad23e-d1e7-46fc-b374-7e86f8f4e449", "ReflectionXmlSerializationWriter.cs", 717, "WriteMember", 3, m.Text, "m.Text");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("9799820e-bb58-4e8c-8ead-8f12421a9b97", "ReflectionXmlSerializationWriter.cs", 717, "WriteMember", 4, m.ChoiceIdentifier, "m.ChoiceIdentifier");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("c4825a4d-fc05-4049-b1e6-046bea2842f3", "ReflectionXmlSerializationWriter.cs", 717, "WriteMember", 5, m.TypeDesc!, "m.TypeDesc!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("34540546-827d-4274-9b93-aa2ae5ed5c1e", "ReflectionXmlSerializationWriter.cs", 717, "WriteMember", 6, true, "true");
                        WriteMember(memberValue, choiceSource, m.ElementsSortedByDerivation!, m.Text, m.ChoiceIdentifier, m.TypeDesc!, true);
}                    }
                }
}
                if (!mapping.IsSoap)
                {
global::AutoRepairTracing.TraceLogger.Log("f257ee5f-4439-4c2e-91b8-779e2e9f0c67", "ReflectionXmlSerializationWriter.cs", 722, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("8b122790-fe08-4f1e-bca5-f522529a65b6", "ReflectionXmlSerializationWriter.cs", 723, "WriteEndElement", 0, o, "o");
                    WriteEndElement(o);
}                }
            }
        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("4881d1f2-99d3-44ed-8a74-c6f198dde68e", "ReflectionXmlSerializationWriter.cs", 582, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("2cd99195-44f4-4dfe-857f-f77746ddb1be", "ReflectionXmlSerializationWriter.cs", 740, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("068a1c3e-8297-4e35-a38a-a936c7dabea9", "ReflectionXmlSerializationWriter.cs", 582, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("2c205390-775a-44cf-8c90-0a03f671fe21", "ReflectionXmlSerializationWriter.cs", 582, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("4646cc90-5eaa-4fa8-964d-11f3c4577fb6", "ReflectionXmlSerializationWriter.cs", 582, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private static object? GetMemberValue(object o, string memberName)
{global::AutoRepairTracing.TraceLogger.Log("411a3801-b9d5-468a-824e-bf1b709b1dde", "ReflectionXmlSerializationWriter.cs", 728, "Enter Method 'GetMemberValue'");
try        {
            MemberInfo memberInfo = ReflectionXmlSerializationHelper.GetEffectiveGetInfo(o.GetType(), memberName);
global::AutoRepairTracing.TraceLogger.Log("5a86f826-470e-4b19-b35f-2d7750afe6b4", "ReflectionXmlSerializationWriter.cs", 730, "Variable Update", memberInfo, "memberInfo");
            object? memberValue = GetMemberValue(o, memberInfo);
global::AutoRepairTracing.TraceLogger.Log("3a523ed5-5d4b-4ca7-b96d-56974ea6ad39", "ReflectionXmlSerializationWriter.cs", 731, "Variable Update", memberValue, "memberValue");
{global::AutoRepairTracing.TraceLogger.Log("d25ff61a-91e3-4085-90bd-2df673415fa3", "ReflectionXmlSerializationWriter.cs", 732, "Return");
            return memberValue;
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("adf79c95-1a6c-4864-bad4-d6f2fe1e4617", "ReflectionXmlSerializationWriter.cs", 728, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("7e8b26b9-2a01-41f0-a0ac-0f90ea76c5ea", "ReflectionXmlSerializationWriter.cs", 748, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("51c55597-d13b-4382-bd58-170b2dea5161", "ReflectionXmlSerializationWriter.cs", 728, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("09195439-7b31-4652-a255-1c22dc70fb85", "ReflectionXmlSerializationWriter.cs", 728, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("073834e3-e947-4ab2-ae51-949848e1f36b", "ReflectionXmlSerializationWriter.cs", 728, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private bool WriteEnumAndArrayTypes(object o, string n, string? ns)
{global::AutoRepairTracing.TraceLogger.Log("4bf0dc75-9141-40e5-9dde-e502a58352ce", "ReflectionXmlSerializationWriter.cs", 735, "Enter Method 'WriteEnumAndArrayTypes'");
try        {
            Type objType = o.GetType();
global::AutoRepairTracing.TraceLogger.Log("156a2563-4883-4011-8f66-78c7f0e653c8", "ReflectionXmlSerializationWriter.cs", 737, "Variable Update", objType, "objType");

            foreach (var m in _mapping.Scope!.TypeMappings)
            {
global::AutoRepairTracing.TraceLogger.Log("62ac6516-932f-4587-8d10-be62ebb75d2d", "ReflectionXmlSerializationWriter.cs", 739, "ForEach Loop");
                if (m is EnumMapping em && em.TypeDesc!.Type == objType)
                {
global::AutoRepairTracing.TraceLogger.Log("bbf8fb37-2243-4dd9-94bb-44a3c3264294", "ReflectionXmlSerializationWriter.cs", 742, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("0fdaeb55-9e46-490e-a080-7d0fd8f23d51", "ReflectionXmlSerializationWriter.cs", 743, "WriteStartElement", 0, n, "n");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("4e7750af-adb4-4e5a-8bc7-166c6b0b4068", "ReflectionXmlSerializationWriter.cs", 743, "WriteStartElement", 1, ns, "ns");
                    Writer.WriteStartElement(n, ns);
}{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("89e7dd28-81fb-4f2b-8b23-159ebe37efc6", "ReflectionXmlSerializationWriter.cs", 744, "WriteXsiType", 0, em.TypeName!, "em.TypeName!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("ff2bb4f6-e6ad-4bec-a7ae-56ecca1207f4", "ReflectionXmlSerializationWriter.cs", 744, "WriteXsiType", 1, ns, "ns");
                    WriteXsiType(em.TypeName!, ns);
}{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("0c5825ce-10ac-4222-9c61-2faf07928130", "ReflectionXmlSerializationWriter.cs", 745, "WriteString", 0, WriteEnumMethod(em, o), "WriteEnumMethod(em, o)");
                    Writer.WriteString(WriteEnumMethod(em, o));
}                    Writer.WriteEndElement();
{global::AutoRepairTracing.TraceLogger.Log("119b6efb-0729-419d-96b1-386c29a3e292", "ReflectionXmlSerializationWriter.cs", 747, "Return");
                    return true;
}                }

                if (m is ArrayMapping am && am.TypeDesc!.Type == objType)
                {
global::AutoRepairTracing.TraceLogger.Log("73155cd8-f1b2-46c4-a729-89514072142b", "ReflectionXmlSerializationWriter.cs", 751, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("27fa84ea-9669-48c2-b280-bdf9c1de7583", "ReflectionXmlSerializationWriter.cs", 752, "WriteStartElement", 0, n, "n");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("80141d63-90d1-47b4-8cad-a74211d8b844", "ReflectionXmlSerializationWriter.cs", 752, "WriteStartElement", 1, ns, "ns");
                    Writer.WriteStartElement(n, ns);
}{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("2e02de41-b9dd-4ac4-971b-36f03d1b56ce", "ReflectionXmlSerializationWriter.cs", 753, "WriteXsiType", 0, am.TypeName!, "am.TypeName!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("1cdc0144-a51a-45b7-960f-8b8ccf8d7545", "ReflectionXmlSerializationWriter.cs", 753, "WriteXsiType", 1, ns, "ns");
                    WriteXsiType(am.TypeName!, ns);
}{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("3bfcbf85-d473-4669-bc35-770719b34eab", "ReflectionXmlSerializationWriter.cs", 754, "WriteMember", 0, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("5cc53939-f30c-4191-832a-1fd1ef9a30b3", "ReflectionXmlSerializationWriter.cs", 754, "WriteMember", 1, null, "null");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("788a2553-58d9-4a94-b237-f7ed0aec1620", "ReflectionXmlSerializationWriter.cs", 754, "WriteMember", 2, am.ElementsSortedByDerivation!, "am.ElementsSortedByDerivation!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("dc6f2b12-36b1-4b1d-854c-71d679dea8ab", "ReflectionXmlSerializationWriter.cs", 754, "WriteMember", 3, null, "null");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("5b689f84-cf09-4f52-bb9f-946a81034438", "ReflectionXmlSerializationWriter.cs", 754, "WriteMember", 4, null, "null");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("09de451e-451b-4212-be20-2c52e5014d35", "ReflectionXmlSerializationWriter.cs", 754, "WriteMember", 5, am.TypeDesc!, "am.TypeDesc!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("5032939e-8fe2-416c-b2d9-465ddd7970e9", "ReflectionXmlSerializationWriter.cs", 754, "WriteMember", 6, true, "true");
                    WriteMember(o, null, am.ElementsSortedByDerivation!, null, null, am.TypeDesc!, true);
}                    Writer.WriteEndElement();
{global::AutoRepairTracing.TraceLogger.Log("28ca4e9c-3d41-42d8-8c29-2ad53072cfca", "ReflectionXmlSerializationWriter.cs", 756, "Return");
                    return true;
}                }
            }
{global::AutoRepairTracing.TraceLogger.Log("d4184a85-5e14-4ea1-ab08-51286d79db94", "ReflectionXmlSerializationWriter.cs", 760, "Return");

            return false;
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("bc9146b3-8a9c-40a9-ad9f-3722fd8477b9", "ReflectionXmlSerializationWriter.cs", 735, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("0b0faf56-e0c9-442c-9af7-49036294d530", "ReflectionXmlSerializationWriter.cs", 777, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("fa590303-1928-4cf7-b708-3be4a870ebe5", "ReflectionXmlSerializationWriter.cs", 735, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("bb67f4be-a05f-4dd0-9a43-1d691f48f8e4", "ReflectionXmlSerializationWriter.cs", 735, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("c6b74784-7626-4a30-aca7-563a08b5e0f0", "ReflectionXmlSerializationWriter.cs", 735, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private string? WriteEnumMethod(EnumMapping mapping, object v)
{global::AutoRepairTracing.TraceLogger.Log("8fc6543d-ddb5-461e-87b7-8a57b7b4ca60", "ReflectionXmlSerializationWriter.cs", 763, "Enter Method 'WriteEnumMethod'");
try        {
            string? returnString = null;
global::AutoRepairTracing.TraceLogger.Log("26eb83f0-8a2d-4cb6-bb82-7b3021e21cce", "ReflectionXmlSerializationWriter.cs", 765, "Variable Update", returnString, "returnString");
global::AutoRepairTracing.TraceLogger.Log("3d8e93d6-7320-4d7f-a0da-04dd3925b2be", "ReflectionXmlSerializationWriter.cs", 765, "Null Assignment Warning", "null", "returnString");            if (mapping != null)
            {
global::AutoRepairTracing.TraceLogger.Log("a77ca204-4764-4416-b88d-aaff681ec91d", "ReflectionXmlSerializationWriter.cs", 767, "If-True Branch");
                ConstantMapping[] constants = mapping.Constants!;
global::AutoRepairTracing.TraceLogger.Log("fd52e07e-57b7-400d-b3a1-242d7b6069c5", "ReflectionXmlSerializationWriter.cs", 768, "Heap State: constants (ConstantMapping[])");global::AutoRepairTracing.TraceLogger.Log("bcf52abb-943c-42ba-ab6c-262a88ac573d", "ReflectionXmlSerializationWriter.cs", 768, "Variable Update", constants, "constants");
                if (constants.Length > 0)
                {
global::AutoRepairTracing.TraceLogger.Log("f8978f14-85d9-4532-ba11-f2b28617cb69", "ReflectionXmlSerializationWriter.cs", 770, "If-True Branch");
                    bool foundValue = false;
global::AutoRepairTracing.TraceLogger.Log("7bdb62ad-bfe6-4364-b2ee-45b1be4f7905", "ReflectionXmlSerializationWriter.cs", 771, "Variable Update", foundValue, "foundValue");
                    var enumValue = Convert.ToInt64(v);
global::AutoRepairTracing.TraceLogger.Log("33fd1e58-9b4e-40db-8be3-5e4e527750ef", "ReflectionXmlSerializationWriter.cs", 772, "Variable Update", enumValue, "enumValue");
{int __loopCounter_f41a05ef = 0;global::AutoRepairTracing.TraceLogger.Log("8ac601aa-7907-46c8-92a3-e2beed15cf5d", "ReflectionXmlSerializationWriter.cs", 1249, "Variable Update", __loopCounter_f41a05ef, "__loopCounter_f41a05ef");
                    for (int i = 0; i < constants.Length; global::AutoRepairTracing.TraceLogger.LogValue("4a0cd427-b038-489d-a618-23cb658f1b7c", "ReflectionXmlSerializationWriter.cs", 1249, "Variable Update", i++, "i"))
                    {
global::AutoRepairTracing.TraceLogger.Log("b780f477-666b-408e-a9b1-4adbd791e4e5", "ReflectionXmlSerializationWriter.cs", 773, "For Loop");
global::AutoRepairTracing.TraceLogger.LogValue("ca900944-1599-4470-b6d6-98ff3d7e920e", "ReflectionXmlSerializationWriter.cs", 1252, "Variable Update", __loopCounter_f41a05ef++, "__loopCounter_f41a05ef");if(__loopCounter_f41a05ef > 10000){global::AutoRepairTracing.TraceLogger.Log("0e714c4c-be4a-43c8-bfa6-3ae5dc1a6f8b", "ReflectionXmlSerializationWriter.cs", 791, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("f41a05ef-1e30-41eb-9b3d-80bfc7a8d634", "ReflectionXmlSerializationWriter.cs", 773, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_f41a05ef, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("c833dd63-e584-48a9-a1ef-d7780f27dad4", "ReflectionXmlSerializationWriter.cs", 791, "Break");
break;}}                        ConstantMapping c = constants[i];
global::AutoRepairTracing.TraceLogger.Log("2316843c-a7c5-466d-b472-1c517a91fa16", "ReflectionXmlSerializationWriter.cs", 775, "Variable Update", c, "c");
                        if (enumValue == c.Value)
                        {
global::AutoRepairTracing.TraceLogger.Log("e39b287b-252a-459a-a3bd-dbf0f842b6c7", "ReflectionXmlSerializationWriter.cs", 777, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("771d7ba1-05d2-4246-a354-8052da918d49", "ReflectionXmlSerializationWriter.cs", 1258, "Variable Update", returnString = c.XmlName, "returnString");
global::AutoRepairTracing.TraceLogger.LogValue("9843b2ff-de7b-449f-99a8-db4649c5febc", "ReflectionXmlSerializationWriter.cs", 1259, "Variable Update", foundValue = true, "foundValue");
{global::AutoRepairTracing.TraceLogger.Log("f74763a6-39f4-47c7-8e5a-de61ef689be4", "ReflectionXmlSerializationWriter.cs", 780, "Break");
                            break;
}                        }
                    }
}
                    if (!foundValue)
                    {
global::AutoRepairTracing.TraceLogger.Log("598600df-4b1e-4c55-8071-3ec93135e05e", "ReflectionXmlSerializationWriter.cs", 785, "If-True Branch");
                        if (mapping.IsFlags)
                        {
global::AutoRepairTracing.TraceLogger.Log("66e8b90d-521e-4e03-9f4b-2006be3ba5d6", "ReflectionXmlSerializationWriter.cs", 787, "If-True Branch");
                            string[] xmlNames = new string[constants.Length];
global::AutoRepairTracing.TraceLogger.Log("e1394cd8-496f-4f99-bb8a-bbadd1874b82", "ReflectionXmlSerializationWriter.cs", 788, "Heap State: xmlNames (string[] size=constants.Length)");global::AutoRepairTracing.TraceLogger.Log("94595d08-f551-4034-b2d9-f246a00b8ccf", "ReflectionXmlSerializationWriter.cs", 788, "Variable Update", xmlNames, "xmlNames");
                            long[] valueIds = new long[constants.Length];
global::AutoRepairTracing.TraceLogger.Log("8364963e-d92d-4bdd-b365-4956dc24524b", "ReflectionXmlSerializationWriter.cs", 789, "Heap State: valueIds (long[] size=constants.Length)");global::AutoRepairTracing.TraceLogger.Log("0a6f6ddd-c668-4382-90b6-6db4358abcdd", "ReflectionXmlSerializationWriter.cs", 789, "Variable Update", valueIds, "valueIds");
{int __loopCounter_be3163de = 0;global::AutoRepairTracing.TraceLogger.Log("9f62fd63-7a2f-4444-9b0d-d289969afb0d", "ReflectionXmlSerializationWriter.cs", 1273, "Variable Update", __loopCounter_be3163de, "__loopCounter_be3163de");

                            for (int i = 0; i < constants.Length; global::AutoRepairTracing.TraceLogger.LogValue("936f3d4c-8d1c-49c4-b0a5-7395e5d3deb4", "ReflectionXmlSerializationWriter.cs", 1274, "Variable Update", i++, "i"))
                            {
global::AutoRepairTracing.TraceLogger.Log("65499e51-f199-47ae-a59a-4c1e334b3189", "ReflectionXmlSerializationWriter.cs", 791, "For Loop");
global::AutoRepairTracing.TraceLogger.LogValue("4e7fb4ed-fef7-41d7-ac21-a24970f9afec", "ReflectionXmlSerializationWriter.cs", 1277, "Variable Update", __loopCounter_be3163de++, "__loopCounter_be3163de");if(__loopCounter_be3163de > 10000){global::AutoRepairTracing.TraceLogger.Log("16664ca8-d5d3-4675-9d50-abb11ffd7ef6", "ReflectionXmlSerializationWriter.cs", 809, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("be3163de-95b5-4a2e-bb4d-3a2b8c4c688c", "ReflectionXmlSerializationWriter.cs", 791, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_be3163de, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("be497e2c-0a3c-4a84-9e00-ae2d9f7e4f6d", "ReflectionXmlSerializationWriter.cs", 809, "Break");
break;}}{global::AutoRepairTracing.TraceLogger.Log("4bdac2d6-1187-47e5-88de-46ab79f7192c", "ReflectionXmlSerializationWriter.cs", 793, "Variable Update", i, "index:xmlNames");
global::AutoRepairTracing.TraceLogger.Log("d0dcf505-8644-47eb-8c21-613cb7d6b165", "ReflectionXmlSerializationWriter.cs", 793, "Variable Update", i, "index:constants");
global::AutoRepairTracing.TraceLogger.LogValue("b1fb06b7-84f4-4015-99c3-a53489e73cad", "ReflectionXmlSerializationWriter.cs", 1279, "Variable Update", xmlNames[i] = constants[i].XmlName, string.Concat("xmlNames[", i, "]"));
global::AutoRepairTracing.TraceLogger.LogArray("c6fe2d3e-32d2-49f1-9d89-14d28f1e29bd", "ReflectionXmlSerializationWriter.cs", 793, "Array Snapshot", xmlNames, "xmlNames");
}{global::AutoRepairTracing.TraceLogger.Log("3bef8c77-22ef-47c2-a1b6-ca31481f7a53", "ReflectionXmlSerializationWriter.cs", 794, "Variable Update", i, "index:valueIds");
global::AutoRepairTracing.TraceLogger.Log("dedfafce-a165-494c-a499-7d5408bba897", "ReflectionXmlSerializationWriter.cs", 794, "Variable Update", i, "index:constants");
global::AutoRepairTracing.TraceLogger.LogValue("70142cdb-b28e-47da-bdc8-f3307dbcb15f", "ReflectionXmlSerializationWriter.cs", 1280, "Variable Update", valueIds[i] = constants[i].Value, string.Concat("valueIds[", i, "]"));
global::AutoRepairTracing.TraceLogger.LogArray("14c14c8a-f0b2-4625-843c-7e90acbce0ce", "ReflectionXmlSerializationWriter.cs", 794, "Array Snapshot", valueIds, "valueIds");
}                            }
}global::AutoRepairTracing.TraceLogger.LogValue("d574e02a-690f-4c27-a32a-24edbe63452e", "ReflectionXmlSerializationWriter.cs", 1283, "Variable Update", returnString = FromEnum(enumValue, xmlNames, valueIds), "returnString");
                        }
                        else
                        {
global::AutoRepairTracing.TraceLogger.Log("3aaad96d-13c2-4293-a2ca-c3ddfbdb03ca", "ReflectionXmlSerializationWriter.cs", 800, "Else Branch");
                            throw CreateInvalidEnumValueException(v, mapping.TypeDesc!.FullName);
                        }
                    }
                }
            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("9a71252e-713c-4759-8d41-2821b0145613", "ReflectionXmlSerializationWriter.cs", 807, "Else Branch");
global::AutoRepairTracing.TraceLogger.LogValue("2aae2d56-9147-4c82-9bf0-84f94f491faa", "ReflectionXmlSerializationWriter.cs", 1296, "Variable Update", returnString = v.ToString(), "returnString");
            }

            if (mapping!.IsSoap)
            {
global::AutoRepairTracing.TraceLogger.Log("9ae01e54-3ccb-496a-ab59-cc7b26005328", "ReflectionXmlSerializationWriter.cs", 812, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("c7facd4e-1668-4d9b-958f-d01a5b3cae8f", "ReflectionXmlSerializationWriter.cs", 813, "WriteXsiType", 0, mapping.TypeName!, "mapping.TypeName!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("f57510b3-ae7c-4d97-8fc0-1a2570128331", "ReflectionXmlSerializationWriter.cs", 813, "WriteXsiType", 1, mapping.Namespace, "mapping.Namespace");
                WriteXsiType(mapping.TypeName!, mapping.Namespace);
}{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("0decb61b-6288-4232-a74e-3724a1e83e1b", "ReflectionXmlSerializationWriter.cs", 814, "WriteString", 0, returnString, "returnString");
                Writer.WriteString(returnString);
}{global::AutoRepairTracing.TraceLogger.Log("b1765101-a59f-41f2-8e94-91b32013df92", "ReflectionXmlSerializationWriter.cs", 815, "Return");
                return null;
}            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("07057e57-7e95-4860-b7c8-5c4ceb6a5e4a", "ReflectionXmlSerializationWriter.cs", 818, "Else Branch");
{global::AutoRepairTracing.TraceLogger.Log("aa4a7c36-eb29-473c-8fa1-3157a33c3dcf", "ReflectionXmlSerializationWriter.cs", 819, "Return");
                return returnString;
}            }
        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("78ee599e-1fbe-4958-ba6e-4d5d4d15695f", "ReflectionXmlSerializationWriter.cs", 763, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("ae1ee64a-8ae6-4060-9f3c-18f95c46643a", "ReflectionXmlSerializationWriter.cs", 838, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("1a24938f-f0fd-42b2-9c08-6a23a7aaaa87", "ReflectionXmlSerializationWriter.cs", 763, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("386625a4-4a25-48de-8fef-92ddfe7319a0", "ReflectionXmlSerializationWriter.cs", 763, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("1cbc2746-94c6-46e1-a3fd-ca3f11d639f0", "ReflectionXmlSerializationWriter.cs", 763, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private static object? GetMemberValue(object? o, MemberInfo memberInfo)
{global::AutoRepairTracing.TraceLogger.Log("73df354e-4a6a-412c-94d7-c9ec5bc44da1", "ReflectionXmlSerializationWriter.cs", 823, "Enter Method 'GetMemberValue'");
try        {
            if (memberInfo is PropertyInfo memberProperty)
            {
global::AutoRepairTracing.TraceLogger.Log("b3b111ca-944e-4852-a91f-a193fd253a98", "ReflectionXmlSerializationWriter.cs", 826, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("94d30429-f53d-421b-a329-d4f8e6bad906", "ReflectionXmlSerializationWriter.cs", 827, "Return");
                return memberProperty.GetValue(o);
}            }
            else {global::AutoRepairTracing.TraceLogger.Log("63f581b9-0418-4e39-9429-c978b93fd680", "ReflectionXmlSerializationWriter.cs", 829, "Else Branch");
if (memberInfo is FieldInfo memberField)
            {
global::AutoRepairTracing.TraceLogger.Log("3b6f6e64-d761-4ac7-938a-85d6a5cad315", "ReflectionXmlSerializationWriter.cs", 830, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("08c1bb60-edc7-42b9-af8d-e6bc8535d4fa", "ReflectionXmlSerializationWriter.cs", 831, "Return");
                return memberField.GetValue(o);
}            }
}
            throw new InvalidOperationException(SR.XmlInternalError);
        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("81661967-fce6-440b-a4d0-6d59afd012c3", "ReflectionXmlSerializationWriter.cs", 823, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("3231d8be-32b6-4377-ab3f-9278ee8af608", "ReflectionXmlSerializationWriter.cs", 853, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("c1730a25-8268-47d9-9b30-1d726b96a644", "ReflectionXmlSerializationWriter.cs", 823, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("7d5fe0bd-0af7-4f74-b47a-9f316f91ef89", "ReflectionXmlSerializationWriter.cs", 823, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("a97c644d-343f-40bd-b6d2-8348d6971df6", "ReflectionXmlSerializationWriter.cs", 823, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private void WriteMember(object? memberValue, AttributeAccessor attribute, TypeDesc memberTypeDesc, object? container)
{global::AutoRepairTracing.TraceLogger.Log("111126d8-f5a9-425b-b55d-e82a520a0441", "ReflectionXmlSerializationWriter.cs", 837, "Enter Method 'WriteMember'");
try        {
            if (memberTypeDesc.IsAbstract) {global::AutoRepairTracing.TraceLogger.Log("e741dfdc-c573-4ac0-b7d8-63302ad48103", "ReflectionXmlSerializationWriter.cs", 839, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("2a79d3e9-2823-45f9-aaf5-22c3f8913ce9", "ReflectionXmlSerializationWriter.cs", 839, "Return");
return;
}            if (memberTypeDesc.IsArrayLike)
            {
global::AutoRepairTracing.TraceLogger.Log("df93af57-4b98-4f7a-9917-6ee6ab9010a3", "ReflectionXmlSerializationWriter.cs", 841, "If-True Branch");
                var sb = new StringBuilder();
global::AutoRepairTracing.TraceLogger.Log("6809e2be-6a1c-42a2-b3c4-82d230bb5dbb", "ReflectionXmlSerializationWriter.cs", 842, "Variable Update", sb, "sb");
                TypeDesc? arrayElementTypeDesc = memberTypeDesc.ArrayElementTypeDesc;
global::AutoRepairTracing.TraceLogger.Log("250c8116-7693-43e1-8a75-d4ddde07e4a1", "ReflectionXmlSerializationWriter.cs", 843, "Variable Update", arrayElementTypeDesc, "arrayElementTypeDesc");
                bool canOptimizeWriteListSequence = CanOptimizeWriteListSequence(arrayElementTypeDesc);
global::AutoRepairTracing.TraceLogger.Log("4535604a-84b8-44b4-a805-79f9d5c55609", "ReflectionXmlSerializationWriter.cs", 844, "Variable Update", canOptimizeWriteListSequence, "canOptimizeWriteListSequence");
                if (attribute.IsList)
                {
global::AutoRepairTracing.TraceLogger.Log("b015d23d-3f6f-44cb-8f2f-39975e46729e", "ReflectionXmlSerializationWriter.cs", 846, "If-True Branch");
                    if (canOptimizeWriteListSequence)
                    {
global::AutoRepairTracing.TraceLogger.Log("54c8fdc4-a92b-45ab-b30f-1d274c591a67", "ReflectionXmlSerializationWriter.cs", 848, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("f2d0d1ea-4252-4c99-b668-59f10a3ae48d", "ReflectionXmlSerializationWriter.cs", 849, "WriteStartAttribute", 0, null, "null");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("bd756452-0132-4aa7-9a19-cd195cc0098e", "ReflectionXmlSerializationWriter.cs", 849, "WriteStartAttribute", 1, attribute.Name, "attribute.Name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("a4f6cc93-ab4c-49c8-869f-64c642c88264", "ReflectionXmlSerializationWriter.cs", 849, "WriteStartAttribute", 2, attribute.Form == XmlSchemaForm.Qualified ? attribute.Namespace : string.Empty, "attribute.Form == XmlSchemaForm.Qualified ? attribute.Namespace : string.Empty");
                        Writer.WriteStartAttribute(null, attribute.Name, attribute.Form == XmlSchemaForm.Qualified ? attribute.Namespace : string.Empty);
}                    }
                }

                if (memberValue != null)
                {
global::AutoRepairTracing.TraceLogger.Log("573b40ea-33da-4b2a-86ff-76ea9b22636a", "ReflectionXmlSerializationWriter.cs", 854, "If-True Branch");
                    var a = (IEnumerable)global::AutoRepairTracing.TraceLogger.LogCastSource("f4649818-26d2-4a37-b63b-4ef5960acd98", "ReflectionXmlSerializationWriter.cs", 1460, (object)(memberValue), "IEnumerable");
global::AutoRepairTracing.TraceLogger.Log("b9e49e7e-1b73-4614-8f17-60e31d456080", "ReflectionXmlSerializationWriter.cs", 855, "Variable Update", a, "a");
                    IEnumerator e = a.GetEnumerator();
global::AutoRepairTracing.TraceLogger.Log("ca140f23-422a-49cd-9466-98175c081a48", "ReflectionXmlSerializationWriter.cs", 856, "Variable Update", e, "e");
                    bool shouldAppendWhitespace = false;
global::AutoRepairTracing.TraceLogger.Log("be9acbbb-4900-4dc8-8762-33cb9ce186ab", "ReflectionXmlSerializationWriter.cs", 857, "Variable Update", shouldAppendWhitespace, "shouldAppendWhitespace");
                    if (e != null)
                    {
global::AutoRepairTracing.TraceLogger.Log("c9325233-238f-4c26-abb7-6758707cdebc", "ReflectionXmlSerializationWriter.cs", 859, "If-True Branch");
{int __loopCounter_464726ca = 0;global::AutoRepairTracing.TraceLogger.Log("83191399-06af-4851-9ef4-ff9441e622e3", "ReflectionXmlSerializationWriter.cs", 1376, "Variable Update", __loopCounter_464726ca, "__loopCounter_464726ca");
                        while (e.MoveNext())
                        {
global::AutoRepairTracing.TraceLogger.Log("bb19a77a-621b-415c-a70e-36214222147f", "ReflectionXmlSerializationWriter.cs", 860, "While Loop");
global::AutoRepairTracing.TraceLogger.LogValue("7bb8b632-cab4-492f-881f-a0b4618f0386", "ReflectionXmlSerializationWriter.cs", 1379, "Variable Update", __loopCounter_464726ca++, "__loopCounter_464726ca");if(__loopCounter_464726ca > 10000){global::AutoRepairTracing.TraceLogger.Log("6fa1af45-3427-4b7f-b83e-62e05c1cb6c4", "ReflectionXmlSerializationWriter.cs", 880, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("464726ca-e390-4cab-a86a-0c790fe6088a", "ReflectionXmlSerializationWriter.cs", 860, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_464726ca, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("7d5c1017-1dfc-456d-a944-575c3b04e183", "ReflectionXmlSerializationWriter.cs", 880, "Break");
break;}}                            object ai = e.Current;
global::AutoRepairTracing.TraceLogger.Log("ab9986f9-94af-44bc-a9be-148c22cfbced", "ReflectionXmlSerializationWriter.cs", 862, "Variable Update", ai, "ai");

                            if (attribute.IsList)
                            {
global::AutoRepairTracing.TraceLogger.Log("1ceb549b-f4b2-4a5d-b78d-38bcd0ccf03d", "ReflectionXmlSerializationWriter.cs", 865, "If-True Branch");
                                string? stringValue;
                                if (attribute.Mapping is EnumMapping enumMapping)
                                {
global::AutoRepairTracing.TraceLogger.Log("17c1c032-8e60-4671-98dc-c8e18392f237", "ReflectionXmlSerializationWriter.cs", 868, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("11495043-4422-4545-9ba3-f3999849e81c", "ReflectionXmlSerializationWriter.cs", 1390, "Variable Update", stringValue = WriteEnumMethod(enumMapping, ai), "stringValue");
                                }
                                else
                                {
global::AutoRepairTracing.TraceLogger.Log("ea492084-6fd7-41b2-b76a-6b73c4d10118", "ReflectionXmlSerializationWriter.cs", 872, "Else Branch");
                                    if (!WritePrimitiveValue(arrayElementTypeDesc!, ai, out stringValue))
                                    {
global::AutoRepairTracing.TraceLogger.Log("0dfc141e-7a46-4c67-a436-70f6338c79d6", "ReflectionXmlSerializationWriter.cs", 874, "If-True Branch");
                                        Debug.Assert(ai is byte[]);
                                    }
                                }

                                // check to see if we can write values of the attribute sequentially
                                if (canOptimizeWriteListSequence)
                                {
global::AutoRepairTracing.TraceLogger.Log("3f818bab-d2fd-4dae-8cc1-82e95aeacea9", "ReflectionXmlSerializationWriter.cs", 881, "If-True Branch");
                                    if (shouldAppendWhitespace)
                                    {
global::AutoRepairTracing.TraceLogger.Log("04b4c38e-163e-455a-bc0d-9b7f739c4947", "ReflectionXmlSerializationWriter.cs", 883, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("53314797-a30a-477f-92e9-de013b5aeef1", "ReflectionXmlSerializationWriter.cs", 884, "WriteString", 0, " ", "\" \"");
                                        Writer.WriteString(" ");
}                                    }

                                    if (ai is byte[])
                                    {
global::AutoRepairTracing.TraceLogger.Log("b03e40d1-600c-4292-85f7-5337f681f7ce", "ReflectionXmlSerializationWriter.cs", 888, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("78fc049a-7fe1-4a3e-9963-6ca90b19834a", "ReflectionXmlSerializationWriter.cs", 889, "WriteValue", 0, (byte[])global::AutoRepairTracing.TraceLogger.LogCastSource("2a35fe48-e8d2-45c3-bfe0-c0e2bf28363a", "ReflectionXmlSerializationWriter.cs", 1511, (object)(ai), "byte[]"), "(byte[])ai");
                                        WriteValue((byte[])global::AutoRepairTracing.TraceLogger.LogCastSource("6e3574e3-d12a-4c96-b733-c2fca65c652e", "ReflectionXmlSerializationWriter.cs", 1512, (object)(ai), "byte[]"));
}                                    }
                                    else
                                    {
global::AutoRepairTracing.TraceLogger.Log("2e8de8f8-6ae5-4198-824e-dc3f2ccfcb08", "ReflectionXmlSerializationWriter.cs", 892, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("0c13e344-1838-4c2b-8f96-6ec5b26ba076", "ReflectionXmlSerializationWriter.cs", 893, "WriteValue", 0, stringValue, "stringValue");
                                        WriteValue(stringValue);
}                                    }
                                }
                                else
                                {
global::AutoRepairTracing.TraceLogger.Log("e93f2acd-20ca-4fbb-8caa-492df477c6ba", "ReflectionXmlSerializationWriter.cs", 897, "Else Branch");
                                    if (shouldAppendWhitespace)
                                    {
global::AutoRepairTracing.TraceLogger.Log("75065964-12eb-47e1-a512-17436b3801d8", "ReflectionXmlSerializationWriter.cs", 899, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("28151cff-f936-4092-b979-a5957838479d", "ReflectionXmlSerializationWriter.cs", 900, "Append", 0, ' ', "' '");
                                        sb.Append(' ');
}                                    }
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("688f33ed-3bd3-4f37-846c-bc56d43f6fe4", "ReflectionXmlSerializationWriter.cs", 903, "Append", 0, stringValue, "stringValue");

                                    sb.Append(stringValue);
}                                }
                            }
                            else
                            {
global::AutoRepairTracing.TraceLogger.Log("28e76632-e68b-43d7-8c24-41f6181d7fa8", "ReflectionXmlSerializationWriter.cs", 907, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("ed8aa3be-8906-47dc-aa09-bbe0e3563553", "ReflectionXmlSerializationWriter.cs", 908, "WriteAttribute", 0, ai, "ai");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d5a7e247-3833-4d91-a38b-0ce89a2025f8", "ReflectionXmlSerializationWriter.cs", 908, "WriteAttribute", 1, attribute, "attribute");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("8a60526e-7d8b-431c-b920-3e2524ad5e5d", "ReflectionXmlSerializationWriter.cs", 908, "WriteAttribute", 2, container, "container");
                                WriteAttribute(ai, attribute, container);
}                            }
global::AutoRepairTracing.TraceLogger.LogValue("0e77a668-2fb4-4bb7-9290-ac065a13d914", "ReflectionXmlSerializationWriter.cs", 1449, "Variable Update", shouldAppendWhitespace = true, "shouldAppendWhitespace");
                        }
}
                        if (attribute.IsList)
                        {
global::AutoRepairTracing.TraceLogger.Log("4af3cd85-0093-4424-a7a0-f05b1db63229", "ReflectionXmlSerializationWriter.cs", 915, "If-True Branch");
                            // check to see if we can write values of the attribute sequentially
                            if (canOptimizeWriteListSequence)
                            {
global::AutoRepairTracing.TraceLogger.Log("5ad7eb4c-b167-485c-b3d0-5ddcbcfd7102", "ReflectionXmlSerializationWriter.cs", 918, "If-True Branch");
                                Writer.WriteEndAttribute();
                            }
                            else
                            {
global::AutoRepairTracing.TraceLogger.Log("c2c20a08-baf8-40fe-92ab-bafc48011c1e", "ReflectionXmlSerializationWriter.cs", 922, "Else Branch");
                                if (sb.Length != 0)
                                {
global::AutoRepairTracing.TraceLogger.Log("1a1e04fc-0445-43a0-b98b-00a2934e62e6", "ReflectionXmlSerializationWriter.cs", 924, "If-True Branch");
                                    string? ns = attribute.Form == XmlSchemaForm.Qualified ? attribute.Namespace : string.Empty;
global::AutoRepairTracing.TraceLogger.Log("ce851f57-19d3-4925-b97b-01472ec52cc7", "ReflectionXmlSerializationWriter.cs", 925, "Variable Update", ns, "ns");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("ba40d92e-fbec-45da-8dff-973dd4d1179d", "ReflectionXmlSerializationWriter.cs", 926, "WriteAttribute", 0, attribute.Name, "attribute.Name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("ccb7ffd4-1ba8-471c-9a18-70df5f293b77", "ReflectionXmlSerializationWriter.cs", 926, "WriteAttribute", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("a36a70a0-7c8e-4052-8d7d-48b4d94c52af", "ReflectionXmlSerializationWriter.cs", 926, "WriteAttribute", 2, sb.ToString(), "sb.ToString()");
                                    WriteAttribute(attribute.Name, ns, sb.ToString());
}                                }
                            }
                        }
                    }
                }
            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("9271a976-db08-47e2-a4f6-0fc55dab3f87", "ReflectionXmlSerializationWriter.cs", 934, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("19379d57-732a-49fa-accc-3fe695a043b5", "ReflectionXmlSerializationWriter.cs", 935, "WriteAttribute", 0, memberValue!, "memberValue!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("b74e3c79-2a5b-4e18-8cb9-32842047f8f9", "ReflectionXmlSerializationWriter.cs", 935, "WriteAttribute", 1, attribute, "attribute");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("c599db3e-3495-4867-8f8f-119e7b6cd7ec", "ReflectionXmlSerializationWriter.cs", 935, "WriteAttribute", 2, container, "container");
                WriteAttribute(memberValue!, attribute, container);
}            }
        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("31979a21-db52-48b4-bfde-83670873c7cd", "ReflectionXmlSerializationWriter.cs", 837, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("b8409b21-1d05-456b-b6e0-2c336f4ae21f", "ReflectionXmlSerializationWriter.cs", 956, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("4bd9de72-bbe8-46d9-84e2-3f80e398d5cd", "ReflectionXmlSerializationWriter.cs", 837, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("5d58eea8-2731-4969-988f-673cbc0ac503", "ReflectionXmlSerializationWriter.cs", 837, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("34a7ee54-a6de-4185-b6bf-b0e7a4a3bed2", "ReflectionXmlSerializationWriter.cs", 837, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private static bool CanOptimizeWriteListSequence(TypeDesc? listElementTypeDesc)
{global::AutoRepairTracing.TraceLogger.Log("1c07d27e-7221-4884-9ccc-afdc4cfa60b8", "ReflectionXmlSerializationWriter.cs", 939, "Enter Method 'CanOptimizeWriteListSequence'");
try        {
{global::AutoRepairTracing.TraceLogger.Log("69a097f8-83f8-4998-aafe-befab15e1861", "ReflectionXmlSerializationWriter.cs", 944, "Return");
            // check to see if we can write values of the attribute sequentially
            // currently we have only one data type (XmlQualifiedName) that we can not write "inline",
            // because we need to output xmlns:qx="..." for each of the qnames
            return (listElementTypeDesc != null && listElementTypeDesc != ReflectionXmlSerializationReader.QnameTypeDesc);
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("a584bc6b-34e6-448e-b41d-e5a6c1b2397e", "ReflectionXmlSerializationWriter.cs", 939, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("e01bd1c9-fbcb-4a43-9aac-59a7b52c73a9", "ReflectionXmlSerializationWriter.cs", 965, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("36fe185d-6951-48d4-b35b-1447f77d2513", "ReflectionXmlSerializationWriter.cs", 939, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("7729e00d-a9a4-49b0-b20f-6ff9e8e07abb", "ReflectionXmlSerializationWriter.cs", 939, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("a1908a78-fd82-43ca-8fab-d454fc3be519", "ReflectionXmlSerializationWriter.cs", 939, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private void WriteAttribute(object memberValue, AttributeAccessor attribute, object? container)
{global::AutoRepairTracing.TraceLogger.Log("f2170a80-5593-461d-9194-e94c8892a6cf", "ReflectionXmlSerializationWriter.cs", 947, "Enter Method 'WriteAttribute'");
try        {
            // TODO: this block is never hit by our tests.
            if (attribute.Mapping is SpecialMapping special)
            {
global::AutoRepairTracing.TraceLogger.Log("30ac0606-acf5-465f-ae8f-7287559ed490", "ReflectionXmlSerializationWriter.cs", 951, "If-True Branch");
                if (special.TypeDesc!.Kind == TypeKind.Attribute || special.TypeDesc.CanBeAttributeValue)
                {
global::AutoRepairTracing.TraceLogger.Log("5ab58124-76a5-429d-9c80-7bb1dc610eec", "ReflectionXmlSerializationWriter.cs", 953, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("df69218b-210f-485f-a327-e2f6389ca66c", "ReflectionXmlSerializationWriter.cs", 954, "WriteXmlAttribute", 0, (XmlNode)global::AutoRepairTracing.TraceLogger.LogCastSource("de5c0f2c-6ef1-463a-b105-d8435de3edb9", "ReflectionXmlSerializationWriter.cs", 1607, (object)(memberValue), "XmlNode"), "(XmlNode)memberValue");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("aff0b9ec-597e-4bd4-b875-9239e80f8d22", "ReflectionXmlSerializationWriter.cs", 954, "WriteXmlAttribute", 1, container, "container");
                    WriteXmlAttribute((XmlNode)global::AutoRepairTracing.TraceLogger.LogCastSource("472b3e63-642b-4157-a925-cd3f79d2ec37", "ReflectionXmlSerializationWriter.cs", 1609, (object)(memberValue), "XmlNode"), container);
}                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("60b255aa-424b-44d6-bf18-b2ff155f4a94", "ReflectionXmlSerializationWriter.cs", 957, "Else Branch");
                    throw new InvalidOperationException(SR.XmlInternalError);
                }
            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("32974bb6-7e60-4ab0-bac1-9571adad626e", "ReflectionXmlSerializationWriter.cs", 962, "Else Branch");
                string? ns = attribute.Form == XmlSchemaForm.Qualified ? attribute.Namespace : string.Empty;
global::AutoRepairTracing.TraceLogger.Log("847eb71e-8557-4343-a2f4-3c1fed98c45e", "ReflectionXmlSerializationWriter.cs", 963, "Variable Update", ns, "ns");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("beab7989-fba3-43ff-9041-17401359384f", "ReflectionXmlSerializationWriter.cs", 964, "WritePrimitive", 0, WritePrimitiveMethodRequirement.WriteAttribute, "WritePrimitiveMethodRequirement.WriteAttribute");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("89a83a6d-b419-473c-a811-37164ec3b424", "ReflectionXmlSerializationWriter.cs", 964, "WritePrimitive", 1, attribute.Name, "attribute.Name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("df45f5fc-7c39-4d1d-9b68-710ac81defd6", "ReflectionXmlSerializationWriter.cs", 964, "WritePrimitive", 2, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("9c80ed4a-25ff-4936-a6b2-09bdff2f57c5", "ReflectionXmlSerializationWriter.cs", 964, "WritePrimitive", 3, attribute.Default, "attribute.Default");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("56ecf47d-a8f2-473d-b6fc-d62adb54d821", "ReflectionXmlSerializationWriter.cs", 964, "WritePrimitive", 4, memberValue, "memberValue");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("2cd3f3c7-f524-4b72-b2da-979f5d1ca7c4", "ReflectionXmlSerializationWriter.cs", 964, "WritePrimitive", 5, attribute.Mapping!, "attribute.Mapping!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("a4891d9f-6150-4737-bc31-a40c140551bc", "ReflectionXmlSerializationWriter.cs", 964, "WritePrimitive", 6, false, "false");
                WritePrimitive(WritePrimitiveMethodRequirement.WriteAttribute, attribute.Name, ns, attribute.Default, memberValue, attribute.Mapping!, false);
}            }
        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("ec590469-c96a-4d17-99d3-b8a38966467e", "ReflectionXmlSerializationWriter.cs", 947, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("75e9b4ad-e31c-42be-b1a6-5dcd6136e4fc", "ReflectionXmlSerializationWriter.cs", 987, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("79cab516-7ad3-49ff-bfa5-1f16a245fb22", "ReflectionXmlSerializationWriter.cs", 947, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("e58799d6-0de2-4e19-ac9e-0b545d5005db", "ReflectionXmlSerializationWriter.cs", 947, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("f9bd285c-197c-4fa8-9450-ec9e13d54347", "ReflectionXmlSerializationWriter.cs", 947, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private static int FindXmlnsIndex(MemberMapping[] members)
{global::AutoRepairTracing.TraceLogger.Log("ec34c7fe-6be7-4e33-abed-d11927464a80", "ReflectionXmlSerializationWriter.cs", 968, "Enter Method 'FindXmlnsIndex'");
global::AutoRepairTracing.TraceLogger.LogArray("ac7ef33f-7aa3-452e-b22e-a2f1032c6b9f", "ReflectionXmlSerializationWriter.cs", 968, "Array Parameter", members, "FindXmlnsIndex#arg0:members");
global::AutoRepairTracing.TraceLogger.LogArrayAliasParameter("aa3e9985-390f-4e57-baaa-036780bbe952", "ReflectionXmlSerializationWriter.cs", 968, "FindXmlnsIndex", 0, members, "members");
try        {
{int __loopCounter_dee93794 = 0;global::AutoRepairTracing.TraceLogger.Log("c5353e67-74f1-4d8c-a710-b51ac7fcf3e3", "ReflectionXmlSerializationWriter.cs", 1544, "Variable Update", __loopCounter_dee93794, "__loopCounter_dee93794");
            for (int i = 0; i < members.Length; global::AutoRepairTracing.TraceLogger.LogValue("ab3de7de-6bf2-48b8-8786-8513b3c7df7e", "ReflectionXmlSerializationWriter.cs", 1544, "Variable Update", i++, "i"))
            {
global::AutoRepairTracing.TraceLogger.Log("3407d0d5-bcb1-42bd-8a12-4e0cc33caec3", "ReflectionXmlSerializationWriter.cs", 970, "For Loop");
global::AutoRepairTracing.TraceLogger.LogValue("410d0eff-6e78-478b-8ebb-6bdca375de07", "ReflectionXmlSerializationWriter.cs", 1547, "Variable Update", __loopCounter_dee93794++, "__loopCounter_dee93794");if(__loopCounter_dee93794 > 10000){global::AutoRepairTracing.TraceLogger.Log("b508a395-e25f-4875-9447-c87ab5d2bc42", "ReflectionXmlSerializationWriter.cs", 993, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("dee93794-d519-44eb-b632-bfc038da2080", "ReflectionXmlSerializationWriter.cs", 970, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_dee93794, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("4ba19acd-4323-4187-9993-a235bf49fd13", "ReflectionXmlSerializationWriter.cs", 993, "Break");
break;}}                if (members[i].Xmlns == null)
{global::AutoRepairTracing.TraceLogger.Log("53e7c50e-2351-4041-a626-636454f57580", "ReflectionXmlSerializationWriter.cs", 973, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("25b96f59-dec0-46c0-96c0-1b5bdc4e4b09", "ReflectionXmlSerializationWriter.cs", 973, "Continue");
                    continue;
}{global::AutoRepairTracing.TraceLogger.Log("79efb1d6-7de7-44b3-8bd3-09c3b5f414ce", "ReflectionXmlSerializationWriter.cs", 975, "Return");

                return i;
}            }
}{global::AutoRepairTracing.TraceLogger.Log("bae6f910-cc1a-41ef-8aa4-a039ce34105d", "ReflectionXmlSerializationWriter.cs", 978, "Return");

            return -1;
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("9cb3549e-a2fe-4606-a017-43059c561f5c", "ReflectionXmlSerializationWriter.cs", 968, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("e0c4b3e2-22f3-459e-80ac-f3fa454d4e47", "ReflectionXmlSerializationWriter.cs", 1001, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("52fda2c5-2ad9-402f-abc4-bf3f467c1a1b", "ReflectionXmlSerializationWriter.cs", 968, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("61062046-ea2b-468f-be06-88579e46b169", "ReflectionXmlSerializationWriter.cs", 968, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("5be3fd75-b4b6-41e4-9c6e-d6f587879ee7", "ReflectionXmlSerializationWriter.cs", 968, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private bool WriteDerivedTypes(StructMapping mapping, string n, string? ns, object o, bool isNullable)
{global::AutoRepairTracing.TraceLogger.Log("55fd1d98-6048-492f-b58d-b57c169cce43", "ReflectionXmlSerializationWriter.cs", 981, "Enter Method 'WriteDerivedTypes'");
try        {
            Type t = o.GetType();
global::AutoRepairTracing.TraceLogger.Log("b69b74c0-863c-4054-987c-1feeb6220c2f", "ReflectionXmlSerializationWriter.cs", 983, "Variable Update", t, "t");
{int __loopCounter_b4ea5ad4 = 0;global::AutoRepairTracing.TraceLogger.Log("20ac2bd6-57ad-483f-886e-7c21c5a7af61", "ReflectionXmlSerializationWriter.cs", 1568, "Variable Update", __loopCounter_b4ea5ad4, "__loopCounter_b4ea5ad4");
            for (StructMapping? derived = mapping.DerivedMappings; derived != null; global::AutoRepairTracing.TraceLogger.LogValue("b96bcae3-84c3-4077-b022-d37e3bee4688", "ReflectionXmlSerializationWriter.cs", 1568, "Variable Update", derived = derived.NextDerivedMapping, "derived"))
            {
global::AutoRepairTracing.TraceLogger.Log("92bcd71c-afbe-4bf1-b9e5-49ccb4158930", "ReflectionXmlSerializationWriter.cs", 984, "For Loop");
global::AutoRepairTracing.TraceLogger.LogValue("410f78be-ef65-450c-bffc-b2cbac7e5771", "ReflectionXmlSerializationWriter.cs", 1571, "Variable Update", __loopCounter_b4ea5ad4++, "__loopCounter_b4ea5ad4");if(__loopCounter_b4ea5ad4 > 10000){global::AutoRepairTracing.TraceLogger.Log("c7dcd272-cfdd-4476-868b-449e99727594", "ReflectionXmlSerializationWriter.cs", 1008, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("b4ea5ad4-8d2d-4bb2-876a-cf6613123513", "ReflectionXmlSerializationWriter.cs", 984, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_b4ea5ad4, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("e1026dc1-ac78-4420-b31e-ea72a4e903e1", "ReflectionXmlSerializationWriter.cs", 1008, "Break");
break;}}                if (t == derived.TypeDesc!.Type)
                {
global::AutoRepairTracing.TraceLogger.Log("22e79a34-cc9f-45ab-b9f3-3c99162bcf36", "ReflectionXmlSerializationWriter.cs", 987, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("802b33ca-f18b-4124-a66d-a66821d21dcf", "ReflectionXmlSerializationWriter.cs", 988, "WriteStructMethod", 0, derived, "derived");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d894b851-59c4-4e4f-b03c-451471d3af7d", "ReflectionXmlSerializationWriter.cs", 988, "WriteStructMethod", 1, n, "n");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("136792c1-9a44-41d9-9543-85c1b7f93b37", "ReflectionXmlSerializationWriter.cs", 988, "WriteStructMethod", 2, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("c1105b5b-dc6c-4ddd-929e-8ed1450eeb24", "ReflectionXmlSerializationWriter.cs", 988, "WriteStructMethod", 3, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("e5624896-7835-4486-a7b2-27f05a491f55", "ReflectionXmlSerializationWriter.cs", 988, "WriteStructMethod", 4, isNullable, "isNullable");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("c8e301eb-e2e4-46be-b79e-c982c85015ba", "ReflectionXmlSerializationWriter.cs", 988, "WriteStructMethod", 5, true, "true");
                    WriteStructMethod(derived, n, ns, o, isNullable, needType: true);
}{global::AutoRepairTracing.TraceLogger.Log("986e419d-9f32-45cd-8403-5d12c0a0f5ee", "ReflectionXmlSerializationWriter.cs", 989, "Return");
                    return true;
}                }

                if (WriteDerivedTypes(derived, n, ns, o, isNullable))
                {
global::AutoRepairTracing.TraceLogger.Log("ff4001ce-99ad-4871-b6d1-878e7d0ed058", "ReflectionXmlSerializationWriter.cs", 993, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("9690cc34-29ca-48f8-b3c6-6820359a8b5e", "ReflectionXmlSerializationWriter.cs", 994, "Return");
                    return true;
}                }
            }
}{global::AutoRepairTracing.TraceLogger.Log("8d5a188f-9cb1-48c7-a277-9607af9ee176", "ReflectionXmlSerializationWriter.cs", 998, "Return");

            return false;
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("badb6220-ab28-4ca1-b83e-0764640c42df", "ReflectionXmlSerializationWriter.cs", 981, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("bd67d9f0-86e4-449e-bfc5-4f9744f5f4b5", "ReflectionXmlSerializationWriter.cs", 1022, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("d7fc9f3c-032e-45a0-9027-2b546835258b", "ReflectionXmlSerializationWriter.cs", 981, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("26bdcc35-1262-4ac9-bbec-f12692dd6e46", "ReflectionXmlSerializationWriter.cs", 981, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("64f3b95a-5918-4a80-afe0-e67c1eb24a25", "ReflectionXmlSerializationWriter.cs", 981, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private void WritePrimitive(WritePrimitiveMethodRequirement method, string name, string? ns, object? defaultValue, object o, TypeMapping mapping, bool writeXsiType)
{global::AutoRepairTracing.TraceLogger.Log("40abf704-a6fc-452f-9403-bed75938ca54", "ReflectionXmlSerializationWriter.cs", 1001, "Enter Method 'WritePrimitive'");
try        {
            TypeDesc typeDesc = mapping.TypeDesc!;
global::AutoRepairTracing.TraceLogger.Log("986a99aa-f73b-4992-aa8d-779f934f3428", "ReflectionXmlSerializationWriter.cs", 1003, "Variable Update", typeDesc, "typeDesc");
            bool hasDefault = defaultValue != null && defaultValue != DBNull.Value && mapping.TypeDesc!.HasDefaultSupport;
global::AutoRepairTracing.TraceLogger.Log("bf8f3c98-2c0d-4374-885f-6e2c0f6a47ea", "ReflectionXmlSerializationWriter.cs", 1004, "Variable Update", hasDefault, "hasDefault");
            if (hasDefault)
            {
global::AutoRepairTracing.TraceLogger.Log("2a47db69-e207-40e0-9d0e-d86395c05839", "ReflectionXmlSerializationWriter.cs", 1006, "If-True Branch");
                if (mapping is EnumMapping)
                {
global::AutoRepairTracing.TraceLogger.Log("6afea0d3-43a1-4265-88b6-f3ce9e346f16", "ReflectionXmlSerializationWriter.cs", 1008, "If-True Branch");
                    if (((EnumMapping)global::AutoRepairTracing.TraceLogger.LogCastSource("2a539f83-a0c6-4538-a159-93b57418493c", "ReflectionXmlSerializationWriter.cs", 1713, (object)(mapping), "EnumMapping")).IsFlags)
                    {
global::AutoRepairTracing.TraceLogger.Log("1614add8-3c90-4fbd-a0cb-1463dac342a5", "ReflectionXmlSerializationWriter.cs", 1010, "If-True Branch");
                        IEnumerable<string> defaultEnumFlagValues = defaultValue!.ToString()!.Split((char[]?)global::AutoRepairTracing.TraceLogger.LogCastSource("e1b44047-d7c0-48f3-b930-26429527d81a", "ReflectionXmlSerializationWriter.cs", 1716, (object)(null), "char[]?"), StringSplitOptions.RemoveEmptyEntries);
global::AutoRepairTracing.TraceLogger.Log("281c57d8-90ca-4697-b1f7-2ec34b22cc7a", "ReflectionXmlSerializationWriter.cs", 1011, "Heap State: defaultEnumFlagValues (IEnumerable<string>)");global::AutoRepairTracing.TraceLogger.Log("19100a3a-9f84-4ce5-892f-b7617aa9c9d4", "ReflectionXmlSerializationWriter.cs", 1011, "Variable Update", defaultEnumFlagValues, "defaultEnumFlagValues");
                        string defaultEnumFlagString = string.Join(", ", defaultEnumFlagValues);
global::AutoRepairTracing.TraceLogger.Log("de06359c-469e-46cf-823d-16ff6eee48bb", "ReflectionXmlSerializationWriter.cs", 1012, "Variable Update", defaultEnumFlagString, "defaultEnumFlagString");

                        if (o.ToString() == defaultEnumFlagString)
{global::AutoRepairTracing.TraceLogger.Log("7dc548f0-59a7-41c2-9910-aa1a838eaab5", "ReflectionXmlSerializationWriter.cs", 1015, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("fdbee89e-d07f-47e6-831d-0c3783b845cb", "ReflectionXmlSerializationWriter.cs", 1015, "Return");
                            return;
}                    }
                    else
                    {
global::AutoRepairTracing.TraceLogger.Log("8f8690ca-b15d-40cd-a211-4e823784381e", "ReflectionXmlSerializationWriter.cs", 1018, "Else Branch");
                        if (o.ToString() == defaultValue!.ToString())
{global::AutoRepairTracing.TraceLogger.Log("4f6e3897-ab52-4fe6-92e9-b438060c6c54", "ReflectionXmlSerializationWriter.cs", 1020, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("a2141a57-da96-4f32-9eff-5be2e9427de3", "ReflectionXmlSerializationWriter.cs", 1020, "Return");
                            return;
}                    }
                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("8fc2586f-f434-4060-8109-1d6bbb056cf1", "ReflectionXmlSerializationWriter.cs", 1024, "Else Branch");
                    if (IsDefaultValue(o, defaultValue!))
                    {
global::AutoRepairTracing.TraceLogger.Log("f7c896ab-cdc5-4f20-b4b1-670bcea35865", "ReflectionXmlSerializationWriter.cs", 1026, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("ebe4f5e4-6b6c-4035-8770-95f105831082", "ReflectionXmlSerializationWriter.cs", 1027, "Return");
                        return;
}                    }
                }
            }

            XmlQualifiedName? xmlQualifiedName = null;
global::AutoRepairTracing.TraceLogger.Log("ed27688a-3be4-448a-ae51-cd9dfa91ddd1", "ReflectionXmlSerializationWriter.cs", 1032, "Variable Update", xmlQualifiedName, "xmlQualifiedName");
global::AutoRepairTracing.TraceLogger.Log("e6794da0-2f1a-4f91-ab67-b1dfe236bf00", "ReflectionXmlSerializationWriter.cs", 1032, "Null Assignment Warning", "null", "xmlQualifiedName");            if (writeXsiType)
            {
global::AutoRepairTracing.TraceLogger.Log("dd1c2be8-be61-4aed-8679-07b38824a655", "ReflectionXmlSerializationWriter.cs", 1034, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("465fa578-06d0-4e47-b25a-5d35de0f806e", "ReflectionXmlSerializationWriter.cs", 1648, "Variable Update", xmlQualifiedName = new XmlQualifiedName(mapping.TypeName, mapping.Namespace), "xmlQualifiedName");
            }

            string? stringValue;
            bool hasValidStringValue;
            if (mapping is EnumMapping enumMapping)
            {
global::AutoRepairTracing.TraceLogger.Log("21fa71ff-da38-417b-bfbe-27517d36dcdb", "ReflectionXmlSerializationWriter.cs", 1041, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("4892e529-30f8-49ad-9df9-ad8dc56aab1f", "ReflectionXmlSerializationWriter.cs", 1656, "Variable Update", stringValue = WriteEnumMethod(enumMapping, o), "stringValue");
global::AutoRepairTracing.TraceLogger.LogValue("ee73d34a-4401-4e21-9441-bbef14d1f52c", "ReflectionXmlSerializationWriter.cs", 1657, "Variable Update", hasValidStringValue = true, "hasValidStringValue");
            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("2efb853e-ac98-4e7c-8ef0-41de0513752e", "ReflectionXmlSerializationWriter.cs", 1046, "Else Branch");
global::AutoRepairTracing.TraceLogger.LogValue("1e78ce74-009b-4dc9-9831-c26b6019f8a2", "ReflectionXmlSerializationWriter.cs", 1662, "Variable Update", hasValidStringValue = WritePrimitiveValue(typeDesc, o, out stringValue), "hasValidStringValue");
            }

            if (hasValidStringValue)
            {
global::AutoRepairTracing.TraceLogger.Log("0666ac6a-fdbc-4696-a0df-a83df9a8565d", "ReflectionXmlSerializationWriter.cs", 1051, "If-True Branch");
                if (hasRequirement(method, WritePrimitiveMethodRequirement.WriteElementString))
                {
global::AutoRepairTracing.TraceLogger.Log("78125524-5dfe-448c-bd8e-5b92ca933d62", "ReflectionXmlSerializationWriter.cs", 1053, "If-True Branch");
                    if (hasRequirement(method, WritePrimitiveMethodRequirement.Raw))
                    {
global::AutoRepairTracing.TraceLogger.Log("4067168c-321f-48bf-b966-5c1bc2471dfb", "ReflectionXmlSerializationWriter.cs", 1055, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("dae27837-5142-41d7-88b3-ee406d040df8", "ReflectionXmlSerializationWriter.cs", 1056, "WriteElementStringRaw", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("ac2bf0c6-283b-42c6-86c2-e4587a76b119", "ReflectionXmlSerializationWriter.cs", 1056, "WriteElementStringRaw", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("98dcb754-470a-4d29-9ebb-f20158c17c36", "ReflectionXmlSerializationWriter.cs", 1056, "WriteElementStringRaw", 2, stringValue, "stringValue");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("ace49b97-315e-40e0-b41f-8dbf047057f8", "ReflectionXmlSerializationWriter.cs", 1056, "WriteElementStringRaw", 3, xmlQualifiedName, "xmlQualifiedName");
                        WriteElementStringRaw(name, ns, stringValue, xmlQualifiedName);
}                    }
                    else
                    {
global::AutoRepairTracing.TraceLogger.Log("edbafb93-a61e-484a-8966-4a5650727b19", "ReflectionXmlSerializationWriter.cs", 1059, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("599a21bc-7b93-4bec-a582-6e2542a2cae6", "ReflectionXmlSerializationWriter.cs", 1060, "WriteElementString", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("c50788d7-f610-4094-8263-f33dd3c13376", "ReflectionXmlSerializationWriter.cs", 1060, "WriteElementString", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("8fad32e0-6a76-48b3-ae2b-8970344dc731", "ReflectionXmlSerializationWriter.cs", 1060, "WriteElementString", 2, stringValue, "stringValue");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("412f068f-2c60-4fc7-8d7a-4fba18a3777f", "ReflectionXmlSerializationWriter.cs", 1060, "WriteElementString", 3, xmlQualifiedName, "xmlQualifiedName");
                        WriteElementString(name, ns, stringValue, xmlQualifiedName);
}                    }
                }

                else {global::AutoRepairTracing.TraceLogger.Log("6d33724b-7a99-4cc1-9ba2-80884e6e215c", "ReflectionXmlSerializationWriter.cs", 1064, "Else Branch");
if (hasRequirement(method, WritePrimitiveMethodRequirement.WriteNullableStringLiteral))
                {
global::AutoRepairTracing.TraceLogger.Log("2f510048-a6b5-4c47-a391-3624189601ba", "ReflectionXmlSerializationWriter.cs", 1065, "If-True Branch");
                    if (hasRequirement(method, WritePrimitiveMethodRequirement.Encoded))
                    {
global::AutoRepairTracing.TraceLogger.Log("7f7a638c-0845-44d1-9723-52781da6f937", "ReflectionXmlSerializationWriter.cs", 1067, "If-True Branch");
                        if (hasRequirement(method, WritePrimitiveMethodRequirement.Raw))
                        {
global::AutoRepairTracing.TraceLogger.Log("aaaedc24-415f-4f9f-8250-c01051027648", "ReflectionXmlSerializationWriter.cs", 1069, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("cc78df9b-de0e-4ff7-b471-2c05dd7323b4", "ReflectionXmlSerializationWriter.cs", 1070, "WriteNullableStringEncodedRaw", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("9bda0866-7b72-49c4-b6ec-40c9f71da52d", "ReflectionXmlSerializationWriter.cs", 1070, "WriteNullableStringEncodedRaw", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("cc6d9e8c-9162-400e-a020-d4e85b5b0746", "ReflectionXmlSerializationWriter.cs", 1070, "WriteNullableStringEncodedRaw", 2, stringValue, "stringValue");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("c55a5c87-29ba-4836-9135-c14dbbfb1d19", "ReflectionXmlSerializationWriter.cs", 1070, "WriteNullableStringEncodedRaw", 3, xmlQualifiedName, "xmlQualifiedName");
                            WriteNullableStringEncodedRaw(name, ns, stringValue, xmlQualifiedName);
}                        }
                        else
                        {
global::AutoRepairTracing.TraceLogger.Log("869e7417-0d83-4691-aca9-ad928969a228", "ReflectionXmlSerializationWriter.cs", 1073, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("799e9d30-bcc0-4ee9-8c47-218ab2944c49", "ReflectionXmlSerializationWriter.cs", 1074, "WriteNullableStringEncoded", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("5cb0cda5-8688-4cb7-9f29-d8563891f85e", "ReflectionXmlSerializationWriter.cs", 1074, "WriteNullableStringEncoded", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("c32178e7-2d4e-4ee7-8983-81252f8de6a1", "ReflectionXmlSerializationWriter.cs", 1074, "WriteNullableStringEncoded", 2, stringValue, "stringValue");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("7744ffa3-b03d-4471-aedc-61dc6b675e8f", "ReflectionXmlSerializationWriter.cs", 1074, "WriteNullableStringEncoded", 3, xmlQualifiedName, "xmlQualifiedName");
                            WriteNullableStringEncoded(name, ns, stringValue, xmlQualifiedName);
}                        }
                    }
                    else
                    {
global::AutoRepairTracing.TraceLogger.Log("10ef5161-03fb-43a0-a4cd-3dbd79523f39", "ReflectionXmlSerializationWriter.cs", 1078, "Else Branch");
                        if (hasRequirement(method, WritePrimitiveMethodRequirement.Raw))
                        {
global::AutoRepairTracing.TraceLogger.Log("af1adebd-1f9b-43c2-9a6d-76aadd9bb3e0", "ReflectionXmlSerializationWriter.cs", 1080, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("25c0034b-6870-475c-8e80-1fa483d5f81c", "ReflectionXmlSerializationWriter.cs", 1081, "WriteNullableStringLiteralRaw", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("fb887bd9-db0a-4956-8704-98c2cfb9c0f3", "ReflectionXmlSerializationWriter.cs", 1081, "WriteNullableStringLiteralRaw", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("7926e73e-4d37-4ec5-96fd-14b334ff1ff6", "ReflectionXmlSerializationWriter.cs", 1081, "WriteNullableStringLiteralRaw", 2, stringValue, "stringValue");
                            WriteNullableStringLiteralRaw(name, ns, stringValue);
}                        }
                        else
                        {
global::AutoRepairTracing.TraceLogger.Log("ce746084-0de5-446d-bf8e-4ddd82eef8df", "ReflectionXmlSerializationWriter.cs", 1084, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("f956c38b-7bf3-4b9b-821f-2c7bf49d501d", "ReflectionXmlSerializationWriter.cs", 1085, "WriteNullableStringLiteral", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("4df934d7-fa30-4278-99ff-3893b9f41ac9", "ReflectionXmlSerializationWriter.cs", 1085, "WriteNullableStringLiteral", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("5cf8a0ea-f9b3-4aac-9ba2-06a78ef4a353", "ReflectionXmlSerializationWriter.cs", 1085, "WriteNullableStringLiteral", 2, stringValue, "stringValue");
                            WriteNullableStringLiteral(name, ns, stringValue);
}                        }
                    }
                }
                else {global::AutoRepairTracing.TraceLogger.Log("8a85b283-e8f6-4437-b7e0-ff514b72cd05", "ReflectionXmlSerializationWriter.cs", 1089, "Else Branch");
if (hasRequirement(method, WritePrimitiveMethodRequirement.WriteAttribute))
                {
global::AutoRepairTracing.TraceLogger.Log("cbbfd9a8-a41d-4729-b71b-48d37d8999cc", "ReflectionXmlSerializationWriter.cs", 1090, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("27397d44-7654-4cf0-8088-2b5870acd33d", "ReflectionXmlSerializationWriter.cs", 1091, "WriteAttribute", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("c9fb56aa-f102-4333-bb98-94cc4a9fcae4", "ReflectionXmlSerializationWriter.cs", 1091, "WriteAttribute", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("2ae23d65-fca7-4cae-aabe-9b09c9f70a37", "ReflectionXmlSerializationWriter.cs", 1091, "WriteAttribute", 2, stringValue, "stringValue");
                    WriteAttribute(name, ns, stringValue);
}                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("d6a3ff33-4717-4922-a873-a36083e04065", "ReflectionXmlSerializationWriter.cs", 1094, "Else Branch");
                    Debug.Fail("https://github.com/dotnet/runtime/issues/18037: Add More Tests for Serialization Code");
                }
}}            }
            else {global::AutoRepairTracing.TraceLogger.Log("1f6b46b6-8a6c-4dde-ad20-cf7ca57d0e2c", "ReflectionXmlSerializationWriter.cs", 1098, "Else Branch");
if (o is byte[] a)
            {
global::AutoRepairTracing.TraceLogger.Log("5970dc4d-2936-44e4-ab40-3dc080b34600", "ReflectionXmlSerializationWriter.cs", 1099, "If-True Branch");
                if (hasRequirement(method, WritePrimitiveMethodRequirement.WriteElementString | WritePrimitiveMethodRequirement.Raw))
                {
global::AutoRepairTracing.TraceLogger.Log("24558ca2-cbf3-41d8-8e71-3990ca07b126", "ReflectionXmlSerializationWriter.cs", 1101, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("b6aae8af-7259-4843-8f13-289b6c78476a", "ReflectionXmlSerializationWriter.cs", 1102, "WriteElementStringRaw", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d030dd5b-4cbd-4e1f-ab94-f83426a35961", "ReflectionXmlSerializationWriter.cs", 1102, "WriteElementStringRaw", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("70dadcf2-5d89-48c2-93b4-150d8e94812c", "ReflectionXmlSerializationWriter.cs", 1102, "WriteElementStringRaw", 2, FromByteArrayBase64(a), "FromByteArrayBase64(a)");
                    WriteElementStringRaw(name, ns, FromByteArrayBase64(a));
}                }
                else {global::AutoRepairTracing.TraceLogger.Log("6bd0f6aa-6f8c-4aa1-881b-ac5a4a04e2cc", "ReflectionXmlSerializationWriter.cs", 1104, "Else Branch");
if (hasRequirement(method, WritePrimitiveMethodRequirement.WriteNullableStringLiteral | WritePrimitiveMethodRequirement.Raw))
                {
global::AutoRepairTracing.TraceLogger.Log("d4d9f10e-9974-426d-b89e-e060755b0b45", "ReflectionXmlSerializationWriter.cs", 1105, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("eef86a01-8e55-4485-a181-fe5f86697627", "ReflectionXmlSerializationWriter.cs", 1106, "WriteNullableStringLiteralRaw", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("7c5ab9cc-dc0b-443e-88eb-9bc83f424f55", "ReflectionXmlSerializationWriter.cs", 1106, "WriteNullableStringLiteralRaw", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("2eb8a056-e965-429b-a38a-9422102cbcd4", "ReflectionXmlSerializationWriter.cs", 1106, "WriteNullableStringLiteralRaw", 2, FromByteArrayBase64(a), "FromByteArrayBase64(a)");
                    WriteNullableStringLiteralRaw(name, ns, FromByteArrayBase64(a));
}                }
                else {global::AutoRepairTracing.TraceLogger.Log("925ec1f8-7b66-497f-80a0-057d244cdebe", "ReflectionXmlSerializationWriter.cs", 1108, "Else Branch");
if (hasRequirement(method, WritePrimitiveMethodRequirement.WriteAttribute))
                {
global::AutoRepairTracing.TraceLogger.Log("c2690f75-e16a-47d3-89a5-89573a5be652", "ReflectionXmlSerializationWriter.cs", 1109, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("fc3eb552-8167-41a4-b60d-4c80edbaf88e", "ReflectionXmlSerializationWriter.cs", 1110, "WriteAttribute", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("5eca066e-4d7a-4d9e-b220-dc2e893e55c6", "ReflectionXmlSerializationWriter.cs", 1110, "WriteAttribute", 1, ns!, "ns!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("3ffa6ceb-68e3-4148-ae2d-6df737134e7d", "ReflectionXmlSerializationWriter.cs", 1110, "WriteAttribute", 2, a, "a");
                    WriteAttribute(name, ns!, a);
}                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("912d8ea1-4d63-4159-bf41-d73d92229ae6", "ReflectionXmlSerializationWriter.cs", 1113, "Else Branch");
                    Debug.Fail("https://github.com/dotnet/runtime/issues/18037: Add More Tests for Serialization Code");
                }
}}            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("8393fa13-3d7d-4828-b7eb-b53d8c4bc97e", "ReflectionXmlSerializationWriter.cs", 1118, "Else Branch");
                Debug.Fail("https://github.com/dotnet/runtime/issues/18037: Add More Tests for Serialization Code");
            }
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("4eaeb9a2-bdc5-4ab3-9358-0770a49392ae", "ReflectionXmlSerializationWriter.cs", 1001, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("8a700bd2-eef9-4f2c-a787-680b387292d8", "ReflectionXmlSerializationWriter.cs", 1145, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("4187688c-f486-45dd-b38f-57b55fd01186", "ReflectionXmlSerializationWriter.cs", 1001, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("69a8d690-afb3-4de0-8912-934c155828c1", "ReflectionXmlSerializationWriter.cs", 1001, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("d6cf0ea0-408c-44ae-a110-6ddec638c9ef", "ReflectionXmlSerializationWriter.cs", 1001, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private static bool hasRequirement(WritePrimitiveMethodRequirement value, WritePrimitiveMethodRequirement requirement)
{global::AutoRepairTracing.TraceLogger.Log("2dad5503-2b92-4acb-8ccd-3f9bd4a916ac", "ReflectionXmlSerializationWriter.cs", 1123, "Enter Method 'hasRequirement'");
try        {
{global::AutoRepairTracing.TraceLogger.Log("aef30682-cc65-4507-8060-543b7e28cf82", "ReflectionXmlSerializationWriter.cs", 1125, "Return");
            return (value & requirement) == requirement;
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("3bf80a1a-0a3e-4a57-b290-86e1198a2b6f", "ReflectionXmlSerializationWriter.cs", 1123, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("54ad0ce8-4588-4e9e-b067-220698ce4e05", "ReflectionXmlSerializationWriter.cs", 1151, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("801dc4de-64f7-40cf-adfa-761cabd90490", "ReflectionXmlSerializationWriter.cs", 1123, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("5a64bfde-62b9-4d4d-bc5b-c860a69d8e17", "ReflectionXmlSerializationWriter.cs", 1123, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("f0b267df-243e-4702-9dd3-686059c2edc8", "ReflectionXmlSerializationWriter.cs", 1123, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private static bool IsDefaultValue(object o, object value)
{global::AutoRepairTracing.TraceLogger.Log("ffc5c65b-ddb5-4165-acbf-04d7b7581354", "ReflectionXmlSerializationWriter.cs", 1128, "Enter Method 'IsDefaultValue'");
try        {
            if (value is string && ((string)global::AutoRepairTracing.TraceLogger.LogCastSource("b67d3cce-ad44-4631-aad2-6a8bc889a67d", "ReflectionXmlSerializationWriter.cs", 1914, (object)(value), "string")).Length == 0)
            {
global::AutoRepairTracing.TraceLogger.Log("84fd4535-0019-4110-84a8-d51eb2df267f", "ReflectionXmlSerializationWriter.cs", 1131, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("2ef1cb14-959f-41fa-8c09-83f0c3b4b37f", "ReflectionXmlSerializationWriter.cs", 1132, "Return");
                return string.IsNullOrEmpty((string)global::AutoRepairTracing.TraceLogger.LogCastSource("ab7edab9-eced-4699-a72d-66e64a93bf1c", "ReflectionXmlSerializationWriter.cs", 1918, (object)(o), "string"));
}            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("0fc36ddb-0a09-4a4a-ab61-2bb9cb63f550", "ReflectionXmlSerializationWriter.cs", 1135, "Else Branch");
{global::AutoRepairTracing.TraceLogger.Log("61eee802-20c9-4121-a4c3-0933cacc342a", "ReflectionXmlSerializationWriter.cs", 1136, "Return");
                return value.Equals(o);
}            }
        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("0d87f784-a6f2-418b-ba08-a8da33527458", "ReflectionXmlSerializationWriter.cs", 1128, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("9584d608-05d7-4a88-9698-fe04525364bb", "ReflectionXmlSerializationWriter.cs", 1164, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("8f8abc8a-c1ca-4a4e-9ada-5f2e6da7aeec", "ReflectionXmlSerializationWriter.cs", 1128, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("385c650f-46b1-45a1-ac3c-df7a4f5e744e", "ReflectionXmlSerializationWriter.cs", 1128, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("652ffa4b-8618-49af-83bd-83e8893e1ce3", "ReflectionXmlSerializationWriter.cs", 1128, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private bool WritePrimitiveValue(TypeDesc typeDesc, object? o, out string? stringValue)
{global::AutoRepairTracing.TraceLogger.Log("e6e6ad6f-894d-4f6c-8ade-4a1cefa56d14", "ReflectionXmlSerializationWriter.cs", 1140, "Enter Method 'WritePrimitiveValue'");
try        {
            if (typeDesc == ReflectionXmlSerializationReader.StringTypeDesc || typeDesc.FormatterName == "String")
            {
global::AutoRepairTracing.TraceLogger.Log("faf70cac-086a-4c22-b9d6-a09d55123948", "ReflectionXmlSerializationWriter.cs", 1143, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("2ce65422-d98a-48fc-b783-60566ef259b1", "ReflectionXmlSerializationWriter.cs", 1832, "Variable Update", stringValue = (string?)global::AutoRepairTracing.TraceLogger.LogCastSource("57fc8da8-bfcb-4dc2-a800-9d0643848824", "ReflectionXmlSerializationWriter.cs", 1936, (object)(o), "string?"), "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("dad93980-68b3-4b74-9ae6-d0e6098a2583", "ReflectionXmlSerializationWriter.cs", 1145, "Return");
                return true;
}            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("86db47b4-c270-4717-8f9f-787f8325582e", "ReflectionXmlSerializationWriter.cs", 1148, "Else Branch");
                if (!typeDesc.HasCustomFormatter)
                {
global::AutoRepairTracing.TraceLogger.Log("2c99148a-982c-4569-9344-502731f617e7", "ReflectionXmlSerializationWriter.cs", 1150, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("7bfaa628-8fb9-49b0-91d8-7c145b308a19", "ReflectionXmlSerializationWriter.cs", 1842, "Variable Update", stringValue = ConvertPrimitiveToString(o!, typeDesc), "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("c6187890-2df6-4904-a308-a935e775547e", "ReflectionXmlSerializationWriter.cs", 1152, "Return");
                    return true;
}                }
                else {global::AutoRepairTracing.TraceLogger.Log("538498bf-a96f-4ff5-915d-43789e5dd318", "ReflectionXmlSerializationWriter.cs", 1154, "Else Branch");
if (o is byte[] && typeDesc.FormatterName == "ByteArrayHex")
                {
global::AutoRepairTracing.TraceLogger.Log("366dbe15-e41f-41be-82c6-01a5cd61f0ca", "ReflectionXmlSerializationWriter.cs", 1155, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("8c6d0236-ed27-43bd-8183-42a0d46dece7", "ReflectionXmlSerializationWriter.cs", 1850, "Variable Update", stringValue = FromByteArrayHex((byte[])global::AutoRepairTracing.TraceLogger.LogCastSource("690b0250-f4d9-4707-b8cc-784803bb8673", "ReflectionXmlSerializationWriter.cs", 1954, (object)(o), "byte[]")), "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("8fd10826-3616-426a-afd5-30ea76d232e1", "ReflectionXmlSerializationWriter.cs", 1157, "Return");
                    return true;
}                }
                else {global::AutoRepairTracing.TraceLogger.Log("a6df8419-d572-4a06-b0bf-44f3e947d887", "ReflectionXmlSerializationWriter.cs", 1159, "Else Branch");
if (o is DateTime)
                {
global::AutoRepairTracing.TraceLogger.Log("5c7d499b-e811-4022-83d7-c58a263ca461", "ReflectionXmlSerializationWriter.cs", 1160, "If-True Branch");
                    if (typeDesc.FormatterName == "DateTime")
                    {
global::AutoRepairTracing.TraceLogger.Log("acc15a77-0240-49ee-bf7d-204604c4ed42", "ReflectionXmlSerializationWriter.cs", 1162, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("8c45074f-f543-4067-85b8-8f2b6dd69785", "ReflectionXmlSerializationWriter.cs", 1861, "Variable Update", stringValue = FromDateTime((DateTime)global::AutoRepairTracing.TraceLogger.LogCastSource("99d4fefa-87c9-41c9-9f01-4c8a8f6fdfb0", "ReflectionXmlSerializationWriter.cs", 1965, (object)(o), "DateTime")), "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("22b49b09-37a6-4fd5-a677-b4756f3f7a41", "ReflectionXmlSerializationWriter.cs", 1164, "Return");
                        return true;
}                    }
                    else {global::AutoRepairTracing.TraceLogger.Log("b12af78d-e5ab-47c5-816b-3fd40f5fca18", "ReflectionXmlSerializationWriter.cs", 1166, "Else Branch");
if (typeDesc.FormatterName == "Date")
                    {
global::AutoRepairTracing.TraceLogger.Log("60ab3f37-c1b7-423c-acb0-0fb8d0e44789", "ReflectionXmlSerializationWriter.cs", 1167, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("bcaa5f09-abee-4587-a0ba-bf3ad15b00ec", "ReflectionXmlSerializationWriter.cs", 1869, "Variable Update", stringValue = FromDate((DateTime)global::AutoRepairTracing.TraceLogger.LogCastSource("1928e006-d8f4-4ee4-9a8e-2b3cdae7a6a7", "ReflectionXmlSerializationWriter.cs", 1973, (object)(o), "DateTime")), "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("266d6a8d-88b7-4b66-b5cd-4a689e180762", "ReflectionXmlSerializationWriter.cs", 1169, "Return");
                        return true;
}                    }
                    else {global::AutoRepairTracing.TraceLogger.Log("d510b12d-be09-4859-b420-1a3bd8a46c05", "ReflectionXmlSerializationWriter.cs", 1171, "Else Branch");
if (typeDesc.FormatterName == "Time")
                    {
global::AutoRepairTracing.TraceLogger.Log("be1ad94f-1a49-4c36-9b96-bd35848906db", "ReflectionXmlSerializationWriter.cs", 1172, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("8e942faf-c0b7-4fbd-9628-d713040b151a", "ReflectionXmlSerializationWriter.cs", 1877, "Variable Update", stringValue = FromTime((DateTime)global::AutoRepairTracing.TraceLogger.LogCastSource("839b694e-fa2a-4051-9615-1f8342cb618c", "ReflectionXmlSerializationWriter.cs", 1981, (object)(o), "DateTime")), "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("3de64235-235f-418e-b1a1-6fa482a70e82", "ReflectionXmlSerializationWriter.cs", 1174, "Return");
                        return true;
}                    }
                    else
                    {
global::AutoRepairTracing.TraceLogger.Log("4c7e1a80-9345-4fbf-865a-bfb8817b8db8", "ReflectionXmlSerializationWriter.cs", 1177, "Else Branch");
                        throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorDetails, "Invalid DateTime"));
                    }
}}                }
                else {global::AutoRepairTracing.TraceLogger.Log("fb18db55-28ee-4a0b-854f-60a2e639df8f", "ReflectionXmlSerializationWriter.cs", 1181, "Else Branch");
if (o is DateOnly)
                {
global::AutoRepairTracing.TraceLogger.Log("3980e3c5-197e-4a65-be56-fc5ef6ae71bd", "ReflectionXmlSerializationWriter.cs", 1182, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("2af9294b-00c8-4632-aa21-28204ae3bfc9", "ReflectionXmlSerializationWriter.cs", 1891, "Variable Update", stringValue = FromDateOnly((DateOnly)global::AutoRepairTracing.TraceLogger.LogCastSource("324c8703-d972-4593-ba9f-70d2695722bb", "ReflectionXmlSerializationWriter.cs", 1995, (object)(o), "DateOnly")), "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("bc5df1c4-44b4-41dc-8f0a-a5acc2a9c34d", "ReflectionXmlSerializationWriter.cs", 1184, "Return");
                    return true;
}                }
                else {global::AutoRepairTracing.TraceLogger.Log("17ea6ec7-7783-4a93-a993-f5673cb77b5e", "ReflectionXmlSerializationWriter.cs", 1186, "Else Branch");
if (o is TimeOnly)
                {
global::AutoRepairTracing.TraceLogger.Log("be44525b-03e5-4f1b-917a-d6a38d6d29d7", "ReflectionXmlSerializationWriter.cs", 1187, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("76f93383-5d09-4a66-ad09-db996d6eb508", "ReflectionXmlSerializationWriter.cs", 1899, "Variable Update", stringValue = FromTimeOnly((TimeOnly)global::AutoRepairTracing.TraceLogger.LogCastSource("c0bc2c27-dbc3-4147-8c1b-69694e3d6f52", "ReflectionXmlSerializationWriter.cs", 2003, (object)(o), "TimeOnly")), "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("773a476d-1e69-43b0-a723-79a95a2b61c2", "ReflectionXmlSerializationWriter.cs", 1189, "Return");
                    return true;
}                }
                else {global::AutoRepairTracing.TraceLogger.Log("d082928c-f83b-4113-8da0-abc907062b95", "ReflectionXmlSerializationWriter.cs", 1191, "Else Branch");
if (typeDesc == ReflectionXmlSerializationReader.QnameTypeDesc)
                {
global::AutoRepairTracing.TraceLogger.Log("94c49bfc-9885-40b6-a820-b6cd1b8467dd", "ReflectionXmlSerializationWriter.cs", 1192, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("4bf0a35b-06c9-44fc-b6fb-0f948ae34846", "ReflectionXmlSerializationWriter.cs", 1907, "Variable Update", stringValue = FromXmlQualifiedName((XmlQualifiedName?)global::AutoRepairTracing.TraceLogger.LogCastSource("3e78de77-7a35-4f12-b648-56b92dad4c10", "ReflectionXmlSerializationWriter.cs", 2011, (object)(o), "XmlQualifiedName?")), "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("27c5af57-b3d5-46b6-90b2-e891b02b70fe", "ReflectionXmlSerializationWriter.cs", 1194, "Return");
                    return true;
}                }
                else {global::AutoRepairTracing.TraceLogger.Log("04f13d0d-fd4a-4a62-ace4-5427dc1b75af", "ReflectionXmlSerializationWriter.cs", 1196, "Else Branch");
if (o is string)
                {
global::AutoRepairTracing.TraceLogger.Log("c1cbb7d0-d291-46da-bb0d-88fdee531b9e", "ReflectionXmlSerializationWriter.cs", 1197, "If-True Branch");
                    switch (typeDesc.FormatterName)
                    {
                        case "XmlName":
global::AutoRepairTracing.TraceLogger.Log("2f4c0e4a-63bc-474b-8e6e-7560614e7631", "ReflectionXmlSerializationWriter.cs", 1226, "Switch Case [case "XmlName":]");
global::AutoRepairTracing.TraceLogger.LogValue("34c8487e-c10c-4abf-a4a9-3eb4171a5298", "ReflectionXmlSerializationWriter.cs", 1919, "Variable Update", stringValue = FromXmlName((string)global::AutoRepairTracing.TraceLogger.LogCastSource("2960b591-32c1-43fd-9709-1e6ad845c471", "ReflectionXmlSerializationWriter.cs", 2023, (object)(o), "string")), "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("69f6de5d-f1e4-4b63-9e94-d5be4efb14b2", "ReflectionXmlSerializationWriter.cs", 1202, "Break");
                            break;
}                        case "XmlNCName":
global::AutoRepairTracing.TraceLogger.Log("fe03071f-898b-4231-832a-b13b03f89769", "ReflectionXmlSerializationWriter.cs", 1229, "Switch Case [case "XmlNCName":]");
global::AutoRepairTracing.TraceLogger.LogValue("cb535b24-98ad-42ef-a44c-0a715e158fc5", "ReflectionXmlSerializationWriter.cs", 1924, "Variable Update", stringValue = FromXmlNCName((string)global::AutoRepairTracing.TraceLogger.LogCastSource("3dfe72c2-b070-4a76-aaa4-44cf4feec804", "ReflectionXmlSerializationWriter.cs", 2028, (object)(o), "string")), "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("d049e041-8271-4644-8c13-5b6aaf35b060", "ReflectionXmlSerializationWriter.cs", 1205, "Break");
                            break;
}                        case "XmlNmToken":
global::AutoRepairTracing.TraceLogger.Log("2ded2b60-ef30-411f-bfcf-379275dc8233", "ReflectionXmlSerializationWriter.cs", 1232, "Switch Case [case "XmlNmToken":]");
global::AutoRepairTracing.TraceLogger.LogValue("98341dcf-c9d7-4fdf-b34c-225680430162", "ReflectionXmlSerializationWriter.cs", 1929, "Variable Update", stringValue = FromXmlNmToken((string)global::AutoRepairTracing.TraceLogger.LogCastSource("cd23f45c-dd53-48a3-a506-41a223c56fac", "ReflectionXmlSerializationWriter.cs", 2033, (object)(o), "string")), "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("495d5fc5-01bd-42c1-b3f9-897254f08a3a", "ReflectionXmlSerializationWriter.cs", 1208, "Break");
                            break;
}                        case "XmlNmTokens":
global::AutoRepairTracing.TraceLogger.Log("b6936b7c-2f72-4474-b85d-0e8a62198631", "ReflectionXmlSerializationWriter.cs", 1235, "Switch Case [case "XmlNmTokens":]");
global::AutoRepairTracing.TraceLogger.LogValue("7bad92eb-9f10-4b9b-b74e-7473c7ce6442", "ReflectionXmlSerializationWriter.cs", 1934, "Variable Update", stringValue = FromXmlNmTokens((string)global::AutoRepairTracing.TraceLogger.LogCastSource("0003b332-fa7a-463e-b62c-5755ee8ca7eb", "ReflectionXmlSerializationWriter.cs", 2038, (object)(o), "string")), "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("95a84560-1a4c-488a-950f-ac2cb39a972c", "ReflectionXmlSerializationWriter.cs", 1211, "Break");
                            break;
}                        default:
global::AutoRepairTracing.TraceLogger.Log("991e2575-b13a-43f9-9ad6-caa4fdfc6ed8", "ReflectionXmlSerializationWriter.cs", 1238, "Switch Case [default:]");
global::AutoRepairTracing.TraceLogger.LogValue("b1dec8e0-5401-413c-b577-72a4181c102b", "ReflectionXmlSerializationWriter.cs", 1939, "Variable Update", stringValue = null, "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("45551693-e79c-4122-aad3-2451c7d59e8d", "ReflectionXmlSerializationWriter.cs", 1214, "Return");
                            return false;
}                    }
{global::AutoRepairTracing.TraceLogger.Log("44f13a99-4d85-4b4a-bd4d-3635718cd8f7", "ReflectionXmlSerializationWriter.cs", 1217, "Return");

                    return true;
}                }
                else {global::AutoRepairTracing.TraceLogger.Log("cb3a9119-068f-48f3-8e44-958bd905afdf", "ReflectionXmlSerializationWriter.cs", 1219, "Else Branch");
if (o is char && typeDesc.FormatterName == "Char")
                {
global::AutoRepairTracing.TraceLogger.Log("244c536d-3c99-4f7d-8b3e-d80485264abd", "ReflectionXmlSerializationWriter.cs", 1220, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("099327f8-9990-4283-9df1-230e54b7280a", "ReflectionXmlSerializationWriter.cs", 1951, "Variable Update", stringValue = FromChar((char)o), "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("7f5dd892-85de-4cee-8922-94bb6de2b8e2", "ReflectionXmlSerializationWriter.cs", 1222, "Return");
                    return true;
}                }
                else {global::AutoRepairTracing.TraceLogger.Log("c6fff062-f395-4b8f-9024-3659f62a16fe", "ReflectionXmlSerializationWriter.cs", 1224, "Else Branch");
if (o is byte[])
                {
global::AutoRepairTracing.TraceLogger.Log("06ac4f32-9611-43a6-9688-465a7145860b", "ReflectionXmlSerializationWriter.cs", 1225, "If-True Branch");
                    // we deal with byte[] specially in WritePrimitive()
                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("f922b1c7-8343-449f-9e98-9d1b5d668055", "ReflectionXmlSerializationWriter.cs", 1229, "Else Branch");
                    throw new InvalidOperationException(SR.XmlInternalError);
                }
}}}}}}}}            }
global::AutoRepairTracing.TraceLogger.LogValue("77427414-e3e1-4b0f-aea1-6c0007973b16", "ReflectionXmlSerializationWriter.cs", 1968, "Variable Update", stringValue = null, "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("90a029f1-fe0f-42e3-8ad3-49fa9df6ff39", "ReflectionXmlSerializationWriter.cs", 1235, "Return");
            return false;
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("9c58c99f-ec84-498b-83cd-11f6c8ab6cd7", "ReflectionXmlSerializationWriter.cs", 1140, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("2cd01649-05d9-4476-b4b5-b4e0cf77134f", "ReflectionXmlSerializationWriter.cs", 1263, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("6522b44b-8df4-4942-b1bd-f8dd228e08fe", "ReflectionXmlSerializationWriter.cs", 1140, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("e2daf248-f5f5-482b-9c1a-7781dce174c8", "ReflectionXmlSerializationWriter.cs", 1140, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("c861c568-d37c-4008-a75d-fe069c8a253b", "ReflectionXmlSerializationWriter.cs", 1140, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private static string ConvertPrimitiveToString(object o, TypeDesc typeDesc)
{global::AutoRepairTracing.TraceLogger.Log("3baef58b-ba43-4675-859b-3047bac81e0c", "ReflectionXmlSerializationWriter.cs", 1238, "Enter Method 'ConvertPrimitiveToString'");
try        {
            string stringValue = typeDesc.FormatterName switch
            {
                "Boolean" => XmlConvert.ToString((bool)o),
                "Int32" => XmlConvert.ToString((int)o),
                "Int16" => XmlConvert.ToString((short)o),
                "Int64" => XmlConvert.ToString((long)o),
                "Single" => XmlConvert.ToString((float)o),
                "Double" => XmlConvert.ToString((double)o),
                "Decimal" => XmlConvert.ToString((decimal)o),
                "Byte" => XmlConvert.ToString((byte)o),
                "SByte" => XmlConvert.ToString((sbyte)o),
                "UInt16" => XmlConvert.ToString((ushort)o),
                "UInt32" => XmlConvert.ToString((uint)o),
                "UInt64" => XmlConvert.ToString((ulong)o),
                // Types without direct mapping (ambiguous)
                "Guid" => XmlConvert.ToString((Guid)global::AutoRepairTracing.TraceLogger.LogCastSource("844d76fb-4db5-4e34-bc80-0f0d7f85b349", "ReflectionXmlSerializationWriter.cs", 2096, (object)(o), "Guid")),
                "Char" => XmlConvert.ToString((char)o),
                "TimeSpan" => XmlConvert.ToString((TimeSpan)global::AutoRepairTracing.TraceLogger.LogCastSource("dddee561-812b-42dc-982f-1792303bfe9d", "ReflectionXmlSerializationWriter.cs", 2098, (object)(o), "TimeSpan")),
                "DateTimeOffset" => XmlConvert.ToString((DateTimeOffset)global::AutoRepairTracing.TraceLogger.LogCastSource("6abe6e0e-9957-4bbe-a7fb-da3a3c2bb95e", "ReflectionXmlSerializationWriter.cs", 2099, (object)(o), "DateTimeOffset")),
                _ => o.ToString()!,
            };
global::AutoRepairTracing.TraceLogger.Log("248416ab-b4c6-4e46-99c6-23b8645d7a10", "ReflectionXmlSerializationWriter.cs", 1240, "Variable Update", stringValue, "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("4ad64f7a-ddc3-4db0-b237-950a23e5dfa0", "ReflectionXmlSerializationWriter.cs", 1261, "Return");
            return stringValue;
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("be36b954-ac44-4e53-ae35-e5fdf51ac761", "ReflectionXmlSerializationWriter.cs", 1238, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("f73cced8-0fad-4261-b6eb-8a168997e53d", "ReflectionXmlSerializationWriter.cs", 1290, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("8a0e65e2-45a1-441d-8342-d483b9c80d6f", "ReflectionXmlSerializationWriter.cs", 1238, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("814517e0-b4fc-466e-8d17-b6ac8dfe71b5", "ReflectionXmlSerializationWriter.cs", 1238, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("95627333-b9d7-41fb-838b-723cd190e745", "ReflectionXmlSerializationWriter.cs", 1238, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private void GenerateMembersElement(object o, XmlMembersMapping xmlMembersMapping)
{global::AutoRepairTracing.TraceLogger.Log("1f68efc9-b5ab-463a-963c-6cc556eb3102", "ReflectionXmlSerializationWriter.cs", 1264, "Enter Method 'GenerateMembersElement'");
try        {
            ElementAccessor element = xmlMembersMapping.Accessor;
global::AutoRepairTracing.TraceLogger.Log("7863dafd-e800-4495-9224-9395d8c693c1", "ReflectionXmlSerializationWriter.cs", 1266, "Variable Update", element, "element");
            MembersMapping mapping = (MembersMapping)global::AutoRepairTracing.TraceLogger.LogCastSource("486ad5c1-d217-4dcb-a281-1a4a12af7fe4", "ReflectionXmlSerializationWriter.cs", 2114, (object)(element.Mapping!), "MembersMapping");
global::AutoRepairTracing.TraceLogger.Log("4bbcd0e4-af52-4c58-b13c-fb49dc53f12e", "ReflectionXmlSerializationWriter.cs", 1267, "Variable Update", mapping, "mapping");
            bool hasWrapperElement = mapping.HasWrapperElement;
global::AutoRepairTracing.TraceLogger.Log("3fb26fa3-098a-476a-a60f-22bde73990fb", "ReflectionXmlSerializationWriter.cs", 1268, "Variable Update", hasWrapperElement, "hasWrapperElement");
            bool writeAccessors = mapping.WriteAccessors;
global::AutoRepairTracing.TraceLogger.Log("fa1b0b60-1087-4ae8-87b8-a0af32ff7211", "ReflectionXmlSerializationWriter.cs", 1269, "Variable Update", writeAccessors, "writeAccessors");
            bool isRpc = xmlMembersMapping.IsSoap && writeAccessors;
global::AutoRepairTracing.TraceLogger.Log("33b80e18-459e-4397-9852-66ae874e49fe", "ReflectionXmlSerializationWriter.cs", 1270, "Variable Update", isRpc, "isRpc");

            WriteStartDocument();

            if (!mapping.IsSoap)
            {
global::AutoRepairTracing.TraceLogger.Log("afd1d5ed-dbaf-4e21-9bca-15f50f01d608", "ReflectionXmlSerializationWriter.cs", 1275, "If-True Branch");
                TopLevelElement();
            }

            object[] p = (object[])global::AutoRepairTracing.TraceLogger.LogCastSource("1d553f23-f25c-457a-b8cd-6d1ca86586cd", "ReflectionXmlSerializationWriter.cs", 2131, (object)(o), "object[]");
global::AutoRepairTracing.TraceLogger.Log("266be03e-ddda-4535-bf75-0ea90f21a901", "ReflectionXmlSerializationWriter.cs", 1279, "Heap State: p (object[])");global::AutoRepairTracing.TraceLogger.Log("caff3f43-1f45-4fb3-acb7-93ec2d2fda17", "ReflectionXmlSerializationWriter.cs", 1279, "Variable Update", p, "p");
            int pLength = p.Length;
global::AutoRepairTracing.TraceLogger.Log("7a1e6878-c5c1-4528-9306-419259531eeb", "ReflectionXmlSerializationWriter.cs", 1280, "Variable Update", pLength, "pLength");

            if (hasWrapperElement)
            {
global::AutoRepairTracing.TraceLogger.Log("630dccd4-9031-4392-8b3e-431053ecbf38", "ReflectionXmlSerializationWriter.cs", 1283, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("dfa75ed0-acda-43e1-bfce-873f5d4cca27", "ReflectionXmlSerializationWriter.cs", 1284, "WriteStartElement", 0, element.Name, "element.Name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("32e949b5-936e-4220-bdcf-2e549659bf82", "ReflectionXmlSerializationWriter.cs", 1284, "WriteStartElement", 1, (element.Form == XmlSchemaForm.Qualified ? element.Namespace : string.Empty), "(element.Form == XmlSchemaForm.Qualified ? element.Namespace : string.Empty)");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("8934ac8d-32b7-45ca-aee5-e7653f1d8b20", "ReflectionXmlSerializationWriter.cs", 1284, "WriteStartElement", 2, mapping.IsSoap, "mapping.IsSoap");
                WriteStartElement(element.Name, (element.Form == XmlSchemaForm.Qualified ? element.Namespace : string.Empty), mapping.IsSoap);
}
                int xmlnsMember = FindXmlnsIndex(mapping.Members!);
global::AutoRepairTracing.TraceLogger.Log("a0e7227e-9f6d-4cd8-970f-108b7b011383", "ReflectionXmlSerializationWriter.cs", 1286, "Variable Update", xmlnsMember, "xmlnsMember");
                if (xmlnsMember >= 0)
                {
global::AutoRepairTracing.TraceLogger.Log("fafb90cc-d55c-485b-a10a-924072019741", "ReflectionXmlSerializationWriter.cs", 1288, "If-True Branch");
                    var source = (XmlSerializerNamespaces)global::AutoRepairTracing.TraceLogger.LogCastSource("54276dee-fd90-4ad9-bc13-8f00d4a290bd", "ReflectionXmlSerializationWriter.cs", 2149, (object)(p[xmlnsMember]), "XmlSerializerNamespaces");
global::AutoRepairTracing.TraceLogger.Log("6fee1131-1e4f-4ea7-bbc3-64f431311faf", "ReflectionXmlSerializationWriter.cs", 1289, "Variable Update", source, "source");

                    if (pLength > xmlnsMember)
                    {
global::AutoRepairTracing.TraceLogger.Log("424be7cf-19d3-4f0e-ad96-7ed9440392d4", "ReflectionXmlSerializationWriter.cs", 1292, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("19169784-e664-4021-aae2-9d607108b163", "ReflectionXmlSerializationWriter.cs", 1293, "WriteNamespaceDeclarations", 0, source, "source");
                        WriteNamespaceDeclarations(source);
}                    }
                }
{int __loopCounter_76c6d0a0 = 0;global::AutoRepairTracing.TraceLogger.Log("66df0461-5004-4da9-8d2f-75bf8b566b06", "ReflectionXmlSerializationWriter.cs", 2046, "Variable Update", __loopCounter_76c6d0a0, "__loopCounter_76c6d0a0");

                for (int i = 0; i < mapping.Members!.Length; global::AutoRepairTracing.TraceLogger.LogValue("db1f783e-9154-42b8-b802-d01ad226c9ce", "ReflectionXmlSerializationWriter.cs", 2047, "Variable Update", i++, "i"))
                {
global::AutoRepairTracing.TraceLogger.Log("e068e218-a0e4-4b92-a00e-753440715197", "ReflectionXmlSerializationWriter.cs", 1297, "For Loop");
global::AutoRepairTracing.TraceLogger.LogValue("300b6ee0-9d59-4169-8019-a82cb94d6a51", "ReflectionXmlSerializationWriter.cs", 2050, "Variable Update", __loopCounter_76c6d0a0++, "__loopCounter_76c6d0a0");if(__loopCounter_76c6d0a0 > 10000){global::AutoRepairTracing.TraceLogger.Log("a7010ca1-dd2a-4758-a080-3c025f17115c", "ReflectionXmlSerializationWriter.cs", 1327, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("76c6d0a0-e219-4cd2-878d-4d5ac24f5dce", "ReflectionXmlSerializationWriter.cs", 1297, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_76c6d0a0, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("3e2fde39-4530-4d7a-a3de-8cbcdc1af79a", "ReflectionXmlSerializationWriter.cs", 1327, "Break");
break;}}                    MemberMapping member = mapping.Members[i];
global::AutoRepairTracing.TraceLogger.Log("d30c0246-128b-4c9b-8891-9b5c996983c2", "ReflectionXmlSerializationWriter.cs", 1299, "Variable Update", member, "member");
                    if (member.Attribute != null && !member.Ignore)
                    {
global::AutoRepairTracing.TraceLogger.Log("616994bc-425e-43c9-bfab-56e7836b5765", "ReflectionXmlSerializationWriter.cs", 1301, "If-True Branch");
                        object source = p[i];
global::AutoRepairTracing.TraceLogger.Log("0c404ea8-3352-4a13-89c4-2f3d755b07e5", "ReflectionXmlSerializationWriter.cs", 1302, "Variable Update", source, "source");
                        bool? specifiedSource = null;
global::AutoRepairTracing.TraceLogger.Log("0aaa7439-9a47-44e2-adef-a0f1c8e0d5b8", "ReflectionXmlSerializationWriter.cs", 1303, "Variable Update", specifiedSource, "specifiedSource");
                        if (member.CheckSpecified != SpecifiedAccessor.None)
                        {
global::AutoRepairTracing.TraceLogger.Log("7afd1f76-b282-4a65-b245-4d3b3abcd188", "ReflectionXmlSerializationWriter.cs", 1305, "If-True Branch");
                            string memberNameSpecified = $"{member.Name}Specified";
global::AutoRepairTracing.TraceLogger.Log("90e87c8d-8f9b-4f55-8784-59ab8a1ad700", "ReflectionXmlSerializationWriter.cs", 1306, "Variable Update", memberNameSpecified, "memberNameSpecified");
                            for (int j = 0; j < Math.Min(pLength, mapping.Members.Length); global::AutoRepairTracing.TraceLogger.LogValue("633e4c80-94a6-460c-8d52-d15938175e17", "ReflectionXmlSerializationWriter.cs", 2062, "Variable Update", j++, "j"))
                            {
global::AutoRepairTracing.TraceLogger.Log("8690ee3b-5c7f-4689-a8dc-ec2cfb424e9e", "ReflectionXmlSerializationWriter.cs", 1307, "For Loop");
                                if (mapping.Members[j].Name == memberNameSpecified)
                                {
global::AutoRepairTracing.TraceLogger.Log("b081cc43-7afa-4804-99f1-4f3bdb7bff6b", "ReflectionXmlSerializationWriter.cs", 1310, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("e1a6c175-2e87-4021-ae65-169371e7048b", "ReflectionXmlSerializationWriter.cs", 1311, "Variable Update", j, "index:p");
global::AutoRepairTracing.TraceLogger.LogValue("1956fe11-ca72-46e5-8ed9-cde3a342b0b3", "ReflectionXmlSerializationWriter.cs", 2068, "Variable Update", specifiedSource = (bool)p[j], "specifiedSource");
}{global::AutoRepairTracing.TraceLogger.Log("82e361f9-506d-4fba-bc7a-b5fbfc26db1c", "ReflectionXmlSerializationWriter.cs", 1312, "Break");
                                    break;
}                                }
                            }
                        }

                        if (pLength > i && (specifiedSource == null || specifiedSource.Value))
                        {
global::AutoRepairTracing.TraceLogger.Log("c79565ad-ff60-47e7-8454-a4fa68c0b423", "ReflectionXmlSerializationWriter.cs", 1318, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("044d7737-3f65-4c0c-8c40-b1bdb9d447c0", "ReflectionXmlSerializationWriter.cs", 1319, "WriteMember", 0, source, "source");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("acf93036-db02-46f0-a278-04ed1988221f", "ReflectionXmlSerializationWriter.cs", 1319, "WriteMember", 1, member.Attribute, "member.Attribute");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("8373682a-106f-4918-80db-034c7d6ca541", "ReflectionXmlSerializationWriter.cs", 1319, "WriteMember", 2, member.TypeDesc!, "member.TypeDesc!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("17159540-810a-458d-93d6-77fbfde8b976", "ReflectionXmlSerializationWriter.cs", 1319, "WriteMember", 3, null, "null");
                            WriteMember(source, member.Attribute, member.TypeDesc!, null);
}                        }
                    }
                }
}            }
{int __loopCounter_82912c9a = 0;global::AutoRepairTracing.TraceLogger.Log("9a783c12-e90c-4d64-8200-90f5cc49d703", "ReflectionXmlSerializationWriter.cs", 2087, "Variable Update", __loopCounter_82912c9a, "__loopCounter_82912c9a");

            for (int i = 0; i < mapping.Members!.Length; global::AutoRepairTracing.TraceLogger.LogValue("2850a71f-36e5-45aa-8e78-d44108ecafdd", "ReflectionXmlSerializationWriter.cs", 2088, "Variable Update", i++, "i"))
            {
global::AutoRepairTracing.TraceLogger.Log("834a9be7-827b-41be-a67a-4bf21e7e6392", "ReflectionXmlSerializationWriter.cs", 1325, "For Loop");
global::AutoRepairTracing.TraceLogger.LogValue("541d8f38-d8ec-4e80-844a-1074f1ab9fa0", "ReflectionXmlSerializationWriter.cs", 2091, "Variable Update", __loopCounter_82912c9a++, "__loopCounter_82912c9a");if(__loopCounter_82912c9a > 10000){global::AutoRepairTracing.TraceLogger.Log("462ac95c-0002-4439-8ef5-6401d3e31f38", "ReflectionXmlSerializationWriter.cs", 1355, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("82912c9a-a12d-48df-865e-b92a52a6919e", "ReflectionXmlSerializationWriter.cs", 1325, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_82912c9a, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("f572e066-e4d8-4f96-b4de-7dfeb5988652", "ReflectionXmlSerializationWriter.cs", 1355, "Break");
break;}}                MemberMapping member = mapping.Members[i];
global::AutoRepairTracing.TraceLogger.Log("f31a243f-925b-4665-ad9d-6e2618245801", "ReflectionXmlSerializationWriter.cs", 1327, "Variable Update", member, "member");
                if (member.Xmlns != null)
{global::AutoRepairTracing.TraceLogger.Log("04aa763e-4bbb-4d95-ad36-ce871b98208e", "ReflectionXmlSerializationWriter.cs", 1329, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("de476526-37aa-4547-bf63-7a49c4b6c930", "ReflectionXmlSerializationWriter.cs", 1329, "Continue");
                    continue;
}
                if (member.Ignore)
{global::AutoRepairTracing.TraceLogger.Log("7e34a9aa-f716-47f4-a018-6edd25ea959a", "ReflectionXmlSerializationWriter.cs", 1332, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("bc0b12ee-80e2-4843-92ad-0144990615b1", "ReflectionXmlSerializationWriter.cs", 1332, "Continue");
                    continue;
}
                bool? specifiedSource = null;
global::AutoRepairTracing.TraceLogger.Log("8097818a-b20f-430a-b71d-710bfe1f9041", "ReflectionXmlSerializationWriter.cs", 1334, "Variable Update", specifiedSource, "specifiedSource");
                if (member.CheckSpecified != SpecifiedAccessor.None)
                {
global::AutoRepairTracing.TraceLogger.Log("e07198e6-c908-4af5-a5fd-960a3dca31b2", "ReflectionXmlSerializationWriter.cs", 1336, "If-True Branch");
                    string memberNameSpecified = $"{member.Name}Specified";
global::AutoRepairTracing.TraceLogger.Log("5d605303-fa74-4405-9c3a-dc53f702da12", "ReflectionXmlSerializationWriter.cs", 1337, "Variable Update", memberNameSpecified, "memberNameSpecified");
                    for (int j = 0; j < Math.Min(pLength, mapping.Members.Length); global::AutoRepairTracing.TraceLogger.LogValue("1176b3ae-b734-49c1-83a4-1fc97354c65a", "ReflectionXmlSerializationWriter.cs", 2109, "Variable Update", j++, "j"))
                    {
global::AutoRepairTracing.TraceLogger.Log("a0424450-fdf0-464a-b346-281f5e545122", "ReflectionXmlSerializationWriter.cs", 1338, "For Loop");
                        if (mapping.Members[j].Name == memberNameSpecified)
                        {
global::AutoRepairTracing.TraceLogger.Log("7e6c5a5a-1891-49dd-a57a-e5c4c7417b70", "ReflectionXmlSerializationWriter.cs", 1341, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("0668754b-69da-405a-a3b2-b951d458d298", "ReflectionXmlSerializationWriter.cs", 1342, "Variable Update", j, "index:p");
global::AutoRepairTracing.TraceLogger.LogValue("962a88dc-f350-408b-b5f0-9cc00d98b236", "ReflectionXmlSerializationWriter.cs", 2115, "Variable Update", specifiedSource = (bool)p[j], "specifiedSource");
}{global::AutoRepairTracing.TraceLogger.Log("ef2e0695-59ec-45c2-9aad-bb8fcbbad4c5", "ReflectionXmlSerializationWriter.cs", 1343, "Break");
                            break;
}                        }
                    }
                }

                if (pLength > i)
                {
global::AutoRepairTracing.TraceLogger.Log("53ed4a34-f05e-4ff9-b76a-e6678d692a59", "ReflectionXmlSerializationWriter.cs", 1349, "If-True Branch");
                    if (specifiedSource == null || specifiedSource.Value)
                    {
global::AutoRepairTracing.TraceLogger.Log("4695b932-8f86-439b-93bb-f6c231c86121", "ReflectionXmlSerializationWriter.cs", 1351, "If-True Branch");

                        object source = p[i];
global::AutoRepairTracing.TraceLogger.Log("3e6847fc-8597-47a8-8770-82a49424910e", "ReflectionXmlSerializationWriter.cs", 1353, "Variable Update", source, "source");
                        object? enumSource = null;
global::AutoRepairTracing.TraceLogger.Log("7cd0dfa1-040c-4196-8cef-be419430f7b5", "ReflectionXmlSerializationWriter.cs", 1354, "Variable Update", enumSource, "enumSource");
                        if (member.ChoiceIdentifier != null)
                        {
global::AutoRepairTracing.TraceLogger.Log("7421b5f3-aec3-48dd-a5c1-b57c2c56ddca", "ReflectionXmlSerializationWriter.cs", 1356, "If-True Branch");
                            for (int j = 0; j < mapping.Members.Length; global::AutoRepairTracing.TraceLogger.LogValue("af119497-88c0-49df-b405-f1d31f6507f5", "ReflectionXmlSerializationWriter.cs", 2134, "Variable Update", j++, "j"))
                            {
global::AutoRepairTracing.TraceLogger.Log("471f2c0f-746c-4279-bba7-6dbcb006cea3", "ReflectionXmlSerializationWriter.cs", 1357, "For Loop");
                                if (mapping.Members[j].Name == member.ChoiceIdentifier.MemberName)
                                {
global::AutoRepairTracing.TraceLogger.Log("d58ff31f-86e1-462d-b4da-75c4acb27c39", "ReflectionXmlSerializationWriter.cs", 1360, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("c1c11eb0-ed8b-4d60-a9c9-b97ce3ee3590", "ReflectionXmlSerializationWriter.cs", 1361, "Variable Update", j, "index:p");
global::AutoRepairTracing.TraceLogger.LogValue("0dae4015-d7eb-438e-a3ac-2845c3d8e155", "ReflectionXmlSerializationWriter.cs", 2140, "Variable Update", enumSource = p[j], "enumSource");
}{global::AutoRepairTracing.TraceLogger.Log("5c7b44ca-fbbf-4e26-ba12-3385b8268844", "ReflectionXmlSerializationWriter.cs", 1362, "Break");
                                    break;
}                                }
                            }
                        }

                        if (isRpc && member.IsReturnValue && member.Elements!.Length > 0)
                        {
global::AutoRepairTracing.TraceLogger.Log("142ab034-0270-4518-afce-27ee59201078", "ReflectionXmlSerializationWriter.cs", 1368, "If-True Branch");
{{global::AutoRepairTracing.TraceLogger.Log("5d804391-344c-4e2c-8524-3408155e0093", "ReflectionXmlSerializationWriter.cs", 2150, "Variable Update", 0, "index:member.Elements");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("299b374c-e7b8-401b-8865-ae9fa567a99a", "ReflectionXmlSerializationWriter.cs", 1369, "WriteRpcResult", 0, member.Elements[0].Name, "member.Elements[0].Name");
}global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("b68917a8-d42c-4c71-bf9e-43532db315b2", "ReflectionXmlSerializationWriter.cs", 1369, "WriteRpcResult", 1, string.Empty, "string.Empty");
{global::AutoRepairTracing.TraceLogger.Log("006018cb-c915-442f-a0f9-8080732798fc", "ReflectionXmlSerializationWriter.cs", 1369, "Variable Update", 0, "index:member.Elements");
                            WriteRpcResult(member.Elements[0].Name, string.Empty);
}}                        }
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("cae20dfa-4c9e-4505-8f98-699808be2789", "ReflectionXmlSerializationWriter.cs", 1373, "WriteMember", 0, source, "source");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("67579dbb-c110-42fc-9fa3-0c0562788c97", "ReflectionXmlSerializationWriter.cs", 1373, "WriteMember", 1, enumSource, "enumSource");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("eb0b9eed-b7dd-4a20-a59f-aad14ac7178b", "ReflectionXmlSerializationWriter.cs", 1373, "WriteMember", 2, member.ElementsSortedByDerivation!, "member.ElementsSortedByDerivation!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("7d679bb5-8e4e-4686-bb22-f887e0c11800", "ReflectionXmlSerializationWriter.cs", 1373, "WriteMember", 3, member.Text, "member.Text");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("8eab6200-5220-4a74-94b8-f5a3b9af2916", "ReflectionXmlSerializationWriter.cs", 1373, "WriteMember", 4, member.ChoiceIdentifier, "member.ChoiceIdentifier");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("63725a3f-51f8-40e7-9225-eef1d9d46823", "ReflectionXmlSerializationWriter.cs", 1373, "WriteMember", 5, member.TypeDesc!, "member.TypeDesc!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("005fc65e-40f1-4d40-88e9-2933f2fcc093", "ReflectionXmlSerializationWriter.cs", 1373, "WriteMember", 6, writeAccessors || hasWrapperElement, "writeAccessors || hasWrapperElement");

                        // override writeAccessors choice when we've written a wrapper element
                        WriteMember(source, enumSource, member.ElementsSortedByDerivation!, member.Text, member.ChoiceIdentifier, member.TypeDesc!, writeAccessors || hasWrapperElement);
}                    }
                }
            }
}
            if (hasWrapperElement)
            {
global::AutoRepairTracing.TraceLogger.Log("e9a2f810-984d-4bb7-af7f-0108fb8e0799", "ReflectionXmlSerializationWriter.cs", 1379, "If-True Branch");
                WriteEndElement();
            }

            if (element.IsSoap)
            {
global::AutoRepairTracing.TraceLogger.Log("1ea4a47b-242b-4ea9-9bda-dfbe8a4afaf9", "ReflectionXmlSerializationWriter.cs", 1384, "If-True Branch");
                if (!hasWrapperElement && !writeAccessors)
                {
global::AutoRepairTracing.TraceLogger.Log("f43395b5-1b36-4cd8-88c2-b6eea487e1af", "ReflectionXmlSerializationWriter.cs", 1386, "If-True Branch");
                    // doc/bare case -- allow extra members
                    if (pLength > mapping.Members.Length)
                    {
global::AutoRepairTracing.TraceLogger.Log("0164d4e8-3d65-443f-a1f9-960c74bde177", "ReflectionXmlSerializationWriter.cs", 1389, "If-True Branch");
{int __loopCounter_06a4ee30 = 0;global::AutoRepairTracing.TraceLogger.Log("a3c4a708-2259-45e7-982a-08ec8455109a", "ReflectionXmlSerializationWriter.cs", 2184, "Variable Update", __loopCounter_06a4ee30, "__loopCounter_06a4ee30");
                        for (int i = mapping.Members.Length; i < pLength; global::AutoRepairTracing.TraceLogger.LogValue("621cca0c-0562-4ed6-ada1-0b2e77855321", "ReflectionXmlSerializationWriter.cs", 2184, "Variable Update", i++, "i"))
                        {
global::AutoRepairTracing.TraceLogger.Log("9f159525-d146-4740-9eb0-38231dd4ce7e", "ReflectionXmlSerializationWriter.cs", 1390, "For Loop");
global::AutoRepairTracing.TraceLogger.LogValue("3ce6233b-cd16-48d7-a56d-6a510693eb5a", "ReflectionXmlSerializationWriter.cs", 2187, "Variable Update", __loopCounter_06a4ee30++, "__loopCounter_06a4ee30");if(__loopCounter_06a4ee30 > 10000){global::AutoRepairTracing.TraceLogger.Log("1632a605-df4f-4602-a0ca-094962b928a1", "ReflectionXmlSerializationWriter.cs", 1420, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("06a4ee30-4d47-4f00-abaf-dc9fbadc2d64", "ReflectionXmlSerializationWriter.cs", 1390, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_06a4ee30, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("6c0c8458-446c-4e15-9259-778669b574c8", "ReflectionXmlSerializationWriter.cs", 1420, "Break");
break;}}                            if (p[i] != null)
                            {
global::AutoRepairTracing.TraceLogger.Log("2068516b-551e-49ec-b024-ca35a2ae7151", "ReflectionXmlSerializationWriter.cs", 1393, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("795b6724-ccc9-4edc-8240-f20c236460b0", "ReflectionXmlSerializationWriter.cs", 1394, "WritePotentiallyReferencingElement", 0, null, "null");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("50e44258-d299-455f-a050-70f67c09f83b", "ReflectionXmlSerializationWriter.cs", 1394, "WritePotentiallyReferencingElement", 1, null, "null");
{global::AutoRepairTracing.TraceLogger.Log("46695a43-f017-4357-9f7a-6724ee35564d", "ReflectionXmlSerializationWriter.cs", 2194, "Variable Update", i, "index:p");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("47a8b2f2-ef94-4e66-98a2-33340c4930ba", "ReflectionXmlSerializationWriter.cs", 1394, "WritePotentiallyReferencingElement", 2, p[i], "p[i]");
}{global::AutoRepairTracing.TraceLogger.Log("04bfabf7-5100-42ac-9909-6facbd3ecd3f", "ReflectionXmlSerializationWriter.cs", 2195, "Variable Update", i, "index:p");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("46c78fe1-eb3b-408d-adb5-8d9be927a876", "ReflectionXmlSerializationWriter.cs", 1394, "WritePotentiallyReferencingElement", 3, p[i].GetType(), "p[i].GetType()");
}global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("4f86457e-f63d-4254-8594-9a926e0585ef", "ReflectionXmlSerializationWriter.cs", 1394, "WritePotentiallyReferencingElement", 4, true, "true");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("6563b1c2-35fb-4293-a480-2238a9314e78", "ReflectionXmlSerializationWriter.cs", 1394, "WritePotentiallyReferencingElement", 5, false, "false");
{global::AutoRepairTracing.TraceLogger.Log("504f19ab-bfad-4f76-bfd1-dec80c229c7d", "ReflectionXmlSerializationWriter.cs", 1394, "Variable Update", i, "index:p");
global::AutoRepairTracing.TraceLogger.Log("ebde355b-8845-428d-ac2a-6ca0e72e6c5e", "ReflectionXmlSerializationWriter.cs", 1394, "Variable Update", i, "index:p");
                                WritePotentiallyReferencingElement(null, null, p[i], p[i].GetType(), true, false);
}}                            }
                        }
}                    }
                }

                WriteReferencedElements();
            }
        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("37628793-3e05-4fd2-9f4d-50f97f64d7db", "ReflectionXmlSerializationWriter.cs", 1264, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("ae50bf09-4613-486a-821c-fb3a09a21f9d", "ReflectionXmlSerializationWriter.cs", 1431, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("2651a3df-644c-40e6-a7f0-bcfd0f90b56c", "ReflectionXmlSerializationWriter.cs", 1264, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("16711bb3-9925-4a83-ae01-0588859904df", "ReflectionXmlSerializationWriter.cs", 1264, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("46589209-8599-4c09-8728-4d4989233a45", "ReflectionXmlSerializationWriter.cs", 1264, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        [Flags]
        private enum WritePrimitiveMethodRequirement
        {
            None = 0,
            Raw = 1,
            WriteAttribute = 2,
            WriteElementString = 4,
            WriteNullableStringLiteral = 8,
            Encoded = 16
        }
    }

    [RequiresUnreferencedCode(XmlSerializer.TrimSerializationWarning)]
    internal static class ReflectionXmlSerializationHelper
    {
        public static MemberInfo? GetMember(Type declaringType, string memberName, bool throwOnNotFound)
{global::AutoRepairTracing.TraceLogger.Log("8deef3ac-85b8-4938-941f-b1722624b360", "ReflectionXmlSerializationWriter.cs", 1419, "Enter Method 'GetMember'");
try        {
            MemberInfo[] memberInfos = declaringType.GetMember(memberName);
global::AutoRepairTracing.TraceLogger.Log("1118d2b0-c86b-444c-91bf-9862a5b4cb25", "ReflectionXmlSerializationWriter.cs", 1421, "Heap State: memberInfos (MemberInfo[])");global::AutoRepairTracing.TraceLogger.Log("93ec6dfb-2515-48c2-87b1-8e10ae23b0e8", "ReflectionXmlSerializationWriter.cs", 1421, "Variable Update", memberInfos, "memberInfos");
            if (memberInfos == null || memberInfos.Length == 0)
            {
global::AutoRepairTracing.TraceLogger.Log("8607f288-98bd-458e-a6b3-9f7ba702805c", "ReflectionXmlSerializationWriter.cs", 1423, "If-True Branch");
                bool foundMatchedMember = false;
global::AutoRepairTracing.TraceLogger.Log("71559324-621d-4801-93e7-aad7bd78dc3f", "ReflectionXmlSerializationWriter.cs", 1424, "Variable Update", foundMatchedMember, "foundMatchedMember");
                Type? currentType = declaringType.BaseType;
global::AutoRepairTracing.TraceLogger.Log("7eb1462a-84b9-4975-bf81-c541b197701a", "ReflectionXmlSerializationWriter.cs", 1425, "Variable Update", currentType, "currentType");
{int __loopCounter_0e15636a = 0;global::AutoRepairTracing.TraceLogger.Log("5b6e61ec-4a8d-4795-afc3-de6274d34513", "ReflectionXmlSerializationWriter.cs", 2234, "Variable Update", __loopCounter_0e15636a, "__loopCounter_0e15636a");
                while (currentType != null)
                {
global::AutoRepairTracing.TraceLogger.Log("9c4be991-c9d2-4525-91c2-1166fac2a44a", "ReflectionXmlSerializationWriter.cs", 1426, "While Loop");
global::AutoRepairTracing.TraceLogger.LogValue("c5ec0467-8e2c-46df-8646-67c17958ef19", "ReflectionXmlSerializationWriter.cs", 2237, "Variable Update", __loopCounter_0e15636a++, "__loopCounter_0e15636a");if(__loopCounter_0e15636a > 10000){global::AutoRepairTracing.TraceLogger.Log("3e9cf7bd-e968-41b0-b884-794944a34fb6", "ReflectionXmlSerializationWriter.cs", 1457, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("0e15636a-f1ea-4583-bae3-81a5f462ceec", "ReflectionXmlSerializationWriter.cs", 1426, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_0e15636a, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("f54695ca-f417-4050-8e39-481ddf16b402", "ReflectionXmlSerializationWriter.cs", 1457, "Break");
break;}}global::AutoRepairTracing.TraceLogger.LogValue("d911f40f-1d83-4795-8c36-621a090b1925", "ReflectionXmlSerializationWriter.cs", 2239, "Variable Update", memberInfos = currentType.GetMember(memberName), "memberInfos");
                    if (memberInfos != null && memberInfos.Length != 0)
                    {
global::AutoRepairTracing.TraceLogger.Log("f5dba9ec-c687-4aef-b3fd-48e4bbbfa148", "ReflectionXmlSerializationWriter.cs", 1430, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("13dca1ab-c890-4165-8f9a-72d253fd328f", "ReflectionXmlSerializationWriter.cs", 2243, "Variable Update", foundMatchedMember = true, "foundMatchedMember");
{global::AutoRepairTracing.TraceLogger.Log("fd57e6cc-6c6a-44b8-a4ac-73d904516508", "ReflectionXmlSerializationWriter.cs", 1432, "Break");
                        break;
}                    }
global::AutoRepairTracing.TraceLogger.LogValue("d11001a6-5d9d-4abe-a498-34d0f26f5cf3", "ReflectionXmlSerializationWriter.cs", 2248, "Variable Update", currentType = currentType.BaseType, "currentType");
                }
}
                if (!foundMatchedMember)
                {
global::AutoRepairTracing.TraceLogger.Log("398fd893-06d4-4c65-89f4-efc8db5d6a6b", "ReflectionXmlSerializationWriter.cs", 1439, "If-True Branch");
                    if (throwOnNotFound)
                    {
global::AutoRepairTracing.TraceLogger.Log("7d768f54-7b2d-423e-a949-609a68bf7bd5", "ReflectionXmlSerializationWriter.cs", 1441, "If-True Branch");
                        throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorDetails, $"Could not find member named {memberName} of type {declaringType}"));
                    }
{global::AutoRepairTracing.TraceLogger.Log("049b80fb-5799-498a-ab47-e0e390513e1c", "ReflectionXmlSerializationWriter.cs", 1444, "Return");
                    return null;
}                }
global::AutoRepairTracing.TraceLogger.LogValue("816381b5-6d65-4a5a-95a6-351bcac71c86", "ReflectionXmlSerializationWriter.cs", 2263, "Variable Update", declaringType = currentType!, "declaringType");
            }

            MemberInfo memberInfo = memberInfos![0];
global::AutoRepairTracing.TraceLogger.Log("a3df01f9-b208-446e-96a7-298692ed8f66", "ReflectionXmlSerializationWriter.cs", 1450, "Variable Update", memberInfo, "memberInfo");
            if (memberInfos.Length != 1)
            {
global::AutoRepairTracing.TraceLogger.Log("432f9afe-f98b-40ce-93d4-29d8a0f82699", "ReflectionXmlSerializationWriter.cs", 1452, "If-True Branch");
                foreach (MemberInfo mi in memberInfos)
                {
global::AutoRepairTracing.TraceLogger.Log("e271c868-5371-43d5-a646-c11d20953702", "ReflectionXmlSerializationWriter.cs", 1453, "ForEach Loop");
                    if (declaringType == mi.DeclaringType)
                    {
global::AutoRepairTracing.TraceLogger.Log("124e5baa-86c4-4859-ab06-31561bb7b388", "ReflectionXmlSerializationWriter.cs", 1456, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("e86e042c-b38b-4f6f-a60a-b3492a297b7d", "ReflectionXmlSerializationWriter.cs", 2276, "Variable Update", memberInfo = mi, "memberInfo");
{global::AutoRepairTracing.TraceLogger.Log("edd061fe-769b-4a92-8069-3a4becb1420b", "ReflectionXmlSerializationWriter.cs", 1458, "Break");
                        break;
}                    }
                }
            }
{global::AutoRepairTracing.TraceLogger.Log("f13d7c05-8062-47eb-b7c7-09ad13a05912", "ReflectionXmlSerializationWriter.cs", 1463, "Return");

            return memberInfo;
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("ddf556af-498b-437e-b799-f967f31eaa4a", "ReflectionXmlSerializationWriter.cs", 1419, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("1b382be8-e69e-401b-bbaa-23d87cbc90e3", "ReflectionXmlSerializationWriter.cs", 1494, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("11197a2f-48d1-47ad-9dcd-086a81a1013f", "ReflectionXmlSerializationWriter.cs", 1419, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("840aed4f-b6c6-479b-ae40-f56238524673", "ReflectionXmlSerializationWriter.cs", 1419, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("9eaa9719-a3d7-42cf-8ed7-b90a5a3a96d1", "ReflectionXmlSerializationWriter.cs", 1419, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        public static MemberInfo GetEffectiveGetInfo(Type declaringType, string memberName)
{global::AutoRepairTracing.TraceLogger.Log("98240ff4-1562-40ec-be44-89de7075be86", "ReflectionXmlSerializationWriter.cs", 1466, "Enter Method 'GetEffectiveGetInfo'");
try        {
            MemberInfo memberInfo = GetMember(declaringType, memberName, true)!;
global::AutoRepairTracing.TraceLogger.Log("a6b63876-94bd-4a19-9007-aeb964aba958", "ReflectionXmlSerializationWriter.cs", 1468, "Variable Update", memberInfo, "memberInfo");

            // For properties, we might have a PropertyInfo that does not have a valid
            // getter at this level of inheritance. If that's the case, we need to look
            // up the chain to find the right PropertyInfo for the getter.
            if (memberInfo is PropertyInfo propInfo && propInfo.GetMethod == null)
            {
global::AutoRepairTracing.TraceLogger.Log("bd90d7e3-2947-42d2-9e32-17144241a0c6", "ReflectionXmlSerializationWriter.cs", 1474, "If-True Branch");
                var parent = declaringType.BaseType;
global::AutoRepairTracing.TraceLogger.Log("6053ef00-b89e-4576-814a-cc5d30a05a51", "ReflectionXmlSerializationWriter.cs", 1475, "Variable Update", parent, "parent");
{int __loopCounter_38b91750 = 0;global::AutoRepairTracing.TraceLogger.Log("caf501a9-883c-4cfd-a8c0-a004eb55aee5", "ReflectionXmlSerializationWriter.cs", 2301, "Variable Update", __loopCounter_38b91750, "__loopCounter_38b91750");

                while (parent != null)
                {
global::AutoRepairTracing.TraceLogger.Log("0386abc7-8c22-4968-827a-1b9611dcf1c7", "ReflectionXmlSerializationWriter.cs", 1477, "While Loop");
global::AutoRepairTracing.TraceLogger.LogValue("27016299-47dd-4b4f-bede-2fb32e8408ad", "ReflectionXmlSerializationWriter.cs", 2305, "Variable Update", __loopCounter_38b91750++, "__loopCounter_38b91750");if(__loopCounter_38b91750 > 10000){global::AutoRepairTracing.TraceLogger.Log("f9372b8b-99f7-4801-acaa-ba8e32733e67", "ReflectionXmlSerializationWriter.cs", 1509, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("38b91750-6999-4ec5-b9fc-1a66d31fd987", "ReflectionXmlSerializationWriter.cs", 1477, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_38b91750, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("86faa760-79a6-4bf5-af70-a62ab5b1fb51", "ReflectionXmlSerializationWriter.cs", 1509, "Break");
break;}}                    var mi = GetMember(parent, memberName, false);
global::AutoRepairTracing.TraceLogger.Log("6ef0aa84-d5d7-4e38-aa19-1819a79dbc0b", "ReflectionXmlSerializationWriter.cs", 1479, "Variable Update", mi, "mi");

                    if (mi is PropertyInfo pi && pi.GetMethod != null && pi.PropertyType == propInfo.PropertyType)
                    {
global::AutoRepairTracing.TraceLogger.Log("803edd45-7d0b-4406-bd8a-b558f34ecd41", "ReflectionXmlSerializationWriter.cs", 1482, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("13509561-0cdd-4c11-993b-d502e098a083", "ReflectionXmlSerializationWriter.cs", 1483, "Return");
                        return pi;
}                    }
global::AutoRepairTracing.TraceLogger.LogValue("e56b10a4-b049-40a4-bead-173092c8689c", "ReflectionXmlSerializationWriter.cs", 2316, "Variable Update", parent = parent.BaseType, "parent");
                }
}            }
{global::AutoRepairTracing.TraceLogger.Log("c1859e0a-b12b-479b-8f1d-d27b210d6c21", "ReflectionXmlSerializationWriter.cs", 1490, "Return");

            return memberInfo;
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("b4ed0bcc-dd8a-4acb-9e15-441cef0f0fc8", "ReflectionXmlSerializationWriter.cs", 1466, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("42a1f997-33db-4b1b-9530-5016ba23b09c", "ReflectionXmlSerializationWriter.cs", 1522, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("3ff9563b-072c-4c0a-88c0-07e81f96c51c", "ReflectionXmlSerializationWriter.cs", 1466, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("f22c5a5f-2c41-45d9-93bf-4a3066ca475d", "ReflectionXmlSerializationWriter.cs", 1466, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("d3f18914-9e2e-41f2-bb16-dfbb32a01c44", "ReflectionXmlSerializationWriter.cs", 1466, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        public static MemberInfo GetEffectiveSetInfo(Type declaringType, string memberName)
{global::AutoRepairTracing.TraceLogger.Log("18869536-f7c3-4294-bffd-feff12705e4f", "ReflectionXmlSerializationWriter.cs", 1493, "Enter Method 'GetEffectiveSetInfo'");
try        {
            MemberInfo memberInfo = GetMember(declaringType, memberName, true)!;
global::AutoRepairTracing.TraceLogger.Log("0ed8d7eb-8a0f-4cda-968f-3fc48d28970e", "ReflectionXmlSerializationWriter.cs", 1495, "Variable Update", memberInfo, "memberInfo");

            // For properties, we might have a PropertyInfo that does not have a valid
            // setter at this level of inheritance. If that's the case, we need to look
            // up the chain to find the right PropertyInfo for the setter.
            if (memberInfo is PropertyInfo propInfo && propInfo.SetMethod == null)
            {
global::AutoRepairTracing.TraceLogger.Log("55fdc2ca-a31b-424f-a059-45bda62bbab1", "ReflectionXmlSerializationWriter.cs", 1501, "If-True Branch");
                var parent = declaringType.BaseType;
global::AutoRepairTracing.TraceLogger.Log("bffa6b7a-aede-4f09-9862-20511e709510", "ReflectionXmlSerializationWriter.cs", 1502, "Variable Update", parent, "parent");
{int __loopCounter_dda4e5b8 = 0;global::AutoRepairTracing.TraceLogger.Log("92d6b617-9b8a-4e17-a4ed-cf7bb5f1cd3a", "ReflectionXmlSerializationWriter.cs", 2338, "Variable Update", __loopCounter_dda4e5b8, "__loopCounter_dda4e5b8");

                while (parent != null)
                {
global::AutoRepairTracing.TraceLogger.Log("03fb17a5-5604-42fa-8ba9-cb1384fea91c", "ReflectionXmlSerializationWriter.cs", 1504, "While Loop");
global::AutoRepairTracing.TraceLogger.LogValue("33e1943c-4075-44f4-81da-7118aa511e96", "ReflectionXmlSerializationWriter.cs", 2342, "Variable Update", __loopCounter_dda4e5b8++, "__loopCounter_dda4e5b8");if(__loopCounter_dda4e5b8 > 10000){global::AutoRepairTracing.TraceLogger.Log("a727a39d-6181-47bf-9360-c8a3a414c035", "ReflectionXmlSerializationWriter.cs", 1537, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("dda4e5b8-eec0-452f-a9c3-dfc937852331", "ReflectionXmlSerializationWriter.cs", 1504, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_dda4e5b8, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("81277801-664e-4a27-b85b-326cdc2efd46", "ReflectionXmlSerializationWriter.cs", 1537, "Break");
break;}}                    var mi = GetMember(parent, memberName, false);
global::AutoRepairTracing.TraceLogger.Log("611b7b72-545d-481d-af17-c45933753c54", "ReflectionXmlSerializationWriter.cs", 1506, "Variable Update", mi, "mi");

                    if (mi is PropertyInfo pi && pi.SetMethod != null && pi.PropertyType == propInfo.PropertyType)
                    {
global::AutoRepairTracing.TraceLogger.Log("b9e88bc4-2a7b-48e9-971a-82f0c0fb0c65", "ReflectionXmlSerializationWriter.cs", 1509, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("02e25686-9db5-4e12-9769-1748478b4ba6", "ReflectionXmlSerializationWriter.cs", 1510, "Return");
                        return pi;
}                    }
global::AutoRepairTracing.TraceLogger.LogValue("270740b9-18e3-4a5e-bceb-b0ac2559cb80", "ReflectionXmlSerializationWriter.cs", 2353, "Variable Update", parent = parent.BaseType, "parent");
                }
}            }
{global::AutoRepairTracing.TraceLogger.Log("81eb398c-1da0-4477-9afd-d9e5bbbe4a76", "ReflectionXmlSerializationWriter.cs", 1517, "Return");

            return memberInfo;
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("b444c052-cae0-4f73-aac9-b2d56410a4c9", "ReflectionXmlSerializationWriter.cs", 1493, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("6f951773-8b17-4bc2-a63e-3f1fb999513b", "ReflectionXmlSerializationWriter.cs", 1550, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("9e3cc223-9c19-4efb-8af0-695eaf686238", "ReflectionXmlSerializationWriter.cs", 1493, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("1127b8a1-33d6-4394-955f-b7fffb9e1260", "ReflectionXmlSerializationWriter.cs", 1493, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("e321ef2f-7ad2-4bb0-84a8-63c6f1e1de3b", "ReflectionXmlSerializationWriter.cs", 1493, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}    }
}

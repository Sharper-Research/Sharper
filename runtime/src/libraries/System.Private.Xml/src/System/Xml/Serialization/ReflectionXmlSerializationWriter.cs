// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

#pragma warning disable CS8604, CS8625, CS8600, CS8603, CS1503, SA1000, CA1810

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
global::AutoRepairTracing.TraceLogger.Log("a2986a35-8efb-4f37-9d81-b60e1f0dcb7f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 21, "Enter Method 'ReflectionXmlSerializationWriter'");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("abfe7f84-7a62-4b81-9b8b-23569309f03b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 23, "Init", 0, xmlWriter, "xmlWriter");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("04a9eef7-e79e-4c68-b825-f5bed9787928", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 23, "Init", 1, namespaces, "namespaces");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("71ffa525-c85c-4898-9010-811c209be01d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 23, "Init", 2, encodingStyle, "encodingStyle");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("1d671d2d-5fa8-47dd-93b8-c3da8a81db55", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 23, "Init", 3, id, "id");
            Init(xmlWriter, namespaces, encodingStyle, id);
}
            if (!xmlMapping.IsWriteable || !xmlMapping.GenerateSerializer)
            {
global::AutoRepairTracing.TraceLogger.Log("72384a4d-df68-4bd1-874b-3c55d889bb52", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 26, "If-True Branch");
                throw new ArgumentException(SR.XmlInternalError);
            }

            if (xmlMapping is XmlTypeMapping || xmlMapping is XmlMembersMapping)
            {
global::AutoRepairTracing.TraceLogger.Log("d95d501b-65dd-49a8-80db-093c01feeb92", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 31, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("3e4f12a3-9a70-4778-ac13-6b787808f151", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 39, "Variable Update", _mapping = xmlMapping, "_mapping");
            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("4ac6e30b-423d-4a8b-969b-e92198f7f8cf", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 35, "Else Branch");
                throw new ArgumentException(SR.XmlInternalError);
            }
        }

        protected override void InitCallbacks()
{global::AutoRepairTracing.TraceLogger.Log("85721f79-7074-4ba8-8d4f-ca5879cd5ecb", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 40, "Enter Method 'InitCallbacks'");
try        {
            TypeScope scope = _mapping.Scope!;
global::AutoRepairTracing.TraceLogger.Log("95537008-3b9d-4d58-97d9-1d0b8e163a86", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 42, "Variable Update", scope, "scope");
            foreach (TypeMapping mapping in scope.TypeMappings)
            {
global::AutoRepairTracing.TraceLogger.Log("a5846d53-4665-461d-8056-9d516e9a9032", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 43, "ForEach Loop");
                if (mapping.IsSoap &&
                    (mapping is StructMapping || mapping is EnumMapping) &&
                    !mapping.TypeDesc!.IsRoot)
                {
global::AutoRepairTracing.TraceLogger.Log("2dd034f0-2e1c-42c6-a531-d926e9ca8f7c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 48, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("5d024faa-c4f7-40ae-b1cd-b86e8e61660c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 49, "AddWriteCallback", 0, mapping.TypeDesc.Type!, "mapping.TypeDesc.Type!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("cdd8c374-8474-478e-adb9-54f790063572", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 49, "AddWriteCallback", 1, mapping.TypeName!, "mapping.TypeName!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("665da0a3-61d8-46b6-810d-06aabe084ba6", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 49, "AddWriteCallback", 2, mapping.Namespace, "mapping.Namespace");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("7668d8e7-d483-41d5-86a8-f948098f8969", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 49, "AddWriteCallback", 3, CreateXmlSerializationWriteCallback(mapping, mapping.TypeName!, mapping.Namespace, mapping.TypeDesc.IsNullable), "CreateXmlSerializationWriteCallback(mapping, mapping.TypeName!, mapping.Namespace, mapping.TypeDesc.IsNullable)");
                    AddWriteCallback(
                        mapping.TypeDesc.Type!,
                        mapping.TypeName!,
                        mapping.Namespace,
                        CreateXmlSerializationWriteCallback(mapping, mapping.TypeName!, mapping.Namespace, mapping.TypeDesc.IsNullable)
                    );
}                }
            }
        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("58867de5-0be0-49c6-bb87-d7f0863542a5", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 40, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("06ab4536-5b9d-4c3d-ad0e-54b135cf532d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 58, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("d5462e3b-4ca4-428c-943e-201ae2db564d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 40, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("ce83af1e-db16-4e7c-acdb-c0b4851a4475", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 40, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("f417aa15-3616-4fdb-aec8-487beaa03a74", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 40, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        public void WriteObject(object? o)
{global::AutoRepairTracing.TraceLogger.Log("6b257af3-222d-4101-9ae6-d350011b26c7", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 59, "Enter Method 'WriteObject'");
try        {
            XmlMapping xmlMapping = _mapping;
global::AutoRepairTracing.TraceLogger.Log("58627b5e-12d5-4f90-9db9-7ca5c745092f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 61, "Variable Update", xmlMapping, "xmlMapping");
            if (xmlMapping is XmlTypeMapping xmlTypeMapping)
            {
global::AutoRepairTracing.TraceLogger.Log("565460dd-0285-4a0b-b710-6016347964e5", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 63, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("8544f93f-ae30-46c5-aa24-93bdd46e8573", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 64, "WriteObjectOfTypeElement", 0, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("a0638eb6-2b56-4ebb-827d-8ca120670410", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 64, "WriteObjectOfTypeElement", 1, xmlTypeMapping, "xmlTypeMapping");
                WriteObjectOfTypeElement(o, xmlTypeMapping);
}            }
            else {global::AutoRepairTracing.TraceLogger.Log("c6b2ce31-8041-4d92-8107-5aa3422db798", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 66, "Else Branch");
if (xmlMapping is XmlMembersMapping xmlMembersMapping)
            {
global::AutoRepairTracing.TraceLogger.Log("2b21da8f-5b0d-4b48-9506-66a0f5559597", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 67, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("a638e0c2-f67e-4fd4-bce5-2e54704aceba", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 68, "GenerateMembersElement", 0, o!, "o!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("0376cb4d-6160-451f-858a-501389a69bbf", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 68, "GenerateMembersElement", 1, xmlMembersMapping, "xmlMembersMapping");
                GenerateMembersElement(o!, xmlMembersMapping);
}            }
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("383e7456-2e29-4d95-b52e-b6f60c6a6660", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 59, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("10d13248-c4c7-406b-bbcc-103c5fd19760", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 72, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("1fda72e9-5d92-42da-8e43-cc110aa3f0e0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 59, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("c845e521-2062-42a6-ac3f-ef01e6a48fbc", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 59, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("ae64111a-213b-4bd7-9701-8d15aa2e6f5b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 59, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private void WriteObjectOfTypeElement(object? o, XmlTypeMapping mapping)
{global::AutoRepairTracing.TraceLogger.Log("5017049d-9b82-4d86-a619-3b5f43ea26c1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 72, "Enter Method 'WriteObjectOfTypeElement'");
try        {
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("ac21110c-3f83-4ec5-996a-d64e33ab561a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 74, "GenerateTypeElement", 0, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("b01a9c8d-0e83-4d28-aa70-143bbfcb7289", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 74, "GenerateTypeElement", 1, mapping, "mapping");
            GenerateTypeElement(o, mapping);
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("ff7ae545-57ad-4113-9d74-3d5588a62ffe", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 72, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("ee003a96-115c-4a5e-bf09-3587cd7c881e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 78, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("95024dd7-ac1a-4302-b392-6d3f4523b939", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 72, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("cc805c03-d058-4ce5-9d0c-a285590aa5cc", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 72, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("95053a9b-b63c-43a2-a823-f55abbaca7a0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 72, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private void GenerateTypeElement(object? o, XmlTypeMapping xmlMapping)
{global::AutoRepairTracing.TraceLogger.Log("ffd0777a-7f65-41fa-a36b-8fa5644ce2e7", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 77, "Enter Method 'GenerateTypeElement'");
try        {
            ElementAccessor element = xmlMapping.Accessor;
global::AutoRepairTracing.TraceLogger.Log("6497e466-a80e-4aa6-9a05-ed66b609d057", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 79, "Variable Update", element, "element");
            TypeMapping mapping = element.Mapping!;
global::AutoRepairTracing.TraceLogger.Log("f5753b6c-f426-4c29-bf4c-96160c3ae3bc", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 80, "Variable Update", mapping, "mapping");

            WriteStartDocument();
            if (o == null)
            {
global::AutoRepairTracing.TraceLogger.Log("dc234591-ba9e-4780-8a09-3c92c276ee2c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 84, "If-True Branch");
                string? ns = (element.Form == XmlSchemaForm.Qualified ? element.Namespace : string.Empty);
global::AutoRepairTracing.TraceLogger.Log("b86ccdb1-2c26-4569-8168-8c5235165612", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 85, "Variable Update", ns, "ns");
                if (element.IsNullable)
                {
global::AutoRepairTracing.TraceLogger.Log("5af60c56-1635-4aa6-bc06-173f5026f274", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 87, "If-True Branch");
                    if (mapping.IsSoap)
                    {
global::AutoRepairTracing.TraceLogger.Log("5673f0c2-4f09-45b4-9de6-54886870e047", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 89, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("2dcb2966-fc0f-4729-9152-48a2825b2f45", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 90, "WriteNullTagEncoded", 0, element.Name, "element.Name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("086580b1-66ac-48a8-a209-fbfb3e5342c5", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 90, "WriteNullTagEncoded", 1, ns, "ns");
                        WriteNullTagEncoded(element.Name, ns);
}                    }
                    else
                    {
global::AutoRepairTracing.TraceLogger.Log("289b3aae-ebc8-4f45-896d-f0c86d7d706f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 93, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("2be1fc20-4f23-4e40-8fec-7fc7728b0adf", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 94, "WriteNullTagLiteral", 0, element.Name, "element.Name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("382e11df-b58d-4f21-8fc1-1cf6dd313cd4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 94, "WriteNullTagLiteral", 1, ns, "ns");
                        WriteNullTagLiteral(element.Name, ns);
}                    }
                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("f04c4a6d-f51a-4046-a79d-d44295119319", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 98, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("63fedc28-4ccc-4930-9a44-4b21a88450c7", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 99, "WriteEmptyTag", 0, element.Name, "element.Name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("f53ca451-f406-442b-9559-d7f671351b10", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 99, "WriteEmptyTag", 1, ns, "ns");
                    WriteEmptyTag(element.Name, ns);
}                }
{global::AutoRepairTracing.TraceLogger.Log("9613e75e-24f1-4b60-b076-6f324276f711", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 102, "Return");

                return;
}            }

            if (!mapping.TypeDesc!.IsValueType && !mapping.TypeDesc.Type!.IsPrimitive)
            {
global::AutoRepairTracing.TraceLogger.Log("208ad86f-d36f-40fe-a74a-9405361a71ea", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 106, "If-True Branch");
                TopLevelElement();
            }
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("e2f65d50-7f2d-402e-b445-afa6560fe31e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 110, "WriteMember", 0, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("b690de5e-5471-4654-957d-a256f35723ce", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 110, "WriteMember", 1, null, "null");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("e1110191-641b-4457-8ee2-15b0fa69d717", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 110, "WriteMember", 2, new ElementAccessor[] { element }, "new ElementAccessor[] { element }");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("970aa1a1-df67-4545-b891-ccbd826ff31b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 110, "WriteMember", 3, null, "null");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("dcaa559e-209a-447e-9225-78d88687a0bd", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 110, "WriteMember", 4, null, "null");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("dc001219-ddfd-442d-8d55-78646fe3fb03", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 110, "WriteMember", 5, mapping.TypeDesc, "mapping.TypeDesc");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("8e6777b2-0282-43c1-903e-383b3b70847c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 110, "WriteMember", 6, !element.IsSoap, "!element.IsSoap");

            WriteMember(o, null, new ElementAccessor[] { element }, null, null, mapping.TypeDesc, !element.IsSoap);
}            if (mapping.IsSoap)
            {
global::AutoRepairTracing.TraceLogger.Log("c4ebcaf4-0350-4b81-9106-0166f8227d31", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 112, "If-True Branch");
                WriteReferencedElements();
            }
        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("0c7c4df1-5469-4541-a2a0-17299516cdfd", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 77, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("9a501630-088a-48d0-975d-057d98c3b4b3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 119, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("e3620549-18b5-480e-9876-b94c5f969f13", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 77, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("093a6ed4-1896-4d9f-b4f9-0b46d0c762d8", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 77, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("52794c2c-487e-44e0-9784-7ba742769c36", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 77, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private void WriteMember(object? o, object? choiceSource, ElementAccessor[] elements, TextAccessor? text, ChoiceIdentifierAccessor? choice, TypeDesc memberTypeDesc, bool writeAccessors)
{global::AutoRepairTracing.TraceLogger.Log("37e702d4-14a1-4c5a-a5f7-b78928967f8b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 117, "Enter Method 'WriteMember'");
global::AutoRepairTracing.TraceLogger.LogArray("8c8d4175-16ed-40f4-b4c7-71a08f03ae7c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 117, "Array Parameter", elements, "WriteMember#arg2:elements");
global::AutoRepairTracing.TraceLogger.LogArrayAliasParameter("80da9822-b579-4e6c-8745-0859b3fc4352", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 117, "WriteMember", 2, elements, "elements");
try        {
            if (memberTypeDesc.IsArrayLike &&
                !(elements.Length == 1 && elements[0].Mapping is ArrayMapping))
            {
global::AutoRepairTracing.TraceLogger.Log("1bcce6fa-e771-47ae-bce8-1a1ef9f210bb", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 121, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("748bacb5-cc3c-4be4-bcf1-a9bba5dd0769", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 122, "WriteArray", 0, o!, "o!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("e0a9a287-feee-43b5-9b12-1ef1a0f0c3d1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 122, "WriteArray", 1, choiceSource, "choiceSource");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("68926d82-dee8-43c7-8d2c-19581f4808cf", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 122, "WriteArray", 2, elements, "elements");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("b1a51a2b-501d-4021-98f8-ca1dc93a54db", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 122, "WriteArray", 3, text, "text");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("4a63527f-d868-44e2-b8c1-92ba0dffb74d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 122, "WriteArray", 4, choice, "choice");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d49ad79b-f46c-4b98-af3b-4ccdbd85e502", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 122, "WriteArray", 5, memberTypeDesc, "memberTypeDesc");
                WriteArray(o!, choiceSource, elements, text, choice, memberTypeDesc);
}            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("60e982d1-fb04-4ba9-aa59-7f35755fb736", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 125, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("6ef36a22-4334-4191-8927-024adb02dcbb", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 126, "WriteElements", 0, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("92efe93c-7d27-4c31-88c9-bccce771132f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 126, "WriteElements", 1, choiceSource, "choiceSource");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("f081c4b3-15d7-4524-acfb-2d028886cda0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 126, "WriteElements", 2, elements, "elements");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("50901ac0-6fcc-4c7f-8766-d7c836cd9e7f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 126, "WriteElements", 3, text, "text");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("3c0d7b90-6c30-42b5-8cf0-0b399c0e46c7", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 126, "WriteElements", 4, choice, "choice");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("5ee27b56-9c5f-4e90-befa-94dca3ed1a6c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 126, "WriteElements", 5, writeAccessors, "writeAccessors");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("06732380-ffff-4861-9f03-8292344339c8", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 126, "WriteElements", 6, memberTypeDesc.IsNullable, "memberTypeDesc.IsNullable");
                WriteElements(o, choiceSource, elements, text, choice, writeAccessors, memberTypeDesc.IsNullable);
}            }
        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("7c585a77-754c-4669-9e3d-d01fc3d46983", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 117, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("1571febf-cb15-4b55-b737-868ae6e3c777", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 133, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("c24d8718-f68d-4b64-b220-8384400c87da", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 117, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("af23117d-0b2e-42c7-891c-0765b09f4a39", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 117, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("3a20d437-26de-462b-a20e-70893a1a6145", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 117, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private void WriteArray(object o, object? choiceSource, ElementAccessor[] elements, TextAccessor? text, ChoiceIdentifierAccessor? choice, TypeDesc arrayTypeDesc)
{global::AutoRepairTracing.TraceLogger.Log("7559ebe0-595e-40cf-b3b9-b955da363b4c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 130, "Enter Method 'WriteArray'");
global::AutoRepairTracing.TraceLogger.LogArray("e343550a-0319-4934-b300-93a67c8f484e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 130, "Array Parameter", elements, "WriteArray#arg2:elements");
global::AutoRepairTracing.TraceLogger.LogArrayAliasParameter("a77142c1-1492-4468-a4cb-4803528075fa", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 130, "WriteArray", 2, elements, "elements");
try        {
            if (elements.Length == 0 && text == null)
            {
global::AutoRepairTracing.TraceLogger.Log("4466e3fc-e517-4557-bce2-15a3ac2b8fc1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 133, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("4ca7bcd7-e5d5-4647-8e04-3f8d43654bde", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 134, "Return");
                return;
}            }

            if (arrayTypeDesc.IsNullable && o == null)
            {
global::AutoRepairTracing.TraceLogger.Log("46430807-a6e9-4186-bc2c-a9b727dd063e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 138, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("ab731008-6e87-4ca2-a2a6-d7bce348729f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 139, "Return");
                return;
}            }

            if (choice != null)
            {
global::AutoRepairTracing.TraceLogger.Log("a1982e3a-a0a8-412b-9bcb-c729b6f4e3c3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 143, "If-True Branch");
                if (choiceSource == null || ((Array)global::AutoRepairTracing.TraceLogger.LogCastSource("a6405fa4-91ef-4148-8564-fb2e1b796c2e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 233, (object)(choiceSource), "Array")).Length < ((Array)global::AutoRepairTracing.TraceLogger.LogCastSource("4324866d-3c9f-4b92-8cf8-cb8cbe37c6df", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 233, (object)(o), "Array")).Length)
                {
global::AutoRepairTracing.TraceLogger.Log("e549159a-0f3d-4525-9b4b-720b81c2ca19", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 145, "If-True Branch");
                    throw CreateInvalidChoiceIdentifierValueException(choice.Mapping!.TypeDesc!.FullName, choice.MemberName!);
                }
            }
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("2f3d1be9-977c-4aa1-8a5e-8f050b851341", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 150, "WriteArrayItems", 0, elements, "elements");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("3e563185-3671-4670-a128-3f2c4be88669", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 150, "WriteArrayItems", 1, text, "text");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("940e97a5-40a7-4238-bb6b-b6ed2ea66655", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 150, "WriteArrayItems", 2, choice, "choice");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("5e798728-97cc-421a-bc42-a94270601cb7", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 150, "WriteArrayItems", 3, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("08c24bdf-ddd3-4a64-bede-ab1c3a305040", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 150, "WriteArrayItems", 4, choiceSource, "choiceSource");

            WriteArrayItems(elements, text, choice, o, choiceSource);
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("8608f5f8-12ca-43d0-ac67-a9c518042fa3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 130, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("af1109e9-e3e7-4891-a00f-ee4f2e6da875", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 157, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("d27857d1-56c5-45ae-bf2e-09416f255856", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 130, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("1bb116d3-9938-4a14-b78c-e73136b000e3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 130, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("e1c6a7b4-a4cc-4e70-8436-4176a425b22b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 130, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private void WriteArrayItems(ElementAccessor[] elements, TextAccessor? text, ChoiceIdentifierAccessor? choice, object o, object? choiceSources)
{global::AutoRepairTracing.TraceLogger.Log("d666e236-ea2f-4004-aef8-a27f93fd3f6c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 153, "Enter Method 'WriteArrayItems'");
global::AutoRepairTracing.TraceLogger.LogArray("300af10a-7080-43f5-bbd0-57b04d88c0f7", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 153, "Array Parameter", elements, "WriteArrayItems#arg0:elements");
global::AutoRepairTracing.TraceLogger.LogArrayAliasParameter("5c607fdf-51d9-4b64-8a5b-c9b19d09cfbe", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 153, "WriteArrayItems", 0, elements, "elements");
try        {
            var arr = global::AutoRepairTracing.TraceLogger.LogAsCast<IList>("7d1e3810-736f-4a59-8940-b7217ed04fb0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 255, (object)(o), "IList");
global::AutoRepairTracing.TraceLogger.Log("9b0cda62-c749-4fd4-9c83-74c7880bcae1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 155, "Variable Update", arr, "arr");

            if (arr != null)
            {
global::AutoRepairTracing.TraceLogger.Log("05dc3562-5053-4d64-bae3-763ada4d6858", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 158, "If-True Branch");
{int __loopCounter_02da27cb = 0;global::AutoRepairTracing.TraceLogger.Log("b3955444-7b51-4988-8117-60067a8ea4c0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 255, "Variable Update", __loopCounter_02da27cb, "__loopCounter_02da27cb");
                for (int i = 0; i < arr.Count; global::AutoRepairTracing.TraceLogger.LogValue("094cccd6-ef4c-48d9-a037-0f77c871111d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 255, "Variable Update", i++, "i"))
                {
global::AutoRepairTracing.TraceLogger.Log("d1d48d39-9209-4694-a20c-1fd8a6b4d567", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 159, "For Loop");
global::AutoRepairTracing.TraceLogger.LogValue("88caa9c6-522e-4b4a-a544-a4f03826dcd1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 258, "Variable Update", __loopCounter_02da27cb++, "__loopCounter_02da27cb");if(__loopCounter_02da27cb > 10000){global::AutoRepairTracing.TraceLogger.Log("ebfbd859-ca35-4661-8e07-47aaa57bff99", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 167, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("02da27cb-0c70-4e57-a516-b800217a68c0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 159, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_02da27cb, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("d3b5677d-bacb-41fc-9486-864f6735a167", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 167, "Break");
break;}}                    object? ai = arr[i];
global::AutoRepairTracing.TraceLogger.Log("a72b39c5-4c49-4a65-9885-cbc95cbc5126", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 161, "Variable Update", ai, "ai");
                    var choiceSource = ((Array?)global::AutoRepairTracing.TraceLogger.LogCastSource("189fd049-c26c-4954-8fa0-48bae21f99e9", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 269, (object)(choiceSources), "Array?"))?.GetValue(i);
global::AutoRepairTracing.TraceLogger.Log("396b604b-4ea6-4c19-a5ae-06b0b3e51f42", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 162, "Variable Update", choiceSource, "choiceSource");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("8ee94976-64e6-4f68-be80-8fac7d2126ea", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 163, "WriteElements", 0, ai, "ai");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("e97a9a2f-28cc-4ede-af66-9f32b9877447", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 163, "WriteElements", 1, choiceSource, "choiceSource");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("057623ab-988c-41ce-add6-92e713dd1b2f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 163, "WriteElements", 2, elements, "elements");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("6503b55e-18ff-4426-911f-20393cf4f908", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 163, "WriteElements", 3, text, "text");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("7500a9fa-fc0d-4fea-960d-414a6dc99b0f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 163, "WriteElements", 4, choice, "choice");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("de4bf62a-1ec4-40f2-a397-05f5c0122a80", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 163, "WriteElements", 5, true, "true");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("e5d71d0d-d3e3-44e7-ba84-8854d0d41d05", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 163, "WriteElements", 6, true, "true");
                    WriteElements(ai, choiceSource, elements, text, choice, true, true);
}                }
}            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("e7938da8-9ff8-42fb-8084-312bd435327c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 167, "Else Branch");
                var a = global::AutoRepairTracing.TraceLogger.LogAsCast<IEnumerable>("76eac37f-7d7e-4857-bbfc-2694c0c85897", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 284, (object)(o), "IEnumerable");
global::AutoRepairTracing.TraceLogger.Log("18b60643-0af3-4bf8-b173-e0b6ad64f1b3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 168, "Variable Update", a, "a");
                Debug.Assert(a != null);

                IEnumerator e = a.GetEnumerator();
global::AutoRepairTracing.TraceLogger.Log("00d1d5b8-9f32-4764-9716-dc4fd95b0d06", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 171, "Variable Update", e, "e");
                if (e != null)
                {
global::AutoRepairTracing.TraceLogger.Log("a2b7b96d-2116-4f54-9620-f862e6164d53", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 173, "If-True Branch");
                    int c = 0;
global::AutoRepairTracing.TraceLogger.Log("e6f6f80e-3add-4fd2-bcc8-2c1d3c9375b3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 174, "Variable Update", c, "c");
{int __loopCounter_1e2c1973 = 0;global::AutoRepairTracing.TraceLogger.Log("3b9a5b1c-2f10-4669-9e55-6ba94d76881f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 283, "Variable Update", __loopCounter_1e2c1973, "__loopCounter_1e2c1973");
                    while (e.MoveNext())
                    {
global::AutoRepairTracing.TraceLogger.Log("44398a98-7d4e-4c07-bafd-dc47f2a44235", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 175, "While Loop");
global::AutoRepairTracing.TraceLogger.LogValue("40a16fa9-e63e-4615-89c0-63e84fb0dd1b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 286, "Variable Update", __loopCounter_1e2c1973++, "__loopCounter_1e2c1973");if(__loopCounter_1e2c1973 > 10000){global::AutoRepairTracing.TraceLogger.Log("8b8f6898-3e85-44d8-9afe-9b4da5dae346", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 183, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("1e2c1973-ca48-454e-ba27-d90d58e6b59d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 175, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_1e2c1973, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("0e3c24f0-a66f-4ce5-9ccb-600a0cc2d7a0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 183, "Break");
break;}}                        object ai = e.Current;
global::AutoRepairTracing.TraceLogger.Log("acb8b014-f1e8-4e41-9369-60471cbc5f50", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 177, "Variable Update", ai, "ai");
                        var choiceSource = ((Array?)global::AutoRepairTracing.TraceLogger.LogCastSource("cadff1be-dd6d-472b-add2-c11ef2034b00", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 303, (object)(choiceSources), "Array?"))?.GetValue(global::AutoRepairTracing.TraceLogger.LogValue("eaa672e2-7c46-400e-984a-c96ec6ed2918", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 289, "Variable Update", c++, "c"));
global::AutoRepairTracing.TraceLogger.Log("afcc34e0-c624-4b92-bd9c-d8a525dc73b1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 178, "Variable Update", choiceSource, "choiceSource");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("fcdb5be3-838d-491a-9cc1-33d53c37ced9", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 179, "WriteElements", 0, ai, "ai");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("ac1a5f01-3d81-4fa5-bc53-2823635efd4d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 179, "WriteElements", 1, choiceSource, "choiceSource");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("92a99134-994b-4f1d-9602-c5e4e5e58f20", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 179, "WriteElements", 2, elements, "elements");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("ecdfb7c3-25c5-4898-a737-1d574a2fc2ca", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 179, "WriteElements", 3, text, "text");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("30af4d10-622e-4d94-a42a-2160863248d6", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 179, "WriteElements", 4, choice, "choice");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("4015e730-7b63-4780-bb42-c5d3d486dbc0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 179, "WriteElements", 5, true, "true");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("6612df66-5419-46a8-8a9f-eab1a6aba370", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 179, "WriteElements", 6, true, "true");
                        WriteElements(ai, choiceSource, elements, text, choice, true, true);
}                    }
}                }
            }
        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("0967e665-0992-4412-a9cf-0d85b462cc34", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 153, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("abf80fda-3b4c-4618-a627-e093e168da5c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 190, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("a9d26813-ea42-425c-9e52-6d6b5a945a2b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 153, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("e1de4556-69c4-46ab-ab78-4453c612ef26", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 153, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("76243785-a880-430b-894e-e3c84d973eeb", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 153, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private void WriteElements(object? o, object? choiceSource, ElementAccessor[] elements, TextAccessor? text, ChoiceIdentifierAccessor? choice, bool writeAccessors, bool isNullable)
{global::AutoRepairTracing.TraceLogger.Log("6210a10a-0636-4b10-b90f-c41b7045475c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 185, "Enter Method 'WriteElements'");
global::AutoRepairTracing.TraceLogger.LogArray("5fbf8f38-dfa3-40ac-8f2d-146317bea647", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 185, "Array Parameter", elements, "WriteElements#arg2:elements");
global::AutoRepairTracing.TraceLogger.LogArrayAliasParameter("2e4d2971-bfe2-4364-845e-e71b7920d648", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 185, "WriteElements", 2, elements, "elements");
try        {
            if (elements.Length == 0 && text == null)
{global::AutoRepairTracing.TraceLogger.Log("64a71567-cb7a-4a56-8249-702b6a45d88a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 188, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("79e13d1a-5a8a-4ad5-b973-206dc3ff7131", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 188, "Return");
                return;
}
            if (elements.Length == 1 && text == null)
            {
global::AutoRepairTracing.TraceLogger.Log("dc9d1368-e9d7-4f55-a522-71b815a47a72", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 191, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("90036981-b0ec-4bd3-a177-6d69f06550fa", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 192, "WriteElement", 0, o, "o");
{global::AutoRepairTracing.TraceLogger.Log("4fe77472-0e3f-4ff0-8a23-573df28ec35b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 319, "Variable Update", 0, "index:elements");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("bf70b78e-bc69-4c7a-af12-e4ac696cea51", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 192, "WriteElement", 1, elements[0], "elements[0]");
}global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("50ae15f2-bf7e-4751-aa79-8b452d53171a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 192, "WriteElement", 2, writeAccessors, "writeAccessors");
{global::AutoRepairTracing.TraceLogger.Log("6d469e9d-7f0f-4432-bdf1-2f30393fa4ce", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 192, "Variable Update", 0, "index:elements");
                WriteElement(o, elements[0], writeAccessors);
}}            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("05eab505-2541-4317-b7e9-bdfa7ddb52c3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 195, "Else Branch");
                if (isNullable && choice == null && o == null)
                {
global::AutoRepairTracing.TraceLogger.Log("9e304c2d-fe9c-4b7f-bc0f-93469d190172", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 197, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("6f60a58e-33ba-433e-86a0-4772617158ba", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 198, "Return");
                    return;
}                }

                int anyCount = 0;
global::AutoRepairTracing.TraceLogger.Log("3459122e-b9c2-4229-b984-289e0f12962f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 201, "Variable Update", anyCount, "anyCount");
                var namedAnys = new List<ElementAccessor>();
global::AutoRepairTracing.TraceLogger.Log("eeaca451-5b92-4278-af6b-d1d0df26dd5b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 202, "Variable Update", namedAnys, "namedAnys");
                ElementAccessor? unnamedAny = null; // can only have one
global::AutoRepairTracing.TraceLogger.Log("c1b6cc97-1c57-449d-9d35-166f3a59f44c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 203, "Variable Update", unnamedAny, "unnamedAny");
global::AutoRepairTracing.TraceLogger.Log("3a8a62b3-e28b-4e38-b01d-09c645815c2d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 203, "Null Assignment Warning", "null", "unnamedAny");{int __loopCounter_6b1d7bcb = 0;global::AutoRepairTracing.TraceLogger.Log("c0fbe0dc-dea5-41e7-bc21-7f110ccec01c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 336, "Variable Update", __loopCounter_6b1d7bcb, "__loopCounter_6b1d7bcb");

                for (int i = 0; i < elements.Length; global::AutoRepairTracing.TraceLogger.LogValue("ab84b3b3-667e-4b0c-b9a0-b18ec53b83e5", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 337, "Variable Update", i++, "i"))
                {
global::AutoRepairTracing.TraceLogger.Log("59857387-e6b7-48d1-bdbd-54e4aed8616b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 205, "For Loop");
global::AutoRepairTracing.TraceLogger.LogValue("aad37eed-253d-4d63-b2d0-d28a0bab1f66", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 340, "Variable Update", __loopCounter_6b1d7bcb++, "__loopCounter_6b1d7bcb");if(__loopCounter_6b1d7bcb > 10000){global::AutoRepairTracing.TraceLogger.Log("939ff351-85bf-4ca5-a852-2028baad4c58", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 214, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("6b1d7bcb-0525-4930-8ec0-d663151e9f80", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 205, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_6b1d7bcb, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("829a53a4-c398-40af-9734-740fbdd7ae47", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 214, "Break");
break;}}                    ElementAccessor element = elements[i];
global::AutoRepairTracing.TraceLogger.Log("dbb2153a-04f8-4e84-afde-e58c6e30601d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 207, "Variable Update", element, "element");

                    if (element.Any)
                    {
global::AutoRepairTracing.TraceLogger.Log("26ed813f-277d-4ebf-bd6a-dfdab92285b3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 210, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("f03c9836-f335-4059-8e50-5e063c83ae1c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 347, "Variable Update", anyCount++, "anyCount");
                        if (element.Name != null && element.Name.Length > 0)
                        {
global::AutoRepairTracing.TraceLogger.Log("17f07874-9a3d-4bab-89e6-782952da3d02", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 213, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("e4f10ae9-b75f-4f22-8fa3-97b7fbcfc186", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 214, "Add", 0, element, "element");
                            namedAnys.Add(element);
}                        }
                        else
                        {
global::AutoRepairTracing.TraceLogger.Log("7e3c6eec-2144-4429-b28c-4ac79a8df91c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 217, "Else Branch");
global::AutoRepairTracing.TraceLogger.LogValue("0a9ea752-88dc-4bbe-9917-8b9115f73131", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 357, "Variable Update", unnamedAny ??= element, "unnamedAny");
                        }
                    }
                    else {global::AutoRepairTracing.TraceLogger.Log("3b7e1943-1d26-4796-822e-3e68350b8440", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 221, "Else Branch");
if (choice != null)
                    {
global::AutoRepairTracing.TraceLogger.Log("16af6bd3-ea80-44a6-88ec-790a5579215a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 222, "If-True Branch");
                        // This looks heavy - getting names of enums in string form for comparison rather than just comparing values.
                        // But this faithfully mimics NetFx, and is necessary to prevent confusion between different enum types.
                        // ie EnumType.ValueX could == 1, but TotallyDifferentEnumType.ValueY could also == 1.
                        TypeDesc td = element.Mapping!.TypeDesc!;
global::AutoRepairTracing.TraceLogger.Log("7df162dd-b182-466a-b6a2-38d511bd6aa4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 226, "Variable Update", td, "td");
                        bool enumUseReflection = choice.Mapping!.TypeDesc!.UseReflection;
global::AutoRepairTracing.TraceLogger.Log("f209fae6-df54-4450-a418-9c8cbe1f3f1b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 227, "Variable Update", enumUseReflection, "enumUseReflection");
                        string enumTypeName = choice.Mapping!.TypeDesc!.FullName;
global::AutoRepairTracing.TraceLogger.Log("d540e366-d315-4a87-8e2c-3b337abb999b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 228, "Variable Update", enumTypeName, "enumTypeName");
                        string enumFullName = (enumUseReflection ? "" : enumTypeName + ".@") + FindChoiceEnumValue(element, (EnumMapping)global::AutoRepairTracing.TraceLogger.LogCastSource("38bda084-d394-43f2-ab24-43d5f1106017", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 395, (object)(choice.Mapping), "EnumMapping"), enumUseReflection);
global::AutoRepairTracing.TraceLogger.Log("1c85d59f-e3d2-4efb-ae2d-07ac333088cb", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 229, "Variable Update", enumFullName, "enumFullName");
                        string choiceFullName = (enumUseReflection ? "" : choiceSource!.GetType().FullName + ".@") + choiceSource!.ToString();
global::AutoRepairTracing.TraceLogger.Log("7525615f-7c7f-47ee-8c0f-dd15c4febd60", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 230, "Variable Update", choiceFullName, "choiceFullName");

                        if (choiceFullName == enumFullName)
                        {
global::AutoRepairTracing.TraceLogger.Log("a733c2dc-aa28-4ad9-b1d8-a91e346904d4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 233, "If-True Branch");
                            // Object is either non-null, or it is allowed to be null
                            if (o != null || (!isNullable || element.IsNullable))
                            {
global::AutoRepairTracing.TraceLogger.Log("f15070e7-a8f0-4480-8b57-2d72a9c0b46d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 236, "If-True Branch");
                                // But if Object is non-null, it's got to match types
                                if (o != null && !td.Type!.IsAssignableFrom(o!.GetType()))
                                {
global::AutoRepairTracing.TraceLogger.Log("b15ca771-780e-4fb7-aae4-fd78dc910be8", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 239, "If-True Branch");
                                    throw CreateMismatchChoiceException(td.FullName, choice.MemberName!, enumFullName);
                                }
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("59b567fd-af4f-4808-8eac-55aefd573ec1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 243, "WriteElement", 0, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("fc0c2b89-4d39-42c7-be9e-3a17819d4138", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 243, "WriteElement", 1, element, "element");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("e7db0a32-2a75-4f37-b35e-759d8408f5ed", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 243, "WriteElement", 2, writeAccessors, "writeAccessors");

                                WriteElement(o, element, writeAccessors);
}{global::AutoRepairTracing.TraceLogger.Log("7fb9eeed-6b5c-4295-bc61-a464658adfcc", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 244, "Return");
                                return;
}                            }
                        }
                    }
                    else
                    {
global::AutoRepairTracing.TraceLogger.Log("fd7ac98a-0a49-4173-ad38-281f1e048427", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 249, "Else Branch");
                        TypeDesc td = element.IsUnbounded ? element.Mapping!.TypeDesc!.CreateArrayTypeDesc() : element.Mapping!.TypeDesc!;
global::AutoRepairTracing.TraceLogger.Log("c9b7596d-58f9-481c-81f5-1328f90c349d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 250, "Variable Update", td, "td");
                        if (td.Type!.IsAssignableFrom(o!.GetType()))
                        {
global::AutoRepairTracing.TraceLogger.Log("4f967dc1-5266-4c0b-8d87-f0d84c97bd4a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 252, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("91952152-55c4-4b41-936d-46ec5053be5f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 253, "WriteElement", 0, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("20f1a32a-3a07-40fa-be89-9e48b8d5f22b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 253, "WriteElement", 1, element, "element");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("4043b423-f3bc-408d-a41e-36a7ea483ffa", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 253, "WriteElement", 2, writeAccessors, "writeAccessors");
                            WriteElement(o, element, writeAccessors);
}{global::AutoRepairTracing.TraceLogger.Log("3f6f6306-a4a2-46f0-aab0-7148c15c654c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 254, "Return");
                            return;
}                        }
                    }
}                }
}
                if (anyCount > 0)
                {
global::AutoRepairTracing.TraceLogger.Log("b9b0a144-3f5c-4b8f-a0cb-c7a36a0767b7", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 260, "If-True Branch");
                    if (o is XmlElement elem)
                    {
global::AutoRepairTracing.TraceLogger.Log("af2248e9-4604-43a7-bbd6-60103e794f7c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 262, "If-True Branch");
                        foreach (ElementAccessor element in namedAnys)
                        {
global::AutoRepairTracing.TraceLogger.Log("50deec2d-1bce-424b-943a-f471b0513198", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 263, "ForEach Loop");
                            if (element.Name == elem.Name && element.Namespace == elem.NamespaceURI)
                            {
global::AutoRepairTracing.TraceLogger.Log("f6d4f420-d5f2-42bb-9df0-1ab42ee5a486", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 266, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("a1baca7d-5a42-4484-bebd-773317a78f55", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 267, "WriteElement", 0, elem, "elem");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("30f307d6-7688-4dc9-b11f-7a8c666acfb5", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 267, "WriteElement", 1, element, "element");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("776a63b2-67fc-4ee3-b53e-686948b5b551", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 267, "WriteElement", 2, writeAccessors, "writeAccessors");
                                WriteElement(elem, element, writeAccessors);
}{global::AutoRepairTracing.TraceLogger.Log("fd6e7018-172a-4443-8713-7b54c3ecfbb9", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 268, "Return");
                                return;
}                            }
                        }

                        if (choice != null)
                        {
global::AutoRepairTracing.TraceLogger.Log("1c6ed297-b0d2-4d8d-8229-31bd0e01b127", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 273, "If-True Branch");
                            throw CreateChoiceIdentifierValueException(choice.Mapping!.TypeDesc!.FullName, choice.MemberName!, elem.Name, elem.NamespaceURI);
                        }

                        if (unnamedAny != null)
                        {
global::AutoRepairTracing.TraceLogger.Log("335b315f-633b-425f-a6cc-45acb29b09ee", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 278, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("23eab8d3-0a9b-4d9b-b48e-ffd46a9c6da3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 279, "WriteElement", 0, elem, "elem");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("b4d7bb87-9e23-4122-b80c-b80fb5ec9009", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 279, "WriteElement", 1, unnamedAny, "unnamedAny");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("c658a444-de10-45bb-98b4-2cda177f23dd", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 279, "WriteElement", 2, writeAccessors, "writeAccessors");
                            WriteElement(elem, unnamedAny, writeAccessors);
}{global::AutoRepairTracing.TraceLogger.Log("ba1d4c2f-f67f-4786-8141-7b66e7a721ba", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 280, "Return");
                            return;
}                        }

                        throw CreateUnknownAnyElementException(elem.Name, elem.NamespaceURI);
                    }
                }

                if (text != null)
                {
global::AutoRepairTracing.TraceLogger.Log("4dcaf88f-beb6-4468-abba-8261a0eebc04", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 288, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("12918d51-96d7-43e5-86f3-cb610ed0fac4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 289, "WriteText", 0, o!, "o!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("3557e1ef-a4fe-4c96-9755-60e2efcc7e9e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 289, "WriteText", 1, text, "text");
                    WriteText(o!, text);
}{global::AutoRepairTracing.TraceLogger.Log("0f0a25d1-4176-4ebb-ac49-2628b5072169", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 290, "Return");
                    return;
}                }

                if (elements.Length > 0 && o != null)
                {
global::AutoRepairTracing.TraceLogger.Log("2248c642-e160-4ca8-890e-867112864df0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 294, "If-True Branch");
                    throw CreateUnknownTypeException(o);
                }
            }
        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("1b12ddf4-5e5b-446b-af46-d78ec044c22e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 185, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("d96997b8-0c4b-498e-b55f-fe3f1539cd82", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 306, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("a6ffd4dc-5cd7-4a5a-b59e-704f96235bce", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 185, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("d49faf53-958c-4c08-bc0b-cc779df9a660", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 185, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("b4215bdf-1f30-4953-be99-bf67fbd608fc", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 185, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private static string FindChoiceEnumValue(ElementAccessor element, EnumMapping choiceMapping, bool useReflection)
{global::AutoRepairTracing.TraceLogger.Log("734c66b9-e021-4a1f-93b8-269c2afdb369", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 300, "Enter Method 'FindChoiceEnumValue'");
try        {
            string? enumValue = null;
global::AutoRepairTracing.TraceLogger.Log("d728daad-e2eb-442e-b19f-642ec25f9ee3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 302, "Variable Update", enumValue, "enumValue");
global::AutoRepairTracing.TraceLogger.Log("08f20a3d-3bb4-4c76-a29e-bfab5628832f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 302, "Null Assignment Warning", "null", "enumValue");{int __loopCounter_877417b3 = 0;global::AutoRepairTracing.TraceLogger.Log("8197120e-e431-42bc-b47f-de907963c6a8", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 479, "Variable Update", __loopCounter_877417b3, "__loopCounter_877417b3");

            for (int i = 0; i < choiceMapping.Constants!.Length; global::AutoRepairTracing.TraceLogger.LogValue("3f8e77af-0342-4b2d-bf10-8b30cdc49484", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 480, "Variable Update", i++, "i"))
            {
global::AutoRepairTracing.TraceLogger.Log("4935ccd2-81f1-41e2-9fdc-e152fc3b0c13", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 304, "For Loop");
global::AutoRepairTracing.TraceLogger.LogValue("24549e9b-ef14-442d-bf6e-6b836245c1ee", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 483, "Variable Update", __loopCounter_877417b3++, "__loopCounter_877417b3");if(__loopCounter_877417b3 > 10000){global::AutoRepairTracing.TraceLogger.Log("d8c7f01c-2274-4cd1-a49a-b583f81479f5", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 314, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("877417b3-6794-48a2-b45c-9b3396fa052f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 304, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_877417b3, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("78fe4ffa-0462-4a97-b203-e5e268e73e0d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 314, "Break");
break;}}                string xmlName = choiceMapping.Constants[i].XmlName;
global::AutoRepairTracing.TraceLogger.Log("99266010-8546-4e86-84a0-fbd0ff8611e3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 306, "Variable Update", xmlName, "xmlName");

                if (element.Any && element.Name.Length == 0)
                {
global::AutoRepairTracing.TraceLogger.Log("6b3f3546-76e5-4043-b274-eedeff881cc7", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 309, "If-True Branch");
                    if (xmlName == "##any:")
                    {
global::AutoRepairTracing.TraceLogger.Log("4affd72e-9086-49ff-aec6-470f151eb4ca", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 311, "If-True Branch");
                        if (useReflection)
{global::AutoRepairTracing.TraceLogger.Log("c577ab5e-b486-45b8-9a8f-eceae1a47af6", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 313, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("135d2db7-0517-44fb-b928-caeade17252e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 313, "Variable Update", i, "index:choiceMapping.Constants");
global::AutoRepairTracing.TraceLogger.LogValue("753de8af-1eeb-480f-99b5-df255c15312c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 495, "Variable Update", enumValue = choiceMapping.Constants[i].Value.ToString(CultureInfo.InvariantCulture), "enumValue");
}}                        else
{global::AutoRepairTracing.TraceLogger.Log("d2d1153d-5bb8-4aa6-87aa-42772593949d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 315, "Else Branch");
{global::AutoRepairTracing.TraceLogger.Log("17578481-4fa0-4783-9e25-46c28a52121a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 315, "Variable Update", i, "index:choiceMapping.Constants");
global::AutoRepairTracing.TraceLogger.LogValue("f64c8460-bc5c-4bff-ab2d-b06dbed2b2ca", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 498, "Variable Update", enumValue = choiceMapping.Constants[i].Name, "enumValue");
}}{global::AutoRepairTracing.TraceLogger.Log("243917a6-4d32-4377-a3b2-f090ccfe5e89", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 316, "Break");
                        break;
}                    }
{global::AutoRepairTracing.TraceLogger.Log("aa7bc3c9-5845-4967-8dde-0e0b4a23a72f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 318, "Continue");
                    continue;
}                }
                int colon = xmlName.LastIndexOf(':');
global::AutoRepairTracing.TraceLogger.Log("5bd27459-edaa-4e09-af4d-d6dcdf73eae2", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 320, "Variable Update", colon, "colon");
                string? choiceNs = colon < 0 ? choiceMapping.Namespace : xmlName.Substring(0, colon);
global::AutoRepairTracing.TraceLogger.Log("9a64f2e1-b6ed-4ee4-89ae-8ce6b58e2e1e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 321, "Variable Update", choiceNs, "choiceNs");
                string choiceName = colon < 0 ? xmlName : xmlName.Substring(global::AutoRepairTracing.TraceLogger.LogCheckedBinary("57101176-0681-4e6f-9c41-9093d2ae6f9a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 542, colon, 1, "+"));
global::AutoRepairTracing.TraceLogger.Log("cbbd859e-db54-4d68-af30-590fba67a2cd", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 322, "Variable Update", choiceName, "choiceName");

                if (element.Name == choiceName)
                {
global::AutoRepairTracing.TraceLogger.Log("a060355e-c983-4a11-8f6d-e64baf2ceaeb", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 325, "If-True Branch");
                    if ((element.Form == XmlSchemaForm.Unqualified && string.IsNullOrEmpty(choiceNs)) || element.Namespace == choiceNs)
                    {
global::AutoRepairTracing.TraceLogger.Log("325f05f8-2545-4ce3-97b7-d94318169d38", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 327, "If-True Branch");
                        if (useReflection)
{global::AutoRepairTracing.TraceLogger.Log("995e6637-782f-45b8-bebe-9f00b201782e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 329, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("6dd2b0c7-fa66-47d2-a136-01bde944b594", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 329, "Variable Update", i, "index:choiceMapping.Constants");
global::AutoRepairTracing.TraceLogger.LogValue("003a5549-67b8-432f-933d-a3c6aa9b4714", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 517, "Variable Update", enumValue = choiceMapping.Constants[i].Value.ToString(CultureInfo.InvariantCulture), "enumValue");
}}                        else
{global::AutoRepairTracing.TraceLogger.Log("5d1c7a63-1e6f-4590-a814-f0bbf601e7f4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 331, "Else Branch");
{global::AutoRepairTracing.TraceLogger.Log("4173c727-e0ca-445d-bb00-b57b11454e2f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 331, "Variable Update", i, "index:choiceMapping.Constants");
global::AutoRepairTracing.TraceLogger.LogValue("2bc4d4aa-086f-49a2-b6a7-0e2341f50d6f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 520, "Variable Update", enumValue = choiceMapping.Constants[i].Name, "enumValue");
}}{global::AutoRepairTracing.TraceLogger.Log("f667afb2-9f25-45b8-9358-e6fb69038857", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 332, "Break");
                        break;
}                    }
                }
            }
}
            if (string.IsNullOrEmpty(enumValue))
            {
global::AutoRepairTracing.TraceLogger.Log("109b8cc7-8319-4b6b-a70c-0217ba394937", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 338, "If-True Branch");
                if (element.Any && element.Name.Length == 0)
                {
global::AutoRepairTracing.TraceLogger.Log("6c5d8c39-126f-489b-8e31-eea83a188355", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 340, "If-True Branch");
                    // Type {0} is missing enumeration value '##any' for XmlAnyElementAttribute.
                    throw new InvalidOperationException(SR.Format(SR.XmlChoiceMissingAnyValue, choiceMapping.TypeDesc!.FullName));
                }
                // Type {0} is missing value for '{1}'.
                throw new InvalidOperationException(SR.Format(SR.XmlChoiceMissingValue, choiceMapping.TypeDesc!.FullName, element.Namespace + ":" + element.Name, element.Name, element.Namespace));
            }
            if (!useReflection)
{global::AutoRepairTracing.TraceLogger.Log("473083ee-e05c-40b3-8bda-bdcdfb4f1165", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 348, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("5e72da4a-5d3c-4eb3-96b6-0f69ddcd6cfa", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 348, "CheckValidIdentifier", 0, enumValue, "enumValue");
                CodeIdentifier.CheckValidIdentifier(enumValue);
}{global::AutoRepairTracing.TraceLogger.Log("06dc7041-d9a6-4dfb-8785-152523327090", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 349, "Return");
            return enumValue;
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("97daa595-c637-4889-98c6-15985b1a21da", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 300, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("d85d301a-2b85-480b-89c3-64425bb93133", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 359, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("8b6ccd8c-2593-458e-98b6-3280517bc4ee", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 300, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("9685c421-8257-4d81-aeb7-12e26d36fd47", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 300, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("65d24524-e2eb-49f1-a1fb-5cd31e79d731", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 300, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private void WriteText(object o, TextAccessor text)
{global::AutoRepairTracing.TraceLogger.Log("4f796968-c80e-4617-97ed-b673c699e4b4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 352, "Enter Method 'WriteText'");
try        {
            if (text.Mapping is PrimitiveMapping primitiveMapping)
            {
global::AutoRepairTracing.TraceLogger.Log("d791ab62-65ce-418c-978f-413cf9c38d38", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 355, "If-True Branch");
                string? stringValue;
                if (text.Mapping is EnumMapping enumMapping)
                {
global::AutoRepairTracing.TraceLogger.Log("c8ad8745-db16-40b4-bdb3-88fd01550c80", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 358, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("ca7ede29-0819-4bdf-873c-15924fad3f03", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 559, "Variable Update", stringValue = WriteEnumMethod(enumMapping, o), "stringValue");
                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("a4e81212-580e-4fbb-91b7-8cfba9155ed2", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 362, "Else Branch");
                    if (!WritePrimitiveValue(primitiveMapping.TypeDesc!, o, out stringValue))
                    {
global::AutoRepairTracing.TraceLogger.Log("60adb438-6d79-4b82-a8f1-8bede7c6d3f5", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 364, "If-True Branch");
                        Debug.Assert(o is byte[]);
                    }
                }

                if (o is byte[] byteArray)
                {
global::AutoRepairTracing.TraceLogger.Log("adbb72ee-9457-427d-9583-fa5cf2a2cd06", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 370, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("01928551-2bba-492d-b876-93df396268bd", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 371, "WriteValue", 0, byteArray, "byteArray");
                    WriteValue(byteArray);
}                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("c4b85b4b-e63a-4a03-84f0-2cf4f173a05b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 374, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("7b882084-6c21-4249-94a9-450922d9c10e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 375, "WriteValue", 0, stringValue, "stringValue");
                    WriteValue(stringValue);
}                }
            }
            else {global::AutoRepairTracing.TraceLogger.Log("652dea9c-b05d-43bc-a5c1-af4f034d9c37", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 378, "Else Branch");
if (text.Mapping is SpecialMapping specialMapping)
            {
global::AutoRepairTracing.TraceLogger.Log("626dec27-9ddb-4bf9-ab11-bb1bd191568b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 379, "If-True Branch");
                switch (specialMapping.TypeDesc!.Kind)
                {
                    case TypeKind.Node:
global::AutoRepairTracing.TraceLogger.Log("2d4bd058-8958-4cc9-99a6-99490165417e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 391, "Switch Case [case TypeKind.Node:]");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d5144b0f-842c-4a65-a5ce-281f562df534", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 383, "WriteTo", 0, Writer, "Writer");
                        ((XmlNode)global::AutoRepairTracing.TraceLogger.LogCastSource("033f3bb9-062f-47d1-a514-539500fd57c1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 631, (object)(o), "XmlNode")).WriteTo(Writer);
}{global::AutoRepairTracing.TraceLogger.Log("10b2e5f8-b6a7-4ee2-874f-7cf318ad150f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 384, "Break");
                        break;
}                    default:
global::AutoRepairTracing.TraceLogger.Log("552b8ac2-6cf1-4bf2-9c73-68bd34d41716", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 394, "Switch Case [default:]");
                        throw new InvalidOperationException(SR.XmlInternalError);
                }
            }
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("532d5da6-a4d8-4f81-adf0-ba842434de51", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 352, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("cd5c5874-abd6-4377-a934-85f2b27dac7b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 399, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("c0898a84-cbf1-4e2e-8f62-bd9d4bad9155", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 352, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("482ea989-6150-4841-897e-e1aed4b27cc9", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 352, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("7638e799-e530-4163-ad96-944d4796ccf3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 352, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private void WriteElement(object? o, ElementAccessor element, bool writeAccessor)
{global::AutoRepairTracing.TraceLogger.Log("51469d1c-e1b6-47e4-ac06-4706dbf2e898", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 391, "Enter Method 'WriteElement'");
try        {
            string name = writeAccessor ? element.Name : element.Mapping!.TypeName!;
global::AutoRepairTracing.TraceLogger.Log("8e3e721c-fbc8-49f7-8664-761e7bc86fd7", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 393, "Variable Update", name, "name");
            string? ns = element.Any && element.Name.Length == 0 ? null : (element.Form == XmlSchemaForm.Qualified ? (writeAccessor ? element.Namespace : element.Mapping!.Namespace) : string.Empty);
global::AutoRepairTracing.TraceLogger.Log("7835d3a9-8193-4000-bf43-b9b48feb4233", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 394, "Variable Update", ns, "ns");

            if (element.Mapping is NullableMapping nullableMapping)
            {
global::AutoRepairTracing.TraceLogger.Log("be8f62f9-a999-480f-abc4-68c3f1b0a3ff", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 397, "If-True Branch");
                if (o != null)
                {
global::AutoRepairTracing.TraceLogger.Log("ea4d1fad-9b12-40d6-b92f-3e08dce0f45b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 399, "If-True Branch");
                    ElementAccessor e = element.Clone();
global::AutoRepairTracing.TraceLogger.Log("46f94ef4-8939-4b24-b678-8578b909ac06", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 400, "Variable Update", e, "e");
global::AutoRepairTracing.TraceLogger.LogValue("e2227768-34e1-4df1-84fe-daae47417c03", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 618, "Variable Update", e.Mapping = nullableMapping.BaseMapping, "e.Mapping");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("c82ef4e8-2b7a-49c8-8a0b-0fefce2602a4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 402, "WriteElement", 0, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("fd23583e-fcbd-4fc9-a887-657955d5a330", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 402, "WriteElement", 1, e, "e");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("dd690208-170b-41cf-af4a-e61c78f57e68", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 402, "WriteElement", 2, writeAccessor, "writeAccessor");
                    WriteElement(o, e, writeAccessor);
}                }
                else {global::AutoRepairTracing.TraceLogger.Log("53626ba1-ab55-4c90-ba9a-c270d3cde4bd", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 404, "Else Branch");
if (element.IsNullable)
                {
global::AutoRepairTracing.TraceLogger.Log("0074eb4c-8cb5-411b-b2df-b9297765631b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 405, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("e66d7f19-cbe4-4504-9a2b-cfc7e3916844", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 406, "WriteNullTagLiteral", 0, element.Name, "element.Name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("424e35ea-b356-4654-bb54-f3fa406db5d8", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 406, "WriteNullTagLiteral", 1, ns, "ns");
                    WriteNullTagLiteral(element.Name, ns);
}                }
}            }
            else {global::AutoRepairTracing.TraceLogger.Log("13151b80-b59c-474f-8f63-f27ba7a91102", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 409, "Else Branch");
if (element.Mapping is ArrayMapping)
            {
global::AutoRepairTracing.TraceLogger.Log("5e5e1290-eb7b-4732-8352-44d39712421b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 410, "If-True Branch");
                var mapping = global::AutoRepairTracing.TraceLogger.LogAsCast<ArrayMapping>("50f4c555-c3ad-4b68-b520-81b4543262fb", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 678, (object)(element.Mapping), "ArrayMapping");
global::AutoRepairTracing.TraceLogger.Log("c4b5e750-7293-4563-887c-367223077ffd", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 411, "Variable Update", mapping, "mapping");
                if (element.IsNullable && o == null)
                {
global::AutoRepairTracing.TraceLogger.Log("09759ff7-80e7-4305-b575-fcb7f4d3babf", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 413, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("f955fd4b-fac5-4dde-a3b9-3af076817038", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 414, "WriteNullTagLiteral", 0, element.Name, "element.Name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("86afaa4c-c2a4-4b1c-83da-729c792d95fc", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 414, "WriteNullTagLiteral", 1, element.Form == XmlSchemaForm.Qualified ? element.Namespace : string.Empty, "element.Form == XmlSchemaForm.Qualified ? element.Namespace : string.Empty");
                    WriteNullTagLiteral(element.Name, element.Form == XmlSchemaForm.Qualified ? element.Namespace : string.Empty);
}                }
                else {global::AutoRepairTracing.TraceLogger.Log("c042d669-757e-4483-a906-dc7843edf1c0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 416, "Else Branch");
if (mapping!.IsSoap)
                {
global::AutoRepairTracing.TraceLogger.Log("067d0d38-e304-4059-919e-47fac2965d68", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 417, "If-True Branch");
                    if (mapping.Elements == null || mapping.Elements.Length != 1)
                    {
global::AutoRepairTracing.TraceLogger.Log("a3d0b1a0-a84a-47d6-bb27-ba8a67a5390e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 419, "If-True Branch");
                        throw new InvalidOperationException(SR.XmlInternalError);
                    }

                    if (!writeAccessor)
                    {
global::AutoRepairTracing.TraceLogger.Log("d9554c9d-ee9b-4867-8cd7-aff6699d612b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 424, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("8ff29d7a-9087-4c8a-ab03-0f8c1215ad22", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 425, "WritePotentiallyReferencingElement", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d3c031ee-a542-4d64-818e-a949da0ceeeb", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 425, "WritePotentiallyReferencingElement", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("3aa730c4-68f8-4a0b-89e2-eaa9d76c099a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 425, "WritePotentiallyReferencingElement", 2, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("85423e1d-080d-4d92-a7b4-cbd539730668", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 425, "WritePotentiallyReferencingElement", 3, mapping.TypeDesc!.Type, "mapping.TypeDesc!.Type");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("14bde9b6-78f2-4799-afc3-bffed8ca2c5f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 425, "WritePotentiallyReferencingElement", 4, true, "true");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("089d6ba8-5006-4d54-9c04-21fbbebd668d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 425, "WritePotentiallyReferencingElement", 5, element.IsNullable, "element.IsNullable");
                        WritePotentiallyReferencingElement(name, ns, o, mapping.TypeDesc!.Type, true, element.IsNullable);
}                    }
                    else
                    {
global::AutoRepairTracing.TraceLogger.Log("cb58e0d6-0111-4d87-ae72-97b3db0aeacf", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 428, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("6b56873c-1437-46ff-a0fd-0ad412e05609", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 429, "WritePotentiallyReferencingElement", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("4be633e3-8b8f-4b48-ab6f-1b2ef8d8cd56", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 429, "WritePotentiallyReferencingElement", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("b20f6c91-89c2-4f2d-a741-2705bfa922f0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 429, "WritePotentiallyReferencingElement", 2, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("2698eec0-7c51-4de3-a795-6e8421f87704", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 429, "WritePotentiallyReferencingElement", 3, null, "null");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("979ed655-8eec-4a17-b0be-cecb34f1abe4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 429, "WritePotentiallyReferencingElement", 4, false, "false");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("f142c162-a822-4099-a494-c55d22efbce0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 429, "WritePotentiallyReferencingElement", 5, element.IsNullable, "element.IsNullable");
                        WritePotentiallyReferencingElement(name, ns, o, null, false, element.IsNullable);
}                    }
                }
                else {global::AutoRepairTracing.TraceLogger.Log("dae841ac-5196-4d45-b22d-59fe1040b47a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 432, "Else Branch");
if (element.IsUnbounded)
                {
global::AutoRepairTracing.TraceLogger.Log("97adcd95-8403-41d5-93ea-8e5e8abd95c1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 433, "If-True Branch");
                    var enumerable = (IEnumerable)global::AutoRepairTracing.TraceLogger.LogCastSource("a3de8031-e4c8-47b4-9660-80888472eae3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 724, (object)(o!), "IEnumerable");
global::AutoRepairTracing.TraceLogger.Log("4466401e-ab35-4cc8-bd62-58fb8bbce60e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 434, "Variable Update", enumerable, "enumerable");
                    foreach (var e in enumerable)
                    {
global::AutoRepairTracing.TraceLogger.Log("84824bff-f1e1-45f5-8687-ebd7d0844655", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 435, "ForEach Loop");
global::AutoRepairTracing.TraceLogger.LogValue("3428d4a9-8ff6-4db4-9876-4b3970f6a9c2", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 686, "Variable Update", element.IsUnbounded = false, "element.IsUnbounded");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("4afd344a-57e3-45f4-b1bd-dd8e5294888e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 438, "WriteElement", 0, e, "e");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("f7984aad-be5b-4245-bc49-1d51a52528b6", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 438, "WriteElement", 1, element, "element");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("a7307606-a8d4-4201-8b2b-b57b3bb2eb46", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 438, "WriteElement", 2, writeAccessor, "writeAccessor");
                        WriteElement(e, element, writeAccessor);
}global::AutoRepairTracing.TraceLogger.LogValue("befe4434-8699-4683-8292-b8e7f973f697", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 691, "Variable Update", element.IsUnbounded = true, "element.IsUnbounded");
                    }
                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("5059b93e-454e-4b2f-b319-005625cb6afc", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 443, "Else Branch");
                    if (o != null)
                    {
global::AutoRepairTracing.TraceLogger.Log("2bf63590-1f77-4a8b-8876-677466e834cb", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 445, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("8c531cb8-65b1-4d79-a12f-124350976a86", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 446, "WriteStartElement", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("776826e3-32ee-4208-b0df-dc51cd0925ca", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 446, "WriteStartElement", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("7b101313-3f15-47be-a141-a5bb7a7277a6", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 446, "WriteStartElement", 2, false, "false");
                        WriteStartElement(name, ns, false);
}{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("b2afbaac-a5c9-46e5-b1f7-022f06e4c139", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 447, "WriteArrayItems", 0, mapping.ElementsSortedByDerivation!, "mapping.ElementsSortedByDerivation!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("e1c3392e-a3b7-41a8-a3bd-5c4fedb6df68", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 447, "WriteArrayItems", 1, null, "null");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("04538c00-ac78-433f-9c29-09b5d56535ed", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 447, "WriteArrayItems", 2, null, "null");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("77ee0f10-653c-4879-b154-78ea2f240bc2", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 447, "WriteArrayItems", 3, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("ee137a16-50c3-42ab-a178-8528671628ab", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 447, "WriteArrayItems", 4, null, "null");
                        WriteArrayItems(mapping.ElementsSortedByDerivation!, null, null, o, null);
}                        WriteEndElement();
                    }
                }
}}            }
            else {global::AutoRepairTracing.TraceLogger.Log("83e1d86c-5c90-4a3a-b71d-6b04f3bc444c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 452, "Else Branch");
if (element.Mapping is EnumMapping)
            {
global::AutoRepairTracing.TraceLogger.Log("ec09ab69-b5b4-4a16-b12f-c4768ecc2be1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 453, "If-True Branch");
                if (element.Mapping.IsSoap)
                {
global::AutoRepairTracing.TraceLogger.Log("aea06b51-c742-4b71-b6cd-3de3f331a0fb", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 455, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d3f0bdae-07ab-4764-bc28-4d4a8ecb4efe", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 456, "WriteStartElement", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("a5e8536b-a6f8-4825-914e-c55c2a1f76ec", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 456, "WriteStartElement", 1, ns, "ns");
                    Writer.WriteStartElement(name, ns);
}{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("ab2ceeef-f0fe-4b90-ac49-23533e8a846c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 457, "WriteEnumMethod", 0, (EnumMapping)global::AutoRepairTracing.TraceLogger.LogCastSource("59ad3ac1-bcc8-42e3-a25a-5f9955e4a816", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 767, (object)(element.Mapping), "EnumMapping"), "(EnumMapping)element.Mapping");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("b159c131-c1e6-41d3-a25d-530363473d8f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 457, "WriteEnumMethod", 1, o!, "o!");
                    WriteEnumMethod((EnumMapping)global::AutoRepairTracing.TraceLogger.LogCastSource("30946137-d6da-450f-9b48-175ea7786adb", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 769, (object)(element.Mapping), "EnumMapping"), o!);
}                    WriteEndElement();
                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("53dd01a9-11a9-4eab-a557-81f3fb9aa92d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 461, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("43932007-f181-4ace-860f-80650d3ac3d3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 462, "WritePrimitive", 0, WritePrimitiveMethodRequirement.WriteElementString, "WritePrimitiveMethodRequirement.WriteElementString");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("98624186-4912-468e-9c5c-dae9aee523a7", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 462, "WritePrimitive", 1, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("c1d9df2d-eaf0-4c01-85c4-0f5cd37b0d80", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 462, "WritePrimitive", 2, ns!, "ns!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("34fd8157-1063-4dc4-bbe4-6e38070417e4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 462, "WritePrimitive", 3, element.Default, "element.Default");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("98cb38b1-c5cc-4962-b825-b98a5fde0817", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 462, "WritePrimitive", 4, o!, "o!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("4d8def84-6caf-4e6c-8e6a-8a6a9fa565d0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 462, "WritePrimitive", 5, element.Mapping, "element.Mapping");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("1ca88fa3-3593-4fb3-aa0b-8ad1e68d6972", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 462, "WritePrimitive", 6, false, "false");
                    WritePrimitive(WritePrimitiveMethodRequirement.WriteElementString, name, ns!, element.Default, o!, element.Mapping, false);
}                }
            }
            else {global::AutoRepairTracing.TraceLogger.Log("5de7cf52-36de-4cd7-856e-b5b3b14dc14f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 465, "Else Branch");
if (element.Mapping is PrimitiveMapping)
            {
global::AutoRepairTracing.TraceLogger.Log("466b3251-fb1c-4cd2-9a50-aeb2b79e7f1e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 466, "If-True Branch");
                var mapping = global::AutoRepairTracing.TraceLogger.LogAsCast<PrimitiveMapping>("124e52c5-060d-4fd8-920f-71fb2ffea585", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 789, (object)(element.Mapping), "PrimitiveMapping");
global::AutoRepairTracing.TraceLogger.Log("1ea4fed7-e3a1-4a5f-83b5-233df196d487", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 467, "Variable Update", mapping, "mapping");
                if (mapping!.TypeDesc == ReflectionXmlSerializationReader.QnameTypeDesc)
                {
global::AutoRepairTracing.TraceLogger.Log("db32cf67-ab94-4c89-8409-82b0a9303b26", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 469, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("9792e6d5-be35-4b60-b2d6-bd715c9222fd", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 470, "WriteQualifiedNameElement", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("bfaa4ba0-1939-4807-bd2b-f5ff8111ed0f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 470, "WriteQualifiedNameElement", 1, ns!, "ns!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("de89ad0e-5cbe-4643-bf7a-c3858cf8e5f5", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 470, "WriteQualifiedNameElement", 2, element.Default, "element.Default");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("e3260bdb-2116-4ca9-a4e1-3636121677e3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 470, "WriteQualifiedNameElement", 3, (XmlQualifiedName)global::AutoRepairTracing.TraceLogger.LogCastSource("374a2657-43fe-4f60-b8d4-c02eea827f95", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 797, (object)(o!), "XmlQualifiedName"), "(XmlQualifiedName)o!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("24200181-e658-4780-995a-6fbeac051e8e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 470, "WriteQualifiedNameElement", 4, element.IsNullable, "element.IsNullable");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("42a1162d-8a1c-4388-8501-1f6267bfdc9c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 470, "WriteQualifiedNameElement", 5, mapping.IsSoap, "mapping.IsSoap");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("ccc62da3-e9f8-42f0-af7d-5b75aa5e3d9c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 470, "WriteQualifiedNameElement", 6, mapping, "mapping");
                    WriteQualifiedNameElement(name, ns!, element.Default, (XmlQualifiedName)global::AutoRepairTracing.TraceLogger.LogCastSource("9cb5e7f8-fd05-4b34-b8e3-16eeea38598e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 801, (object)(o!), "XmlQualifiedName"), element.IsNullable, mapping.IsSoap, mapping);
}                }
                else {global::AutoRepairTracing.TraceLogger.Log("709bc760-2e6a-4710-bc6a-2f15e4afefe3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 472, "Else Branch");
if (o == null && element.IsNullable)
                {
global::AutoRepairTracing.TraceLogger.Log("cec26f8b-95a8-4095-9089-a1714adcf2ee", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 473, "If-True Branch");
                    if (mapping.IsSoap)
                    {
global::AutoRepairTracing.TraceLogger.Log("3b598eb3-ec92-40ee-8e50-a2be4f087efa", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 475, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("71ac49b4-ee54-4292-8c4b-291a74df2f20", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 476, "WriteNullTagEncoded", 0, element.Name, "element.Name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("baffc387-4bea-499b-8bdd-b1f4a61c4a38", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 476, "WriteNullTagEncoded", 1, ns, "ns");
                        WriteNullTagEncoded(element.Name, ns);
}                    }
                    else
                    {
global::AutoRepairTracing.TraceLogger.Log("edbbbc1b-ba5f-4b53-a52f-daba6a885b1d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 479, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("74b3506d-ad70-4b4b-af09-d2253747be4f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 480, "WriteNullTagLiteral", 0, element.Name, "element.Name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("fdc93689-ac42-4b61-92f8-566d41217625", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 480, "WriteNullTagLiteral", 1, ns, "ns");
                        WriteNullTagLiteral(element.Name, ns);
}                    }
                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("b7ca0d68-1e2c-4618-a26c-9605f54716fc", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 484, "Else Branch");
                    WritePrimitiveMethodRequirement suffixNullable = mapping.IsSoap ? WritePrimitiveMethodRequirement.Encoded : WritePrimitiveMethodRequirement.None;
global::AutoRepairTracing.TraceLogger.Log("061f4903-801e-4d36-bff7-ebf2a469b6bc", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 485, "Variable Update", suffixNullable, "suffixNullable");
                    WritePrimitiveMethodRequirement suffixRaw = mapping.TypeDesc!.XmlEncodingNotRequired ? WritePrimitiveMethodRequirement.Raw : WritePrimitiveMethodRequirement.None;
global::AutoRepairTracing.TraceLogger.Log("80b4816e-048d-4920-b67b-5598853669bb", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 486, "Variable Update", suffixRaw, "suffixRaw");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("da72323c-c13b-4980-9966-c5203185a170", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 487, "WritePrimitive", 0, element.IsNullable
                        ? WritePrimitiveMethodRequirement.WriteNullableStringLiteral | suffixNullable | suffixRaw
                        : WritePrimitiveMethodRequirement.WriteElementString | suffixRaw, "element.IsNullable                         ? WritePrimitiveMethodRequirement.WriteNullableStringLiteral | suffixNullable | suffixRaw                         : WritePrimitiveMethodRequirement.WriteElementString | suffixRaw");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("6898473b-d9ce-452c-82f9-d772d60009c1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 487, "WritePrimitive", 1, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("c2abc26d-e56c-4772-94cf-de7f9efc3810", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 487, "WritePrimitive", 2, ns!, "ns!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("73ad6ad9-17e0-4378-b655-25194a01c102", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 487, "WritePrimitive", 3, element.Default, "element.Default");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("0a49584b-2381-4486-8ff5-421b353abad3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 487, "WritePrimitive", 4, o!, "o!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("bf5a3ce3-7843-4213-bbf4-39b23173a769", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 487, "WritePrimitive", 5, mapping, "mapping");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("b1a84f6e-541a-49b8-bcb7-97c9a34d3f68", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 487, "WritePrimitive", 6, mapping.IsSoap, "mapping.IsSoap");
                    WritePrimitive(element.IsNullable
                        ? WritePrimitiveMethodRequirement.WriteNullableStringLiteral | suffixNullable | suffixRaw
                        : WritePrimitiveMethodRequirement.WriteElementString | suffixRaw,
                        name, ns!, element.Default, o!, mapping, mapping.IsSoap);
}                }
}            }
            else {global::AutoRepairTracing.TraceLogger.Log("5431af31-6c23-45dd-a30e-2b754265fadb", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 493, "Else Branch");
if (element.Mapping is StructMapping)
            {
global::AutoRepairTracing.TraceLogger.Log("434f7f5e-c043-472c-b1f6-acbea792ed20", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 494, "If-True Branch");
                var mapping = global::AutoRepairTracing.TraceLogger.LogAsCast<StructMapping>("0ebadc0b-12e9-4402-b292-fc97a2c6afba", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 848, (object)(element.Mapping), "StructMapping");
global::AutoRepairTracing.TraceLogger.Log("87cf6ca4-3dce-4c7d-98ec-0727dd9515a2", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 495, "Variable Update", mapping, "mapping");
                if (mapping!.IsSoap)
                {
global::AutoRepairTracing.TraceLogger.Log("7faf9c12-6bcf-4791-af88-589d2efbf2da", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 497, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("211f2d31-b72d-47d6-855a-49ba8c730150", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 498, "WritePotentiallyReferencingElement", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("086457f9-8240-42bc-8cea-5a81b593c428", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 498, "WritePotentiallyReferencingElement", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("a781a040-7f02-4dc5-8a3b-b2afa2cf832b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 498, "WritePotentiallyReferencingElement", 2, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d0ea414d-108f-4226-b63d-c80205bde3bd", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 498, "WritePotentiallyReferencingElement", 3, !writeAccessor ? mapping.TypeDesc!.Type : null, "!writeAccessor ? mapping.TypeDesc!.Type : null");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("3f534055-2965-43f2-a7b7-be3c8763cc4e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 498, "WritePotentiallyReferencingElement", 4, !writeAccessor, "!writeAccessor");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("9a312521-9d72-4708-8cad-d01110b98f2a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 498, "WritePotentiallyReferencingElement", 5, element.IsNullable, "element.IsNullable");
                    WritePotentiallyReferencingElement(name, ns, o, !writeAccessor ? mapping.TypeDesc!.Type : null, !writeAccessor, element.IsNullable);
}                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("92600974-93ea-4844-9060-e0052aad2eb7", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 501, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("025f7bf4-396d-4524-87e0-dc8f5ab816a3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 502, "WriteStructMethod", 0, mapping, "mapping");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("0eddd1d5-adb7-42d0-9bb4-ef4986621c02", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 502, "WriteStructMethod", 1, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("937e1bea-3ced-4e80-86a5-3a088eb2a7e7", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 502, "WriteStructMethod", 2, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("8706aa14-9fc4-4c04-a0d6-c2ca1085675b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 502, "WriteStructMethod", 3, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("470939ab-4c30-4111-a9ed-0e4aeb8414b7", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 502, "WriteStructMethod", 4, element.IsNullable, "element.IsNullable");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("45a70df7-558b-4170-bb46-ac2148cf7a1d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 502, "WriteStructMethod", 5, false, "false");
                    WriteStructMethod(mapping, name, ns, o, element.IsNullable, needType: false);
}                }
            }
            else {global::AutoRepairTracing.TraceLogger.Log("187df6df-2182-4ff8-9564-ebda5a1d985f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 505, "Else Branch");
if (element.Mapping is SpecialMapping)
            {
global::AutoRepairTracing.TraceLogger.Log("16565a68-c808-4f75-bc43-d29290ae0d6f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 506, "If-True Branch");
                if (element.Mapping is SerializableMapping)
                {
global::AutoRepairTracing.TraceLogger.Log("347816a1-ceec-40a9-bfc2-ba6c4de531af", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 508, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("729ae43e-2743-49c3-b80a-83ea33403ad7", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 509, "WriteSerializable", 0, (IXmlSerializable)global::AutoRepairTracing.TraceLogger.LogCastSource("1d086746-8a60-49f1-a37c-b4a4dafef1ae", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 880, (object)(o!), "IXmlSerializable"), "(IXmlSerializable)o!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d25f55a0-9c55-408e-95d9-2dd987811474", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 509, "WriteSerializable", 1, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("ca941eb6-437e-4eff-a937-015946dcf378", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 509, "WriteSerializable", 2, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("6ca3c13c-398e-49b1-84ff-6560a85b0ede", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 509, "WriteSerializable", 3, element.IsNullable, "element.IsNullable");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("bee16b97-9a60-4c1b-8a25-86ba3695cc07", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 509, "WriteSerializable", 4, !element.Any, "!element.Any");
                    WriteSerializable((IXmlSerializable)global::AutoRepairTracing.TraceLogger.LogCastSource("736c2aaa-cc78-418c-8163-5cb84e96c5ca", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 885, (object)(o!), "IXmlSerializable"), name, ns, element.IsNullable, !element.Any);
}                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("5cd08918-ee3c-4eab-9a63-77c298991b2e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 512, "Else Branch");
                    // XmlNode, XmlElement
                    if (o is XmlNode node)
                    {
global::AutoRepairTracing.TraceLogger.Log("1bf2a8ba-bbaa-4fa5-b38b-4861d8b1ee17", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 515, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("68fb1e09-a160-4d81-a7e7-84f83e9db6ac", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 516, "WriteElementLiteral", 0, node, "node");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("b2ed2290-04ed-4280-a53a-0732614d57a8", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 516, "WriteElementLiteral", 1, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("5b9a8b7f-016d-4570-9a97-539c359a0146", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 516, "WriteElementLiteral", 2, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("c08fc521-926d-4f35-8a7d-fb058d9e9c88", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 516, "WriteElementLiteral", 3, element.IsNullable, "element.IsNullable");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("e81f9a8c-fe80-419a-979a-7b0d5eec91e4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 516, "WriteElementLiteral", 4, element.Any, "element.Any");
                        WriteElementLiteral(node, name, ns, element.IsNullable, element.Any);
}                    }
                    else
                    {
global::AutoRepairTracing.TraceLogger.Log("a21de880-9882-4027-b8b1-d03e7a886478", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 519, "Else Branch");
                        throw CreateInvalidAnyTypeException(o!);
                    }
                }
            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("2055f6ca-497b-4945-b188-e11c40c6e645", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 525, "Else Branch");
                throw new InvalidOperationException(SR.XmlInternalError);
            }
}}}}}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("ec0935f6-b119-4226-bbfe-1ed56e42b5ec", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 391, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("56fe423a-68a9-49ee-bd41-33f1ba89bbb0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 539, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("b900518d-0ab6-47d7-9c3e-7e398fd4fe55", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 391, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("b7f1acf3-b365-4ee1-91d2-51be8afd8bb9", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 391, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("37fca967-66ac-46ff-b6c4-be2b024ccf52", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 391, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private XmlSerializationWriteCallback CreateXmlSerializationWriteCallback(TypeMapping mapping, string name, string? ns, bool isNullable)
{global::AutoRepairTracing.TraceLogger.Log("994cbdd2-ee13-48ef-90c7-230624d6581e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 530, "Enter Method 'CreateXmlSerializationWriteCallback'");
try        {
            if (mapping is StructMapping structMapping)
            {
global::AutoRepairTracing.TraceLogger.Log("a8ac26d1-3cb8-45a4-89ed-bd77d4062f73", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 533, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("e2f5eacb-db8a-4216-8602-e8fc789595c1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 534, "Return");
                return (o) =>
                {
                    WriteStructMethod(structMapping, name, ns, o, isNullable, needType: false);
                };
}            }
            else {global::AutoRepairTracing.TraceLogger.Log("382b8b48-cad2-46e3-b11d-96b25175668a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 539, "Else Branch");
if (mapping is EnumMapping enumMapping)
            {
global::AutoRepairTracing.TraceLogger.Log("7e8ff26a-02cb-4ffa-9b22-f6274f0a2c12", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 540, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("b750499d-c322-4b21-a26e-bd4f0cdb46a1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 541, "Return");
                return (o) =>
                {
                    WriteEnumMethod(enumMapping, o);
                };
}            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("5ce9a347-e8cd-4c4e-a033-96e9f1d8165c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 547, "Else Branch");
                throw new InvalidOperationException(SR.XmlInternalError);
            }
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("0b6c95f7-ffbd-4266-8545-62ea9044452e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 530, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("aec8cfde-78e4-4f77-9b34-e7ea25968282", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 562, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("71bbaf17-f8c8-4d79-ba12-4425357035ef", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 530, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("deb6112e-a590-4c3b-9695-6315244a9bb4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 530, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("52ad25cc-e9b8-456a-85a2-fded55071594", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 530, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private void WriteQualifiedNameElement(string name, string ns, object? defaultValue, XmlQualifiedName o, bool nullable, bool isSoap, PrimitiveMapping mapping)
{global::AutoRepairTracing.TraceLogger.Log("8be46593-ab2a-46e0-82d8-0d8970aef39f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 552, "Enter Method 'WriteQualifiedNameElement'");
try        {
            bool hasDefault = defaultValue != null && defaultValue != DBNull.Value && mapping.TypeDesc!.HasDefaultSupport;
global::AutoRepairTracing.TraceLogger.Log("1c3d45b6-e70f-4ba4-8b3b-7b77d56263a0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 554, "Variable Update", hasDefault, "hasDefault");
            if (hasDefault && IsDefaultValue(o, defaultValue!))
{global::AutoRepairTracing.TraceLogger.Log("fd8909cc-35e2-45f9-9e03-f47da5adf173", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 556, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("f0b7fbd3-1524-4c5c-84c8-da8a182d1e6a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 556, "Return");
                return;
}
            if (isSoap)
            {
global::AutoRepairTracing.TraceLogger.Log("b9ae935b-fd27-422d-9a71-2a077184c464", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 559, "If-True Branch");
                if (nullable)
                {
global::AutoRepairTracing.TraceLogger.Log("1ab027d2-272d-42b0-8dd3-ed68ebb23e70", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 561, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("a3b57049-1a50-45cf-b151-068349ba390b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 562, "WriteNullableQualifiedNameEncoded", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("44276a34-05b4-4092-b4c2-2a49ebf97b21", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 562, "WriteNullableQualifiedNameEncoded", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("b97ce84d-148e-4807-8798-bddbcbeabc69", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 562, "WriteNullableQualifiedNameEncoded", 2, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("0373ae4a-cd9c-458a-89d2-8f7d3b950f72", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 562, "WriteNullableQualifiedNameEncoded", 3, new XmlQualifiedName(mapping.TypeName, mapping.Namespace), "new XmlQualifiedName(mapping.TypeName, mapping.Namespace)");
                    WriteNullableQualifiedNameEncoded(name, ns, o, new XmlQualifiedName(mapping.TypeName, mapping.Namespace));
}                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("086570ce-2880-43f9-b076-68d5d2014e5a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 565, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d7fd2cd5-25f8-4a68-98c8-0b8fa528df00", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 566, "WriteElementQualifiedName", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("1c8b5837-3213-4812-bcb6-700dec9f6f2a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 566, "WriteElementQualifiedName", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("287eb1b4-c666-49e7-ba73-39eab7215909", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 566, "WriteElementQualifiedName", 2, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("8da5102d-edec-4cbf-84ec-6cfa449803a9", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 566, "WriteElementQualifiedName", 3, new XmlQualifiedName(mapping.TypeName, mapping.Namespace), "new XmlQualifiedName(mapping.TypeName, mapping.Namespace)");
                    WriteElementQualifiedName(name, ns, o, new XmlQualifiedName(mapping.TypeName, mapping.Namespace));
}                }
            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("fc10d787-2be4-440a-a78e-a0ff9aa04002", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 570, "Else Branch");
                if (nullable)
                {
global::AutoRepairTracing.TraceLogger.Log("832c6c32-c3f5-47e9-80d0-33177b4fba87", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 572, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("2d743efe-7983-422b-b771-d931e33cacf6", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 573, "WriteNullableQualifiedNameLiteral", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("1b5a6e9d-4080-468c-beaf-b0cba229f5f2", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 573, "WriteNullableQualifiedNameLiteral", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("729ded9d-8c42-4f1d-a2d7-814ad861f9a2", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 573, "WriteNullableQualifiedNameLiteral", 2, o, "o");
                    WriteNullableQualifiedNameLiteral(name, ns, o);
}                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("a253dfbb-9767-4882-b396-2129f062dc42", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 576, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d5edf79e-b83f-432e-8a9f-1c7227495697", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 577, "WriteElementQualifiedName", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("b7351b14-be4a-4ab1-bce7-4c884cb70b7a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 577, "WriteElementQualifiedName", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("4681199c-129e-4f79-acf9-c136a3910259", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 577, "WriteElementQualifiedName", 2, o, "o");
                    WriteElementQualifiedName(name, ns, o);
}                }
            }
        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("04d8558b-57f1-4052-b1cd-a18ec5fe8b5d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 552, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("86b39983-b86e-44da-8502-9c4e95382748", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 593, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("529c5be5-5326-46cb-ae03-a05f08f41de6", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 552, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("cf979c23-fd48-4d9a-b9b6-06c47e61a701", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 552, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("fc482053-bc9f-40a3-9b82-8890018c4398", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 552, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private void WriteStructMethod(StructMapping mapping, string n, string? ns, object? o, bool isNullable, bool needType)
{global::AutoRepairTracing.TraceLogger.Log("b1468777-8a0d-405d-9281-c707b0d5dfd3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 582, "Enter Method 'WriteStructMethod'");
try        {
            if (mapping.IsSoap && mapping.TypeDesc!.IsRoot) {global::AutoRepairTracing.TraceLogger.Log("94965f08-5887-4454-afbd-94362dd8064a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 584, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("73378e63-f39e-4114-8971-00144adef237", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 584, "Return");
return;
}
            if (!mapping.IsSoap)
            {
global::AutoRepairTracing.TraceLogger.Log("0f7a80cf-97ba-4390-8f09-e663c42be621", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 587, "If-True Branch");
                if (o == null)
                {
global::AutoRepairTracing.TraceLogger.Log("dec16853-872d-4b56-ba08-917a1e5dfc03", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 589, "If-True Branch");
                    if (isNullable) {global::AutoRepairTracing.TraceLogger.Log("ed322238-6181-41ac-ac0a-85148ba7ec04", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 590, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("b955da73-e55b-4242-8d85-ae3e54cd1053", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 590, "WriteNullTagLiteral", 0, n, "n");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("1b3fc714-7c89-48d3-83ea-a8096350229c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 590, "WriteNullTagLiteral", 1, ns, "ns");
WriteNullTagLiteral(n, ns);
}{global::AutoRepairTracing.TraceLogger.Log("f893a419-8583-4b5f-ac9e-533f1e48c2da", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 591, "Return");
                    return;
}                }

                if (!needType
                 && o.GetType() != mapping.TypeDesc!.Type)
                {
global::AutoRepairTracing.TraceLogger.Log("1e853027-030c-46f5-a137-bfd4cf8677f8", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 596, "If-True Branch");
                    if (WriteDerivedTypes(mapping, n, ns, o, isNullable))
                    {
global::AutoRepairTracing.TraceLogger.Log("dfef8a61-cd9e-4179-95ad-36918bf4913a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 598, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("7e39f739-d3bc-4d28-a975-21998cb69943", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 599, "Return");
                        return;
}                    }

                    if (mapping.TypeDesc.IsRoot)
                    {
global::AutoRepairTracing.TraceLogger.Log("14c3e3cb-a44b-4fe9-bf9e-4201d85408eb", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 603, "If-True Branch");
                        if (WriteEnumAndArrayTypes(o, n!, ns))
                        {
global::AutoRepairTracing.TraceLogger.Log("a55ec8b7-d5c5-4612-9d50-e0d117d6d630", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 605, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("dc75f9dc-330c-4d72-b914-b850d079d741", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 606, "Return");
                            return;
}                        }
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("cb389dad-beb8-4365-9b1f-e2c2caa62144", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 609, "WriteTypedPrimitive", 0, n, "n");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("c8dd700f-4090-4e1d-932d-5a47c7966de8", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 609, "WriteTypedPrimitive", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("dda2dea4-157f-4a1d-94be-a98fa24a2d5e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 609, "WriteTypedPrimitive", 2, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("4a316934-e96c-4940-af5e-059237fabd5e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 609, "WriteTypedPrimitive", 3, true, "true");

                        WriteTypedPrimitive(n, ns, o, true);
}{global::AutoRepairTracing.TraceLogger.Log("ad8e6db2-07cf-4d48-903f-3867f7c1ab75", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 610, "Return");
                        return;
}                    }

                    throw CreateUnknownTypeException(o);
                }
            }

            if (!mapping.TypeDesc!.IsAbstract)
            {
global::AutoRepairTracing.TraceLogger.Log("d0e7bc6e-8004-4270-806d-9250a27e7947", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 618, "If-True Branch");
                if (mapping.TypeDesc.Type != null && typeof(XmlSchemaObject).IsAssignableFrom(mapping.TypeDesc.Type))
                {
global::AutoRepairTracing.TraceLogger.Log("30c90fdd-a6d2-4372-97d9-1bb9172b8bdf", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 620, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("fc17390b-52f3-49d7-9186-5970e6f26c4e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1017, "Variable Update", EscapeName = false, "EscapeName");
                }

                XmlSerializerNamespaces? xmlnsSource = null;
global::AutoRepairTracing.TraceLogger.Log("75f85b94-afe9-42a0-a2c2-3b413eb175a4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 624, "Variable Update", xmlnsSource, "xmlnsSource");
global::AutoRepairTracing.TraceLogger.Log("29844efe-35d4-448a-8f5e-fcbcd7c47455", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 624, "Null Assignment Warning", "null", "xmlnsSource");                MemberMapping[] members = TypeScope.GetAllMembers(mapping);
global::AutoRepairTracing.TraceLogger.Log("b26ff93f-0fc8-42f3-91ce-5127e70f98a3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 625, "Heap State: members (MemberMapping[])");global::AutoRepairTracing.TraceLogger.Log("6866c221-978a-45d7-ae98-7626e04dd9f7", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 625, "Variable Update", members, "members");
                int xmlnsMember = FindXmlnsIndex(members);
global::AutoRepairTracing.TraceLogger.Log("a28dcb87-58f2-4a27-a1a7-9acf1f2d07a8", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 626, "Variable Update", xmlnsMember, "xmlnsMember");
                if (xmlnsMember >= 0)
                {
global::AutoRepairTracing.TraceLogger.Log("32e63c10-1d98-41f4-88cc-c197f647104c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 628, "If-True Branch");
                    MemberMapping member = members[xmlnsMember];
global::AutoRepairTracing.TraceLogger.Log("5c16f980-e8c2-49cd-b4bf-981c574e8cb9", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 629, "Variable Update", member, "member");
global::AutoRepairTracing.TraceLogger.LogValue("fc4e6f8b-bb1d-43c6-a997-af237358c4d2", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1027, "Variable Update", xmlnsSource = (XmlSerializerNamespaces?)global::AutoRepairTracing.TraceLogger.LogCastSource("79ffcbba-0bd4-41b7-b4e1-64583829b047", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1079, (object)(GetMemberValue(o!, member.Name)), "XmlSerializerNamespaces?"), "xmlnsSource");
                }

                if (!mapping.IsSoap)
                {
global::AutoRepairTracing.TraceLogger.Log("c27dbf95-05cc-4ad7-84c0-428cb6e4e971", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 634, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("5a064cf5-2a2b-4773-98f5-3290d2765e82", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 635, "WriteStartElement", 0, n, "n");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("52dd2cdd-b5de-45bf-90e9-1a2351a6ae0c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 635, "WriteStartElement", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("fbd2e5d6-ab09-4e28-b930-65e3b6289adc", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 635, "WriteStartElement", 2, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("387364ad-9a27-4630-b023-20f0969b3306", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 635, "WriteStartElement", 3, false, "false");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("5d67fb23-dddf-40c6-a088-0ba36d58d5b6", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 635, "WriteStartElement", 4, xmlnsSource, "xmlnsSource");
                    WriteStartElement(n, ns, o, false, xmlnsSource);
}
                    if (!mapping.TypeDesc.IsRoot)
                    {
global::AutoRepairTracing.TraceLogger.Log("08bb2324-fe63-491d-86ed-b995a57abd68", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 638, "If-True Branch");
                        if (needType)
                        {
global::AutoRepairTracing.TraceLogger.Log("01572b4c-2ed2-4839-b760-f753640d1ad7", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 640, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("fa382d4c-e44c-4220-8b1b-cf74537a9319", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 641, "WriteXsiType", 0, mapping.TypeName!, "mapping.TypeName!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("39dc88d1-0a7d-42f0-a673-81ce39c4aae6", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 641, "WriteXsiType", 1, mapping.Namespace, "mapping.Namespace");
                            WriteXsiType(mapping.TypeName!, mapping.Namespace);
}                        }
                    }
                }
                else {global::AutoRepairTracing.TraceLogger.Log("6356e419-03ca-4b2a-b4f5-c823aad47b82", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 645, "Else Branch");
if (xmlnsSource != null)
                {
global::AutoRepairTracing.TraceLogger.Log("2d0bada5-f1ee-4bd1-bf6f-9787d8c37ad2", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 646, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("716a2b6d-a451-48f5-a6b1-c29996083e78", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 647, "WriteNamespaceDeclarations", 0, xmlnsSource, "xmlnsSource");
                    WriteNamespaceDeclarations(xmlnsSource);
}                }
}{int __loopCounter_07a6f2b0 = 0;global::AutoRepairTracing.TraceLogger.Log("e8a1c346-9c9b-4d6c-a430-cc56706dae38", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1059, "Variable Update", __loopCounter_07a6f2b0, "__loopCounter_07a6f2b0");

                for (int i = 0; i < members.Length; global::AutoRepairTracing.TraceLogger.LogValue("726b415e-589a-42d8-9789-64aa31ccda0e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1060, "Variable Update", i++, "i"))
                {
global::AutoRepairTracing.TraceLogger.Log("b8bba5c3-6534-4b7e-a534-4ea16edaa43b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 650, "For Loop");
global::AutoRepairTracing.TraceLogger.LogValue("0ace9d31-aff3-4713-96d4-5d77bddab4c4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1063, "Variable Update", __loopCounter_07a6f2b0++, "__loopCounter_07a6f2b0");if(__loopCounter_07a6f2b0 > 10000){global::AutoRepairTracing.TraceLogger.Log("ff36c486-057c-4e66-b8fc-f16323bbd8ed", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 665, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("07a6f2b0-bb87-491f-b4e2-fb34a7e12ffc", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 650, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_07a6f2b0, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("63e17c1b-eb0c-4ab2-86bd-0764e03ee1ff", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 665, "Break");
break;}}                    MemberMapping m = members[i];
global::AutoRepairTracing.TraceLogger.Log("b55284a8-83f8-481e-a5a8-daecea6ef3ad", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 652, "Variable Update", m, "m");

                    bool isSpecified = true;
global::AutoRepairTracing.TraceLogger.Log("8ec98544-b6f6-4064-93fe-1cb3887fb6c3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 654, "Variable Update", isSpecified, "isSpecified");
                    bool shouldPersist = true;
global::AutoRepairTracing.TraceLogger.Log("2e9e4dc6-17db-4514-9d82-3e6fc3c2d895", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 655, "Variable Update", shouldPersist, "shouldPersist");
                    if (m.CheckSpecified != SpecifiedAccessor.None)
                    {
global::AutoRepairTracing.TraceLogger.Log("589a2241-736a-4d4d-8b98-6eeb5661a419", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 657, "If-True Branch");
                        string specifiedMemberName = $"{m.Name}Specified";
global::AutoRepairTracing.TraceLogger.Log("bae8ddf1-efb4-44f5-a25a-dcfc0fc5fea7", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 658, "Variable Update", specifiedMemberName, "specifiedMemberName");
global::AutoRepairTracing.TraceLogger.LogValue("baee484b-cadc-4104-937c-712aacf0de92", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1073, "Variable Update", isSpecified = (bool)GetMemberValue(o!, specifiedMemberName)!, "isSpecified");
                    }

                    if (m.CheckShouldPersist)
                    {
global::AutoRepairTracing.TraceLogger.Log("11b88774-94e0-48d9-a762-24d575ddf8f1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 663, "If-True Branch");
                        string methodInvoke = $"ShouldSerialize{m.Name}";
global::AutoRepairTracing.TraceLogger.Log("ce1f0ef2-34b0-4664-9a13-b9dbd3ae2ebc", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 664, "Variable Update", methodInvoke, "methodInvoke");
                        MethodInfo method = o!.GetType().GetMethod(methodInvoke, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)!;
global::AutoRepairTracing.TraceLogger.Log("c6166216-13a2-4de7-a9b1-b4d8a7e3aa9e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 665, "Variable Update", method, "method");
global::AutoRepairTracing.TraceLogger.LogValue("c1fcfc95-f86d-4c20-942f-5b741c84c9bf", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1081, "Variable Update", shouldPersist = (bool)method.Invoke(o, Array.Empty<object>())!, "shouldPersist");
                    }

                    if (m.Attribute != null)
                    {
global::AutoRepairTracing.TraceLogger.Log("868dcca7-4fdb-421a-8e69-5333af428820", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 670, "If-True Branch");
                        if (isSpecified && shouldPersist)
                        {
global::AutoRepairTracing.TraceLogger.Log("887ff6e2-4417-45b5-8b58-46823c86572f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 672, "If-True Branch");
                            object? memberValue = GetMemberValue(o!, m.Name);
global::AutoRepairTracing.TraceLogger.Log("fb7c1f68-8a1b-4089-ab60-b4d608d880e9", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 673, "Variable Update", memberValue, "memberValue");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d58a67d6-d983-47f6-bfe3-2cded580d039", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 674, "WriteMember", 0, memberValue, "memberValue");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("0ad2a301-8182-4ea6-b5bc-a350dd2e46a2", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 674, "WriteMember", 1, m.Attribute, "m.Attribute");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("0a2dcc67-3854-4a66-b64d-e990e0e067a4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 674, "WriteMember", 2, m.TypeDesc!, "m.TypeDesc!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("88b065d5-ee19-49a5-b104-40ab28273fd1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 674, "WriteMember", 3, o, "o");
                            WriteMember(memberValue, m.Attribute, m.TypeDesc!, o);
}                        }
                    }
                }
}{int __loopCounter_04ab43e3 = 0;global::AutoRepairTracing.TraceLogger.Log("7b48dfad-23dc-449a-98bb-bf3810650ac6", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1099, "Variable Update", __loopCounter_04ab43e3, "__loopCounter_04ab43e3");

                for (int i = 0; i < members.Length; global::AutoRepairTracing.TraceLogger.LogValue("b3cf2bf1-c701-4c16-9302-03f943d3c9fc", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1100, "Variable Update", i++, "i"))
                {
global::AutoRepairTracing.TraceLogger.Log("e7b167ae-3f08-4c00-be30-183a9b804510", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 679, "For Loop");
global::AutoRepairTracing.TraceLogger.LogValue("16a4ec96-316d-45ec-b9fd-345929a164e8", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1103, "Variable Update", __loopCounter_04ab43e3++, "__loopCounter_04ab43e3");if(__loopCounter_04ab43e3 > 10000){global::AutoRepairTracing.TraceLogger.Log("facdff23-7326-4cda-b423-26f22aaa48e4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 694, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("04ab43e3-2743-4f82-b542-4681bde5e871", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 679, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_04ab43e3, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("dd7fe057-e5a3-4402-ab8f-28ad4f27be6b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 694, "Break");
break;}}                    MemberMapping m = members[i];
global::AutoRepairTracing.TraceLogger.Log("4e9cd7cc-d152-466a-a58f-e6b4abe9ac7b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 681, "Variable Update", m, "m");

                    if (m.Xmlns != null)
{global::AutoRepairTracing.TraceLogger.Log("6673e07a-8ca5-4d44-81af-ffcf246a8fd2", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 684, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("6dbbbf11-2ed7-4353-beb7-039cb56cfa54", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 684, "Continue");
                        continue;
}
                    bool isSpecified = true;
global::AutoRepairTracing.TraceLogger.Log("1b094c3b-2ff1-49d7-a340-0455f471d384", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 686, "Variable Update", isSpecified, "isSpecified");
                    bool shouldPersist = true;
global::AutoRepairTracing.TraceLogger.Log("fd881c36-b512-4374-ac76-23444bc7343c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 687, "Variable Update", shouldPersist, "shouldPersist");
                    if (m.CheckSpecified != SpecifiedAccessor.None)
                    {
global::AutoRepairTracing.TraceLogger.Log("2ac48f0e-57bc-47d5-bd83-7cb27dddf6d9", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 689, "If-True Branch");
                        string specifiedMemberName = $"{m.Name}Specified";
global::AutoRepairTracing.TraceLogger.Log("ad8b67e8-3a47-479e-8bb0-44f6f852ddc4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 690, "Variable Update", specifiedMemberName, "specifiedMemberName");
global::AutoRepairTracing.TraceLogger.LogValue("e074c70a-d0ab-43ea-a8a9-3b33187f6b23", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1118, "Variable Update", isSpecified = (bool)GetMemberValue(o!, specifiedMemberName)!, "isSpecified");
                    }

                    if (m.CheckShouldPersist)
                    {
global::AutoRepairTracing.TraceLogger.Log("07c87799-c185-46fd-b749-62f3b89afc11", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 695, "If-True Branch");
                        string methodInvoke = $"ShouldSerialize{m.Name}";
global::AutoRepairTracing.TraceLogger.Log("11966b30-a880-4e47-9ff6-082f687887ef", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 696, "Variable Update", methodInvoke, "methodInvoke");
                        MethodInfo method = o!.GetType().GetMethod(methodInvoke, BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.DeclaredOnly)!;
global::AutoRepairTracing.TraceLogger.Log("07195224-a145-45fb-b652-4b3bbcacd764", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 697, "Variable Update", method, "method");
global::AutoRepairTracing.TraceLogger.LogValue("bef018bd-a4b3-49b1-8a80-4821aa546a2c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1126, "Variable Update", shouldPersist = (bool)method.Invoke(o, Array.Empty<object>())!, "shouldPersist");
                    }

                    bool checkShouldPersist = m.CheckShouldPersist && (m.Elements!.Length > 0 || m.Text != null);
global::AutoRepairTracing.TraceLogger.Log("02c33345-73b5-4047-9494-37679785c516", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 701, "Variable Update", checkShouldPersist, "checkShouldPersist");

                    if (!checkShouldPersist)
                    {
global::AutoRepairTracing.TraceLogger.Log("b2daec11-25d9-4bd2-ba8c-ea727e398fd3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 704, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("8af0916a-ba32-4937-a72f-627ed2dabe8f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1134, "Variable Update", shouldPersist = true, "shouldPersist");
                    }

                    if (isSpecified && shouldPersist)
                    {
global::AutoRepairTracing.TraceLogger.Log("49c50adc-55bc-4236-8fda-510bcafb0dd4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 709, "If-True Branch");
                        object? choiceSource = null;
global::AutoRepairTracing.TraceLogger.Log("0692098d-82b1-4839-8804-018af90b3fab", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 710, "Variable Update", choiceSource, "choiceSource");
                        if (m.ChoiceIdentifier != null)
                        {
global::AutoRepairTracing.TraceLogger.Log("8aa3ec08-f28b-44ad-b77f-2747a62fcc3c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 712, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("ab85c458-025f-492a-b0a7-bfed47ec41eb", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1144, "Variable Update", choiceSource = GetMemberValue(o!, m.ChoiceIdentifier.MemberName!), "choiceSource");
                        }

                        object? memberValue = GetMemberValue(o!, m.Name);
global::AutoRepairTracing.TraceLogger.Log("dae664c7-ef8e-4503-a375-f526a3e4c245", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 716, "Variable Update", memberValue, "memberValue");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("88e69b8b-001d-4993-a555-40f9a11fd37c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 717, "WriteMember", 0, memberValue, "memberValue");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("f2fa8bf2-34c7-4fac-98d0-542714603676", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 717, "WriteMember", 1, choiceSource, "choiceSource");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("4bcfa3bf-ad3e-41f7-9c4b-3f083284be8f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 717, "WriteMember", 2, m.ElementsSortedByDerivation!, "m.ElementsSortedByDerivation!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("3fbcc813-7dc5-41a5-8539-81a88aa31edb", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 717, "WriteMember", 3, m.Text, "m.Text");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("5b8d86fb-b078-4157-a889-2e5817a141a1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 717, "WriteMember", 4, m.ChoiceIdentifier, "m.ChoiceIdentifier");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("9cb56eac-7dd1-4ce4-aec4-010805009db0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 717, "WriteMember", 5, m.TypeDesc!, "m.TypeDesc!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("e11bd471-4042-4591-8314-1fa51d5dc5cd", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 717, "WriteMember", 6, true, "true");
                        WriteMember(memberValue, choiceSource, m.ElementsSortedByDerivation!, m.Text, m.ChoiceIdentifier, m.TypeDesc!, true);
}                    }
                }
}
                if (!mapping.IsSoap)
                {
global::AutoRepairTracing.TraceLogger.Log("5c71d2b8-be01-48df-b064-5430a2472eae", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 722, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("72969fdc-f5a7-4fe0-9e4f-d1cc2702cd4f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 723, "WriteEndElement", 0, o, "o");
                    WriteEndElement(o);
}                }
            }
        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("e6509dca-a759-43d0-a639-be30530f723e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 582, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("6eb636d8-37b5-4db7-9d87-0997294df16e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 740, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("6b506feb-43c8-42e5-b8c9-9e829a077471", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 582, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("b02c2dff-9cfa-4e03-949d-2142c4f78a22", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 582, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("fb394fc9-24de-4d98-8bd9-68ee56f52f4f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 582, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private static object? GetMemberValue(object o, string memberName)
{global::AutoRepairTracing.TraceLogger.Log("2ed65db2-fd4d-4d56-8091-4fd2dbdf3d5a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 728, "Enter Method 'GetMemberValue'");
try        {
            MemberInfo memberInfo = ReflectionXmlSerializationHelper.GetEffectiveGetInfo(o.GetType(), memberName);
global::AutoRepairTracing.TraceLogger.Log("0004321b-6080-4354-a06b-2d08f12edb69", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 730, "Variable Update", memberInfo, "memberInfo");
            object? memberValue = GetMemberValue(o, memberInfo);
global::AutoRepairTracing.TraceLogger.Log("36c8ac32-6450-4507-9e62-1baca2437581", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 731, "Variable Update", memberValue, "memberValue");
{global::AutoRepairTracing.TraceLogger.Log("56043e2c-596f-4935-ab3f-ce510b933987", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 732, "Return");
            return memberValue;
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("321e697e-b355-483c-b667-fffcd3163002", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 728, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("7eead845-544e-4d8b-ae7a-4b95e5437806", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 748, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("0292937d-34b0-4a92-afb2-9680bdcbeda9", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 728, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("a78b74a5-c928-4e0a-838b-72f30f9d3eb8", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 728, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("8be86627-07a1-43da-af2c-81c0ceb2713e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 728, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private bool WriteEnumAndArrayTypes(object o, string n, string? ns)
{global::AutoRepairTracing.TraceLogger.Log("388f4183-f929-42a5-85b7-b5477e0370b8", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 735, "Enter Method 'WriteEnumAndArrayTypes'");
try        {
            Type objType = o.GetType();
global::AutoRepairTracing.TraceLogger.Log("f94a0dc2-6e37-4ccc-9894-0d9994959bf0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 737, "Variable Update", objType, "objType");

            foreach (var m in _mapping.Scope!.TypeMappings)
            {
global::AutoRepairTracing.TraceLogger.Log("72f77339-95af-4f9b-af84-ce3809accbbc", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 739, "ForEach Loop");
                if (m is EnumMapping em && em.TypeDesc!.Type == objType)
                {
global::AutoRepairTracing.TraceLogger.Log("694a04f9-513e-4be8-ae0a-29b4ebc6f546", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 742, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("bdae0d1f-7af8-4780-84c6-671d78121490", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 743, "WriteStartElement", 0, n, "n");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("7d2bcb03-c144-4a0a-8bbc-bd65496f770c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 743, "WriteStartElement", 1, ns, "ns");
                    Writer.WriteStartElement(n, ns);
}{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d5f6e5fd-1779-4717-b589-81711d102d61", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 744, "WriteXsiType", 0, em.TypeName!, "em.TypeName!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("5232f01e-5a2c-424f-8524-1ed886848411", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 744, "WriteXsiType", 1, ns, "ns");
                    WriteXsiType(em.TypeName!, ns);
}{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d22ac4b8-b035-4bff-8f7e-eb0b4f7a8e62", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 745, "WriteString", 0, WriteEnumMethod(em, o), "WriteEnumMethod(em, o)");
                    Writer.WriteString(WriteEnumMethod(em, o));
}                    Writer.WriteEndElement();
{global::AutoRepairTracing.TraceLogger.Log("5b3e13d9-3c56-4ffb-aa09-adbfa49cecef", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 747, "Return");
                    return true;
}                }

                if (m is ArrayMapping am && am.TypeDesc!.Type == objType)
                {
global::AutoRepairTracing.TraceLogger.Log("29eccd58-f9cd-4404-9bc7-92915af4e9bf", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 751, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("882a1976-3700-45a7-a973-dda78aaae4c3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 752, "WriteStartElement", 0, n, "n");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("a3277f5d-ab6d-4799-823f-c58dd5b66730", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 752, "WriteStartElement", 1, ns, "ns");
                    Writer.WriteStartElement(n, ns);
}{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("bbc79ed1-b08b-41dc-9ef4-6cb0875f5131", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 753, "WriteXsiType", 0, am.TypeName!, "am.TypeName!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("74e8656c-80d3-4e79-ba25-923480bdf548", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 753, "WriteXsiType", 1, ns, "ns");
                    WriteXsiType(am.TypeName!, ns);
}{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("c31e552f-1bcf-41cb-9a6b-9e497def0a01", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 754, "WriteMember", 0, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("058a429f-3d43-411b-bd50-da4f55abd435", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 754, "WriteMember", 1, null, "null");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("4a1fd1bd-a138-4260-bc73-53769964cdb0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 754, "WriteMember", 2, am.ElementsSortedByDerivation!, "am.ElementsSortedByDerivation!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("c0a7e716-79ff-4242-b1a2-c86cc0f15acc", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 754, "WriteMember", 3, null, "null");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("1d7cb0dc-ca2b-46bc-97b6-7f3630c6ed34", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 754, "WriteMember", 4, null, "null");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("dfc81554-58a2-4044-9b30-1ab6e07d7030", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 754, "WriteMember", 5, am.TypeDesc!, "am.TypeDesc!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("f6425584-e746-457b-83d9-acadbaee489d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 754, "WriteMember", 6, true, "true");
                    WriteMember(o, null, am.ElementsSortedByDerivation!, null, null, am.TypeDesc!, true);
}                    Writer.WriteEndElement();
{global::AutoRepairTracing.TraceLogger.Log("9198ad89-ec09-41fb-92de-6248abcdd9f1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 756, "Return");
                    return true;
}                }
            }
{global::AutoRepairTracing.TraceLogger.Log("480e1e39-118d-4591-90ec-b40870eb1c29", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 760, "Return");

            return false;
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("d469e199-d2be-4919-8bad-caf186c3e85e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 735, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("bb99d877-0eb5-4ee5-93f4-2ac4feee2eb9", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 777, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("d53dc6b8-92a4-4c15-b4fb-50e834980a24", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 735, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("0790fc3f-db1b-4ca6-a0f3-6624537b479d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 735, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("2a3ea39b-69ca-4024-997c-7174b6993f04", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 735, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private string? WriteEnumMethod(EnumMapping mapping, object v)
{global::AutoRepairTracing.TraceLogger.Log("459a4362-8113-42c3-91de-473c085c0834", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 763, "Enter Method 'WriteEnumMethod'");
try        {
            string? returnString = null;
global::AutoRepairTracing.TraceLogger.Log("532ccb80-8549-4a49-9d59-6430ad1bbda7", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 765, "Variable Update", returnString, "returnString");
global::AutoRepairTracing.TraceLogger.Log("dfaddeb2-0772-47a4-8cbc-ba51592769d4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 765, "Null Assignment Warning", "null", "returnString");            if (mapping != null)
            {
global::AutoRepairTracing.TraceLogger.Log("5c81adc7-ebdf-4564-a4e9-f26f83fb6c26", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 767, "If-True Branch");
                ConstantMapping[] constants = mapping.Constants!;
global::AutoRepairTracing.TraceLogger.Log("ce9f67bd-88c2-4f0a-9242-88f518380dcc", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 768, "Heap State: constants (ConstantMapping[])");global::AutoRepairTracing.TraceLogger.Log("7254c94a-c621-4bb4-9b23-15600cf515ad", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 768, "Variable Update", constants, "constants");
                if (constants.Length > 0)
                {
global::AutoRepairTracing.TraceLogger.Log("63ab4e93-7bf6-4526-8e3b-55d2ec1546ae", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 770, "If-True Branch");
                    bool foundValue = false;
global::AutoRepairTracing.TraceLogger.Log("e1293031-7270-400c-a7b6-9e203a8fe72a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 771, "Variable Update", foundValue, "foundValue");
                    var enumValue = Convert.ToInt64(v);
global::AutoRepairTracing.TraceLogger.Log("0aad9904-f3cf-4393-8075-6aa98881471a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 772, "Variable Update", enumValue, "enumValue");
{int __loopCounter_8ca7a9c3 = 0;global::AutoRepairTracing.TraceLogger.Log("2fe9554f-2e87-4bb8-b9ff-10af13435a3c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1247, "Variable Update", __loopCounter_8ca7a9c3, "__loopCounter_8ca7a9c3");
                    for (int i = 0; i < constants.Length; global::AutoRepairTracing.TraceLogger.LogValue("d778b839-ee1a-4052-9dad-9f1c1293cf5f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1247, "Variable Update", i++, "i"))
                    {
global::AutoRepairTracing.TraceLogger.Log("5087e77a-2655-455f-ab04-b6581b382b89", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 773, "For Loop");
global::AutoRepairTracing.TraceLogger.LogValue("e437780d-53ee-424e-a01c-c72c4ef3dffb", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1250, "Variable Update", __loopCounter_8ca7a9c3++, "__loopCounter_8ca7a9c3");if(__loopCounter_8ca7a9c3 > 10000){global::AutoRepairTracing.TraceLogger.Log("8661ade5-1693-46f3-80a9-d9310f449f9e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 791, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("8ca7a9c3-37b7-4a86-b35f-e11bfc47025b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 773, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_8ca7a9c3, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("93c97757-433e-47bd-ab9f-8519a2b59e85", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 791, "Break");
break;}}                        ConstantMapping c = constants[i];
global::AutoRepairTracing.TraceLogger.Log("1999be0c-21eb-41d8-86ae-cbdea948ba74", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 775, "Variable Update", c, "c");
                        if (enumValue == c.Value)
                        {
global::AutoRepairTracing.TraceLogger.Log("eaeecd37-e824-4cfd-a6f7-b4bc491bbd80", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 777, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("bd88e721-ec17-4c97-a31f-93346b1a4aec", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1256, "Variable Update", returnString = c.XmlName, "returnString");
global::AutoRepairTracing.TraceLogger.LogValue("8603cea8-dd31-45cb-8c3f-c8a3068813cd", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1257, "Variable Update", foundValue = true, "foundValue");
{global::AutoRepairTracing.TraceLogger.Log("616e8656-eb31-48a7-8421-6956d73bdba2", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 780, "Break");
                            break;
}                        }
                    }
}
                    if (!foundValue)
                    {
global::AutoRepairTracing.TraceLogger.Log("600f1b00-b0eb-46dc-a08c-7013fd0af359", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 785, "If-True Branch");
                        if (mapping.IsFlags)
                        {
global::AutoRepairTracing.TraceLogger.Log("27eb10e1-27ee-419e-9b3d-c3565b364ad0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 787, "If-True Branch");
                            string[] xmlNames = new string[constants.Length];
global::AutoRepairTracing.TraceLogger.Log("54f4dca0-8e65-4a70-9401-768afed09efb", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 788, "Heap State: xmlNames (string[] size=constants.Length)");global::AutoRepairTracing.TraceLogger.Log("27c500e5-5cac-4f9e-ae21-2344216964de", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 788, "Variable Update", xmlNames, "xmlNames");
                            long[] valueIds = new long[constants.Length];
global::AutoRepairTracing.TraceLogger.Log("9599d53c-1225-4293-b3ea-7f96208eb6c6", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 789, "Heap State: valueIds (long[] size=constants.Length)");global::AutoRepairTracing.TraceLogger.Log("d979e4d3-eec8-48c1-8a07-e422f028a860", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 789, "Variable Update", valueIds, "valueIds");
{int __loopCounter_250fac5c = 0;global::AutoRepairTracing.TraceLogger.Log("6252f143-58ae-4664-9bd3-8e5316f430a7", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1271, "Variable Update", __loopCounter_250fac5c, "__loopCounter_250fac5c");

                            for (int i = 0; i < constants.Length; global::AutoRepairTracing.TraceLogger.LogValue("ac7d5c2b-a25a-4dca-9008-f9069eb6369c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1272, "Variable Update", i++, "i"))
                            {
global::AutoRepairTracing.TraceLogger.Log("5efb567c-96f7-46ea-b4f1-b98da9e017e2", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 791, "For Loop");
global::AutoRepairTracing.TraceLogger.LogValue("a87da033-bf10-4140-bfb5-8e2c619d0227", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1275, "Variable Update", __loopCounter_250fac5c++, "__loopCounter_250fac5c");if(__loopCounter_250fac5c > 10000){global::AutoRepairTracing.TraceLogger.Log("6aba5de3-fb9b-4882-9fbe-878419983bc2", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 809, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("250fac5c-60a5-4ec2-a582-8a05327aaf65", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 791, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_250fac5c, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("533c9149-f8bf-4611-99cd-86f5d9279eef", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 809, "Break");
break;}}{global::AutoRepairTracing.TraceLogger.Log("89eccbc2-f34d-4f4a-9d36-c7f5543c59d5", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 793, "Variable Update", i, "index:xmlNames");
global::AutoRepairTracing.TraceLogger.Log("83ff1909-3e6a-4ba9-80f0-164942756735", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 793, "Variable Update", i, "index:constants");
global::AutoRepairTracing.TraceLogger.LogValue("14f9ba11-cf0a-412c-9591-4ee735f0c345", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1277, "Variable Update", xmlNames[i] = constants[i].XmlName, string.Concat("xmlNames[", i, "]"));
global::AutoRepairTracing.TraceLogger.LogArray("7d884b71-64e8-458d-99ed-c2fda9649b54", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 793, "Array Snapshot", xmlNames, "xmlNames");
}{global::AutoRepairTracing.TraceLogger.Log("40e06711-d261-445b-ad39-097d3988bae3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 794, "Variable Update", i, "index:valueIds");
global::AutoRepairTracing.TraceLogger.Log("c67fad0a-0d21-4fc0-86ad-51ac0c0c6b4b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 794, "Variable Update", i, "index:constants");
global::AutoRepairTracing.TraceLogger.LogValue("792c057b-42b9-41f3-90bf-bad492aac75d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1278, "Variable Update", valueIds[i] = constants[i].Value, string.Concat("valueIds[", i, "]"));
global::AutoRepairTracing.TraceLogger.LogArray("8c448ef5-6a6e-4b71-970e-d6844e2ce8c9", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 794, "Array Snapshot", valueIds, "valueIds");
}                            }
}global::AutoRepairTracing.TraceLogger.LogValue("17832642-9e31-4f3a-a16e-0960ad51128f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1281, "Variable Update", returnString = FromEnum(enumValue, xmlNames, valueIds), "returnString");
                        }
                        else
                        {
global::AutoRepairTracing.TraceLogger.Log("bcf76b38-5b99-4d57-a326-f018581e174c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 800, "Else Branch");
                            throw CreateInvalidEnumValueException(v, mapping.TypeDesc!.FullName);
                        }
                    }
                }
            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("a8a06b5a-1a3c-4671-a063-8cd4203babb3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 807, "Else Branch");
global::AutoRepairTracing.TraceLogger.LogValue("5612c1b3-2b58-4692-868c-698604ba0f89", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1294, "Variable Update", returnString = v.ToString(), "returnString");
            }

            if (mapping!.IsSoap)
            {
global::AutoRepairTracing.TraceLogger.Log("1b4c442a-81ad-4a9f-8e9a-be079c9b5414", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 812, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("2d566d1e-327b-4f7a-aa7a-c6a037d418e1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 813, "WriteXsiType", 0, mapping.TypeName!, "mapping.TypeName!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("9c2e562a-1626-40fd-956f-f5c5e1d669e4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 813, "WriteXsiType", 1, mapping.Namespace, "mapping.Namespace");
                WriteXsiType(mapping.TypeName!, mapping.Namespace);
}{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d52afcdf-de96-4fe3-8601-cc98173b1d0b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 814, "WriteString", 0, returnString, "returnString");
                Writer.WriteString(returnString);
}{global::AutoRepairTracing.TraceLogger.Log("9010a8f2-33c0-43a9-aa3c-9c2603d66b99", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 815, "Return");
                return null;
}            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("8a91e1f8-3497-4834-b7bd-5a47b08e0a0c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 818, "Else Branch");
{global::AutoRepairTracing.TraceLogger.Log("8a5fc3fd-ad84-4a3c-afd7-7846012568fb", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 819, "Return");
                return returnString;
}            }
        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("1d5c8a98-0a1e-4dc9-810c-12d8d4dd5a28", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 763, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("eb7e2998-1b35-49c4-a7ac-562b91d11ff6", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 838, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("aeb4521c-9e08-486f-a023-ee78b53d23f0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 763, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("a2b90586-dcf2-451b-b4f7-bc3c0656b540", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 763, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("199db686-1a29-447d-90a8-faace92b468a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 763, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private static object? GetMemberValue(object? o, MemberInfo memberInfo)
{global::AutoRepairTracing.TraceLogger.Log("b1e16b1e-383f-427e-b47a-b3009b8e1af5", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 823, "Enter Method 'GetMemberValue'");
try        {
            if (memberInfo is PropertyInfo memberProperty)
            {
global::AutoRepairTracing.TraceLogger.Log("c3bd2b43-6f9f-4830-912f-34999b1edaf0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 826, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("eb266cd0-7cdf-439e-bd88-79c9ded55da5", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 827, "Return");
                return memberProperty.GetValue(o);
}            }
            else {global::AutoRepairTracing.TraceLogger.Log("d4807218-3632-48a9-b7ce-43775b7662ae", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 829, "Else Branch");
if (memberInfo is FieldInfo memberField)
            {
global::AutoRepairTracing.TraceLogger.Log("f875e01d-90ee-4ff0-9b2b-979e20ba7d58", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 830, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("e6b79027-4a05-4329-9556-93c942a4dcdf", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 831, "Return");
                return memberField.GetValue(o);
}            }
}
            throw new InvalidOperationException(SR.XmlInternalError);
        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("2142899a-dc78-4c8a-a788-2ea42246d74d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 823, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("9538382d-b8d0-4808-810a-98a1f41cd0a6", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 853, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("484f7911-1c76-4321-8f56-63d8ae7a9148", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 823, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("a341c846-c090-447d-9850-8ef18d19a187", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 823, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("33952a88-398b-42bf-aa2a-2fb6398bc839", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 823, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private void WriteMember(object? memberValue, AttributeAccessor attribute, TypeDesc memberTypeDesc, object? container)
{global::AutoRepairTracing.TraceLogger.Log("2b1352ea-3998-4886-a034-9d62f98e4b30", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 837, "Enter Method 'WriteMember'");
try        {
            if (memberTypeDesc.IsAbstract) {global::AutoRepairTracing.TraceLogger.Log("0e3bf582-2365-4b09-a9f2-c0affb340d28", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 839, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("33b259b0-1efc-43e3-892c-69041138adf3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 839, "Return");
return;
}            if (memberTypeDesc.IsArrayLike)
            {
global::AutoRepairTracing.TraceLogger.Log("30837325-2ffb-488f-9c1d-6c4a9d2dba21", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 841, "If-True Branch");
                var sb = new StringBuilder();
global::AutoRepairTracing.TraceLogger.Log("4de1d9aa-1e94-4e2b-87d3-4e29a6220d1f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 842, "Variable Update", sb, "sb");
                TypeDesc? arrayElementTypeDesc = memberTypeDesc.ArrayElementTypeDesc;
global::AutoRepairTracing.TraceLogger.Log("290e41da-9d72-492a-ba6d-4b9062b8522d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 843, "Variable Update", arrayElementTypeDesc, "arrayElementTypeDesc");
                bool canOptimizeWriteListSequence = CanOptimizeWriteListSequence(arrayElementTypeDesc);
global::AutoRepairTracing.TraceLogger.Log("35d9c36e-143b-46b1-b122-6b14756fb898", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 844, "Variable Update", canOptimizeWriteListSequence, "canOptimizeWriteListSequence");
                if (attribute.IsList)
                {
global::AutoRepairTracing.TraceLogger.Log("cc3aaa14-e72a-4948-b92e-e7b297ef1667", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 846, "If-True Branch");
                    if (canOptimizeWriteListSequence)
                    {
global::AutoRepairTracing.TraceLogger.Log("8e9b9546-9568-4db4-a1e8-7fbc7183c1a4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 848, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("4ed65bf1-9480-4fc0-9c84-0b8189d40b97", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 849, "WriteStartAttribute", 0, null, "null");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("e5d658ac-ad0e-4142-a0ed-0cf488e8144e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 849, "WriteStartAttribute", 1, attribute.Name, "attribute.Name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("b75e6fe2-3cbe-464a-9860-ede5d5022338", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 849, "WriteStartAttribute", 2, attribute.Form == XmlSchemaForm.Qualified ? attribute.Namespace : string.Empty, "attribute.Form == XmlSchemaForm.Qualified ? attribute.Namespace : string.Empty");
                        Writer.WriteStartAttribute(null, attribute.Name, attribute.Form == XmlSchemaForm.Qualified ? attribute.Namespace : string.Empty);
}                    }
                }

                if (memberValue != null)
                {
global::AutoRepairTracing.TraceLogger.Log("1d2b139d-b9f9-4b70-a5f6-ceb2de860c25", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 854, "If-True Branch");
                    var a = (IEnumerable)global::AutoRepairTracing.TraceLogger.LogCastSource("b8cccd8f-a148-4291-8743-7cf80a5726bb", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1458, (object)(memberValue), "IEnumerable");
global::AutoRepairTracing.TraceLogger.Log("886a98ae-b633-4ccf-9b35-1f3b773b1ca0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 855, "Variable Update", a, "a");
                    IEnumerator e = a.GetEnumerator();
global::AutoRepairTracing.TraceLogger.Log("d995dfaa-73ec-498a-939e-31374fcd146d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 856, "Variable Update", e, "e");
                    bool shouldAppendWhitespace = false;
global::AutoRepairTracing.TraceLogger.Log("b619dda5-0405-4e58-a2d4-e8f2271a8212", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 857, "Variable Update", shouldAppendWhitespace, "shouldAppendWhitespace");
                    if (e != null)
                    {
global::AutoRepairTracing.TraceLogger.Log("cfb20a0d-9018-4fc7-a02f-8c48715b22d7", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 859, "If-True Branch");
{int __loopCounter_0f21a2c3 = 0;global::AutoRepairTracing.TraceLogger.Log("7afca2fc-7cd6-427e-9a91-f99574125457", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1374, "Variable Update", __loopCounter_0f21a2c3, "__loopCounter_0f21a2c3");
                        while (e.MoveNext())
                        {
global::AutoRepairTracing.TraceLogger.Log("5d7461cb-58d9-49de-9b12-40d9291b36e9", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 860, "While Loop");
global::AutoRepairTracing.TraceLogger.LogValue("2c70e551-9a63-4abf-93fc-b76d7a4e1052", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1377, "Variable Update", __loopCounter_0f21a2c3++, "__loopCounter_0f21a2c3");if(__loopCounter_0f21a2c3 > 10000){global::AutoRepairTracing.TraceLogger.Log("453a0e0e-ba36-488e-bc68-07ce05320721", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 880, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("0f21a2c3-0c32-4593-8b63-73f8d98b7ae5", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 860, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_0f21a2c3, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("159d212c-e39a-486f-8385-eb644154eea7", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 880, "Break");
break;}}                            object ai = e.Current;
global::AutoRepairTracing.TraceLogger.Log("73f1df0c-2e97-42d5-81c7-da4a9f469ff9", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 862, "Variable Update", ai, "ai");

                            if (attribute.IsList)
                            {
global::AutoRepairTracing.TraceLogger.Log("9851264c-80de-438e-b39e-7ed6a127534e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 865, "If-True Branch");
                                string? stringValue;
                                if (attribute.Mapping is EnumMapping enumMapping)
                                {
global::AutoRepairTracing.TraceLogger.Log("457732a0-970e-4199-a2cb-60123ff5051a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 868, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("e9a96dcc-2684-4cfc-b9ff-f77ac18c33e3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1388, "Variable Update", stringValue = WriteEnumMethod(enumMapping, ai), "stringValue");
                                }
                                else
                                {
global::AutoRepairTracing.TraceLogger.Log("64d82a22-5a1a-421f-b731-ac8722e6802b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 872, "Else Branch");
                                    if (!WritePrimitiveValue(arrayElementTypeDesc!, ai, out stringValue))
                                    {
global::AutoRepairTracing.TraceLogger.Log("2a619708-6955-47cb-a6c7-f77eb570dc17", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 874, "If-True Branch");
                                        Debug.Assert(ai is byte[]);
                                    }
                                }

                                // check to see if we can write values of the attribute sequentially
                                if (canOptimizeWriteListSequence)
                                {
global::AutoRepairTracing.TraceLogger.Log("29eba16b-2b6a-492f-baef-e7a7369f6237", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 881, "If-True Branch");
                                    if (shouldAppendWhitespace)
                                    {
global::AutoRepairTracing.TraceLogger.Log("11ebdc1f-6576-4d08-89c3-e3422af5c5f4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 883, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d7d4ed2c-04f8-41b9-9dd8-64e6deded3fe", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 884, "WriteString", 0, " ", "\" \"");
                                        Writer.WriteString(" ");
}                                    }

                                    if (ai is byte[])
                                    {
global::AutoRepairTracing.TraceLogger.Log("3ee0baf8-ca31-4f47-a726-69d427d604fb", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 888, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("5541de3d-bb53-4e7a-9804-129fd2381139", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 889, "WriteValue", 0, (byte[])global::AutoRepairTracing.TraceLogger.LogCastSource("d4e03ef1-ce87-4055-a1fa-98772122deb2", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1509, (object)(ai), "byte[]"), "(byte[])ai");
                                        WriteValue((byte[])global::AutoRepairTracing.TraceLogger.LogCastSource("653617fb-39ba-4dad-8d68-fce31b1e215e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1510, (object)(ai), "byte[]"));
}                                    }
                                    else
                                    {
global::AutoRepairTracing.TraceLogger.Log("930b9e16-78d3-4a60-a9ae-0b54d8e8a0ad", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 892, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("184a0e98-5e61-43bd-8213-15771174b8a7", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 893, "WriteValue", 0, stringValue, "stringValue");
                                        WriteValue(stringValue);
}                                    }
                                }
                                else
                                {
global::AutoRepairTracing.TraceLogger.Log("f0695821-7c57-4893-af25-4ee6b4862672", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 897, "Else Branch");
                                    if (shouldAppendWhitespace)
                                    {
global::AutoRepairTracing.TraceLogger.Log("0929735b-d60e-49f8-a715-1ec401488fe8", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 899, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("44647722-5939-4b22-838b-4dc7e4073acf", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 900, "Append", 0, ' ', "' '");
                                        sb.Append(' ');
}                                    }
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("ab14e370-8f6b-40a1-9bf1-5895cf164afe", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 903, "Append", 0, stringValue, "stringValue");

                                    sb.Append(stringValue);
}                                }
                            }
                            else
                            {
global::AutoRepairTracing.TraceLogger.Log("9778884f-28e4-4657-bc33-f27fd77f1d07", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 907, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("7dba86b8-d64f-4f56-97fd-e58a296e7471", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 908, "WriteAttribute", 0, ai, "ai");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("a3b6bd4c-00a6-4343-ab63-86dec9002021", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 908, "WriteAttribute", 1, attribute, "attribute");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("31ad08b6-4198-489d-8dde-a14e5a364fb5", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 908, "WriteAttribute", 2, container, "container");
                                WriteAttribute(ai, attribute, container);
}                            }
global::AutoRepairTracing.TraceLogger.LogValue("0049aadc-2e40-4b48-8e29-07282b37daa9", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1447, "Variable Update", shouldAppendWhitespace = true, "shouldAppendWhitespace");
                        }
}
                        if (attribute.IsList)
                        {
global::AutoRepairTracing.TraceLogger.Log("e2edac17-7ba3-4065-a575-3740940e8851", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 915, "If-True Branch");
                            // check to see if we can write values of the attribute sequentially
                            if (canOptimizeWriteListSequence)
                            {
global::AutoRepairTracing.TraceLogger.Log("9ba0e2b4-2936-417b-9396-85c545e2acb3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 918, "If-True Branch");
                                Writer.WriteEndAttribute();
                            }
                            else
                            {
global::AutoRepairTracing.TraceLogger.Log("f3cdc065-c21a-426f-90cb-32956c487581", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 922, "Else Branch");
                                if (sb.Length != 0)
                                {
global::AutoRepairTracing.TraceLogger.Log("3a74d851-df8d-41f3-95c8-8bfa3763ea85", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 924, "If-True Branch");
                                    string? ns = attribute.Form == XmlSchemaForm.Qualified ? attribute.Namespace : string.Empty;
global::AutoRepairTracing.TraceLogger.Log("f2de4a33-c19e-4b54-a874-901faed5fda8", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 925, "Variable Update", ns, "ns");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("35cbe133-3b2c-4070-b697-1796f0c6a7f5", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 926, "WriteAttribute", 0, attribute.Name, "attribute.Name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("777b0aa9-b208-4c88-afd8-a0dc70af9c08", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 926, "WriteAttribute", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("fcbbf94b-a63a-40bc-8d5d-ba11920b5f97", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 926, "WriteAttribute", 2, sb.ToString(), "sb.ToString()");
                                    WriteAttribute(attribute.Name, ns, sb.ToString());
}                                }
                            }
                        }
                    }
                }
            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("2d34defa-0088-4ec3-bbbf-1f32ea3803ed", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 934, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("c7f6f44d-e5fa-4764-a1b6-c55926c57ddf", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 935, "WriteAttribute", 0, memberValue!, "memberValue!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("e51097c2-645d-4466-aaed-50e19211dd1c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 935, "WriteAttribute", 1, attribute, "attribute");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("6947ee36-53ba-4a97-9dde-96681f16ced5", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 935, "WriteAttribute", 2, container, "container");
                WriteAttribute(memberValue!, attribute, container);
}            }
        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("749f851d-63f5-499d-a740-75b869ee5ef5", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 837, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("0d030d0b-71ce-4810-86c8-b7d2ad27da45", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 956, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("5cec288c-b8d8-4d6d-b673-110cc21d4dab", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 837, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("21c1845c-3088-47e0-a060-765e2b9e117b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 837, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("b6d8fe95-806a-4450-93cb-7b7e811e9651", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 837, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private static bool CanOptimizeWriteListSequence(TypeDesc? listElementTypeDesc)
{global::AutoRepairTracing.TraceLogger.Log("63967a9f-6007-448f-9773-31ef5377976f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 939, "Enter Method 'CanOptimizeWriteListSequence'");
try        {
{global::AutoRepairTracing.TraceLogger.Log("dcb52d2d-4693-4a96-b501-a5b62396313b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 944, "Return");
            // check to see if we can write values of the attribute sequentially
            // currently we have only one data type (XmlQualifiedName) that we can not write "inline",
            // because we need to output xmlns:qx="..." for each of the qnames
            return (listElementTypeDesc != null && listElementTypeDesc != ReflectionXmlSerializationReader.QnameTypeDesc);
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("48cd5c4b-c6fd-4ea0-8b36-86275e331aa7", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 939, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("6e4135fe-8152-4020-8240-42f789711fad", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 965, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("5a766ec8-0311-4876-bb9c-f82dea7103d8", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 939, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("bae7c8a3-5a8e-461e-8735-c3f50d0c76ce", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 939, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("57818626-f840-4331-bdd2-e7a3c8fdaa6a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 939, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private void WriteAttribute(object memberValue, AttributeAccessor attribute, object? container)
{global::AutoRepairTracing.TraceLogger.Log("b598122c-74df-4904-ab41-b49d0d4c0751", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 947, "Enter Method 'WriteAttribute'");
try        {
            // TODO: this block is never hit by our tests.
            if (attribute.Mapping is SpecialMapping special)
            {
global::AutoRepairTracing.TraceLogger.Log("75354046-fdba-4a0b-879b-7bb4c223aac1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 951, "If-True Branch");
                if (special.TypeDesc!.Kind == TypeKind.Attribute || special.TypeDesc.CanBeAttributeValue)
                {
global::AutoRepairTracing.TraceLogger.Log("282f83d5-9e90-4fbd-b9bc-885ce56d0f6e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 953, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("f5739a2c-9eb0-483e-8693-a7cfa6065bed", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 954, "WriteXmlAttribute", 0, (XmlNode)global::AutoRepairTracing.TraceLogger.LogCastSource("91545f5f-05ea-45a6-89a5-753372435fce", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1605, (object)(memberValue), "XmlNode"), "(XmlNode)memberValue");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("92f817ce-88cc-4b43-8108-2196e50d4bca", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 954, "WriteXmlAttribute", 1, container, "container");
                    WriteXmlAttribute((XmlNode)global::AutoRepairTracing.TraceLogger.LogCastSource("6e05f440-bde5-4c20-bd67-6a9111928dd5", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1607, (object)(memberValue), "XmlNode"), container);
}                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("9b916071-589b-4be7-93ab-6a97b32d74eb", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 957, "Else Branch");
                    throw new InvalidOperationException(SR.XmlInternalError);
                }
            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("9514600f-c124-4586-9e76-65b304ea19e8", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 962, "Else Branch");
                string? ns = attribute.Form == XmlSchemaForm.Qualified ? attribute.Namespace : string.Empty;
global::AutoRepairTracing.TraceLogger.Log("15f716b2-c4ae-4fe9-8f94-aec1f791fa99", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 963, "Variable Update", ns, "ns");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("10c2b88a-cfc9-4a5b-b8ea-175a19b94adf", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 964, "WritePrimitive", 0, WritePrimitiveMethodRequirement.WriteAttribute, "WritePrimitiveMethodRequirement.WriteAttribute");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("185b2a45-4b64-4f2d-ba96-bb4a1152a51c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 964, "WritePrimitive", 1, attribute.Name, "attribute.Name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("82f7b5b7-3623-4694-a6a0-f42c6a810663", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 964, "WritePrimitive", 2, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d18a28fd-6eaa-4529-9bb7-87b80b1705c7", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 964, "WritePrimitive", 3, attribute.Default, "attribute.Default");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d9dcf70f-8c26-49b2-ac89-1891c9d5396f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 964, "WritePrimitive", 4, memberValue, "memberValue");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("39a3f840-b7f6-441a-84e2-61d27acb17f5", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 964, "WritePrimitive", 5, attribute.Mapping!, "attribute.Mapping!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("f7abbb88-947f-456e-b824-15c8a31455bc", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 964, "WritePrimitive", 6, false, "false");
                WritePrimitive(WritePrimitiveMethodRequirement.WriteAttribute, attribute.Name, ns, attribute.Default, memberValue, attribute.Mapping!, false);
}            }
        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("5ff41fda-eb60-456d-9e30-cc8d40125621", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 947, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("a92e5319-4362-4114-9b57-111eecb5d2f0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 987, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("70ae0275-b285-4965-b75e-af3982c0bcd0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 947, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("b2eb0abd-447d-44e2-bb28-3554fac9cd9a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 947, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("333d6911-ee6f-43ff-9969-9b184a780ac5", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 947, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private static int FindXmlnsIndex(MemberMapping[] members)
{global::AutoRepairTracing.TraceLogger.Log("d768decb-6f2a-4e2a-ba50-763f621c2e60", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 968, "Enter Method 'FindXmlnsIndex'");
global::AutoRepairTracing.TraceLogger.LogArray("ec34b681-278a-4df0-ab61-4de65b055591", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 968, "Array Parameter", members, "FindXmlnsIndex#arg0:members");
global::AutoRepairTracing.TraceLogger.LogArrayAliasParameter("39062b39-e757-4811-95e5-90b79d642351", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 968, "FindXmlnsIndex", 0, members, "members");
try        {
{int __loopCounter_40a56589 = 0;global::AutoRepairTracing.TraceLogger.Log("e19fde9d-cf76-4be7-a3d6-37f4e0ec200a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1542, "Variable Update", __loopCounter_40a56589, "__loopCounter_40a56589");
            for (int i = 0; i < members.Length; global::AutoRepairTracing.TraceLogger.LogValue("1a6fa92e-b13e-494f-9848-018adf872a25", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1542, "Variable Update", i++, "i"))
            {
global::AutoRepairTracing.TraceLogger.Log("5f4e8682-867b-473c-839e-f7478af7e5f8", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 970, "For Loop");
global::AutoRepairTracing.TraceLogger.LogValue("618f234e-7a18-4be8-a28b-558871a7bbac", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1545, "Variable Update", __loopCounter_40a56589++, "__loopCounter_40a56589");if(__loopCounter_40a56589 > 10000){global::AutoRepairTracing.TraceLogger.Log("262c5a7a-a554-4a4b-a29b-e414c072e7be", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 993, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("40a56589-38cd-40f9-b658-a326addc5107", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 970, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_40a56589, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("501e8be1-4335-4b1f-ba86-dc77e9ca9864", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 993, "Break");
break;}}                if (members[i].Xmlns == null)
{global::AutoRepairTracing.TraceLogger.Log("b0c94edf-b30b-4c16-9fb1-630ea0aef4df", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 973, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("510aa360-ee3b-4132-b78f-adcd10b89f41", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 973, "Continue");
                    continue;
}{global::AutoRepairTracing.TraceLogger.Log("95b26bf9-f164-4c10-9177-357997e8aafb", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 975, "Return");

                return i;
}            }
}{global::AutoRepairTracing.TraceLogger.Log("fccf55f7-9669-459e-ac40-738e66822ee4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 978, "Return");

            return -1;
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("5543271c-b36d-48d4-829a-fa48921b4dba", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 968, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("b6128cfc-2f9b-4ac3-8194-96ed733fe4b9", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1001, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("b3708abf-b941-439f-8c03-922cdced25e6", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 968, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("896333ec-7575-4073-97b0-e3a51b031606", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 968, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("a572b45f-877e-4c5c-81b3-77e1c0baed70", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 968, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private bool WriteDerivedTypes(StructMapping mapping, string n, string? ns, object o, bool isNullable)
{global::AutoRepairTracing.TraceLogger.Log("d946947b-39c2-4555-a61b-ff2cc86f1f96", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 981, "Enter Method 'WriteDerivedTypes'");
try        {
            Type t = o.GetType();
global::AutoRepairTracing.TraceLogger.Log("494ec1d4-81fd-4ec8-8fe9-b68f01d341a3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 983, "Variable Update", t, "t");
{int __loopCounter_921e0f24 = 0;global::AutoRepairTracing.TraceLogger.Log("51c12aa5-1314-4c51-94f5-b772ef3f6a01", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1566, "Variable Update", __loopCounter_921e0f24, "__loopCounter_921e0f24");
            for (StructMapping? derived = mapping.DerivedMappings; derived != null; global::AutoRepairTracing.TraceLogger.LogValue("1cd8f7a1-2fd8-46ee-998c-224d7ab0eb85", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1566, "Variable Update", derived = derived.NextDerivedMapping, "derived"))
            {
global::AutoRepairTracing.TraceLogger.Log("0a63dad7-5ff0-49f4-a3f0-97980c769d3e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 984, "For Loop");
global::AutoRepairTracing.TraceLogger.LogValue("108035c5-e5a8-4e36-8867-95ff6335e471", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1569, "Variable Update", __loopCounter_921e0f24++, "__loopCounter_921e0f24");if(__loopCounter_921e0f24 > 10000){global::AutoRepairTracing.TraceLogger.Log("ff45be2d-6f87-4b99-8bab-acf2b6eab0d3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1008, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("921e0f24-052c-441a-b1dc-84e8b91c478a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 984, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_921e0f24, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("e193c803-a26a-4aab-9a4e-f3ce5280120b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1008, "Break");
break;}}                if (t == derived.TypeDesc!.Type)
                {
global::AutoRepairTracing.TraceLogger.Log("82949391-743e-4259-b35a-d2c3c0036691", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 987, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("67fb09d6-5da7-41bc-a125-f4f36b4a380d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 988, "WriteStructMethod", 0, derived, "derived");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("0e66c5a8-dcef-42b4-a5cf-591c4c86cb15", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 988, "WriteStructMethod", 1, n, "n");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("ffe8cb17-e514-4b25-b3ea-7410bd4d4acd", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 988, "WriteStructMethod", 2, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("e205a8a2-5dbc-44c3-8bc9-0d8fe797b97a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 988, "WriteStructMethod", 3, o, "o");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("85dfa689-d7a2-4a30-a032-78c60d26e124", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 988, "WriteStructMethod", 4, isNullable, "isNullable");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("1d7dafa9-5439-483c-880a-2a90fcdb5ec8", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 988, "WriteStructMethod", 5, true, "true");
                    WriteStructMethod(derived, n, ns, o, isNullable, needType: true);
}{global::AutoRepairTracing.TraceLogger.Log("20a66d26-1409-4b8a-96a8-5dee09a0af8a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 989, "Return");
                    return true;
}                }

                if (WriteDerivedTypes(derived, n, ns, o, isNullable))
                {
global::AutoRepairTracing.TraceLogger.Log("03eeda47-94f2-46e1-b707-20da7a714d57", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 993, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("62f6a58d-89d6-4df4-8e91-bf6f6fdb89d4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 994, "Return");
                    return true;
}                }
            }
}{global::AutoRepairTracing.TraceLogger.Log("af46758a-c7d8-4fc8-a1fa-e8bd7d1179de", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 998, "Return");

            return false;
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("fdb80774-781a-49c7-80ad-c2c551f8b82d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 981, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("63ca2a70-b847-4f61-ac34-720946b652cb", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1022, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("54d4e6d2-5ee0-41fa-94a1-c9614f6e3905", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 981, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("436ab09e-5290-46f9-866b-b3ce6e79e510", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 981, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("9c3ce7ff-8202-4929-8fe5-44f246a044f3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 981, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private void WritePrimitive(WritePrimitiveMethodRequirement method, string name, string? ns, object? defaultValue, object o, TypeMapping mapping, bool writeXsiType)
{global::AutoRepairTracing.TraceLogger.Log("501b3ef7-2546-435e-b23a-f7d60dc41ac6", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1001, "Enter Method 'WritePrimitive'");
try        {
            TypeDesc typeDesc = mapping.TypeDesc!;
global::AutoRepairTracing.TraceLogger.Log("9fc9a2aa-679f-4497-83ea-3dd3e4375cfd", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1003, "Variable Update", typeDesc, "typeDesc");
            bool hasDefault = defaultValue != null && defaultValue != DBNull.Value && mapping.TypeDesc!.HasDefaultSupport;
global::AutoRepairTracing.TraceLogger.Log("d633a094-420c-4c7b-b148-407664fada2b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1004, "Variable Update", hasDefault, "hasDefault");
            if (hasDefault)
            {
global::AutoRepairTracing.TraceLogger.Log("86b106d9-1faa-4d74-836a-f27d5acc8ef1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1006, "If-True Branch");
                if (mapping is EnumMapping)
                {
global::AutoRepairTracing.TraceLogger.Log("2182536e-978f-42cc-bf79-4babfe1599da", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1008, "If-True Branch");
                    if (((EnumMapping)global::AutoRepairTracing.TraceLogger.LogCastSource("57b809bd-9c14-4c88-a5bd-7fe7a267874c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1711, (object)(mapping), "EnumMapping")).IsFlags)
                    {
global::AutoRepairTracing.TraceLogger.Log("3dae0b9d-374f-45c7-9ed9-d53d0e1ff130", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1010, "If-True Branch");
                        IEnumerable<string> defaultEnumFlagValues = defaultValue!.ToString()!.Split((char[]?)global::AutoRepairTracing.TraceLogger.LogCastSource("0b253137-4d5b-4318-8edd-cf4238b33c27", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1714, (object)(null), "char[]?"), StringSplitOptions.RemoveEmptyEntries);
global::AutoRepairTracing.TraceLogger.Log("edb1a410-0311-464c-aa6b-0bba8019430c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1011, "Heap State: defaultEnumFlagValues (IEnumerable<string>)");global::AutoRepairTracing.TraceLogger.Log("59f51f09-5d35-4b87-825d-11917dd4d664", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1011, "Variable Update", defaultEnumFlagValues, "defaultEnumFlagValues");
                        string defaultEnumFlagString = string.Join(", ", defaultEnumFlagValues);
global::AutoRepairTracing.TraceLogger.Log("2e8aa8d4-9dcf-44dd-ad7e-37ad19325111", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1012, "Variable Update", defaultEnumFlagString, "defaultEnumFlagString");

                        if (o.ToString() == defaultEnumFlagString)
{global::AutoRepairTracing.TraceLogger.Log("144a07de-2449-4e73-9fc6-5461dea33429", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1015, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("2c3fcd3b-2066-4141-ada4-44dbfdfb0702", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1015, "Return");
                            return;
}                    }
                    else
                    {
global::AutoRepairTracing.TraceLogger.Log("6f7cad88-bc14-4182-abc7-0615bbde33f2", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1018, "Else Branch");
                        if (o.ToString() == defaultValue!.ToString())
{global::AutoRepairTracing.TraceLogger.Log("4803354e-0b2a-4fc8-8ebd-313ea4647cf7", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1020, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("0a137bb0-0e77-4dda-80a9-4f6e9b723606", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1020, "Return");
                            return;
}                    }
                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("d1ce3b63-e913-4811-b8db-d8fbf8854773", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1024, "Else Branch");
                    if (IsDefaultValue(o, defaultValue!))
                    {
global::AutoRepairTracing.TraceLogger.Log("31377e97-7640-4fee-b758-10fc8950485c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1026, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("64072d31-eb55-481f-9797-0444429920bf", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1027, "Return");
                        return;
}                    }
                }
            }

            XmlQualifiedName? xmlQualifiedName = null;
global::AutoRepairTracing.TraceLogger.Log("d7d89819-ae3b-4bab-a945-3d225c882efd", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1032, "Variable Update", xmlQualifiedName, "xmlQualifiedName");
global::AutoRepairTracing.TraceLogger.Log("eea63cab-61cb-4731-8ed8-b1b672f87d2a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1032, "Null Assignment Warning", "null", "xmlQualifiedName");            if (writeXsiType)
            {
global::AutoRepairTracing.TraceLogger.Log("3ebfc47d-cbb3-4ba5-9a39-386b8a71a191", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1034, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("50b8b12e-728f-4c85-a309-9325b18c2d06", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1646, "Variable Update", xmlQualifiedName = new XmlQualifiedName(mapping.TypeName, mapping.Namespace), "xmlQualifiedName");
            }

            string? stringValue;
            bool hasValidStringValue;
            if (mapping is EnumMapping enumMapping)
            {
global::AutoRepairTracing.TraceLogger.Log("ef0d15db-2c97-4bde-957d-d7b0eab35644", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1041, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("a0ea546b-1015-4932-92bf-2773e7076a7a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1654, "Variable Update", stringValue = WriteEnumMethod(enumMapping, o), "stringValue");
global::AutoRepairTracing.TraceLogger.LogValue("a52c17c2-3afc-4f85-ba64-9a892619dddb", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1655, "Variable Update", hasValidStringValue = true, "hasValidStringValue");
            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("f634bc58-de32-4448-833f-7150e405c7b0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1046, "Else Branch");
global::AutoRepairTracing.TraceLogger.LogValue("57c36134-6b82-4f2e-87a1-d6dcc0fdfa85", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1660, "Variable Update", hasValidStringValue = WritePrimitiveValue(typeDesc, o, out stringValue), "hasValidStringValue");
            }

            if (hasValidStringValue)
            {
global::AutoRepairTracing.TraceLogger.Log("dfaacf1a-6c3a-4f18-a72d-41869b7c96ee", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1051, "If-True Branch");
                if (hasRequirement(method, WritePrimitiveMethodRequirement.WriteElementString))
                {
global::AutoRepairTracing.TraceLogger.Log("f5eb7374-8d7c-4698-8351-c6c9fae1d814", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1053, "If-True Branch");
                    if (hasRequirement(method, WritePrimitiveMethodRequirement.Raw))
                    {
global::AutoRepairTracing.TraceLogger.Log("c08f6af1-2910-4dd3-9c15-8d523e045fd6", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1055, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d4f7ee34-9f2c-432f-8319-356b1a585494", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1056, "WriteElementStringRaw", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("1c6508c8-f661-4f65-a48d-d5a5384f6831", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1056, "WriteElementStringRaw", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("41cc8225-c04b-4919-8e19-e760cea383c0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1056, "WriteElementStringRaw", 2, stringValue, "stringValue");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("b0ec381c-bca4-4a41-9bb9-b2fb75287f89", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1056, "WriteElementStringRaw", 3, xmlQualifiedName, "xmlQualifiedName");
                        WriteElementStringRaw(name, ns, stringValue, xmlQualifiedName);
}                    }
                    else
                    {
global::AutoRepairTracing.TraceLogger.Log("dce816b5-632f-4570-8812-31cd5fd854c4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1059, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("47fd8e2a-6abf-4bec-8da8-1ac3a3b20b7e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1060, "WriteElementString", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("de52e3c1-3dbe-48a8-ba2c-eb8ae6289e81", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1060, "WriteElementString", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("1380f30a-fb8a-4cd3-ad7d-47f9eef8da3b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1060, "WriteElementString", 2, stringValue, "stringValue");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("a482a69a-4da2-4e8f-a51c-5d1a41d7ea67", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1060, "WriteElementString", 3, xmlQualifiedName, "xmlQualifiedName");
                        WriteElementString(name, ns, stringValue, xmlQualifiedName);
}                    }
                }

                else {global::AutoRepairTracing.TraceLogger.Log("70be5ab2-e296-46a9-a539-3f40a94302b5", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1064, "Else Branch");
if (hasRequirement(method, WritePrimitiveMethodRequirement.WriteNullableStringLiteral))
                {
global::AutoRepairTracing.TraceLogger.Log("8d32debe-c6a4-4c18-b116-9b949ce270aa", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1065, "If-True Branch");
                    if (hasRequirement(method, WritePrimitiveMethodRequirement.Encoded))
                    {
global::AutoRepairTracing.TraceLogger.Log("934807ec-725d-4bdf-9b7f-da6f213192c3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1067, "If-True Branch");
                        if (hasRequirement(method, WritePrimitiveMethodRequirement.Raw))
                        {
global::AutoRepairTracing.TraceLogger.Log("9fc1fbe2-283f-49b8-8f7d-12e1eb4a4b7b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1069, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d3dd8690-949c-400a-9f3f-bae83b66e9fa", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1070, "WriteNullableStringEncodedRaw", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("6b453f2f-7cbb-4c96-a407-d07d5c4b73c5", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1070, "WriteNullableStringEncodedRaw", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("fb5a0d99-bebb-4c83-8e06-983dd8cb398c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1070, "WriteNullableStringEncodedRaw", 2, stringValue, "stringValue");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("0f47fb2e-66f1-4002-a7da-2b8fe26cee4c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1070, "WriteNullableStringEncodedRaw", 3, xmlQualifiedName, "xmlQualifiedName");
                            WriteNullableStringEncodedRaw(name, ns, stringValue, xmlQualifiedName);
}                        }
                        else
                        {
global::AutoRepairTracing.TraceLogger.Log("08667d15-a6bc-4ab1-87d1-e36ee1f5e504", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1073, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("ee7a5242-8710-4ca0-9555-d03a59132c5f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1074, "WriteNullableStringEncoded", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("bcf8c03e-4162-4636-a3d0-e8630c4ccc50", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1074, "WriteNullableStringEncoded", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("46a7fa8c-e9fc-4617-9ab3-4453a1af7107", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1074, "WriteNullableStringEncoded", 2, stringValue, "stringValue");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("3bd98607-e717-40da-9838-57cb19b63b56", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1074, "WriteNullableStringEncoded", 3, xmlQualifiedName, "xmlQualifiedName");
                            WriteNullableStringEncoded(name, ns, stringValue, xmlQualifiedName);
}                        }
                    }
                    else
                    {
global::AutoRepairTracing.TraceLogger.Log("029b82bf-8fac-4176-aa22-077a879252c8", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1078, "Else Branch");
                        if (hasRequirement(method, WritePrimitiveMethodRequirement.Raw))
                        {
global::AutoRepairTracing.TraceLogger.Log("dcc759dc-3952-4dec-ab5c-9749285ecfec", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1080, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d1f33597-b285-4d05-9d19-eaf6fb52883d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1081, "WriteNullableStringLiteralRaw", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("58893470-f451-4a56-93b7-ebf57ebeca6a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1081, "WriteNullableStringLiteralRaw", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("f40bbe78-fec5-451a-8fcb-a32eb06fd182", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1081, "WriteNullableStringLiteralRaw", 2, stringValue, "stringValue");
                            WriteNullableStringLiteralRaw(name, ns, stringValue);
}                        }
                        else
                        {
global::AutoRepairTracing.TraceLogger.Log("4466c03c-2151-492f-a0b2-94ba749fb10f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1084, "Else Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("96163c7f-7b57-41c9-8945-fbd8f9824054", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1085, "WriteNullableStringLiteral", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("bc17a694-8367-4280-837b-45a069683ba1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1085, "WriteNullableStringLiteral", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("c42d8fa0-4573-4b69-9d26-3330fa119cb0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1085, "WriteNullableStringLiteral", 2, stringValue, "stringValue");
                            WriteNullableStringLiteral(name, ns, stringValue);
}                        }
                    }
                }
                else {global::AutoRepairTracing.TraceLogger.Log("d61bca7a-33df-4d96-b2b0-28fd1828e219", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1089, "Else Branch");
if (hasRequirement(method, WritePrimitiveMethodRequirement.WriteAttribute))
                {
global::AutoRepairTracing.TraceLogger.Log("4a72953f-b36e-4cd2-ab7c-651231c42758", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1090, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("85d4d8f7-c124-4774-8acb-bae6aa1f7cab", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1091, "WriteAttribute", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("b3bbf7af-2539-44c1-baed-feeb42b309a6", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1091, "WriteAttribute", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("cb797924-86e4-45c4-a48a-b6c1b5e7216d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1091, "WriteAttribute", 2, stringValue, "stringValue");
                    WriteAttribute(name, ns, stringValue);
}                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("b011a684-dfc9-4d7c-812f-04aeb19c286c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1094, "Else Branch");
                    Debug.Fail("https://github.com/dotnet/runtime/issues/18037: Add More Tests for Serialization Code");
                }
}}            }
            else {global::AutoRepairTracing.TraceLogger.Log("a116bc5e-bae0-4a30-8e83-dee290bef01c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1098, "Else Branch");
if (o is byte[] a)
            {
global::AutoRepairTracing.TraceLogger.Log("7c6b7179-1b5e-401f-911d-ba50598bcc57", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1099, "If-True Branch");
                if (hasRequirement(method, WritePrimitiveMethodRequirement.WriteElementString | WritePrimitiveMethodRequirement.Raw))
                {
global::AutoRepairTracing.TraceLogger.Log("289e2e0d-e86f-4a9f-93bd-89e59fce97d9", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1101, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("f43947cc-4a62-4d73-a39a-992ad20855b2", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1102, "WriteElementStringRaw", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("22a49769-84bb-4c28-aaba-f18cfe6bd078", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1102, "WriteElementStringRaw", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("b46c420d-8fdf-4856-8468-a2ea23ec496e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1102, "WriteElementStringRaw", 2, FromByteArrayBase64(a), "FromByteArrayBase64(a)");
                    WriteElementStringRaw(name, ns, FromByteArrayBase64(a));
}                }
                else {global::AutoRepairTracing.TraceLogger.Log("9b3569af-e22a-40d3-8de6-6354dfa77cbc", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1104, "Else Branch");
if (hasRequirement(method, WritePrimitiveMethodRequirement.WriteNullableStringLiteral | WritePrimitiveMethodRequirement.Raw))
                {
global::AutoRepairTracing.TraceLogger.Log("bb0edcf2-cc25-4464-b5ea-93d572294c7d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1105, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("a8d227e0-9128-435f-b914-6593193bb545", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1106, "WriteNullableStringLiteralRaw", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("1aaf8e7d-7821-4f3e-88ab-d373f761b33d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1106, "WriteNullableStringLiteralRaw", 1, ns, "ns");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("7a387842-8ba3-4a4a-95e2-a94ce87b49cb", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1106, "WriteNullableStringLiteralRaw", 2, FromByteArrayBase64(a), "FromByteArrayBase64(a)");
                    WriteNullableStringLiteralRaw(name, ns, FromByteArrayBase64(a));
}                }
                else {global::AutoRepairTracing.TraceLogger.Log("cc5dc2c3-ed75-4fe1-95f4-a0da10a2970c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1108, "Else Branch");
if (hasRequirement(method, WritePrimitiveMethodRequirement.WriteAttribute))
                {
global::AutoRepairTracing.TraceLogger.Log("5835ef13-c06a-4e6f-be70-1a7cb3d09975", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1109, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("c11b8622-b439-4b66-af7d-a04cad90f657", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1110, "WriteAttribute", 0, name, "name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("0a3beb60-d6f6-4528-9901-f3d1e95af9c6", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1110, "WriteAttribute", 1, ns!, "ns!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("cea43f1f-639c-4536-a321-ff85ea00c161", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1110, "WriteAttribute", 2, a, "a");
                    WriteAttribute(name, ns!, a);
}                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("ba824556-d952-47f2-8111-c182b24567ac", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1113, "Else Branch");
                    Debug.Fail("https://github.com/dotnet/runtime/issues/18037: Add More Tests for Serialization Code");
                }
}}            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("908ddc7f-7549-42cb-8a65-74016458c764", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1118, "Else Branch");
                Debug.Fail("https://github.com/dotnet/runtime/issues/18037: Add More Tests for Serialization Code");
            }
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("a8cc081d-e25d-4794-a3bf-6fc8f24dad72", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1001, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("0234405a-de2a-441e-b649-b4d56e39166f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1145, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("bfddd1f0-1b67-4ce3-9ddf-4dc281890b25", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1001, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("47655145-b488-4377-b560-638463580585", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1001, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("f415e4bf-6a2c-4b2b-b790-2a323c2a59eb", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1001, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private static bool hasRequirement(WritePrimitiveMethodRequirement value, WritePrimitiveMethodRequirement requirement)
{global::AutoRepairTracing.TraceLogger.Log("7caf9d17-584f-4adb-a34d-60fa81de1110", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1123, "Enter Method 'hasRequirement'");
try        {
{global::AutoRepairTracing.TraceLogger.Log("50548b3e-fe0e-44b4-98cc-664004cd2ea8", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1125, "Return");
            return (value & requirement) == requirement;
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("b06dd1e5-646b-4e77-b3b2-d6bbbc13fad1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1123, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("29ea1e69-665e-4197-a90d-d5d07a8e26d2", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1151, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("90e06d05-25d8-40f4-a162-80bd0f7fa3ff", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1123, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("5aab25d9-c516-43c0-929a-62690845a6c3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1123, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("07a1b755-fe78-4dc5-a2a6-662e452290ad", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1123, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private static bool IsDefaultValue(object o, object value)
{global::AutoRepairTracing.TraceLogger.Log("38bb0d07-49e8-4514-b2dc-91d4769541af", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1128, "Enter Method 'IsDefaultValue'");
try        {
            if (value is string && ((string)global::AutoRepairTracing.TraceLogger.LogCastSource("4e6bb08e-9a70-4e8f-95bb-d1156869a531", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1912, (object)(value), "string")).Length == 0)
            {
global::AutoRepairTracing.TraceLogger.Log("b96d6eae-a0ef-4d7b-b62e-dc9b2a9f21b7", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1131, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("c676328a-8bfc-436e-9f14-4639f31fb232", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1132, "Return");
                return string.IsNullOrEmpty((string)global::AutoRepairTracing.TraceLogger.LogCastSource("e1082df1-0f24-4bca-9ff8-cecd2b667140", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1916, (object)(o), "string"));
}            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("28ed3ee6-3b40-40e1-a15e-4627f833130a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1135, "Else Branch");
{global::AutoRepairTracing.TraceLogger.Log("2ff749f6-0740-4378-b3ce-1dc73b60388e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1136, "Return");
                return value.Equals(o);
}            }
        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("1daa2c74-fbda-4132-be71-885d79b9e73e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1128, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("d10cfabf-aeae-4eef-aa7b-af3d002d58e2", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1164, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("da21c182-1f7c-4247-9d7f-182ab435f077", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1128, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("ffa3fd49-926a-4749-98f0-70a79d74eb38", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1128, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("8c15b14a-ecd3-4c17-90a8-d67a3eeec3a4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1128, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private bool WritePrimitiveValue(TypeDesc typeDesc, object? o, out string? stringValue)
{global::AutoRepairTracing.TraceLogger.Log("7355e0f3-c253-4444-a5d9-1a3b0a96a6f4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1140, "Enter Method 'WritePrimitiveValue'");
try        {
            if (typeDesc == ReflectionXmlSerializationReader.StringTypeDesc || typeDesc.FormatterName == "String")
            {
global::AutoRepairTracing.TraceLogger.Log("d7426062-8be2-4128-a8d1-6854d6d47915", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1143, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("9567742b-ae84-4f50-b185-be221ff8b85c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1830, "Variable Update", stringValue = (string?)global::AutoRepairTracing.TraceLogger.LogCastSource("1210fb76-e3d0-4d43-a769-e115c34f7a06", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1934, (object)(o), "string?"), "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("a57a6abd-d0ae-4a76-810e-e3659f72d31d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1145, "Return");
                return true;
}            }
            else
            {
global::AutoRepairTracing.TraceLogger.Log("3d8e32f9-4662-48f4-a35d-6ace7531c676", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1148, "Else Branch");
                if (!typeDesc.HasCustomFormatter)
                {
global::AutoRepairTracing.TraceLogger.Log("0bfced19-7bd0-4ddf-828e-36548ed06e2c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1150, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("dcbf5c45-0675-4022-80f2-40d6bce8faf7", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1840, "Variable Update", stringValue = ConvertPrimitiveToString(o!, typeDesc), "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("c7b3e854-f755-4879-9914-29b291af6871", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1152, "Return");
                    return true;
}                }
                else {global::AutoRepairTracing.TraceLogger.Log("1f7c6b64-6a30-4c19-9529-0fa36894ee87", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1154, "Else Branch");
if (o is byte[] && typeDesc.FormatterName == "ByteArrayHex")
                {
global::AutoRepairTracing.TraceLogger.Log("de65c517-2fb6-4b45-8388-9a9145d18483", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1155, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("2df67ae8-ad7b-4c2b-b1af-73f1e9c86e63", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1848, "Variable Update", stringValue = FromByteArrayHex((byte[])global::AutoRepairTracing.TraceLogger.LogCastSource("4e6f274c-46ec-45f1-9191-8d128d919f30", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1952, (object)(o), "byte[]")), "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("d98219a8-c759-422f-8046-290f6ad306c0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1157, "Return");
                    return true;
}                }
                else {global::AutoRepairTracing.TraceLogger.Log("a37a35b9-90f5-4cd1-ae94-f2c9aa81a9ce", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1159, "Else Branch");
if (o is DateTime)
                {
global::AutoRepairTracing.TraceLogger.Log("2974690d-d599-485e-8721-b6479b8a731f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1160, "If-True Branch");
                    if (typeDesc.FormatterName == "DateTime")
                    {
global::AutoRepairTracing.TraceLogger.Log("b7841449-3d7d-409a-be4d-f4c50308112e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1162, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("8e9fb160-52fd-4d29-a2b8-ab944e5d4a88", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1859, "Variable Update", stringValue = FromDateTime((DateTime)global::AutoRepairTracing.TraceLogger.LogCastSource("1e970766-9f78-45cb-b1a3-27d4288de336", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1963, (object)(o), "DateTime")), "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("7c1d9687-9dde-41e6-9117-61aeb25d946f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1164, "Return");
                        return true;
}                    }
                    else {global::AutoRepairTracing.TraceLogger.Log("8398320f-5f27-4d54-b5f3-491525f10b89", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1166, "Else Branch");
if (typeDesc.FormatterName == "Date")
                    {
global::AutoRepairTracing.TraceLogger.Log("d71615f3-2851-41ab-a08e-8c81ee9d3a83", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1167, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("0ae8f5fd-3fab-4193-b830-d147b8cca29d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1867, "Variable Update", stringValue = FromDate((DateTime)global::AutoRepairTracing.TraceLogger.LogCastSource("2b933f17-a7aa-4b1c-bd38-041f739ecbec", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1971, (object)(o), "DateTime")), "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("33ad0108-02ea-4af2-a105-fa470274193b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1169, "Return");
                        return true;
}                    }
                    else {global::AutoRepairTracing.TraceLogger.Log("4afda394-982d-4806-8e1a-937aa00cb2b3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1171, "Else Branch");
if (typeDesc.FormatterName == "Time")
                    {
global::AutoRepairTracing.TraceLogger.Log("18462083-37a7-4a25-96d1-1106a57ddbcb", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1172, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("78171d4c-0e8a-46e9-a936-bdc76aa3e1fd", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1875, "Variable Update", stringValue = FromTime((DateTime)global::AutoRepairTracing.TraceLogger.LogCastSource("a7ce9fb1-d0cb-4ec5-87bb-2e515e7d76b5", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1979, (object)(o), "DateTime")), "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("20e7a549-a3ef-4f0b-a9f6-5f9d9f729c29", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1174, "Return");
                        return true;
}                    }
                    else
                    {
global::AutoRepairTracing.TraceLogger.Log("31e02f8c-598f-4cda-8e46-eca3100d686a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1177, "Else Branch");
                        throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorDetails, "Invalid DateTime"));
                    }
}}                }
                else {global::AutoRepairTracing.TraceLogger.Log("135394db-76f2-4670-9f17-cb278d2ff0c4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1181, "Else Branch");
if (o is DateOnly)
                {
global::AutoRepairTracing.TraceLogger.Log("ad6c0d09-9078-40a7-a9a7-20431801629a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1182, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("c02db31a-49f7-4002-ba57-88eef9de5b8c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1889, "Variable Update", stringValue = FromDateOnly((DateOnly)global::AutoRepairTracing.TraceLogger.LogCastSource("4970b548-a8cf-4221-8df0-5da5ea3ffae1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1993, (object)(o), "DateOnly")), "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("ad49b97e-6f6e-4024-90c2-96cb1ae1e972", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1184, "Return");
                    return true;
}                }
                else {global::AutoRepairTracing.TraceLogger.Log("6212afa3-67c2-4269-88b2-1265f27bbced", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1186, "Else Branch");
if (o is TimeOnly)
                {
global::AutoRepairTracing.TraceLogger.Log("dbdbe7d8-b65f-474d-b97c-784978e5167a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1187, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("3ab1c324-b917-407c-b116-3094bd340f16", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1897, "Variable Update", stringValue = FromTimeOnly((TimeOnly)global::AutoRepairTracing.TraceLogger.LogCastSource("7b9d1234-51e8-46ee-8b7f-7b35b175d0a1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2001, (object)(o), "TimeOnly")), "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("978c83c0-7129-4e7b-8419-63fc85365dcd", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1189, "Return");
                    return true;
}                }
                else {global::AutoRepairTracing.TraceLogger.Log("b5b3ceac-6bee-4a4d-a5ad-1c19b1f809e0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1191, "Else Branch");
if (typeDesc == ReflectionXmlSerializationReader.QnameTypeDesc)
                {
global::AutoRepairTracing.TraceLogger.Log("f8ab5811-c152-4d98-a4cd-589219f6d657", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1192, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("3fb0b2d7-0d45-4c20-9f1b-62fb6ebccb43", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1905, "Variable Update", stringValue = FromXmlQualifiedName((XmlQualifiedName?)global::AutoRepairTracing.TraceLogger.LogCastSource("86c6d2be-633c-41a0-8c98-4a464af9cb5a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2009, (object)(o), "XmlQualifiedName?")), "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("e87819cb-4bd3-4ece-946c-e0283aea0409", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1194, "Return");
                    return true;
}                }
                else {global::AutoRepairTracing.TraceLogger.Log("e2e946ab-7d45-4b2a-afb1-8ae037b1e826", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1196, "Else Branch");
if (o is string)
                {
global::AutoRepairTracing.TraceLogger.Log("5c45947e-1d09-4a80-8085-5f0e347a15ba", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1197, "If-True Branch");
                    switch (typeDesc.FormatterName)
                    {
                        case "XmlName":
global::AutoRepairTracing.TraceLogger.Log("07910152-c370-41d9-8de5-b2a52fb3c78d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1226, "Switch Case [case \"XmlName\":]");
global::AutoRepairTracing.TraceLogger.LogValue("038e2d61-11f0-4725-8ec5-88b17dcdfb1c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1917, "Variable Update", stringValue = FromXmlName((string)global::AutoRepairTracing.TraceLogger.LogCastSource("e35425d0-2959-43ee-88b0-68ec3b56047f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2021, (object)(o), "string")), "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("14f8d98a-fb05-4e17-b749-8f9e0690b3a5", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1202, "Break");
                            break;
}                        case "XmlNCName":
global::AutoRepairTracing.TraceLogger.Log("bd8f4668-07d5-4ca7-a7fb-ec52b62f90ed", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1229, "Switch Case [case \"XmlNCName\":]");
global::AutoRepairTracing.TraceLogger.LogValue("c838bba3-21e6-43d2-968f-a1a39a94a649", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1922, "Variable Update", stringValue = FromXmlNCName((string)global::AutoRepairTracing.TraceLogger.LogCastSource("44193dec-6bfc-4100-b056-730c188790e8", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2026, (object)(o), "string")), "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("aeedeba4-ed04-40bf-86ca-4358ff4138ac", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1205, "Break");
                            break;
}                        case "XmlNmToken":
global::AutoRepairTracing.TraceLogger.Log("d1b4270c-f3b2-45c4-be1c-86630d19d117", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1232, "Switch Case [case \"XmlNmToken\":]");
global::AutoRepairTracing.TraceLogger.LogValue("4f816f81-1c30-487a-9e29-8ac71aaf83b9", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1927, "Variable Update", stringValue = FromXmlNmToken((string)global::AutoRepairTracing.TraceLogger.LogCastSource("11f09d2e-5528-481c-a647-4eb16aba954a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2031, (object)(o), "string")), "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("3dd74997-c700-45e8-a4c4-aecca54d2733", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1208, "Break");
                            break;
}                        case "XmlNmTokens":
global::AutoRepairTracing.TraceLogger.Log("1a61094e-27ac-44d5-80c5-ccafd473d0b9", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1235, "Switch Case [case \"XmlNmTokens\":]");
global::AutoRepairTracing.TraceLogger.LogValue("dc9c3384-40e4-43b8-877e-335694c113c2", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1932, "Variable Update", stringValue = FromXmlNmTokens((string)global::AutoRepairTracing.TraceLogger.LogCastSource("aa6d4069-0b3c-4ff9-9a80-3e44eef1c897", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2036, (object)(o), "string")), "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("e546409d-d48e-4d2f-8393-a115e874ff71", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1211, "Break");
                            break;
}                        default:
global::AutoRepairTracing.TraceLogger.Log("59d8b725-3ee4-45eb-8ace-67f6c688a5e8", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1238, "Switch Case [default:]");
global::AutoRepairTracing.TraceLogger.LogValue("a585cd73-6916-4650-a85f-64cff2a0a20b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1937, "Variable Update", stringValue = null, "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("c9aa6c45-8e38-4491-bcf4-7703d3d96f12", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1214, "Return");
                            return false;
}                    }
{global::AutoRepairTracing.TraceLogger.Log("7e082047-8728-4d1d-a63e-3c3b6e559cd3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1217, "Return");

                    return true;
}                }
                else {global::AutoRepairTracing.TraceLogger.Log("ead1811e-aacf-45d6-817b-d20823d90066", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1219, "Else Branch");
if (o is char && typeDesc.FormatterName == "Char")
                {
global::AutoRepairTracing.TraceLogger.Log("f1998fff-76a6-4e83-a987-0c14c631ccd6", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1220, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("78d3fe84-d163-4785-afbd-95a28530f8ee", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1949, "Variable Update", stringValue = FromChar((char)o), "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("1c8bf728-bdd0-4513-8d29-479f9fd73bf6", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1222, "Return");
                    return true;
}                }
                else {global::AutoRepairTracing.TraceLogger.Log("cfb85a83-ce7a-49c4-8738-d544a9bd0fe2", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1224, "Else Branch");
if (o is byte[])
                {
global::AutoRepairTracing.TraceLogger.Log("9ff21662-112e-49b7-b784-7b1d242e1d0a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1225, "If-True Branch");
                    // we deal with byte[] specially in WritePrimitive()
                }
                else
                {
global::AutoRepairTracing.TraceLogger.Log("5b506c8e-2941-4d4a-a85a-4d888adb0023", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1229, "Else Branch");
                    throw new InvalidOperationException(SR.XmlInternalError);
                }
}}}}}}}}            }
global::AutoRepairTracing.TraceLogger.LogValue("3d9ca3c6-b8c8-4b99-be07-5d04af3b3b42", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1966, "Variable Update", stringValue = null, "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("b881c265-71f2-44c3-b60a-3afa168e032f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1235, "Return");
            return false;
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("3e98af39-374d-480a-9b37-c36e926173f4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1140, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("3e24a8c2-544b-470e-a52d-4fc84241e159", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1263, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("c4781dc3-9067-4732-9e31-815439bdff95", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1140, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("6c8dff5d-7d62-47cb-8f06-3ea5336f54f1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1140, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("83798c96-34e8-4c72-8cae-216489691745", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1140, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private static string ConvertPrimitiveToString(object o, TypeDesc typeDesc)
{global::AutoRepairTracing.TraceLogger.Log("e9051dcb-f0b5-424a-bdd4-a19fabc6bc86", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1238, "Enter Method 'ConvertPrimitiveToString'");
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
                "Guid" => XmlConvert.ToString((Guid)global::AutoRepairTracing.TraceLogger.LogCastSource("d62162cf-2187-412e-880f-808aa6a452a3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2094, (object)(o), "Guid")),
                "Char" => XmlConvert.ToString((char)o),
                "TimeSpan" => XmlConvert.ToString((TimeSpan)global::AutoRepairTracing.TraceLogger.LogCastSource("382bc4bf-d8b1-43a3-86b4-ee76a58fdffa", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2096, (object)(o), "TimeSpan")),
                "DateTimeOffset" => XmlConvert.ToString((DateTimeOffset)global::AutoRepairTracing.TraceLogger.LogCastSource("227ba2fd-fc2f-4eb9-b71f-bb407acc6ebd", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2097, (object)(o), "DateTimeOffset")),
                _ => o.ToString()!,
            };
global::AutoRepairTracing.TraceLogger.Log("7bd6feac-d9db-4cb5-ae80-4692652173aa", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1240, "Variable Update", stringValue, "stringValue");
{global::AutoRepairTracing.TraceLogger.Log("297a565b-f23f-405c-ba46-d61ef7071a52", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1261, "Return");
            return stringValue;
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("9fc8f05f-7fa6-4eea-8050-a60ce7a048c1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1238, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("4503224d-7988-4fe6-b22b-d8e0ff0e6fba", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1290, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("cd06e0fc-05f9-4428-be2c-b39db02141c6", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1238, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("4abdca04-ed20-40eb-aa43-d74a352aebd2", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1238, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("646d3197-c239-431b-84a0-e4369488f048", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1238, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        private void GenerateMembersElement(object o, XmlMembersMapping xmlMembersMapping)
{global::AutoRepairTracing.TraceLogger.Log("dee57728-2f66-4c53-80d4-f0f1dfc12eed", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1264, "Enter Method 'GenerateMembersElement'");
try        {
            ElementAccessor element = xmlMembersMapping.Accessor;
global::AutoRepairTracing.TraceLogger.Log("c00af65f-3572-44e8-bcfc-51c3d2df49e3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1266, "Variable Update", element, "element");
            MembersMapping mapping = (MembersMapping)global::AutoRepairTracing.TraceLogger.LogCastSource("d6f8184a-2412-46c8-9a98-c2670c564f1c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2112, (object)(element.Mapping!), "MembersMapping");
global::AutoRepairTracing.TraceLogger.Log("d256c495-e14f-42f8-a969-3e8c5fdf8d52", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1267, "Variable Update", mapping, "mapping");
            bool hasWrapperElement = mapping.HasWrapperElement;
global::AutoRepairTracing.TraceLogger.Log("e38d543f-361a-4ac2-aae0-73a25579062c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1268, "Variable Update", hasWrapperElement, "hasWrapperElement");
            bool writeAccessors = mapping.WriteAccessors;
global::AutoRepairTracing.TraceLogger.Log("e26a2ebe-c4d4-4fc2-8b4b-e5813347bd10", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1269, "Variable Update", writeAccessors, "writeAccessors");
            bool isRpc = xmlMembersMapping.IsSoap && writeAccessors;
global::AutoRepairTracing.TraceLogger.Log("af4f9f67-9f87-4913-acc6-6394a7dd1020", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1270, "Variable Update", isRpc, "isRpc");

            WriteStartDocument();

            if (!mapping.IsSoap)
            {
global::AutoRepairTracing.TraceLogger.Log("c6c6871e-8c12-424f-9f35-575c1053a15c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1275, "If-True Branch");
                TopLevelElement();
            }

            object[] p = (object[])global::AutoRepairTracing.TraceLogger.LogCastSource("8efe09d8-0aba-4ac5-861d-fcdfb152fe07", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2129, (object)(o), "object[]");
global::AutoRepairTracing.TraceLogger.Log("65e9fcd9-0dbc-4451-8837-3264ecb6f89b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1279, "Heap State: p (object[])");global::AutoRepairTracing.TraceLogger.Log("0d469633-2c03-499a-ad9b-2d83746a0d29", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1279, "Variable Update", p, "p");
            int pLength = p.Length;
global::AutoRepairTracing.TraceLogger.Log("20e3e0f1-4142-4305-b37c-e8ac33194811", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1280, "Variable Update", pLength, "pLength");

            if (hasWrapperElement)
            {
global::AutoRepairTracing.TraceLogger.Log("068adcba-1d94-4ca3-99f0-a918de59cdd4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1283, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("2d024cfb-7c50-41da-ad99-8433c5c18092", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1284, "WriteStartElement", 0, element.Name, "element.Name");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("46678590-08d1-4cf9-a6fa-3af33409887a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1284, "WriteStartElement", 1, (element.Form == XmlSchemaForm.Qualified ? element.Namespace : string.Empty), "(element.Form == XmlSchemaForm.Qualified ? element.Namespace : string.Empty)");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("488e1359-ca2b-4e44-9043-59122e1d9d86", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1284, "WriteStartElement", 2, mapping.IsSoap, "mapping.IsSoap");
                WriteStartElement(element.Name, (element.Form == XmlSchemaForm.Qualified ? element.Namespace : string.Empty), mapping.IsSoap);
}
                int xmlnsMember = FindXmlnsIndex(mapping.Members!);
global::AutoRepairTracing.TraceLogger.Log("63857a60-e94b-4de8-8533-6fdffcd77cd2", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1286, "Variable Update", xmlnsMember, "xmlnsMember");
                if (xmlnsMember >= 0)
                {
global::AutoRepairTracing.TraceLogger.Log("6a5796e0-403b-4eb4-aa85-ffe8a74c4263", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1288, "If-True Branch");
                    var source = (XmlSerializerNamespaces)global::AutoRepairTracing.TraceLogger.LogCastSource("74c08462-754f-4044-9e7e-c71985914ea0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2147, (object)(p[xmlnsMember]), "XmlSerializerNamespaces");
global::AutoRepairTracing.TraceLogger.Log("d1b3f2a2-8e03-4ca0-bf0a-58a3842f116f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1289, "Variable Update", source, "source");

                    if (pLength > xmlnsMember)
                    {
global::AutoRepairTracing.TraceLogger.Log("1ba9c7a5-45aa-4464-b4a9-e3ff6a4d562c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1292, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("95906b24-1d97-4b73-9ef3-478ceec857df", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1293, "WriteNamespaceDeclarations", 0, source, "source");
                        WriteNamespaceDeclarations(source);
}                    }
                }
{int __loopCounter_07f69116 = 0;global::AutoRepairTracing.TraceLogger.Log("dc484a9e-b259-49ba-b1b7-8f00ecf0beca", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2044, "Variable Update", __loopCounter_07f69116, "__loopCounter_07f69116");

                for (int i = 0; i < mapping.Members!.Length; global::AutoRepairTracing.TraceLogger.LogValue("b9b15b15-8699-4ca9-a471-b7216562a99f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2045, "Variable Update", i++, "i"))
                {
global::AutoRepairTracing.TraceLogger.Log("14bf0873-9be5-42b2-9ead-80d4f2478f24", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1297, "For Loop");
global::AutoRepairTracing.TraceLogger.LogValue("b62a4af3-1f25-4c95-a336-7eb059a61e70", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2048, "Variable Update", __loopCounter_07f69116++, "__loopCounter_07f69116");if(__loopCounter_07f69116 > 10000){global::AutoRepairTracing.TraceLogger.Log("7c849bec-8999-4593-b403-4b1e0538748f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1327, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("07f69116-9c41-4642-81ef-ea91d8ef8e18", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1297, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_07f69116, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("44aaf3af-c775-4166-8dee-f35f4e65f543", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1327, "Break");
break;}}                    MemberMapping member = mapping.Members[i];
global::AutoRepairTracing.TraceLogger.Log("cb670fe8-cb82-4356-ac3d-7084165448d9", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1299, "Variable Update", member, "member");
                    if (member.Attribute != null && !member.Ignore)
                    {
global::AutoRepairTracing.TraceLogger.Log("488e202a-c89c-470e-88b9-db7a8ea0afd2", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1301, "If-True Branch");
                        object source = p[i];
global::AutoRepairTracing.TraceLogger.Log("5555ed9f-240a-45c9-b4c4-d87baaf75712", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1302, "Variable Update", source, "source");
                        bool? specifiedSource = null;
global::AutoRepairTracing.TraceLogger.Log("b6efb57d-2fa4-4f1b-a7cd-90b8a5e4c5f2", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1303, "Variable Update", specifiedSource, "specifiedSource");
                        if (member.CheckSpecified != SpecifiedAccessor.None)
                        {
global::AutoRepairTracing.TraceLogger.Log("90f98931-e948-4286-aa28-bbdc37948d27", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1305, "If-True Branch");
                            string memberNameSpecified = $"{member.Name}Specified";
global::AutoRepairTracing.TraceLogger.Log("5edb82c0-ed5a-46a5-81ba-d0d3e70ee886", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1306, "Variable Update", memberNameSpecified, "memberNameSpecified");
                            for (int j = 0; j < Math.Min(pLength, mapping.Members.Length); global::AutoRepairTracing.TraceLogger.LogValue("04dc2663-673e-4cb5-b7ea-777925937177", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2060, "Variable Update", j++, "j"))
                            {
global::AutoRepairTracing.TraceLogger.Log("88bce9e9-6194-4a86-9698-172b143f6357", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1307, "For Loop");
                                if (mapping.Members[j].Name == memberNameSpecified)
                                {
global::AutoRepairTracing.TraceLogger.Log("f499cbaa-f8a3-4aa9-8348-cf808a43fc53", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1310, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("2c8337a0-3f1a-47e6-8d3a-3a559588b384", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1311, "Variable Update", j, "index:p");
global::AutoRepairTracing.TraceLogger.LogValue("c2e49812-78ed-415a-bea4-513da5f40543", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2066, "Variable Update", specifiedSource = (bool)p[j], "specifiedSource");
}{global::AutoRepairTracing.TraceLogger.Log("e5e03323-0c7a-4604-9636-3149a3b6a1cf", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1312, "Break");
                                    break;
}                                }
                            }
                        }

                        if (pLength > i && (specifiedSource == null || specifiedSource.Value))
                        {
global::AutoRepairTracing.TraceLogger.Log("f4a81310-8b0c-4da6-a2bd-c7f53549e884", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1318, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("51aa53bc-8a53-4935-a488-ccd9d06d481f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1319, "WriteMember", 0, source, "source");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d2de34c4-605f-409f-ae01-2a434e08607e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1319, "WriteMember", 1, member.Attribute, "member.Attribute");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d63b643a-da7a-45ac-a0bb-f2eb76416374", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1319, "WriteMember", 2, member.TypeDesc!, "member.TypeDesc!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("669d160f-84ed-4310-a3ce-b2aa2ccaa11a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1319, "WriteMember", 3, null, "null");
                            WriteMember(source, member.Attribute, member.TypeDesc!, null);
}                        }
                    }
                }
}            }
{int __loopCounter_02fe0a64 = 0;global::AutoRepairTracing.TraceLogger.Log("c2e578ba-1965-434b-8b94-04bb0d65caee", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2085, "Variable Update", __loopCounter_02fe0a64, "__loopCounter_02fe0a64");

            for (int i = 0; i < mapping.Members!.Length; global::AutoRepairTracing.TraceLogger.LogValue("fbc1c3bc-9ddf-4e4d-a7bb-f427cfafd6a8", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2086, "Variable Update", i++, "i"))
            {
global::AutoRepairTracing.TraceLogger.Log("424ea402-630d-401f-b50a-f876d92552c1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1325, "For Loop");
global::AutoRepairTracing.TraceLogger.LogValue("f12c39c6-5084-4e55-b6f7-cf54b7bd2989", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2089, "Variable Update", __loopCounter_02fe0a64++, "__loopCounter_02fe0a64");if(__loopCounter_02fe0a64 > 10000){global::AutoRepairTracing.TraceLogger.Log("4cec6358-5c05-4f10-a293-43f54679dfff", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1355, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("02fe0a64-3be4-45ae-995e-a53a60f9608a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1325, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_02fe0a64, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("c81a730b-d51f-488c-ab5a-57c0d11af5a0", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1355, "Break");
break;}}                MemberMapping member = mapping.Members[i];
global::AutoRepairTracing.TraceLogger.Log("7de7f681-9c36-4b42-a2d4-0a57e2545d19", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1327, "Variable Update", member, "member");
                if (member.Xmlns != null)
{global::AutoRepairTracing.TraceLogger.Log("b30a1d5e-e5d5-40cd-89bd-defd4d0289d1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1329, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("639f6cac-e7ea-4f73-a52b-40ac0422fe2c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1329, "Continue");
                    continue;
}
                if (member.Ignore)
{global::AutoRepairTracing.TraceLogger.Log("86c7f12e-9c7e-4dc5-b1ae-4f25b8ddf0a8", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1332, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("576b4c5a-6d73-4507-afa2-feaafb63240c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1332, "Continue");
                    continue;
}
                bool? specifiedSource = null;
global::AutoRepairTracing.TraceLogger.Log("ffaed368-82ba-4e03-9090-4b63b7f01e67", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1334, "Variable Update", specifiedSource, "specifiedSource");
                if (member.CheckSpecified != SpecifiedAccessor.None)
                {
global::AutoRepairTracing.TraceLogger.Log("052f9781-ad6f-482e-9a0b-6d9ba1a53741", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1336, "If-True Branch");
                    string memberNameSpecified = $"{member.Name}Specified";
global::AutoRepairTracing.TraceLogger.Log("a2a13134-3c30-4b53-b979-b501cc27066a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1337, "Variable Update", memberNameSpecified, "memberNameSpecified");
                    for (int j = 0; j < Math.Min(pLength, mapping.Members.Length); global::AutoRepairTracing.TraceLogger.LogValue("dcc4a835-481c-44ef-bf7b-39b2f42ccc39", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2107, "Variable Update", j++, "j"))
                    {
global::AutoRepairTracing.TraceLogger.Log("7d2a32f5-8105-4c07-afae-f22f6d4ea5cf", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1338, "For Loop");
                        if (mapping.Members[j].Name == memberNameSpecified)
                        {
global::AutoRepairTracing.TraceLogger.Log("5f8330f8-3114-42da-a6d9-fe946d905b4d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1341, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("658da66d-ce4a-48ac-962e-431478871558", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1342, "Variable Update", j, "index:p");
global::AutoRepairTracing.TraceLogger.LogValue("0e878ab1-10e5-45b1-931c-14a8c08726ff", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2113, "Variable Update", specifiedSource = (bool)p[j], "specifiedSource");
}{global::AutoRepairTracing.TraceLogger.Log("764e9171-6dd0-49ad-abbe-cd1e64564b9d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1343, "Break");
                            break;
}                        }
                    }
                }

                if (pLength > i)
                {
global::AutoRepairTracing.TraceLogger.Log("28910c3a-d671-4ed7-8829-e7e3bd570fb3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1349, "If-True Branch");
                    if (specifiedSource == null || specifiedSource.Value)
                    {
global::AutoRepairTracing.TraceLogger.Log("01a2c171-db72-4db1-b9b9-faf7d7f25bd3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1351, "If-True Branch");

                        object source = p[i];
global::AutoRepairTracing.TraceLogger.Log("6802c975-d818-4f78-bb85-44d19fc00e98", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1353, "Variable Update", source, "source");
                        object? enumSource = null;
global::AutoRepairTracing.TraceLogger.Log("cd4bf2bd-f52b-4fef-95c6-844042941653", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1354, "Variable Update", enumSource, "enumSource");
                        if (member.ChoiceIdentifier != null)
                        {
global::AutoRepairTracing.TraceLogger.Log("4eeabd8a-1676-4efe-8fc0-c948776fa581", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1356, "If-True Branch");
                            for (int j = 0; j < mapping.Members.Length; global::AutoRepairTracing.TraceLogger.LogValue("d79de1d7-a884-491f-b632-c5a102be4d32", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2132, "Variable Update", j++, "j"))
                            {
global::AutoRepairTracing.TraceLogger.Log("b61cba75-4c85-458b-94c0-8eef864acee8", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1357, "For Loop");
                                if (mapping.Members[j].Name == member.ChoiceIdentifier.MemberName)
                                {
global::AutoRepairTracing.TraceLogger.Log("48545ccf-db78-4140-aca2-f3cf1863d4f3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1360, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("5f8a3873-5cce-40dd-a2c7-e1e5ba74a72e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1361, "Variable Update", j, "index:p");
global::AutoRepairTracing.TraceLogger.LogValue("8eb1f754-56ea-4e67-b0b9-c8a48b99293b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2138, "Variable Update", enumSource = p[j], "enumSource");
}{global::AutoRepairTracing.TraceLogger.Log("716bc607-b2de-4956-8e23-c7321b51a4f3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1362, "Break");
                                    break;
}                                }
                            }
                        }

                        if (isRpc && member.IsReturnValue && member.Elements!.Length > 0)
                        {
global::AutoRepairTracing.TraceLogger.Log("ee4d27bf-5880-4d1f-981a-cb434b47db0c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1368, "If-True Branch");
{{global::AutoRepairTracing.TraceLogger.Log("e0f45043-d70f-443c-8aaa-6fbe99bdca2b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2148, "Variable Update", 0, "index:member.Elements");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("52950b32-c65a-48a9-bc40-60fbfbbcc520", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1369, "WriteRpcResult", 0, member.Elements[0].Name, "member.Elements[0].Name");
}global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("1acd1399-8bed-44ba-8576-0e23ecd25909", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1369, "WriteRpcResult", 1, string.Empty, "string.Empty");
{global::AutoRepairTracing.TraceLogger.Log("78824b6c-a755-4502-9dcd-fc0d44bbc803", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1369, "Variable Update", 0, "index:member.Elements");
                            WriteRpcResult(member.Elements[0].Name, string.Empty);
}}                        }
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("d58f34fd-38a3-4a83-be09-53bbe24a7125", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1373, "WriteMember", 0, source, "source");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("9ab95026-7249-47bc-9b01-488ec9918d4c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1373, "WriteMember", 1, enumSource, "enumSource");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("763108d6-4305-4702-a50b-a518542ddf14", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1373, "WriteMember", 2, member.ElementsSortedByDerivation!, "member.ElementsSortedByDerivation!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("ddf57693-c83f-4bc6-9fba-0a5400fbc4ef", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1373, "WriteMember", 3, member.Text, "member.Text");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("34507cf0-64ba-481a-8177-4b6519231568", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1373, "WriteMember", 4, member.ChoiceIdentifier, "member.ChoiceIdentifier");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("04846be0-33c0-4ffe-9d45-6d0e9733d015", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1373, "WriteMember", 5, member.TypeDesc!, "member.TypeDesc!");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("c6c4c152-7bb5-4477-85e9-0432871a51a4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1373, "WriteMember", 6, writeAccessors || hasWrapperElement, "writeAccessors || hasWrapperElement");

                        // override writeAccessors choice when we've written a wrapper element
                        WriteMember(source, enumSource, member.ElementsSortedByDerivation!, member.Text, member.ChoiceIdentifier, member.TypeDesc!, writeAccessors || hasWrapperElement);
}                    }
                }
            }
}
            if (hasWrapperElement)
            {
global::AutoRepairTracing.TraceLogger.Log("804b2632-a4b1-41bd-b75e-c809a326307b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1379, "If-True Branch");
                WriteEndElement();
            }

            if (element.IsSoap)
            {
global::AutoRepairTracing.TraceLogger.Log("23968233-2bfa-4eb0-a14b-25c93f91fe20", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1384, "If-True Branch");
                if (!hasWrapperElement && !writeAccessors)
                {
global::AutoRepairTracing.TraceLogger.Log("d3a4c627-e100-4c83-a2cb-b73271026dca", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1386, "If-True Branch");
                    // doc/bare case -- allow extra members
                    if (pLength > mapping.Members.Length)
                    {
global::AutoRepairTracing.TraceLogger.Log("6e9ff499-51ec-4196-ae43-820846acb720", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1389, "If-True Branch");
{int __loopCounter_50492c0e = 0;global::AutoRepairTracing.TraceLogger.Log("74a702b2-53f6-4253-bec6-9442a85fc153", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2182, "Variable Update", __loopCounter_50492c0e, "__loopCounter_50492c0e");
                        for (int i = mapping.Members.Length; i < pLength; global::AutoRepairTracing.TraceLogger.LogValue("fdcb58ad-6d21-4fa4-9af2-e846e44519b4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2182, "Variable Update", i++, "i"))
                        {
global::AutoRepairTracing.TraceLogger.Log("8f0e0baa-e048-4788-8e51-01993b5d5ca4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1390, "For Loop");
global::AutoRepairTracing.TraceLogger.LogValue("e398fcd3-fc28-45c4-ae19-2fed50e55af3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2185, "Variable Update", __loopCounter_50492c0e++, "__loopCounter_50492c0e");if(__loopCounter_50492c0e > 10000){global::AutoRepairTracing.TraceLogger.Log("fe26979c-a185-49fc-9e03-25201edd4b48", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1420, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("50492c0e-2de7-4635-bd9a-96ad51324011", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1390, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_50492c0e, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("bce248ae-b933-4691-8c8e-cbcc18b59524", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1420, "Break");
break;}}                            if (p[i] != null)
                            {
global::AutoRepairTracing.TraceLogger.Log("2f824e21-ff53-4a3b-8457-7f68b1e44d7b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1393, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("678fca43-31c6-4375-a11a-c23f559e094d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1394, "WritePotentiallyReferencingElement", 0, null, "null");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("92dcdf8c-2d94-4947-b273-7ec02a7e6422", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1394, "WritePotentiallyReferencingElement", 1, null, "null");
{global::AutoRepairTracing.TraceLogger.Log("51097aab-c7c2-488e-8a0a-472fa4b4067f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2192, "Variable Update", i, "index:p");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("89225641-e6bc-45f3-960f-f498d474fd8b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1394, "WritePotentiallyReferencingElement", 2, p[i], "p[i]");
}{global::AutoRepairTracing.TraceLogger.Log("bdf3f7f2-dc5b-43ec-9e97-18c779839d22", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2193, "Variable Update", i, "index:p");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("a0d9d544-6905-455f-a9c6-49470acd9f4a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1394, "WritePotentiallyReferencingElement", 3, p[i].GetType(), "p[i].GetType()");
}global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("e043e5a7-e862-41f2-ae1f-6f7cdff0f904", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1394, "WritePotentiallyReferencingElement", 4, true, "true");
global::AutoRepairTracing.TraceLogger.LogArrayAliasArgument("e8ee2822-e1db-4f8f-b1c3-ca2e05b2f570", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1394, "WritePotentiallyReferencingElement", 5, false, "false");
{global::AutoRepairTracing.TraceLogger.Log("d4eaa37d-700f-4fe1-a511-ff1f198e430d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1394, "Variable Update", i, "index:p");
global::AutoRepairTracing.TraceLogger.Log("16456c6b-bee5-4823-ba3c-3501892c38ab", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1394, "Variable Update", i, "index:p");
                                WritePotentiallyReferencingElement(null, null, p[i], p[i].GetType(), true, false);
}}                            }
                        }
}                    }
                }

                WriteReferencedElements();
            }
        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("69338450-6c0b-41c8-887d-6c1b0e15da94", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1264, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("2457b347-4dba-4f39-88ee-02aaf63e28b3", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1431, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("60ac2100-5f68-48c6-8dbc-efded6013560", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1264, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("e9d36b64-3a31-45a3-8ff2-b86d3956ce3b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1264, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("389fe5e2-21de-4395-88bf-452b9c537c1c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1264, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
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
{global::AutoRepairTracing.TraceLogger.Log("e27d09f5-4807-4a6c-9145-1b184c158425", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1419, "Enter Method 'GetMember'");
try        {
            MemberInfo[] memberInfos = declaringType.GetMember(memberName);
global::AutoRepairTracing.TraceLogger.Log("68456109-55f7-4c48-bad3-26caaf650dc8", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1421, "Heap State: memberInfos (MemberInfo[])");global::AutoRepairTracing.TraceLogger.Log("7ae42cec-9c9a-4bb0-8f0a-8f1c8266be41", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1421, "Variable Update", memberInfos, "memberInfos");
            if (memberInfos == null || memberInfos.Length == 0)
            {
global::AutoRepairTracing.TraceLogger.Log("1ab4e037-8bfe-4c14-9b3a-d52b8c5ae0e7", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1423, "If-True Branch");
                bool foundMatchedMember = false;
global::AutoRepairTracing.TraceLogger.Log("067f2be2-3170-48b9-932a-e204a2edee65", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1424, "Variable Update", foundMatchedMember, "foundMatchedMember");
                Type? currentType = declaringType.BaseType;
global::AutoRepairTracing.TraceLogger.Log("f84b2a11-4747-4b1c-923d-9c7978ea9679", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1425, "Variable Update", currentType, "currentType");
{int __loopCounter_182b3806 = 0;global::AutoRepairTracing.TraceLogger.Log("9dbd8cb8-5ee2-4546-a47b-f1687dc9ab28", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2232, "Variable Update", __loopCounter_182b3806, "__loopCounter_182b3806");
                while (currentType != null)
                {
global::AutoRepairTracing.TraceLogger.Log("3358c537-649e-48c8-86a0-8d74ab733e6f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1426, "While Loop");
global::AutoRepairTracing.TraceLogger.LogValue("ac38d3fe-6956-4113-810e-54f468d72834", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2235, "Variable Update", __loopCounter_182b3806++, "__loopCounter_182b3806");if(__loopCounter_182b3806 > 10000){global::AutoRepairTracing.TraceLogger.Log("578b6dfa-575f-47e3-8c3c-766fa41ea313", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1457, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("182b3806-d8c2-49e1-932e-bbbc42cc0132", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1426, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_182b3806, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("6f001694-efb1-4a17-8537-f02685ec3d06", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1457, "Break");
break;}}global::AutoRepairTracing.TraceLogger.LogValue("5cdcbedb-94fd-4ac0-9db2-64bbea1d297a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2237, "Variable Update", memberInfos = currentType.GetMember(memberName), "memberInfos");
                    if (memberInfos != null && memberInfos.Length != 0)
                    {
global::AutoRepairTracing.TraceLogger.Log("a1dc63a3-3175-4cd9-8946-c516949e82cf", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1430, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("abf0fdf8-3375-4a03-971e-73f1d6e0e1e1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2241, "Variable Update", foundMatchedMember = true, "foundMatchedMember");
{global::AutoRepairTracing.TraceLogger.Log("e268e5a4-035c-4664-99d8-e1b6f3140f91", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1432, "Break");
                        break;
}                    }
global::AutoRepairTracing.TraceLogger.LogValue("bf2cd5f8-783d-4bf6-b9fb-255242e98087", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2246, "Variable Update", currentType = currentType.BaseType, "currentType");
                }
}
                if (!foundMatchedMember)
                {
global::AutoRepairTracing.TraceLogger.Log("cb7638db-8c94-4cc1-84f8-dbb499e7ed6c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1439, "If-True Branch");
                    if (throwOnNotFound)
                    {
global::AutoRepairTracing.TraceLogger.Log("d03cf5c3-7e7b-4ff8-9643-6f4a9a97815b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1441, "If-True Branch");
                        throw new InvalidOperationException(SR.Format(SR.XmlInternalErrorDetails, $"Could not find member named {memberName} of type {declaringType}"));
                    }
{global::AutoRepairTracing.TraceLogger.Log("20c975da-c8b0-48c6-89d2-6400eedbec7f", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1444, "Return");
                    return null;
}                }
global::AutoRepairTracing.TraceLogger.LogValue("b82112eb-87a0-4617-9fbd-467ba3046441", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2261, "Variable Update", declaringType = currentType!, "declaringType");
            }

            MemberInfo memberInfo = memberInfos![0];
global::AutoRepairTracing.TraceLogger.Log("4ffff918-f076-4cb0-b6c4-097a3027f829", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1450, "Variable Update", memberInfo, "memberInfo");
            if (memberInfos.Length != 1)
            {
global::AutoRepairTracing.TraceLogger.Log("59cf9f1a-0247-4c29-9116-fccebbb3811a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1452, "If-True Branch");
                foreach (MemberInfo mi in memberInfos)
                {
global::AutoRepairTracing.TraceLogger.Log("c8fccac5-edee-4e2b-946a-5bf23f75da6b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1453, "ForEach Loop");
                    if (declaringType == mi.DeclaringType)
                    {
global::AutoRepairTracing.TraceLogger.Log("0e10bff6-5678-4eb4-8dca-f945077f43ad", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1456, "If-True Branch");
global::AutoRepairTracing.TraceLogger.LogValue("ca9ec2be-181e-4a8b-bada-23be7bf88464", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2274, "Variable Update", memberInfo = mi, "memberInfo");
{global::AutoRepairTracing.TraceLogger.Log("036d7390-f3f7-43c8-9599-339f06924672", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1458, "Break");
                        break;
}                    }
                }
            }
{global::AutoRepairTracing.TraceLogger.Log("3b1d073c-3153-419e-bd16-9dadec806505", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1463, "Return");

            return memberInfo;
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("0a25a680-101f-4b85-90f2-fcf0bdefc5bd", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1419, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("27b82165-ff72-4b5a-bd30-2eec86edde69", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1494, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("24542fda-1699-4be1-af9d-d82856138bc1", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1419, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("0a5ce53a-87c2-405b-9764-1909c600e797", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1419, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("ba4c29b1-0624-4d30-bb9b-c7d9b7a0d58a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1419, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        public static MemberInfo GetEffectiveGetInfo(Type declaringType, string memberName)
{global::AutoRepairTracing.TraceLogger.Log("44b783b0-a932-407d-b408-22ce52e370ab", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1466, "Enter Method 'GetEffectiveGetInfo'");
try        {
            MemberInfo memberInfo = GetMember(declaringType, memberName, true)!;
global::AutoRepairTracing.TraceLogger.Log("c1bcb138-2eee-4aca-ab2d-977417b92952", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1468, "Variable Update", memberInfo, "memberInfo");

            // For properties, we might have a PropertyInfo that does not have a valid
            // getter at this level of inheritance. If that's the case, we need to look
            // up the chain to find the right PropertyInfo for the getter.
            if (memberInfo is PropertyInfo propInfo && propInfo.GetMethod == null)
            {
global::AutoRepairTracing.TraceLogger.Log("da6044a4-5618-4cce-b6b8-254cdf578b7b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1474, "If-True Branch");
                var parent = declaringType.BaseType;
global::AutoRepairTracing.TraceLogger.Log("730fd6a9-092f-4afc-ba93-f7dcd3311d13", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1475, "Variable Update", parent, "parent");
{int __loopCounter_054051d8 = 0;global::AutoRepairTracing.TraceLogger.Log("3c10341e-52f1-48fc-93cf-01e4e4bb18fa", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2299, "Variable Update", __loopCounter_054051d8, "__loopCounter_054051d8");

                while (parent != null)
                {
global::AutoRepairTracing.TraceLogger.Log("4398f722-2ca9-4618-8d51-2654e2f97fad", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1477, "While Loop");
global::AutoRepairTracing.TraceLogger.LogValue("adc77f0e-6441-4460-aa9b-1b12bfa0327c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2303, "Variable Update", __loopCounter_054051d8++, "__loopCounter_054051d8");if(__loopCounter_054051d8 > 10000){global::AutoRepairTracing.TraceLogger.Log("d40d9d7b-c319-4eb4-a6a0-3e27ee5f1960", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1509, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("054051d8-27d0-4427-bb48-ea9815ae6e18", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1477, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_054051d8, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("adb904c8-364a-4b34-b883-647eb6838d02", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1509, "Break");
break;}}                    var mi = GetMember(parent, memberName, false);
global::AutoRepairTracing.TraceLogger.Log("e951aa8c-2cd7-4fb1-b889-0f2bc9c8e6be", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1479, "Variable Update", mi, "mi");

                    if (mi is PropertyInfo pi && pi.GetMethod != null && pi.PropertyType == propInfo.PropertyType)
                    {
global::AutoRepairTracing.TraceLogger.Log("63c28ebf-604b-4081-9226-df9d8ff57719", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1482, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("18c179fb-e047-49db-a5a8-8852f51e7c4d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1483, "Return");
                        return pi;
}                    }
global::AutoRepairTracing.TraceLogger.LogValue("59798fcd-192d-4bdd-8146-937ac4777f8b", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2314, "Variable Update", parent = parent.BaseType, "parent");
                }
}            }
{global::AutoRepairTracing.TraceLogger.Log("6f7a8cf6-8e26-445e-8bef-c5bd2330806c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1490, "Return");

            return memberInfo;
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("36d52a1d-4369-4183-a4df-31703e5cb8c4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1466, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("cf3a05e7-ab8e-4a35-9f97-78a340786984", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1522, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("98b96b2b-827b-41d4-8ca8-aa9e55016158", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1466, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("9303d0e1-90b9-4128-9e5a-bbc31e0b0088", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1466, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("aa16e704-0f67-4cce-a968-0557d4c273b2", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1466, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}
        public static MemberInfo GetEffectiveSetInfo(Type declaringType, string memberName)
{global::AutoRepairTracing.TraceLogger.Log("d1be66f3-a749-48ed-86a6-c6fea1b08dbd", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1493, "Enter Method 'GetEffectiveSetInfo'");
try        {
            MemberInfo memberInfo = GetMember(declaringType, memberName, true)!;
global::AutoRepairTracing.TraceLogger.Log("b43f6d0b-540f-4f11-95ce-8a4df1c7215c", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1495, "Variable Update", memberInfo, "memberInfo");

            // For properties, we might have a PropertyInfo that does not have a valid
            // setter at this level of inheritance. If that's the case, we need to look
            // up the chain to find the right PropertyInfo for the setter.
            if (memberInfo is PropertyInfo propInfo && propInfo.SetMethod == null)
            {
global::AutoRepairTracing.TraceLogger.Log("be3f0667-079e-445d-b296-0ba08f0c2fe5", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1501, "If-True Branch");
                var parent = declaringType.BaseType;
global::AutoRepairTracing.TraceLogger.Log("be746b51-abcf-4c23-a09c-34d36a91a015", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1502, "Variable Update", parent, "parent");
{int __loopCounter_0141a03e = 0;global::AutoRepairTracing.TraceLogger.Log("bab6197c-8e6f-48c6-aa52-4adba313468d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2336, "Variable Update", __loopCounter_0141a03e, "__loopCounter_0141a03e");

                while (parent != null)
                {
global::AutoRepairTracing.TraceLogger.Log("90153122-286d-4f45-a546-98b8a0e8adac", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1504, "While Loop");
global::AutoRepairTracing.TraceLogger.LogValue("e4cf219f-5878-4dd1-990e-9441d9dabac9", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2340, "Variable Update", __loopCounter_0141a03e++, "__loopCounter_0141a03e");if(__loopCounter_0141a03e > 10000){global::AutoRepairTracing.TraceLogger.Log("bb8d21d3-02a9-4e14-b365-73dfc0f17a5d", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1537, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("0141a03e-3c54-458b-8c64-8841b752fd64", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1504, "Infinite Loop Warning: exceeded 10000 iterations", __loopCounter_0141a03e, "loopCounter");{global::AutoRepairTracing.TraceLogger.Log("875d740c-74ba-49c5-8cc0-6e05c3411da9", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1537, "Break");
break;}}                    var mi = GetMember(parent, memberName, false);
global::AutoRepairTracing.TraceLogger.Log("6288e836-f497-4083-a157-9eb3acdb7430", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1506, "Variable Update", mi, "mi");

                    if (mi is PropertyInfo pi && pi.SetMethod != null && pi.PropertyType == propInfo.PropertyType)
                    {
global::AutoRepairTracing.TraceLogger.Log("316a3a80-011a-4328-b4e0-88ed3a4d01b7", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1509, "If-True Branch");
{global::AutoRepairTracing.TraceLogger.Log("29e5bcf6-23c8-480b-b615-5696f0c7e2ff", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1510, "Return");
                        return pi;
}                    }
global::AutoRepairTracing.TraceLogger.LogValue("b45dd069-e3d0-4439-8755-a7250ddac3d4", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 2351, "Variable Update", parent = parent.BaseType, "parent");
                }
}            }
{global::AutoRepairTracing.TraceLogger.Log("48def960-6f67-40a9-838f-cde2c842c329", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1517, "Return");

            return memberInfo;
}        }
catch (System.Exception ex) {global::AutoRepairTracing.TraceLogger.Log("c77b0606-f3be-4e12-b411-3f9a90d96a57", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1493, "Exception Caught", ex.Message, "ex.Message");if (ex is System.NullReferenceException) { global::AutoRepairTracing.TraceLogger.Log("fe062b0d-c5cf-4bc2-a981-da8e82ebba78", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1550, "If-True Branch");
global::AutoRepairTracing.TraceLogger.Log("c0c91d60-c0bb-464e-ac93-c712d7cc9ac5", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1493, "Null Dereference", ex.Message, "null.dereference"); }global::AutoRepairTracing.TraceLogger.Log("968d6174-dded-4ccb-a07e-fa3708a7f77a", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1493, "Exception Type", ex.GetType().FullName, "ex.Type");global::AutoRepairTracing.TraceLogger.Log("29c44b87-71ec-496c-a14d-dec46ac2d05e", "System\\Xml\\Serialization\\ReflectionXmlSerializationWriter.cs", 1493, "Exception Stack", ex.StackTrace ?? string.Empty, "ex.Stack");System.Diagnostics.Debug.Fail("Unhandled exception", ex.ToString());throw;
}}    }
}

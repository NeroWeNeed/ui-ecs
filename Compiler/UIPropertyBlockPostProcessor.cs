using System;
using System.Collections.Generic;
using Mono.Cecil;
using NeroWeNeed.Commons.AssemblyAnalyzers.Editor;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;
using Unity.Entities;

namespace NeroWeNeed.UIECS.Compiler
{
    public class UIPropertyBlockPostProcessor : ILPostProcessor
    {
        private CecilPostProcessor postProcessor = new CecilPostProcessor();
        public override ILPostProcessor GetInstance()
        {
            return this;
        }
        public override ILPostProcessResult Process(ICompiledAssembly compiledAssembly)
        {
            return postProcessor.Process(compiledAssembly);
        }
        public override bool WillProcess(ICompiledAssembly compiledAssembly)
        {
            return postProcessor.WillProcess(compiledAssembly);
        }
        public class CecilPostProcessor : UIElementCecilPostProcessor
        {
            public override bool Process(AssemblyDefinition assemblyDefinition, List<DiagnosticMessage> diagnostics)
            {
                var propertyBlocks = new List<TypeReference>();
                var elements = new List<TypeReference>();
                bool changed = false;
                foreach (var moduleDefinition in assemblyDefinition.Modules)
                {
                    foreach (var typeDefinition in moduleDefinition.Types)
                    {
                        if (typeDefinition.HasInterface<IPropertyBlock>() && typeDefinition.IsValueType)
                        {
                            propertyBlocks.Add(typeDefinition);
                        }
                        else if (typeDefinition.HasInterface<IUINode>() && typeDefinition.IsValueType)
                        {
                            elements.Add(typeDefinition);
                        }
                    }
                }
                    var registerGenericComponentAttribute = assemblyDefinition.MainModule.ImportReference(typeof(RegisterGenericComponentTypeAttribute).GetConstructor(new Type[] { typeof(Type) }));
                if (propertyBlocks.Count > 0)
                {
                    foreach (var propertyBlock in propertyBlocks)
                    {
                        var attr = new CustomAttribute(registerGenericComponentAttribute);
                        var type = new GenericInstanceType(assemblyDefinition.MainModule.ImportReference(typeof(UIPropertyBlockTag<>)));
                        type.GenericArguments.Add(propertyBlock);
                        attr.ConstructorArguments.Add(new CustomAttributeArgument(assemblyDefinition.MainModule.TypeSystem.TypedReference, type));
                        assemblyDefinition.CustomAttributes.Add(attr);
                    }

                    changed = true;
                }
                if (elements.Count > 0)
                {
                    foreach (var element in elements)
                    {
                        var attr = new CustomAttribute(registerGenericComponentAttribute);
                        var type = new GenericInstanceType(assemblyDefinition.MainModule.ImportReference(typeof(UIElementTag<>)));
                        type.GenericArguments.Add(element);
                        attr.ConstructorArguments.Add(new CustomAttributeArgument(assemblyDefinition.MainModule.TypeSystem.TypedReference, type));
                        assemblyDefinition.CustomAttributes.Add(attr);
                    }
                    changed = true;
                }
                return changed;
            }
        }
    }
}
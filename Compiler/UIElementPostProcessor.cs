using System;
using System.Collections.Generic;
using System.IO;
using Mono.Cecil;
using Mono.Cecil.Cil;
using Unity.CompilationPipeline.Common.Diagnostics;
using Unity.CompilationPipeline.Common.ILPostProcessing;

namespace NeroWeNeed.UIECS.Compiler {
    public abstract class UIElementCecilPostProcessor {
        public const string UIECSEditorAssemblyName = "NeroWeNeed.UIECS.Editor";
        public const string UIECSManagerAssemblyName = "NeroWeNeed.UIECS.Manager";
        public const string UIECSCompilerAssemblyName = "Unity.NeroWeNeed.UIECS.Compiler";

        public ILPostProcessResult Process(ICompiledAssembly compiledAssembly) {
            if (!WillProcess(compiledAssembly))
                return null;
            var assemblyDefinition = MonoUtility.GetAssemblyDefinition(compiledAssembly);
            var diagnostics = new List<DiagnosticMessage>();
            if (Process(assemblyDefinition, diagnostics)) {
                var pe = new MemoryStream();
                var pdb = new MemoryStream();
                var writerParameters = new WriterParameters
                {
                    SymbolWriterProvider = new PortablePdbWriterProvider(), SymbolStream = pdb, WriteSymbols = true
                };

                assemblyDefinition.Write(pe, writerParameters);
                return new ILPostProcessResult(new InMemoryAssembly(pe.ToArray(), pdb.ToArray()), diagnostics);
            }
            else {
                return new ILPostProcessResult(null, diagnostics);
            }
        }
        public abstract bool Process(AssemblyDefinition assemblyDefinition, List<DiagnosticMessage> diagnostics);

        public bool WillProcess(ICompiledAssembly compiledAssembly) {
            if (compiledAssembly.Name == UIECSEditorAssemblyName || compiledAssembly.Name == UIECSManagerAssemblyName || compiledAssembly.Name == UIECSCompilerAssemblyName) {
                return false;
            }
            foreach (var reference in compiledAssembly.References) {
                var fileName = Path.GetFileNameWithoutExtension(reference);
                if (fileName == UIECSManagerAssemblyName) {
                    return true;
                }
            }
            return false;
        }
    }
}
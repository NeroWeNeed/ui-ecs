using System;
using Mono.Cecil;
using UnityEditor;

namespace NeroWeNeed.UIECS.Editor
{
    public static class UIModelAssemblyBuilder
    {
        public static bool BuildAssembly(string directory, string modelGuid, UIModel model, out string assemblyPath)
        {
            assemblyPath = $"{directory}/{model.assembly}.dll";
            var resolver = new DefaultAssemblyResolver();
            resolver.AddSearchDirectory("Library/ScriptAssemblies");
            resolver.AddSearchDirectory($"{EditorApplication.applicationContentsPath}/Managed");
            resolver.AddSearchDirectory($"{EditorApplication.applicationContentsPath}/Managed/UnityEngine");
            using var assemblyDefinition = AssemblyDefinition.CreateAssembly(new AssemblyNameDefinition(model.assembly, new Version(0, 0, 0, 0)), "UIECS.Models", new ModuleParameters { AssemblyResolver = resolver });
            var shouldWrite = false;
            shouldWrite |= GenerateComponents(assemblyDefinition, assemblyDefinition.MainModule, model);
            if (shouldWrite)
            {
                assemblyDefinition.Write(assemblyPath);
                return true;
            }
            else
            {
                assemblyPath = null;
                return false;
            }
        }
        private static bool GenerateComponents(AssemblyDefinition assemblyDefinition, ModuleDefinition moduleDefinition, UIModel model)
        {
            
            var changed = false;
            foreach (var property in model.properties)
            {
                var type = model.GetProvider(property).CreateType(model, property,assemblyDefinition, moduleDefinition);
                moduleDefinition.Types.Add(type);
                changed = true;
            }
            return changed;
        }
    }
}
using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using Microsoft.CSharp;

namespace HerhangiOT.ServerLibrary
{
    public class ScriptManager
    {
        public static bool LoadCsScripts()
        {
            Logger.LogOperationStart("Loading .cs Scripts");

            Logger.LogOperationDone();
            return true;
        }

        public static bool LoadLuaScripts()
        {
            Logger.LogOperationStart("Loading .lua Scripts");

            Logger.LogOperationDone();
            return true;
        }

        public static bool CompileCsScripts(string path, string outputPath, List<string> externalAssemblies, out Assembly assembly)
        {
            if (!Directory.Exists(path))
            {
                Logger.LogOperationFailed("CsScript directory could not be found: "+path+"!");
                assembly = null;
                return false;
            }
            string[] files = Directory.GetFiles(path, "*.cs");
            if (files.Length == 0)
            {
                Logger.LogOperationFailed("No files found in CsScript directory: "+path+"!");
                assembly = null;
                return false;
            }
            
            var codeProvider = new CSharpCodeProvider();
            var compilerParameters = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = false,
                IncludeDebugInformation = true,
                OutputAssembly = outputPath
            };
            compilerParameters.ReferencedAssemblies.Add(Assembly.GetExecutingAssembly().Location);
            foreach (string externalAssembly in externalAssemblies)
            {
                compilerParameters.ReferencedAssemblies.Add(externalAssembly);
            }

            CompilerResults result = codeProvider.CompileAssemblyFromFile(compilerParameters, files); // Compile

            if (result.Errors.Count > 0)
            {
                string errorText = String.Empty;
                foreach (CompilerError compilerError in result.Errors)
                    errorText += compilerError + "\n";

                Logger.LogOperationFailed("CsScript compilation error: " + errorText);
                assembly = null;
                return false;
            }

            assembly = result.CompiledAssembly;
            return true;
        }
    }
}

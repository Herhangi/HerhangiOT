using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using HerhangiOT.ServerLibrary;
using HerhangiOT.ServerLibrary.Utility;
using Microsoft.CSharp;

namespace HerhangiOT.ScriptLibrary
{
    public enum ExternalDll { CLO }

    public class ScriptManager
    {
        protected  static List<Assembly> RemovableAssemblies = new List<Assembly>(); 
        protected static Dictionary<ExternalDll, Assembly> ReferencedAssemblies = new Dictionary<ExternalDll, Assembly>(); 
        public static Dictionary<string, Action<string[]>> CommandLineOperations = new Dictionary<string, Action<string[]>>();

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

        private static void RemoveOldDlls()
        {
            for (int i = RemovableAssemblies.Count - 1; i > -1; i--)
            {
                string location = RemovableAssemblies[i].Location;
                RemovableAssemblies.RemoveAt(i);
                File.Delete(location);
            }
        }

        public static bool LoadCommandLineOperations()
        {
            Logger.LogOperationStart("Loading Command Line Operations");

            string dllName = string.Format("CLO.{0:yyyyMMdd_HHmmss}.dll", DateTime.Now);
            Assembly cloAssembly;
            List<string> externalAssemblies = new List<string>();
            externalAssemblies.Add(Assembly.GetExecutingAssembly().Location);
            //externalAssemblies.Add(Assembly.GetAssembly(typeof(Tools)).Location);
            externalAssemblies.Add(Assembly.GetAssembly(typeof(Constants)).Location);
            externalAssemblies.Add(Assembly.GetAssembly(typeof(CommandLineOperation)).Location);

            if (!Directory.Exists("CompiledDllCache"))
                Directory.CreateDirectory("CompiledDllCache");

            if (!CompileCsScripts("Scripts/CLO", "CompiledDllCache/" + dllName, externalAssemblies, out cloAssembly))
                return false;

            // Removing if compilation is successful so that we are not out of current assembly if compilation fails
            CommandLineOperations.Clear();
            if (ReferencedAssemblies.ContainsKey(ExternalDll.CLO))
            {
                RemovableAssemblies.Add(ReferencedAssemblies[ExternalDll.CLO]);
                ReferencedAssemblies.Remove(ExternalDll.CLO);
            }
            ReferencedAssemblies.Add(ExternalDll.CLO, cloAssembly);
            RemoveOldDlls();

            try
            {
                foreach (Type clo in cloAssembly.GetTypes())
                {
                    if (clo.BaseType == typeof(CommandLineOperation))
                    {
                        CommandLineOperation voc = (CommandLineOperation)Activator.CreateInstance(clo);

                        voc.Setup();
                        CommandLineOperations.Add(voc.Command, voc.Operation);
                    }
                }
            }
            catch (Exception e)
            {
                Logger.LogOperationFailed(e.ToString());
                return false;
            }

            Logger.LogOperationDone();
            return true;
        }
    }
}

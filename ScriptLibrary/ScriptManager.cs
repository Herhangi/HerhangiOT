using System;
using System.CodeDom.Compiler;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using HerhangiOT.GameServerLibrary.Model;
using HerhangiOT.GameServerLibrary.Model.Vocations;
using HerhangiOT.ServerLibrary;
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

        public static bool CompileCsScripts(string path, string outputDllPattern, List<string> externalAssemblies, out Assembly assembly, out bool redFromCache, bool forceCompilation = false)
        {
            redFromCache = false;
            if (!Directory.Exists(path))
            {
                Logger.LogOperationFailed("CsScript directory could not be found: "+path+"!");
                assembly = null;
                return false;
            }
            string[] files = Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
            if (files.Length == 0)
            {
                Logger.LogOperationFailed("No files found in CsScript directory: "+path+"!");
                assembly = null;
                return false;
            }

            if (!Directory.Exists("CompiledDllCache"))
                Directory.CreateDirectory("CompiledDllCache");

            if (!forceCompilation)
            {
                string cachedDll = GetCachedDllAndRemoveOlds(outputDllPattern);

                if (cachedDll != null)
                {
                    DateTime lastCompileTime = new FileInfo(cachedDll).LastWriteTime;
                    DateTime lastEditTime = GetNewestFile(new DirectoryInfo("Scripts/CLO")).LastWriteTime;

                    if (lastEditTime < lastCompileTime)
                    {
                        redFromCache = true;
                        assembly = Assembly.LoadFile(Path.Combine(Environment.CurrentDirectory, cachedDll));
                        return true;
                    }
                }
            }

            List<string> referencedAssemblies = new List<string>();
            referencedAssemblies.Add(Assembly.GetEntryAssembly().Location); // GameServer or LoginServer
            referencedAssemblies.Add(Assembly.GetAssembly(typeof(Constants)).Location); //ServerLibrary
            referencedAssemblies.Add(Assembly.GetAssembly(typeof(Vocation)).Location); //GameServerLibrary
            referencedAssemblies.Add(Assembly.GetAssembly(typeof(CommandLineOperation)).Location); //ServiceLibrary
            //referencedAssemblies.Add("System.dll");
            if (externalAssemblies != null) referencedAssemblies.AddRange(externalAssemblies); //Assemblies sent from compilation requester
            //Add assemblies in "assembly" file found in compiled folder
            if (File.Exists(Path.Combine(path, "assembly")))
            {
                foreach (string assemblyName in File.ReadAllLines(Path.Combine(path, "assembly")))
                {
                    if(!string.IsNullOrWhiteSpace(assemblyName) && !assemblyName.StartsWith("//"))
                        referencedAssemblies.Add(assemblyName);
                }
            }

            string outputFile = "CompiledDllCache/" + (outputDllPattern.Replace("*", string.Format("{0:yyyyMMdd_HHmmss}", DateTime.Now)));

            var codeProvider = new CSharpCodeProvider();
            var compilerParameters = new CompilerParameters
            {
                GenerateExecutable = false,
                GenerateInMemory = false,
                IncludeDebugInformation = true,
                OutputAssembly = outputFile
            };
            foreach (string referencedAssembly in referencedAssemblies)
            {
                compilerParameters.ReferencedAssemblies.Add(referencedAssembly);
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

        public static bool LoadCommandLineOperations(bool forceCompilation = false)
        {
            Logger.LogOperationStart("Loading Command Line Operations");
            bool readFromCache;

            Assembly cloAssembly;
            
            if (!CompileCsScripts("Scripts/CLO", "CLO.*.dll", null, out cloAssembly, out readFromCache, forceCompilation))
                return false;

            // Removing if compilation is successful so that we are not out of current assembly if compilation fails
            CommandLineOperations.Clear();
            if (ReferencedAssemblies.ContainsKey(ExternalDll.CLO))
            {
                RemovableAssemblies.Add(ReferencedAssemblies[ExternalDll.CLO]);
                ReferencedAssemblies.Remove(ExternalDll.CLO);
            }
            ReferencedAssemblies.Add(ExternalDll.CLO, cloAssembly);

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

            if(readFromCache)
                Logger.LogOperationCached();
            else
                Logger.LogOperationDone();
            return true;
        }

        private static string GetCachedDllAndRemoveOlds(string pattern)
        {
            if (!Directory.Exists("CompiledDllCache"))
                Directory.CreateDirectory("CompiledDllCache");

            string[] files = Directory.GetFiles("CompiledDllCache", pattern);

            for (int i = 0; i < files.Length - 1; i++)
            {
                try
                {
                    File.Delete(files[i]);
                    File.Delete(files[i].Replace(".dll", ".pdb"));   
                }
                catch
                {
                    // THIS IS UNIMPORTANT, DLLs ARE USED BY CURRENT PROCESS WILL BE DELETED ON NEXT TIME WE EXECUTE PROGRAM
                }
            }

            if (files.Length > 0)
                return files[files.Length - 1];
            return null;
        }

        private static FileInfo GetNewestFile(DirectoryInfo directory)
        {
            return directory.GetFiles()
               .Union(directory.GetDirectories().Select(GetNewestFile))
               .OrderByDescending(f => (f == null ? DateTime.MinValue : f.LastWriteTime))
               .FirstOrDefault();
        }

        #region Vocations
        public static bool LoadVocations(bool forceCompilation = false)
        {
            Logger.LogOperationStart("Loading vocations");
            VocationNone none = new VocationNone();
            none.AddToList();

            Assembly vocationsAssembly;
            List<string> externalAssemblies = new List<string>();
            externalAssemblies.Add(Assembly.GetExecutingAssembly().Location);
            bool redFromCache;

            if (!CompileCsScripts("Data/Vocations", "Vocations.*.dll", externalAssemblies, out vocationsAssembly, out redFromCache, forceCompilation))
                return false;

            try
            {
                foreach (Type vocation in vocationsAssembly.GetTypes())
                {
                    if (vocation.BaseType == typeof(Vocation))
                    {
                        Vocation voc = (Vocation)Activator.CreateInstance(vocation);
                        voc.AddToList();
                    }
                }
            }
            catch (Exception e)
            {
                if (!forceCompilation)
                {
                    Vocation.Vocations.Clear();
                    return LoadVocations(true);
                }

                Logger.LogOperationFailed(e.ToString());
                return false;
            }

            if (redFromCache)
                Logger.LogOperationCached();
            else
                Logger.LogOperationDone();
            return true;
        }
        #endregion
    }
}

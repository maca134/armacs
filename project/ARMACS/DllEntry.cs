using RGiesecke.DllExport;
using System.Runtime.InteropServices;
using System.Text;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;
using System.IO;
using Microsoft.CSharp;
using System.CodeDom.Compiler;

namespace ARMACS
{
    public class DllEntry
    {

        [DllExport("_RVExtension@12", CallingConvention = System.Runtime.InteropServices.CallingConvention.Winapi)]
        public static void RVExtension(StringBuilder output, int outputSize, [MarshalAs(UnmanagedType.LPStr)] string function)
        {
            outputSize--;
            var args = function.Split('\n');
            var cmd = args[0].ToLower();
            var script = (args.Length > 1) ? args[1] : string.Empty;
            var scriptArgs = (args.Length > 2) ? args[2] : string.Empty;

            try
            {
                switch (cmd)
                {
                    case "load":
                        output.Append(Load(script, scriptArgs));
                        break;
                    case "run":
                        output.Append(Run(script, scriptArgs));
                        break;
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.TargetSite.ToString());
                output.Append("ERROR1\n" + ex.Message);
            }
        }

        static DllEntry()
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            ConsoleHelper.CreateConsole();
        }

        private static readonly Regex referenceRegex = new Regex(@"^[\ \t]*(?:\/{2})?\#r[\ \t]+""([^""]+)""", RegexOptions.Multiline);
        private static Dictionary<string, Dictionary<string, Assembly>> referencedAssemblies = new Dictionary<string, Dictionary<string, Assembly>>();
        private static Dictionary<int, Func<string, string>> funcCache = new Dictionary<int, Func<string, string>>();
        private static int funcCachePointer = 0;

        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            Assembly result = null;
            Dictionary<string, Assembly> requesting;
            if (referencedAssemblies.TryGetValue(args.RequestingAssembly.FullName, out requesting))
            {
                requesting.TryGetValue(args.Name, out result);
            }

            return result;
        }

        private static string _basePath
        {
            get { return Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location); }
        }

        private static string Load(string script, string scriptArgs)
        {
            var basepath = _basePath;
            var cspath = script;

            if (Path.IsPathRooted(script))
            {
                basepath = Path.GetDirectoryName(script);
            }
            else
            {
                cspath = Path.Combine(basepath, script);
            }

            if (!File.Exists(cspath))
            {
                return "ERROR2\nNo cs found";
            }

            var source = string.Empty;
            try
            {
                source = File.ReadAllText(cspath);
            }
            catch (Exception ex)
            {
                return "ERROR3\n" + ex.Message;
            }

            List<string> references = new List<string>();
            Match match = referenceRegex.Match(source);
            while (match.Success)
            {
                var dll = match.Groups[1].Value;
                if (dll.StartsWith(".\\"))
                {
                    dll = Path.Combine(basepath, dll.Replace(".\\", ""));
                }
                references.Add(dll);
                source = source.Substring(0, match.Index) + source.Substring(match.Index + match.Length);
                match = referenceRegex.Match(source);
            }

            Dictionary<string, string> options = new Dictionary<string, string> {
                { "CompilerVersion", "v4.0" }
            };
            CSharpCodeProvider csc = new CSharpCodeProvider(options);
            CompilerParameters parameters = new CompilerParameters();
            parameters.CompilerOptions = "/platform:x86";
            parameters.GenerateInMemory = true;
            parameters.ReferencedAssemblies.AddRange(references.ToArray());
            parameters.ReferencedAssemblies.Add("System.dll");
            parameters.ReferencedAssemblies.Add("System.Core.dll");
            parameters.ReferencedAssemblies.Add("Microsoft.CSharp.dll");

            CompilerResults results = csc.CompileAssemblyFromSource(parameters, source);
            if (results.Errors.HasErrors)
            {
                return "ERROR4\n" + results.Errors[0].ToString();
            }
            var assembly = results.CompiledAssembly;
            referencedAssemblies[assembly.FullName] = new Dictionary<string, Assembly>();

            foreach (var reference in references)
            {
                var dll = reference;
                try
                {
                    Console.WriteLine("Loading " + dll);
                    var referencedAssembly = Assembly.UnsafeLoadFrom(dll);
                    referencedAssemblies[assembly.FullName][referencedAssembly.FullName] = referencedAssembly;
                }
                catch (Exception ex)
                {
                    Console.WriteLine("Error Loading " + dll + " - " + ex.Message);
                }
            }

            Type startupType = assembly.GetType("Startup", true, true);
            object instance = Activator.CreateInstance(startupType, false);
            MethodInfo invokeMethod = startupType.GetMethod("RVExtension", BindingFlags.Static | BindingFlags.Public);
            if (invokeMethod == null)
            {
                return "ERROR5\nUnable to access CLR method to wrap through reflection. Make sure it is a public instance method.";
            }
            var pointer = funcCachePointer++;
            funcCache.Add(pointer, (string input) =>
            {
                var output = new StringBuilder();
                invokeMethod.Invoke(instance, new object[] { output, 10000, input });
                return output.ToString();
            });
            return pointer.ToString();
        }

        private static string Run(string script, string scriptArgs)
        {
            var pointer = Convert.ToInt32(script);
            if (!funcCache.ContainsKey(pointer))
            {
                return "ERROR6\nInvalid pointer.";
            }
            try
            {
                return funcCache[pointer](scriptArgs);
            }
            catch (Exception ex)
            {
                return "ERROR7\n" + ex.Message;
            }
        }
    }
}

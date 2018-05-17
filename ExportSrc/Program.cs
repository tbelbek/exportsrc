#region usings

using System;
using System.Diagnostics;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Windows.Forms;

using CodeFluent.Runtime.Diagnostics;

#endregion

namespace ExportSrc
{
    internal static class Program
    {
        private static Assembly CurrentDomain_AssemblyResolve(object sender, ResolveEventArgs args)
        {
            var baseResourceName = Assembly.GetExecutingAssembly().GetName().Name + ".External."
                                                                                  + new AssemblyName(args.Name).Name;
            byte[] assemblyData = null;
            byte[] symbolsData = null;
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(baseResourceName + ".dll"))
            {
                if (stream == null)
                    return null;
                assemblyData = new byte[stream.Length];
                stream.Read(assemblyData, 0, assemblyData.Length);
            }

            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(baseResourceName + ".pdb"))
            {
                if (stream != null)
                {
                    symbolsData = new byte[stream.Length];
                    stream.Read(symbolsData, 0, symbolsData.Length);
                }
            }

            return Assembly.Load(assemblyData, symbolsData);
        }

        /// <summary>
        ///     The main entry point for the application.
        /// </summary>
        [STAThread]
        private static void Main(string[] args)
        {
            AppDomain.CurrentDomain.AssemblyResolve += CurrentDomain_AssemblyResolve;
            if (Debugger.IsAttached) SafeMain(args);
            else
                try
                {
                    SafeMain(args);
                }
                catch (Exception e)
                {
                    Console.WriteLine(e.ToString());
                }
        }

        private static void PrintUsage()
        {
            Console.WriteLine("ExportSrc.exe <source> <destination> [config]");
            Console.WriteLine("\tsource              Source directory");
            Console.WriteLine("\tdestination         Target ");
            Console.WriteLine("\tconfig              Configuration file");
        }

        [MethodImpl(MethodImplOptions.NoInlining)]
        private static void SafeMain(string[] args)
        {
            if (args.Length == 0)
            {
                Application.EnableVisualStyles();
                Application.SetCompatibleTextRenderingDefault(false);
                Application.Run(new MainForm());
                return;
            }

            ConsoleListener.EnsureConsole();

            if (args.Any(s => s == "/?" || s == "-?"))
            {
                PrintUsage();
                return;
            }

            if (Trace.Listeners.Count == 0 || Trace.Listeners.Count == 1
                && Trace.Listeners[0].GetType() == typeof(DefaultTraceListener))
                Trace.Listeners.Add(new ConsoleTraceListener());

            Settings settings;
            if (args.Length > 2) settings = Settings.Deserialize(args[2]);
            else settings = Settings.GetDefault();

            // XmlSerializer serializer = new XmlSerializer(typeof(Settings));
            // using (Stream s = File.OpenWrite("settings.xml"))
            // serializer.Serialize(s, settings);
            var exporter = new Exporter(args[0], settings);
            var result = exporter.Export(args[1]);

            Logger.Current.Log(LogCategory.Summary, "Directories: " + result.Directories);
            Logger.Current.Log(LogCategory.Summary, "Files:       " + result.Files);
        }
    }
}
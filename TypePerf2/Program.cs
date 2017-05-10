using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.InteropServices;
using System.Threading;

namespace TypePerf2
{
    class Program
    {
        private static readonly IDictionary<string, Func<PerformanceCounter, string>> types = new Dictionary
            <string, Func<PerformanceCounter, string>>
            {
                {"raw", pc => pc.RawValue.ToString()},
                {"next", pc => pc.NextValue().ToString()},
                {
                    "next2", pc =>
                    {
                        pc.NextValue();
                        Thread.Sleep(1000);
                        return pc.NextValue().ToString();
                    }
                }
            };

        static void Main(string[] args)
        {
            //File.AppendAllLines("TypePerf2.log", new[] { Environment.CommandLine });

            if (args.Length == 1)
            {
                args = CommandLineToArgs(args[0]);
            }

            if (args.Length == 3 || args.Length == 4)
            {
                string type = args[0];
                string category = args[1];
                string counter = args[2];
                string instance = args.Length == 3 ? "" : args[3];

                if (!types.ContainsKey(type))
                {
                    Log("'{0}' is not a supported type", type);
                    return;
                }

                if (!PerformanceCounterCategory.Exists(category))
                {
                    Log("Performance counter category {0} does not exist", category);
                    return;
                }
                if (!PerformanceCounterCategory.CounterExists(counter, category))
                {
                    Log("Performance counter {0}:{1} does not exist", category, counter);
                    return;
                }
                if (instance.Length > 0 && !PerformanceCounterCategory.InstanceExists(instance, category))
                {
                    Log("There is no instance {0} for performance counter {1}:{2}", instance, category,
                        counter);
                    return;
                }

                var performanceCounter = instance.Length == 0 
                    ? new PerformanceCounter(category, counter) 
                    : new PerformanceCounter(category, counter, instance);

                string value = types[type](performanceCounter);
                Console.Write(value);
                //File.AppendAllLines("TypePerf2.log", new []{ Environment.CommandLine + " => " + value});
            }
            else
            {
                Log(@"Usage:
TypePerf2 <value type> <category> <counter> [instance]
    where type is any of:
        raw => the raw value of the counter
        next2 => the second calculated value
        next => the calculated value");
            }
        }

        private static void Log(string message, params object[] args)
        {
            if (args != null && args.Length > 0)
            {
                message = string.Format(message, args);
            }

            Console.WriteLine(message);
            //File.AppendAllLines("TypePerf2.log", new[] { message });
        }

        [DllImport("shell32.dll", SetLastError = true)]
        static extern IntPtr CommandLineToArgvW(
    [MarshalAs(UnmanagedType.LPWStr)] string lpCmdLine, out int pNumArgs);

        public static string[] CommandLineToArgs(string commandLine)
        {
            int argc;
            var argv = CommandLineToArgvW(commandLine, out argc);
            if (argv == IntPtr.Zero)
                throw new System.ComponentModel.Win32Exception();
            try
            {
                var args = new string[argc];
                for (var i = 0; i < args.Length; i++)
                {
                    var p = Marshal.ReadIntPtr(argv, i * IntPtr.Size);
                    args[i] = Marshal.PtrToStringUni(p);
                }

                return args;
            }
            finally
            {
                Marshal.FreeHGlobal(argv);
            }
        }
    }
}

﻿#define CLASSIC

using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Management;
using System.Reflection;

namespace Adaptive.Aeron.Samples.Common
{
    internal class RuntimeInformation
    {
        internal static bool IsWindows()
        {
            return new[] {PlatformID.Win32NT, PlatformID.Win32S, PlatformID.Win32Windows, PlatformID.WinCE}
                .Contains(Environment.OSVersion.Platform);
        }

        internal static bool IsMono() => Type.GetType("Mono.Runtime") != null;

        internal static string GetOsVersion()
        {
            return Environment.OSVersion.ToString();
        }

        internal static string GetProcessorName()
        {
            if (IsWindows() && !IsMono())
            {
                var info = string.Empty;
                try
                {
                    var mosProcessor = new ManagementObjectSearcher("SELECT * FROM Win32_Processor");
                    foreach (var moProcessor in mosProcessor.Get().Cast<ManagementObject>())
                        info += moProcessor["name"]?.ToString();
                }
                catch (Exception)
                {
                }

                return info;
            }
            return "?"; // TODO: verify if it is possible to get this for CORE
        }

        internal static string GetClrVersion()
        {
            if (IsMono())
            {
                var monoRuntimeType = Type.GetType("Mono.Runtime");
                var monoDisplayName = monoRuntimeType?.GetMethod("GetDisplayName", BindingFlags.NonPublic | BindingFlags.Static);
                if (monoDisplayName != null)
                    return "Mono " + monoDisplayName.Invoke(null, null);
            }

            return "MS.NET " + Environment.Version;
        }

        internal static string GetCurrent()
        {
            return IsMono() ? "Mono" : "Clr";
        }

        internal static string GetJitModules()
        {
            return string.Join(";",
                Process.GetCurrentProcess().Modules
                    .OfType<ProcessModule>()
                    .Where(module => module.ModuleName.Contains("jit"))
                    .Select(module => Path.GetFileNameWithoutExtension(module.FileName) + "-v" + module.FileVersionInfo.ProductVersion));
        }

        internal static bool HasRyuJit()
        {
            return !IsMono()
                   && IntPtr.Size == 8
                   && GetConfiguration() != "DEBUG"
                   && !new JitHelper().IsMsX64();
        }

        internal static string GetConfiguration()
        {
#if DEBUG
            return "DEBUG";
#endif
            return "RELEASE";
        }

        // See http://aakinshin.net/en/blog/dotnet/jit-version-determining-in-runtime/
        private class JitHelper
        {
            private int bar;

            public bool IsMsX64(int step = 1)
            {
                var value = 0;
                for (var i = 0; i < step; i++)
                {
                    bar = i + 10;
                    for (var j = 0; j < 2*step; j += step)
                        value = j + 10;
                }
                return value == 20 + step;
            }
        }
    }
}
using System.Reflection;
using System.Reflection.Emit;
using BepInEx;
using BepInEx.Logging;
using BepInEx.NET.Common;
using BepInExResoniteShim;
using FrooxEngine;
using HarmonyLib;

namespace LinuxSortedFiles;

[ResonitePlugin(PluginMetadata.GUID, PluginMetadata.NAME, PluginMetadata.VERSION, PluginMetadata.AUTHORS, PluginMetadata.REPOSITORY_URL)]
[BepInDependency(BepInExResoniteShim.PluginMetadata.GUID, BepInDependency.DependencyFlags.HardDependency)]
public class Plugin : BasePlugin
{
    internal static new ManualLogSource Log = null!;

    private static bool _isLinux; 

    public override void Load()
    {
        Log = base.Log;
        
        _isLinux = OperatingSystem.IsLinux();

        HarmonyInstance.PatchAll();

        Log.LogInfo($"Plugin {PluginMetadata.GUID} is loaded!");
    }

    [HarmonyPatch(typeof(FileBrowser), "Refresh", MethodType.Async)]
    public static class Thingy
    {
        private static readonly MethodInfo GetFilesOriginal = AccessTools.Method(typeof(Directory), "GetFiles", new Type[] { typeof(string) });
        private static readonly MethodInfo GetDirsOriginal = AccessTools.Method(typeof(Directory), "GetDirectories", new Type[] { typeof(string) });

        private static readonly MethodInfo GetFilesReplacement = AccessTools.Method(typeof(Thingy), nameof(GetFiles), new Type[] { typeof(string) });
        private static readonly MethodInfo GetDirsReplacement = AccessTools.Method(typeof(Thingy), nameof(GetDirectories), new Type[] { typeof(string) });

        private static IEnumerable<CodeInstruction> Transpiler(IEnumerable<CodeInstruction> instructions)
        {
            foreach (CodeInstruction instruction in instructions)
            {
                if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo method)
                {
                    if (method == GetFilesOriginal)
                    {
                        yield return new CodeInstruction(OpCodes.Call, GetFilesReplacement);
                        continue;
                    }

                    if (method == GetDirsOriginal)
                    {
                        yield return new CodeInstruction(OpCodes.Call, GetDirsReplacement);
                        continue;
                    }
                }

                yield return instruction;
            }
        }

        public static string[] GetFiles(string path)
        {
            string[] files = Directory.GetFiles(path);

            if (_isLinux)
            {
                Array.Sort(files, StringComparer.OrdinalIgnoreCase);
            }

            return files;
        }

        public static string[] GetDirectories(string path)
        {
            string[] dirs = Directory.GetDirectories(path);

            if (_isLinux)
            {
                Array.Sort(dirs, StringComparer.OrdinalIgnoreCase);
            }

            return dirs;
        }
    }
}
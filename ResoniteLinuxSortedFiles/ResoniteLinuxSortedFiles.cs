
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

using FrooxEngine;
using HarmonyLib;
using ResoniteModLoader;

namespace ResoniteLinuxSortedFiles;

public class Mod : ResoniteMod {
	internal const string VERSION_CONSTANT = "1.0.0";
	public override string Name => "ResoniteLinuxSortedFiles";
	public override string Author => "Baplar";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/Baplar/ResoniteLinuxSortedFiles";

	public override void OnEngineInit() {
		if (Engine.Current.Platform != Platform.Linux) {
			Mod.Warn("This mod only works on Linux. Skipping.");
			return;
		}
		Harmony harmony = new Harmony("fr.Baplar.ResoniteLinuxSortedFiles");
		harmony.PatchAll();
	}
}

[HarmonyPatch(typeof(FileBrowser))]
public static class FileBrowserPatches {
	[HarmonyPrefix]
	[HarmonyPatch(typeof(BrowserDialog), "BeginGenerateToolPanel")]
	public static void BeginGenerateToolPanel_Prefix(BrowserDialog __instance) {
		if (__instance is FileBrowser fileBrowser) {
			Mod.Msg("Initializing file browser in home folder rather than root");
			fileBrowser.CurrentPath.Value = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile);
		}
	}

	private static readonly MethodInfo GetFilesOriginal = AccessTools.Method(typeof(Directory), "GetFiles", [typeof(string)]);
	private static readonly MethodInfo GetDirectoriesOriginal = AccessTools.Method(typeof(Directory), "GetDirectories", [typeof(string)]);
	private static readonly MethodInfo GetLogicalDrivesOriginal = AccessTools.Method(typeof(Directory), "GetLogicalDrives");

	private static readonly MethodInfo GetFilesReplacement = AccessTools.Method(typeof(FileBrowserPatches), nameof(GetFiles), [typeof(string)]);
	private static readonly MethodInfo GetDirectoriesReplacement = AccessTools.Method(typeof(FileBrowserPatches), nameof(GetDirectories), [typeof(string)]);
	private static readonly MethodInfo GetLogicalDrivesReplacement = AccessTools.Method(typeof(FileBrowserPatches), nameof(GetLogicalDrives));


	[HarmonyTranspiler]
	[HarmonyPatch("Refresh", MethodType.Async)]
	public static IEnumerable<CodeInstruction> Refresh_Transpiler(IEnumerable<CodeInstruction> instructions) {
		foreach (CodeInstruction instruction in instructions){
			if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo method)
			{
				if (method == GetFilesOriginal) {
					yield return new CodeInstruction(OpCodes.Call, GetFilesReplacement);
					continue;
				}
				if (method == GetDirectoriesOriginal) {
					yield return new CodeInstruction(OpCodes.Call, GetDirectoriesReplacement);
					continue;
				}
				if (method == GetLogicalDrivesOriginal) {
					yield return new CodeInstruction(OpCodes.Call, GetLogicalDrivesReplacement);
					continue;
				}
			}
			yield return instruction;
		}
	}

	public static string[] GetFiles(string path) {
		Mod.Debug("Running GetFiles");
		string[] files = Directory.GetFiles(path);
		Array.Sort(files, StringComparer.OrdinalIgnoreCase);
		return files;
	}

	public static string[] GetDirectories(string path) {
		Mod.Debug("Running GetDirectories");
		string[] dirs = Directory.GetDirectories(path);
		Array.Sort(dirs, StringComparer.OrdinalIgnoreCase);
		return dirs;
	}

	public static string[] GetLogicalDrives() {
		Mod.Debug("Running GetLogicalDrives");
		return ["/"];
	}
}

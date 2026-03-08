
using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;
using System.Reflection.Emit;

using Elements.Assets;

using FrooxEngine;

using HarmonyLib;

using ResoniteModLoader;

namespace LinuxSortedFiles;

public class Mod : ResoniteMod {
	internal const string VERSION_CONSTANT = "1.1.0";
	public override string Name => "LinuxSortedFiles";
	public override string Author => "Baplar";
	public override string Version => VERSION_CONSTANT;
	public override string Link => "https://github.com/Baplar/ResoniteLinuxSortedFiles";

	public override void OnEngineInit() {
		Harmony harmony = new Harmony("fr.baplar.LinuxSortedFiles");
		harmony.PatchAllUncategorized();
		if (Engine.Current.Platform == Platform.Linux) {
			harmony.PatchCategory("Linux");
		}
	}
}

[HarmonyPatch(typeof(FileBrowser))]
[HarmonyPatchCategory("Linux")]
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
		foreach (CodeInstruction instruction in instructions) {
			if (instruction.opcode == OpCodes.Call && instruction.operand is MethodInfo method) {
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

[HarmonyPatch(typeof(RecordDirectory))]
public static class RecordDirectoryPatches {
	[HarmonyPostfix]
	[HarmonyPatch(typeof(RecordDirectory), "ProcessContents", [typeof(List<FrooxEngine.Store.Record>), typeof(bool)])]
	public static void ProcessContents_Postfix(RecordDirectory __instance) {
		SortDirsStripped(__instance);
	}

	[HarmonyPostfix]
	[HarmonyPatch(typeof(RecordDirectory), MethodType.Constructor, [typeof(Engine), typeof(List<RecordDirectory>), typeof(List<FrooxEngine.Store.Record>)])]
	public static void Construtor_Postfix(RecordDirectory __instance) {
		SortDirsStripped(__instance);
	}

	private static void SortDirsStripped(RecordDirectory directory) {
		AccessTools
			.FieldRefAccess<RecordDirectory, List<RecordDirectory>>(directory, "subdirectories")
			.Sort((a, b) => StringComparer.CurrentCultureIgnoreCase.Compare(
				new StringRenderTree(a.Name).GetRawString(),
				new StringRenderTree(b.Name).GetRawString()
			));
	}
}

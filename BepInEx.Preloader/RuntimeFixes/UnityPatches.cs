using System;
using System.Collections.Generic;
using System.Reflection;
using HarmonyLib;

namespace BepInEx.Preloader.RuntimeFixes
{
	internal static class UnityPatches
	{
		private static HarmonyLib.Harmony HarmonyInstance { get; set; }

		public static Dictionary<string, string> AssemblyLocations { get; } = new Dictionary<string, string>(StringComparer.InvariantCultureIgnoreCase);

		public static void Apply()
		{
			HarmonyInstance = HarmonyLib.Harmony.CreateAndPatchAll(typeof(GetLocation));
			HarmonyInstance.PatchAll(typeof(GetCodeBase));

			try
			{
				TraceFix.ApplyFix();
			}
			catch { } //ignore everything, if it's thrown an exception, we're using an assembly that has already fixed this
		}

		[HarmonyPatch]
		internal static class GetLocation
		{
			public static MethodInfo TargetMethod()
			{
				try
				{
					var method = AccessTools.DeclaredPropertyGetter(typeof(UnityPatches).Assembly.GetType(), nameof(Assembly.Location));

					if (method == null)
					{
						return AccessTools.DeclaredPropertyGetter(typeof(Assembly), nameof(Assembly.Location));
					}

					return method;
				}
				catch (Exception e)
				{
					throw;
				}
			}

			public static void Postfix(ref string __result, Assembly __instance)
			{
				if (AssemblyLocations.TryGetValue(__instance.FullName, out string location))
					__result = location;
			}
		}

		[HarmonyPatch]
		internal static class GetCodeBase
		{
			public static MethodInfo TargetMethod()
			{
				try
				{
					var method = AccessTools.DeclaredPropertyGetter(typeof(UnityPatches).Assembly.GetType(), nameof(Assembly.CodeBase));

					if (method == null)
					{
						return AccessTools.DeclaredPropertyGetter(typeof(Assembly), nameof(Assembly.CodeBase));
					}

					return method;
				}
				catch (Exception e)
				{
					throw;
				}
				
			}

			public static void Postfix(ref string __result, Assembly __instance)
			{
				if (AssemblyLocations.TryGetValue(__instance.FullName, out string location))
					__result = $"file://{location.Replace('\\', '/')}";
			}
		}
	}
}
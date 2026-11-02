/* Copyright (c) 2020 ExT (V.Sigalkin) */

using UnityEditor;
using UnityEditor.Build;

using System;

namespace extOSC.Editor
{
	[InitializeOnLoad]
	public static class OSCDefinesManager
	{
		#region Static Private Vars

		private const string DefaultDefine = "EXTOSC";

		#endregion

		#region Constructor Methods

		static OSCDefinesManager()
		{
			if (!HasDefine(DefaultDefine))
				SetDefine(DefaultDefine, true);
		}

		#endregion

		#region Static Public Methods

		public static void SetDefine(string define, bool active)
		{
			var buildTargets = (BuildTargetGroup[]) Enum.GetValues(typeof(BuildTargetGroup));
			foreach (var targetGroup in buildTargets)
			{
				if (!CheckBuildTarget(targetGroup)) continue;

				NamedBuildTarget namedTarget;
				try { namedTarget = NamedBuildTarget.FromBuildTargetGroup(targetGroup); }
				catch (ArgumentException) { continue; }

				var scriptingDefines = PlayerSettings.GetScriptingDefineSymbols(namedTarget);
				if (!scriptingDefines.Contains(define) && active)
				{
					scriptingDefines += ";" + define;
				}
				else if (!active)
				{
					scriptingDefines = scriptingDefines.Replace(define, string.Empty);
				}

				PlayerSettings.SetScriptingDefineSymbols(namedTarget, scriptingDefines);
			}
		}

		public static bool HasDefine(string define)
		{
			// Get current define group.
			var currentBuildTarget = EditorUserBuildSettings.selectedBuildTargetGroup;

			// Check.
			return PlayerSettings.GetScriptingDefineSymbols(NamedBuildTarget.FromBuildTargetGroup(currentBuildTarget)).Contains(define);
		}

		#endregion

		#region Static Private Methods

		private static bool CheckBuildTarget(BuildTargetGroup buildTarget)
		{
			// Not available id Unknown.
			if (buildTarget == BuildTargetGroup.Unknown)
				return false;

			// Or Obsolete.
			var buildTargetString = buildTarget.ToString();
			var field = typeof(BuildTargetGroup).GetField(buildTargetString);
			return !Attribute.IsDefined(field, typeof(ObsoleteAttribute), true);
		}

		#endregion
	}
}
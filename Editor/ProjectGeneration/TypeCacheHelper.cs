﻿using SR = System.Reflection;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;

namespace Neovim.Editor
{
	internal class TypeCacheHelper
	{
		internal static IEnumerable<SR.MethodInfo> GetPostProcessorCallbacks(string name)
		{
			return TypeCache
				.GetTypesDerivedFrom<AssetPostprocessor>()
				.Where(t => t.Assembly.GetName().Name != KnownAssemblies.Bridge) // never call into the bridge if loaded with the package
				.Select(t => t.GetMethod(name, SR.BindingFlags.Public | SR.BindingFlags.NonPublic | SR.BindingFlags.Static))
				.Where(m => m != null);
		}
	}

}

﻿using System;

namespace Neovim.Editor
{
	internal class ProjectProperties
	{
		public string ProjectGuid { get; set; } = string.Empty;
		public string LangVersion { get; set; } = "latest";
		public string AssemblyName { get; set; } = string.Empty;
		public string RootNamespace { get; set; } = string.Empty;
		public string OutputPath { get; set; } = string.Empty;

		// Analyzers
		public string[] Analyzers { get; set; } = Array.Empty<string>();
		public string RulesetPath { get; set; } = string.Empty;
		public string AnalyzerConfigPath { get; set; } = string.Empty;
		// Source generators
		public string[] AdditionalFilePaths { get; set; } = Array.Empty<string>();

		// RSP alterable
		public string[] Defines { get; set; } = Array.Empty<string>();
		public bool Unsafe { get; set; } = false;

		// VSTU Flavouring
		public string FlavoringProjectType { get; set; } = string.Empty;
		public string FlavoringBuildTarget { get; set; } = string.Empty;
		public string FlavoringUnityVersion { get; set; } = string.Empty;
		public string FlavoringPackageVersion { get; set; } = string.Empty;
	}
}

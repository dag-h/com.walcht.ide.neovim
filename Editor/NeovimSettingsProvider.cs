using System;
using UnityEngine;
using UnityEditor;
using Unity.CodeEditor;

namespace Neovim.Editor
{
    internal static class NeovimSettingsProvider
    {
        private const string k_TerminalPathKey = "Neovim.TerminalPath";
        private const string k_TerminalArgsKey = "Neovim.TerminalArgs";
        private const string k_SocketPathKey   = "Neovim.ServerSocketPath";

        private static IGenerator s_Generator => GeneratorFactory.GetInstance(GeneratorStyle.SDK);

        [SettingsProvider]
        public static SettingsProvider CreateNeovimSettingsProvider()
        {
            var provider = new SettingsProvider("Preferences/Neovim", SettingsScope.User)
            {
                label = "Neovim",
                guiHandler = (searchContext) =>
                {
                    EditorGUILayout.LabelField("Neovim Settings", EditorStyles.boldLabel);
                    EditorGUI.indentLevel++;

                    string termPath = EditorPrefs.GetString(k_TerminalPathKey, "wt");
                    string termArgs = EditorPrefs.GetString(k_TerminalArgsKey, "");
                    string socketPath = EditorPrefs.GetString(k_SocketPathKey, @"\\.\pipe\nvim-unity");

                    string newTermPath = EditorGUILayout.TextField("Terminal Path", termPath);
                    string newTermArgs = EditorGUILayout.TextField("Terminal Arguments", termArgs);
                    string newSocket = EditorGUILayout.TextField("Server Socket Path", socketPath);

                    if (newTermPath != termPath)
                        EditorPrefs.SetString(k_TerminalPathKey, newTermPath);
                    if (newTermArgs != termArgs)
                        EditorPrefs.SetString(k_TerminalArgsKey, newTermArgs);
                    if (newSocket != socketPath)
                        EditorPrefs.SetString(k_SocketPathKey, newSocket);

                    EditorGUI.indentLevel--;
                }
            };

            return provider;
        }

        // Optional convenience accessors
        public static string TerminalPath => EditorPrefs.GetString(k_TerminalPathKey, "wt");
        public static string TerminalArgs => EditorPrefs.GetString(k_TerminalArgsKey, "");
        public static string ServerSocketPath => EditorPrefs.GetString(k_SocketPathKey, @"\\.\pipe\nvim-unity");
    }
}

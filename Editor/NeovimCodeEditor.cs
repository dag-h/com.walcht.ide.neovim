using System;
using System.IO;
using System.Linq;
using System.Diagnostics;
using UnityEditor;
using UnityEngine;
using Unity.CodeEditor;
using Debug = UnityEngine.Debug;

namespace Neovim.Editor
{
    [InitializeOnLoad]
    public class NeovimCodeEditor : IExternalCodeEditor
    {
        // list of valid neovim executable names
        static readonly string[] _supportedFileNames = { "nvim", "nvim.exe" };

        private IGenerator m_Generator;
        private NeovimSettings m_Settings = new();

        // because of the "InitializeOnLoad" attribute, this will be called when scripts in the project are recompiled
        static NeovimCodeEditor()
        {
            var editor = new NeovimCodeEditor(GeneratorFactory.GetInstance(GeneratorStyle.SDK));
            CodeEditor.Register(editor);
            editor.CreateIfDoesntExist();
        }

        private void CreateIfDoesntExist()
        {
            m_Generator.Sync();
        }

        private CodeEditor.Installation[] m_Installations;

        public CodeEditor.Installation[] Installations
        {
            get
            {
                m_Installations ??= DiscoverInstallations();
                return m_Installations;
            }
        }

        public NeovimCodeEditor(IGenerator projectGeneration)
        {
            m_Generator = projectGeneration;
        }

        // Callback to the IExternalCodeEditor when it has been chosen from the PreferenceWindow.
        public void Initialize(string editorInstallationPath)
        {
        }

        // Unity stores the path of the chosen editor. An instance of
        // IExternalCodeEditor can take responsibility for this path, by returning
        // true when this method is being called. The out variable installation need
        // to be constructed with the path and the name that should be shown in the
        // "External Tools" code editor list.
        public bool TryGetInstallationForPath(string editorPath, out CodeEditor.Installation installation)
        {
            var lowerCasePath = editorPath.ToLower();
            var filename = Path.GetFileName(lowerCasePath).Replace(" ", "");
            var installations = Installations;

            if (!_supportedFileNames.Contains(filename))
            {
                installation = default;
                return false;
            }

            if (!installations.Any())
            {
                installation = new CodeEditor.Installation
                {
                    Name = "Neovim",
                    Path = editorPath
                };
            }
            else
            {
                try
                {
                    installation = installations.First(inst => inst.Path == editorPath);
                }
                catch (InvalidOperationException)
                {
                    installation = new CodeEditor.Installation
                    {
                        Name = "Neovim",
                        Path = editorPath
                    };
                }
            }

            return true;
        }

        // Unity calls this method when it populates "Preferences/External Tools"
        // in order to allow the code editor to generate necessary GUI. For example,
        // when creating an argument field for modifying the arguments sent to
        // the code editor.
        public void OnGUI()
        {
            EditorGUILayout.LabelField("Generate .csproj files for:");
            EditorGUI.indentLevel++;
            {
                SettingsButton(ProjectGenerationFlag.Embedded, "Embedded packages", "");
                SettingsButton(ProjectGenerationFlag.Local, "Local packages", "");
                SettingsButton(ProjectGenerationFlag.Registry, "Registry packages", "");
                SettingsButton(ProjectGenerationFlag.Git, "Git packages", "");
                SettingsButton(ProjectGenerationFlag.BuiltIn, "Built-in packages", "");
                SettingsButton(ProjectGenerationFlag.LocalTarBall, "Local tarball", "");
                SettingsButton(ProjectGenerationFlag.Unknown, "Packages from unknown sources", "");
                SettingsButton(ProjectGenerationFlag.PlayerAssemblies, "Player projects",
                    "For each player project generate an additional csproj with the name 'project-player.csproj'");
                RegenerateProjectFiles();
            }
            EditorGUI.indentLevel--;

            EditorGUILayout.LabelField("Neovim Settings");
            EditorGUI.indentLevel++;
            m_Settings.TerminalPath = EditorGUILayout.TextField("Terminal Path", m_Settings.TerminalPath);
            m_Settings.TerminalArgs = EditorGUILayout.TextField("Terminal Arguments", m_Settings.TerminalArgs);
            m_Settings.ServerSocketPath = EditorGUILayout.TextField("Server Socket Path", m_Settings.ServerSocketPath);

            EditorGUI.indentLevel--;
        }


        private void RegenerateProjectFiles()
        {
            var rect = EditorGUI.IndentedRect(EditorGUILayout.GetControlRect());
            rect.width = 252;
            if (GUI.Button(rect, "Regenerate project files"))
            {
                m_Generator.Sync();
            }
        }


        private void SettingsButton(ProjectGenerationFlag preference, string guiMessage, string toolTip)
        {
            var prevValue = m_Generator.AssemblyNameProvider.ProjectGenerationFlag.HasFlag(preference);
            var newValue = EditorGUILayout.Toggle(new GUIContent(guiMessage, toolTip), prevValue);
            if (newValue != prevValue)
            {
                m_Generator.AssemblyNameProvider.ToggleProjectGeneration(preference);
            }
        }

        // When you change Assets in Unity, this method for the current chosen
        // instance of IExternalCodeEditor parses the new and changed Assets.
        public void SyncIfNeeded(string[] addedFiles, string[] deletedFiles,
            string[] movedFiles, string[] movedFromFiles, string[] importedFiles)
        {
            m_Generator.SyncIfNeeded(addedFiles.Union(deletedFiles).Union(movedFiles).Union(movedFromFiles).ToList(),
                importedFiles);
        }


        // Unity calls this function during initialization in order to sync the
        // Project. This is different from SyncIfNeeded in that it does not get a
        // list of changes.
        public void SyncAll()
        {
            m_Generator.Sync();
        }

        // The external code editor needs to handle the request to open a file.
        public bool OpenProject(string filePath = "", int line = -1, int column = -1)
        {
            if (string.IsNullOrEmpty(filePath) || !File.Exists(filePath))
                return false;
            if (line == -1)
                line = 1;
            if (column == -1)
                column = 0;

            var nvimPath = EditorPrefs.GetString("kScriptsDefaultApp");
            var socketPath = m_Settings.ServerSocketPath;
            var terminalPath = m_Settings.TerminalPath;

            var hasInstance = File.Exists(socketPath) || CreateInstance(nvimPath, socketPath, terminalPath, filePath);
            return hasInstance && OpenInActiveInstance(nvimPath, socketPath, filePath, line, column);
        }

        private static bool CreateInstance(string nvimPath, string socketPath, string terminalPath, string filePath)
        {
            try
            {
                var args = $" -- {nvimPath} {filePath} --listen {socketPath}";

                using var serverProc = new Process();

                serverProc.StartInfo.FileName = terminalPath;
                serverProc.StartInfo.Arguments = args;
                serverProc.StartInfo.WindowStyle = ProcessWindowStyle.Normal;
                serverProc.StartInfo.CreateNoWindow = false;
                serverProc.StartInfo.UseShellExecute = false;
                serverProc.Start();
                if (!serverProc.WaitForExit(500))
                {
#if DEBUG
                    Debug.LogError("[neovim.ide] failed at creating a Neovim server instance");
#endif
                    return false;
                }
            }
            catch (Exception e)
            {
#if DEBUG
                Debug.LogError($"[neovim.ide] failed at creating a Neovim server instance: {e.Message}");
#endif
                return false;
            }
            return true;
        }

        private static bool OpenInActiveInstance(
            string nvimPath,
            string socketPath,
            string filePath,
            int line,
            int column)
        {
            var arguments =
                $"--server {socketPath} --remote-tab {filePath} --remote-send \":call cursor({line},{column})<CR>\"";

            try
            {
                using var proc = new Process();
                proc.StartInfo.FileName = nvimPath;
                proc.StartInfo.Arguments = arguments;
                proc.StartInfo.WindowStyle = ProcessWindowStyle.Hidden;
                proc.StartInfo.CreateNoWindow = true;
                proc.StartInfo.UseShellExecute = true;
                proc.Start();
                if (!proc.WaitForExit(500))
                {
#if DEBUG
                    Debug.LogError(
                        $"[neovim.ide] failed at sending request to open {filePath} to Neovim server instance " +
                        $"listening to {socketPath}");
#endif
                    return false;
                }
            }
            catch (Exception e)
            {
#if DEBUG
                Debug.LogError($"[neovim.ide] failed at sending request to open {filePath} to Neovim server instance " +
                               $"listening to {socketPath}. Reason: {e.Message}");
#endif
                return false;
            }

            return true;
        }

        private static bool TryGetFullPathOfNvimExecutable(out string path)
        {
            path = null;
            var command = Environment.OSVersion.Platform == PlatformID.Win32NT
                ? "where"
                : "which";
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = command,
                        Arguments = "nvim",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                var result = process.StandardOutput.ReadLine();
                process.WaitForExit(500);
                path = File.Exists(result) ? result : null;
                return path != null;
            }
            catch
            {
                return false;
            }
        }

        private static CodeEditor.Installation[] DiscoverInstallations()
        {
            return TryGetFullPathOfNvimExecutable(out var nvimPath) && IsNvimExecutable(nvimPath)
                ? new CodeEditor.Installation[]
                {
                    new()
                    {
                        Name = "Neovim",
                        Path = nvimPath,
                    }
                }
                : Array.Empty<CodeEditor.Installation>();
        }

        private static bool IsNvimExecutable(string executableName)
        {
            try
            {
                var process = new Process
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = executableName,
                        Arguments = "--version",
                        RedirectStandardOutput = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };
                process.Start();
                process.WaitForExit(500);
                return process.ExitCode == 0;
            }
            catch
            {
                return false;
            }
        }

    }
}
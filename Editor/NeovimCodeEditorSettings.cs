using System;
using UnityEngine;

namespace Neovim.Editor
{
    public class NeovimSettings
    {
        public string TerminalPath =
            Environment.OSVersion.Platform == PlatformID.Win32NT ? "cmd.exe" : "gnome-terminal";
        public string TerminalArgs = "";
        public string ServerSocketPath =
            Environment.OSVersion.Platform == PlatformID.Win32NT ? @"\\.\pipe\nvimsocket" : "/tmp/nvimsocket";
    }
}
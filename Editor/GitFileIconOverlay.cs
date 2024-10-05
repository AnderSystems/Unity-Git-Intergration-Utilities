using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class GitFileIconOverlay
{
    private static Dictionary<string, Texture2D> iconCache = new Dictionary<string, Texture2D>();

    // HashSets to store the paths of modified and added files
    private static HashSet<string> GitModifiedFiles = new HashSet<string>();
    private static HashSet<string> GitAddedFiles = new HashSet<string>();

    static GitFileIconOverlay()
    {
        UpdateGitIcons();
        // Update icons when the project is loaded or recompiled
        EditorApplication.projectWindowItemOnGUI += OnProjectWindowItemGUI;
        EditorApplication.delayCall += UpdateGitIcons;

        // Optional: Update periodically or after specific actions
    }

    // Function called to draw icons in the Editor
    private static void OnProjectWindowItemGUI(string guid, Rect selectionRect)
    {
        // Full path of the file from the GUID
        string assetPath = AssetDatabase.GUIDToAssetPath(guid);
        string fullPath = Path.GetFullPath(Path.Combine(Application.dataPath.Replace("/Assets", ""), assetPath)).Replace('\\', '/');

        // Check if the file is modified or added in Git
        if (GitModifiedFiles.Contains(fullPath))
        {
            // Load the modified file icon and draw it
            Texture2D modifiedIcon = GetIcon("ModifiedIcon.png");
            if (modifiedIcon != null)
            {
                Rect iconRect = new Rect(selectionRect.xMax - 16, selectionRect.yMin, 16, 16);
                GUI.DrawTexture(iconRect, modifiedIcon);
            }
        }
        else
        {
            if (GitAddedFiles.Contains(fullPath))
            {
                // Load the added file icon and draw it
                Texture2D addedIcon = GetIcon("AddedIcon.png");
                if (addedIcon != null)
                {
                    Rect iconRect = new Rect(selectionRect.xMax - 16, selectionRect.yMin, 16, 16);
                    GUI.DrawTexture(iconRect, addedIcon);
                }
            }
            else
            {
                Texture2D syncedIcon = GetIcon("SyncedIcon.png");
                if (syncedIcon != null)
                {
                    Rect iconRect = new Rect(selectionRect.xMax - 16, selectionRect.yMin, 16, 16);
                    GUI.DrawTexture(iconRect, syncedIcon);
                }
            }
        }
    }

    // Function to get the icon from the path
    private static Texture2D GetIcon(string iconFileName)
    {
        if (!iconCache.ContainsKey(iconFileName))
        {
            // Modify the path to use the package folder
            //string packagePath = Path.Combine("Library/PackageCache/com.andersystems.gitutilities/Editor", iconFileName);
            string iconPath = Application.dataPath.Replace("/Assets", "/Library/PackageCache/com.andersystems.gitutilities/Editor/Icons/" + iconFileName);
            UnityEngine.Debug.Log("iconPath: " + iconPath);

            if (File.Exists(iconPath))
            {
                byte[] fileData = File.ReadAllBytes(iconPath);
                Texture2D texture = new Texture2D(2, 2);
                texture.LoadImage(fileData);
                iconCache[iconFileName] = texture;
            }
            else
            {
                UnityEngine.Debug.LogWarning("Icon not found: " + iconPath);
            }
        }

        return iconCache.ContainsKey(iconFileName) ? iconCache[iconFileName] : null;
    }

    // Function that updates the list of modified and added files
    private static void UpdateGitIcons()
    {
        // Clear current lists
        GitModifiedFiles.Clear();
        GitAddedFiles.Clear();

        // Project path where the .git folder is located
        string projectPath = Application.dataPath.Replace("/Assets", "");

        // Git commands to get modified and added files
        string gitStatus = RunGitCommand("git status --short", projectPath);

        // Process modified and added files
        string[] lines = gitStatus.Split(new[] { "\r\n", "\r", "\n" }, System.StringSplitOptions.None);
        foreach (string line in lines)
        {
            if (!string.IsNullOrEmpty(line))
            {
                string status = line.Substring(0, 2);
                string filePath = line.Substring(3).Trim();
                string fullPath = Path.Combine(projectPath, filePath).Replace('\\', '/');

                if (status.StartsWith("M") || status.EndsWith("M")) // Modified file
                {
                    GitModifiedFiles.Add(fullPath);
                }
                else if (status.StartsWith("A") || status.EndsWith("A")) // Added file
                {
                    GitAddedFiles.Add(fullPath);
                }
            }
        }

        // Repaint the Project Window to update icons
        EditorApplication.RepaintProjectWindow();
    }

    // Function to run Git commands
    private static string RunGitCommand(string command, string workingDirectory)
    {
        ProcessStartInfo processInfo = new ProcessStartInfo("cmd.exe", "/c " + command)
        {
            WorkingDirectory = workingDirectory,
            CreateNoWindow = true,
            UseShellExecute = false,
            RedirectStandardOutput = true,
            RedirectStandardError = true
        };

        using (Process process = Process.Start(processInfo))
        {
            string output = process.StandardOutput.ReadToEnd();
            process.WaitForExit();
            if (!string.IsNullOrEmpty(output))
            {
                UnityEngine.Debug.Log(output);
            }

            string error = process.StandardError.ReadToEnd();
            if (!string.IsNullOrEmpty(error))
            {
                UnityEngine.Debug.LogError(error);
            }

            return output;
        }
    }
}

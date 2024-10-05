using System;
using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class GitAutoCommit
{
    static GitAutoCommit()
    {
        // Adds the callback when the editor is about to quit
        EditorApplication.wantsToQuit += OnEditorQuit;
    }

    private static bool OnEditorQuit()
    {
        // Project path where the .git folder is located
        string projectPath = Application.dataPath.Replace("/Assets", "");

        // Check if the directory is a Git repository
        if (!CheckIfGitRepository(projectPath))
        {
            EditorUtility.DisplayDialog("Error", "You need to create and configure a Git repository before committing changes.", "OK");
            return true; // Allow the editor to close
        }

        // Get the Git status
        string gitStatus = RunGitCommand("git status --short", projectPath);
        int modifiedFiles = CountModifiedFiles(gitStatus);
        string modifiedFilesList = GetModifiedFilesList(gitStatus);

        // Display a confirmation dialog with the number of modified files
        if (EditorUtility.DisplayDialog("Commit Changes",
            $"You have {modifiedFiles} modified files. Do you want to send the changes to GitHub before closing?", "Yes", "No"))
        {
            // Function to send changes with a progress bar
            CommitAndPush(modifiedFilesList);
        }

        return true; // Allow the editor to close
    }

    private static bool CheckIfGitRepository(string projectPath)
    {
        // Run 'git rev-parse --is-inside-work-tree' command to check if it is a Git repository
        string result = RunGitCommand("git rev-parse --is-inside-work-tree", projectPath);
        return !string.IsNullOrEmpty(result) && result.Trim() == "true";
    }

    private static int CountModifiedFiles(string gitStatus)
    {
        // Uses regular expression to count the number of lines in git status (modified or new files)
        var matches = Regex.Matches(gitStatus, @"^\s*[MADRCU?]"); // M - modified, A - added, etc.
        return matches.Count;
    }

    private static string GetModifiedFilesList(string gitStatus)
    {
        // Returns the list of modified files from git status
        string[] lines = gitStatus.Split(new[] { "\r\n", "\r", "\n" }, StringSplitOptions.None);
        string modifiedFilesList = "Modified files:\n";
        foreach (string line in lines)
        {
            if (!string.IsNullOrEmpty(line))
            {
                modifiedFilesList += line + "\n";
            }
        }
        return modifiedFilesList;
    }

    private static void CommitAndPush(string modifiedFilesList)
    {
        // Project path where the .git folder is located
        string projectPath = Application.dataPath.Replace("/Assets", "");

        // Generate the commit version based on date and time (format v YYMMDD.HHMM)
        string version = $"v {DateTime.Now:yyMMdd.HHmm}";

        // Git commands
        string commitMessage = $"{version} - Automatic update before closing the editor\n\n{modifiedFilesList}";

        // Display progress bar
        EditorUtility.DisplayProgressBar("Sending to GitHub", "Adding files...", 0.3f);
        RunGitCommand("git add .", projectPath);

        // Update the progress bar
        EditorUtility.DisplayProgressBar("Sending to GitHub", "Committing...", 0.6f);
        RunGitCommand($"git commit -m \"{commitMessage}\"", projectPath);

        // Update the progress bar
        EditorUtility.DisplayProgressBar("Sending to GitHub", "Pushing...", 0.9f);
        RunGitCommand("git push", projectPath);

        // Clear the progress bar
        EditorUtility.ClearProgressBar();

        // Display success message
        EditorUtility.DisplayDialog("Git Push", "Updates sent successfully!", "OK");
    }

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
            UnityEngine.Debug.Log(output);
            UnityEngine.Debug.LogError(process.StandardError.ReadToEnd());
            return output;
        }
    }
}

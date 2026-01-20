using UnityEditor;
using UnityEngine;
using System.Diagnostics;

public static class SyncImagesFromGoogleDrive
{
    private const string SourcePath = @"G:\マイドライブ\協力脱出ゲーム\イラスト";
    private const string DestPath = @"Assets/Images";
    private const string ScriptPath = @"Tools/SyncGoogleDriveImages.ps1";

    [MenuItem("Tools/Sync Images from Google Drive")]
    public static void SyncImages()
    {
        string projectRoot = System.IO.Path.GetDirectoryName(Application.dataPath);
        string fullScriptPath = System.IO.Path.Combine(projectRoot, ScriptPath);
        string fullDestPath = System.IO.Path.Combine(projectRoot, DestPath);

        if (!System.IO.File.Exists(fullScriptPath))
        {
            EditorUtility.DisplayDialog("Error", 
                $"PowerShell script not found at:\n{fullScriptPath}", "OK");
            return;
        }

        // Create destination directory if it doesn't exist
        if (!System.IO.Directory.Exists(fullDestPath))
        {
            System.IO.Directory.CreateDirectory(fullDestPath);
        }

        EditorUtility.DisplayProgressBar("Syncing Images", "Running sync script...", 0.5f);

        try
        {
            ProcessStartInfo psi = new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = $"-ExecutionPolicy Bypass -File \"{fullScriptPath}\" " +
                           $"-SourcePath \"{SourcePath}\" -DestPath \"{fullDestPath}\"",
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            using (Process process = Process.Start(psi))
            {
                string output = process.StandardOutput.ReadToEnd();
                string error = process.StandardError.ReadToEnd();
                process.WaitForExit();

                UnityEngine.Debug.Log($"[SyncImages] Output:\n{output}");
                
                if (!string.IsNullOrEmpty(error))
                {
                    UnityEngine.Debug.LogError($"[SyncImages] Error:\n{error}");
                }

                if (process.ExitCode == 0)
                {
                    UnityEngine.Debug.Log("[SyncImages] Sync completed successfully!");
                }
                else
                {
                    UnityEngine.Debug.LogError($"[SyncImages] Sync failed with exit code: {process.ExitCode}");
                }
            }
        }
        finally
        {
            EditorUtility.ClearProgressBar();
        }

        // Refresh the AssetDatabase to see the new files
        AssetDatabase.Refresh();
        
        EditorUtility.DisplayDialog("Sync Complete", 
            "Image sync from Google Drive completed.\nCheck the Console for details.", "OK");
    }
}

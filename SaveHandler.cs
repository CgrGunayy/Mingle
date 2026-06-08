using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using System.Windows;

namespace MingleWPF
{
    public static class SaveHandler
    {
        private static readonly JsonSerializerOptions jsonOptions = new JsonSerializerOptions
        {
            WriteIndented = true
        };

        public static void SaveProject(string projectPath, string projectName, ProjectSaveData data)
        {
            try
            {
                if (!Directory.Exists(projectPath))
                {
                    Directory.CreateDirectory(projectPath);
                }

                string saveFilePath = Path.Combine(projectPath, $"{projectName}.ming");
                string jsonString = JsonSerializer.Serialize(data, jsonOptions);
                File.WriteAllText(saveFilePath, jsonString);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to save the project file.\nError: {ex.Message}", "Save Error");
            }
        }

        public static ProjectSaveData LoadProject(string projectPath, string projectName)
        {
            string saveFilePath = Path.Combine(projectPath, $"{projectName}.ming");

            if (!File.Exists(saveFilePath))
                return new ProjectSaveData();

            try
            {
                string jsonString = File.ReadAllText(saveFilePath);

                ProjectSaveData loadedData = JsonSerializer.Deserialize<ProjectSaveData>(jsonString, jsonOptions);
                return loadedData ?? new ProjectSaveData();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Failed to load the project file. The file might be corrupted.\nError: {ex.Message}", "Load Error");
                return new ProjectSaveData();
            }
        }
    }
}

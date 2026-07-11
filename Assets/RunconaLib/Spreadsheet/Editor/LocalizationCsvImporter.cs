using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using RunconaLib.Localization;
using UnityEditor;
using UnityEngine;

namespace RunconaLib.Spreadsheet.Editor
{
    public sealed class LocalizationCsvImporter : EditorWindow
    {
        private string _spreadsheetId = string.Empty;
        private string _sheetId = "0";
        private LocalizationTable _table;

        [MenuItem("Tools/RunconaLib/ローカライズCSVをインポート")]
        private static void Open() => GetWindow<LocalizationCsvImporter>("Localization CSV");

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Google Spreadsheet -> LocalizationTable", EditorStyles.boldLabel);
            _spreadsheetId = EditorGUILayout.TextField("Spreadsheet ID", _spreadsheetId);
            _sheetId = EditorGUILayout.TextField("Sheet gid", _sheetId);
            _table = (LocalizationTable)EditorGUILayout.ObjectField("Output Table", _table, typeof(LocalizationTable),
                false);
            EditorGUILayout.HelpBox("Columns: key, ja, en. The sheet must be readable by link.", MessageType.Info);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_spreadsheetId) || _table == null))
                if (GUILayout.Button("Download CSV and Import"))
                    ImportFromGoogleSheet();
            if (_table != null && GUILayout.Button("Import Local CSV")) ImportLocalCsv();
        }

        private async void ImportFromGoogleSheet()
        {
            try
            {
                using (var client = new HttpClient())
                    Import(await client.GetStringAsync(GoogleSheetsCsv.BuildExportUrl(_spreadsheetId, _sheetId)));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void ImportLocalCsv()
        {
            string path = EditorUtility.OpenFilePanel("Localization CSV", string.Empty, "csv");
            if (!string.IsNullOrEmpty(path)) Import(File.ReadAllText(path));
        }

        private void Import(string csv)
        {
            List<string[]> rows = CsvReader.Parse(csv);
            var entries = new List<LocalizationTable.Entry>();
            for (int i = 1; i < rows.Count; i++)
            {
                string[] row = rows[i];
                if (row.Length < 3 || string.IsNullOrWhiteSpace(row[0])) continue;
                entries.Add(new LocalizationTable.Entry { key = row[0].Trim(), japanese = row[1], english = row[2] });
            }

            Undo.RecordObject(_table, "Import localization CSV");
            _table.ReplaceEntries(entries);
            EditorUtility.SetDirty(_table);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Localization CSV] Imported {entries.Count} entries into {_table.name}.");
        }
    }
}
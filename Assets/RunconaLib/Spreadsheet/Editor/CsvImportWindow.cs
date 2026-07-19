using System;
using System.IO;
using System.Net.Http;
using UnityEditor;
using UnityEngine;

namespace RunconaLib.Spreadsheet.Editor
{
    /// <summary>CSVの取得元だけを共通化した、用途非依存のImporter基底クラス。</summary>
    public abstract class CsvImportWindow : EditorWindow
    {
        private string _spreadsheetId = string.Empty;
        private string _sheetId = "0";

        protected abstract string Description { get; }
        protected abstract string CsvColumns { get; }
        protected abstract bool CanImport { get; }

        protected void DrawImportGui()
        {
            EditorGUILayout.LabelField(Description, EditorStyles.boldLabel);
            _spreadsheetId = EditorGUILayout.TextField("Spreadsheet ID", _spreadsheetId);
            _sheetId = EditorGUILayout.TextField("Sheet gid", _sheetId);
            DrawTargetGui();
            EditorGUILayout.HelpBox(CsvColumns, MessageType.Info);

            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_spreadsheetId) || !CanImport))
                if (GUILayout.Button("Download CSV and Import"))
                    ImportFromGoogleSheet();

            using (new EditorGUI.DisabledScope(!CanImport))
                if (GUILayout.Button("Import Local CSV"))
                    ImportLocalCsv();
        }

        protected abstract void DrawTargetGui();
        protected abstract void ImportCsv(string csv);

        private async void ImportFromGoogleSheet()
        {
            try
            {
                using (var client = new HttpClient())
                    ImportCsv(await client.GetStringAsync(
                        GoogleSheetsCsv.BuildExportUrl(_spreadsheetId, _sheetId)));
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
        }

        private void ImportLocalCsv()
        {
            string path = EditorUtility.OpenFilePanel("CSV", string.Empty, "csv");
            if (!string.IsNullOrEmpty(path)) ImportCsv(File.ReadAllText(path));
        }
    }
}
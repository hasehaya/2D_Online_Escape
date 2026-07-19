using System.Collections.Generic;
using RunconaLib.Spreadsheet;
using RunconaLib.Spreadsheet.Editor;
using UnityEditor;
using UnityEngine;

namespace RunconaLib.Localization.Editor
{
    public sealed class LocalizationCsvImporter : CsvImportWindow
    {
        private LocalizationTable _table;

        protected override string Description => "Google Spreadsheet -> LocalizationTable";
        protected override string CsvColumns => "Columns: key, ja, en. The sheet must be readable by link.";
        protected override bool CanImport => _table != null;

        [MenuItem("Tools/RunconaLib/ローカライズCSVをインポート")]
        private static void Open() => GetWindow<LocalizationCsvImporter>("Localization CSV");

        private void OnGUI() => DrawImportGui();

        protected override void DrawTargetGui()
        {
            _table = (LocalizationTable)EditorGUILayout.ObjectField(
                "Output Table", _table, typeof(LocalizationTable), false);
        }

        protected override void ImportCsv(string csv)
        {
            List<string[]> rows = CsvReader.Parse(csv);
            var entries = new List<LocalizationTable.Entry>();
            for (int i = 1; i < rows.Count; i++)
            {
                string[] row = rows[i];
                if (row.Length < 3 || string.IsNullOrWhiteSpace(row[0])) continue;
                entries.Add(new LocalizationTable.Entry
                    { key = row[0].Trim(), japanese = row[1], english = row[2] });
            }

            Undo.RecordObject(_table, "Import localization CSV");
            _table.ReplaceEntries(entries);
            EditorUtility.SetDirty(_table);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Localization CSV] Imported {entries.Count} entries into {_table.name}.");
        }
    }
}
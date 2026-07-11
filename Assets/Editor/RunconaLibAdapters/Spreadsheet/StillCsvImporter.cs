using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using RunconaLib.Spreadsheet;
using UnityEditor;
using UnityEngine;

namespace Escape.EditorAdapters.Spreadsheet
{
    public sealed class StillCsvImporter : EditorWindow
    {
        private string _spreadsheetId = string.Empty;
        private string _sheetId = "0";
        private StillDialogueCatalog _catalog;

        [MenuItem("Tools/RunconaLib/Import Still CSV")]
        private static void Open() => GetWindow<StillCsvImporter>("Still CSV");

        private void OnGUI()
        {
            EditorGUILayout.LabelField("Google Spreadsheet -> StillDialogueCatalog", EditorStyles.boldLabel);
            _spreadsheetId = EditorGUILayout.TextField("Spreadsheet ID", _spreadsheetId);
            _sheetId = EditorGUILayout.TextField("Sheet gid", _sheetId);
            _catalog = (StillDialogueCatalog)EditorGUILayout.ObjectField("Output Catalog", _catalog,
                typeof(StillDialogueCatalog), false);
            EditorGUILayout.HelpBox(
                "Columns: still_id, order, character, ja, en. Text is stored as localization keys still.<id>.<order>.",
                MessageType.Info);
            using (new EditorGUI.DisabledScope(string.IsNullOrWhiteSpace(_spreadsheetId) || _catalog == null))
                if (GUILayout.Button("Download CSV and Import"))
                    ImportFromGoogleSheet();
            if (_catalog != null && GUILayout.Button("Import Local CSV")) ImportLocalCsv();
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
            string path = EditorUtility.OpenFilePanel("Still CSV", string.Empty, "csv");
            if (!string.IsNullOrEmpty(path)) Import(File.ReadAllText(path));
        }

        private void Import(string csv)
        {
            List<string[]> rows = CsvReader.Parse(csv);
            var grouped = new Dictionary<string, List<(int order, DialogueEntry entry)>>();
            for (int i = 1; i < rows.Count; i++)
            {
                string[] row = rows[i];
                if (row.Length < 5 || string.IsNullOrWhiteSpace(row[0])) continue;
                if (!Enum.TryParse(row[2], true, out DialogueCharacter character))
                    character = DialogueCharacter.Narration;
                int.TryParse(row[1], out int order);
                if (!grouped.TryGetValue(row[0], out var list))
                    grouped[row[0]] = list = new List<(int, DialogueEntry)>();
                list.Add((order, DialogueEntry.CreateLocalized(character, $"still.{row[0]}.{order}", row[3], row[4])));
            }

            var stills = new List<StillDialogueCatalog.Still>();
            foreach (var pair in grouped)
            {
                pair.Value.Sort((a, b) => a.order.CompareTo(b.order));
                stills.Add(new StillDialogueCatalog.Still
                    { id = pair.Key, dialogues = pair.Value.ConvertAll(x => x.entry).ToArray() });
            }

            Undo.RecordObject(_catalog, "Import still CSV");
            _catalog.Replace(stills);
            EditorUtility.SetDirty(_catalog);
            AssetDatabase.SaveAssets();
            Debug.Log($"[Still CSV] Imported {stills.Count} stills into {_catalog.name}.");
        }
    }
}
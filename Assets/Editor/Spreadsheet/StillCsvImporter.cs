using System;
using System.Collections.Generic;
using RunconaLib.Spreadsheet;
using RunconaLib.Spreadsheet.Editor;
using UnityEditor;
using UnityEngine;

namespace Escape.Editor.Spreadsheet
{
    public sealed class StillCsvImporter : CsvImportWindow
    {
        private StillDialogueCatalog _catalog;

        protected override string Description => "Google Spreadsheet -> StillDialogueCatalog";

        protected override string CsvColumns =>
            "Columns: still_id, order, character, ja, en. Text is stored as localization keys still.<id>.<order>.";

        protected override bool CanImport => _catalog != null;

        [MenuItem("Tools/スチルCSVをインポート")]
        private static void Open() => GetWindow<StillCsvImporter>("Still CSV");

        private void OnGUI() => DrawImportGui();

        protected override void DrawTargetGui()
        {
            _catalog = (StillDialogueCatalog)EditorGUILayout.ObjectField("Output Catalog", _catalog,
                typeof(StillDialogueCatalog), false);
        }

        protected override void ImportCsv(string csv)
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
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace RunconaLib.Spreadsheet
{
    public static class CsvReader
    {
        public static List<string[]> Parse(string csv)
        {
            var rows = new List<string[]>();
            var row = new List<string>();
            var field = new StringBuilder();
            bool quoted = false;

            using (var reader = new StringReader(csv ?? string.Empty))
            {
                while (reader.Peek() >= 0)
                {
                    char c = (char)reader.Read();
                    if (quoted && c == '"' && reader.Peek() == '"')
                    {
                        reader.Read();
                        field.Append('"');
                    }
                    else if (c == '"') quoted = !quoted;
                    else if (!quoted && c == ',')
                    {
                        row.Add(field.ToString());
                        field.Clear();
                    }
                    else if (!quoted && (c == '\r' || c == '\n'))
                    {
                        if (c == '\r' && reader.Peek() == '\n') reader.Read();
                        row.Add(field.ToString());
                        field.Clear();
                        if (row.Count > 1 || row[0].Length > 0) rows.Add(row.ToArray());
                        row.Clear();
                    }
                    else field.Append(c);
                }
            }

            if (field.Length > 0 || row.Count > 0)
            {
                row.Add(field.ToString());
                rows.Add(row.ToArray());
            }

            return rows;
        }
    }
}
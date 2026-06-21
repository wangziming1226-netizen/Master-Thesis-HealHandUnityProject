using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Text;
using UnityEngine;

namespace Thesis.Core
{
    public static class ExportUtils
    {
        // Safe for iOS/iPadOS and Editor.
        public static string GetExportDir()
        {
            string dir = Path.Combine(Application.persistentDataPath, "ThesisExports");
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);
            return dir;
        }

        public static string IsoNowUtc()
        {
            return DateTime.UtcNow.ToString("yyyy-MM-ddTHH:mm:ss.fffZ", CultureInfo.InvariantCulture);
        }

        public static void WriteTextFile(string path, string content)
        {
            File.WriteAllText(path, content, Encoding.UTF8);
        }

        public static string ToCsv(List<string> headers, List<List<string>> rows)
        {
            var sb = new StringBuilder();
            sb.AppendLine(string.Join(",", EscapeRow(headers)));
            foreach (var row in rows)
                sb.AppendLine(string.Join(",", EscapeRow(row)));
            return sb.ToString();
        }

        private static List<string> EscapeRow(List<string> row)
        {
            var escaped = new List<string>(row.Count);
            foreach (var cell in row)
            {
                if (cell == null) { escaped.Add(""); continue; }
                string c = cell.Replace("\"", "\"\"");
                bool needQuotes = c.Contains(",") || c.Contains("\n") || c.Contains("\r") || c.Contains("\"");
                escaped.Add(needQuotes ? $"\"{c}\"" : c);
            }
            return escaped;
        }
    }
}

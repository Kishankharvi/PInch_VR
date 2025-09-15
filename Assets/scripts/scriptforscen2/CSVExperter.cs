using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;

public class CSVExporter : MonoBehaviour
{
    public string fileNamePrefix = "rehab_session";

    public void ExportRows<T>(List<T> rows)
    {
        if (rows == null || rows.Count == 0)
        {
            Debug.Log("No rows to export.");
            return;
        }

        string path = Path.Combine(Application.persistentDataPath, $"{fileNamePrefix}_{System.DateTime.Now.ToString("yyyyMMdd_HHmmss")}.csv");
        StringBuilder sb = new StringBuilder();

        // Use reflection to write header
        var props = typeof(T).GetFields();
        for (int i = 0; i < props.Length; i++)
        {
            sb.Append(props[i].Name);
            if (i < props.Length - 1) sb.Append(",");
        }
        sb.AppendLine();

        foreach (var r in rows)
        {
            var vals = typeof(T).GetFields();
            for (int i = 0; i < vals.Length; i++)
            {
                object v = vals[i].GetValue(r);
                string s = v != null ? v.ToString() : "";
                // escape commas
                if (s.Contains(",")) s = "\"" + s + "\"";
                sb.Append(s);
                if (i < vals.Length - 1) sb.Append(",");
            }
            sb.AppendLine();
        }

        File.WriteAllText(path, sb.ToString(), Encoding.UTF8);
        Debug.Log($"CSV exported to: {path}");
        #if UNITY_ANDROID && !UNITY_EDITOR
        // optional: trigger Android share, or show toast via plugin
        #endif
    }
}

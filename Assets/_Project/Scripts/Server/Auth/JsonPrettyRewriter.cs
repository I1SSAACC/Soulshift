#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using System.IO;
using System.Text;

public class JsonPrettyRewriter : EditorWindow
{
    [MenuItem("Tools/Json Pretty Rewriter")]
    public static void ShowWindow() => GetWindow<JsonPrettyRewriter>("Json Pretty Rewriter");

    private void OnGUI()
    {
        if (GUILayout.Button("Reformat db JSON files"))
            ReformatDbJson();
    }

    private static void ReformatDbJson()
    {
        string dbPath = Path.Combine(Application.dataPath, "db");
        if (!Directory.Exists(dbPath))
        {
            Debug.LogWarning("[JsonPrettyRewriter] db folder not found at " + dbPath);
            return;
        }

        var files = Directory.GetFiles(dbPath, "*.json", SearchOption.AllDirectories);
        foreach (var f in files)
        {
            try
            {
                string raw = File.ReadAllText(f, Encoding.UTF8);
                var obj = UnityEngine.JsonUtility.FromJson<object>(raw);
                string pretty = UnityEngine.JsonUtility.ToJson(obj, true);
                if (!string.IsNullOrEmpty(pretty))
                    File.WriteAllText(f, pretty, Encoding.UTF8);
                Debug.Log("[JsonPrettyRewriter] Reformatted " + f);
            }
            catch (System.Exception ex)
            {
                Debug.LogError("[JsonPrettyRewriter] Failed " + f + " : " + ex);
            }
        }
        AssetDatabase.Refresh();
    }
}
#endif
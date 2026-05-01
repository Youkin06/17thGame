using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Linq;
using UnityEngine.SceneManagement;

namespace DependencyGraph
{
    public class DependencyGraphExporter : EditorWindow
    {
        // UI設定値
        private string scriptNameFilter = "";
        private string objectNameFilter = "";
        private string targetTag = "Untagged";
        private int targetLayer = 0; // Default
        private bool useFilters = false;
        private string outputDirectory = "Assets/Docs/DependencyGraphs";

        // スキャン結果保持用
        private int scannedGameObjectCount = 0;
        private int scannedScriptCount = 0;
        private int missingScriptCount = 0;
        private string lastExportPath = "";

        [MenuItem("Tools/Dependency Graph Exporter")]
        public static void ShowWindow()
        {
            GetWindow<DependencyGraphExporter>("Dep. Graph");
        }


        private void OnGUI()
        {
            GUILayout.Label("Dependency Graph Exporter", EditorStyles.boldLabel);

            // --- フィルタ設定 ---
            EditorGUILayout.Space();
            GUILayout.Label("Filters", EditorStyles.boldLabel);

            useFilters = EditorGUILayout.Toggle("Enable Filters", useFilters);

            if (useFilters)
            {
                EditorGUI.indentLevel++;
                scriptNameFilter = EditorGUILayout.TextField("Script Name Contains", scriptNameFilter);
                objectNameFilter = EditorGUILayout.TextField("GameObject Name Contains", objectNameFilter);
                targetTag = EditorGUILayout.TagField("Tag", targetTag);
                targetLayer = EditorGUILayout.LayerField("Layer", targetLayer);
                EditorGUI.indentLevel--;
            }

            // --- 出力設定 ---
            EditorGUILayout.Space();
            GUILayout.Label("Output Settings", EditorStyles.boldLabel);
            outputDirectory = EditorGUILayout.TextField("Output Path", outputDirectory);

            // --- 実行 ---
            EditorGUILayout.Space();
            if (GUILayout.Button("Scan & Export Mermaid", GUILayout.Height(40)))
            {
                ScanAndExport();
            }

            // --- 結果表示 ---
            if (scannedGameObjectCount > 0)
            {
                EditorGUILayout.Space();
                EditorGUILayout.HelpBox(
                    $"Scan Result:\n" +
                    $"- GameObjects: {scannedGameObjectCount}\n" +
                    $"- Scripts: {scannedScriptCount}\n" +
                    $"- Missing Scripts: {missingScriptCount}\n" +
                    $"Exported to: {lastExportPath}",
                    MessageType.Info);

                if (GUILayout.Button("Open Output Folder"))
                {
                    EditorUtility.RevealInFinder(lastExportPath);
                }
            }
        }

        private void ScanAndExport()
        {
            // 1. 初期化
            scannedGameObjectCount = 0;
            scannedScriptCount = 0;
            missingScriptCount = 0;

            StringBuilder sb = new StringBuilder();
            sb.AppendLine("flowchart LR"); // 左から右へのフローチャート

            // ノードとエッジの重複を防ぐためのセット
            HashSet<string> declaredNodes = new HashSet<string>();
            HashSet<string> edges = new HashSet<string>();

            // 2. シーン走査
            Scene activeScene = SceneManager.GetActiveScene();
            GameObject[] rootObjects = activeScene.GetRootGameObjects();

            foreach (var root in rootObjects)
            {
                TraverseHierarchy(root, sb, declaredNodes, edges);
            }

            // エッジを書き出し
            foreach (var edge in edges)
            {
                sb.AppendLine(edge);
            }

            // 3. ファイル出力
            EnsureDirectoryExists(outputDirectory);
            string fileName = $"{activeScene.name}_DepGraph_{System.DateTime.Now:yyyyMMdd_HHmm}.mmd";
            string fullPath = Path.Combine(outputDirectory, fileName);

            try
            {
                File.WriteAllText(fullPath, sb.ToString(), Encoding.UTF8);
                lastExportPath = fullPath;
                AssetDatabase.Refresh();
                Debug.Log($"[DependencyGraph] Exported: {fullPath}");
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to write file: {e.Message}");
            }
        }

        private void TraverseHierarchy(GameObject go, StringBuilder sb, HashSet<string> declaredNodes, HashSet<string> edges)
        {
            // フィルタリング処理
            if (useFilters)
            {
                if (!string.IsNullOrEmpty(objectNameFilter) && !go.name.Contains(objectNameFilter)) return;
                if (!string.IsNullOrEmpty(scriptNameFilter)) { /* スクリプト側で判定するためここではスキップしないが最適化余地あり */ }
                if (targetTag != "Untagged" && !go.CompareTag(targetTag)) return;
                // Layerフィルタは簡易実装（厳密にはビットマスクだが今回は一致判定）
                if (targetLayer != 0 && go.layer != targetLayer) return;
            }

            scannedGameObjectCount++;

            // GameObjectノード定義
            string goNodeId = $"GO_{go.GetInstanceID()}";
            string goNodeLabel = SanitizeLabel(go.name);
            string goDef = $"    {goNodeId}[\"{goNodeLabel}\"]";

            if (!declaredNodes.Contains(goNodeId))
            {
                sb.AppendLine(goDef);
                declaredNodes.Add(goNodeId);

                // クラススタイル適用（GameObjectを目立たせる）
                sb.AppendLine($"    style {goNodeId} fill:#e1f5fe,stroke:#01579b,stroke-width:2px");
            }

            // Components走査
            MonoBehaviour[] scripts = go.GetComponents<MonoBehaviour>();
            foreach (var script in scripts)
            {
                // Missing Script 対応
                if (script == null)
                {
                    missingScriptCount++;
                    string missingId = $"{goNodeId}_Missing_{missingScriptCount}";
                    sb.AppendLine($"    {missingId}(\"Missing Script\"):::missing");
                    edges.Add($"    {goNodeId} -.-> {missingId}");
                    continue;
                }

                // スクリプト名フィルタ
                if (useFilters && !string.IsNullOrEmpty(scriptNameFilter) && !script.GetType().Name.Contains(scriptNameFilter))
                {
                    continue;
                }

                scannedScriptCount++;
                string scriptName = script.GetType().Name;
                string scriptNodeId = $"SC_{script.GetInstanceID()}";
                string scriptLabel = SanitizeLabel(scriptName);

                // Scriptノード定義
                if (!declaredNodes.Contains(scriptNodeId))
                {
                    sb.AppendLine($"    {scriptNodeId}[[\"{scriptLabel}\"]]");
                    declaredNodes.Add(scriptNodeId);
                }

                // Edge: GameObject -> Script (アタッチ関係)
                edges.Add($"    {goNodeId} --> {scriptNodeId}");

                // Level 1: 依存関係解析 (SerializeField等の参照)
                AnalyzeDependencies(script, scriptNodeId, edges);
            }

            // 子オブジェクトへ再帰
            foreach (Transform child in go.transform)
            {
                TraverseHierarchy(child.gameObject, sb, declaredNodes, edges);
            }
        }

        private void AnalyzeDependencies(MonoBehaviour script, string sourceId, HashSet<string> edges)
        {
            // SerializedObjectを使ってInspector上の参照を取得
            SerializedObject so = new SerializedObject(script);
            SerializedProperty sp = so.GetIterator();

            while (sp.NextVisible(true))
            {
                // オブジェクト参照プロパティのみ対象
                if (sp.propertyType == SerializedPropertyType.ObjectReference)
                {
                    // 参照先がnullでない場合
                    if (sp.objectReferenceValue != null)
                    {
                        // 自身への参照は除外
                        if (sp.objectReferenceValue == script) continue;

                        Object target = sp.objectReferenceValue;
                        string targetId = "";

                        // 参照先が Component (Script) の場合
                        if (target is Component targetComp)
                        {
                            // ユーザー定義スクリプトなら SC_ID、標準コンポーネントなら GOへのリンクとして扱うか検討
                            // 今回は区別せず Component の InstanceID を使用
                            // ※注意: 対象がスキャン範囲外（非アクティブや別シーン）だとノード定義がないため、Mermaid上で孤立ノードになる可能性がある

                            if (targetComp is MonoBehaviour)
                            {
                                targetId = $"SC_{targetComp.GetInstanceID()}";
                            }
                            else
                            {
                                // 標準コンポーネント（Transform, MeshRenderer等）への参照は
                                // そのGameObjectへの依存として表現する（グラフ爆発を防ぐため）
                                targetId = $"GO_{targetComp.gameObject.GetInstanceID()}";
                            }
                        }
                        // 参照先が GameObject の場合
                        else if (target is GameObject targetGo)
                        {
                            targetId = $"GO_{targetGo.GetInstanceID()}";
                        }

                        if (!string.IsNullOrEmpty(targetId))
                        {
                            // Edge: Script -> Dependency
                            // 点線矢印にしてアタッチ関係と区別
                            edges.Add($"    {sourceId} -.-> {targetId}");
                        }
                    }
                }
            }
        }

        // ラベル用の文字列サニタイズ（ダブルクォート等を置換）
        private string SanitizeLabel(string label)
        {
            return label.Replace("\"", "'").Replace("\n", "");
        }

        private void EnsureDirectoryExists(string path)
        {
            if (path.StartsWith("Assets"))
            {
                string diskPath = Application.dataPath + path.Substring("Assets".Length);
                if (!Directory.Exists(diskPath))
                {
                    Directory.CreateDirectory(diskPath);
                }
            }
        }
    }
}
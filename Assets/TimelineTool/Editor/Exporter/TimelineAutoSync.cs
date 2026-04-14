using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.Timeline;

namespace TimelineTool.Editor
{
    /// <summary>
    /// Watches for .playable file saves and automatically syncs the paired
    /// TimelineSequenceData ScriptableObject.
    ///
    /// Naming convention:
    ///   Assets/Foo/MyCutscene.playable  →  Assets/Foo/MyCutscene_SequenceData.asset
    ///
    /// - First save: creates the SO automatically.
    /// - Subsequent saves: overwrites the SO in-place so scene references stay valid.
    /// - Only timelines containing at least one TimelineActionTrack are processed.
    /// </summary>
    internal class TimelineAutoSync : AssetPostprocessor
    {
        static void OnPostprocessAllAssets(
            string[] importedAssets,
            string[] deletedAssets,
            string[] movedAssets,
            string[] movedFromAssetPaths)
        {
            var pending = new List<string>();
            foreach (var path in importedAssets)
                if (path.EndsWith(".playable", System.StringComparison.OrdinalIgnoreCase))
                    pending.Add(path);

            if (pending.Count == 0) return;

            // Defer out of the import phase to avoid re-entrant processing.
            EditorApplication.delayCall += () => SyncAll(pending);
        }

        // -----------------------------------------------------------------------

        private static void SyncAll(IEnumerable<string> playablePaths)
        {
            bool anyChanged = false;

            foreach (var path in playablePaths)
            {
                var timeline = AssetDatabase.LoadAssetAtPath<TimelineAsset>(path);
                if (timeline == null || !HasActionTrack(timeline)) continue;

                string soPath = GetPairedPath(path);
                SyncOne(timeline, soPath);
                anyChanged = true;
            }

            if (anyChanged)
            {
                AssetDatabase.SaveAssets();
                AssetDatabase.Refresh();
            }
        }

        /// <summary>
        /// Syncs (or creates) the TimelineSequenceData SO at <paramref name="soPath"/>
        /// from <paramref name="timeline"/>. Public so SequenceExporter can call it directly.
        /// </summary>
        internal static void SyncOne(TimelineAsset timeline, string soPath)
        {
            var newData  = SequenceExporter.BuildSequenceData(timeline);
            var existing = AssetDatabase.LoadAssetAtPath<TimelineSequenceData>(soPath);

            if (existing != null)
            {
                // Overwrite in-place so any existing scene references remain valid.
                EditorUtility.CopySerialized(newData, existing);
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(newData);
                Debug.Log($"[TimelineTool] Synced → {soPath}  ({existing.tracks.Count} track(s))");
            }
            else
            {
                // Ensure the destination directory exists.
                string dir = Path.GetDirectoryName(soPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                AssetDatabase.CreateAsset(newData, soPath);
                Debug.Log($"[TimelineTool] Created → {soPath}  ({newData.tracks.Count} track(s))");
            }
        }

        // -----------------------------------------------------------------------

        internal static string GetPairedPath(string playablePath)
        {
            string dir  = Path.GetDirectoryName(playablePath)?.Replace('\\', '/');
            string name = Path.GetFileNameWithoutExtension(playablePath);
            return $"{dir}/{name}_SequenceData.asset";
        }

        private static bool HasActionTrack(TimelineAsset timeline)
        {
            foreach (var track in timeline.GetOutputTracks())
                if (track is TimelineActionTrack) return true;
            return false;
        }
    }
}

using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Playables;
using UnityEngine.Timeline;
using TimelinePlayer.Player;
using TimelinePlayer.Timeline;

namespace TimelinePlayer.Editor
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
                AssetDatabase.Refresh();
        }

        /// <summary>
        /// Syncs (or creates) the TimelineSequenceData SO at <paramref name="soPath"/>
        /// from <paramref name="timeline"/>. Public so SequenceExporter can call it directly.
        /// </summary>
        internal static void SyncOne(TimelineAsset timeline, string defaultSoPath)
        {
            string timelineGuid = AssetDatabase.AssetPathToGUID(AssetDatabase.GetAssetPath(timeline));

            var newData = SequenceExporter.BuildSequenceData(timeline);
            newData.SourceTimelineGuid = timelineGuid;

            // Prefer to find by stored GUID so the renamed file is updated in-place.
            // Fall back to the default naming-convention path for backward compatibility.
            var existing = FindExistingByTimelineGuid(timelineGuid)
                           ?? AssetDatabase.LoadAssetAtPath<TimelineSequenceData>(defaultSoPath);

            TimelineSequenceData finalAsset;

            if (existing != null)
            {
                string existingPath = AssetDatabase.GetAssetPath(existing);
                newData.name = existing.name;   // CopySerialized also copies 'name'; keep the existing filename
                EditorUtility.CopySerialized(newData, existing);
                EditorUtility.SetDirty(existing);
                Object.DestroyImmediate(newData);
                finalAsset = existing;
                Debug.Log($"[TimelineTool] Synced → {existingPath}  ({existing.Tracks.Count} track(s))");
            }
            else
            {
                string dir = Path.GetDirectoryName(defaultSoPath);
                if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

                newData.name = Path.GetFileNameWithoutExtension(defaultSoPath);
                AssetDatabase.CreateAsset(newData, defaultSoPath);
                finalAsset = newData;
                Debug.Log($"[TimelineTool] Created → {defaultSoPath}  ({newData.Tracks.Count} track(s))");
            }

            BindSceneSequencePlayers(timeline, finalAsset);
        }

        /// <summary>
        /// Searches the whole project for a TimelineSequenceData whose
        /// sourceTimelineGuid matches the given GUID.
        /// Returns null if not found.
        /// </summary>
        private static TimelineSequenceData FindExistingByTimelineGuid(string timelineGuid)
        {
            if (string.IsNullOrEmpty(timelineGuid)) return null;

            foreach (var guid in AssetDatabase.FindAssets("t:TimelineSequenceData"))
            {
                var path  = AssetDatabase.GUIDToAssetPath(guid);
                var asset = AssetDatabase.LoadAssetAtPath<TimelineSequenceData>(path);
                if (asset != null && asset.SourceTimelineGuid == timelineGuid)
                    return asset;
            }
            return null;
        }

        /// <summary>
        /// Finds all PlayableDirectors in loaded scenes that use <paramref name="timeline"/>,
        /// then gets or adds a SequencePlayer on each and assigns <paramref name="data"/>.
        /// </summary>
        private static void BindSceneSequencePlayers(TimelineAsset timeline, TimelineSequenceData data)
        {
#pragma warning disable CS0618
            var directors = Object.FindObjectsOfType<PlayableDirector>();
#pragma warning restore CS0618
            foreach (var director in directors)
            {
                if (director.playableAsset != timeline) continue;

                var go = director.gameObject;
                if (!go.scene.IsValid()) continue;   // skip prefab stage objects

                var player = go.GetComponent<SequencePlayer>();
                bool added = player == null;
                if (added)
                    player = Undo.AddComponent<SequencePlayer>(go);

                var so = new SerializedObject(player);
                so.FindProperty("sequenceData").objectReferenceValue = data;
                so.ApplyModifiedProperties();

                EditorUtility.SetDirty(player);
                EditorSceneManager.MarkSceneDirty(go.scene);

                Debug.Log(added
                    ? $"[TimelineTool] Added SequencePlayer + assigned sequenceData on '{go.name}'"
                    : $"[TimelineTool] Assigned sequenceData on '{go.name}'");
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

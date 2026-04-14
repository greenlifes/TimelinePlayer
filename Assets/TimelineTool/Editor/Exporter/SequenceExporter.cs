using System.IO;
using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace TimelineTool.Editor
{
    /// <summary>
    /// Reads the currently open Timeline and exports all TimelineActionTrack data
    /// (clip start frame, duration, action reference) into a TimelineSequenceData SO.
    ///
    /// Usage: TimelineTool > Export Active Timeline  (menu bar)
    ///        or call SequenceExporter.Export() from code.
    /// </summary>
    public static class SequenceExporter
    {
        private const int FPS = 60;

        // -----------------------------------------------------------------------

        [MenuItem("TimelineTool/Export Active Timeline to ScriptableObject")]
        public static void ExportActiveTimeline()
        {
            var director = TimelineEditor.inspectedDirector;
            if (director == null)
            {
                EditorUtility.DisplayDialog(
                    "TimelineTool Export",
                    "Open a Timeline in the Timeline window first, then run this export.",
                    "OK");
                return;
            }

            if (director.playableAsset is not TimelineAsset timelineAsset)
            {
                EditorUtility.DisplayDialog("TimelineTool Export", "No TimelineAsset found on the selected director.", "OK");
                return;
            }

            string defaultName = Path.GetFileNameWithoutExtension(
                AssetDatabase.GetAssetPath(timelineAsset)) + "_SequenceData";

            string savePath = EditorUtility.SaveFilePanelInProject(
                "Save Sequence Data",
                defaultName,
                "asset",
                "Choose where to save the exported SequenceData");

            if (string.IsNullOrEmpty(savePath)) return;

            var asset = BuildSequenceData(timelineAsset);
            AssetDatabase.CreateAsset(asset, savePath);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            EditorUtility.FocusProjectWindow();
            Selection.activeObject = asset;

            Debug.Log($"[TimelineTool] Exported → {savePath}  ({asset.tracks.Count} track(s))");
        }

        // -----------------------------------------------------------------------

        /// <summary>
        /// Builds a TimelineSequenceData from a TimelineAsset without saving it.
        /// Useful for in-code export pipelines.
        /// </summary>
        public static TimelineSequenceData BuildSequenceData(TimelineAsset timelineAsset)
        {
            var sequenceData = ScriptableObject.CreateInstance<TimelineSequenceData>();
            sequenceData.totalFrames = Mathf.RoundToInt((float)timelineAsset.duration * FPS);

            foreach (var track in timelineAsset.GetOutputTracks())
            {
                if (track is not TimelineActionTrack actionTrack) continue;

                var trackData = new TrackData
                {
                    trackName  = actionTrack.name,
                    bindingKey = actionTrack.name   // override in Inspector if needed
                };

                foreach (var clip in actionTrack.GetClips())
                {
                    if (clip.asset is not TimelineActionClip actionClip) continue;

                    trackData.clips.Add(new ClipData
                    {
                        startFrame     = Mathf.RoundToInt((float)clip.start    * FPS),
                        durationFrames = Mathf.RoundToInt((float)clip.duration * FPS),
                        actionData     = actionClip.actionData
                    });
                }

                sequenceData.tracks.Add(trackData);
            }

            return sequenceData;
        }
    }
}

using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;
using TimelinePlayer.Timeline;

namespace TimelinePlayer.Editor
{
    /// <summary>
    /// Utility for building TimelineSequenceData from a TimelineAsset.
    /// Auto-sync is handled by TimelineAutoSync (AssetPostprocessor).
    /// Use the menu item below only when you need an immediate manual sync.
    /// </summary>
    public static class SequenceExporter
    {
        // -----------------------------------------------------------------------

        [MenuItem("TimelineTool/Force Sync Active Timeline Now")]
        public static void ForceSyncActiveTimeline()
        {
            var director = TimelineEditor.inspectedDirector;
            if (director == null)
            {
                EditorUtility.DisplayDialog(
                    "TimelineTool",
                    "Open a Timeline in the Timeline window first.",
                    "OK");
                return;
            }

            if (director.playableAsset is not TimelineAsset timelineAsset)
            {
                EditorUtility.DisplayDialog("TimelineTool", "No TimelineAsset found.", "OK");
                return;
            }

            string playablePath = AssetDatabase.GetAssetPath(timelineAsset);
            TimelineAutoSync.SyncOne(timelineAsset, TimelineAutoSync.GetPairedPath(playablePath));
            AssetDatabase.SaveAssets();
        }

        // -----------------------------------------------------------------------

        /// <summary>
        /// Builds a fresh (unsaved) TimelineSequenceData from a TimelineAsset.
        /// The caller is responsible for saving or destroying the returned instance.
        /// </summary>
        public static TimelineSequenceData BuildSequenceData(TimelineAsset timelineAsset)
        {
            var data = ScriptableObject.CreateInstance<TimelineSequenceData>();
            float frameRate = (float)timelineAsset.editorSettings.frameRate;
            data.FrameRate   = frameRate;
            data.TotalFrames = Mathf.RoundToInt((float)timelineAsset.duration * frameRate);

            foreach (var track in timelineAsset.GetOutputTracks())
            {
                if (track is not TimelineActionTrack actionTrack) continue;

                var trackData = new TrackData
                {
                    TrackName = actionTrack.name
                };

                foreach (var clip in actionTrack.GetClips())
                {
                    if (clip.asset is not TimelineActionClipHolder actionClip) continue;

                    trackData.Clips.Add(new ClipData
                    {
                        StartFrame     = Mathf.RoundToInt((float)clip.start * frameRate),
                        DurationFrames = Mathf.RoundToInt((float)clip.duration * frameRate),
                        ActionData     = actionClip.ActionClip
                    });
                }

                data.Tracks.Add(trackData);
            }

            return data;
        }
    }
}

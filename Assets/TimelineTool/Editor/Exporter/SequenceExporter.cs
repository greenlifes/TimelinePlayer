using UnityEditor;
using UnityEditor.Timeline;
using UnityEngine;
using UnityEngine.Timeline;

namespace TimelinePlayer.Editor
{
    /// <summary>
    /// Utility for building TimelineSequenceData from a TimelineAsset.
    /// Auto-sync is handled by TimelineAutoSync (AssetPostprocessor).
    /// Use the menu item below only when you need an immediate manual sync.
    /// </summary>
    public static class SequenceExporter
    {
        internal const int FPS = 60;

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
            data.totalFrames = Mathf.RoundToInt((float)timelineAsset.duration * FPS);

            foreach (var track in timelineAsset.GetOutputTracks())
            {
                if (track is not TimelineActionTrack actionTrack) continue;

                var trackData = new TrackData
                {
                    trackName  = actionTrack.name,
                    bindingKey = actionTrack.name
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

                data.tracks.Add(trackData);
            }

            return data;
        }
    }
}

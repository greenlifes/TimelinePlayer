using System.Collections.Generic;

namespace TimelinePlayer
{
    /// <summary>
    /// Represents a single track exported from the Timeline.
    /// bindingKey maps to a GameObject supplied by SequencePlayer at runtime.
    /// </summary>
    [System.Serializable]
    public class TrackData
    {
        /// <summary>Display name of the track (matches the Timeline track name).</summary>
        public string trackName;

        /// <summary>
        /// Key used to resolve the bound GameObject at runtime.
        /// The SequencePlayer's binding list maps this key to a scene object.
        /// Defaults to trackName during export.
        /// </summary>
        public string bindingKey;

        public List<ClipData> clips = new();
    }
}

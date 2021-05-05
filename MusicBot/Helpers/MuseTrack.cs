using Discord;
using Victoria;

namespace MusicBot.Helpers
{
    /// <summary>
    /// Derivered from LavaTrack to add Requester
    /// </summary>
    public class MuseTrack : LavaTrack
    {
        public IGuildUser Requester { get; }
        public MuseTrack(LavaTrack lavaTrack, IGuildUser requester) : base(lavaTrack)
        {
            Requester = requester;
        }
    }
}

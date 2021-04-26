using Discord;
using Victoria;

namespace MusicBot.Helpers
{
    public class MuseTrack : LavaTrack
    {
        public LavaTrack Track { get; }
        public IGuildUser Requester { get; }
        public MuseTrack (LavaTrack lavaTrack, IGuildUser requester) : base(lavaTrack)
        {
            Track = lavaTrack;
            Requester = requester;
        }
    }
}

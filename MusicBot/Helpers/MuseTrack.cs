using Discord;
using Victoria;

namespace MusicBot.Helpers
{
    public class MuseTrack : LavaTrack
    {
        public IGuildUser Requester { get; }
        public MuseTrack (LavaTrack lavaTrack, IGuildUser requester) : base(lavaTrack)
        {
            Requester = requester;
        }
    }
}

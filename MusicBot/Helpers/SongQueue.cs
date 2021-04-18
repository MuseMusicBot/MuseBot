using Discord;
using System;
using System.Collections.Generic;
using System.Text;
using Victoria;
using System.Linq;

namespace MusicBot.Helpers
{
    public class SongQueue
    {
        public struct Song
        {
            public LavaTrack Track;
            public IGuildUser Requester;
        }

        private LinkedList<Song> Queue = new LinkedList<Song>();
        private readonly object QueueLock = new object();
        private static readonly Random random = new Random();
        public int Count
        {
            get
            {
                lock (QueueLock)
                {
                    return Queue.Count;
                }
            }
        }

        public void Enqueue(Song song)
        {
            lock (QueueLock)
            {
                Queue.AddLast(song);
            }
        }

        public bool TryDequeue(out Song song)
        {
            lock (QueueLock)
            {
                if (Queue.First == null)
                {
                    song = default;
                    return false;
                }

                song = Queue.First.Value;
                Queue.RemoveFirst();
            }

            return true;
        }

        public void Shuffle()
        {
            // Thanks Fisher Yates
            lock (QueueLock)
            {
                if (Queue.Count < 2)
                {
                    return;
                }

                int count = Queue.Count;
                int i = 0;
                LinkedListNode<Song>[] result = new LinkedListNode<Song>[count];

                for (var node = Queue.First; node != null; node = node.Next)
                {
                    int j = random.Next(i + 1);
                    if (i != j)
                    {
                        result[i] = result[j];
                    }
                    result[j] = node;
                    i++;
                }

                Queue.Clear();
                foreach (var entry in result)
                {
                    Queue.AddLast(entry);
                }
            }
        }
    }
}

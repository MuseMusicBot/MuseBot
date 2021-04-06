using Discord;
using System;
using System.Collections.Concurrent;
using System.Threading.Tasks;

namespace MusicBot.Helpers
{
    public class MessageHelper
    {
        private ConcurrentQueue<Tuple<IMessage, int>> DeletionQueue = new ConcurrentQueue<Tuple<IMessage, int>>();
        private readonly Task DeleteTask;

        public MessageHelper()
        {
            //DeleteTask = Task.Factory.StartNew(Delete);
        }

        public Task RemoveMessageAfterTimeout(IMessage message, int timeout = 10000)
        {
            message.DeleteAsync();
            //DeletionQueue.Enqueue(new Tuple<IMessage, int>(message, timeout));
            return Task.CompletedTask;
        }

        private async Task Delete()
        {
            while (true)
            {

                if (DeletionQueue.Count > 0)
                {
                    if (DeletionQueue.TryDequeue(out var msg))
                    {
                        _ = Task.Factory.StartNew(async () =>
                        {
                            await Task.Delay(msg.Item2);
                            await msg.Item1.DeleteAsync();
                        });
                    }
                }

                await Task.Delay(250);
            }
        }

    }
}
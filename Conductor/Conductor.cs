using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Messages;
using Microsoft.AspNetCore.SignalR;

namespace Conductor
{
    public class Conductor
    {
        private readonly SemaphoreSlim conductorSem = new SemaphoreSlim(1, 1);
        private readonly IHubContext<ConductorHub> hub;

        public int Musicians { get; private set; }
        public int Connected { get; private set; } = 0;
        public bool AllConnected => Connected == Musicians;

        public Conductor(IHubContext<ConductorHub> hubContext)
        {
            hub = hubContext;
            Musicians = MusiciansLoader.GetNumberOfMusicians();
        }

        public Task ConnectMusician(ConnectMessage message, string connectionId)
        {
            return LockAsync(async () => 
            {
                if (AllConnected)
                    return;

                Connected++;
                await hub.Groups.AddAsync(connectionId, message.Sender.ToString());
                Console.WriteLine($"Musician: {message.Sender} ({connectionId}), connected: {Connected}/{Musicians}");

                if (AllConnected)
                {
                    Console.WriteLine("All connected. Starting...");
                    await hub.Clients.All.SendAsync("start");
                }
            });
        }

        public Task SendMessage(string name, Message message)
        {
            return LockAsync(async () =>
            {
                foreach (var receiver in message.Receivers)
                {
                    await hub.Clients.Group(receiver.ToString()).SendAsync(name, message);
                }
            });
        }

        private async Task LockAsync(Func<Task> action)
        {
            await conductorSem.WaitAsync();

            try
            {
                await action();
            }
            finally
            {
                conductorSem.Release();
            }
        }
    }
}
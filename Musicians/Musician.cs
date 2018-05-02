using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Common;
using Common.Messages;
using Microsoft.AspNetCore.SignalR.Client;

namespace Musicians
{
    using BaseMusician = Common.Musician;

    public class Musician : BaseMusician
    {
        private readonly HubConnection connection;

        public Musician(int id, Position position) : base(id, position)
        {
            connection = new HubConnectionBuilder()
                .WithUrl(Configuration.ConductorHubUrl)
                .WithConsoleLogger()
                .Build();
        }

        public async Task Run()
        {
            connection.Closed += HandleConnectionError;
            await connection.StartAsync();

            connection.On("start", async () => await OnStartMessage());
            connection.On("neighbors", (int neighbor) => OnNeighborsMessage(neighbor));

            await connection.InvokeAsync("join", new JoinMessage() { SenderId = Id });
        }

        private async Task OnStartMessage()
        {
            Console.WriteLine($"[{Id}] Starting");
            await connection.InvokeAsync("neighbors", new NeighborsMessage() { SenderId = Id });
        }

        private void OnNeighborsMessage(int neighbor)
        {
            Console.WriteLine($"[{Id}] Received neighbor message from {neighbor}");
        }

        private void HandleConnectionError(Exception ex)
        {
            Console.WriteLine($"Connection closed with error: {ex}");
        }
    }
}
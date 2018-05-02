using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Messages;
using Microsoft.AspNetCore.Http.Connections;
using Microsoft.AspNetCore.SignalR.Client;

namespace Musicians
{
    public class Musician
    {
        private readonly HubConnection connection;

        public int Id { get; private set; }
        public Position Position { get; private set; }
        public MusicianStatus Status { get; private set; }

        private List<int> neighbors;
        public IReadOnlyList<int> Neighbors => neighbors;

        public Musician(int id, Position position)
        {
            Id = id;
            Position = position;
            Status = MusicianStatus.Waiting;

            connection = new HubConnectionBuilder()
                .WithUrl(Configuration.ConductorHubUrl)
                .WithConsoleLogger()
                .Build();
        }

        public void SetNeighbors(IEnumerable<int> neighbors)
        {
            this.neighbors = new List<int>(neighbors);
        }

        public async Task Run()
        {
            connection.Closed += HandleConnectionError;
            await connection.StartAsync();

            connection.On("start", OnStartMessage);

            await connection.InvokeAsync<ConnectMessage>("join", new ConnectMessage() { ConnectedId = Id });
        }

        private void OnStartMessage()
        {
            Console.WriteLine($"[{Id}] Starting");
        }

        private void HandleConnectionError(Exception ex)
        {
            Console.WriteLine($"Connection closed with error: {ex.Message}");
        }

        public enum MusicianStatus
        {
            Waiting,
            Performing,
            Inactive
        }
    }

    public static class MusiciansExtensions
    {
        public static void SetNeighbors(this IEnumerable<Musician> musicians)
        {
            foreach (var musician in musicians)
            {
                var neighbors = musicians.Where(m => m != musician && m.Position.DistanceTo(musician.Position) <= Configuration.NeighborMaximumDistance);
                var neighborsIds = neighbors.Select(n => n.Id);
                musician.SetNeighbors(neighborsIds);
            }
        }
    }
}
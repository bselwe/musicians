using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Common.Messages;
using Microsoft.AspNetCore.SignalR.Client;
using static Common.Messages.ExchangeMessage;
using static Common.Messages.PriorityMessage;
using static Musicians.Neighbor;

namespace Musicians
{
    public class Musician
    {
        private readonly HubConnection connection;
        private Dictionary<int, Neighbor> neighbors = new Dictionary<int, Neighbor>();
        private Timer timer;

        public int Id { get; private set; }
        public Position Position { get; private set; }
        public MusicianPriority Priority { get; private set; }
        public int PriorityValue { get; private set; }
        public bool Started { get; private set; }

        public IReadOnlyDictionary<int, Neighbor> Neighbors => neighbors;
        public IEnumerable<int> NeighborsIds => Neighbors.Keys;

        public bool NotWinnerSent { get; private set; }
        public DateTime PerformanceTimestamp { get; private set; }

        public Musician(int id, Position position, int priorityValue)
        {
            Id = id;
            Position = position;
            Priority = MusicianPriority.Unknown;
            PriorityValue = priorityValue;

            // Configure connection with the conductor
            connection = new HubConnectionBuilder()
                .WithUrl(Configuration.ConductorHubUrl)
                .WithConsoleLogger()
                .Build();
        }

        public void SetNeighbors(Dictionary<int, Neighbor> neighbors)
        {
            this.neighbors = new Dictionary<int, Neighbor>(neighbors);
        }

        public async Task Run()
        {
            connection.Closed += HandleConnectionError;
            await connection.StartAsync();

            connection.On("start", () => OnStartMessage());
            connection.On("exchange", (ExchangeMessage message) => OnExchangeMessage(message));
            connection.On("prioritize", (PriorityMessage message) => OnPriorityMessage(message));
            connection.On("perform", (PerformanceMessage message) => OnPerformanceMessage(message));
            connection.On("reject", (RejectMessage message) => OnRejectMessage(message));

            await Connect();
        }

        private Task OnStartMessage()
        {
            if (!Started)
            {
                Started = true;
                Console.WriteLine($"[{Id}] Starting with priority value {PriorityValue}");
                return Exchange(PriorityValue, ExchangeStatus.Requested, NeighborsIds);
            }
            
            return Task.CompletedTask;
        }

        private Task OnExchangeMessage(ExchangeMessage message)
        {
            Console.WriteLine($"[{Id}] Received exchange message from {message.Sender} with status {message.Status}");

            switch (message.Status)
            {
                case ExchangeStatus.Requested:
                    return HandleRequestedExchangeMessage(message);
                case ExchangeStatus.Accepted:
                    return HandleAcceptedExchangeMessage(message);
                case ExchangeStatus.Rejected:
                    return HandleRejectedExchangeMessage(message);
            }

            return Task.CompletedTask;
        }

        private Task HandleRequestedExchangeMessage(ExchangeMessage message)
        {
            if (Priority == MusicianPriority.Loser)
                return Exchange(message.Value, ExchangeStatus.Accepted, new[] { message.Sender });
            else if (Priority == MusicianPriority.Winner)
                return Exchange(Id, ExchangeStatus.Rejected, new[] { message.Sender });

            if (message.Value > PriorityValue)
                return Exchange(message.Value, ExchangeStatus.Accepted, new[] { message.Sender });
            return Exchange(Id, ExchangeStatus.Rejected, new[] { message.Sender });
        }

        private Task HandleAcceptedExchangeMessage(ExchangeMessage message)
        {
            Neighbors[message.Sender].Exchange = ExchangeResult.Accepted;
            return HandleVerifiedExchangeMessage(message);
        }

        private Task HandleRejectedExchangeMessage(ExchangeMessage message)
        {
            Neighbors[message.Sender].Exchange = ExchangeResult.Rejected;
            return HandleVerifiedExchangeMessage(message);
        }

        private async Task HandleVerifiedExchangeMessage(ExchangeMessage message)
        {
            if (Neighbors.Values.All(n => n.Exchange == ExchangeResult.Accepted))
            {
                await Prioritize(PriorityStatus.Winner, NeighborsIds);
                await Perform();
            }
        }

        private Task OnPriorityMessage(PriorityMessage message)
        {
            if (Priority == MusicianPriority.Winner)
                return Task.CompletedTask;

            Console.WriteLine($"[{Id}] Received priority message from {message.Sender} with status {message.Status}");
                
            switch (message.Status)
            {
                case PriorityStatus.Winner:
                    return HandleWinnerPriorityMessage(message);
                case PriorityStatus.NotWinner:
                    return HandleNotWinnerPriorityMessage(message);
            }

            return Task.CompletedTask;
        }

        private async Task HandleWinnerPriorityMessage(PriorityMessage message)
        {
            Neighbors[message.Sender].Priority = PriorityResult.Winner;
            Priority = MusicianPriority.Loser;

            if (!NotWinnerSent)
            {
                NotWinnerSent = true;
                await Prioritize(PriorityStatus.NotWinner, NeighborsIds);
            }

            WaitForPerformance();
        }

        private void WaitForPerformance()
        {
            PerformanceTimestamp = DateTime.Now;
            timer = new Timer((s) => CheckForPerformanceEnd(), null, Configuration.MusicianPerformanceEndTimeMs, Timeout.Infinite);
        }

        private Task CheckForPerformanceEnd()
        {
            if ((DateTime.Now - PerformanceTimestamp).TotalMilliseconds > Configuration.MusicianPerformanceEndTimeMs)
                return StartNewRound();

            timer.Change(Configuration.MusicianPerformanceEndTimeMs, Timeout.Infinite);
            return Task.CompletedTask;
        }

        private Task StartNewRound()
        {
            Console.WriteLine($"[{Id}] Starting new round...");
            return Task.CompletedTask;
        }

        private async Task HandleNotWinnerPriorityMessage(PriorityMessage message)
        {
            Neighbors[message.Sender].Priority = PriorityResult.NotWinner;

            if (!NotWinnerSent)
            {
                NotWinnerSent = true;
                await Prioritize(PriorityStatus.NotWinner, NeighborsIds.Where(id => id != message.Sender));
            }

            if (Neighbors.Values.All(n => n.Priority == PriorityResult.NotWinner))
            {
                ResetForExchange();
                Console.WriteLine($"[{Id}] Not winner, trying to exchange again...");
                await Exchange(PriorityValue, ExchangeStatus.Requested, NeighborsIds);
            }
        }

        private Task OnPerformanceMessage(PerformanceMessage message)
        {
            if (Priority == MusicianPriority.Loser)
                PerformanceTimestamp = DateTime.Now;
            return Task.CompletedTask;
        }

        private void OnRejectMessage(RejectMessage message)
        {
            Console.WriteLine($"[{Id}] Rejected by {message.Sender} with reason: \"{message.Reason}\"");
        }

        private Task Connect()
        {
            return connection.InvokeAsync("connect", new ConnectMessage() { Sender = Id });
        }

        private Task Exchange(int value, ExchangeStatus status, IEnumerable<int> receivers)
        {
            var message = new ExchangeMessage() 
            { 
                Sender = Id, 
                Receivers = receivers,
                Value = value,
                Status = status
            };

            return connection.InvokeAsync("exchange", message);
        }

        private Task Prioritize(PriorityStatus status, IEnumerable<int> receivers)
        {
            var message = new PriorityMessage() 
            { 
                Sender = Id, 
                Receivers = receivers,
                Status = status
            };

            return connection.InvokeAsync("prioritize", message);
        }

        private async Task Perform()
        {
            Priority = MusicianPriority.Winner;
            Console.WriteLine($"[{Id}] PERFORMING");

            var messagesLeft = Configuration.MusicianPerformanceTimeMs / Configuration.TimeBetweenPerformanceMessagesMs;
            while (messagesLeft > 0)
            {
                await connection.InvokeAsync("perform", new PerformanceMessage() { Sender = Id, Receivers = NeighborsIds });
                if (messagesLeft-- > 0) await Task.Delay(Configuration.TimeBetweenPerformanceMessagesMs);
            }

            Console.WriteLine($"[{Id}] FINISHED PERFORMING");
        }

        private Task Reject(Message msg, string reason)
        {
            var message = new RejectMessage() 
            { 
                Sender = Id, 
                Receivers = new[] { msg.Sender },
                Reason = reason
            };

            return connection.InvokeAsync("reject", message);
        }

        private void ResetForExchange()
        {
            // Priority = MusicianPriority.Unknown;
            NotWinnerSent = false;

            foreach (var neighbor in Neighbors.Keys)
            {
                Neighbors[neighbor].Priority = PriorityResult.Unknown;
                Neighbors[neighbor].Exchange = ExchangeResult.Unknown;
            }
        }

        private void HandleConnectionError(Exception ex)
        {
            Console.WriteLine($"Connection closed with error: {ex?.Message}");
        }

        public enum MusicianPriority
        {
            Unknown,
            Winner,
            Loser
        }
    }
}
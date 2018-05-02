using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Common;
using Microsoft.AspNetCore.SignalR.Client;

namespace Musicians
{
    class Musicians
    {
        static void Main(string[] args)
        {
            InitializeMusicians();
            WaitForTermination();
        }

        static void InitializeMusicians()
        {
            var musicians = new List<Musician>();
            var musicianIndex = 0;

            foreach (var position in MusiciansLoader.GetMusiciansPositions())
                musicians.Add(new Musician(musicianIndex++, position));

            musicians.SetNeighbors();

            foreach (var musician in musicians)
                new Thread(async () => await musician.Run()).Start();
        }

        static void WaitForTermination()
        {
            var quitEvent = new ManualResetEvent(false);

            Console.CancelKeyPress += (sender, e) => {
                quitEvent.Set();
                e.Cancel = true;
            };

            quitEvent.WaitOne();
        }
    }
}

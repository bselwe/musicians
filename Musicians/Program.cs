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
    class Program
    {
        static void Main(string[] args)
        {
            InitializeMusicians();
            WaitForTermination();
        }

        static void InitializeMusicians()
        {
            foreach (var musician in MusiciansLoader.GetMusicians())
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

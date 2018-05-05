using System;
using System.IO;
using System.Reflection;

namespace Common
{
    public class Configuration
    {   
        public static string ConductorBaseUrl = "http://localhost:3000";
        public static string ConductorHub = "/conductor";
        public static string ConductorHubUrl = $"{ConductorBaseUrl}{ConductorHub}";

        public static string MusiciansFile = $"{AppDomain.CurrentDomain.BaseDirectory}/positions-1";
        public static int NeighborMaximumDistance = 3;
        public static int MusicianPerformanceTimeMs = 2000;
        public static int MusicianPerformanceEndTimeMs = 1000;
        public static int TimeBetweenPerformanceMessagesMs = 100;
    }
}
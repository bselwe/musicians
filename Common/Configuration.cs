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

        public static string MusiciansFile = $"{AppDomain.CurrentDomain.BaseDirectory}/positions-2";
        public static int NeighborMaximumDistance = 3;
    }
}
using System;
using System.IO;
using Xamarin.Tools.Zip;

namespace Server
{
    class Program
    {
        static void Main(string[] args)
        {
            using (var zip = ZipArchive.Open ("/Users/dean/Downloads/xnageometry.zip", FileMode.Open)) {
                foreach (var e in zip) {
                    Console.WriteLine ($"{e.FullName} {e.CompressedSize} {e.CompressionMethod} {e.CRC} {e.ModificationTime} {e.Size}");
                }
            }
        }
    }
}

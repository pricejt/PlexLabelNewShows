using System;
using PlexLabelNewShows.Repositories;

namespace PlexLabelNewShows
{
    class Program
    {
        static void Main(string[] args)
        {
            var dataclass = new DataClass();
            var dt = dataclass.GetShowsWithoutLabel();
            dataclass.DisplayResults(dt);
            Console.WriteLine("Press enter to close...");
            Console.ReadLine();
        }
    }
}

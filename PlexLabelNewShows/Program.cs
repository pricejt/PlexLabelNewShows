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
            if (dt.Rows.Count > 0)
            {
                dataclass.DisplayResults(dt);
                dataclass.MarkShows(dt);
                dataclass.EmailShows(dt);
            }
            else
            {
                Console.WriteLine("No Rows to Process");
            }
            Console.WriteLine("Press enter to close...");
            Console.ReadLine();
        }
    }
}

using System;
using System.Data.SQLite;
using System.Configuration;
using System.Data;

namespace PlexLabelNewShows.Repositories
{
    class DataClass
    {
        private static SQLiteConnection sqlite;
        private static readonly string PlexDbPath = ConfigurationManager.AppSettings["PlexDB"];

        public DataClass()
        {
            var pathToFile = "Data Source=" + PlexDbPath + ";New=False;";
            sqlite = new SQLiteConnection(pathToFile);
        }

        public DataTable GetShowsWithoutLabel()
        {
            SQLiteDataAdapter ad;
            DataTable dt = new DataTable();

            const string query = @"SELECT mi.id
	,title
	,mi.Studio
	,round(mi.rating,2) as rating
	,mi.tags_genre
	,added_at
	,episodes.EpisodeCount
FROM metadata_items mi
LEFT JOIN (SELECT series.id, Count(*) as EpisodeCount
	From metadata_items episode 
	JOIN metadata_items season ON season.id = episode.parent_id 
	JOIN metadata_items series ON series.id = season.parent_id 
	Where episode.metadata_type = 4
	Group by series.id) episodes on
episodes.id = mi.id
WHERE mi.library_section_id = 1
	AND mi.parent_id IS NULL
	AND NOT EXISTS (
		SELECT tags.tag
		FROM tags
			,taggings
		WHERE tags.id = taggings.tag_id
			AND tags.tag_type = 11
			AND taggings.metadata_item_id = mi.id
		)
		AND Cast((JulianDay('now') - julianday(added_at)) as INTEGER) <= 60
ORDER BY added_at DESC";

            try
            {
                sqlite.Open();
                var cmd = sqlite.CreateCommand();
                cmd.CommandText = query;
                ad = new SQLiteDataAdapter(cmd);
                ad.Fill(dt);
            }
            catch (SQLiteException ex)
            {
                Console.WriteLine("Error: " + ex.Message);
                Console.WriteLine("Press enter to close...");
                Console.ReadLine();
            }
            finally
            {
                sqlite.Close();
            }

            return dt;
        }

        public void DisplayResults(DataTable dt)
        {
            Console.WriteLine("Rows: " + dt.Rows.Count);
            
            foreach (DataRow dataRow in dt.Rows)
            {
                foreach (var item in dataRow.ItemArray)
                {
                    Console.WriteLine(item);
                }
            }
        }
    }
}

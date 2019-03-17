using System;
using System.Data.SQLite;
using System.Configuration;
using System.Data;
using System.Net.Mail;
using static System.DateTime;

namespace PlexLabelNewShows.Repositories
{
    internal class DataClass
    {
        private static SQLiteConnection sqlite;
        private static readonly string PlexDbPath = ConfigurationManager.AppSettings["PlexDB"];
        private static readonly string PlexLabel = ConfigurationManager.AppSettings["Label"];
        private static readonly string FromEmail = ConfigurationManager.AppSettings["FromEmail"];
        private static readonly string ToEmail = ConfigurationManager.AppSettings["ToEmail"];
        private static readonly string SMTP = ConfigurationManager.AppSettings["SMTP"];
        private static readonly string Password = ConfigurationManager.AppSettings["Password"];
        private static readonly string EmailSubject = ConfigurationManager.AppSettings["EmailSubject"];

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

        public void MarkShows(DataTable dt)
        {
            var tag_id = GetTagId(PlexLabel);
            var now = UtcNow;
            const string query = @"insert into taggings (metadata_item_id, tag_id, created_at, 'index') values (@item_id, @tag_id, @created, 0);";


            if (tag_id != -1)
            {
                try
                {
                    sqlite.Open();

                    foreach (DataRow dataRow in dt.Rows)
                    {
                        var meta_id = dataRow[0];
                        var cmd = sqlite.CreateCommand();
                        cmd.Parameters.AddWithValue("@item_id", meta_id);
                        cmd.Parameters.AddWithValue("@tag_id", tag_id);
                        cmd.Parameters.AddWithValue("@created", now);
                        cmd.CommandText = query;
                        cmd.ExecuteNonQuery();
                    }
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

            }
        }

        public int GetTagId(string label)
        {
            const string query = @"select id from tags where tag=@label and tag_type=11";
            object tagId = null;
            try
            {
                sqlite.Open();
                var cmd = sqlite.CreateCommand();
                cmd.Parameters.AddWithValue("@label", label);
                cmd.CommandText = query;
                tagId = cmd.ExecuteScalar();
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
            if (tagId != null)
                return Convert.ToInt32(tagId);
            return -1;
        }

        public void EmailShows(DataTable dt)
        {
            string textBody = " <table border=" + 1 + " cellpadding=" + 0 + " cellspacing=" + 0 + " width = " + 800 + "><tr bgcolor='#4da6ff'><td><b>Id</b></td> <td> <b> Title</b> </td><td> <b> Studio</b> </td><td> <b> Rating</b> </td><td> <b> Genre</b> </td><td> <b> Added</b> </td><td> <b> Episode Count</b> </td></tr>";
            for (int loopCount = 0; loopCount < dt.Rows.Count; loopCount++)
            {
                textBody += "<tr><td>" + dt.Rows[loopCount]["id"] + "</td><td> " + dt.Rows[loopCount]["title"] + "</td><td> " + dt.Rows[loopCount]["studio"] + "</td><td> " + dt.Rows[loopCount]["rating"] + "</td><td> " + dt.Rows[loopCount]["tags_genre"] + "</td><td> " + dt.Rows[loopCount]["added_at"] + "</td><td> " + dt.Rows[loopCount]["EpisodeCount"] + "</td> </tr>";
            }
            textBody += "</table>";
            MailMessage mail = new MailMessage();
            System.Net.Mail.SmtpClient SmtpServer = new SmtpClient(SMTP);

            mail.From = new MailAddress(FromEmail);

            foreach (var email in ToEmail.Split(';'))
            {
                mail.To.Add(email);
            }
            mail.Subject = EmailSubject;
            mail.Body = textBody;
            mail.IsBodyHtml = true;
            SmtpServer.Port = 587;
            SmtpServer.DeliveryMethod = SmtpDeliveryMethod.Network;
            SmtpServer.UseDefaultCredentials = false;
            SmtpServer.Credentials = new System.Net.NetworkCredential(FromEmail, Password);
            SmtpServer.EnableSsl = true;
            System.Net.ServicePointManager.ServerCertificateValidationCallback = delegate {
                return true;
            };

            SmtpServer.Send(mail);
        }
    }
}
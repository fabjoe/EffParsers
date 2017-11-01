using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Net;
using System.IO;
using Json;
using Newtonsoft.Json;
using System.Data.SqlClient;
using System.Configuration;
using HtmlAgilityPack;
using System.Text.RegularExpressions;

namespace EffParsers
{
    class Program
    {
        static void Main(string[] args)
        {
            List<Player> playersList = new List<Player>();
            
            ParsePage(playersList);
            PrepareGamesTable();
            ParseEffPage(0);
            //ParseEffPage(15);
            //ParseEffPage(30);
            //ParseEffPage(45);
            //ParseEffPage(60);
            //ParseEffPage(75);
            //ParseEffPage(90);
            //ParseEffPage(105);
            //ParseEffPage(120);
            //ParseEffPage(135);
            //ParseEffPage(150);
            //ParseEffPage(165);
        }

        private static void ParseWeb2()
        {
            using (WebClient wc1 = new WebClient())
            {
                var link = "http://stats.nba.com/stats/leaguedashplayerstats?Season=2017-18&SeasonType=Regular+Season&LeagueID=00&MeasureType=Base&PerMode=PerGame&PlusMinus=N&PaceAdjust=N&Rank=N&Outcome=&Location=&Month=0&SeasonSegment=&DateFrom=&DateTo=&OpponentTeamID=0&VsConference=&VsDivision=&GameSegment=&Period=0&LastNGames=0&GameScope=&PlayerExperience=&PlayerPosition=&StarterBench=&ls=iref%3Anba%3Agnav&pageNo=1&rowsPerPage=500";
                wc1.Headers.Add("accept-encoding", "Accepflate, sdch");
                wc1.Headers.Add("Accept-Language", "en");
                wc1.Headers.Add("origin", "http://stats.nba.com");
                wc1.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0.2987.133 Safari/537.36");
                var json1 = wc1.DownloadString(link);
            }
            

            //Machine machine = JsonConvert.DeserializeObject<Machine>(json);
            //Console.WriteLine(machine.id);

            Console.Read();
        }

        private static void PrepareGamesTable()
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["ConnString"].ConnectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = "DELETE FROM Games";
                    cmd.ExecuteNonQuery();
                    cmd.CommandText = "DBCC CHECKIDENT ('Games', RESEED, 1);";
                    cmd.ExecuteNonQuery();

                }
            }
        }

        private static void ParseEffPage(int page)
        {
            string urlToParse = string.Empty;
            if (page == 0)
            {
                urlToParse = @"http://forum.gruppoesperti.it/viewtopic.php?f=188&t=114449";
            }
            else
            {
                urlToParse = @"http://forum.gruppoesperti.it/viewtopic.php?f=188&t=114449&start=" + page.ToString();
            }
            HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(urlToParse);
            myRequest.Method = "GET";
            WebResponse myResponse = myRequest.GetResponse();
            StreamReader sr = new StreamReader(myResponse.GetResponseStream(), System.Text.Encoding.UTF8);
            string result = sr.ReadToEnd();
            sr.Close();
            myResponse.Close();
            string[] lines = result.Split(new string[] { "<br />" }, StringSplitOptions.None);
            foreach (string line in lines)
            {
                if (!String.IsNullOrEmpty(line) && Char.IsLetter(line[0]))
                {
                    
                    if (!line.Contains("Grazie") && (!line.Contains("quotidianamente")) && (!line.Contains("pregati")) && (!line.Contains("Traduzione Italiana")) && (!line.Contains("Fantacalcio")) && (!line.Contains("Software")))
                    {
                        string game = line.Replace('\t', '_').Replace(@"/","").Replace("__","_");
                        string playerName = game.Substring(0, game.IndexOfAny("0123456789".ToCharArray()));
                        playerName = playerName.Replace("_", "").Trim();
                        string numericInfo = game.Substring(game.IndexOfAny("0123456789".ToCharArray()));
                        string[] gameInfo = numericInfo.Split('_');
                        Game gameToSave = new Game();
                        gameToSave.playerName = playerName;
                        gameToSave.minutesPlayed = gameInfo[0];
                        gameToSave.efficiency = Regex.Match(gameInfo[1].Replace("</div>",""), @"[+-]?\d+(\.\d+)?").Value;
                        gameToSave.effMin = CalculateEffMin(gameToSave.minutesPlayed, gameToSave.efficiency);
                        SaveGameOnDb(gameToSave);
                    }
                }
            }
        }

        private static void SaveGameOnDb(Game gameToSave)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["ConnString"].ConnectionString))
            {
                conn.Open();
                using (SqlCommand cmd = new SqlCommand())
                {
                    cmd.Connection = conn;
                    cmd.CommandText = @"INSERT INTO [dbo].[Games]
                                               ([PlayerName]
                                               ,[Minutes]
                                               ,[Efficiency]
                                               ,[EffMin]) VALUES
                                                (@PlayerName,
                                                 @Minutes,
                                                @Efficiency,
                                                @EffMin)";
                        cmd.Parameters.AddWithValue("@PlayerName", gameToSave.playerName);
                        cmd.Parameters.AddWithValue("@Minutes", gameToSave.minutesPlayed);
                        cmd.Parameters.AddWithValue("@Efficiency", gameToSave.efficiency);
                        cmd.Parameters.AddWithValue("@EffMin", gameToSave.effMin);
                        cmd.ExecuteNonQuery();
                }
            }
        }

        private static string CalculateEffMin(string min, string eff)
        {
            try
            {
                Decimal effmin = Decimal.Parse(eff) / Decimal.Parse(min);
                return effmin.ToString();
            }
            catch (System.Exception excep)
            {
                return string.Empty;
            }
        }

        private static string ConvertDateTimeForDb(string p)
        {
            string[] dates = p.Split('/');
            string date = dates[2] + dates[1].PadLeft(2,'0') + dates[0].PadLeft(2,'0');
            return date;
        }

        private static void ParsePage(List<Player> playersList)
        {
            WebClient wc1 = new WebClient();
            var link = "http://stats.nba.com/stats/leaguedashplayerstats?Season=2017-18&SeasonType=Regular+Season&LeagueID=00&MeasureType=Base&PerMode=PerGame&PlusMinus=N&PaceAdjust=N&Rank=N&Outcome=&Location=&Month=0&SeasonSegment=&DateFrom=&DateTo=&OpponentTeamID=0&VsConference=&VsDivision=&GameSegment=&Period=0&LastNGames=0&GameScope=&PlayerExperience=&PlayerPosition=&StarterBench=&ls=iref%3Anba%3Agnav&pageNo=1&rowsPerPage=500";
            wc1.Headers.Add("accept-encoding", "Accepflate, sdch");
            wc1.Headers.Add("Accept-Language", "en");
            wc1.Headers.Add("origin", "http://stats.nba.com");
            wc1.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 6.2; WOW64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/57.0.2987.133 Safari/537.36");
            var json1 = wc1.DownloadString(link);
            wc1.Dispose();
            SaveResultOnDb(ConvertResultToPlayersList(json1));
            //return result;
        }

        private static void SaveResultOnDb(List<Player> playersList)
        {
            using (SqlConnection conn = new SqlConnection(ConfigurationManager.ConnectionStrings["ConnString"].ConnectionString))
            {
                conn.Open();
                    using (SqlCommand cmd = new SqlCommand())
                    {
                        cmd.Connection = conn;
                        cmd.CommandText = "DELETE FROM Players";
                        cmd.ExecuteNonQuery();
                        foreach (Player p in playersList)
                        {
                            cmd.CommandText = @"INSERT INTO [dbo].[Players]
                                               ([PlayerID]
                                               ,[PlayerName]
                                               ,[GamesPlayed]
                                               ,[MinsPerGame]
                                               ,[FieldsGoalPercentage]
                                               ,[ThreePointsPercentage]
                                               ,[FreeThrowsPercentage]
                                               ,[ReboundsPerGame]
                                               ,[AssistsPerGame]
                                               ,[StealsPerGame]
                                               ,[BlocksPerGame]
                                               ,[TurnoversPerGame]
                                               ,[PointsPerGame]
                                               ,[EffPerGame]
                                               ,[EffMin]) VALUES
                                                (@PlayerID,
                                                 @PlayerName,
                                                @GamesPlayed,
                                                @MinsPerGame,
                                                @FieldsGoalPercentage,
                                                @ThreePointsPercentage,
                                                @FreeThrowsPercentage,
                                                @ReboundsPerGame,
                                                @AssistsPerGame,
                                                @StealsPerGame,
                                                @BlocksPerGame,
                                                @TurnoversPerGame,
                                                @PointsPerGame,
                                                @EffPerGame,
                                                @EffMin)";
                            cmd.Parameters.AddWithValue("@PlayerID",p.PlayerID);
                            cmd.Parameters.AddWithValue("@PlayerName", p.PlayerName);
                            cmd.Parameters.AddWithValue("@GamesPlayed", p.GamesPlayed);
                            cmd.Parameters.AddWithValue("@MinsPerGame", p.MinsPerGame);
                            cmd.Parameters.AddWithValue("@FieldsGoalPercentage", p.FieldsGoalPercentage);
                            cmd.Parameters.AddWithValue("@ThreePointsPercentage", p.ThreePointsPercentage);
                            cmd.Parameters.AddWithValue("@FreeThrowsPercentage", p.FreeThrowsPercentage);
                            cmd.Parameters.AddWithValue("@ReboundsPerGame", p.ReboundsPerGame);
                            cmd.Parameters.AddWithValue("@AssistsPerGame", p.AssistsPerGame);
                            cmd.Parameters.AddWithValue("@StealsPerGame", p.StealsPerGame);
                            cmd.Parameters.AddWithValue("@BlocksPerGame", p.BlocksPerGame);
                            cmd.Parameters.AddWithValue("@TurnoversPerGame", p.TurnoversPerGame);
                            cmd.Parameters.AddWithValue("@PointsPerGame", p.PointsPerGame);
                            cmd.Parameters.AddWithValue("@EffPerGame", p.EffPerGame);
                            cmd.Parameters.AddWithValue("@EffMin", p.EffMin);
                            cmd.ExecuteNonQuery();
                            cmd.Parameters.Clear();
                    }
                }
            }
        }

        private static List<Player> ConvertResultToPlayersList(string result)
        {
            List<Player> playersList = new List<Player>();
            RootObject oRes = JsonConvert.DeserializeObject<RootObject>(result);
            foreach (object o in oRes.resultSets[0].rowSet)
            {
                Player player = CreatePlayer((List<object>)o);
                playersList.Add(player);
            }
            return playersList;            
        }

        private static Player CreatePlayer(List<object> o)
        {
            Player player = new Player();
            player.PlayerID = o[0].ToString();
            player.PlayerName = o[1].ToString();
            player.GamesPlayed = Int32.Parse(o[5].ToString());
            player.MinsPerGame = Decimal.Parse(o[9].ToString());
            player.FieldsGoalPercentage = Decimal.Parse(o[12].ToString());
            player.ThreePointsPercentage = Decimal.Parse(o[15].ToString());
            player.FreeThrowsPercentage = Decimal.Parse(o[18].ToString());
            player.ReboundsPerGame = Decimal.Parse(o[21].ToString());
            player.AssistsPerGame  = Decimal.Parse(o[22].ToString());
            player.TurnoversPerGame = Decimal.Parse(o[23].ToString());
            player.StealsPerGame  = Decimal.Parse(o[24].ToString());
            player.BlocksPerGame  = Decimal.Parse(o[25].ToString());
            player.PointsPerGame = Decimal.Parse(o[29].ToString());
            player.EffPerGame = player.PointsPerGame - ((Decimal.Parse(o[11].ToString()))-(Decimal.Parse(o[10].ToString()))) - ((Decimal.Parse(o[17].ToString()))-(Decimal.Parse(o[16].ToString()))) + player.ReboundsPerGame + player.StealsPerGame - player.TurnoversPerGame + player.BlocksPerGame + player.AssistsPerGame;
            if (player.MinsPerGame > 0)
            {
                player.EffMin = player.EffPerGame / player.MinsPerGame;
            }
            else
            {
                player.EffMin = 0;
            }

            return player;
        }
    }
}

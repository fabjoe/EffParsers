﻿using System;
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
using System.Globalization;


namespace EffParsers
{
    class Program
    {
        static void Main(string[] args)
        {
            List<Player> playersList = new List<Player>();
            
            //ParsePage(playersList);
            //ParsePageFromFile(playersList);

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
            //ParseEffPage(180);
            //ParseEffPage(195);
            //ParseEffPage(210);
        }

        private static void ParseWeb2()
        {
            using (WebClient wc1 = new WebClient())
            {
                var link = "https://stats.nba.com/stats/leaguedashplayerstats?Season=2019-20&SeasonType=Regular+Season&LeagueID=00&MeasureType=Base&PerMode=PerGame&PlusMinus=N&PaceAdjust=N&Rank=N&Outcome=&Location=&Month=0&SeasonSegment=&DateFrom=&DateTo=&OpponentTeamID=0&VsConference=&VsDivision=&GameSegment=&Period=0&LastNGames=0&GameScope=&PlayerExperience=&PlayerPosition=&StarterBench=&ls=iref%3Anba%3Agnav&pageNo=1&rowsPerPage=500";
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
                urlToParse = @"https://forum.gruppoesperti.it/viewtopic.php?f=188&t=219118";
            }
            else
            {
                urlToParse = @"https://forum.gruppoesperti.it/viewtopic.php?f=188&t=219118&start=" + page.ToString();
            }
            HttpWebRequest myRequest = (HttpWebRequest)WebRequest.Create(urlToParse);
            myRequest.Method = "GET";
            WebResponse myResponse = myRequest.GetResponse();
            StreamReader sr = new StreamReader(myResponse.GetResponseStream(), System.Text.Encoding.UTF8);
            string result = sr.ReadToEnd();
            sr.Close();
            myResponse.Close();
            string[] lines = result.Split(new string[] { "<br/>" }, StringSplitOptions.None);
            DateTime dtMatchDate = new DateTime(1972, 6, 14);
            foreach (string line in lines)
            {
                if (line.Contains("#0000FF"))
                {
                    dtMatchDate = ExtractDateFromLine(line);
                }
                if (!String.IsNullOrEmpty(line) && Char.IsLetter(line[0]))
                {
                    string cleanLine = string.Empty;
                    if (line.Contains("<"))
                        cleanLine = line.Substring(0, line.IndexOfAny("<".ToCharArray()));
                    else
                        cleanLine = line;
                    if (!cleanLine.Contains("Grazie") && (!cleanLine.Contains("quotidianamente")) && (!cleanLine.Contains("pregati")) && (!cleanLine.Contains("Traduzione Italiana")) && (!cleanLine.Contains("Fantacalcio")) && (!cleanLine.Contains("Software")))
                    {
                        string game = cleanLine.Replace('\t', '_').Replace(@"/","").Replace("__","_");
                        string playerName = game.Substring(0, game.IndexOfAny("0123456789".ToCharArray()));
                        playerName = playerName.Replace("_", "").Trim();
                        string numericInfo = game.Substring(game.IndexOfAny("0123456789".ToCharArray()));
                        string[] gameInfo = null;
                        if(numericInfo.Contains("_"))
                            gameInfo = numericInfo.Split('_');
                        if (numericInfo.Contains(" "))
                            gameInfo = numericInfo.Split(' ');
                        Game gameToSave = new Game();
                        gameToSave.playerName = playerName;
                        gameToSave.minutesPlayed = Extensions.GetNumbers(gameInfo[0]);
                        gameToSave.efficiency = Extensions.GetNumbers(Regex.Match(gameInfo[gameInfo.Length-1].Replace("</div>",""), @"[+-]?\d+(\.\d+)?").Value);
                        gameToSave.effMin = CalculateEffMin(gameToSave.minutesPlayed, gameToSave.efficiency);
                        gameToSave.matchDate = dtMatchDate;
                        SaveGameOnDb(gameToSave);
                    }
                }
            }
        }

        private static DateTime ExtractDateFromLine(string line)
        {
            string lineWithDate = line;
            int position = lineWithDate.LastIndexOf(@"color: #0000FF""><span style=""font-weight: bold"">");
            if (position > -1)
            {
                lineWithDate = lineWithDate.Substring(position + 52);
                var stringPart = lineWithDate.Split('<');
                var dateParts = stringPart[0].Split('/');
                int day = Int32.Parse(dateParts[0]);
                int month = Int32.Parse(dateParts[1]);
                int year = Int32.Parse(dateParts[2]);

                DateTime dt = new DateTime(year,month,day);
                return dt;
                //Regex rgx = new Regex(@"\d{2}/\d{2}/\d{4}");
                //Match mat = rgx.Match(stringPart[0]);
                //if (mat.Success)
                //    if (DateTime.TryParseExact(mat.ToString(), "dd/MM/yyyy", null, DateTimeStyles.None, out dt))
                //return dt;
            }
            return new DateTime(1972,6,14);
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
                                               ,[EffMin]
                                               ,[MatchDate]) VALUES
                                                (@PlayerName,
                                                 @Minutes,
                                                @Efficiency,
                                                @EffMin,
                                                @MatchDate)";
                        cmd.Parameters.AddWithValue("@PlayerName", gameToSave.playerName);
                        cmd.Parameters.AddWithValue("@Minutes", gameToSave.minutesPlayed);
                        cmd.Parameters.AddWithValue("@Efficiency", gameToSave.efficiency);
                        cmd.Parameters.AddWithValue("@EffMin", gameToSave.effMin);
                        cmd.Parameters.AddWithValue("@MatchDate", gameToSave.matchDate);
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
                return "0";
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
            var link = "https://stats.nba.com/stats/leaguedashplayerstats?Season=2019-20&SeasonType=Regular+Season&LeagueID=00&MeasureType=Base&PerMode=PerGame&PlusMinus=N&PaceAdjust=N&Rank=N&Outcome=&Location=&Month=0&SeasonSegment=&DateFrom=&DateTo=&OpponentTeamID=0&VsConference=&VsDivision=&GameSegment=&Period=0&LastNGames=0&GameScope=&PlayerExperience=&PlayerPosition=&StarterBench=&ls=iref%3Anba%3Agnav&pageNo=1&rowsPerPage=500";
            wc1.Headers.Add("host", "stats.nba.com");
            //wc1.Headers.Add("cache-control", "max-age=0");
            wc1.Headers.Add("Referer", "https://stats.nba.com");
            wc1.Headers.Add("Origin", "https://stats.nba.com");
            wc1.Headers.Add("x-nba-stats-origin", "stats");
            wc1.Headers.Add("x-nba-stats-token", "true");
            wc1.Headers.Add("user-agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64) AppleWebKit/537.36 (KHTML, like Gecko) Chrome/69.0.3497.100 Safari/537.36");
            //wc1.Headers.Add("accept", "text/html,application/xhtml+xml,application/xml;q=0.9,image/webp,image/apng,*/*;q=0.8");
            //wc1.Headers.Add("accept-Encoding", "gzip, deflate, br, Accepflate, sdch");
            //wc1.Headers.Add("accept-Language", "it-IT,it;q=0.9,en-US;q=0.8,en;q=0.7");


            var json1 = wc1.DownloadString(link);
            wc1.Dispose();
            SaveResultOnDb(ConvertResultToPlayersList(json1));
            //return result;
        }
        private static void ParsePageFromFile(List<Player> playersList)
        {
            string json = string.Empty;
            using (StreamReader r = new StreamReader("PlayerFile.txt"))
            {
                json = r.ReadToEnd();
                
            }
            SaveResultOnDb(ConvertResultToPlayersList(json));
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

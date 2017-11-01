using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EffParsers
{
    public class Player
    {
        public string PlayerID { get; set; }
        public string PlayerName { get; set; }
        public Decimal MinsPerGame { get; set; }
        public Decimal EffPerGame { get; set; }
        public Decimal EffMin { get; set; }
        public int GamesPlayed { get; set; }
        public Decimal FieldsGoalPercentage { get; set; }
        public Decimal ThreePointsPercentage { get; set; }
        public Decimal FreeThrowsPercentage { get; set; }
        public Decimal ReboundsPerGame { get; set; }
        public Decimal AssistsPerGame { get; set; }
        public Decimal StealsPerGame { get; set; }
        public Decimal BlocksPerGame { get; set; }
        public Decimal TurnoversPerGame { get; set; }
        public Decimal PointsPerGame { get; set; }
 
    }
    public class Parameters
    {
        public string MeasureType { get; set; }
        public string PerMode { get; set; }
        public string PlusMinus { get; set; }
        public string PaceAdjust { get; set; }
        public string Rank { get; set; }
        public string LeagueID { get; set; }
        public string Season { get; set; }
        public string SeasonType { get; set; }
        public object Outcome { get; set; }
        public object Location { get; set; }
        public int Month { get; set; }
        public object SeasonSegment { get; set; }
        public object DateFrom { get; set; }
        public object DateTo { get; set; }
        public int OpponentTeamID { get; set; }
        public object VsConference { get; set; }
        public object VsDivision { get; set; }
        public object GameSegment { get; set; }
        public int Period { get; set; }
        public int LastNGames { get; set; }
        public object GameScope { get; set; }
        public object PlayerExperience { get; set; }
        public object PlayerPosition { get; set; }
        public object StarterBench { get; set; }
    }

    public class ResultSet
    {
        public string name { get; set; }
        public List<string> headers { get; set; }
        public List<List<object>> rowSet { get; set; }
    }

    public class RootObject
    {
        public string resource { get; set; }
        public Parameters parameters { get; set; }
        public List<ResultSet> resultSets { get; set; }
    }
}

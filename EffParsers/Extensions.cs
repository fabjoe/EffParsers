using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace EffParsers
{
    public static class Extensions
    {
        public static string GetNumbers(string input)
        {
            var match = Regex.Match(input, "-?\\d+");
            return match.Value;
            //return new string(input.Where(c => char.IsDigit(c)).ToArray());
        }
    }
}

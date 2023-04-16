using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace EffParsers
{
    public static class Extensions
    {
        public static string GetNumbers(string input)
        {
            return new string(input.Where(c => char.IsDigit(c)).ToArray());
        }
    }
}

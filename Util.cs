using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MaxAI
{
    class Util
    {
        public static A[,] Assemble<A>(IEnumerable<A>[,] matrix, int index) 
        {
            var ret = new A[4, 4];
            for (int i = 0; i < 4; i++)
            {
                for (int j = 0; j < 4; j++)
                {
                    ret[i, j] = matrix[i, j].ElementAt(index);
                }
            }
            return ret;
        }

        private static Random rng = new Random();

        public static string GetRandomName()
        {
            string[] names = new string[] { "deep", "blue", "krissi", "jens", "go", "max", "bölle", "spitz", "haut", "herz", "alt", "blau", "ober", "unter", "meier", "aiges", "sechzig", "zehner", "herzacht", "rockstar", "gerald", "luke", "vader", "wenz", "hentai", "metallica", "mario", "luigi", "shyguy", "drei", "schlogerwechsel", "maschie", "gspannt", "ruffy", "zorro", "totoro", "link", "cloud", "herzsau", "baiml", "bohemian", "trump", "ratm", "ai", "darwin", "loner", "madhatter", "alise", "tesla", "goku", "geist", "blue", "milano", "sauerkraut", "toad", "ray", "vestigo", "sam", "newtown", "stubn", "gsicht", "chick", "donald", "öyster" };

            return names[rng.Next(names.Count() - 1)] + rng.Next(99);
        }

    }


    static class RichInt
    {

        public static IEnumerable<T> Times<T>(this int times, Func<int, T> function)
        {
            for (int i = 0; i < times; i++)
            {
                yield return function(i);
            }
        }


        public static void Times(this int times, Action action)
        {
            for (int i = 0; i < times; i++)
            {
                action();
            }
        }

    }

    public static class RichMatrix
    {

        public static A[][] AssembleAt <A>(this IEnumerable<A>[][] matrix, int index)
        {
            var ret = new A[4] [];
            for (int i = 0; i < 4; i++)
            {
                ret[i] = new A[4];
                for (int j = 0; j < 4; j++)
                {
                    ret[i] [j] = matrix[i] [j].ElementAt(index);
                }
            }
            return ret;
        }

        }

    static class RichList
    {
        public static A MaxBy<A, T>(this IEnumerable<A> ents, Func<A, T> func)
        {
            var orderd = ents.OrderByDescending(x => func(x));
            return orderd.FirstOrDefault();
        }

        public static A MinBy<A, T>(this IEnumerable<A> ents, Func<A, T> func)
        {
            var orderd = ents.OrderBy(x => func(x));
            return orderd.FirstOrDefault();
        }

        public static string MakeString<A>(this IEnumerable<A> ents, string separationString, string endingSeperator, Func<A, string> func)
        {
            var sb = new StringBuilder();
            int c = 1;
            foreach (var e in ents)
            {
                if (sb.Length == 0)
                {
                    sb.Append(func(e));
                }
                else
                {
                    if (c == ents.Count() && c != 1 && !endingSeperator.Equals(""))
                    {
                        sb.Append(endingSeperator + func(e));
                    }
                    else
                    {
                        sb.Append(separationString + func(e));
                    }
                }
                c++;
            }
            return sb.ToString();
        }

        public static string MakeString<A>(this IEnumerable<A> ents, string seperationString, Func<A, string> func)
        {
            return MakeString<A>(ents, seperationString, "", func);
        }

        public static string MakeString(this IEnumerable<string> ents, string seperationString)
        {
            return MakeString(ents, seperationString, x => x);
        }

        private static Random rng = new Random();
        public static void Shuffle<T> (this List<T> list, Random rnd = null)
        {
            var random = rnd == null ? rng : rnd;
            int n = list.Count;
            while (n > 1)
            {
                n--;
                int k = random.Next(n + 1);
                var value = list[k];
                list[k] = list[n];
                list[n] = value;
            }
        }

        public static string GetRandomName()
        {
            string[] names = new string[] { "deep", "mind", "krissi", "jens", "go", "max", "bölle", "spitz", "haut", "herz", "alt", "blau", "ober", "unter", "meier", "aiges", "sechzig", "zehner", "herzacht", "rockstar", "gerald", "luke", "vader", "wenz", "sexy", "metallica", "mario", "luigi", "shyguy", "drei", "schlogerwechsel", "maschie", "gspannt", "ruffy", "zorro", "totoro", "link", "cloud", "herzsau", "baiml", "bohemian", "trumpf", "ratm", "ai", "darwin", "loner", "madhatter", "alise", "tesla", "goku", "obstler", "schnitzel", "milano", "sauerkraut", "toad", "ray", "vestigo", "sam", "newtown", "stubn", "gsicht", "chick", "donald", "öyster" };

            return names[rng.Next(names.Count() - 1)] + rng.Next(99);
        }
    }
}

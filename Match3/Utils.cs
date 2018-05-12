using System;

namespace Match3
{
    public static class Utils
    {
        public static Tuple<int, int> Add(Tuple<int, int> a, Tuple<int, int> b)
        {
            return Tuple.Create(a.Item1 + b.Item1, a.Item2 + b.Item2);
        }

        public static T Get<T>(T[,] values, Tuple<int, int> index)
        {
            return values[index.Item1, index.Item2];
        }

        public static void Set<T>(T[,] values, Tuple<int, int> index, T value)
        {
            values[index.Item1, index.Item2] = value;
        }
    }
}

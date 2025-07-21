using MAUI_Nonsense_App.Services;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MAUI_Nonsense_App.Platforms.iOS.Services.Random
{
    public class iOSRandomNumberService : IRandomNumberService
    {
        private readonly System.Random _random = new System.Random();

        public List<int> GenerateNumbers(int from, int to, int count, bool allowDuplicates)
        {
            if (from > to) throw new ArgumentException("From must be <= To");

            var result = new List<int>();
            var available = Enumerable.Range(from, to - from + 1).ToList();

            if (!allowDuplicates && count > available.Count)
                throw new InvalidOperationException("Not enough unique numbers in range");

            for (int i = 0; i < count; i++)
            {
                if (allowDuplicates)
                {
                    result.Add(_random.Next(from, to + 1));
                }
                else
                {
                    int index = _random.Next(available.Count);
                    result.Add(available[index]);
                    available.RemoveAt(index);
                }
            }

            return result;
        }
    }
}

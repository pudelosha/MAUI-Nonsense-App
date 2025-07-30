using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App.Platforms.Android.Services.Random
{
    public class AndroidDiceRollService : IDiceRollService
    {
        private readonly System.Random _random = new();

        public Task<List<int>> RollAsync(int diceCount)
        {
            var result = new List<int>();
            for (int i = 0; i < diceCount; i++)
                result.Add(_random.Next(1, 7));

            return Task.FromResult(result);
        }
    }
}

using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace MAUI_Nonsense_App.Services
{
    public interface IStepCounterService
    {
        int TotalSteps { get; }
        int Last24HoursSteps { get; }
        long ActiveSecondsToday { get; }

        // Full history from install:
        // Daily totals
        Dictionary<string, int> StepHistory { get; }               // kept for backward compatibility (daily)
        Dictionary<string, int> StepHistoryDaily { get; }          // same as StepHistory
        // Hourly buckets (per day)
        Dictionary<string, int[]> StepHistoryHourly { get; }

        // First day we started recording
        DateTime InstallDate { get; }

        // Convenience helpers
        int[] GetHourlySteps(DateTime localDate);
        IEnumerable<(DateTime WeekStart, int TotalSteps)> EnumerateWeeklyTotals(DayOfWeek weekStart);

        event EventHandler? StepsUpdated;

        Task StartAsync();
        Task StopAsync();

        /// <summary>
        /// Clears only today's counters and re-baselines against the current sensor value
        /// so step counting restarts from 0 immediately.
        /// </summary>
        void ResetToday();

        /// <summary>
        /// Wipes all accumulated data (daily + hourly + counters). InstallDate is preserved.
        /// </summary>
        void ResetAll();

        void RaiseStepsUpdated();
    }
}

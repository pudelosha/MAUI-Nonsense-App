using Android.App;
using Android.Content;
using Android.OS;
using Android.Util;
using Microsoft.Maui.Storage;

namespace MAUI_Nonsense_App.Platforms.Android.Receivers
{
    [BroadcastReceiver(Enabled = true, Exported = true, Name = "com.companyname.mauinonsenseapp.BootReceiver")]
    [IntentFilter(new[] { Intent.ActionBootCompleted })]
    public class BootReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            if (intent == null || intent.Action != Intent.ActionBootCompleted)
                return;

            try
            {
                bool startOnBoot = Preferences.Get("StartServiceOnBoot", true);
                if (!startOnBoot)
                {
                    Log.Info("BootReceiver", "StartServiceOnBoot is disabled. Aborting.");
                    return;
                }

                Log.Info("BootReceiver", "Boot completed. Attempting to start StepCounterForegroundService...");

                var serviceIntent = new Intent(context, typeof(Services.StepCounter.StepCounterForegroundService));
                serviceIntent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ExcludeFromRecents);

                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                    context.StartForegroundService(serviceIntent);
                else
                    context.StartService(serviceIntent);
            }
            catch (Exception ex)
            {
                Log.Error("BootReceiver", $"BootReceiver error: {ex}");
            }
        }
    }
}

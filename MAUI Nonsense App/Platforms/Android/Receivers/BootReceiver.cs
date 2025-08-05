using Android.App;
using Android.Content;
using Android.OS;
using Android.Preferences;
using Microsoft.Maui.Storage;

namespace MAUI_Nonsense_App.Platforms.Android.Receivers
{
    [BroadcastReceiver(Enabled = true, Exported = true)]
    [IntentFilter(new[] { Intent.ActionBootCompleted })]
    public class BootReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
        {
            if (intent?.Action == Intent.ActionBootCompleted)
            {
                // Optional: Reset boot base step value after reboot
                Preferences.Set("BootBaseStepValue", 0);

                // Only proceed on real boot (exclude shutdown broadcasts)
                if (Build.VERSION.SdkInt >= BuildVersionCodes.O)
                {
                    // Start MainActivity to make the app visible
                    Intent startIntent = new Intent(context, typeof(MainActivity));
                    startIntent.AddFlags(ActivityFlags.NewTask | ActivityFlags.ClearTop);
                    context.StartActivity(startIntent);
                }
            }
        }
    }
}

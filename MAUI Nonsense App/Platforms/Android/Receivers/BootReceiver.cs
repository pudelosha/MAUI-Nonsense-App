using Android.App;
using Android.Content;
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
            if (intent.Action == Intent.ActionBootCompleted)
            {
                // Reset boot base step value after reboot
                Preferences.Set("BootBaseStepValue", 0);
            }
        }
    }
}

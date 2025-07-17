#if ANDROID

using System;
using Android.Content;

namespace MAUI_Nonsense_App.Platforms.Android.Helpers
{
    class BroadcastReceiverWrapper : BroadcastReceiver
    {
        private readonly Action _action;

        public BroadcastReceiverWrapper(Action action)
        {
            _action = action;
        }

        public override void OnReceive(Context? context, Intent? intent)
        {
            _action();
        }
    }
}

#endif

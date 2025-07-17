using Android.Media;
using MAUI_Nonsense_App.Services;
using AApp = Android.App.Application;

namespace MAUI_Nonsense_App.Platforms.Android.Services.AlarmSound
{
    public class AndroidAlarmSoundService : IAlarmSoundService
    {
        private MediaPlayer? _mediaPlayer;

        public void PlayLooping()
        {
            Stop();

            var context = AApp.Context!;
            var alarmUri = RingtoneManager.GetDefaultUri(RingtoneType.Alarm);

            _mediaPlayer = new MediaPlayer();
            _mediaPlayer.SetDataSource(context, alarmUri);
            _mediaPlayer.Looping = true;
            _mediaPlayer.Prepare();
            _mediaPlayer.Start();
        }

        public void Stop()
        {
            if (_mediaPlayer != null)
            {
                _mediaPlayer.Stop();
                _mediaPlayer.Release();
                _mediaPlayer.Dispose();
                _mediaPlayer = null;
            }
        }
    }
}

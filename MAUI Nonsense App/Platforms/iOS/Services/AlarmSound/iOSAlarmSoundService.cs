using AVFoundation;
using Foundation;
using MAUI_Nonsense_App.Services;

namespace MAUI_Nonsense_App.Platforms.iOS.Services.AlarmSound
{
    public class iOSAlarmSoundService : IAlarmSoundService
    {
        private AVAudioPlayer? _player;

        public void PlayLooping()
        {
            Stop();

            var url = NSUrl.FromFilename("alarm.caf");
            NSError error;
            _player = new AVAudioPlayer(url, "caf", out error);
            if (_player != null && error == null)
            {
                _player.NumberOfLoops = -1; // infinite
                _player.Play();
            }
        }

        public void Stop()
        {
            if (_player != null)
            {
                _player.Stop();
                _player.Dispose();
                _player = null;
            }
        }
    }
}

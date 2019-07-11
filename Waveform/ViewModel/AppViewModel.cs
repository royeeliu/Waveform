using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Waveform.Common;
using Waveform.Models;

namespace Waveform.ViewModel
{
    class AppViewModel : BindableBase
    {
        AudioPlayer audioPlayer;

        public AppViewModel(AudioPlayer audioPlayer)
        {
            this.audioPlayer = audioPlayer;
        }
    }
}

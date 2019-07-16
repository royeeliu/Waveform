using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Media;
using Windows.Media.MediaProperties;

namespace AudioEffects
{
    public interface IWaveformRenderer
    {
        void SetEncodingProperties(AudioEncodingProperties encodingProperties);
        void Render(AudioFrame frame);
    }
}

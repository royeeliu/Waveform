using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Media;
using Windows.Media.Effects;
using Windows.Media.MediaProperties;

namespace AudioEffects
{
    public sealed class WaveformBridge: IBasicAudioEffect
    {
        private AudioEncodingProperties currentEncodingProperties;
        private List<AudioEncodingProperties> supportedEncodingProperties;
        private IPropertySet propertySet;
        private IWaveformRenderer renderer;

        public WaveformBridge()
        {
            // Support 44.1kHz and 48kHz mono float
            supportedEncodingProperties = new List<AudioEncodingProperties>();
            AudioEncodingProperties encodingProps1 = AudioEncodingProperties.CreatePcm(44100, 1, 32);
            encodingProps1.Subtype = MediaEncodingSubtypes.Float;
            AudioEncodingProperties encodingProps2 = AudioEncodingProperties.CreatePcm(48000, 1, 32);
            encodingProps2.Subtype = MediaEncodingSubtypes.Float;

            supportedEncodingProperties.Add(encodingProps1);
            supportedEncodingProperties.Add(encodingProps2);
        }

        public void SetEncodingProperties(AudioEncodingProperties encodingProperties)
        {
            currentEncodingProperties = encodingProperties;
            renderer?.SetEncodingProperties(encodingProperties);
        }

        unsafe public void ProcessFrame(ProcessAudioFrameContext context)
        {
            AudioFrame inputFrame = context.InputFrame;
            renderer?.Render(inputFrame);
        }

        public void Close(MediaEffectClosedReason reason)
        {
        }

        public void DiscardQueuedFrames()
        {
        }

        public IReadOnlyList<AudioEncodingProperties> SupportedEncodingProperties { get => supportedEncodingProperties; }

        public bool UseInputFrameForOutput { get => true; }

        public void SetProperties(IPropertySet configuration)
        {
            this.propertySet = configuration;

            object val;
            if (configuration != null && configuration.TryGetValue(typeof(IWaveformRenderer).FullName, out val))
            {
                this.renderer = val as IWaveformRenderer;
            }
        }
    }
}

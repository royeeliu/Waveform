using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Media.Effects;

namespace AudioEffects
{
    public sealed class WaveformBridgeDefinition : IAudioEffectDefinition
    {
        PropertySet properties = new PropertySet();
        IWaveformRenderer renderer;

        public string ActivatableClassId { get => typeof(WaveformBridge).FullName; }
        public IPropertySet Properties { get => properties; }

        public IWaveformRenderer Renderer
        {
            get => renderer;
            set
            {
                renderer = value;
                properties[typeof(IWaveformRenderer).FullName] = renderer;
            }
        }
    }
}

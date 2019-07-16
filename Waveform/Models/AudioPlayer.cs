using AudioEffects;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation.Collections;
using Windows.Media.Audio;
using Windows.Media.Effects;
using Windows.Media.Render;
using Windows.Storage;

namespace Waveform.Models
{
    class AudioPlayer : IDisposable
    {
        private AudioGraph graph;
        private AudioFileInputNode fileInputNode;
        private AudioDeviceOutputNode deviceOutput;

        public AudioPlayer()
        {
        }

        public void Dispose()
        {
            WaveformRenderer = null;
            graph?.Dispose();
            fileInputNode?.Dispose();
            deviceOutput?.Dispose();
        }

        public async Task InitializeAsync()
        {
            await CreateAudioGraph();
        }

        public IWaveformRenderer WaveformRenderer { get; set; }

        public bool IsPaused { get; private set; }

        public async Task LoadFileAsync(StorageFile file)
        {
            CreateAudioFileInputNodeResult fileInputResult = await graph.CreateFileInputNodeAsync(file);
            if (fileInputResult.Status != AudioFileNodeCreationStatus.Success)
            {
                // Cannot read input file
                //rootPage.NotifyUser(String.Format("Cannot read input file because {0}", fileInputResult.Status.ToString()), NotifyType.ErrorMessage);
                return;
            }

            fileInputNode = fileInputResult.FileInputNode;

            if (fileInputNode.Duration <= TimeSpan.FromSeconds(3))
            {
                // Imported file is too short
                //rootPage.NotifyUser("Please pick an audio file which is longer than 3 seconds", NotifyType.ErrorMessage);
                fileInputNode.Dispose();
                fileInputNode = null;
                return;
            }

            fileInputNode.AddOutgoingConnection(deviceOutput);
            //fileButton.Background = new SolidColorBrush(Colors.Green);

            // Trim the file: set the start time to 3 seconds from the beginning
            // fileInput.EndTime can be used to trim from the end of file
            //fileInput.StartTime = TimeSpan.FromSeconds(3);

            // Enable buttons in UI to start graph, loop and change playback speed factor
            //graphButton.IsEnabled = true;
            //loopToggle.IsEnabled = true;
            //playSpeedSlider.IsEnabled = true;

            if (WaveformRenderer != null)
            {
                WaveformBridgeDefinition definition = new WaveformBridgeDefinition();
                definition.Renderer = WaveformRenderer;
                fileInputNode.EffectDefinitions.Add(definition);
            }

            IsPaused = true;
        }

        private async Task CreateAudioGraph()
        {
            // Create an AudioGraph with default settings
            AudioGraphSettings settings = new AudioGraphSettings(AudioRenderCategory.Media);
            CreateAudioGraphResult result = await AudioGraph.CreateAsync(settings);

            if (result.Status != AudioGraphCreationStatus.Success)
            {
                // Cannot create graph
                //rootPage.NotifyUser(String.Format("AudioGraph Creation Error because {0}", result.Status.ToString()), NotifyType.ErrorMessage);
                return;
            }

            graph = result.Graph;

            // Create a device output node
            CreateAudioDeviceOutputNodeResult deviceOutputNodeResult = await graph.CreateDeviceOutputNodeAsync();

            if (deviceOutputNodeResult.Status != AudioDeviceNodeCreationStatus.Success)
            {
                // Cannot create device output node
                //rootPage.NotifyUser(String.Format("Device Output unavailable because {0}", deviceOutputNodeResult.Status.ToString()), NotifyType.ErrorMessage);
                //speakerContainer.Background = new SolidColorBrush(Colors.Red);
                return;
            }

            deviceOutput = deviceOutputNodeResult.DeviceOutputNode;
            //rootPage.NotifyUser("Device Output Node successfully created", NotifyType.StatusMessage);
            //speakerContainer.Background = new SolidColorBrush(Colors.Green);
        }

        public void Play()
        {
            graph.Start();
            IsPaused = false;
        }

        public void Pause()
        {
            graph.Stop();
            IsPaused = true;
        }
    }
}

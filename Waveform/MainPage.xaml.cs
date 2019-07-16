using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Waveform.Models;
using Waveform.ViewModel;
using Windows.Foundation;
using Windows.Foundation.Collections;
using Windows.Storage;
using Windows.Storage.Pickers;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;
using Windows.UI.Xaml.Controls.Primitives;
using Windows.UI.Xaml.Data;
using Windows.UI.Xaml.Input;
using Windows.UI.Xaml.Media;
using Windows.UI.Xaml.Navigation;

// https://go.microsoft.com/fwlink/?LinkId=402352&clcid=0x804 上介绍了“空白页”项模板

namespace Waveform
{
    /// <summary>
    /// 可用于自身或导航至 Frame 内部的空白页。
    /// </summary>
    public sealed partial class MainPage : Page
    {
        AudioPlayer audioPlayer = new AudioPlayer();
        WaveformRenderer waveformRenderer;
        AppViewModel viewModel;

        public MainPage()
        {
            this.InitializeComponent();

            waveformRenderer = new WaveformRenderer(this.Dispatcher);
            audioPlayer.WaveformRenderer = waveformRenderer;
            viewModel = new AppViewModel(audioPlayer);

            waveformBorlder.SizeChanged += WaveformBorlder_SizeChanged;
            waveformImage.SizeChanged += WaveformImage_SizeChanged;
        }

        private void WaveformImage_SizeChanged(object sender, SizeChangedEventArgs e)
        {
        }

        private void WaveformBorlder_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            int widht = (int)(waveformBorlder.ActualWidth + 0.5);
            int height = (int)(waveformBorlder.ActualHeight + 0.5);
            waveformRenderer.UpdateTargetSize(widht, height);
            waveformImage.Source = waveformRenderer.ImageSource;
        }

        protected override async void OnNavigatedTo(NavigationEventArgs e)
        {
            base.OnNavigatedTo(e);
            await audioPlayer.InitializeAsync();
        }

        protected override void OnNavigatedFrom(NavigationEventArgs e)
        {
            audioPlayer?.Dispose();
            base.OnNavigatedFrom(e);
        }

        private async void FileButton_Click(object sender, RoutedEventArgs e)
        {
            FileOpenPicker filePicker = new FileOpenPicker();
            filePicker.SuggestedStartLocation = PickerLocationId.MusicLibrary;
            filePicker.FileTypeFilter.Add(".mp3");
            filePicker.FileTypeFilter.Add(".wav");
            filePicker.FileTypeFilter.Add(".wma");
            filePicker.FileTypeFilter.Add(".m4a");
            filePicker.ViewMode = PickerViewMode.Thumbnail;
            StorageFile file = await filePicker.PickSingleFileAsync();

            // File can be null if cancel is hit in the file picker
            if (file == null)
            {
                return;
            }

            await audioPlayer.LoadFileAsync(file);
            TogglePlay();
            audioPlayer.Play();
        }

        private void PauseButton_Click(object sender, RoutedEventArgs e)
        {
            TogglePlay();
        }

        private void TogglePlay()
        {
            if (audioPlayer.IsPaused)
            {
                audioPlayer.Play();
                pauseButton.Content = "Pause";
            }
            else
            {
                audioPlayer.Pause();
                pauseButton.Content = "Play";
            }
        }
    }
}

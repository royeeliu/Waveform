using AudioEffects;
using System;
using System.Runtime.InteropServices;
using SharpDX;
using SharpDX.Direct2D1;
using SharpDX.Direct3D;
using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using Windows.UI.Xaml.Media.Imaging;
using Windows.Media;
using Windows.Foundation;
using Windows.UI.Xaml;
using Device = SharpDX.Direct3D11.Device;
using FeatureLevel = SharpDX.Direct3D.FeatureLevel;
using D2D = SharpDX.Direct2D1;
using D3D11 = SharpDX.Direct3D11;
using Windows.UI;
using Windows.UI.Core;
using Windows.Media.MediaProperties;

namespace Waveform.Models
{
    // Using the COM interface IMemoryBufferByteAccess allows us to access the underlying byte array in an AudioFrame
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    class WaveformRenderer : IWaveformRenderer
    {
        CoreDispatcher dispatcher;
        SurfaceImageSource imageSource;
        private D3D11.Device d3dDevice;
        private D2D.Device d2dDevice;
        private D2D.DeviceContext d2dContext;
        object dataLock = new object();
        float[] waveData;
        int targetWidth = 0;
        int targetHeight = 0;
        AudioEncodingProperties encodingProperties;

        public WaveformRenderer(CoreDispatcher dispatcher)
        {
            this.dispatcher = dispatcher;

            CreateDeviceResources();
            Application.Current.Suspending += OnSuspending;
        }

        public SurfaceImageSource ImageSource { get => imageSource; }

        public void UpdateTargetSize(int width, int height)
        {
            targetWidth = width;
            targetHeight = height;
            imageSource = new SurfaceImageSource(width, height);
            // Query for ISurfaceImageSourceNative interface.
            using (var dxgiDevice = d3dDevice.QueryInterface<SharpDX.DXGI.Device>())
            using (var sisNative = ComObject.QueryInterface<ISurfaceImageSourceNative>(imageSource))
            {
                sisNative.Device = dxgiDevice;
            }

            UpdateImage();
        }

        private void UpdateImage()
        {
            BeginDraw();
            Clear(Colors.Black);
            DrawWaveData();
            DrawBaseLine();
            EndDraw();
        }

        public void SetEncodingProperties(AudioEncodingProperties encodingProperties)
        {
            this.encodingProperties = encodingProperties;
        }

        unsafe public void Render(AudioFrame frame)
        {
            using (AudioBuffer inputBuffer = frame.LockBuffer(AudioBufferAccessMode.Read))
            using (IMemoryBufferReference inputReference = inputBuffer.CreateReference())
            {
                byte* inputDataInBytes;
                uint inputCapacity;

                ((IMemoryBufferByteAccess)inputReference).GetBuffer(out inputDataInBytes, out inputCapacity);

                int dataInFloatLength = (int)inputBuffer.Length / sizeof(float);

                float[] waveData = new float[dataInFloatLength];
                Marshal.Copy((IntPtr)inputDataInBytes, waveData, 0, dataInFloatLength);

                lock(dataLock)
                {
                    this.waveData = waveData;
                }

                Invoke(UpdateImage);
            } 
        }

        private async void Invoke(Action action)
        {
            await dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
        }

        private void DrawWaveData()
        {
            float[] waveData;
            lock (dataLock)
            {
                waveData = this.waveData;
            }

            if (waveData == null)
            {
                return;
            }

            // Create a solid color D2D brush.
            using (var brush = new SolidColorBrush(d2dContext, ConvertToColorF(Colors.Green)))
            {
                int channels = (int)encodingProperties.ChannelCount;
                int count = Math.Min(waveData.Length, targetWidth) / channels;
                int halfHeight = targetHeight / (channels * 2);

                for (int i = 0; i < count; i++)
                {
                    for (int j = 0; j < channels; j++)
                    {
                        int x = i * channels + j;
                        int Y0 = (j * 2 + 1) * halfHeight;
                        float Y1 = waveData[x] * halfHeight + Y0;
                        RawVector2 point0 = new RawVector2(x, Y0);
                        RawVector2 point1 = new RawVector2(x, Y1);
                        d2dContext.DrawLine(point0, point1, brush);
                    }
                }
            }
       }

        private void DrawBaseLine()
        {
            if (encodingProperties == null)
            {
                return;
            }

            using (var brush = new SolidColorBrush(d2dContext, ConvertToColorF(Colors.Red)))
            {
                int channels = (int)encodingProperties.ChannelCount;
                int halfHeight = targetHeight / (channels * 2);

                for (int i = 0; i < channels; i++)
                {
                    int Y = (i * 2 + 1) * halfHeight;
                    RawVector2 point0 = new RawVector2(0.0f, Y);
                    RawVector2 point1 = new RawVector2((float)targetWidth, Y);
                    d2dContext.DrawLine(point0, point1, brush);
                }
            }
        }

        // Initialize hardware-dependent resources.
        private void CreateDeviceResources()
        {
            // Unlike the original C++ sample, we don't have smart pointers so we need to
            // dispose Direct3D objects explicitly
            Utilities.Dispose(ref d3dDevice);
            Utilities.Dispose(ref d2dDevice);
            Utilities.Dispose(ref d2dContext);

            // This flag adds support for surfaces with a different color channel ordering
            // than the API default. It is required for compatibility with Direct2D.
            var creationFlags = DeviceCreationFlags.BgraSupport;

#if DEBUG
            // If the project is in a debug build, enable debugging via SDK Layers.
            creationFlags |= DeviceCreationFlags.Debug;
#endif

            // This array defines the set of DirectX hardware feature levels this app will support.
            // Note the ordering should be preserved.
            // Don't forget to declare your application's minimum required feature level in its
            // description.  All applications are assumed to support 9.1 unless otherwise stated.
            FeatureLevel[] featureLevels =
            {
                FeatureLevel.Level_11_1,
                FeatureLevel.Level_11_0,
                FeatureLevel.Level_10_1,
                FeatureLevel.Level_10_0,
                FeatureLevel.Level_9_3,
                FeatureLevel.Level_9_2,
                FeatureLevel.Level_9_1,
            };

            // Create the Direct3D 11 API device object.
            d3dDevice = new Device(DriverType.Hardware, creationFlags, featureLevels);

            // Get the Direct3D 11.1 API device.
            using (var dxgiDevice = d3dDevice.QueryInterface<SharpDX.DXGI.Device>())
            {
                // Create the Direct2D device object and a corresponding context.
                d2dDevice = new SharpDX.Direct2D1.Device(dxgiDevice);

                d2dContext = new SharpDX.Direct2D1.DeviceContext(d2dDevice, DeviceContextOptions.None);

                // Query for ISurfaceImageSourceNative interface.
                //using (var sisNative = ComObject.QueryInterface<ISurfaceImageSourceNative>(imageSource))
                    //sisNative.Device = dxgiDevice;
            }
        }

        public void BeginDraw()
        {
            BeginDraw(new Windows.Foundation.Rect(0, 0, targetWidth, targetHeight));
        }

        public void BeginDraw(Windows.Foundation.Rect updateRect)
        {
            // Express target area as a native RECT type.
            var updateRectNative = new RawRectangle
            {
                Left = (int)updateRect.Left,
                Top = (int)updateRect.Top,
                Right = (int)updateRect.Right,
                Bottom = (int)updateRect.Bottom
            };


            // Query for ISurfaceImageSourceNative interface.
            using (var sisNative = ComObject.QueryInterface<ISurfaceImageSourceNative>(imageSource))
            {
                // Begin drawing - returns a target surface and an offset to use as the top left origin when drawing.
                try
                {
                    RawPoint offset;
                    using (var surface = sisNative.BeginDraw(updateRectNative, out offset))
                    {

                        // Create render target.
                        using (var bitmap = new Bitmap1(d2dContext, surface))
                        {
                            // Set context's render target.
                            d2dContext.Target = bitmap;
                        }

                        // Begin drawing using D2D context.
                        d2dContext.BeginDraw();

                        // Apply a clip and transform to constrain updates to the target update area.
                        // This is required to ensure coordinates within the target surface remain
                        // consistent by taking into account the offset returned by BeginDraw, and
                        // can also improve performance by optimizing the area that is drawn by D2D.
                        // Apps should always account for the offset output parameter returned by 
                        // BeginDraw, since it may not match the passed updateRect input parameter's location.
                        d2dContext.PushAxisAlignedClip(
                            new RawRectangleF(
                                (offset.X),
                                (offset.Y),
                                (offset.X + (float)updateRect.Width),
                                (offset.Y + (float)updateRect.Height)
                                ),
                            AntialiasMode.Aliased
                            );

                        //d2dContext.Transform = RawMatrix3x2.Translation(offset.X, offset.Y);
                    }
                }
                catch (SharpDXException ex)
                {
                    if (ex.ResultCode == SharpDX.DXGI.ResultCode.DeviceRemoved ||
                        ex.ResultCode == SharpDX.DXGI.ResultCode.DeviceReset)
                    {
                        // If the device has been removed or reset, attempt to recreate it and continue drawing.
                        CreateDeviceResources();
                        BeginDraw(updateRect);
                    }
                    else
                    {
                        throw;
                    }
                }
            }
        }

        public void EndDraw()
        {
            // Remove the transform and clip applied in BeginDraw since
            // the target area can change on every update.
           // d2dContext.Transform = Matrix3x2.Identity;
            d2dContext.PopAxisAlignedClip();

            // Remove the render target and end drawing.
            d2dContext.EndDraw();

            d2dContext.Target = null;

            // Query for ISurfaceImageSourceNative interface.
            using (var sisNative = ComObject.QueryInterface<ISurfaceImageSourceNative>(imageSource))
                sisNative.EndDraw();
        }

        public void Clear(Windows.UI.Color color)
        {
            d2dContext.Clear(ConvertToColorF(color));
        }

        public void FillSolidRect(Windows.UI.Color color, Windows.Foundation.Rect rect)
        {
            // Create a solid color D2D brush.
            using (var brush = new SolidColorBrush(d2dContext, ConvertToColorF(color)))
            {
                // Draw a filled rectangle.
                d2dContext.FillRectangle(ConvertToRectF(rect), brush);
            }
        }

        private void OnSuspending(object sender, Windows.ApplicationModel.SuspendingEventArgs e)
        {
            // Hints to the driver that the app is entering an idle state and that its memory can be used temporarily for other apps.
            using (var dxgiDevice = d3dDevice.QueryInterface<SharpDX.DXGI.Device3>())
                dxgiDevice.Trim();
        }

        private static RawColor4 ConvertToColorF(Windows.UI.Color color)
        {
            return new RawColor4(color.R, color.G, color.B, color.A);
        }

        private static RawRectangleF ConvertToRectF(Windows.Foundation.Rect rect)
        {
            return new RawRectangleF((float)rect.X, (float)rect.Y, (float)rect.Width, (float)rect.Height);
        }
    }
}

using Microsoft.AspNetCore.SignalR.Client;
using System;
using System.Collections.Generic;
using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Threading;

namespace ScreenShareWPF
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private HubConnection _connection;
        WriteableBitmap writeableBitmap;

        public class ClientInfo
        {
            public string ConnectionId { get; set; }
            public string Name { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();
            InitializeSignalRConnection();
        }

        private async void InitializeSignalRConnection()
        {

            // Initialize the SignalR connection
            _connection = new HubConnectionBuilder()
                .WithUrl("https://localhost:7170/clientHub") // Replace with your hub URL
                .Build();

            //Initialize the SignalR connection
            //_connection = new HubConnectionBuilder()
            //    .WithUrl("http://192.168.2.81:7095/clientHub") // Replace with your hub URL
            //    .Build();

            // Handle incoming messages
            _connection.On<string, string>("ReceiveMessage", (clientName, message) =>
            {
                // Update the UI with the received message
                Dispatcher.Invoke(() =>
                {
                    MessagesList.Items.Add($"{clientName}: {message}");
                });
            });

            _connection.On<string>("ReceiveScreen", (imageData) =>
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        byte[] imageBytes = Convert.FromBase64String(imageData);

                        using (var stream = new MemoryStream(imageBytes))
                        {
                            var decoder = BitmapDecoder.Create(stream, BitmapCreateOptions.None, BitmapCacheOption.OnLoad);
                            var bitmapFrame = decoder.Frames[0];

                            if (writeableBitmap == null ||
                                writeableBitmap.PixelWidth != bitmapFrame.PixelWidth ||
                                writeableBitmap.PixelHeight != bitmapFrame.PixelHeight)
                            {
                                writeableBitmap = new WriteableBitmap(
                                    bitmapFrame.PixelWidth, bitmapFrame.PixelHeight,
                                    bitmapFrame.DpiX, bitmapFrame.DpiY,
                                    bitmapFrame.Format, null
                                );
                                SharedScreenImage.Source = writeableBitmap;
                            }

                            writeableBitmap.Lock();
                            bitmapFrame.CopyPixels(
                                new Int32Rect(0, 0, bitmapFrame.PixelWidth, bitmapFrame.PixelHeight),
                                writeableBitmap.BackBuffer,
                                writeableBitmap.BackBufferStride * bitmapFrame.PixelHeight,
                                writeableBitmap.BackBufferStride
                            );
                            writeableBitmap.AddDirtyRect(new Int32Rect(0, 0, bitmapFrame.PixelWidth, bitmapFrame.PixelHeight));
                            writeableBitmap.Unlock();
                        }
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error displaying received frame: " + ex.Message);
                    }
                }, DispatcherPriority.Render); // Render priority for smoother updates
            });

            _connection.On<string>("ReceiveAudio", (base64Audio) =>
            {
                Dispatcher.Invoke(() =>
                {
                    try
                    {
                        byte[] audioBytes = Convert.FromBase64String(base64Audio);
                        string tempFile = Path.Combine(Path.GetTempPath(), "tempAudio.webm");

                        // Write the audio data to a temporary file
                        File.WriteAllBytes(tempFile, audioBytes);

                        var player = new MediaPlayer();
                        player.Open(new Uri(tempFile, UriKind.Absolute));
                        player.Play();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine("Error playing audio: " + ex.Message);
                    }
                });
            });

            try
            {
                // Start the connection
                await _connection.StartAsync();
                //MessageBox.Show("SignalR connection established.");
            }
            catch (Exception ex)
            {
                MessageBox.Show($"SignalR connection failed: {ex.Message}");
            }
        }
    }
}


using Microsoft.Win32;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;
using System.Text.Json;
using System.Collections.ObjectModel;
using System.Windows.Controls;
using System.Xml;
using AForge.Video;
using AForge.Video.DirectShow;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;

namespace Project_AI
{
    public partial class MainWindow : Window
    {
        public class Item
        {
            public string Name { get; set; }
            public string Kcal { get; set; }
            public string Describe { get; set; }
            public string ImagePath { get; set; }
        }
        private FilterInfoCollection videoDevices; // Khai báo biến để lưu thông tin về các thiết bị video
        private VideoCaptureDevice videoSource; // Khai báo biến để thực hiện việc capture video
        private BitmapImage currentFrame;
        public MainWindow()
        {
            InitializeComponent();
            InitializeWebcam(); // Gọi hàm khởi tạo webcam khi khởi động ứng dụng
        }

        private void InitializeWebcam()
        {
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            if (videoDevices.Count > 0)
            {
                videoSource = new VideoCaptureDevice(videoDevices[0].MonikerString);
                videoSource.NewFrame += new NewFrameEventHandler(VideoCaptureDevice_NewFrame);
                videoSource.Start();
            }
            else
            {
                MessageBox.Show("No webcam found.");
            }
        }

        private void VideoCaptureDevice_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone();
            currentFrame = ConvertToBitmapImage(bitmap);

            // Update the UI on the UI thread
            Dispatcher.Invoke(() => WebcamImage.Source = currentFrame);

        }
        private BitmapImage ConvertToBitmapImage(Bitmap bitmap)
        {
            using (MemoryStream memoryStream = new MemoryStream())
            {
                bitmap.Save(memoryStream, ImageFormat.Bmp);
                memoryStream.Position = 0;

                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.StreamSource = memoryStream;
                bitmapImage.EndInit();
                bitmapImage.Freeze(); // Freeze the BitmapImage to avoid threading issues

                return bitmapImage;
            }
        }
        // Xử lý sự kiện khi ứng dụng đóng
        protected override void OnClosing(CancelEventArgs e)
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                // Dừng stream từ webcam khi ứng dụng đóng
                videoSource.SignalToStop();
                videoSource.WaitForStop();
            }
            base.OnClosing(e);
        }

        private async void CaptureButton_Click(object sender, RoutedEventArgs e)
        {

            if (currentFrame != null)
            {
                if (videoSource != null && videoSource.IsRunning)
                {
                    videoSource.SignalToStop();
                }
                // Save the current frame to a file (you can customize the file name and format)
                Microsoft.Win32.SaveFileDialog saveDialog = new Microsoft.Win32.SaveFileDialog();
                saveDialog.Filter = "JPEG Image|*.jpg|PNG Image|*.png";
                if (saveDialog.ShowDialog() == true)
                {
                    BitmapEncoder encoder = (saveDialog.FilterIndex == 1) ? new JpegBitmapEncoder() : new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(currentFrame));

                    using (FileStream fileStream = new FileStream(saveDialog.FileName, FileMode.Create))
                    {
                        encoder.Save(fileStream);
                    }
                }


                // Hiển thị thông tin "Loading" tương tự như khi tải ảnh lên
                NameLabel.Content = "Loading...";
                KcalLabel.Content = "Loading...";
                DescribeLabel.Content = "Loading...";

  
                // Gọi hàm CallGeminiAPI với đường dẫn của ảnh đã chụp
                await CallGeminiAPI(saveDialog.FileName);
                
            }
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg;*.jpeg;*.png) | *.jpg;*.jpeg;*.png";
            if (openFileDialog.ShowDialog() == true)
            {
                string imagePath = openFileDialog.FileName;
                NameLabel.Content = "Loading...";
                KcalLabel.Content = "Loading...";
                DescribeLabel.Content = "Loading...";
                await CallGeminiAPI(imagePath);
            }
        }

        private async Task CallGeminiAPI(string imagePath)
        {
            string prompt = "show me a name of food or drink, so that so me a kcal food .repose me style example {name:\"Cake\", kcal : 500 ,describe:\"Cake with cherry\"}";
            string endpointUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro-vision:generateContent?key=AIzaSyD3J41xmlxjIMGUljYo8yUtKK_MC87i5u4";

            try
            {
                // Convert image to base64
                byte[] imageBytes = File.ReadAllBytes(imagePath);
                string imageBase64 = Convert.ToBase64String(imageBytes);

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

                    //var content = new StringContent($"{{\"contents\":[{{\"parts\":[{{\"text\":\"{prompt}\"}},{{\"inline_data\":{{\"mime_type\":\"image/jpeg\",\"data\":\"{imageBase64}\"}}}}]}}]}}", Encoding.UTF8, "application/json");
                    var content = new StringContent($"{{\"contents\":\r\n    [\r\n        {{\"parts\":\r\n        [\r\n            {{\"text\":\"show me a name of food or drink, so that so me a kcal food .repose me style example {{\\\"name\\\":\\\"Cake\\\", \\\"kcal\\\" : 500,\\\"describe\\\":\\\"Cake with cherry\\\"}}\"}},\r\n            {{\"inline_data\": \r\n            {{\r\n                \r\n            \"mime_type\" : \"image/jpeg\",\r\n            \"data\": \"{imageBase64}\"\r\n             }}\r\n            \r\n            }}\r\n        ]\r\n        }}\r\n    ]\r\n}}");
                    var response = await client.PostAsync(endpointUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonString = await response.Content.ReadAsStringAsync();
                        // Process the JSON response (implementation omitted for brevity)
                        var data = JsonSerializer.Deserialize<GeminiResponse>(jsonString);
                        if (data?.candidates?.Any() == true)
                        {
                            var firstCandidate = data.candidates[0];
                            if (firstCandidate.content?.parts?.Any() == true)
                            {
                                string text = firstCandidate.content.parts[0].text.Trim(); // Remove extra characters
                                text = text.Replace("'", "\"");
                                // var document = JsonSerializer.Deserialize<Food>(text);
                                Food foodItem = JsonSerializer.Deserialize<Food>(text);
                                WebcamImage.Source = new BitmapImage(new Uri(imagePath));

                                NameLabel.Content = "Food name: " + foodItem.name;
                                KcalLabel.Content = "Kcal : " + foodItem.kcal + "KCal";
                                DescribeLabel.Content = "Describe : " + foodItem.describe;

                                //Items.Add(new Item {Describe = "Describe : " + foodItem.describe,
                                //                    ImagePath = imagePath,
                                //                    Kcal = "Kcal : " + foodItem.kcal + "KCal",
                                //                    Name = "Food name: " + foodItem.name
                                //});
                                // Check if it's a valid food or drink
                                if (string.IsNullOrWhiteSpace(foodItem.name) || foodItem.kcal <= 0 || string.IsNullOrWhiteSpace(foodItem.describe))
                                {
                                    MessageBox.Show("Invalid food or drink");
                                }
                                // Set color for KcalLabel based on calorie level
                                SetKcalLabelColor(foodItem.kcal);
                            }
                            else
                            {
                                Console.WriteLine("Content or parts missing in the response");
                            }
                        }
                        else
                        {
                            Console.WriteLine("Empty response or invalid structure");
                        }
                    }
                    else
                    {
                        string errorMessage = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Error calling Gemini API: {errorMessage}");
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error calling Gemini API: {ex.Message}");
            }
        }
        private void SetKcalLabelColor(int kcal)
        {
            // Define threshold values for calorie levels
            int highThreshold = 300; // Example value, adjust as needed
            int mediumThreshold = 200; // Example value, adjust as needed

            // Set color based on calorie level
            if (kcal > highThreshold)
            {
                // High calorie (red)
                KcalLabel.Foreground = System.Windows.Media.Brushes.Red;
            }
            else if (kcal > mediumThreshold)
            {
                // Medium calorie (orange)
                KcalLabel.Foreground = System.Windows.Media.Brushes.Orange;
            }
            else
            {
                // Low calorie (green)
                KcalLabel.Foreground = System.Windows.Media.Brushes.Green;
            }
        }

        private void ReloadButton_Click(object sender, RoutedEventArgs e)
        {
            // Bắt đầu lại việc capture ảnh từ webcam
            InitializeWebcam();

            // Đặt lại thông tin "Loading"
            NameLabel.Content = "Loading...";
            KcalLabel.Content = "Loading...";
            DescribeLabel.Content = "Loading...";
        }

        private async void ChooseImageButton_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg;*.jpeg;*.png) | *.jpg;*.jpeg;*.png";
            if (openFileDialog.ShowDialog() == true)
            {
                string imagePath = openFileDialog.FileName;

                // Load ảnh từ đường dẫn được chọn
                BitmapImage bitmap = new BitmapImage(new Uri(imagePath));

                // Gán ảnh cho đối tượng WebcamImage
                WebcamImage.Source = bitmap;

                // Hiển thị thông tin "Loading"
                NameLabel.Content = "Loading...";
                KcalLabel.Content = "Loading...";
                DescribeLabel.Content = "Loading...";

                // Gọi hàm CallGeminiAPI để xử lý ảnh đã chọn
                await CallGeminiAPI(imagePath);
            }
        }
    }


    public class GeminiResponse
    {
        public List<Candidate> candidates { get; set; }
        public PromptFeedback promptFeedback { get; set; }
    }

    public class Candidate
    {
        public Content content { get; set; }
        public string finishReason { get; set; }
        public int index { get; set; }
        public List<SafetyRating> safetyRatings { get; set; }
    }

    public class Content
    {
        public List<Part> parts { get; set; }
        public string role { get; set; }
    }

    public class Part
    {
        public string text { get; set; }
    }
    public class Food
    {
        public string name { get; set; }
        public int kcal { get; set; }
        public string describe { get; set; }
    }
    public class SafetyRating
    {
        public string category { get; set; }
        public string probability { get; set; }
    }
}

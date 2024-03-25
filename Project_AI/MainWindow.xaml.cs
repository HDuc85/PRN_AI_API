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
                videoSource.NewFrame += new NewFrameEventHandler(videoSource_NewFrame);
                videoSource.Start();
            }
            else
            {
                MessageBox.Show("No webcam found.");
            }
        }

        private void videoSource_NewFrame(object sender, NewFrameEventArgs eventArgs)
        {
            Bitmap bitmap = (Bitmap)eventArgs.Frame.Clone();
            Dispatcher.BeginInvoke(new ThreadStart(delegate
            {
                WebcamImage.Source = ConvertBitmap(bitmap);
            }));
        }

        private BitmapImage ConvertBitmap(Bitmap bitmap)
        {
            using (MemoryStream memory = new MemoryStream())
            {
                bitmap.Save(memory, System.Drawing.Imaging.ImageFormat.Bmp);
                memory.Position = 0;
                BitmapImage bitmapImage = new BitmapImage();
                bitmapImage.BeginInit();
                bitmapImage.StreamSource = memory;
                bitmapImage.CacheOption = BitmapCacheOption.OnLoad;
                bitmapImage.EndInit();
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

        private void CaptureButton_Click(object sender, RoutedEventArgs e)
        {
            if (videoSource != null && videoSource.IsRunning)
            {
                videoSource.SignalToStop();
            }

            // Tạo tên mới cho ảnh dựa trên thời gian hiện tại
            string imageName = DateTime.Now.ToString("yyyyMMddHHmmss") + ".jpg";
            string imagePath = Path.Combine(@"C:\Users\admin\User\Desktop\ky7\PRN221\PRN_AI_API\image", imageName);

            // Lưu ảnh từ webcam vào đường dẫn đã tạo
            BitmapSource bitmapSource = (BitmapSource)WebcamImage.Source;
            using (var fileStream = new FileStream(imagePath, FileMode.Create))
            {
                BitmapEncoder encoder = new PngBitmapEncoder();
                encoder.Frames.Add(BitmapFrame.Create(bitmapSource));
                encoder.Save(fileStream);
            }

            // Hiển thị thông tin "Loading" tương tự như khi tải ảnh lên
            NameLabel.Content = "Loading...";
            KcalLabel.Content = "Loading...";
            DescribeLabel.Content = "Loading...";

            // Gọi hàm CallGeminiAPI với đường dẫn của ảnh đã chụp
            CallGeminiAPI(imagePath);
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

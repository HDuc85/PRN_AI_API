using Microsoft.Win32;
using System;
using System.IO;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Media.Imaging;

namespace Project_AI
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private async void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg;*.jpeg;*.png) | *.jpg;*.jpeg;*.png";
            if (openFileDialog.ShowDialog() == true)
            {
                Picture.Source = new BitmapImage(new Uri(openFileDialog.FileName));
                string imagePath = openFileDialog.FileName;

                await CallGeminiAPI(imagePath);
            }
        }

        private async Task CallGeminiAPI(string imagePath)
        {
            string prompt = "show me a name of food or drink, so that so me a kcal food .repose me style example {name:\"Cake\", kcal : 500 kcal}";
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
                    var content = new StringContent($"{{\"contents\":\r\n    [\r\n        {{\"parts\":\r\n        [\r\n            {{\"text\":\"show me a name of food or drink, so that so me a kcal food .repose me style example {{name:\\\"Cake\\\", kcal : 500 kcal}}\"}},\r\n            {{\"inline_data\": \r\n            {{\r\n                \r\n            \"mime_type\" : \"image/jpeg\",\r\n            \"data\": \"{imageBase64}\"\r\n             }}\r\n            \r\n            }}\r\n        ]\r\n        }}\r\n    ]\r\n}}");
                    var response = await client.PostAsync(endpointUrl, content);

                    if (response.IsSuccessStatusCode)
                    {
                        string jsonString = await response.Content.ReadAsStringAsync();
                        // Process the JSON response (implementation omitted for brevity)
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
    }
}

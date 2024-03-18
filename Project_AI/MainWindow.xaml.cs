using Microsoft.Win32;
using System.Net.Http.Headers;
using System.Net.Http;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.IO;

namespace Project_AI
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Button_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "Image files (*.jpg;*.jpeg;*.png) | *.jpg;*.jpeg;*.png";
            if (openFileDialog.ShowDialog() == true)
            {
                Picture.Source = new BitmapImage(new Uri(openFileDialog.FileName));
                string imagePath = openFileDialog.FileName;

                CallGeminiAPI(imagePath);
            }
        }

        private async void CallGeminiAPI(string imagePath)
        {
            // Replace with the actual prompt and endpoint URL (if different)
            string prompt = "show me a name of food or drink, so that so me a kcal food .repose me style example {name:\"Cake\", kcal : 500 kcal}";
            string endpointUrl = "https://generativelanguage.googleapis.com/v1beta/models/gemini-pro:generateContent?key=AIzaSyD3J41xmlxjIMGUljYo8yUtKK_MC87i5u4"; // Replace with your project ID

            // Accessing Google AI Studio's API requires authentication (refer to official documentation)
            // This example omits authentication for security reasons. Implement appropriate authentication mechanisms before using this code in production.

            // Assuming you have authentication implemented, uncomment the following lines:
            // string apiKey = "<YOUR_API_KEY>"; // Replace with your API key
            // client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", apiKey);

             var client = new HttpClient();
      client.DefaultRequestHeaders.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

      try
      {
        byte[] imageBytes;
        using (var imageStream = File.OpenRead(imagePath))
        {
          imageBytes = new byte[imageStream.Length];
          imageStream.Read(imageBytes, 0, (int)imageStream.Length);
        }

        string imageBase64 = Convert.ToBase64String(imageBytes);

                // var content = new StringContent($"{{\r\n  \"contents\":[\r\n    {{\r\n      \"parts\":[\r\n        {{\"text\": \"{prompt}\"}},\r\n        {{\r\n          \"inline_data\": {{\r\n            \"mime_type\":\"image/jpeg\",\r\n            \"data\": \"{imageBase64}\"\r\n          }}\r\n        }}\r\n      ]\r\n    }}\r\n  ]\r\n}}", Encoding.UTF8, "application/json");
                var content = new StringContent($"{{\"contents\":[{{\"parts\":[{{\"text\":\"Write a story about a magic backpack\"}}]}}]}}", Encoding.UTF8, "application/json");
        var response = await client.PostAsync(endpointUrl, content);

        if (response.IsSuccessStatusCode)
        {
          string jsonString = await response.Content.ReadAsStringAsync();
          // Process the JSON response to extract food name and kcal (implementation omitted for brevity)

          // Example: Parse the JSON response to get predicted food name and kcal (assuming the response format)
        
          
        }
        else
        {
          MessageBox.Show("Error calling Gemini API");
        }
      }
      catch (Exception ex)
      {
        MessageBox.Show($"Error reading image: {ex.Message}");
      }
    }

    }
}
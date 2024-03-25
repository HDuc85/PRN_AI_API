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
        //public ObservableCollection<Item> Items { get; set; }
        public MainWindow()
        {
            InitializeComponent();
            //Items = new ObservableCollection<Item>();
            //listView.DataContext = Items;
        }
        //private void ListView_SelectionChanged(object sender, SelectionChangedEventArgs e)
        //{
        //    if (listView.SelectedItem != null)
        //    {
        //        Item selectedItem = (Item)listView.SelectedItem;
        //        NameLabel.Content = selectedItem.Name;
        //        KcalLabel.Content = selectedItem.Kcal.ToString();
        //        DescribeLabel.Content = selectedItem.Describe;
        //        // Set image source
        //        Uri uri = new Uri(selectedItem.ImagePath, UriKind.RelativeOrAbsolute);
        //        Picture.Source = new System.Windows.Media.Imaging.BitmapImage(uri);
        //    }
        //}

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
                                Picture.Source = new BitmapImage(new Uri(imagePath));

                                NameLabel.Content = "Food name: " + foodItem.name;
                                KcalLabel.Content = "Kcal : " + foodItem.kcal + "KCal";

                                DescribeLabel.Content = "Describe : " + foodItem.describe;

                                //Items.Add(new Item {Describe = "Describe : " + foodItem.describe,
                                //                    ImagePath = imagePath,
                                //                    Kcal = "Kcal : " + foodItem.kcal + "KCal",
                                //                    Name = "Food name: " + foodItem.name
                                //});


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

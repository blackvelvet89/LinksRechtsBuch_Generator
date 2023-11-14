using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;
using System.Xml.Linq;
using System.Xml;
using System.Windows.Forms.VisualStyles;

namespace LinksRechtsBuch_Generator
{
    public partial class FormMain : Form
    {
        #region 0. Declarations
        bool debug = true;

        private CancellationTokenSource cancellationTokenSource;

        string osrmServerUrl = "http://le31.de:5000";
        string cityName = "Witten";
        double originLat = 51.44341032311196;
        double originLon = 7.336857729777326;

        List<string> directions = new List<string>();

        List<string> streetNames;
        Dictionary<char, List<string>> cityStreets = new Dictionary<char, List<string>>();
        #endregion

        #region 1. Init
        public FormMain()
        {
            InitializeComponent();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            if (debug)
            {
                this.Size = new System.Drawing.Size(1106, 910);
                groupBoxDebug.Enabled = true;
                groupBoxDebug.Visible = true;
            }
            else
            {
                this.Size = new System.Drawing.Size(1106, 760);
                groupBoxDebug.Enabled = false;
                groupBoxDebug.Visible = false;
            }
        }
        #endregion

        #region 2. UI

        private void buttonGetStreets_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON files (*.json)|*.json";
            openFileDialog.Title = "Select a JSON File";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                GetStreetsFromOverpassJSON(openFileDialog.FileName);
                DisplayControl();
            }
        }

        private void DisplayControl()
        {
            groupBoxCheck.Enabled = true;
            groupBoxRouting.Enabled = true;
            tabCheck.Controls.Clear();

            foreach (KeyValuePair<char, List<string>> kvp in cityStreets)
            {
                char firstLetter = kvp.Key;

                // Create a TabPage
                TabPage tabPage = new TabPage(firstLetter.ToString());
                tabPage.Text = firstLetter.ToString();

                // Create a ListBox
                ListBox listBox = new ListBox();
                listBox.Dock = DockStyle.Fill;

                // Add items to the ListBox
                foreach (string item in kvp.Value)
                {
                    listBox.Items.Add(item);
                }

                Button removeButton = new Button();
                removeButton.Text = "Ausgewählten Eintrag löschen";
                removeButton.Dock = DockStyle.Bottom;
                removeButton.Click += (sender, e) =>
                {
                    // Remove the selected item from the ListBox and the dictionary
                    if (listBox.SelectedIndex != -1)
                    {
                        string selectedItem = listBox.SelectedItem.ToString();
                        kvp.Value.Remove(selectedItem);

                        listBox.Items.RemoveAt(listBox.SelectedIndex);

                        if (kvp.Value.Count == 0)
                        {
                            cityStreets.Remove(firstLetter);
                            DisplayControl();
                        }
                    }
                };

                // Add the ListBox to the TabPage
                tabPage.Controls.Add(listBox);
                tabPage.Controls.Add(removeButton);


                // Add the TabPage to the TabControl
                tabCheck.TabPages.Add(tabPage);
            }

        }

        private void buttonSaveStreetsJson_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "JSON files (*.json)|*.json";
            saveFileDialog.Title = "Save Dictionary to JSON File";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Serialize and save the updated dictionary to the selected file path
                SerializeDictionary(cityStreets, saveFileDialog.FileName);
            }
        }

        private void buttonLoadStreets_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.Filter = "JSON files (*.json)|*.json";
            openFileDialog.Title = "Load Dictionary from JSON File";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Load and deserialize the dictionary from the selected file path
                cityStreets.Clear();
                cityStreets = DeserializeDictionary(openFileDialog.FileName);
                DisplayControl();
            }
        }

        private async void buttonGetRoutes_Click(object sender, EventArgs e)
        {
            if (await SetOrigin())
            {
                buttonCancelRoute.Enabled = true;
                groupBoxProgress.Text = "Status: Läuft";
                await GetRoutes();

                buttonSaveRoutes.Enabled = true;
                buttonCancelRoute.Enabled = false;
            }
        }
        private void buttonSaveRoutes_Click(object sender, EventArgs e)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "txt files (*.txt)|*.txt";
            saveFileDialog.Title = "Links-Rechts-Buch speichern.";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                // Serialize and save the updated dictionary to the selected file path
                File.WriteAllLines(saveFileDialog.FileName, directions);
            }
        }

        #endregion

        #region 3. Data Handling

        private void GetStreetsFromOverpassJSON(string filePath)
        {
            // Your JSON data as a string
            string jsonData = File.ReadAllText(filePath);

            // Parse the JSON content

            JObject jsonObject = JObject.Parse(jsonData);

            //Elements
            JArray elementsArray = (JArray)jsonObject["elements"];


            // List to store street names
            streetNames = new List<string>();

            // Iterate through each element in the array
            foreach (JObject element in elementsArray)
            {
                // Check if the element has "tags" property
                if (element.TryGetValue("tags", out var tags))
                {
                    // Check if the "tags" property has a "name" property
                    if (tags is JObject tagsObject && tagsObject.TryGetValue("name", out var nameValue))
                    {
                        if (tagsObject.TryGetValue("highway", out var highwayValue)
                            && (highwayValue.ToString() == "platform"
                                || highwayValue.ToString() == "bus_stop"))
                        {
                            //Bus stop instead of a street.
                        }
                        else
                        {
                            // Extract the street name
                            string streetName = nameValue.ToString();

                            // Add the street name to the list
                            streetNames.Add(streetName);
                        }
                    }
                }
            }

            List<string> streetNamesDistinctSorted = streetNames.OrderBy(x => x).ToList().Distinct().ToList();

            cityStreets = streetNamesDistinctSorted.GroupBy(x => Char.ToLower(x[0])).ToDictionary(group => group.Key, group => group.ToList());

        }

        async Task GetRoutes()
        {
            cancellationTokenSource = new CancellationTokenSource();

            progressBarRoutesOuter.ForeColor = Color.FromArgb(167, 41, 32);
            progressBarRoutesInner.ForeColor = Color.FromArgb(167, 41, 32);

            progressBarRoutesOuter.Value = 0;
            progressBarRoutesInner.Value = 0;

            progressBarRoutesOuter.Maximum = cityStreets.Count;


            // Iterate over your directory of streets
            foreach (KeyValuePair<char, List<string>> entry in cityStreets)
            {
                char firstLetter = entry.Key;
                List<string> streets = entry.Value;

                labelProgressOuter.Text = "Anfangsbuchstabe: " + firstLetter;

                labelProgressInner.Text = "0 von " + streets.Count.ToString();
                progressBarRoutesInner.Value = 0;
                progressBarRoutesInner.Maximum = streets.Count;

                foreach (string street in streets)
                {
                    if (cancellationTokenSource.Token.IsCancellationRequested)
                    {
                        groupBoxProgress.Text = "Status: Abgebrochen";
                        return;
                    }

                    // Get the route for each street
                    string streetCoordinates = await GetCoordinatesForStreetAsync(street);
                    if (string.IsNullOrEmpty(streetCoordinates) == false)
                    {
                        string routeUrl = $"{osrmServerUrl}/route/v1/driving/{originLon.ToString(CultureInfo.InvariantCulture)},{originLat.ToString(CultureInfo.InvariantCulture)};{streetCoordinates}?steps=true";

                        string routeJson = await GetRouteForStreetAsync(routeUrl);

                        Console.WriteLine(routeJson);
                        ExtractRouteInformation(street, routeJson);
                    }
                    progressBarRoutesInner.Value++;

                    labelProgressInner.Text = progressBarRoutesInner.Value.ToString() + " von " + streets.Count.ToString();

                    // Optional: Allow the UI to update during the loop
                    Application.DoEvents();

                }
                progressBarRoutesOuter.Value++;

                // Optional: Allow the UI to update during the loop
                Application.DoEvents();
            }
            groupBoxProgress.Text = "Status: Fertig";
        }


        // Function to get coordinates for a street using OSRM's Table API
        async Task<string?> GetCoordinatesForStreetAsync(string street)
        {
            using (HttpClient client = new HttpClient())
            {
                // Construct the URL with query parameters
                string url = $"https://photon.komoot.io/api/?q={cityName},{street}&limit=1";

                try
                {
                    // Make the GET request
                    HttpResponseMessage response = await client.GetAsync(url);

                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Read and process the response content
                        string responseBody = await response.Content.ReadAsStringAsync();
                        if (responseBody == "{\"features\":[],\"type\":\"FeatureCollection\"}")
                        {
                            return null;
                        }
                        // Parse the JSON response using Newtonsoft.Json
                        JObject jsonResponse = JObject.Parse(responseBody);



                        // Extract lon and lat values
                        JToken coordinates = jsonResponse["features"]?[0]?["geometry"]?["coordinates"];
                        double lon = coordinates?[0].Value<double>() ?? 0.0;
                        double lat = coordinates?[1].Value<double>() ?? 0.0;
                        return lon.ToString(CultureInfo.InvariantCulture) + ',' + lat.ToString(CultureInfo.InvariantCulture);
                    }
                    else
                    {
                        Console.WriteLine($"Error: {response.StatusCode}");
                        return null;
                    }
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"Request error: {e.Message}");
                    return null;
                }
            }
        }


        async Task<string?> GetRouteForStreetAsync(string routeUrl)
        {
            using (HttpClient client = new HttpClient())
            {

                try
                {
                    // Make the GET request
                    HttpResponseMessage response = await client.GetAsync(routeUrl);

                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Read and process the response content
                        string responseBody = await response.Content.ReadAsStringAsync();
                        // Parse the JSON response using Newtonsoft.Json
                        return JObject.Parse(responseBody).ToString();

                    }
                    else
                    {
                        Console.WriteLine($"Error: {response.StatusCode}");
                        return null;
                    }
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"Request error: {e.Message}");
                    return null;
                }

            }
        }

        private void ExtractRouteInformation(string targetStreet, string routeJSON)
        {
            StringBuilder directionBuilder = new StringBuilder();

            directionBuilder.AppendLine(targetStreet);
            directionBuilder.AppendLine();

            // Deserialize the JSON response
            OSRMResponse osrmResponse = JsonConvert.DeserializeObject<OSRMResponse>(routeJSON);

            // Iterate through steps
            foreach (Step step in osrmResponse.Routes[0].Legs[0].Steps)
            {
                if (step.Maneuver.Type == "turn")
                {
                    string direction = "";
                    if (step.Maneuver.Modifier.Contains("left"))
                    {
                        direction = "Links";
                    }
                    else if (step.Maneuver.Modifier.Contains("right"))
                    {
                        direction = "Rechts";
                    }

                    string streetName = step.Name;
                    if (!string.IsNullOrEmpty(streetName))
                    {
                        directionBuilder.AppendLine($"{direction} auf {streetName}");
                    }
                }
                else if (step.Maneuver.Type == "new name")
                {
                    string streetName = step.Name;
                    directionBuilder.AppendLine($"Weiter auf {streetName}");
                }
            }
            directionBuilder.AppendLine("==============================");
            directions.Add(directionBuilder.ToString());
            if (debug)
            {
                textBoxDebug.Text = directionBuilder.ToString();
            }
        }
        private async Task<bool> SetOrigin()
        {
            if (string.IsNullOrWhiteSpace(textBoxOriginCity.Text))
            {
                MessageBox.Show("Keine Stadt als Startpunkt angegeben.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }

            if (string.IsNullOrWhiteSpace(textBoxOriginStreet.Text))
            {
                MessageBox.Show("Keine Strasse als Startpunkt angegeben.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return false;
            }


            cityName = textBoxOriginCity.Text;
            using (HttpClient client = new HttpClient())
            {
                // Construct the URL with query parameters
                string url = $"https://photon.komoot.io/api/?q={cityName},{textBoxOriginStreet.Text}&limit=1";

                try
                {
                    // Make the GET request
                    HttpResponseMessage response = await client.GetAsync(url);

                    // Check if the request was successful
                    if (response.IsSuccessStatusCode)
                    {
                        // Read and process the response content
                        string responseBody = await response.Content.ReadAsStringAsync();
                        // Parse the JSON response using Newtonsoft.Json
                        JObject jsonResponse = JObject.Parse(responseBody);

                        // Extract lon and lat values
                        JToken coordinates = jsonResponse["features"]?[0]?["geometry"]?["coordinates"];
                        originLon = coordinates?[0].Value<double>() ?? 0.0;
                        originLat = coordinates?[1].Value<double>() ?? 0.0;
                        return true;
                    }
                    else
                    {
                        Console.WriteLine($"Error: {response.StatusCode}");
                        return false;
                    }
                }
                catch (HttpRequestException e)
                {
                    Console.WriteLine($"Request error: {e.Message}");
                    return false;
                }
            }
        }

        static void SerializeDictionary(Dictionary<char, List<string>> dictionary, string filePath)
        {
            // Serialize the dictionary to JSON
            string jsonContent = JsonConvert.SerializeObject(dictionary, Newtonsoft.Json.Formatting.Indented);

            // Save the JSON content to a file
            File.WriteAllText(filePath, jsonContent);
        }

        static Dictionary<char, List<string>> DeserializeDictionary(string filePath)
        {
            // Read the JSON content from the file
            string jsonContent = File.ReadAllText(filePath);

            // Deserialize the JSON content into a dictionary
            Dictionary<char, List<string>> loadedDictionary = JsonConvert.DeserializeObject<Dictionary<char, List<string>>>(jsonContent);

            return loadedDictionary;
        }

        #endregion

        private void buttonCancelRoute_Click(object sender, EventArgs e)
        {
            cancellationTokenSource?.Cancel();
        }
    }

    #region 6. Custom Classes

    public class OSRMResponse
    {
        public List<Route> Routes { get; set; }
    }

    public class Route
    {
        public List<Leg> Legs { get; set; }
    }

    public class Leg
    {
        public List<Step> Steps { get; set; }
    }

    public class Step
    {
        public Maneuver Maneuver { get; set; }
        public string Name { get; set; }
    }

    public class Maneuver
    {
        public string Type { get; set; }
        public string Modifier { get; set; }
    }
}

#endregion
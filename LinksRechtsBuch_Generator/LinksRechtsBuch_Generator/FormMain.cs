using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using PdfSharp.Drawing;
using PdfSharp.Internal;
using PdfSharp.Pdf;
using System.Data;
using System.Globalization;
using System.Text;
using PdfSharp.Fonts;
using System.Runtime.InteropServices;

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
        List<List<string>> directionLists = new List<List<string>>();
        
        #endregion

        #region 1. Init
        public FormMain()
        {
            InitializeComponent();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            GlobalFontSettings.FontResolver = new NewFontResolver();

            if (debug)
            {
                this.Size = new System.Drawing.Size(1106, 910);
                groupBoxDebug.Enabled = true;
                groupBoxDebug.Visible = true;

                textBoxOriginCity.Text = "Witten";
                textBoxOriginStreet.Text = "Hauptstrasse 60";

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
                removeButton.Text = "Ausgew�hlten Eintrag l�schen";
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
                groupBoxProgress.Text = "Status: L�uft";
                await GetRoutes();

                buttonSaveRoutes.Enabled = true;
                buttonCancelRoute.Enabled = false;
            }
        }
        private void buttonSaveRoutes_Click(object sender, EventArgs e)
        {
            SaveRoutes();
        }



        private void buttonCancelRoute_Click(object sender, EventArgs e)
        {
            cancellationTokenSource?.Cancel();
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
                    string? streetCoordinates = await GetCoordinatesForStreetAsync(street);
                    if (string.IsNullOrEmpty(streetCoordinates) == false)
                    {
                        string routeUrl = $"{osrmServerUrl}/route/v1/driving/{originLon.ToString(CultureInfo.InvariantCulture)},{originLat.ToString(CultureInfo.InvariantCulture)};{streetCoordinates}?steps=true";

                        string? routeJson = await GetRouteForStreetAsync(routeUrl);

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
            List<string> directionList = new List<string>();

            directionList.Add(targetStreet);
            directionBuilder.AppendLine(targetStreet);
            directionBuilder.AppendLine();

            // Deserialize the JSON response
            OSRMResponse? osrmResponse = JsonConvert.DeserializeObject<OSRMResponse>(routeJSON);

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
                        directionList.Add($"{direction} auf {streetName}");
                        directionBuilder.AppendLine($"{direction} auf {streetName}");
                    }
                }
                else if (step.Maneuver.Type == "new name")
                {
                    string streetName = step.Name;
                    directionList.Add($"Weiter auf {streetName}");
                    directionBuilder.AppendLine($"Weiter auf {streetName}");
                }
            }
            directionLists.Add(directionList);
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

        #region 4. File Creation
        private void SaveRoutes()
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Filter = "Text Datei (*.txt)|*.txt|PDF Datei (*.pdf)|*.pdf";
            saveFileDialog.Title = "Links-Rechts-Buch speichern.";

            if (saveFileDialog.ShowDialog() == DialogResult.OK)
            {
                if (Path.GetExtension(saveFileDialog.FileName).ToLower() == ".txt")
                {
                    File.WriteAllLines(saveFileDialog.FileName, directions);
                }
                else if (Path.GetExtension(saveFileDialog.FileName).ToLower() == ".pdf")
                {
                    CreatePdf(saveFileDialog.FileName);
                }
            }
        }

        private void CreatePdf(string fileName)
        {

            PdfDocument pdfDocument = new PdfDocument();

            CreateTitlepage(pdfDocument);


            pdfDocument.Save(fileName);
        }

        private void CreateTitlepage(PdfDocument pdf)
        {
            PdfPage titlepage = pdf.AddPage();

            //DIN A6
            double pageWidth = XUnit.FromMillimeter(105);
            double pageHeight = XUnit.FromMillimeter(148);
            double pageMargins = XUnit.FromMillimeter(10);
            double pageBindingMargin = XUnit.FromMillimeter(20);
            
            titlepage.Width = pageWidth;
            titlepage.Height = pageHeight;


            XGraphics gfx = XGraphics.FromPdfPage(titlepage);

            XFont titleFont = new XFont("Times New Roman", 20, XFontStyleEx.Regular);
            XFont subtitleFont = new XFont("Times New Roman", 10, XFontStyleEx.Regular);

            XRect recTitle1 = new XRect(pageBindingMargin, pageMargins, pageWidth, 20);
            XRect recTitle2 = new XRect(pageBindingMargin, recTitle1.Bottom + 5, pageWidth, 20);
            gfx.DrawString("Strassenverzeichnis", titleFont, XBrushes.Black, recTitle1, XStringFormats.TopLeft);
            gfx.DrawString(cityName, titleFont, XBrushes.Black, recTitle2, XStringFormats.TopLeft);

            double yOffset = 10;

            XRect recSubtitle = new XRect(pageBindingMargin, recTitle2.Bottom + yOffset, pageWidth, 10);
            gfx.DrawString($"Startpunkt {textBoxOriginStreet.Text}", subtitleFont, XBrushes.Black, recSubtitle, XStringFormats.TopLeft);

        }
        #endregion

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


public class NewFontResolver : IFontResolver
{
    /// <summary>
    /// NewFontResolver singleton for use in unit tests.
    /// </summary>
    public static NewFontResolver Get()
    {
        try
        {
            Monitor.Enter(typeof(NewFontResolver));

            if (_singleton != null)
                return _singleton;
            return _singleton = new NewFontResolver();
        }
        finally
        {
            Monitor.Exit(typeof(NewFontResolver));
        }
    }
    private static NewFontResolver? _singleton;

#if DEBUG
    public override String ToString()
    {
        var result = "Base: " + (base.ToString() ?? "<null>");
        if (ReferenceEquals(this, _singleton))
            result = "<Singleton>. " + result;

        return result;
    }
#endif

    public record Family(
        string FamilyName,
        string FaceName,
        string LinuxFaceName = "",
        params string[] LinuxSubstituteFamilyNames)
    { }

    public static readonly List<Family> Families;

    static NewFontResolver()
    {
        Families = new List<Family>
            {
                new("Arial", "arial", "Arial", "FreeSans"),
                new("Arial Black", "ariblk", "Arial-Black"),
                new("Arial Bold", "arialbd", "Arial-Bold", "FreeSansBold"),
                new("Arial Italic", "ariali", "Arial-Italic", "FreeSansOblique"),
                new("Arial Bold Italic", "arialbi", "Arial-BoldItalic", "FreeSansBoldOblique"),

                new("Courier New", "cour", "Courier-Bold", "DejaVu Sans Mono", "Bitstream Vera Sans Mono", "FreeMono"),
                new("Courier New Bold", "courbd", "CourierNew-Bold", "DejaVu Sans Mono Bold", "Bitstream Vera Sans Mono Bold", "FreeMonoBold"),
                new("Courier New Italic", "couri", "CourierNew-Italic", "DejaVu Sans Mono Oblique", "Bitstream Vera Sans Mono Italic", "FreeMonoOblique"),
                new("Courier New Bold Italic", "courbi", "CourierNew-BoldItalic", "DejaVu Sans Mono Bold Oblique", "Bitstream Vera Sans Mono Bold Italic", "FreeMonoBoldOblique"),

                new("Verdana", "verdana", "Verdana", "DejaVu Sans", "Bitstream Vera Sans"),
                new("Verdana Bold", "verdanab", "Verdana-Bold", "DejaVu Sans Bold", "Bitstream Vera Sans Bold"),
                new("Verdana Italic", "verdanai", "Verdana-Italic", "DejaVu Sans Oblique", "Bitstream Vera Sans Italic"),
                new("Verdana Bold Italic", "verdanaz", "Verdana-BoldItalic", "DejaVu Sans Bold Oblique", "Bitstream Vera Sans Bold Italic"),

                new("Times New Roman", "times", "TimesNewRoman", "FreeSerif"),
                new("Times New Roman Bold", "timesbd", "TimesNewRoman-Bold", "FreeSerifBold"),
                new("Times New Roman Italic", "timesi", "TimesNewRoman-Italic", "FreeSerifItalic"),
                new("Times New Roman Bold Italic", "timesbi", "TimesNewRoman-BoldItalic", "FreeSerifBoldItalic"),

                new("Lucida Console", "lucon", "LucidaConsole", "DejaVu Sans Mono"),

                new("Symbol", "symbol", "", "Noto Sans Symbols Regular"), // Noto Symbols may not replace exactly

                new("Wingdings", "wingding"), // No Linux substitute

                // Linux Substitute Fonts
                // TODO Nimbus and Liberation are only readily available as OTF.

                // Ubuntu packages: fonts-dejavu fonts-dejavu-core fonts-dejavu-extra
                new("DejaVu Sans", "DejaVuSans"),
                new("DejaVu Sans Bold", "DejaVuSans-Bold"),
                new("DejaVu Sans Oblique", "DejaVuSans-Oblique"),
                new("DejaVu Sans Bold Oblique", "DejaVuSans-BoldOblique"),
                new("DejaVu Sans Mono", "DejaVuSansMono"),
                new("DejaVu Sans Mono Bold", "DejaVuSansMono-Bold"),
                new("DejaVu Sans Mono Oblique", "DejaVuSansMono-Oblique"),
                new("DejaVu Sans Mono Bold Oblique", "DejaVuSansMono-BoldOblique"),

                // Ubuntu packages: fonts-freefont-ttf
                new("FreeSans", "FreeSans"),
                new("FreeSansBold", "FreeSansBold"),
                new("FreeSansOblique", "FreeSansOblique"),
                new("FreeSansBoldOblique", "FreeSansBoldOblique"),
                new("FreeMono", "FreeMono"),
                new("FreeMonoBold", "FreeMonoBold"),
                new("FreeMonoOblique", "FreeMonoOblique"),
                new("FreeMonoBoldOblique", "FreeMonoBoldOblique"),
                new("FreeSerif", "FreeSerif"),
                new("FreeSerifBold", "FreeSerifBold"),
                new("FreeSerifItalic", "FreeSerifItalic"),
                new("FreeSerifBoldItalic", "FreeSerifBoldItalic"),

                // Ubuntu packages: ttf-bitstream-vera
                new("Bitstream Vera Sans", "Vera"),
                new("Bitstream Vera Sans Bold", "VeraBd"),
                new("Bitstream Vera Sans Italic", "VeraIt"),
                new("Bitstream Vera Sans Bold Italic", "VeraBI"),
                new("Bitstream Vera Sans Mono", "VeraMono"),
                new("Bitstream Vera Sans Mono Bold", "VeraMoBd"),
                new("Bitstream Vera Sans Mono Italic", "VeraMoIt"),
                new("Bitstream Vera Sans Mono Bold Italic", "VeraMoBI"),

                // Ubuntu packages: fonts-noto-core
                new("Noto Sans Symbols Regular", "NotoSansSymbols-Regular"),
                new("Noto Sans Symbols Bold", "NotoSansSymbols-Bold"),
            };
    }

    public FontResolverInfo? ResolveTypeface(string familyName, bool isBold, bool isItalic)
    {
        var families = Families.Where(f => f.FamilyName.StartsWith(familyName));
        var baseFamily = Families.FirstOrDefault();

        if (isBold)
            families = families.Where(f => f.FamilyName.ToLowerInvariant().Contains("bold") || f.FamilyName.ToLowerInvariant().Contains("heavy"));

        if (isItalic)
            families = families.Where(f => f.FamilyName.ToLowerInvariant().Contains("italic") || f.FamilyName.ToLowerInvariant().Contains("oblique"));

        var family = families.FirstOrDefault();
        if (family is not null)
            return new FontResolverInfo(family.FaceName);

        if (baseFamily is not null)
            return new FontResolverInfo(baseFamily.FaceName, isBold, isItalic);

        return null;
    }

    public byte[]? GetFont(string faceName)
    {
        if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            return GetFontWindows(faceName);

        if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            return GetFontLinux(faceName);

        return null;
    }

    byte[]? GetFontWindows(string faceName)
    {
        var fontLocations = new List<string>
            {
                @"C:\Windows\Fonts",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "Microsoft\\Windows\\Fonts")
            };

        foreach (var fontLocation in fontLocations)
        {
            var filepath = Path.Combine(fontLocation, faceName + ".ttf");
            if (File.Exists(filepath))
                return File.ReadAllBytes(filepath);
        }

        return null;
    }

    byte[]? GetFontLinux(string faceName)
    {
        // TODO Query fontconfig.
        // Fontconfig is the de facto standard for indexing and managing fonts on linux.
        // Example command that should return a full file path to FreeSansBoldOblique.ttf:
        //     fc-match -f '%{file}\n' 'FreeSans:Bold:Oblique:fontformat=TrueType' : file
        //
        // Caveat: fc-match *always* returns a "next best" match or default font, even if it's bad.
        // Caveat: some preprocessing/refactoring needed to produce a pattern fc-match can understand.
        // Caveat: fontconfig needs additional configuration to know about WSL having Windows Fonts available at /mnt/c/Windows/Fonts.

        var fontLocations = new List<string>
            {
                "/mnt/c/Windows/Fonts", // WSL first or substitutes will be found.
                "/usr/share/fonts",
                "/usr/share/X11/fonts",
                "/usr/X11R6/lib/X11/fonts",
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "/.fonts"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.UserProfile), "/.local/share/fonts"),
            };

        var fcp = Environment.GetEnvironmentVariable("FONTCONFIG_PATH");
        if (fcp is not null && !fontLocations.Contains(fcp))
            fontLocations.Add(fcp);

        foreach (var fontLocation in fontLocations)
        {
            if (!Directory.Exists(fontLocation))
                continue;

            var fontPath = FindFileRecursive(fontLocation, faceName);
            if (fontPath is not null && File.Exists(fontPath))
                return File.ReadAllBytes(fontPath);
        }

        return null;
    }

    /// <summary>
    /// Finds filename candidates recursively on Linux, as organizing fonts into arbitrary subdirectories is allowed.
    /// </summary>
    string? FindFileRecursive(string basepath, string faceName)
    {
        var filenameCandidates = FaceNameToFilenameCandidates(faceName);

        foreach (var file in Directory.GetFiles(basepath).Select(Path.GetFileName))
            foreach (var filenameCandidate in filenameCandidates)
            {
                // Most programs treat fonts case-sensitive on Linux. We ignore case because we also target WSL.
                if (!String.IsNullOrEmpty(file) && file.Equals(filenameCandidate, StringComparison.OrdinalIgnoreCase))
                    return Path.Combine(basepath, filenameCandidate);
            }

        // Linux allows arbitrary subdirectories for organizing fonts.
        foreach (var directory in Directory.GetDirectories(basepath).Select(Path.GetFileName))
        {
            if (String.IsNullOrEmpty(directory))
                continue;

            var file = FindFileRecursive(Path.Combine(basepath, directory), faceName);
            if (file is not null)
                return file;
        }

        return null;
    }

    /// <summary>
    /// Generates filename candidates for Linux systems.
    /// </summary>
    string[] FaceNameToFilenameCandidates(string faceName)
    {
        const string fileExtension = ".ttf";
        // TODO OTF Fonts are popular on Linux too.

        var candidates = new List<string>
            {
                faceName + fileExtension // We need to look for Windows face name too in case of WSL or copied files.
            };

        var family = Families.FirstOrDefault(f => f.FaceName == faceName);
        if (family is null)
            return candidates.ToArray();

        if (!String.IsNullOrEmpty(family.LinuxFaceName))
            candidates.Add(family.LinuxFaceName + fileExtension);
        candidates.Add(family.FamilyName + fileExtension);

        // Add substitute fonts as last candidates.
        foreach (var replacement in family.LinuxSubstituteFamilyNames)
        {
            var replacementFamily = Families.FirstOrDefault(f => f.FamilyName == replacement);
            if (replacementFamily is null)
                continue;

            candidates.Add(replacementFamily.FamilyName + fileExtension);
            if (!String.IsNullOrEmpty(replacementFamily.FaceName))
                candidates.Add(replacementFamily.FaceName + fileExtension);
            if (!String.IsNullOrEmpty(replacementFamily.LinuxFaceName))
                candidates.Add(replacementFamily.LinuxFaceName + fileExtension);
        }

        return candidates.ToArray();
    }
}

#endregion
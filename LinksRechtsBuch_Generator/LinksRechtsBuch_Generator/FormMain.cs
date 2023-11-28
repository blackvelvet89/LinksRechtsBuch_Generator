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

        List<string> directionsPlainText = new List<string>();

        List<string> streetNames;
        Dictionary<char, List<string>> cityStreets = new Dictionary<char, List<string>>();
        List<Tuple<string, List<string>>> directionSet = new List<Tuple<string, List<string>>>();

        #endregion

        #region 1. Init
        public FormMain()
        {
            InitializeComponent();
        }

        private void FormMain_Load(object sender, EventArgs e)
        {
            GlobalFontSettings.FontResolver = new FontHandling.NewFontResolver();

            if (debug)
            {
                this.Size = new System.Drawing.Size(1106, 960);
                groupBoxDebug.Enabled = true;
                groupBoxDebug.Visible = true;

                comboBoxState.SelectedItem = "Nordrhein-Westfalen";
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

                        if (routeJson == null)
                        {
                            MessageBox.Show("Verbindung zu Routing Service fehlgeschlagen.", "Fehler", MessageBoxButtons.OK, MessageBoxIcon.Error);
                            return;
                        }
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

            directionBuilder.AppendLine(targetStreet);
            directionBuilder.AppendLine();

            // Deserialize the JSON response
            OSRMResponse? osrmResponse = JsonConvert.DeserializeObject<OSRMResponse>(routeJSON);

            // Iterate through steps
            foreach (Step step in osrmResponse.Routes[0].Legs[0].Steps)
            {
                if (step.Maneuver.Type == "turn" || step.Maneuver.Type == "end of road" || step.Maneuver.Type == "continue")
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
                else if (step.Maneuver.Type == "exit roundabout")
                {
                    string streetName = step.Name;
                    directionList.Add($"Kreisverkehr auf {streetName}");
                    directionBuilder.AppendLine($"Kreisverkehr auf {streetName}");
                }
                else if (step.Maneuver.Type == "on ramp")
                {
                    string streetName = step.Destinations;
                    directionList.Add($"Auf Autobahn {streetName}");
                    directionBuilder.AppendLine($"Auf Autobahn {streetName}");
                }
                else if (step.Maneuver.Type == "off ramp")
                {
                    string streetName = step.Destinations;
                    directionList.Add($"Abfahrt {streetName}");
                    directionBuilder.AppendLine($"Abfahrt {streetName}");
                }
            }
            directionSet.Add(new Tuple<string, List<string>>(targetStreet, directionList));
            directionBuilder.AppendLine("==============================");
            directionsPlainText.Add(directionBuilder.ToString());
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


            cityName = $"{comboBoxState.SelectedItem.ToString()}, {textBoxOriginCity.Text}";
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
        private void SaveRoutes(FileType fileType)
        {
            SaveFileDialog saveFileDialog = new SaveFileDialog();
            saveFileDialog.Title = "Links-Rechts-Buch speichern.";

            switch (fileType)
            {

                case FileType.txt:

                    saveFileDialog.Filter = "Text Datei (*.txt)|*.txt";

                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        File.WriteAllLines(saveFileDialog.FileName, directionsPlainText);
                    }
                    break;
                case FileType.pdf:
                    saveFileDialog.Filter = "PDF Datei(*.pdf) | *.pdf";
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        CreatePdf().Save(saveFileDialog.FileName);
                    }
                    break;
                case FileType.pdfBook:
                    if (saveFileDialog.ShowDialog() == DialogResult.OK)
                    {
                        saveFileDialog.Filter = "PDF Datei(*.pdf) | *.pdf";
                        CreatePdfBook().Save(saveFileDialog.FileName);
                    }
                    break;
                default:
                    break;
            }
        }

        private PdfDocument CreatePdfBook()
        {
            PdfDocument pdfDocument = CreatePdf();
            PdfDocument sortedDocument = new PdfDocument();

            //Sort it so it can be printed correctly
            //Intended for printing two DIN A6 pages on a DIN A5 sheet of paper
            //Both sides will be printed on

            //The book will be bound by putting all the pages on a stack
            //then they'll be stapled in the middle and folded.
            //Page[0] is the title page
            //Page[1] is empty
            //Directions start on page [2]

            //First page will be the outer back side of the booklet
            CreateEmptyPage(sortedDocument);

            //Second page will be the outer front side of the booklet
            CreateTitlepage(sortedDocument);

            //Third page will be the first page inside the booklet
            CreateEmptyPage(sortedDocument);

            //Fourth page will be the last page inside the booklet
            CreateEmptyPage(sortedDocument);

            //All other pages will have to be iterated through
            //The sorting schema is
            //  front left: x-1
            //  front right: n
            //  back left: n+1
            //  back right: x-2
            //until all pages are used. Due to this the number of pages will have to be divisible by 4
            
            int remainder = (pdfDocument.PageCount - 2) % 4;
            int addToMakeDivisibleByFour = remainder == 0 ? 0 : 4 - remainder;

            for (int i = 0; i <= addToMakeDivisibleByFour; i++)
            {
                CreateEmptyPage(pdfDocument);
            }



            return pdfDocument;
        }

        private PdfDocument CreatePdf()
        {

            PdfDocument pdfDocument = new PdfDocument();

            int currentPage = 1;

            CreateTitlepage(pdfDocument);
            CreateEmptyPage(pdfDocument);

            currentPage++;

            double usedHeight = 0.00;
            double neededHeight = 0.00;
            bool firstOnPage = true;

            List<Tuple<string, List<string>>> pageContents = new List<Tuple<string, List<string>>>();
            foreach (Tuple<string, List<string>> currentDirections in directionSet)
            {
                if (firstOnPage)
                {
                    neededHeight = CalculateNeededHeight(currentDirections.Item2, A6Page.streetRectBindLeft, A6Page.directionRectBindLeft, 0);
                }
                else
                {
                    neededHeight = CalculateNeededHeight(currentDirections.Item2, A6Page.streetRectBindLeft, A6Page.directionRectBindLeft, A6Page.spacer);
                }

                //Check if there's enough space left on the page
                if (usedHeight + neededHeight <= A6Page.pageHeightPrintable)
                {
                    //Collect
                    pageContents.Add(currentDirections);
                    usedHeight = usedHeight + neededHeight;
                    firstOnPage = false;
                }
                else
                {
                    //Create the current page and put this street to the next page
                    //CreateDirectionPage
                    CreateDirectionPage(pdfDocument, currentPage, pageContents);
                    pageContents.Clear();
                    pageContents.Add(currentDirections);
                    usedHeight = neededHeight;
                    currentPage++;
                }

                //If it's the last entry, always create the current page
                if (currentDirections == directionSet.Last())
                {
                    //Create the current page and put this street to the next page
                    //CreateDirectionPage
                    CreateDirectionPage(pdfDocument, currentPage, pageContents);
                    pageContents.Clear();
                    usedHeight = neededHeight;
                }

            }
            return pdfDocument;
        }



        private void CreateTitlepage(PdfDocument pdf)
        {
            PdfPage titlepage = pdf.AddPage();

            //DIN A6 Settings
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
            gfx.DrawString($"{textBoxOriginCity.Text}", titleFont, XBrushes.Black, recTitle2, XStringFormats.TopLeft);

            double yOffset = 10;

            XRect recSubtitle = new XRect(pageBindingMargin, recTitle2.Bottom + yOffset, pageWidth, 10);
            gfx.DrawString($"Startpunkt {textBoxOriginStreet.Text}", subtitleFont, XBrushes.Black, recSubtitle, XStringFormats.TopLeft);

        }
        private void CreateEmptyPage(PdfDocument pdf)
        {
            PdfPage emptyPage = new PdfPage();
            emptyPage.Height = A6Page.pageHeight;
            emptyPage.Width = A6Page.pageWidth;
            pdf.AddPage(emptyPage);
        }

        private void CreateTestPage(PdfDocument pdf)
        {
            /*
                Using DIN A6 paper
                Paper height is 148mm
                Page margins are 10mm
                148mm - 2 * 10mm = 128mm
                Leaving space for page number => 115mm of usable space

                Got to change the Binding Margin side for each page, since they will be printed on both sides!
            */

            PdfPage testpage = pdf.AddPage();

            //DIN A6 Settings
            double pageWidth = XUnit.FromMillimeter(105);
            double pageHeight = XUnit.FromMillimeter(148);
            double pageMargins = XUnit.FromMillimeter(10);
            double pageBindingMargin = XUnit.FromMillimeter(20);

            double pageWidthPrintable = pageWidth - pageBindingMargin - pageMargins;
            double pageHeightPrintable = pageHeight - pageMargins - pageMargins;

            testpage.Width = pageWidth;
            testpage.Height = pageHeight;

            XGraphics gfx = XGraphics.FromPdfPage(testpage);
            XFont streetFont = new XFont("Times New Roman", 10, XFontStyleEx.Bold);
            XFont directionFont = new XFont("Times New Roman", 10, XFontStyleEx.Regular);
            XFont pageNumberFont = new XFont("Times New Roman", 6, XFontStyleEx.Regular);

            XRect streetRect = new XRect(pageBindingMargin, pageMargins, pageWidth, 15);
            XRect directionRect = new XRect(pageBindingMargin, pageMargins, pageWidth, 10);
            XRect pageNumberRect = new XRect(pageBindingMargin, XUnit.FromMillimeter(128), pageWidth - pageBindingMargin - pageMargins, XUnit.FromMillimeter(10));


            gfx.DrawString($"Testzeile", streetFont, XBrushes.Black, streetRect, XStringFormats.TopLeft);
            directionRect.Y = streetRect.Bottom;
            gfx.DrawString($"Testzeile", directionFont, XBrushes.Black, directionRect, XStringFormats.TopLeft);
            gfx.DrawString($"1", pageNumberFont, XBrushes.Black, pageNumberRect, XStringFormats.BottomCenter);

            //show margins in black rectangles
            if (true)
            {
                XRect topMargin = new XRect(0, 0, pageWidth, pageMargins);
                gfx.DrawRectangle(new XPen(XColor.FromKnownColor(XKnownColor.Black)), topMargin);

                XRect bottomMargin = new XRect(0, pageHeight - pageMargins, pageWidth, pageMargins);
                gfx.DrawRectangle(new XPen(XColor.FromKnownColor(XKnownColor.Black)), bottomMargin);

                XRect rightMargin = new XRect(pageWidth - pageMargins, 0, pageMargins, pageHeight);
                gfx.DrawRectangle(new XPen(XColor.FromKnownColor(XKnownColor.Black)), rightMargin);

                XRect bindingMargin = new XRect(0, 0, pageBindingMargin, pageHeight);
                gfx.DrawRectangle(new XPen(XColor.FromKnownColor(XKnownColor.Black)), bindingMargin);
            }
        }

        private double CalculateNeededHeight(List<string> directions, XRect streetRect, XRect directionRect, double spacer)
        {
            double neededHeight = spacer + streetRect.Height + (directions.Count * directionRect.Height);
            return neededHeight;
        }

        private PdfPage CreateDirectionPage(PdfDocument pdf, int pageNumber, List<Tuple<string, List<string>>> pageContents)
        {
            PageSide bindingSide = pageNumber % 2 == 0 ? PageSide.Left : PageSide.Right;
            PdfPage currentPage = pdf.AddPage();
            currentPage.Height = A6Page.pageHeight;
            currentPage.Width = A6Page.pageWidth;
            XGraphics gfx = XGraphics.FromPdfPage(currentPage);
            XRect streetRect = bindingSide == PageSide.Left ? A6Page.streetRectBindRight : A6Page.streetRectBindLeft;
            XRect directionRect = bindingSide == PageSide.Left ? A6Page.directionRectBindRight : A6Page.directionRectBindLeft;
            XRect pageNumberRect = bindingSide == PageSide.Left ? A6Page.pageNumberRectBindLeft : A6Page.pageNumberRectBindRight;

            foreach (Tuple<string, List<string>> currentStreet in pageContents)
            {
                gfx.DrawString($"{currentStreet.Item1}", A6Page.streetFont, XBrushes.Black, streetRect, XStringFormats.TopLeft);
                directionRect.Y = streetRect.Bottom;
                foreach (string currentDirection in currentStreet.Item2)
                {
                    gfx.DrawString($"{currentDirection}", A6Page.directionFont, XBrushes.Black, directionRect, XStringFormats.TopLeft);
                    directionRect.Y = directionRect.Bottom;
                }
                streetRect.Y = directionRect.Bottom + A6Page.spacer;
            }
            gfx.DrawString($"{pageNumber.ToString()}", A6Page.pageNumberFont, XBrushes.Black, pageNumberRect, XStringFormats.BottomCenter);

            return currentPage;
        }

        private void SortPDF(PdfDocument pdf)
        {
            int pageCount = pdf.Pages.Count;

            //Sort the pages so they can be printed on A5 sheets, double sided

            //First we'll have to find out how many pages we've got.
            if (pageCount)
            {

            }
        }

        #endregion

        private void buttonSaveText_Click(object sender, EventArgs e)
        {
            SaveRoutes(FileType.txt);
        }

        private void buttonSavePdf_Click(object sender, EventArgs e)
        {
            SaveRoutes(FileType.pdf);
        }

        private void buttonSavePdfBook_Click(object sender, EventArgs e)
        {
            SaveRoutes(FileType.pdfBook);
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
        public string Destinations { get; set; }
    }

    public class Maneuver
    {
        public string Type { get; set; }
        public string Modifier { get; set; }
    }

    public class A6Page
    {
        public static readonly double pageWidth = XUnit.FromMillimeter(105);
        public static readonly double pageHeight = XUnit.FromMillimeter(148);
        public static readonly double pageMargins = XUnit.FromMillimeter(10);
        public static readonly double pageBindingMargin = XUnit.FromMillimeter(20);

        public static readonly double pageWidthPrintable = pageWidth - pageBindingMargin - pageMargins;
        public static readonly double pageHeightPrintable = pageHeight - pageMargins - pageMargins - XUnit.FromMillimeter(10);

        public static readonly XFont streetFont = new XFont("Times New Roman", 10, XFontStyleEx.Bold);
        public static readonly XFont directionFont = new XFont("Times New Roman", 10, XFontStyleEx.Regular);
        public static readonly XFont pageNumberFont = new XFont("Times New Roman", 6, XFontStyleEx.Regular);

        public static readonly XRect streetRectBindLeft = new XRect(pageBindingMargin, pageMargins, pageWidth - pageMargins, 15);
        public static readonly XRect directionRectBindLeft = new XRect(pageBindingMargin, pageMargins, pageWidth - pageMargins, 10);
        public static readonly XRect streetRectBindRight = new XRect(pageMargins, pageMargins, pageWidth - pageBindingMargin, 15);
        public static readonly XRect directionRectBindRight = new XRect(pageMargins, pageMargins, pageWidth - pageBindingMargin, 10);
        public static readonly XRect pageNumberRectBindLeft = new XRect(pageMargins, XUnit.FromMillimeter(128), pageWidth - pageBindingMargin - pageMargins, XUnit.FromMillimeter(10));
        public static readonly XRect pageNumberRectBindRight = new XRect(pageBindingMargin, XUnit.FromMillimeter(128), pageWidth - pageBindingMargin - pageMargins, XUnit.FromMillimeter(10));
        public static readonly double spacer = 10.00;
    }

    public enum PageSide { Left, Right }

    public enum FileType { txt, pdf, pdfBook }
}

#endregion
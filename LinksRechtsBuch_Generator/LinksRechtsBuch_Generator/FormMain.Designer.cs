namespace LinksRechtsBuch_Generator
{
    partial class FormMain
    {
        /// <summary>
        /// Erforderliche Designervariable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Verwendete Ressourcen bereinigen.
        /// </summary>
        /// <param name="disposing">True, wenn verwaltete Ressourcen gelöscht werden sollen; andernfalls False.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Vom Windows Form-Designer generierter Code

        /// <summary>
        /// Erforderliche Methode für die Designerunterstützung.
        /// Der Inhalt der Methode darf nicht mit dem Code-Editor geändert werden.
        /// </summary>
        private void InitializeComponent()
        {
            buttonGetStreets = new Button();
            groupBoxBaseData = new GroupBox();
            buttonLoadStreets = new Button();
            groupBoxCheck = new GroupBox();
            buttonSaveStreetsJson = new Button();
            tabCheck = new TabControl();
            groupBoxInstructions = new GroupBox();
            textBoxInstructions = new TextBox();
            groupBoxRouting = new GroupBox();
            buttonCancelRoute = new Button();
            groupBoxProgress = new GroupBox();
            labelProgressInner = new Label();
            progressBarRoutesOuter = new ProgressBar();
            labelProgressOuter = new Label();
            progressBarRoutesInner = new ProgressBar();
            buttonSaveRoutes = new Button();
            groupBoxOrigin = new GroupBox();
            comboBoxState = new ComboBox();
            labelState = new Label();
            labelOriginStreet = new Label();
            textBoxOriginStreet = new TextBox();
            labelOriginCity = new Label();
            textBoxOriginCity = new TextBox();
            buttonGetRoutes = new Button();
            groupBoxDebug = new GroupBox();
            textBoxDebug = new TextBox();
            groupBoxBaseData.SuspendLayout();
            groupBoxCheck.SuspendLayout();
            groupBoxInstructions.SuspendLayout();
            groupBoxRouting.SuspendLayout();
            groupBoxProgress.SuspendLayout();
            groupBoxOrigin.SuspendLayout();
            groupBoxDebug.SuspendLayout();
            SuspendLayout();
            // 
            // buttonGetStreets
            // 
            buttonGetStreets.Dock = DockStyle.Top;
            buttonGetStreets.Location = new Point(4, 19);
            buttonGetStreets.Margin = new Padding(4, 3, 4, 3);
            buttonGetStreets.Name = "buttonGetStreets";
            buttonGetStreets.Size = new Size(212, 58);
            buttonGetStreets.TabIndex = 0;
            buttonGetStreets.Text = "Straßen extrahieren\r\n(JSON von overpass)";
            buttonGetStreets.UseVisualStyleBackColor = true;
            buttonGetStreets.Click += buttonGetStreets_Click;
            // 
            // groupBoxBaseData
            // 
            groupBoxBaseData.Controls.Add(buttonLoadStreets);
            groupBoxBaseData.Controls.Add(buttonGetStreets);
            groupBoxBaseData.Location = new Point(14, 14);
            groupBoxBaseData.Margin = new Padding(4, 3, 4, 3);
            groupBoxBaseData.Name = "groupBoxBaseData";
            groupBoxBaseData.Padding = new Padding(4, 3, 4, 3);
            groupBoxBaseData.Size = new Size(220, 148);
            groupBoxBaseData.TabIndex = 3;
            groupBoxBaseData.TabStop = false;
            groupBoxBaseData.Text = "1. Basisdaten einlesen";
            // 
            // buttonLoadStreets
            // 
            buttonLoadStreets.Dock = DockStyle.Bottom;
            buttonLoadStreets.Location = new Point(4, 87);
            buttonLoadStreets.Margin = new Padding(4, 3, 4, 3);
            buttonLoadStreets.Name = "buttonLoadStreets";
            buttonLoadStreets.Size = new Size(212, 58);
            buttonLoadStreets.TabIndex = 4;
            buttonLoadStreets.Text = "Straßen einlesen\r\n(Nach Kontrolle/Anpassung)";
            buttonLoadStreets.UseVisualStyleBackColor = true;
            buttonLoadStreets.Click += buttonLoadStreets_Click;
            // 
            // groupBoxCheck
            // 
            groupBoxCheck.Controls.Add(buttonSaveStreetsJson);
            groupBoxCheck.Controls.Add(tabCheck);
            groupBoxCheck.Enabled = false;
            groupBoxCheck.Location = new Point(14, 172);
            groupBoxCheck.Margin = new Padding(4, 3, 4, 3);
            groupBoxCheck.Name = "groupBoxCheck";
            groupBoxCheck.Padding = new Padding(4, 3, 4, 3);
            groupBoxCheck.Size = new Size(1058, 374);
            groupBoxCheck.TabIndex = 4;
            groupBoxCheck.TabStop = false;
            groupBoxCheck.Text = "2. Datenkontrolle und -anpassung";
            // 
            // buttonSaveStreetsJson
            // 
            buttonSaveStreetsJson.Anchor = AnchorStyles.Bottom | AnchorStyles.Right;
            buttonSaveStreetsJson.Location = new Point(964, 336);
            buttonSaveStreetsJson.Margin = new Padding(4, 3, 4, 3);
            buttonSaveStreetsJson.Name = "buttonSaveStreetsJson";
            buttonSaveStreetsJson.Size = new Size(88, 27);
            buttonSaveStreetsJson.TabIndex = 1;
            buttonSaveStreetsJson.Text = "Speichern";
            buttonSaveStreetsJson.UseVisualStyleBackColor = true;
            buttonSaveStreetsJson.Click += buttonSaveStreetsJson_Click;
            // 
            // tabCheck
            // 
            tabCheck.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
            tabCheck.Location = new Point(10, 22);
            tabCheck.Margin = new Padding(4, 3, 4, 3);
            tabCheck.Name = "tabCheck";
            tabCheck.SelectedIndex = 0;
            tabCheck.Size = new Size(1041, 307);
            tabCheck.TabIndex = 0;
            // 
            // groupBoxInstructions
            // 
            groupBoxInstructions.Controls.Add(textBoxInstructions);
            groupBoxInstructions.Font = new Font("Microsoft Sans Serif", 9.75F, FontStyle.Bold, GraphicsUnit.Point);
            groupBoxInstructions.Location = new Point(414, 14);
            groupBoxInstructions.Margin = new Padding(4, 3, 4, 3);
            groupBoxInstructions.Name = "groupBoxInstructions";
            groupBoxInstructions.Padding = new Padding(4, 3, 4, 3);
            groupBoxInstructions.Size = new Size(662, 151);
            groupBoxInstructions.TabIndex = 5;
            groupBoxInstructions.TabStop = false;
            groupBoxInstructions.Text = "Anleitung";
            // 
            // textBoxInstructions
            // 
            textBoxInstructions.Dock = DockStyle.Fill;
            textBoxInstructions.Font = new Font("Microsoft Sans Serif", 8.25F, FontStyle.Regular, GraphicsUnit.Point);
            textBoxInstructions.Location = new Point(4, 18);
            textBoxInstructions.Margin = new Padding(4, 3, 4, 3);
            textBoxInstructions.Multiline = true;
            textBoxInstructions.Name = "textBoxInstructions";
            textBoxInstructions.ReadOnly = true;
            textBoxInstructions.ScrollBars = ScrollBars.Both;
            textBoxInstructions.Size = new Size(654, 130);
            textBoxInstructions.TabIndex = 0;
            // 
            // groupBoxRouting
            // 
            groupBoxRouting.Controls.Add(buttonCancelRoute);
            groupBoxRouting.Controls.Add(groupBoxProgress);
            groupBoxRouting.Controls.Add(buttonSaveRoutes);
            groupBoxRouting.Controls.Add(groupBoxOrigin);
            groupBoxRouting.Controls.Add(buttonGetRoutes);
            groupBoxRouting.Enabled = false;
            groupBoxRouting.Location = new Point(14, 553);
            groupBoxRouting.Margin = new Padding(4, 3, 4, 3);
            groupBoxRouting.Name = "groupBoxRouting";
            groupBoxRouting.Padding = new Padding(4, 3, 4, 3);
            groupBoxRouting.Size = new Size(1058, 203);
            groupBoxRouting.TabIndex = 6;
            groupBoxRouting.TabStop = false;
            groupBoxRouting.Text = "3. Routenberechnung";
            // 
            // buttonCancelRoute
            // 
            buttonCancelRoute.Enabled = false;
            buttonCancelRoute.Location = new Point(382, 68);
            buttonCancelRoute.Margin = new Padding(4, 3, 4, 3);
            buttonCancelRoute.Name = "buttonCancelRoute";
            buttonCancelRoute.Size = new Size(166, 27);
            buttonCancelRoute.TabIndex = 14;
            buttonCancelRoute.Text = "Berechnung abbrechen";
            buttonCancelRoute.UseVisualStyleBackColor = true;
            buttonCancelRoute.Click += buttonCancelRoute_Click;
            // 
            // groupBoxProgress
            // 
            groupBoxProgress.Controls.Add(labelProgressInner);
            groupBoxProgress.Controls.Add(progressBarRoutesOuter);
            groupBoxProgress.Controls.Add(labelProgressOuter);
            groupBoxProgress.Controls.Add(progressBarRoutesInner);
            groupBoxProgress.Location = new Point(554, 22);
            groupBoxProgress.Margin = new Padding(4, 3, 4, 3);
            groupBoxProgress.Name = "groupBoxProgress";
            groupBoxProgress.Padding = new Padding(4, 3, 4, 3);
            groupBoxProgress.Size = new Size(497, 175);
            groupBoxProgress.TabIndex = 13;
            groupBoxProgress.TabStop = false;
            groupBoxProgress.Text = "Status: Wartet auf Start";
            // 
            // labelProgressInner
            // 
            labelProgressInner.AutoSize = true;
            labelProgressInner.Location = new Point(8, 73);
            labelProgressInner.Margin = new Padding(4, 0, 4, 0);
            labelProgressInner.Name = "labelProgressInner";
            labelProgressInner.Size = new Size(45, 15);
            labelProgressInner.TabIndex = 13;
            labelProgressInner.Text = "0 von 0";
            // 
            // progressBarRoutesOuter
            // 
            progressBarRoutesOuter.Location = new Point(11, 38);
            progressBarRoutesOuter.Margin = new Padding(4, 3, 4, 3);
            progressBarRoutesOuter.Name = "progressBarRoutesOuter";
            progressBarRoutesOuter.Size = new Size(432, 27);
            progressBarRoutesOuter.TabIndex = 9;
            // 
            // labelProgressOuter
            // 
            labelProgressOuter.AutoSize = true;
            labelProgressOuter.Location = new Point(8, 19);
            labelProgressOuter.Margin = new Padding(4, 0, 4, 0);
            labelProgressOuter.Name = "labelProgressOuter";
            labelProgressOuter.Size = new Size(109, 15);
            labelProgressOuter.TabIndex = 12;
            labelProgressOuter.Text = "Anfangsbuchstabe:";
            // 
            // progressBarRoutesInner
            // 
            progressBarRoutesInner.Location = new Point(11, 91);
            progressBarRoutesInner.Margin = new Padding(4, 3, 4, 3);
            progressBarRoutesInner.Name = "progressBarRoutesInner";
            progressBarRoutesInner.Size = new Size(432, 27);
            progressBarRoutesInner.TabIndex = 11;
            // 
            // buttonSaveRoutes
            // 
            buttonSaveRoutes.Enabled = false;
            buttonSaveRoutes.Location = new Point(382, 164);
            buttonSaveRoutes.Margin = new Padding(4, 3, 4, 3);
            buttonSaveRoutes.Name = "buttonSaveRoutes";
            buttonSaveRoutes.Size = new Size(166, 27);
            buttonSaveRoutes.TabIndex = 10;
            buttonSaveRoutes.Text = "Routen speichern";
            buttonSaveRoutes.UseVisualStyleBackColor = true;
            buttonSaveRoutes.Click += buttonSaveRoutes_Click;
            // 
            // groupBoxOrigin
            // 
            groupBoxOrigin.Controls.Add(comboBoxState);
            groupBoxOrigin.Controls.Add(labelState);
            groupBoxOrigin.Controls.Add(labelOriginStreet);
            groupBoxOrigin.Controls.Add(textBoxOriginStreet);
            groupBoxOrigin.Controls.Add(labelOriginCity);
            groupBoxOrigin.Controls.Add(textBoxOriginCity);
            groupBoxOrigin.Location = new Point(10, 22);
            groupBoxOrigin.Margin = new Padding(4, 3, 4, 3);
            groupBoxOrigin.Name = "groupBoxOrigin";
            groupBoxOrigin.Padding = new Padding(4, 3, 4, 3);
            groupBoxOrigin.Size = new Size(364, 169);
            groupBoxOrigin.TabIndex = 3;
            groupBoxOrigin.TabStop = false;
            groupBoxOrigin.Text = "Startpunkt";
            // 
            // comboBoxState
            // 
            comboBoxState.DropDownStyle = ComboBoxStyle.DropDownList;
            comboBoxState.FormattingEnabled = true;
            comboBoxState.Items.AddRange(new object[] { "Baden-Württemberg", "Bayern", "Berlin", "Brandenburg", "Bremen", "Hamburg", "Hessen", "Mecklenburg-Vorpommern", "Niedersachsen", "Nordrhein-Westfalen", "Rheinland-Pfalz", "Saarland", "Sachsen", "Sachsen-Anhalt", "Schleswig-Holstein", "Thüringen" });
            comboBoxState.Location = new Point(8, 38);
            comboBoxState.Name = "comboBoxState";
            comboBoxState.Size = new Size(333, 23);
            comboBoxState.TabIndex = 7;
            // 
            // labelState
            // 
            labelState.AutoSize = true;
            labelState.Location = new Point(8, 19);
            labelState.Margin = new Padding(4, 0, 4, 0);
            labelState.Name = "labelState";
            labelState.Size = new Size(69, 15);
            labelState.TabIndex = 6;
            labelState.Text = "Bundesland";
            // 
            // labelOriginStreet
            // 
            labelOriginStreet.AutoSize = true;
            labelOriginStreet.Location = new Point(8, 109);
            labelOriginStreet.Margin = new Padding(4, 0, 4, 0);
            labelOriginStreet.Name = "labelOriginStreet";
            labelOriginStreet.Size = new Size(108, 15);
            labelOriginStreet.TabIndex = 4;
            labelOriginStreet.Text = "Straße und Hausnr.";
            // 
            // textBoxOriginStreet
            // 
            textBoxOriginStreet.Location = new Point(8, 128);
            textBoxOriginStreet.Margin = new Padding(4, 3, 4, 3);
            textBoxOriginStreet.Name = "textBoxOriginStreet";
            textBoxOriginStreet.Size = new Size(333, 23);
            textBoxOriginStreet.TabIndex = 3;
            // 
            // labelOriginCity
            // 
            labelOriginCity.AutoSize = true;
            labelOriginCity.Location = new Point(8, 64);
            labelOriginCity.Margin = new Padding(4, 0, 4, 0);
            labelOriginCity.Name = "labelOriginCity";
            labelOriginCity.Size = new Size(34, 15);
            labelOriginCity.TabIndex = 2;
            labelOriginCity.Text = "Stadt";
            // 
            // textBoxOriginCity
            // 
            textBoxOriginCity.Location = new Point(8, 83);
            textBoxOriginCity.Margin = new Padding(4, 3, 4, 3);
            textBoxOriginCity.Name = "textBoxOriginCity";
            textBoxOriginCity.Size = new Size(333, 23);
            textBoxOriginCity.TabIndex = 0;
            // 
            // buttonGetRoutes
            // 
            buttonGetRoutes.Location = new Point(382, 35);
            buttonGetRoutes.Margin = new Padding(4, 3, 4, 3);
            buttonGetRoutes.Name = "buttonGetRoutes";
            buttonGetRoutes.Size = new Size(166, 27);
            buttonGetRoutes.TabIndex = 1;
            buttonGetRoutes.Text = "Routen berechnen";
            buttonGetRoutes.UseVisualStyleBackColor = true;
            buttonGetRoutes.Click += buttonGetRoutes_Click;
            // 
            // groupBoxDebug
            // 
            groupBoxDebug.Controls.Add(textBoxDebug);
            groupBoxDebug.Location = new Point(14, 762);
            groupBoxDebug.Margin = new Padding(4, 3, 4, 3);
            groupBoxDebug.Name = "groupBoxDebug";
            groupBoxDebug.Padding = new Padding(4, 3, 4, 3);
            groupBoxDebug.Size = new Size(1058, 148);
            groupBoxDebug.TabIndex = 7;
            groupBoxDebug.TabStop = false;
            groupBoxDebug.Text = "Debugging";
            // 
            // textBoxDebug
            // 
            textBoxDebug.Dock = DockStyle.Fill;
            textBoxDebug.Location = new Point(4, 19);
            textBoxDebug.Margin = new Padding(4, 3, 4, 3);
            textBoxDebug.Multiline = true;
            textBoxDebug.Name = "textBoxDebug";
            textBoxDebug.ReadOnly = true;
            textBoxDebug.ScrollBars = ScrollBars.Horizontal;
            textBoxDebug.Size = new Size(1050, 126);
            textBoxDebug.TabIndex = 0;
            // 
            // FormMain
            // 
            AutoScaleDimensions = new SizeF(7F, 15F);
            AutoScaleMode = AutoScaleMode.Font;
            ClientSize = new Size(1090, 921);
            Controls.Add(groupBoxDebug);
            Controls.Add(groupBoxRouting);
            Controls.Add(groupBoxInstructions);
            Controls.Add(groupBoxCheck);
            Controls.Add(groupBoxBaseData);
            FormBorderStyle = FormBorderStyle.FixedToolWindow;
            Margin = new Padding(4, 3, 4, 3);
            Name = "FormMain";
            Text = "LinksRechtsBuch Generator";
            Load += FormMain_Load;
            groupBoxBaseData.ResumeLayout(false);
            groupBoxCheck.ResumeLayout(false);
            groupBoxInstructions.ResumeLayout(false);
            groupBoxInstructions.PerformLayout();
            groupBoxRouting.ResumeLayout(false);
            groupBoxProgress.ResumeLayout(false);
            groupBoxProgress.PerformLayout();
            groupBoxOrigin.ResumeLayout(false);
            groupBoxOrigin.PerformLayout();
            groupBoxDebug.ResumeLayout(false);
            groupBoxDebug.PerformLayout();
            ResumeLayout(false);
        }

        #endregion

        private Button buttonGetStreets;
        private GroupBox groupBoxBaseData;
        private GroupBox groupBoxCheck;
        private TabControl tabCheck;
        private Button buttonSaveStreetsJson;
        private GroupBox groupBoxInstructions;
        private TextBox textBoxInstructions;
        private Button buttonLoadStreets;
        private GroupBox groupBoxRouting;
        private TextBox textBoxOriginCity;
        private Button buttonGetRoutes;
        private GroupBox groupBoxDebug;
        private TextBox textBoxDebug;
        private Label labelOriginCity;
        private GroupBox groupBoxOrigin;
        private Label labelOriginStreet;
        private TextBox textBoxOriginStreet;
        private ProgressBar progressBarRoutesOuter;
        private Button buttonSaveRoutes;
        private ProgressBar progressBarRoutesInner;
        private GroupBox groupBoxProgress;
        private Label labelProgressOuter;
        private Label labelProgressInner;
        private Button buttonCancelRoute;
        private ComboBox comboBoxState;
        private Label labelState;
    }
}


using System;
using Gtk;
using System.IO;
using System.Reflection;

namespace VigiFolder
{
    public partial class MainWindow : Window
    {
        private Label lblFolderPath;
        private Entry txtFolderPath;
        private TextView rtbLogs;
        private Button btnStartStop;
        private Button btnClear;
        private Button btnExport;
        private Button btnSelectFolder;
        private CheckButton chkIncludeSubfolders;

        public MainWindow() : base("VigiFolder")
        {
            InitializeComponents();
            using (Stream logoStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("vigifolder.logo.png"))
            {
                Gdk.Pixbuf logo = new Gdk.Pixbuf(logoStream);
                this.Icon = logo;
            }
        }

        private void InitializeComponents()
        {
            SetDefaultSize(1060, 500);
            SetPosition(WindowPosition.Center);
            DeleteEvent += (o, args) => { Application.Quit(); };

            lblFolderPath = new Label("Nom du dossier à surveiller :");
            lblFolderPath.Xalign = 0.005f;

            txtFolderPath = new Entry();
            txtFolderPath.Text = "";

            rtbLogs = new TextView();
            rtbLogs.Editable = false;
            rtbLogs.WrapMode = WrapMode.WordChar;

            ScrolledWindow scrolledWindow = new ScrolledWindow();
            scrolledWindow.Add(rtbLogs);

            btnStartStop = new Button("Lancer l'analyse");
            btnStartStop.Clicked += BtnStartStop_Clicked;

            btnClear = new Button("Effacer le texte");
            btnClear.Clicked += BtnClear_Clicked;

            btnExport = new Button("Exporter");
            btnExport.Clicked += BtnExport_Clicked;

            btnSelectFolder = new Button("...");
            btnSelectFolder.Clicked += BtnSelectFolder_Clicked;

            chkIncludeSubfolders = new CheckButton("Inclure les sous dossiers");
            chkIncludeSubfolders.Active = true;

            VBox mainVBox = new VBox(false, 5);
            VBox folderPathVBox = new VBox(false, 5);
            HBox folderPathHBox = new HBox(false, 5);
            HBox buttonsHBox = new HBox(false, 5);

            folderPathVBox.PackStart(lblFolderPath, false, false, 5);
            folderPathVBox.PackStart(folderPathHBox, false, false, 5);

            folderPathHBox.PackStart(txtFolderPath, true, true, 5);
            folderPathHBox.PackStart(btnSelectFolder, false, false, 5);

            buttonsHBox.PackStart(btnStartStop, false, false, 5);
            buttonsHBox.PackStart(btnClear, false, false, 5);
            buttonsHBox.PackStart(btnExport, false, false, 5);
            buttonsHBox.PackStart(chkIncludeSubfolders, false, false, 5);

            mainVBox.PackStart(rtbLogs, true, true, 5);
            mainVBox.PackStart(scrolledWindow, true, true, 5);
            mainVBox.PackStart(folderPathVBox, false, false, 5);
            mainVBox.PackStart(buttonsHBox, false, false, 5);

            TextTag tagCreated = new TextTag("created");
            tagCreated.Foreground = "green";
            rtbLogs.Buffer.TagTable.Add(tagCreated);

            TextTag tagRenamed = new TextTag("renamed");
            tagRenamed.Foreground = "lightblue";
            rtbLogs.Buffer.TagTable.Add(tagRenamed);

            TextTag tagChanged = new TextTag("changed");
            tagChanged.Foreground = "orange";
            rtbLogs.Buffer.TagTable.Add(tagChanged);

            TextTag tagDeleted = new TextTag("deleted");
            tagDeleted.Foreground = "red";
            rtbLogs.Buffer.TagTable.Add(tagDeleted);

            Add(mainVBox);
            ShowAll();
        }
        private FileSystemWatcher watcher;
        private void BtnStartStop_Clicked(object sender, EventArgs e)
        {
            if (btnStartStop.Label == "Lancer l'analyse")
            {
                if (txtFolderPath.Text == "")
                {
                    MessageDialog md = new MessageDialog(this,
                        DialogFlags.DestroyWithParent, MessageType.Info,
                        ButtonsType.Close, "Le chemin spécifié est vide");
                    md.Run();
                    md.Destroy();
                    return;
                }
                try
                {
                        watcher = new FileSystemWatcher(txtFolderPath.Text);

                        watcher.NotifyFilter = NotifyFilters.FileName
                                            | NotifyFilters.LastWrite;

                        watcher.IncludeSubdirectories = chkIncludeSubfolders.Active;

                        watcher.Created += OnCreated;
                        watcher.Changed += OnChanged;
                        watcher.Deleted += OnDeleted;
                        watcher.Renamed += OnRenamed;

                        watcher.EnableRaisingEvents = true;

                        chkIncludeSubfolders.Sensitive = false;
                        btnStartStop.Label = "Arrêter l'analyse";

                }
                catch (Exception ex)
                {
                    MessageDialog md = new MessageDialog(this,
                        DialogFlags.DestroyWithParent, MessageType.Info,
                        ButtonsType.Close, "Le chemin spécifié est invalide");
                    md.Run();
                    md.Destroy();
                }
            }
            else if (btnStartStop.Label == "Arrêter l'analyse")
            {
                watcher.EnableRaisingEvents = false;
                watcher.Dispose();
                chkIncludeSubfolders.Sensitive = true;
                btnStartStop.Label = "Lancer l'analyse";
            }
        }

        private void BtnClear_Clicked(object sender, EventArgs e)
        {
            rtbLogs.Buffer.Clear();
        }

        private void BtnExport_Clicked(object sender, EventArgs e)
        {
            if (rtbLogs.Buffer.Text != "")
            {
                FileChooserDialog save = new FileChooserDialog(
                    "Choose the file to save",
                    this,
                    FileChooserAction.Save,
                    "Cancel", ResponseType.Cancel,
                    "Save", ResponseType.Accept);

                save.Filter = new FileFilter();
                save.Filter.AddPattern("*.txt");
                save.CurrentName = "logs_" + DateTime.Now.ToString("yyyy-MM-dd") + ".txt";

                if (save.Run() == (int)ResponseType.Accept)
                {
                    System.IO.File.WriteAllText(save.Filename, rtbLogs.Buffer.Text);
                }

                save.Destroy();
            }
            else
            {
                MessageDialog md = new MessageDialog(this,
                    DialogFlags.DestroyWithParent, MessageType.Info,
                    ButtonsType.Close, "Veuillez attendre que des données soient présentes avant de pouvoir les exporter");
                md.Run();
                md.Destroy();
            }
        }

        private void BtnSelectFolder_Clicked(object sender, EventArgs e)
        {
            FileChooserDialog dialog = new FileChooserDialog(
                "Choisissez un dossier",
                this,
                FileChooserAction.SelectFolder,
                "Annuler", ResponseType.Cancel,
                "Sélectionner", ResponseType.Accept);
            
            dialog.SelectMultiple = false;
            dialog.Filter = new FileFilter();
            dialog.Filter.AddPattern("*");

            if (dialog.Run() == (int)ResponseType.Accept)
            {
                txtFolderPath.Text = dialog.Filename;
            }

            dialog.Destroy();
        }
        public static void Main(string[] args)
        {
            Application.Init();
            new MainWindow();
            Application.Run();
        }
        private void OnCreated(object source, FileSystemEventArgs e)
        {
            TextIter iter = rtbLogs.Buffer.EndIter;
            rtbLogs.Buffer.InsertWithTagsByName(ref iter, $"Fichier créé à : {DateTime.Now.ToString("HH:mm:ss")} :\nChemin d'accès : {e.FullPath}\n\n", "created");
        }

        private void OnRenamed(object source, RenamedEventArgs e)
        {
            TextIter iter = rtbLogs.Buffer.EndIter;
            rtbLogs.Buffer.InsertWithTagsByName(ref iter, $"Fichier renommé à : {DateTime.Now.ToString("HH:mm:ss")} :\nChemin d'accès : {e.FullPath}\n{e.OldName} => {e.Name}\n\n", "renamed");
        }

        private DateTime lastRead = DateTime.MinValue;
        private void OnChanged(object source, FileSystemEventArgs e)
        {
            DateTime lastWriteTime = File.GetLastWriteTime(e.FullPath);

            if (lastWriteTime != lastRead)
            {
                lastRead = lastWriteTime;

                TextIter iter = rtbLogs.Buffer.EndIter;
                rtbLogs.Buffer.InsertWithTagsByName(ref iter, $"Fichier modifié à : {DateTime.Now.ToString("HH:mm:ss")} :\nChemin d'accès : {e.FullPath}\n\n", "changed");
            }
        }

        private void OnDeleted(object source, FileSystemEventArgs e)
        {
            TextIter iter = rtbLogs.Buffer.EndIter;
            rtbLogs.Buffer.InsertWithTagsByName(ref iter, $"Fichier supprimé à : {DateTime.Now.ToString("HH:mm:ss")} :\nChemin d'accès : {e.FullPath}\n\n", "deleted");
        }
        
    }
}

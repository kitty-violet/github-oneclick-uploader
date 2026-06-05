using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace GitHubUploader
{
    public class Finding
    {
        public string Severity;
        public string PathText;
        public string Message;
    }

    public class CommandResult
    {
        public int ExitCode;
        public string Output;
        public string Error;

        public string Combined
        {
            get
            {
                var text = (Output ?? "") + (string.IsNullOrWhiteSpace(Error) ? "" : Environment.NewLine + Error);
                return text.Trim();
            }
        }
    }

    public static class Program
    {
        public const string VersionText = "0.1.0";

        [STAThread]
        public static int Main(string[] args)
        {
            if (args != null && args.Length > 0)
            {
                return CliMode.Run(args);
            }

            NativeMethods.FreeConsole();
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
            return 0;
        }
    }

    internal static class NativeMethods
    {
        [DllImport("kernel32.dll")]
        public static extern bool FreeConsole();
    }

    public class MainForm : Form
    {
        private readonly TextBox folderText = new TextBox();
        private readonly TextBox repoText = new TextBox();
        private readonly TextBox descriptionText = new TextBox();
        private readonly TextBox logBox = new TextBox();
        private readonly ListView findingsList = new ListView();
        private readonly Button browseButton = new Button();
        private readonly Button checkButton = new Button();
        private readonly Button loginButton = new Button();
        private readonly Button uploadButton = new Button();
        private readonly Button openButton = new Button();
        private readonly CheckBox publicCheck = new CheckBox();
        private readonly CheckBox strictCheck = new CheckBox();
        private readonly Label statusLabel = new Label();
        private readonly Label githubStatusLabel = new Label();
        private readonly Label safetyStatusLabel = new Label();
        private readonly Label uploadStatusLabel = new Label();
        private readonly Label projectSummaryLabel = new Label();

        private readonly Color pageBack = Color.FromArgb(246, 247, 244);
        private readonly Color panelBack = Color.White;
        private readonly Color sideBack = Color.FromArgb(35, 39, 42);
        private readonly Color textMain = Color.FromArgb(31, 35, 40);
        private readonly Color textMuted = Color.FromArgb(92, 99, 106);
        private readonly Color border = Color.FromArgb(218, 223, 226);
        private readonly Color accent = Color.FromArgb(28, 116, 100);
        private readonly Color accentDark = Color.FromArgb(21, 92, 79);
        private readonly Color warn = Color.FromArgb(188, 96, 44);
        private readonly Color danger = Color.FromArgb(177, 48, 53);

        private List<Finding> lastFindings = new List<Finding>();

        public MainForm()
        {
            Text = "GitHub One-Click Uploader";
            Width = 1040;
            Height = 760;
            MinimumSize = new Size(900, 640);
            StartPosition = FormStartPosition.CenterScreen;
            AutoScaleMode = AutoScaleMode.None;
            BackColor = pageBack;
            Font = new Font("Segoe UI", 9);

            BuildLayout();
        }

        private void BuildLayout()
        {
            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.BackColor = pageBack;
            root.RowCount = 1;
            root.ColumnCount = 2;
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 196));
            root.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            Controls.Add(root);

            var sidebar = BuildSidebar();
            root.Controls.Add(sidebar, 0, 0);

            var page = new TableLayoutPanel();
            page.Dock = DockStyle.Fill;
            page.Padding = new Padding(18, 16, 18, 16);
            page.BackColor = pageBack;
            page.RowCount = 4;
            page.ColumnCount = 1;
            page.RowStyles.Add(new RowStyle(SizeType.Absolute, 82));
            page.RowStyles.Add(new RowStyle(SizeType.Absolute, 100));
            page.RowStyles.Add(new RowStyle(SizeType.Absolute, 192));
            page.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            root.Controls.Add(page, 1, 0);

            page.Controls.Add(BuildHeader(), 0, 0);
            page.Controls.Add(BuildStatusCards(), 0, 1);
            page.Controls.Add(BuildProjectCard(), 0, 2);
            page.Controls.Add(BuildWorkArea(), 0, 3);

            statusLabel.Text = "Ready. Choose a project folder, then run a safety check.";
            UpdateSummaryLabels("No project selected.", "Not checked", "No scan yet", "Waiting");
        }

        private Control BuildSidebar()
        {
            var side = new Panel();
            side.Dock = DockStyle.Fill;
            side.BackColor = sideBack;
            side.Padding = new Padding(18, 20, 18, 18);

            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.RowCount = 6;
            layout.ColumnCount = 1;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 50));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 72));
            side.Controls.Add(layout);

            var brand = new Label();
            brand.Dock = DockStyle.Fill;
            brand.Text = "GitHub\r\nUploader";
            brand.ForeColor = Color.White;
            brand.Font = new Font("Segoe UI", 16, FontStyle.Bold);
            brand.TextAlign = ContentAlignment.MiddleLeft;
            layout.Controls.Add(brand, 0, 0);

            layout.Controls.Add(MakeSidebarLabel("1  Select project"), 0, 1);
            layout.Controls.Add(MakeSidebarLabel("2  Check safety"), 0, 2);
            layout.Controls.Add(MakeSidebarLabel("3  Upload"), 0, 3);

            var footer = new Label();
            footer.Dock = DockStyle.Fill;
            footer.ForeColor = Color.FromArgb(176, 184, 190);
            footer.Font = new Font("Segoe UI", 8);
            footer.Text = "Local Git and GitHub CLI.\r\nNo token storage.";
            footer.TextAlign = ContentAlignment.BottomLeft;
            layout.Controls.Add(footer, 0, 5);

            return side;
        }

        private Control BuildHeader()
        {
            var panel = new TableLayoutPanel();
            panel.Dock = DockStyle.Fill;
            panel.ColumnCount = 2;
            panel.RowCount = 2;
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            panel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 220));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 58));
            panel.RowStyles.Add(new RowStyle(SizeType.Percent, 42));

            var title = new Label();
            title.Dock = DockStyle.Fill;
            title.Text = "One-click project publishing";
            title.Font = new Font("Segoe UI", 19, FontStyle.Bold);
            title.ForeColor = textMain;
            title.TextAlign = ContentAlignment.BottomLeft;
            panel.Controls.Add(title, 0, 0);

            var subtitle = new Label();
            subtitle.Dock = DockStyle.Fill;
            subtitle.Text = "Check secrets, keep hidden config files, and push to GitHub from a polished desktop workflow.";
            subtitle.Font = new Font("Segoe UI", 9);
            subtitle.ForeColor = textMuted;
            subtitle.TextAlign = ContentAlignment.TopLeft;
            panel.Controls.Add(subtitle, 0, 1);

            statusLabel.Dock = DockStyle.Fill;
            statusLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            statusLabel.ForeColor = accentDark;
            statusLabel.TextAlign = ContentAlignment.MiddleRight;
            panel.Controls.Add(statusLabel, 1, 0);
            panel.SetRowSpan(statusLabel, 2);
            return panel;
        }

        private Control BuildStatusCards()
        {
            var grid = new TableLayoutPanel();
            grid.Dock = DockStyle.Fill;
            grid.ColumnCount = 3;
            grid.RowCount = 1;
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.33f));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 33.34f));
            grid.Controls.Add(MakeInfoCard("GitHub", githubStatusLabel), 0, 0);
            grid.Controls.Add(MakeInfoCard("Safety", safetyStatusLabel), 1, 0);
            grid.Controls.Add(MakeInfoCard("Upload", uploadStatusLabel), 2, 0);
            return grid;
        }

        private Control BuildProjectCard()
        {
            var card = MakeCard();
            card.Padding = new Padding(18, 16, 18, 16);

            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.ColumnCount = 4;
            layout.RowCount = 5;
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 104));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
            layout.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 118));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 34));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 40));
            card.Controls.Add(layout);

            var title = new Label();
            title.Dock = DockStyle.Fill;
            title.Text = "Project setup";
            title.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            title.ForeColor = textMain;
            title.TextAlign = ContentAlignment.MiddleLeft;
            layout.Controls.Add(title, 0, 0);
            layout.SetColumnSpan(title, 4);

            AddLabel(layout, "Folder", 0, 1);
            folderText.Dock = DockStyle.Fill;
            folderText.Font = new Font("Segoe UI", 9);
            folderText.BorderStyle = BorderStyle.FixedSingle;
            folderText.TextChanged += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(repoText.Text) && Directory.Exists(folderText.Text))
                {
                    repoText.Text = SuggestRepoName(folderText.Text);
                }
                RefreshProjectSummary();
            };
            layout.Controls.Add(folderText, 1, 1);

            browseButton.Text = "Browse";
            StyleButton(browseButton, false);
            browseButton.Dock = DockStyle.Fill;
            browseButton.Click += (s, e) => BrowseFolder();
            layout.Controls.Add(browseButton, 2, 1);

            openButton.Text = "Open";
            StyleButton(openButton, false);
            openButton.Dock = DockStyle.Fill;
            openButton.Click += (s, e) => OpenProjectFolder();
            layout.Controls.Add(openButton, 3, 1);

            AddLabel(layout, "Repository", 0, 2);
            repoText.Dock = DockStyle.Fill;
            repoText.Font = new Font("Segoe UI", 9);
            repoText.BorderStyle = BorderStyle.FixedSingle;
            repoText.TextChanged += (s, e) => RefreshProjectSummary();
            layout.Controls.Add(repoText, 1, 2);

            publicCheck.Text = "Public repo";
            publicCheck.Checked = true;
            publicCheck.Dock = DockStyle.Fill;
            publicCheck.ForeColor = textMain;
            publicCheck.CheckedChanged += (s, e) => RefreshProjectSummary();
            layout.Controls.Add(publicCheck, 2, 2);

            strictCheck.Text = "Strict scan";
            strictCheck.Checked = true;
            strictCheck.Dock = DockStyle.Fill;
            strictCheck.ForeColor = textMain;
            layout.Controls.Add(strictCheck, 3, 2);

            AddLabel(layout, "Description", 0, 3);
            descriptionText.Dock = DockStyle.Fill;
            descriptionText.Font = new Font("Segoe UI", 9);
            descriptionText.BorderStyle = BorderStyle.FixedSingle;
            layout.Controls.Add(descriptionText, 1, 3);
            layout.SetColumnSpan(descriptionText, 3);

            var actionPanel = new FlowLayoutPanel();
            actionPanel.Dock = DockStyle.Fill;
            actionPanel.FlowDirection = FlowDirection.LeftToRight;
            actionPanel.WrapContents = false;
            actionPanel.Padding = new Padding(0, 5, 0, 0);
            layout.Controls.Add(actionPanel, 1, 4);
            layout.SetColumnSpan(actionPanel, 3);

            checkButton.Text = "Check project";
            checkButton.Width = 132;
            checkButton.Height = 32;
            StyleButton(checkButton, false);
            checkButton.Click += async (s, e) => await CheckProject();
            actionPanel.Controls.Add(checkButton);

            loginButton.Text = "GitHub Login";
            loginButton.Width = 132;
            loginButton.Height = 32;
            StyleButton(loginButton, false);
            loginButton.Click += async (s, e) => await StartGitHubLogin();
            actionPanel.Controls.Add(loginButton);

            uploadButton.Text = "Upload now";
            uploadButton.Width = 132;
            uploadButton.Height = 32;
            StyleButton(uploadButton, true);
            uploadButton.Click += async (s, e) => await UploadProject();
            actionPanel.Controls.Add(uploadButton);

            projectSummaryLabel.Dock = DockStyle.Fill;
            projectSummaryLabel.ForeColor = textMuted;
            projectSummaryLabel.Font = new Font("Segoe UI", 8);
            projectSummaryLabel.TextAlign = ContentAlignment.MiddleLeft;
            layout.Controls.Add(projectSummaryLabel, 0, 4);

            return card;
        }

        private Control BuildWorkArea()
        {
            var grid = new TableLayoutPanel();
            grid.Dock = DockStyle.Fill;
            grid.ColumnCount = 2;
            grid.RowCount = 1;
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 57));
            grid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 43));

            SetupFindingsList();
            grid.Controls.Add(WrapInCard("Safety findings", "High findings block uploads. Medium findings block in strict mode.", findingsList), 0, 0);

            logBox.Dock = DockStyle.Fill;
            logBox.Multiline = true;
            logBox.ReadOnly = true;
            logBox.ScrollBars = ScrollBars.Vertical;
            logBox.Font = new Font("Consolas", 9);
            logBox.BorderStyle = BorderStyle.None;
            logBox.BackColor = Color.FromArgb(251, 252, 250);
            logBox.ForeColor = textMain;
            grid.Controls.Add(WrapInCard("Activity log", "Git and GitHub CLI output stays local on this machine.", logBox), 1, 0);

            return grid;
        }

        private void AddLabel(TableLayoutPanel parent, string text, int column, int row)
        {
            var label = new Label();
            label.Text = text;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            label.ForeColor = textMuted;
            parent.Controls.Add(label, column, row);
        }

        private Label MakeSidebarLabel(string text)
        {
            var label = new Label();
            label.Dock = DockStyle.Fill;
            label.Text = text;
            label.ForeColor = Color.FromArgb(225, 229, 232);
            label.Font = new Font("Segoe UI", 10, FontStyle.Bold);
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Padding = new Padding(4, 0, 0, 0);
            return label;
        }

        private BorderedPanel MakeCard()
        {
            var panel = new BorderedPanel();
            panel.Dock = DockStyle.Fill;
            panel.Margin = new Padding(0, 0, 0, 14);
            panel.BackColor = panelBack;
            panel.BorderColor = border;
            return panel;
        }

        private Control MakeInfoCard(string title, Label valueLabel)
        {
            var card = MakeCard();
            card.Margin = new Padding(0, 0, 12, 14);
            card.Padding = new Padding(16, 12, 16, 12);

            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.RowCount = 2;
            layout.ColumnCount = 1;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            card.Controls.Add(layout);

            var titleLabel = new Label();
            titleLabel.Dock = DockStyle.Fill;
            titleLabel.Text = title;
            titleLabel.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            titleLabel.ForeColor = textMuted;
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            layout.Controls.Add(titleLabel, 0, 0);

            valueLabel.Dock = DockStyle.Fill;
            valueLabel.Font = new Font("Segoe UI", 13, FontStyle.Bold);
            valueLabel.ForeColor = textMain;
            valueLabel.TextAlign = ContentAlignment.MiddleLeft;
            layout.Controls.Add(valueLabel, 0, 1);
            return card;
        }

        private Control WrapInCard(string title, string subtitle, Control content)
        {
            var card = MakeCard();
            card.Margin = new Padding(0, 0, 14, 0);
            card.Padding = new Padding(16, 14, 16, 16);

            var layout = new TableLayoutPanel();
            layout.Dock = DockStyle.Fill;
            layout.RowCount = 3;
            layout.ColumnCount = 1;
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 28));
            layout.RowStyles.Add(new RowStyle(SizeType.Absolute, 26));
            layout.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
            card.Controls.Add(layout);

            var titleLabel = new Label();
            titleLabel.Dock = DockStyle.Fill;
            titleLabel.Text = title;
            titleLabel.Font = new Font("Segoe UI", 12, FontStyle.Bold);
            titleLabel.ForeColor = textMain;
            titleLabel.TextAlign = ContentAlignment.MiddleLeft;
            layout.Controls.Add(titleLabel, 0, 0);

            var subtitleLabel = new Label();
            subtitleLabel.Dock = DockStyle.Fill;
            subtitleLabel.Text = subtitle;
            subtitleLabel.Font = new Font("Segoe UI", 8);
            subtitleLabel.ForeColor = textMuted;
            subtitleLabel.TextAlign = ContentAlignment.MiddleLeft;
            layout.Controls.Add(subtitleLabel, 0, 1);

            content.Margin = new Padding(0, 8, 0, 0);
            layout.Controls.Add(content, 0, 2);
            return card;
        }

        private void StyleButton(Button button, bool primary)
        {
            button.FlatStyle = FlatStyle.Flat;
            button.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            button.Margin = new Padding(0, 0, 10, 0);
            button.UseVisualStyleBackColor = false;
            button.BackColor = primary ? accent : Color.FromArgb(239, 242, 240);
            button.ForeColor = primary ? Color.White : textMain;
            button.FlatAppearance.BorderSize = 1;
            button.FlatAppearance.BorderColor = primary ? accentDark : border;
        }

        private void UpdateSummaryLabels(string project, string github, string safety, string upload)
        {
            projectSummaryLabel.Text = project;
            githubStatusLabel.Text = github;
            safetyStatusLabel.Text = safety;
            uploadStatusLabel.Text = upload;
        }

        private void RefreshProjectSummary()
        {
            var repo = NormalizeRepoName(repoText.Text);
            var visibility = publicCheck.Checked ? "Public" : "Private";
            projectSummaryLabel.Text = string.IsNullOrWhiteSpace(repo)
                ? "Repository name will be suggested from the selected folder."
                : visibility + " repository: " + repo;
        }

        private void SetupFindingsList()
        {
            findingsList.Dock = DockStyle.Fill;
            findingsList.View = View.Details;
            findingsList.FullRowSelect = true;
            findingsList.GridLines = true;
            findingsList.HideSelection = false;
            findingsList.Font = new Font("Segoe UI", 9);
            findingsList.BorderStyle = BorderStyle.None;
            findingsList.BackColor = Color.FromArgb(251, 252, 250);
            findingsList.ForeColor = textMain;
            findingsList.Columns.Add("Severity", 90);
            findingsList.Columns.Add("Message", 430);
            findingsList.Columns.Add("Path", 560);
        }

        private void BrowseFolder()
        {
            using (var dialog = new FolderBrowserDialog())
            {
                dialog.Description = "Choose the project folder to upload.";
                dialog.ShowNewFolderButton = false;
                if (Directory.Exists(folderText.Text)) dialog.SelectedPath = folderText.Text;
                if (dialog.ShowDialog(this) == DialogResult.OK)
                {
                    folderText.Text = dialog.SelectedPath;
                    repoText.Text = SuggestRepoName(dialog.SelectedPath);
                }
            }
        }

        private void OpenProjectFolder()
        {
            if (Directory.Exists(folderText.Text))
            {
                Process.Start("explorer.exe", folderText.Text);
            }
        }

        private async Task CheckProject()
        {
            SetBusy(true);
            try
            {
                await RunChecks();
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task UploadProject()
        {
            SetBusy(true);
            try
            {
                uploadStatusLabel.Text = "Preparing";
                var checkOk = await RunChecks();
                if (!checkOk) return;

                var folder = NormalizeFolder(folderText.Text);
                var repo = NormalizeRepoName(repoText.Text);
                if (string.IsNullOrWhiteSpace(repo))
                {
                    MessageBox.Show("Repository name is required.", "Upload", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var visibility = publicCheck.Checked ? "public" : "private";
                var message = "Upload this folder to GitHub?\r\n\r\n" +
                              folder + "\r\n\r\nRepository: " + repo + "\r\nVisibility: " + visibility +
                              "\r\n\r\nThe app will create or update local Git metadata and push commits to GitHub.";
                if (MessageBox.Show(message, "Confirm upload", MessageBoxButtons.YesNo, MessageBoxIcon.Warning) != DialogResult.Yes)
                {
                    AppendLog("Upload cancelled.");
                    uploadStatusLabel.Text = "Cancelled";
                    return;
                }

                var isPublic = publicCheck.Checked;
                var description = descriptionText.Text.Trim();
                var request = new UploadRequest
                {
                    ProjectFolder = folder,
                    RepoName = repo,
                    IsPublic = isPublic,
                    Description = description,
                    Strict = strictCheck.Checked
                };
                uploadStatusLabel.Text = "Uploading";
                var uploadedUrl = await Task.Run(() => UploadEngine.Upload(request, AppendLog));
                uploadStatusLabel.Text = "Published";
                statusLabel.Text = "Uploaded: " + uploadedUrl;
                if (MessageBox.Show("Upload finished.\r\n\r\nOpen repository?", "GitHub Uploader", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    Process.Start(uploadedUrl);
                }
            }
            catch (Exception ex)
            {
                AppendLog("Upload failed: " + ex.Message);
                uploadStatusLabel.Text = "Failed";
                statusLabel.Text = "Upload failed.";
                MessageBox.Show(ex.Message, "Upload failed", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                SetBusy(false);
            }
        }

        private async Task<bool> RunChecks()
        {
            logBox.Clear();
            findingsList.Items.Clear();
            lastFindings = new List<Finding>();
            githubStatusLabel.Text = "Checking";
            safetyStatusLabel.Text = "Scanning";
            uploadStatusLabel.Text = "Waiting";

            var folder = NormalizeFolder(folderText.Text);
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                AddFinding("High", folderText.Text, "Project folder does not exist.");
                RenderFindings();
                safetyStatusLabel.Text = "Blocked";
                githubStatusLabel.Text = "Not checked";
                uploadStatusLabel.Text = "Blocked";
                statusLabel.Text = "Check failed.";
                return false;
            }

            if (IsDangerousRoot(folder))
            {
                AddFinding("High", folder, "Refusing to upload a drive root or user profile root.");
            }

            var repo = NormalizeRepoName(repoText.Text);
            if (string.IsNullOrWhiteSpace(repo))
            {
                AddFinding("High", repoText.Text, "Repository name is empty or invalid.");
            }
            else
            {
                repoText.Text = repo;
            }

            AppendLog("Project: " + folder);
            AppendLog("Repository: " + repo);

            var git = CommandRunner.FindOnPath("git.exe");
            if (git == null) AddFinding("High", "git.exe", "Git is not installed or not on PATH.");
            else AppendLog("Git: " + git);

            var gh = FindGitHubCli();
            if (gh == null)
            {
                AddFinding("High", "gh.exe", "GitHub CLI is not installed. Install with: winget install --id GitHub.cli -e");
                githubStatusLabel.Text = "CLI missing";
            }
            else AppendLog("GitHub CLI: " + gh);

            AppendLog("Scanning for risky files and secrets...");
            lastFindings.AddRange(SafetyScanner.Scan(folder));

            if (gh != null)
            {
                var auth = await Task.Run(() => CommandRunner.Run(gh, "auth status", folder, 30000, true));
                if (auth.ExitCode != 0)
                {
                    AddFinding("High", "GitHub CLI", "Not logged in. Click GitHub Login, authorize in browser, then check again.");
                    githubStatusLabel.Text = "Login needed";
                }
                else
                {
                    AppendLog(auth.Combined);
                    githubStatusLabel.Text = "Connected";
                    if (!auth.Combined.Contains("workflow"))
                    {
                        AddFinding("Medium", "GitHub CLI", "Token may not include workflow scope. Login again with repo,workflow scopes if pushing .github workflows fails.");
                    }
                }
            }

            RenderFindings();
            var blocking = HasBlockingFindings();
            safetyStatusLabel.Text = blocking ? "Needs review" : "Clear";
            uploadStatusLabel.Text = blocking ? "Blocked" : "Ready";
            statusLabel.Text = blocking ? "Check found blocking issues." : "Check passed.";
            AppendLog(blocking ? "Check failed." : "Check passed.");
            return !blocking;
        }

        private async Task StartGitHubLogin()
        {
            var gh = FindGitHubCli();
            if (gh == null)
            {
                MessageBox.Show("GitHub CLI is not installed.\r\n\r\nInstall with:\r\nwinget install --id GitHub.cli -e", "GitHub Login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var status = await Task.Run(() => CommandRunner.Run(gh, "auth status", AppDomain.CurrentDomain.BaseDirectory, 30000, true));
            if (status.ExitCode == 0)
            {
                githubStatusLabel.Text = "Connected";
                MessageBox.Show("GitHub CLI is already logged in.\r\n\r\nYou can click Check or Upload now.", "GitHub Login", MessageBoxButtons.OK, MessageBoxIcon.Information);
                AppendLog(status.Combined);
                return;
            }

            var command = "& '" + gh.Replace("'", "''") + "' auth login --hostname github.com --web --clipboard --git-protocol https --scopes repo,workflow";
            Process.Start(new ProcessStartInfo
            {
                FileName = "powershell.exe",
                Arguments = "-NoProfile -NoExit -Command \"" + command.Replace("\"", "\\\"") + "\"",
                UseShellExecute = true
            });
            AppendLog("Started GitHub login. The one-time code is copied to the clipboard by gh.");
            githubStatusLabel.Text = "Authorizing";
            statusLabel.Text = "Finish GitHub login in the opened PowerShell/browser, then click Check.";

            await Task.Delay(2500);
            var code = "";
            try
            {
                if (Clipboard.ContainsText()) code = Clipboard.GetText().Trim();
            }
            catch { }

            var text = "GitHub has opened an authorization page.\r\n\r\n" +
                       "When the page asks for a code, paste the code copied by GitHub CLI.";
            if (Regex.IsMatch(code, "^[A-Z0-9]{4}-[A-Z0-9]{4}$", RegexOptions.IgnoreCase))
            {
                text += "\r\n\r\nCurrent device code: " + code;
            }
            text += "\r\n\r\nIf this code expires, click GitHub Login again.";
            MessageBox.Show(text, "GitHub Login Code", MessageBoxButtons.OK, MessageBoxIcon.Information);
        }

        private bool HasBlockingFindings()
        {
            if (!strictCheck.Checked) return lastFindings.Any(x => x.Severity == "High");
            return lastFindings.Any(x => x.Severity == "High" || x.Severity == "Medium");
        }

        private void RenderFindings()
        {
            findingsList.BeginUpdate();
            findingsList.Items.Clear();
            foreach (var finding in lastFindings.OrderBy(x => SeverityRank(x.Severity)).ThenBy(x => x.PathText))
            {
                var item = new ListViewItem(finding.Severity);
                item.SubItems.Add(finding.Message);
                item.SubItems.Add(finding.PathText);
                if (finding.Severity == "High") item.ForeColor = Color.FromArgb(170, 40, 35);
                else if (finding.Severity == "Medium") item.ForeColor = Color.FromArgb(150, 90, 20);
                findingsList.Items.Add(item);
            }
            findingsList.EndUpdate();
        }

        private int SeverityRank(string severity)
        {
            if (severity == "High") return 0;
            if (severity == "Medium") return 1;
            return 2;
        }

        private void AddFinding(string severity, string path, string message)
        {
            lastFindings.Add(new Finding { Severity = severity, PathText = path ?? "", Message = message ?? "" });
        }

        private void AppendLog(string text)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action<string>(AppendLog), text);
                return;
            }
            logBox.AppendText("[" + DateTime.Now.ToString("HH:mm:ss", CultureInfo.InvariantCulture) + "] " + text + Environment.NewLine);
        }

        private void SetBusy(bool busy)
        {
            browseButton.Enabled = !busy;
            checkButton.Enabled = !busy;
            loginButton.Enabled = !busy;
            uploadButton.Enabled = !busy;
            openButton.Enabled = !busy;
            Cursor = busy ? Cursors.WaitCursor : Cursors.Default;
        }

        private static string NormalizeFolder(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            try { return Path.GetFullPath(text.Trim().Trim('"')); }
            catch { return text.Trim(); }
        }

        private static string SuggestRepoName(string folder)
        {
            try { return NormalizeRepoName(new DirectoryInfo(folder).Name); }
            catch { return ""; }
        }

        private static string NormalizeRepoName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            var lower = value.Trim().ToLowerInvariant();
            var sb = new StringBuilder();
            foreach (var ch in lower)
            {
                if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '-' || ch == '_' || ch == '.')
                {
                    sb.Append(ch);
                }
                else if (char.IsWhiteSpace(ch))
                {
                    sb.Append('-');
                }
            }
            return sb.ToString().Trim('.', '-', '_');
        }

        private static bool IsDangerousRoot(string folder)
        {
            var full = Path.GetFullPath(folder).TrimEnd('\\');
            var root = Path.GetPathRoot(full).TrimEnd('\\');
            if (string.Equals(full, root, StringComparison.OrdinalIgnoreCase)) return true;

            var user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).TrimEnd('\\');
            return string.Equals(full, user, StringComparison.OrdinalIgnoreCase);
        }

        private static string FindGitHubCli()
        {
            var path = CommandRunner.FindOnPath("gh.exe");
            if (path != null) return path;
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"GitHub CLI\gh.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\WinGet\Links\gh.exe")
            };
            return candidates.FirstOrDefault(File.Exists);
        }

    }

    public class BorderedPanel : Panel
    {
        public Color BorderColor { get; set; }

        public BorderedPanel()
        {
            BorderColor = Color.FromArgb(218, 223, 226);
            DoubleBuffered = true;
        }

        protected override void OnPaint(PaintEventArgs e)
        {
            base.OnPaint(e);
            using (var pen = new Pen(BorderColor))
            {
                var rect = ClientRectangle;
                rect.Width -= 1;
                rect.Height -= 1;
                e.Graphics.DrawRectangle(pen, rect);
            }
        }
    }

    public class UploadRequest
    {
        public string ProjectFolder;
        public string RepoName;
        public bool IsPublic = true;
        public string Description = "";
        public bool Strict = true;
    }

    public class CheckResult
    {
        public readonly List<Finding> Findings = new List<Finding>();
        public string GitPath;
        public string GitHubCliPath;
        public string GitHubAuthStatus;

        public bool HasBlockingFindings(bool strict)
        {
            if (strict) return Findings.Any(x => x.Severity == "High" || x.Severity == "Medium");
            return Findings.Any(x => x.Severity == "High");
        }
    }

    public static class CliMode
    {
        public static int Run(string[] args)
        {
            try
            {
                var command = args[0].Trim().ToLowerInvariant();
                if (command == "--self-test") return SelfTest.Run();
                if (command == "--version" || command == "version")
                {
                    Console.WriteLine("GitHub One-Click Uploader " + Program.VersionText);
                    return 0;
                }
                if (command == "--help" || command == "-h" || command == "help")
                {
                    PrintHelp();
                    return 0;
                }
                if (command == "login") return RunLogin();
                if (command == "check") return RunCheck(args.Skip(1).ToArray());
                if (command == "upload") return RunUpload(args.Skip(1).ToArray());

                Console.Error.WriteLine("Unknown command: " + args[0]);
                PrintHelp();
                return 2;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine("Error: " + ex.Message);
                return 1;
            }
        }

        private static int RunLogin()
        {
            var gh = Tooling.FindGitHubCli();
            if (gh == null)
            {
                Console.Error.WriteLine("GitHub CLI was not found. Install with: winget install --id GitHub.cli -e");
                return 2;
            }

            Console.WriteLine("GitHub will open an authorization page.");
            Console.WriteLine("The device code is copied to your clipboard by GitHub CLI.");
            Console.WriteLine("When GitHub asks for a code, press Ctrl+V.");
            Console.WriteLine();
            return CommandRunner.RunInteractive(gh, "auth login --hostname github.com --web --clipboard --git-protocol https --scopes repo,workflow", Environment.CurrentDirectory);
        }

        private static int RunCheck(string[] args)
        {
            var request = ParseRequest(args, false);
            var result = UploadEngine.Check(request, Console.WriteLine);
            PrintFindings(result);
            var blocking = result.HasBlockingFindings(request.Strict);
            Console.WriteLine(blocking ? "Check failed." : "Check passed.");
            return blocking ? 3 : 0;
        }

        private static int RunUpload(string[] args)
        {
            var yes = HasFlag(args, "--yes") || HasFlag(args, "-y");
            var request = ParseRequest(args, true);

            if (!yes)
            {
                Console.WriteLine("About to upload:");
                Console.WriteLine("  Project: " + request.ProjectFolder);
                Console.WriteLine("  Repo:    " + request.RepoName);
                Console.WriteLine("  Public:  " + request.IsPublic);
                Console.WriteLine();
                Console.Write("Type UPLOAD to continue: ");
                var text = Console.ReadLine();
                if (!string.Equals(text, "UPLOAD", StringComparison.Ordinal))
                {
                    Console.WriteLine("Cancelled.");
                    return 4;
                }
            }

            var url = UploadEngine.Upload(request, Console.WriteLine);
            Console.WriteLine("Uploaded: " + url);
            return 0;
        }

        private static UploadRequest ParseRequest(string[] args, bool requireRepo)
        {
            var request = new UploadRequest();
            request.ProjectFolder = ReadOption(args, "--project") ?? ReadOption(args, "-p") ?? "";
            request.ProjectFolder = Tooling.NormalizeFolder(request.ProjectFolder);
            request.RepoName = ReadOption(args, "--repo") ?? ReadOption(args, "-r") ?? "";
            request.Description = ReadOption(args, "--description") ?? ReadOption(args, "-d") ?? "";
            request.IsPublic = !HasFlag(args, "--private");
            if (HasFlag(args, "--public")) request.IsPublic = true;
            request.Strict = !HasFlag(args, "--allow-medium");
            if (HasFlag(args, "--strict")) request.Strict = true;

            if (string.IsNullOrWhiteSpace(request.ProjectFolder))
            {
                throw new ArgumentException("--project is required.");
            }
            if (!Directory.Exists(request.ProjectFolder))
            {
                throw new DirectoryNotFoundException("Project folder does not exist: " + request.ProjectFolder);
            }

            if (string.IsNullOrWhiteSpace(request.RepoName))
            {
                request.RepoName = Tooling.SuggestRepoName(request.ProjectFolder);
            }
            request.RepoName = Tooling.NormalizeRepoName(request.RepoName);
            if (requireRepo && string.IsNullOrWhiteSpace(request.RepoName))
            {
                throw new ArgumentException("--repo is required.");
            }
            return request;
        }

        private static bool HasFlag(string[] args, string name)
        {
            return args.Any(x => string.Equals(x, name, StringComparison.OrdinalIgnoreCase));
        }

        private static string ReadOption(string[] args, string name)
        {
            for (var i = 0; i < args.Length; i++)
            {
                if (string.Equals(args[i], name, StringComparison.OrdinalIgnoreCase))
                {
                    if (i + 1 >= args.Length) throw new ArgumentException("Missing value for " + name);
                    return args[i + 1];
                }
                if (args[i].StartsWith(name + "=", StringComparison.OrdinalIgnoreCase))
                {
                    return args[i].Substring(name.Length + 1);
                }
            }
            return null;
        }

        private static void PrintFindings(CheckResult result)
        {
            Console.WriteLine();
            if (!string.IsNullOrWhiteSpace(result.GitPath)) Console.WriteLine("Git: " + result.GitPath);
            if (!string.IsNullOrWhiteSpace(result.GitHubCliPath)) Console.WriteLine("GitHub CLI: " + result.GitHubCliPath);
            if (!string.IsNullOrWhiteSpace(result.GitHubAuthStatus)) Console.WriteLine(result.GitHubAuthStatus);
            Console.WriteLine();

            if (result.Findings.Count == 0)
            {
                Console.WriteLine("No findings.");
                return;
            }

            foreach (var finding in result.Findings.OrderBy(x => x.Severity).ThenBy(x => x.PathText))
            {
                Console.WriteLine("[" + finding.Severity + "] " + finding.Message + " | " + finding.PathText);
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine("GitHub One-Click Uploader");
            Console.WriteLine("Version: " + Program.VersionText);
            Console.WriteLine();
            Console.WriteLine("GUI:");
            Console.WriteLine("  GitHubOneClickUploader.exe");
            Console.WriteLine();
            Console.WriteLine("Login:");
            Console.WriteLine("  GitHubOneClickUploader.exe login");
            Console.WriteLine();
            Console.WriteLine("Check:");
            Console.WriteLine("  GitHubOneClickUploader.exe check --project \"<project-path>\" --repo my-app");
            Console.WriteLine();
            Console.WriteLine("Upload:");
            Console.WriteLine("  GitHubOneClickUploader.exe upload --project \"<project-path>\" --repo my-app --public --yes");
            Console.WriteLine("  GitHubOneClickUploader.exe upload -p \"<project-path>\" -r my-app --private --description \"My app\"");
            Console.WriteLine();
            Console.WriteLine("Options:");
            Console.WriteLine("  --project, -p       Project folder to upload.");
            Console.WriteLine("  --repo, -r          GitHub repository name. Defaults to folder name.");
            Console.WriteLine("  --public            Create public repository. Default.");
            Console.WriteLine("  --private           Create private repository.");
            Console.WriteLine("  --description, -d   Repository description.");
            Console.WriteLine("  --strict            Block high and medium findings. Default.");
            Console.WriteLine("  --allow-medium      Block high findings only.");
            Console.WriteLine("  --yes, -y           Upload without typing UPLOAD.");
        }
    }

    public static class UploadEngine
    {
        public static CheckResult Check(UploadRequest request, Action<string> log)
        {
            var result = new CheckResult();
            log = log ?? (_ => { });

            request.ProjectFolder = Tooling.NormalizeFolder(request.ProjectFolder);
            request.RepoName = Tooling.NormalizeRepoName(request.RepoName);

            log("Project: " + request.ProjectFolder);
            log("Repository: " + request.RepoName);

            if (string.IsNullOrWhiteSpace(request.ProjectFolder) || !Directory.Exists(request.ProjectFolder))
            {
                result.Findings.Add(new Finding { Severity = "High", PathText = request.ProjectFolder ?? "", Message = "Project folder does not exist." });
                return result;
            }
            if (Tooling.IsDangerousRoot(request.ProjectFolder))
            {
                result.Findings.Add(new Finding { Severity = "High", PathText = request.ProjectFolder, Message = "Refusing to upload a drive root or user profile root." });
            }
            if (string.IsNullOrWhiteSpace(request.RepoName))
            {
                result.Findings.Add(new Finding { Severity = "High", PathText = request.RepoName ?? "", Message = "Repository name is empty or invalid." });
            }

            result.GitPath = CommandRunner.FindOnPath("git.exe");
            if (result.GitPath == null) result.Findings.Add(new Finding { Severity = "High", PathText = "git.exe", Message = "Git is not installed or not on PATH." });

            result.GitHubCliPath = Tooling.FindGitHubCli();
            if (result.GitHubCliPath == null)
            {
                result.Findings.Add(new Finding { Severity = "High", PathText = "gh.exe", Message = "GitHub CLI is not installed. Install with: winget install --id GitHub.cli -e" });
            }

            log("Scanning for risky files and secrets...");
            result.Findings.AddRange(SafetyScanner.Scan(request.ProjectFolder));

            if (result.GitHubCliPath != null)
            {
                var auth = CommandRunner.Run(result.GitHubCliPath, "auth status", request.ProjectFolder, 30000, true);
                if (auth.ExitCode != 0)
                {
                    result.Findings.Add(new Finding { Severity = "High", PathText = "GitHub CLI", Message = "Not logged in. Run: GitHubOneClickUploader.exe login" });
                }
                else
                {
                    result.GitHubAuthStatus = auth.Combined;
                    if (!auth.Combined.Contains("workflow"))
                    {
                        result.Findings.Add(new Finding { Severity = "Medium", PathText = "GitHub CLI", Message = "Token may not include workflow scope." });
                    }
                }
            }

            return result;
        }

        public static string Upload(UploadRequest request, Action<string> log)
        {
            log = log ?? (_ => { });
            var check = Check(request, log);
            if (check.HasBlockingFindings(request.Strict))
            {
                throw new InvalidOperationException("Safety check failed. Run check to see findings.");
            }

            var folder = Tooling.NormalizeFolder(request.ProjectFolder);
            var repo = Tooling.NormalizeRepoName(request.RepoName);
            var gh = Tooling.FindGitHubCli();
            if (gh == null) throw new InvalidOperationException("GitHub CLI was not found.");

            log("Adding safe .gitignore defaults...");
            Tooling.EnsureDefaultGitIgnore(folder);

            if (!Directory.Exists(Path.Combine(folder, ".git")))
            {
                log("Initializing Git repository...");
                var init = CommandRunner.Run("git", "init -b main", folder, 60000, true);
                if (init.ExitCode != 0)
                {
                    Tooling.RequireOk(CommandRunner.Run("git", "init", folder, 60000, false), "git init");
                    Tooling.RequireOk(CommandRunner.Run("git", "checkout -B main", folder, 60000, false), "git checkout -B main");
                }
            }
            else
            {
                log("Git repository already exists.");
                CommandRunner.Run("git", "checkout -B main", folder, 60000, true);
            }

            var login = Tooling.RequireText(CommandRunner.Run(gh, "api user --jq \".login\"", folder, 30000, false), "GitHub login");
            var id = Tooling.RequireText(CommandRunner.Run(gh, "api user --jq \".id\"", folder, 30000, false), "GitHub user id");
            var email = id.Trim() + "+" + login.Trim() + "@users.noreply.github.com";
            Tooling.RequireOk(CommandRunner.Run("git", "config user.name \"" + Tooling.EscapeArg(login.Trim()) + "\"", folder, 30000, false), "git config user.name");
            Tooling.RequireOk(CommandRunner.Run("git", "config user.email \"" + Tooling.EscapeArg(email) + "\"", folder, 30000, false), "git config user.email");

            log("Using Git identity: " + login.Trim() + " <" + email + ">");
            Tooling.RequireOk(CommandRunner.Run("git", "add -A", folder, 120000, false), "git add");

            var hasHead = CommandRunner.Run("git", "rev-parse --verify HEAD", folder, 60000, true).ExitCode == 0;
            var diff = CommandRunner.Run("git", "diff --cached --quiet", folder, 60000, true);
            if (diff.ExitCode != 0)
            {
                log("Creating commit...");
                Tooling.RequireOk(CommandRunner.Run("git", "commit -m \"Upload project\"", folder, 120000, false), "git commit");
            }
            else if (!hasHead)
            {
                log("Creating initial empty commit...");
                Tooling.RequireOk(CommandRunner.Run("git", "commit --allow-empty -m \"Upload project\"", folder, 120000, false), "git commit");
            }
            else
            {
                log("No staged changes to commit.");
            }

            var ownerRepo = login.Trim() + "/" + repo;
            var repoView = CommandRunner.Run(gh, "repo view " + ownerRepo + " --json nameWithOwner,url,visibility", folder, 30000, true);
            if (repoView.ExitCode != 0)
            {
                log("Creating GitHub repository: " + ownerRepo);
                var visibility = request.IsPublic ? "--public" : "--private";
                var args = "repo create " + repo + " " + visibility;
                if (!string.IsNullOrWhiteSpace(request.Description)) args += " --description \"" + Tooling.EscapeArg(request.Description.Trim()) + "\"";
                Tooling.RequireOk(CommandRunner.Run(gh, args, folder, 180000, false), "gh repo create");
            }
            else
            {
                log("Repository already exists: " + ownerRepo);
            }

            var remoteUrl = "https://github.com/" + ownerRepo + ".git";
            var remote = CommandRunner.Run("git", "remote get-url origin", folder, 30000, true);
            if (remote.ExitCode != 0)
            {
                Tooling.RequireOk(CommandRunner.Run("git", "remote add origin " + remoteUrl, folder, 30000, false), "git remote add origin");
            }
            else if (!string.Equals(remote.Combined.Trim(), remoteUrl, StringComparison.OrdinalIgnoreCase))
            {
                log("Updating origin remote to: " + remoteUrl);
                Tooling.RequireOk(CommandRunner.Run("git", "remote set-url origin " + remoteUrl, folder, 30000, false), "git remote set-url origin");
            }

            Tooling.RequireOk(CommandRunner.Run("git", "push -u origin main", folder, 180000, false), "git push");
            return "https://github.com/" + ownerRepo;
        }
    }

    public static class Tooling
    {
        public static string NormalizeFolder(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return "";
            try { return Path.GetFullPath(text.Trim().Trim('"')); }
            catch { return text.Trim(); }
        }

        public static string SuggestRepoName(string folder)
        {
            try { return NormalizeRepoName(new DirectoryInfo(folder).Name); }
            catch { return ""; }
        }

        public static string NormalizeRepoName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "";
            var lower = value.Trim().ToLowerInvariant();
            var sb = new StringBuilder();
            foreach (var ch in lower)
            {
                if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9') || ch == '-' || ch == '_' || ch == '.')
                {
                    sb.Append(ch);
                }
                else if (char.IsWhiteSpace(ch))
                {
                    sb.Append('-');
                }
            }
            return sb.ToString().Trim('.', '-', '_');
        }

        public static bool IsDangerousRoot(string folder)
        {
            var full = Path.GetFullPath(folder).TrimEnd('\\');
            var root = Path.GetPathRoot(full).TrimEnd('\\');
            if (string.Equals(full, root, StringComparison.OrdinalIgnoreCase)) return true;

            var user = Environment.GetFolderPath(Environment.SpecialFolder.UserProfile).TrimEnd('\\');
            return string.Equals(full, user, StringComparison.OrdinalIgnoreCase);
        }

        public static string FindGitHubCli()
        {
            var path = CommandRunner.FindOnPath("gh.exe");
            if (path != null) return path;
            var candidates = new[]
            {
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles), @"GitHub CLI\gh.exe"),
                Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), @"Microsoft\WinGet\Links\gh.exe")
            };
            return candidates.FirstOrDefault(File.Exists);
        }

        public static void EnsureDefaultGitIgnore(string folder)
        {
            var path = Path.Combine(folder, ".gitignore");
            var defaults = new[]
            {
                "",
                "# Added by GitHub One-Click Uploader",
                ".env",
                ".env.*",
                "*.pem",
                "*.pfx",
                "*.p12",
                "*.key",
                "id_rsa",
                "id_dsa",
                "credentials.json",
                "token.json",
                "node_modules/",
                "bin/",
                "obj/",
                "artifacts/",
                "dist/",
                "*.log",
                "*.bak",
                "*.bak_*",
                "latest.tsv",
                "daily-snapshots.tsv",
                "snapshots.csv",
                "cleanup-log.txt",
                "startup-log.txt"
            };

            var existing = File.Exists(path)
                ? new HashSet<string>(File.ReadAllLines(path, Encoding.UTF8), StringComparer.OrdinalIgnoreCase)
                : new HashSet<string>(StringComparer.OrdinalIgnoreCase);
            using (var writer = new StreamWriter(path, true, Encoding.UTF8))
            {
                foreach (var line in defaults)
                {
                    if (line.Length == 0)
                    {
                        writer.WriteLine();
                        continue;
                    }
                    if (!existing.Contains(line))
                    {
                        writer.WriteLine(line);
                        existing.Add(line);
                    }
                }
            }
        }

        public static string EscapeArg(string value)
        {
            return (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        public static void RequireOk(CommandResult result, string label)
        {
            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException(label + " failed:\r\n" + result.Combined);
            }
        }

        public static string RequireText(CommandResult result, string label)
        {
            RequireOk(result, label);
            return result.Combined.Trim();
        }
    }

    public static class SafetyScanner
    {
        private static readonly string[] IgnoredDirs =
        {
            ".git", "node_modules", "bin", "obj", "artifacts", "dist", "build", ".vs", ".idea"
        };

        private static readonly string[] BlockedNames =
        {
            ".env", ".env.local", ".env.production", ".env.development",
            "id_rsa", "id_dsa", "credentials.json", "token.json",
            "latest.tsv", "daily-snapshots.tsv", "snapshots.csv",
            "cleanup-log.txt", "startup-log.txt"
        };

        private static readonly string[] BlockedExtensions =
        {
            ".pem", ".pfx", ".p12", ".key"
        };

        private static readonly Regex[] SecretPatterns =
        {
            new Regex(@"AIza[0-9A-Za-z_-]{20,}", RegexOptions.Compiled),
            new Regex(@"AQ\.[0-9A-Za-z_-]{20,}", RegexOptions.Compiled),
            new Regex(@"sk-[0-9A-Za-z_-]{20,}", RegexOptions.Compiled),
            new Regex(@"github_pat_[0-9A-Za-z_]{20,}", RegexOptions.Compiled),
            new Regex(@"gh[pousr]_[0-9A-Za-z_]{20,}", RegexOptions.Compiled),
            new Regex(@"(?i)(api[_-]?key|secret|token)\s*[:=]\s*['""][^'""]{8,}['""]", RegexOptions.Compiled)
        };

        public static List<Finding> Scan(string root)
        {
            var findings = new List<Finding>();
            var files = SafeEnumerateFiles(root, findings).Take(20000).ToList();
            if (files.Count >= 20000)
            {
                findings.Add(new Finding { Severity = "Medium", PathText = root, Message = "Large project: scanned first 20,000 files only." });
            }

            foreach (var file in files)
            {
                var name = Path.GetFileName(file);
                var ext = Path.GetExtension(file).ToLowerInvariant();
                if (BlockedNames.Contains(name, StringComparer.OrdinalIgnoreCase) || BlockedExtensions.Contains(ext, StringComparer.OrdinalIgnoreCase) || name.EndsWith(".bak", StringComparison.OrdinalIgnoreCase) || name.IndexOf(".bak_", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    findings.Add(new Finding { Severity = "High", PathText = MakeRelative(root, file), Message = "Blocked sensitive/local data filename." });
                    continue;
                }

                if (IsTextLike(file))
                {
                    ScanTextFile(root, file, findings);
                }
            }
            return findings;
        }

        private static IEnumerable<string> SafeEnumerateFiles(string root, List<Finding> findings)
        {
            var stack = new Stack<string>();
            stack.Push(root);
            while (stack.Count > 0)
            {
                var dir = stack.Pop();
                string[] dirs = new string[0];
                string[] files = new string[0];
                try { dirs = Directory.GetDirectories(dir); }
                catch (Exception ex)
                {
                    findings.Add(new Finding { Severity = "Low", PathText = MakeRelative(root, dir), Message = "Could not read directory: " + ex.Message });
                }
                foreach (var child in dirs)
                {
                    if (IgnoredDirs.Contains(Path.GetFileName(child), StringComparer.OrdinalIgnoreCase)) continue;
                    if (IsReparse(child)) continue;
                    stack.Push(child);
                }
                try { files = Directory.GetFiles(dir); }
                catch (Exception ex)
                {
                    findings.Add(new Finding { Severity = "Low", PathText = MakeRelative(root, dir), Message = "Could not list files: " + ex.Message });
                }
                foreach (var file in files) yield return file;
            }
        }

        private static void ScanTextFile(string root, string file, List<Finding> findings)
        {
            try
            {
                var info = new FileInfo(file);
                if (info.Length > 1024 * 1024) return;
                var text = File.ReadAllText(file, Encoding.UTF8);
                foreach (var pattern in SecretPatterns)
                {
                    if (pattern.IsMatch(text))
                    {
                        findings.Add(new Finding { Severity = "High", PathText = MakeRelative(root, file), Message = "Possible secret/token pattern." });
                        break;
                    }
                }
                if (Regex.IsMatch(text, @"C:\\Users\\[A-Za-z0-9._-]+"))
                {
                    findings.Add(new Finding { Severity = "Medium", PathText = MakeRelative(root, file), Message = "Contains a local Windows user path." });
                }
            }
            catch { }
        }

        private static bool IsTextLike(string file)
        {
            var ext = Path.GetExtension(file).ToLowerInvariant();
            if (ext == "") return true;
            return new[]
            {
                ".txt", ".md", ".cs", ".ps1", ".json", ".xml", ".yml", ".yaml", ".gitignore", ".gitattributes",
                ".js", ".ts", ".tsx", ".jsx", ".html", ".css", ".py", ".toml", ".ini", ".config", ".sln", ".csproj"
            }.Contains(ext, StringComparer.OrdinalIgnoreCase) || Path.GetFileName(file).StartsWith(".", StringComparison.Ordinal);
        }

        private static bool IsReparse(string path)
        {
            try { return (File.GetAttributes(path) & FileAttributes.ReparsePoint) == FileAttributes.ReparsePoint; }
            catch { return true; }
        }

        private static string MakeRelative(string root, string path)
        {
            var fullRoot = Path.GetFullPath(root).TrimEnd('\\') + "\\";
            var fullPath = Path.GetFullPath(path);
            if (fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase))
            {
                return fullPath.Substring(fullRoot.Length);
            }
            return fullPath;
        }
    }

    public static class CommandRunner
    {
        public static int RunInteractive(string fileName, string arguments, string workingDirectory)
        {
            using (var process = new Process())
            {
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.WorkingDirectory = workingDirectory;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = false;
                process.StartInfo.RedirectStandardError = false;
                process.StartInfo.CreateNoWindow = false;
                process.Start();
                process.WaitForExit();
                return process.ExitCode;
            }
        }

        public static CommandResult Run(string fileName, string arguments, string workingDirectory, int timeoutMs, bool allowFailure)
        {
            var result = new CommandResult();
            using (var process = new Process())
            {
                process.StartInfo.FileName = fileName;
                process.StartInfo.Arguments = arguments;
                process.StartInfo.WorkingDirectory = workingDirectory;
                process.StartInfo.UseShellExecute = false;
                process.StartInfo.RedirectStandardOutput = true;
                process.StartInfo.RedirectStandardError = true;
                process.StartInfo.StandardOutputEncoding = Encoding.UTF8;
                process.StartInfo.StandardErrorEncoding = Encoding.UTF8;
                process.StartInfo.CreateNoWindow = true;

                var output = new StringBuilder();
                var error = new StringBuilder();
                process.OutputDataReceived += (s, e) => { if (e.Data != null) output.AppendLine(e.Data); };
                process.ErrorDataReceived += (s, e) => { if (e.Data != null) error.AppendLine(e.Data); };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();
                if (!process.WaitForExit(timeoutMs))
                {
                    try { process.Kill(); } catch { }
                    result.ExitCode = -1;
                    result.Output = output.ToString();
                    result.Error = "Timed out after " + timeoutMs + " ms.";
                    if (!allowFailure) throw new TimeoutException(result.Error);
                    return result;
                }
                process.WaitForExit();
                result.ExitCode = process.ExitCode;
                result.Output = output.ToString();
                result.Error = error.ToString();
            }

            if (!allowFailure && result.ExitCode != 0)
            {
                throw new InvalidOperationException(fileName + " " + arguments + " failed:\r\n" + result.Combined);
            }
            return result;
        }

        public static string FindOnPath(string exeName)
        {
            var path = Environment.GetEnvironmentVariable("PATH") ?? "";
            foreach (var dir in path.Split(';'))
            {
                if (string.IsNullOrWhiteSpace(dir)) continue;
                try
                {
                    var candidate = Path.Combine(dir.Trim(), exeName);
                    if (File.Exists(candidate)) return candidate;
                }
                catch { }
            }
            return null;
        }
    }

    public static class SelfTest
    {
        public static int Run()
        {
            var root = Path.Combine(Path.GetTempPath(), "github-uploader-self-test-" + Guid.NewGuid().ToString("N"));
            try
            {
                Directory.CreateDirectory(root);
                File.WriteAllText(Path.Combine(root, "README.md"), "# Sample" + Environment.NewLine, Encoding.UTF8);
                File.WriteAllText(Path.Combine(root, ".env"), "SECRET=value" + Environment.NewLine, Encoding.UTF8);
                var fakeKey = "sk-" + "123456789012345678901234";
                File.WriteAllText(Path.Combine(root, "config.txt"), "api_key=\"" + fakeKey + "\"" + Environment.NewLine, Encoding.UTF8);

                var findings = SafetyScanner.Scan(root);
                var blockedFile = findings.Any(x => x.Severity == "High" && x.PathText.IndexOf(".env", StringComparison.OrdinalIgnoreCase) >= 0);
                var secretPattern = findings.Any(x => x.Severity == "High" && x.Message.IndexOf("secret", StringComparison.OrdinalIgnoreCase) >= 0);
                if (!blockedFile || !secretPattern)
                {
                    return 2;
                }
                return 0;
            }
            catch
            {
                return 1;
            }
            finally
            {
                try
                {
                    if (Directory.Exists(root)) Directory.Delete(root, true);
                }
                catch { }
            }
        }
    }
}

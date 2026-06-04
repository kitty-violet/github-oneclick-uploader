using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
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
        [STAThread]
        public static int Main(string[] args)
        {
            if (args != null && args.Length > 0 && string.Equals(args[0], "--self-test", StringComparison.OrdinalIgnoreCase))
            {
                return SelfTest.Run();
            }

            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);
            Application.Run(new MainForm());
            return 0;
        }
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

        private List<Finding> lastFindings = new List<Finding>();

        public MainForm()
        {
            Text = "GitHub One-Click Uploader";
            Width = 1120;
            Height = 760;
            MinimumSize = new Size(880, 620);
            StartPosition = FormStartPosition.CenterScreen;

            BuildLayout();
        }

        private void BuildLayout()
        {
            var root = new TableLayoutPanel();
            root.Dock = DockStyle.Fill;
            root.Padding = new Padding(14);
            root.RowCount = 4;
            root.ColumnCount = 1;
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 152));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 48));
            root.RowStyles.Add(new RowStyle(SizeType.Percent, 52));
            root.RowStyles.Add(new RowStyle(SizeType.Absolute, 32));
            Controls.Add(root);

            var inputs = new TableLayoutPanel();
            inputs.Dock = DockStyle.Fill;
            inputs.ColumnCount = 4;
            inputs.RowCount = 4;
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 105));
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 112));
            inputs.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 126));
            inputs.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            inputs.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            inputs.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            inputs.RowStyles.Add(new RowStyle(SizeType.Absolute, 38));
            root.Controls.Add(inputs, 0, 0);

            AddLabel(inputs, "Project", 0, 0);
            folderText.Dock = DockStyle.Fill;
            folderText.Font = new Font("Segoe UI", 9);
            folderText.TextChanged += (s, e) =>
            {
                if (string.IsNullOrWhiteSpace(repoText.Text) && Directory.Exists(folderText.Text))
                {
                    repoText.Text = SuggestRepoName(folderText.Text);
                }
            };
            inputs.Controls.Add(folderText, 1, 0);

            browseButton.Text = "Browse";
            browseButton.Dock = DockStyle.Fill;
            browseButton.Click += (s, e) => BrowseFolder();
            inputs.Controls.Add(browseButton, 2, 0);

            openButton.Text = "Open";
            openButton.Dock = DockStyle.Fill;
            openButton.Click += (s, e) => OpenProjectFolder();
            inputs.Controls.Add(openButton, 3, 0);

            AddLabel(inputs, "Repository", 0, 1);
            repoText.Dock = DockStyle.Fill;
            repoText.Font = new Font("Segoe UI", 9);
            inputs.Controls.Add(repoText, 1, 1);

            publicCheck.Text = "Public";
            publicCheck.Checked = true;
            publicCheck.Dock = DockStyle.Fill;
            inputs.Controls.Add(publicCheck, 2, 1);

            strictCheck.Text = "Strict safety";
            strictCheck.Checked = true;
            strictCheck.Dock = DockStyle.Fill;
            inputs.Controls.Add(strictCheck, 3, 1);

            AddLabel(inputs, "Description", 0, 2);
            descriptionText.Dock = DockStyle.Fill;
            descriptionText.Font = new Font("Segoe UI", 9);
            inputs.Controls.Add(descriptionText, 1, 2);
            inputs.SetColumnSpan(descriptionText, 3);

            var actionPanel = new FlowLayoutPanel();
            actionPanel.Dock = DockStyle.Fill;
            actionPanel.FlowDirection = FlowDirection.LeftToRight;
            actionPanel.WrapContents = false;
            actionPanel.Padding = new Padding(0, 4, 0, 0);
            inputs.Controls.Add(actionPanel, 1, 3);
            inputs.SetColumnSpan(actionPanel, 3);

            checkButton.Text = "Check";
            checkButton.Width = 120;
            checkButton.Height = 32;
            checkButton.Click += async (s, e) => await CheckProject();
            actionPanel.Controls.Add(checkButton);

            loginButton.Text = "GitHub Login";
            loginButton.Width = 130;
            loginButton.Height = 32;
            loginButton.Click += (s, e) => StartGitHubLogin();
            actionPanel.Controls.Add(loginButton);

            uploadButton.Text = "Upload";
            uploadButton.Width = 120;
            uploadButton.Height = 32;
            uploadButton.Click += async (s, e) => await UploadProject();
            actionPanel.Controls.Add(uploadButton);

            SetupFindingsList();
            root.Controls.Add(findingsList, 0, 1);

            logBox.Dock = DockStyle.Fill;
            logBox.Multiline = true;
            logBox.ReadOnly = true;
            logBox.ScrollBars = ScrollBars.Vertical;
            logBox.Font = new Font("Consolas", 9);
            root.Controls.Add(logBox, 0, 2);

            statusLabel.Dock = DockStyle.Fill;
            statusLabel.TextAlign = ContentAlignment.MiddleLeft;
            statusLabel.ForeColor = Color.FromArgb(70, 70, 70);
            statusLabel.Text = "Choose a project folder, then click Check or Upload.";
            root.Controls.Add(statusLabel, 0, 3);
        }

        private void AddLabel(TableLayoutPanel parent, string text, int column, int row)
        {
            var label = new Label();
            label.Text = text;
            label.Dock = DockStyle.Fill;
            label.TextAlign = ContentAlignment.MiddleLeft;
            label.Font = new Font("Segoe UI", 9, FontStyle.Bold);
            parent.Controls.Add(label, column, row);
        }

        private void SetupFindingsList()
        {
            findingsList.Dock = DockStyle.Fill;
            findingsList.View = View.Details;
            findingsList.FullRowSelect = true;
            findingsList.GridLines = true;
            findingsList.HideSelection = false;
            findingsList.Font = new Font("Segoe UI", 9);
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
                    return;
                }

                var isPublic = publicCheck.Checked;
                var description = descriptionText.Text.Trim();
                await Task.Run(() => UploadCore(folder, repo, isPublic, description));
            }
            catch (Exception ex)
            {
                AppendLog("Upload failed: " + ex.Message);
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

            var folder = NormalizeFolder(folderText.Text);
            if (string.IsNullOrWhiteSpace(folder) || !Directory.Exists(folder))
            {
                AddFinding("High", folderText.Text, "Project folder does not exist.");
                RenderFindings();
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
            if (gh == null) AddFinding("High", "gh.exe", "GitHub CLI is not installed. Install with: winget install --id GitHub.cli -e");
            else AppendLog("GitHub CLI: " + gh);

            AppendLog("Scanning for risky files and secrets...");
            lastFindings.AddRange(SafetyScanner.Scan(folder));

            if (gh != null)
            {
                var auth = await Task.Run(() => CommandRunner.Run(gh, "auth status", folder, 30000, true));
                if (auth.ExitCode != 0)
                {
                    AddFinding("High", "GitHub CLI", "Not logged in. Click GitHub Login, authorize in browser, then check again.");
                }
                else
                {
                    AppendLog(auth.Combined);
                    if (!auth.Combined.Contains("workflow"))
                    {
                        AddFinding("Medium", "GitHub CLI", "Token may not include workflow scope. Login again with repo,workflow scopes if pushing .github workflows fails.");
                    }
                }
            }

            RenderFindings();
            var blocking = HasBlockingFindings();
            statusLabel.Text = blocking ? "Check found blocking issues." : "Check passed.";
            AppendLog(blocking ? "Check failed." : "Check passed.");
            return !blocking;
        }

        private void UploadCore(string folder, string repo, bool isPublic, string description)
        {
            var gh = FindGitHubCli();
            if (gh == null) throw new InvalidOperationException("GitHub CLI was not found.");

            AppendLog("Adding safe .gitignore defaults...");
            EnsureDefaultGitIgnore(folder);

            if (!Directory.Exists(Path.Combine(folder, ".git")))
            {
                AppendLog("Initializing Git repository...");
                var init = CommandRunner.Run("git", "init -b main", folder, 60000, true);
                if (init.ExitCode != 0)
                {
                    RequireOk(CommandRunner.Run("git", "init", folder, 60000, false), "git init");
                    RequireOk(CommandRunner.Run("git", "checkout -B main", folder, 60000, false), "git checkout -B main");
                }
                else
                {
                    AppendLog(init.Combined);
                }
            }
            else
            {
                AppendLog("Git repository already exists.");
                CommandRunner.Run("git", "checkout -B main", folder, 60000, true);
            }

            var login = RequireText(CommandRunner.Run(gh, "api user --jq \".login\"", folder, 30000, false), "GitHub login");
            var id = RequireText(CommandRunner.Run(gh, "api user --jq \".id\"", folder, 30000, false), "GitHub user id");
            var email = id.Trim() + "+" + login.Trim() + "@users.noreply.github.com";
            RequireOk(CommandRunner.Run("git", "config user.name \"" + EscapeArg(login.Trim()) + "\"", folder, 30000, false), "git config user.name");
            RequireOk(CommandRunner.Run("git", "config user.email \"" + EscapeArg(email) + "\"", folder, 30000, false), "git config user.email");

            AppendLog("Using Git identity: " + login.Trim() + " <" + email + ">");
            RequireOk(CommandRunner.Run("git", "add -A", folder, 120000, false), "git add");

            var diff = CommandRunner.Run("git", "diff --cached --quiet", folder, 60000, true);
            if (diff.ExitCode != 0)
            {
                AppendLog("Creating commit...");
                RequireOk(CommandRunner.Run("git", "commit -m \"Upload project\"", folder, 120000, false), "git commit");
            }
            else
            {
                AppendLog("No staged changes to commit.");
            }

            var ownerRepo = login.Trim() + "/" + repo;
            var repoView = CommandRunner.Run(gh, "repo view " + ownerRepo + " --json nameWithOwner,url,visibility", folder, 30000, true);
            if (repoView.ExitCode != 0)
            {
                AppendLog("Creating GitHub repository: " + ownerRepo);
                var visibility = isPublic ? "--public" : "--private";
                var desc = (description ?? "").Trim();
                var args = "repo create " + repo + " " + visibility + " --source . --remote origin --push";
                if (!string.IsNullOrWhiteSpace(desc)) args += " --description \"" + EscapeArg(desc) + "\"";
                RequireOk(CommandRunner.Run(gh, args, folder, 180000, false), "gh repo create");
            }
            else
            {
                AppendLog("Repository already exists: " + ownerRepo);
                var remoteUrl = "https://github.com/" + ownerRepo + ".git";
                var remote = CommandRunner.Run("git", "remote get-url origin", folder, 30000, true);
                if (remote.ExitCode != 0)
                {
                    RequireOk(CommandRunner.Run("git", "remote add origin " + remoteUrl, folder, 30000, false), "git remote add origin");
                }
                else
                {
                    AppendLog("Remote origin: " + remote.Combined);
                }
                RequireOk(CommandRunner.Run("git", "push -u origin main", folder, 180000, false), "git push");
            }

            var url = "https://github.com/" + ownerRepo;
            AppendLog("Uploaded: " + url);
            BeginInvoke(new Action(() =>
            {
                statusLabel.Text = "Uploaded: " + url;
                if (MessageBox.Show("Upload finished.\r\n\r\nOpen repository?", "GitHub Uploader", MessageBoxButtons.YesNo, MessageBoxIcon.Information) == DialogResult.Yes)
                {
                    Process.Start(url);
                }
            }));
        }

        private void StartGitHubLogin()
        {
            var gh = FindGitHubCli();
            if (gh == null)
            {
                MessageBox.Show("GitHub CLI is not installed.\r\n\r\nInstall with:\r\nwinget install --id GitHub.cli -e", "GitHub Login", MessageBoxButtons.OK, MessageBoxIcon.Warning);
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
            statusLabel.Text = "Finish GitHub login in the opened PowerShell/browser, then click Check.";
        }

        private void EnsureDefaultGitIgnore(string folder)
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

        private static string EscapeArg(string value)
        {
            return (value ?? "").Replace("\\", "\\\\").Replace("\"", "\\\"");
        }

        private static void RequireOk(CommandResult result, string label)
        {
            if (result.ExitCode != 0)
            {
                throw new InvalidOperationException(label + " failed:\r\n" + result.Combined);
            }
        }

        private static string RequireText(CommandResult result, string label)
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

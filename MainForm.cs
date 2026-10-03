namespace GitHubSimpleUploader;

public sealed class MainForm : Form
{
    private static readonly string[] BasicGitIgnoreLines =
    [
        "bin/",
        "obj/",
        ".vs/",
        "*.user",
        "*.suo",
        "appsettings.Development.json",
        ".env",
        "node_modules/",
        "dist/"
    ];

    private static readonly string[] SecretExactNames =
    [
        "appsettings.json",
        ".env",
        "credentials.json",
        "service-account.json"
    ];

    private static readonly string[] SecretScanIgnoredDirectories =
    [
        ".git",
        ".vs",
        "bin",
        "obj",
        "node_modules",
        "dist"
    ];

    private static readonly string[] GitIgnoredTrackedPaths =
    [
        "node_modules",
        "dist",
        "bin",
        "obj",
        ".vs"
    ];

    private readonly GitService gitService = new();
    private readonly AppSettingsService appSettingsService = new();
    private AppSettings appSettings = new();
    private bool isLoadingRepository;
    private readonly ComboBox savedReposComboBox = new();
    private readonly TextBox projectFolderTextBox = new();
    private readonly TextBox repoUrlTextBox = new();
    private readonly TextBox branchTextBox = new();
    private readonly TextBox commitMessageTextBox = new();
    private readonly TextBox cloneFolderNameTextBox = new();
    private readonly RichTextBox logTextBox = new();
    private readonly ProgressBar progressBar = new();
    private readonly Label stateLabel = new();
    private readonly List<Button> commandButtons = [];

    public MainForm()
    {
        Text = "GitHubSimpleUploader";
        MinimumSize = new Size(940, 680);
        StartPosition = FormStartPosition.CenterScreen;
        Font = new Font("Segoe UI", 10F);

        BuildLayout();
        LoadSettings();
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ColumnCount = 1,
            RowCount = 4
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var inputGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 3
        };
        inputGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));
        inputGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        inputGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));

        AddLabel(inputGrid, "Thư mục project local", 0);
        projectFolderTextBox.Dock = DockStyle.Fill;
        inputGrid.Controls.Add(projectFolderTextBox, 1, 0);

        var browseButton = new Button { Text = "Browse", Dock = DockStyle.Fill };
        browseButton.Click += (_, _) => BrowseProjectFolder();
        inputGrid.Controls.Add(browseButton, 2, 0);

        AddLabel(inputGrid, "GitHub repository URL", 1);
        repoUrlTextBox.Dock = DockStyle.Fill;
        inputGrid.Controls.Add(repoUrlTextBox, 1, 1);
        inputGrid.SetColumnSpan(repoUrlTextBox, 2);

        AddLabel(inputGrid, "Branch", 2);
        branchTextBox.Text = "main";
        branchTextBox.Dock = DockStyle.Left;
        branchTextBox.Width = 220;
        inputGrid.Controls.Add(branchTextBox, 1, 2);
        inputGrid.SetColumnSpan(branchTextBox, 2);

        AddLabel(inputGrid, "Commit message", 3);
        commitMessageTextBox.Dock = DockStyle.Fill;
        inputGrid.Controls.Add(commitMessageTextBox, 1, 3);
        inputGrid.SetColumnSpan(commitMessageTextBox, 2);

        AddLabel(inputGrid, "Tên folder clone", 4);
        cloneFolderNameTextBox.Dock = DockStyle.Left;
        cloneFolderNameTextBox.Width = 300;
        cloneFolderNameTextBox.PlaceholderText = "Tùy chọn";
        inputGrid.Controls.Add(cloneFolderNameTextBox, 1, 4);
        inputGrid.SetColumnSpan(cloneFolderNameTextBox, 2);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            Padding = new Padding(0, 12, 0, 10),
            WrapContents = true
        };

        buttonPanel.Controls.Add(new Label
        {
            Text = "Cai dat repo",
            AutoSize = true,
            TextAlign = ContentAlignment.MiddleLeft,
            Margin = new Padding(0, 7, 8, 8)
        });

        savedReposComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        savedReposComboBox.Width = 260;
        savedReposComboBox.Margin = new Padding(0, 0, 8, 8);
        savedReposComboBox.SelectedIndexChanged += (_, _) => LoadSelectedRepository();
        buttonPanel.Controls.Add(savedReposComboBox);

        var saveRepoButton = new Button
        {
            Text = "Settings",
            AutoSize = true,
            Height = 36,
            Margin = new Padding(0, 0, 8, 8),
            Padding = new Padding(10, 4, 10, 4)
        };
        saveRepoButton.Click += (_, _) => OpenSettings();
        commandButtons.Add(saveRepoButton);
        buttonPanel.Controls.Add(saveRepoButton);

        var deleteRepoButton = new Button
        {
            Text = "Delete",
            AutoSize = true,
            Height = 36,
            Margin = new Padding(0, 0, 18, 8),
            Padding = new Padding(10, 4, 10, 4)
        };
        deleteRepoButton.Click += (_, _) => DeleteSelectedRepository();
        deleteRepoButton.Visible = false;
        commandButtons.Add(deleteRepoButton);
        buttonPanel.Controls.Add(deleteRepoButton);

        AddCommandButton(buttonPanel, "Check Git Installed", CheckGitInstalledAsync);
        AddCommandButton(buttonPanel, "Init / Link Repo", InitOrLinkRepoAsync);
        AddCommandButton(buttonPanel, "Upload Today", UploadTodayAsync);
        AddCommandButton(buttonPanel, "Clone Project", CloneProjectAsync);
        AddCommandButton(buttonPanel, "Pull Latest", PullLatestAsync);
        AddCommandButton(buttonPanel, "View Status", ViewStatusAsync);

        logTextBox.Dock = DockStyle.Fill;
        logTextBox.ReadOnly = true;
        logTextBox.BackColor = Color.FromArgb(30, 30, 30);
        logTextBox.ForeColor = Color.FromArgb(235, 235, 235);
        logTextBox.Font = new Font("Consolas", 10F);
        logTextBox.BorderStyle = BorderStyle.FixedSingle;

        var statusPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            ColumnCount = 2
        };
        statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        statusPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 180));

        stateLabel.Text = "Sẵn sàng";
        stateLabel.Dock = DockStyle.Fill;
        stateLabel.TextAlign = ContentAlignment.MiddleLeft;

        progressBar.Dock = DockStyle.Fill;
        progressBar.Style = ProgressBarStyle.Marquee;
        progressBar.Visible = false;

        statusPanel.Controls.Add(stateLabel, 0, 0);
        statusPanel.Controls.Add(progressBar, 1, 0);

        inputGrid.Visible = false;
        root.Controls.Add(inputGrid, 0, 0);
        root.Controls.Add(buttonPanel, 0, 1);
        root.Controls.Add(logTextBox, 0, 2);
        root.Controls.Add(statusPanel, 0, 3);

        Controls.Add(root);
    }

    private static void AddLabel(TableLayoutPanel grid, string text, int row)
    {
        var label = new Label
        {
            Text = text,
            AutoSize = true,
            Dock = DockStyle.Fill,
            TextAlign = ContentAlignment.MiddleLeft,
            Padding = new Padding(0, 6, 8, 6)
        };
        grid.Controls.Add(label, 0, row);
    }

    private void AddCommandButton(FlowLayoutPanel panel, string text, Func<Task> handler)
    {
        var button = new Button
        {
            Text = text,
            AutoSize = true,
            Height = 36,
            Margin = new Padding(0, 0, 8, 8),
            Padding = new Padding(10, 4, 10, 4)
        };

        button.Click += async (_, _) => await RunUiCommandAsync(text, handler);
        commandButtons.Add(button);
        panel.Controls.Add(button);
    }

    private void BrowseProjectFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Chọn thư mục project local",
            UseDescriptionForTitle = true
        };

        if (Directory.Exists(projectFolderTextBox.Text))
        {
            dialog.InitialDirectory = projectFolderTextBox.Text;
        }

        if (dialog.ShowDialog(this) == DialogResult.OK)
        {
            projectFolderTextBox.Text = dialog.SelectedPath;
        }
    }

    private void LoadSettings()
    {
        appSettings = appSettingsService.Load();
        RefreshSavedRepositoryCombo(appSettings.LastRepositoryId);
    }

    private void OpenSettings()
    {
        using var settingsForm = new SettingsForm(appSettings, appSettingsService);
        settingsForm.ShowDialog(this);
        appSettings = appSettingsService.Load();
        RefreshSavedRepositoryCombo(settingsForm.SelectedRepositoryId ?? appSettings.LastRepositoryId);
    }

    private void RefreshSavedRepositoryCombo(string? selectedRepositoryId = null)
    {
        isLoadingRepository = true;
        try
        {
            var repositories = appSettings.Repositories
                .OrderBy(repository => repository.Name)
                .ThenBy(repository => repository.ProjectFolder)
                .ToList();

            savedReposComboBox.DataSource = null;
            savedReposComboBox.DataSource = repositories;

            if (savedReposComboBox.Items.Count == 0)
            {
                return;
            }

            var selectedRepository = repositories.FirstOrDefault(repository =>
                repository.Id.Equals(selectedRepositoryId, StringComparison.OrdinalIgnoreCase));
            if (selectedRepository is not null)
            {
                savedReposComboBox.SelectedItem = selectedRepository;
            }
            else
            {
                savedReposComboBox.SelectedIndex = 0;
            }
        }
        finally
        {
            isLoadingRepository = false;
        }

        LoadSelectedRepository();
    }

    private void LoadSelectedRepository()
    {
        if (isLoadingRepository || savedReposComboBox.SelectedItem is not SavedRepository repository)
        {
            return;
        }

        projectFolderTextBox.Text = repository.ProjectFolder;
        repoUrlTextBox.Text = repository.RepoUrl;
        branchTextBox.Text = string.IsNullOrWhiteSpace(repository.Branch) ? "main" : repository.Branch;
        commitMessageTextBox.Text = repository.CommitMessage;
        cloneFolderNameTextBox.Text = repository.CloneFolderName;
        appSettings.LastRepositoryId = repository.Id;
        appSettingsService.Save(appSettings);
        AppendLog($"Loaded repo: {repository}");
    }

    private void SaveCurrentRepository()
    {
        var projectFolder = projectFolderTextBox.Text.Trim();
        var repoUrl = repoUrlTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(projectFolder))
        {
            MessageBox.Show(this, "Vui long chon thu muc project local truoc khi luu.", "Thieu thong tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(repoUrl))
        {
            MessageBox.Show(this, "Vui long nhap GitHub repository URL truoc khi luu.", "Thieu thong tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var repository = savedReposComboBox.SelectedItem as SavedRepository;
        repository = repository is not null
            && repository.ProjectFolder.Equals(projectFolder, StringComparison.OrdinalIgnoreCase)
                ? repository
                : appSettings.Repositories.FirstOrDefault(item =>
                    item.ProjectFolder.Equals(projectFolder, StringComparison.OrdinalIgnoreCase)
                    || item.RepoUrl.Equals(repoUrl, StringComparison.OrdinalIgnoreCase));

        if (repository is null)
        {
            repository = new SavedRepository();
            appSettings.Repositories.Add(repository);
        }

        repository.Name = GetRepositoryDisplayName(projectFolder, repoUrl);
        repository.ProjectFolder = projectFolder;
        repository.RepoUrl = repoUrl;
        repository.Branch = GetBranch();
        repository.CommitMessage = commitMessageTextBox.Text.Trim();

        appSettings.LastRepositoryId = repository.Id;
        appSettingsService.Save(appSettings);
        RefreshSavedRepositoryCombo(repository.Id);
        AppendLog($"Saved repo: {repository}");
    }

    private void DeleteSelectedRepository()
    {
        if (savedReposComboBox.SelectedItem is not SavedRepository repository)
        {
            MessageBox.Show(this, "Chua co repo nao de xoa.", "Thong bao", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var confirm = MessageBox.Show(this, $"Xoa repo khoi danh sach?\n\n{repository}", "Xac nhan xoa", MessageBoxButtons.YesNo, MessageBoxIcon.Warning);
        if (confirm != DialogResult.Yes)
        {
            return;
        }

        appSettings.Repositories.RemoveAll(item => item.Id.Equals(repository.Id, StringComparison.OrdinalIgnoreCase));
        appSettings.LastRepositoryId = appSettings.Repositories.FirstOrDefault()?.Id;
        appSettingsService.Save(appSettings);
        RefreshSavedRepositoryCombo(appSettings.LastRepositoryId);
        AppendLog($"Deleted repo: {repository}");
    }

    private static string GetRepositoryDisplayName(string projectFolder, string repoUrl)
    {
        var folderName = Path.GetFileName(projectFolder.TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar));
        if (!string.IsNullOrWhiteSpace(folderName))
        {
            return folderName;
        }

        var repoName = repoUrl.TrimEnd('/').Split('/', '\\').LastOrDefault();
        return string.IsNullOrWhiteSpace(repoName) ? "Repository" : repoName.Replace(".git", string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    private async Task RunUiCommandAsync(string name, Func<Task> action)
    {
        SetBusy(true, $"Đang chạy: {name}");
        AppendLog("");
        AppendLog($"===== {name} =====");

        try
        {
            await action();
        }
        catch (Exception ex)
        {
            AppendLog($"Lỗi: {ex.Message}");
            MessageBox.Show(this, ex.Message, "Có lỗi", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            SetBusy(false, "Sẵn sàng");
        }
    }

    private async Task CheckGitInstalledAsync()
    {
        await RunGitAsync(string.Empty, "--version");
    }

    private async Task InitOrLinkRepoAsync()
    {
        var projectFolder = RequireProjectFolderForInit();
        var repoUrl = RequireRepoUrl();
        var branch = GetBranch();

        AppendLog($"Project folder: {projectFolder}");
        AppendLog($"Repository URL: {repoUrl}");

        EnsureBasicGitIgnoreRules(projectFolder);

        await EnsureGitRepositoryInitializedAsync(projectFolder, branch);

        var remoteResult = await RunGitAsync(projectFolder, "remote get-url origin", false);
        var quotedRepoUrl = GitService.QuoteArgument(repoUrl);

        if (remoteResult.IsSuccess)
        {
            await EnsureSuccessAsync(projectFolder, $"remote set-url origin {quotedRepoUrl}");
        }
        else
        {
            await EnsureSuccessAsync(projectFolder, $"remote add origin {quotedRepoUrl}");
        }

        await VerifyRemoteRepositoryAsync(projectFolder);
        await RunGitAsync(projectFolder, "status --untracked-files=normal");
    }

    private async Task EnsureGitRepositoryInitializedAsync(string projectFolder, string branch)
    {
        var insideRepoResult = await RunGitAsync(projectFolder, "rev-parse --is-inside-work-tree", false, logOutput: false);
        if (!insideRepoResult.IsSuccess || !insideRepoResult.StandardOutput.Trim().Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            if (Directory.Exists(Path.Combine(projectFolder, ".git")))
            {
                AppendLog("Phát hiện thư mục .git không hợp lệ. Đang khởi tạo lại Git metadata...");
            }

            await EnsureSuccessAsync(projectFolder, "init");
            insideRepoResult = await RunGitAsync(projectFolder, "rev-parse --is-inside-work-tree", false, logOutput: false);
        }

        if (!insideRepoResult.IsSuccess || !insideRepoResult.StandardOutput.Trim().Equals("true", StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidOperationException(
                "Không thể khởi tạo Git repo trong thư mục này.\n\n"
                + $"Thư mục: {projectFolder}\n\n"
                + "Hãy kiểm tra lại Project folder trong Settings, rồi bấm Init / Link Repo lại.");
        }

        await EnsureSuccessAsync(projectFolder, $"branch -M {GitService.QuoteArgument(branch)}");
    }
    private async Task VerifyRemoteRepositoryAsync(string projectFolder)
    {
        var result = await RunGitAsync(projectFolder, "ls-remote --heads origin", false, logOutput: false);
        if (result.IsSuccess)
        {
            AppendLog("Đã kiểm tra remote origin thành công.");
            return;
        }

        throw new InvalidOperationException(BuildGitErrorMessage("ls-remote --heads origin", result));
    }
    private async Task UploadTodayAsync()
    {
        var projectFolder = RequireProjectFolder();
        var branch = GetBranch();

        if (!Directory.Exists(Path.Combine(projectFolder, ".git")))
        {
            throw new InvalidOperationException("Thư mục này chưa phải Git repo. Hãy chạy Init / Link Repo trước.");
        }

        EnsureBasicGitIgnoreRules(projectFolder);

        var uploadCandidates = await GetGitUploadCandidatePathsAsync(projectFolder);
        var secretUploadCandidates = uploadCandidates
            .Where(IsPotentialSecretFile)
            .OrderBy(path => path)
            .ToList();
        var safeUploadCandidates = uploadCandidates
            .Where(path => !IsPotentialSecretFile(path))
            .OrderBy(path => path)
            .ToList();
        LogSkippedSecretFiles(secretUploadCandidates);

        await EnsureIgnoredTrackedPathsAreUntrackedAsync(projectFolder);

        var status = await RunGitAsync(projectFolder, "status --porcelain --untracked-files=normal", false, logOutput: false);
        if (!status.IsSuccess)
        {
            throw new InvalidOperationException("Không kiểm tra được trạng thái Git. Xem log để biết chi tiết.");
        }

        if (string.IsNullOrWhiteSpace(status.StandardOutput))
        {
            if (await HasUnpushedCommitsAsync(projectFolder, branch))
            {
                AppendLog("Khong co thay doi moi, nhung co commit local chua push. Bat dau push.");
                await PushBranchAsync(projectFolder, branch);
                return;
            }

            AppendLog("Không có thay đổi mới để upload.");
            return;
        }

        if (safeUploadCandidates.Count > 0)
        {
            AppendLog("Da phat hien thay doi an toan, bat dau add/commit/push.");
            await StageSafeFilesAsync(projectFolder, safeUploadCandidates);
        }
        else
        {
            AppendLog("Khong co file an toan moi de dua vao commit.");
        }

        await UnstageSecretFilesAsync(projectFolder, secretUploadCandidates);

        var stagedStatus = await RunGitAsync(projectFolder, "diff --cached --name-only", false, logOutput: false);
        if (!stagedStatus.IsSuccess || string.IsNullOrWhiteSpace(stagedStatus.StandardOutput))
        {
            if (await HasUnpushedCommitsAsync(projectFolder, branch))
            {
                AppendLog("Khong co commit moi an toan, nhung co commit local chua push. Bat dau push.");
                await PushBranchAsync(projectFolder, branch);
                return;
            }

            AppendLog("Khong co thay doi an toan de upload sau khi bo qua file secret.");
            return;
        }

        await EnsureSuccessAsync(projectFolder, $"commit -q -m {GitService.QuoteArgument(GetCommitMessage())}");

        await PushBranchAsync(projectFolder, branch);
    }

    private async Task CloneProjectAsync()
    {
        var repoUrl = RequireRepoUrl();

        using var dialog = new FolderBrowserDialog
        {
            Description = "Chọn thư mục cha để clone source",
            UseDescriptionForTitle = true
        };

        if (dialog.ShowDialog(this) != DialogResult.OK)
        {
            AppendLog("Đã hủy clone vì chưa chọn thư mục cha.");
            return;
        }

        var arguments = $"clone {GitService.QuoteArgument(repoUrl)}";
        var cloneFolderName = cloneFolderNameTextBox.Text.Trim();
        if (!string.IsNullOrWhiteSpace(cloneFolderName))
        {
            if (cloneFolderName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0)
            {
                throw new InvalidOperationException("Tên folder clone chứa ký tự không hợp lệ.");
            }

            arguments += $" {GitService.QuoteArgument(cloneFolderName)}";
        }

        await EnsureSuccessAsync(dialog.SelectedPath, arguments);
    }

    private async Task PullLatestAsync()
    {
        var projectFolder = RequireProjectFolder();
        var branch = GetBranch();

        await EnsureSuccessAsync(projectFolder, $"pull origin {GitService.QuoteArgument(branch)}");
    }

    private async Task ViewStatusAsync()
    {
        var projectFolder = RequireProjectFolder();
        await RunGitAsync(projectFolder, "status --untracked-files=normal");
    }

    private string RequireProjectFolderForInit()
    {
        var folder = projectFolderTextBox.Text.Trim().Trim('\"');
        if (string.IsNullOrWhiteSpace(folder))
        {
            throw new InvalidOperationException("Vui long nhap hoac chon thu muc project local.");
        }

        try
        {
            folder = Path.GetFullPath(folder);
            Directory.CreateDirectory(folder);
        }
        catch (Exception ex)
        {
            throw new InvalidOperationException($"Khong the tao thu muc project local: {ex.Message}");
        }

        return folder;
    }
    private string RequireProjectFolder()
    {
        var folder = projectFolderTextBox.Text.Trim().Trim('\"');
        if (string.IsNullOrWhiteSpace(folder))
        {
            throw new InvalidOperationException("Vui lòng chọn thư mục project local.");
        }

        if (!Directory.Exists(folder))
        {
            throw new InvalidOperationException("Thư mục project local không tồn tại.");
        }

        return folder;
    }

    private string RequireRepoUrl()
    {
        var repoUrl = repoUrlTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(repoUrl))
        {
            throw new InvalidOperationException("Vui lòng nhập GitHub repository URL.");
        }

        var normalizedUrl = NormalizeGitHubRepoUrl(repoUrl);
        if (string.IsNullOrWhiteSpace(normalizedUrl))
        {
            throw new InvalidOperationException(
                "GitHub repository URL không hợp lệ.\n\n"
                + "Ví dụ đúng:\n"
                + "https://github.com/username/repository.git\n"
                + "https://github.com/username/repository\n"
                + "git@github.com:username/repository.git");
        }

        if (!normalizedUrl.Equals(repoUrl, StringComparison.Ordinal))
        {
            AppendLog($"Đã chuẩn hóa GitHub URL: {normalizedUrl}");
            repoUrlTextBox.Text = normalizedUrl;
        }

        return normalizedUrl;
    }

    private static string? NormalizeGitHubRepoUrl(string repoUrl)
    {
        repoUrl = repoUrl.Trim();

        if (repoUrl.StartsWith("git@github.com:", StringComparison.OrdinalIgnoreCase))
        {
            var path = repoUrl["git@github.com:".Length..].Trim('/');
            var parts = path.Split('/', StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length < 2)
            {
                return null;
            }

            var owner = parts[0];
            var repo = parts[1].EndsWith(".git", StringComparison.OrdinalIgnoreCase)
                ? parts[1]
                : parts[1] + ".git";
            return $"git@github.com:{owner}/{repo}";
        }

        if (!Uri.TryCreate(repoUrl, UriKind.Absolute, out var uri))
        {
            return null;
        }

        if (!uri.Host.Equals("github.com", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var urlParts = uri.AbsolutePath.Trim('/').Split('/', StringSplitOptions.RemoveEmptyEntries);
        if (urlParts.Length < 2 || urlParts[0].Equals("new", StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var repoName = urlParts[1].EndsWith(".git", StringComparison.OrdinalIgnoreCase)
            ? urlParts[1][..^4]
            : urlParts[1];
        if (string.IsNullOrWhiteSpace(urlParts[0]) || string.IsNullOrWhiteSpace(repoName))
        {
            return null;
        }

        return $"https://github.com/{urlParts[0]}/{repoName}.git";
    }
    private string GetBranch()
    {
        var branch = branchTextBox.Text.Trim();
        if (string.IsNullOrWhiteSpace(branch))
        {
            branch = "main";
            branchTextBox.Text = branch;
        }

        return branch;
    }

    private string GetCommitMessage()
    {
        var message = commitMessageTextBox.Text.Trim();
        return string.IsNullOrWhiteSpace(message)
            ? $"Daily update {DateTime.Now:yyyy-MM-dd HH:mm}"
            : message;
    }

    private async Task<GitCommandResult> RunGitAsync(
        string workingDirectory,
        string arguments,
        bool throwOnGitMissing = true,
        bool logOutput = true)
    {
        if (logOutput)
        {
            AppendLog($"> git {arguments}");
        }

        var result = await gitService.RunGitCommandAsync(
            workingDirectory,
            arguments,
            logOutput ? AppendLog : null);

        if (!result.IsSuccess)
        {
            if (logOutput)
            {
                AppendLog($"Exit code: {result.ExitCode}");
            }

            if (throwOnGitMissing && result.ExitCode == -1)
            {
                throw new InvalidOperationException(result.StandardError);
            }
        }

        return result;
    }

    private async Task EnsureSuccessAsync(string workingDirectory, string arguments, bool logOutput = true)
    {
        var result = await RunGitAsync(workingDirectory, arguments, logOutput: logOutput);
        if (!result.IsSuccess)
        {
            throw new InvalidOperationException(BuildGitErrorMessage(arguments, result));
        }
    }

    private static string BuildGitErrorMessage(string arguments, GitCommandResult result)
    {
        var details = string.Join(
            Environment.NewLine,
            new[] { result.StandardError.Trim(), result.StandardOutput.Trim() }
                .Where(value => !string.IsNullOrWhiteSpace(value)));

        if (string.IsNullOrWhiteSpace(details))
        {
            details = "Git không trả về nội dung lỗi.";
        }

        return "Lệnh Git chạy không thành công."
            + Environment.NewLine + Environment.NewLine
            + $"Lệnh: git {arguments}"
            + Environment.NewLine
            + $"Exit code: {result.ExitCode}"
            + Environment.NewLine + Environment.NewLine
            + "Chi tiết lỗi:"
            + Environment.NewLine
            + details;
    }
    private void EnsureBasicGitIgnoreRules(string projectFolder)
    {
        var gitIgnorePath = Path.Combine(projectFolder, ".gitignore");
        if (!File.Exists(gitIgnorePath))
        {
            File.WriteAllLines(gitIgnorePath, BasicGitIgnoreLines);
            AppendLog("Da tao .gitignore co ban.");
            return;
        }

        var currentLines = File.ReadAllLines(gitIgnorePath).ToList();
        var missingLines = BasicGitIgnoreLines
            .Where(line => !currentLines.Any(existing => existing.Trim().Equals(line, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (missingLines.Count == 0)
        {
            AppendLog("Da co .gitignore voi cac rule co ban.");
            return;
        }

        currentLines.Add(string.Empty);
        currentLines.Add("# GitHubSimpleUploader");
        currentLines.AddRange(missingLines);
        File.WriteAllLines(gitIgnorePath, currentLines);
        AppendLog("Da bo sung .gitignore de bo qua file build va node_modules.");
    }

    private async Task EnsureIgnoredTrackedPathsAreUntrackedAsync(string projectFolder)
    {
        var quotedPaths = string.Join(" ", GitIgnoredTrackedPaths.Select(GitService.QuoteArgument));
        var trackedResult = await RunGitAsync(projectFolder, $"ls-files -- {quotedPaths}", false, logOutput: false);
        if (!trackedResult.IsSuccess || string.IsNullOrWhiteSpace(trackedResult.StandardOutput))
        {
            return;
        }

        var trackedGroups = trackedResult.StandardOutput
            .Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
            .Select(path => path.Replace('\\', '/'))
            .Select(path => GitIgnoredTrackedPaths.FirstOrDefault(ignored =>
                path.Equals(ignored, StringComparison.OrdinalIgnoreCase)
                || path.StartsWith($"{ignored}/", StringComparison.OrdinalIgnoreCase)))
            .Where(group => !string.IsNullOrWhiteSpace(group))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (trackedGroups.Count == 0)
        {
            return;
        }

        AppendLog("Phat hien cac muc nen ignore nhung da bi Git track:");
        foreach (var group in trackedGroups)
        {
            AppendLog($"- {group}");
        }

        var confirm = MessageBox.Show(
            this,
            "Phat hien node_modules/bin/obj/dist/.vs da tung duoc add vao Git.\n\n"
                + "Ban co muon go chung khoi Git index khong?\n"
                + "Thao tac nay KHONG xoa file tren may, chi de Git khong upload chung nua.",
            "Go file build khoi Git",
            MessageBoxButtons.YesNo,
            MessageBoxIcon.Warning);

        if (confirm != DialogResult.Yes)
        {
            return;
        }

        await EnsureSuccessAsync(projectFolder, $"rm -r -q --cached --ignore-unmatch -- {quotedPaths}");
    }

    private void EnsureBasicGitIgnore(string projectFolder)
    {
        var gitIgnorePath = Path.Combine(projectFolder, ".gitignore");
        if (File.Exists(gitIgnorePath))
        {
            AppendLog("Đã có .gitignore, giữ nguyên file hiện tại.");
            return;
        }

        File.WriteAllLines(gitIgnorePath, BasicGitIgnoreLines);
        AppendLog("Đã tạo .gitignore cơ bản.");
    }

    private async Task<bool> ConfirmSecretFilesAsync(string projectFolder)
    {
        var uploadCandidates = await GetGitUploadCandidatePathsAsync(projectFolder);
        var riskyFiles = uploadCandidates
            .Where(IsPotentialSecretFile)
            .OrderBy(path => path)
            .Take(20)
            .ToList();

        if (riskyFiles.Count == 0)
        {
            return true;
        }

        AppendLog("Cảnh báo: phát hiện file có thể chứa secret:");
        foreach (var file in riskyFiles)
        {
            AppendLog($"- {file}");
        }

        var message = "Phát hiện file có thể chứa secret:\n\n"
            + string.Join(Environment.NewLine, riskyFiles)
            + "\n\nBạn chắc chắn muốn tiếp tục upload?";

        return MessageBox.Show(this, message, "Xác nhận upload", MessageBoxButtons.YesNo, MessageBoxIcon.Warning)
            == DialogResult.Yes;
    }

    private async Task<List<string>> GetGitUploadCandidatePathsAsync(string projectFolder)
    {
        var candidates = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
        var commands = new[]
        {
            "diff --name-only",
            "diff --cached --name-only",
            "ls-files --others --exclude-standard"
        };

        foreach (var command in commands)
        {
            var result = await RunGitAsync(projectFolder, command, false, logOutput: false);
            if (!result.IsSuccess || string.IsNullOrWhiteSpace(result.StandardOutput))
            {
                continue;
            }

            foreach (var path in result.StandardOutput.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries))
            {
                candidates.Add(path.Trim().Replace('\\', '/'));
            }
        }

        return candidates.ToList();
    }

    private async Task<List<string>> GetSecretUploadCandidatePathsAsync(string projectFolder)
    {
        var uploadCandidates = await GetGitUploadCandidatePathsAsync(projectFolder);
        return uploadCandidates
            .Where(IsPotentialSecretFile)
            .OrderBy(path => path)
            .ToList();
    }

    private async Task StageSafeFilesAsync(string projectFolder, IReadOnlyList<string> safePaths)
    {
        const int batchSize = 40;

        for (var index = 0; index < safePaths.Count; index += batchSize)
        {
            var batch = safePaths
                .Skip(index)
                .Take(batchSize)
                .Select(GitService.QuoteArgument);
            await EnsureSuccessAsync(projectFolder, $"add -- {string.Join(" ", batch)}", logOutput: false);
        }

        AppendLog($"Da add {safePaths.Count} file an toan.");
    }

    private async Task<bool> HasUnpushedCommitsAsync(string projectFolder, string branch)
    {
        var result = await RunGitAsync(
            projectFolder,
            $"rev-list --count {GitService.QuoteArgument($"origin/{branch}..HEAD")}",
            false,
            logOutput: false);

        return result.IsSuccess
            && int.TryParse(result.StandardOutput.Trim(), out var count)
            && count > 0;
    }

    private async Task PushBranchAsync(string projectFolder, string branch)
    {
        var pushResult = await RunGitAsync(projectFolder, $"push origin {GitService.QuoteArgument(branch)}", false);
        if (pushResult.IsSuccess)
        {
            return;
        }

        if (IsAuthenticationError(pushResult))
        {
            throw new InvalidOperationException("Git không tìm thấy credential hợp lệ cho repository này. Hãy đăng nhập GitHub bằng Git Credential Manager/GitHub Desktop, hoặc kiểm tra remote URL có đúng với repo đã từng push không.");
        }

        if (IsNonFastForwardPushError(pushResult))
        {
            AppendLog("Remote đang có commit mới. App sẽ pull về trước rồi push lại...");

            var pullResult = await RunGitAsync(projectFolder, $"pull --rebase --autostash origin {GitService.QuoteArgument(branch)}", false);
            if (!pullResult.IsSuccess && IsUnrelatedHistoriesError(pullResult))
            {
                AppendLog("Remote và local chưa có lịch sử chung. Thử merge với --allow-unrelated-histories...");
                await EnsureSuccessAsync(projectFolder, $"pull --no-rebase --allow-unrelated-histories origin {GitService.QuoteArgument(branch)}");
            }
            else if (!pullResult.IsSuccess)
            {
                throw new InvalidOperationException(BuildGitErrorMessage($"pull --rebase --autostash origin {branch}", pullResult));
            }

            await EnsureSuccessAsync(projectFolder, $"push origin {GitService.QuoteArgument(branch)}");
            return;
        }

        AppendLog("Push chưa thành công. Thử set upstream rồi push lại...");
        await EnsureSuccessAsync(projectFolder, $"push -u origin {GitService.QuoteArgument(branch)}");
    }
    private void LogSkippedSecretFiles(IReadOnlyCollection<string> secretPaths)
    {
        if (secretPaths.Count == 0)
        {
            return;
        }

        AppendLog("Bo qua cac file co the chua secret, khong dua vao commit:");
        foreach (var path in secretPaths.Take(20))
        {
            AppendLog($"- {path}");
        }
    }

    private async Task UnstageSecretFilesAsync(string projectFolder, IReadOnlyCollection<string> secretPaths)
    {
        if (secretPaths.Count == 0)
        {
            return;
        }

        var quotedPaths = string.Join(" ", secretPaths.Select(GitService.QuoteArgument));
        await RunGitAsync(projectFolder, $"restore --staged -- {quotedPaths}", false, logOutput: false);
    }

    private static IEnumerable<string> EnumerateFilesForSecretScan(string rootFolder)
    {
        var pendingDirectories = new Stack<string>();
        pendingDirectories.Push(rootFolder);

        while (pendingDirectories.Count > 0)
        {
            var currentDirectory = pendingDirectories.Pop();
            string[] files;
            string[] directories;

            try
            {
                files = Directory.GetFiles(currentDirectory);
                directories = Directory.GetDirectories(currentDirectory);
            }
            catch (UnauthorizedAccessException)
            {
                continue;
            }
            catch (IOException)
            {
                continue;
            }

            foreach (var file in files)
            {
                yield return file;
            }

            foreach (var directory in directories)
            {
                var directoryName = Path.GetFileName(directory);
                if (SecretScanIgnoredDirectories.Any(ignored =>
                    ignored.Equals(directoryName, StringComparison.OrdinalIgnoreCase)))
                {
                    continue;
                }

                pendingDirectories.Push(directory);
            }
        }
    }

    private static bool IsIgnoredScanPath(string projectFolder, string path)
    {
        var relativePath = Path.GetRelativePath(projectFolder, path);
        var parts = relativePath.Split(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        return parts.Any(part => part.Equals(".git", StringComparison.OrdinalIgnoreCase)
            || part.Equals("bin", StringComparison.OrdinalIgnoreCase)
            || part.Equals("obj", StringComparison.OrdinalIgnoreCase)
            || part.Equals("node_modules", StringComparison.OrdinalIgnoreCase));
    }

    private static bool IsPotentialSecretFile(string path)
    {
        var fileName = Path.GetFileName(path);
        return SecretExactNames.Any(name => name.Equals(fileName, StringComparison.OrdinalIgnoreCase))
            || fileName.EndsWith(".pfx", StringComparison.OrdinalIgnoreCase)
            || fileName.EndsWith(".key", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsNonFastForwardPushError(GitCommandResult result)
    {
        var text = $"{result.StandardOutput}\n{result.StandardError}";
        return text.Contains("fetch first", StringComparison.OrdinalIgnoreCase)
            || text.Contains("non-fast-forward", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Updates were rejected because the remote contains work", StringComparison.OrdinalIgnoreCase)
            || text.Contains("rejected", StringComparison.OrdinalIgnoreCase) && text.Contains("failed to push", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsUnrelatedHistoriesError(GitCommandResult result)
    {
        var text = $"{result.StandardOutput}\n{result.StandardError}";
        return text.Contains("refusing to merge unrelated histories", StringComparison.OrdinalIgnoreCase)
            || text.Contains("unrelated histories", StringComparison.OrdinalIgnoreCase);
    }
    private static bool IsAuthenticationError(GitCommandResult result)
    {
        var text = $"{result.StandardOutput}\n{result.StandardError}";
        return text.Contains("Authentication failed", StringComparison.OrdinalIgnoreCase)
            || text.Contains("could not read Username", StringComparison.OrdinalIgnoreCase)
            || text.Contains("could not read Password", StringComparison.OrdinalIgnoreCase)
            || text.Contains("terminal prompts disabled", StringComparison.OrdinalIgnoreCase)
            || text.Contains("repository not found", StringComparison.OrdinalIgnoreCase)
            || text.Contains("Permission denied", StringComparison.OrdinalIgnoreCase);
    }

    private void SetBusy(bool busy, string message)
    {
        foreach (var button in commandButtons)
        {
            button.Enabled = !busy;
        }

        progressBar.Visible = busy;
        stateLabel.Text = message;
        UseWaitCursor = busy;
    }

    private void AppendLog(string text)
    {
        if (InvokeRequired)
        {
            BeginInvoke(() => AppendLog(text));
            return;
        }

        logTextBox.AppendText(text + Environment.NewLine);
        logTextBox.SelectionStart = logTextBox.TextLength;
        logTextBox.ScrollToCaret();
    }
}

















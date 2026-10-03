namespace GitHubSimpleUploader;

public sealed class SettingsForm : Form
{
    private readonly AppSettings appSettings;
    private readonly AppSettingsService appSettingsService;
    private readonly ComboBox repositoriesComboBox = new();
    private readonly TextBox projectFolderTextBox = new();
    private readonly TextBox repoUrlTextBox = new();
    private readonly TextBox branchTextBox = new();
    private readonly TextBox commitMessageTextBox = new();
    private readonly TextBox cloneFolderNameTextBox = new();
    private bool isLoading;
    private bool isAddingNew;

    public string? SelectedRepositoryId { get; private set; }

    public SettingsForm(AppSettings appSettings, AppSettingsService appSettingsService)
    {
        this.appSettings = appSettings;
        this.appSettingsService = appSettingsService;
        SelectedRepositoryId = appSettings.LastRepositoryId;

        Text = "Cai dat repo";
        StartPosition = FormStartPosition.CenterParent;
        MinimumSize = new Size(760, 360);
        Font = new Font("Segoe UI", 10F);

        BuildLayout();
        RefreshRepositoryCombo(SelectedRepositoryId);
    }

    private void BuildLayout()
    {
        var root = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            Padding = new Padding(14),
            ColumnCount = 1,
            RowCount = 3
        };
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));
        root.RowStyles.Add(new RowStyle(SizeType.Percent, 100));
        root.RowStyles.Add(new RowStyle(SizeType.AutoSize));

        var selectorPanel = new TableLayoutPanel
        {
            Dock = DockStyle.Top,
            AutoSize = true,
            ColumnCount = 2
        };
        selectorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        selectorPanel.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        AddLabel(selectorPanel, "Repo da luu", 0);
        repositoriesComboBox.Dock = DockStyle.Fill;
        repositoriesComboBox.DropDownStyle = ComboBoxStyle.DropDownList;
        repositoriesComboBox.SelectedIndexChanged += (_, _) => LoadSelectedRepository();
        selectorPanel.Controls.Add(repositoriesComboBox, 1, 0);

        var formGrid = new TableLayoutPanel
        {
            Dock = DockStyle.Fill,
            AutoSize = true,
            ColumnCount = 3,
            Padding = new Padding(0, 12, 0, 0)
        };
        formGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 170));
        formGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Percent, 100));
        formGrid.ColumnStyles.Add(new ColumnStyle(SizeType.Absolute, 110));

        AddLabel(formGrid, "Thu muc project local", 0);
        projectFolderTextBox.Dock = DockStyle.Fill;
        formGrid.Controls.Add(projectFolderTextBox, 1, 0);
        var browseButton = new Button { Text = "Browse", Dock = DockStyle.Fill };
        browseButton.Click += (_, _) => BrowseProjectFolder();
        formGrid.Controls.Add(browseButton, 2, 0);

        AddLabel(formGrid, "GitHub repository URL", 1);
        repoUrlTextBox.Dock = DockStyle.Fill;
        formGrid.Controls.Add(repoUrlTextBox, 1, 1);
        formGrid.SetColumnSpan(repoUrlTextBox, 2);

        AddLabel(formGrid, "Branch", 2);
        branchTextBox.Text = "main";
        branchTextBox.Width = 220;
        branchTextBox.Dock = DockStyle.Left;
        formGrid.Controls.Add(branchTextBox, 1, 2);
        formGrid.SetColumnSpan(branchTextBox, 2);

        AddLabel(formGrid, "Commit message", 3);
        commitMessageTextBox.Dock = DockStyle.Fill;
        formGrid.Controls.Add(commitMessageTextBox, 1, 3);
        formGrid.SetColumnSpan(commitMessageTextBox, 2);

        AddLabel(formGrid, "Ten folder clone", 4);
        cloneFolderNameTextBox.Dock = DockStyle.Left;
        cloneFolderNameTextBox.Width = 300;
        cloneFolderNameTextBox.PlaceholderText = "Tuy chon";
        formGrid.Controls.Add(cloneFolderNameTextBox, 1, 4);
        formGrid.SetColumnSpan(cloneFolderNameTextBox, 2);

        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            AutoSize = true,
            FlowDirection = FlowDirection.RightToLeft,
            Padding = new Padding(0, 12, 0, 0)
        };

        var closeButton = new Button { Text = "Close", AutoSize = true, DialogResult = DialogResult.OK };
        var deleteButton = new Button { Text = "Delete", AutoSize = true };
        deleteButton.Click += (_, _) => DeleteSelectedRepository();
        var newButton = new Button { Text = "New", AutoSize = true };
        newButton.Click += (_, _) => StartNewRepository();
        var saveButton = new Button { Text = "Save/Update", AutoSize = true };
        saveButton.Click += (_, _) => SaveCurrentRepository();

        buttonPanel.Controls.Add(closeButton);
        buttonPanel.Controls.Add(deleteButton);
        buttonPanel.Controls.Add(newButton);
        buttonPanel.Controls.Add(saveButton);

        root.Controls.Add(selectorPanel, 0, 0);
        root.Controls.Add(formGrid, 0, 1);
        root.Controls.Add(buttonPanel, 0, 2);
        Controls.Add(root);
        AcceptButton = saveButton;
        CancelButton = closeButton;
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

    private void BrowseProjectFolder()
    {
        using var dialog = new FolderBrowserDialog
        {
            Description = "Chon thu muc project local",
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

    private void RefreshRepositoryCombo(string? selectedRepositoryId)
    {
        isLoading = true;
        try
        {
            var repositories = appSettings.Repositories
                .OrderBy(repository => repository.Name)
                .ThenBy(repository => repository.ProjectFolder)
                .ToList();

            repositoriesComboBox.DataSource = null;
            repositoriesComboBox.DataSource = repositories;

            if (repositories.Count == 0)
            {
                ClearFields();
                return;
            }

            var selectedRepository = repositories.FirstOrDefault(repository =>
                repository.Id.Equals(selectedRepositoryId, StringComparison.OrdinalIgnoreCase));
            repositoriesComboBox.SelectedItem = selectedRepository ?? repositories[0];
        }
        finally
        {
            isLoading = false;
        }

        LoadSelectedRepository();
    }

    private void LoadSelectedRepository()
    {
        if (isLoading || repositoriesComboBox.SelectedItem is not SavedRepository repository)
        {
            return;
        }

        projectFolderTextBox.Text = repository.ProjectFolder;
        repoUrlTextBox.Text = repository.RepoUrl;
        branchTextBox.Text = string.IsNullOrWhiteSpace(repository.Branch) ? "main" : repository.Branch;
        commitMessageTextBox.Text = repository.CommitMessage;
        cloneFolderNameTextBox.Text = repository.CloneFolderName;
        SelectedRepositoryId = repository.Id;
        isAddingNew = false;
    }

    private void StartNewRepository()
    {
        isLoading = true;
        try
        {
            repositoriesComboBox.SelectedIndex = -1;
        }
        finally
        {
            isLoading = false;
        }

        ClearFields();
        isAddingNew = true;
        projectFolderTextBox.Focus();
    }

    private void SaveCurrentRepository()
    {
        var projectFolder = NormalizeFolderPath(projectFolderTextBox.Text);
        var repoUrl = repoUrlTextBox.Text.Trim();

        if (string.IsNullOrWhiteSpace(projectFolder))
        {
            MessageBox.Show(this, "Vui lòng chọn thư mục project local.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(repoUrl))
        {
            MessageBox.Show(this, "Vui lòng nhập GitHub repository URL.", "Thiếu thông tin", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        SavedRepository? repository;
        if (isAddingNew || string.IsNullOrWhiteSpace(SelectedRepositoryId))
        {
            var duplicate = FindDuplicateRepository(projectFolder, repoUrl, null);
            if (duplicate is not null)
            {
                MessageBox.Show(this, "Repo này đã tồn tại trong danh sách. App sẽ chọn repo đó để bạn sửa.", "Repo đã tồn tại", MessageBoxButtons.OK, MessageBoxIcon.Information);
                RefreshRepositoryCombo(duplicate.Id);
                return;
            }

            repository = new SavedRepository();
            appSettings.Repositories.Add(repository);
        }
        else
        {
            repository = appSettings.Repositories.FirstOrDefault(item =>
                item.Id.Equals(SelectedRepositoryId, StringComparison.OrdinalIgnoreCase));
            if (repository is null)
            {
                MessageBox.Show(this, "Repo đang chọn không còn tồn tại. Bấm New để thêm repo mới.", "Không tìm thấy repo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                RefreshRepositoryCombo(appSettings.LastRepositoryId);
                return;
            }

            var duplicate = FindDuplicateRepository(projectFolder, repoUrl, repository.Id);
            if (duplicate is not null)
            {
                MessageBox.Show(this, $"Folder hoặc URL này đã thuộc repo khác:\n\n{duplicate}", "Trùng repo", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }
        }

        UpdateRepository(repository, projectFolder, repoUrl);

        isAddingNew = false;
        SelectedRepositoryId = repository.Id;
        appSettings.LastRepositoryId = repository.Id;
        appSettingsService.Save(appSettings);
        RefreshRepositoryCombo(repository.Id);
        MessageBox.Show(this, "Đã lưu repo.", "Thành công", MessageBoxButtons.OK, MessageBoxIcon.Information);
    }
    private SavedRepository? FindDuplicateRepository(string projectFolder, string repoUrl, string? ignoredRepositoryId)
    {
        return appSettings.Repositories.FirstOrDefault(repository =>
            !repository.Id.Equals(ignoredRepositoryId, StringComparison.OrdinalIgnoreCase)
            && (NormalizeFolderPath(repository.ProjectFolder).Equals(projectFolder, StringComparison.OrdinalIgnoreCase)
                || repository.RepoUrl.Trim().Equals(repoUrl, StringComparison.OrdinalIgnoreCase)));
    }

    private void UpdateRepository(SavedRepository repository, string projectFolder, string repoUrl)
    {
        repository.Name = GetRepositoryDisplayName(projectFolder, repoUrl);
        repository.ProjectFolder = projectFolder;
        repository.RepoUrl = repoUrl;
        repository.Branch = string.IsNullOrWhiteSpace(branchTextBox.Text) ? "main" : branchTextBox.Text.Trim();
        repository.CommitMessage = commitMessageTextBox.Text.Trim();
        repository.CloneFolderName = cloneFolderNameTextBox.Text.Trim();
    }

    private static string NormalizeFolderPath(string folder)
    {
        folder = folder.Trim().Trim('"');
        if (string.IsNullOrWhiteSpace(folder))
        {
            return string.Empty;
        }

        try
        {
            return Path.GetFullPath(folder);
        }
        catch
        {
            return folder;
        }
    }
    private void DeleteSelectedRepository()
    {
        if (repositoriesComboBox.SelectedItem is not SavedRepository repository)
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
        SelectedRepositoryId = appSettings.Repositories.FirstOrDefault()?.Id;
        appSettings.LastRepositoryId = SelectedRepositoryId;
        appSettingsService.Save(appSettings);
        RefreshRepositoryCombo(SelectedRepositoryId);
    }

    private void ClearFields()
    {
        projectFolderTextBox.Clear();
        repoUrlTextBox.Clear();
        branchTextBox.Text = "main";
        commitMessageTextBox.Clear();
        cloneFolderNameTextBox.Clear();
        SelectedRepositoryId = null;
        isAddingNew = true;
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
}





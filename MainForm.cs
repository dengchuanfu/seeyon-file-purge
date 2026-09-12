using System.Data;
using System.Net.Http.Json;
using System.Text;
using System.Text.Json;
using Microsoft.Data.SqlClient;

namespace SeeyonFilePurge;

public sealed class MainForm : Form
{
    private sealed record PushResult(bool Success, string ErrorMessage);

    private const string DefaultUploadRoot = @"D:\Seeyon\A8\base\upload";
    private const string DefaultOfficeTransRoot = @"D:\Seeyon\A8\base\officetrans";
    private static readonly HttpClient WebhookClient = new() { Timeout = TimeSpan.FromSeconds(60) };
    private readonly Panel headerPanel = new();
    private readonly Panel connectionPanel = new();
    private readonly Label connectionStatus = new();
    private readonly Button webhookSettingsButton = new();
    private readonly Label webhookStatus = new();
    private readonly Button storageSettingsButton = new();
    private readonly Label storageStatus = new();
    private readonly TextBox serverInput = new();
    private readonly TextBox databaseInput = new();
    private readonly TextBox usernameInput = new();
    private readonly TextBox passwordInput = new();
    private readonly Button connectButton = new();
    private readonly Panel folderPanel = new();
    private readonly TextBox folderNameInput = new();
    private readonly Button searchButton = new();
    private readonly Button deleteSelectedButton = new();
    private readonly Button deleteAllButton = new();
    private readonly Label queryStatus = new();
    private readonly DataGridView resultGrid = new();
    private SqlConnection? connection;
    private string? enterpriseWechatWebhook;
    private string uploadRoot = DefaultUploadRoot;
    private string officeTransRoot = DefaultOfficeTransRoot;

    public MainForm()
    {
        Text = "致远 OA 文件清理工具";
        StartPosition = FormStartPosition.CenterScreen;
        MinimumSize = new Size(760, 500);
        Size = new Size(920, 620);
        BackColor = Color.FromArgb(245, 247, 250);

        BuildHeader();
        BuildConnectionPanel();
        Resize += (_, _) => CenterConnectionPanel();
        FormClosed += (_, _) => connection?.Dispose();
    }

    private void BuildHeader()
    {
        headerPanel.Dock = DockStyle.Top;
        headerPanel.Height = 104;
        headerPanel.BackColor = Color.White;
        headerPanel.Padding = new Padding(28, 0, 28, 0);

        var title = new Label
        {
            Text = "致远 OA 文件清理工具",
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 15, FontStyle.Bold),
            ForeColor = Color.FromArgb(32, 48, 64),
            Location = new Point(28, 23)
        };

        connectionStatus.Text = "未连接数据库";
        connectionStatus.AutoSize = true;
        connectionStatus.Font = new Font("Microsoft YaHei UI", 10);
        connectionStatus.ForeColor = Color.FromArgb(109, 119, 130);
        connectionStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        webhookStatus.Text = "未配置消息推送";
        webhookStatus.AutoSize = true;
        webhookStatus.Font = new Font("Microsoft YaHei UI", 9);
        webhookStatus.ForeColor = Color.FromArgb(109, 119, 130);
        webhookStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        webhookSettingsButton.Text = "设置消息推送";
        webhookSettingsButton.AutoSize = false;
        webhookSettingsButton.Size = new Size(112, 32);
        webhookSettingsButton.FlatStyle = FlatStyle.Flat;
        webhookSettingsButton.FlatAppearance.BorderColor = Color.FromArgb(188, 197, 207);
        webhookSettingsButton.BackColor = Color.White;
        webhookSettingsButton.ForeColor = Color.FromArgb(46, 60, 73);
        webhookSettingsButton.Font = new Font("Microsoft YaHei UI", 9);
        webhookSettingsButton.Cursor = Cursors.Hand;
        webhookSettingsButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        webhookSettingsButton.Click += WebhookSettingsButton_Click;

        storageStatus.Text = "使用默认存储路径";
        storageStatus.AutoSize = true;
        storageStatus.Font = new Font("Microsoft YaHei UI", 9);
        storageStatus.ForeColor = Color.FromArgb(109, 119, 130);
        storageStatus.Anchor = AnchorStyles.Top | AnchorStyles.Right;

        storageSettingsButton.Text = "设置存储路径";
        storageSettingsButton.AutoSize = false;
        storageSettingsButton.Size = new Size(112, 32);
        storageSettingsButton.FlatStyle = FlatStyle.Flat;
        storageSettingsButton.FlatAppearance.BorderColor = Color.FromArgb(188, 197, 207);
        storageSettingsButton.BackColor = Color.White;
        storageSettingsButton.ForeColor = Color.FromArgb(46, 60, 73);
        storageSettingsButton.Font = new Font("Microsoft YaHei UI", 9);
        storageSettingsButton.Cursor = Cursors.Hand;
        storageSettingsButton.Anchor = AnchorStyles.Top | AnchorStyles.Right;
        storageSettingsButton.Click += StorageSettingsButton_Click;

        headerPanel.Controls.AddRange([title, storageSettingsButton, storageStatus,
            webhookSettingsButton, webhookStatus, connectionStatus]);
        headerPanel.Resize += (_, _) => LayoutHeaderActions();
        Controls.Add(headerPanel);
        LayoutHeaderActions();
    }

    private void LayoutHeaderActions()
    {
        connectionStatus.Location = new Point(headerPanel.ClientSize.Width - connectionStatus.Width - 28, 20);
        webhookStatus.Location = new Point(headerPanel.ClientSize.Width - webhookStatus.Width - 28, 69);
        webhookSettingsButton.Location = new Point(webhookStatus.Left - webhookSettingsButton.Width - 10, 59);
        storageStatus.Location = new Point(webhookSettingsButton.Left - storageStatus.Width - 22, 69);
        storageSettingsButton.Location = new Point(storageStatus.Left - storageSettingsButton.Width - 10, 59);
    }

    private void WebhookSettingsButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new Form
        {
            Text = "设置企业微信消息推送",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(580, 220),
            BackColor = Color.White,
            Font = new Font("Microsoft YaHei UI", 9)
        };
        var title = new Label
        {
            Text = "企业微信机器人 Webhook 地址",
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(32, 48, 64),
            Location = new Point(24, 24)
        };
        var hint = new Label
        {
            Text = "请输入企业微信群机器人提供的 Webhook 地址。地址仅在本次运行期间保存。",
            AutoSize = true,
            ForeColor = Color.FromArgb(109, 119, 130),
            Location = new Point(24, 57)
        };
        var input = new TextBox
        {
            Text = enterpriseWechatWebhook ?? string.Empty,
            PlaceholderText = "https://qyapi.weixin.qq.com/cgi-bin/webhook/send?key=...",
            Location = new Point(24, 91),
            Size = new Size(532, 29),
            AccessibleName = "企业微信消息推送地址"
        };
        var saveButton = new Button
        {
            Text = "保存",
            DialogResult = DialogResult.None,
            Size = new Size(92, 34),
            Location = new Point(364, 158),
            BackColor = Color.FromArgb(35, 110, 197),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        saveButton.FlatAppearance.BorderSize = 0;
        var cancelButton = new Button
        {
            Text = "取消",
            DialogResult = DialogResult.Cancel,
            Size = new Size(92, 34),
            Location = new Point(464, 158)
        };
        saveButton.Click += (_, _) =>
        {
            var value = input.Text.Trim();
            if (!IsValidEnterpriseWechatWebhook(value))
            {
                MessageBox.Show("请输入有效的企业微信机器人 Webhook 地址。地址必须使用 HTTPS，并包含 webhook/send 和 key 参数。", "地址无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                input.Focus();
                return;
            }
            enterpriseWechatWebhook = value;
            dialog.DialogResult = DialogResult.OK;
            dialog.Close();
        };
        dialog.AcceptButton = saveButton;
        dialog.CancelButton = cancelButton;
        dialog.Controls.AddRange([title, hint, input, saveButton, cancelButton]);

        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        webhookStatus.Text = "已配置消息推送";
        webhookStatus.ForeColor = Color.FromArgb(25, 126, 76);
        webhookSettingsButton.Text = "修改消息推送";
        LayoutHeaderActions();
    }

    private static bool IsValidEnterpriseWechatWebhook(string value)
    {
        if (!Uri.TryCreate(value, UriKind.Absolute, out var uri) || uri.Scheme != Uri.UriSchemeHttps)
            return false;
        return uri.Host.Equals("qyapi.weixin.qq.com", StringComparison.OrdinalIgnoreCase)
            && uri.AbsolutePath.EndsWith("/cgi-bin/webhook/send", StringComparison.OrdinalIgnoreCase)
            && uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries)
                .Any(part => part.StartsWith("key=", StringComparison.OrdinalIgnoreCase) && part.Length > 4);
    }

    private void StorageSettingsButton_Click(object? sender, EventArgs e)
    {
        using var dialog = new Form
        {
            Text = "设置 OA 文件存储路径",
            StartPosition = FormStartPosition.CenterParent,
            FormBorderStyle = FormBorderStyle.FixedDialog,
            MaximizeBox = false,
            MinimizeBox = false,
            ShowInTaskbar = false,
            ClientSize = new Size(700, 300),
            BackColor = Color.White,
            Font = new Font("Microsoft YaHei UI", 9)
        };
        var title = new Label
        {
            Text = "OA 文件存储根目录",
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 11, FontStyle.Bold),
            ForeColor = Color.FromArgb(32, 48, 64),
            Location = new Point(24, 22)
        };
        var hint = new Label
        {
            Text = "路径仅在本次运行期间保存。删除操作只允许访问以下两个根目录。",
            AutoSize = true,
            ForeColor = Color.FromArgb(109, 119, 130),
            Location = new Point(24, 53)
        };
        var uploadLabel = new Label
        {
            Text = "upload 根目录",
            AutoSize = true,
            Location = new Point(24, 88)
        };
        var uploadInput = new TextBox
        {
            Text = uploadRoot,
            Location = new Point(24, 109),
            Size = new Size(548, 29),
            AccessibleName = "upload 根目录"
        };
        var uploadBrowseButton = CreateBrowseButton(new Point(584, 108));
        uploadBrowseButton.Click += (_, _) => SelectStorageDirectory(uploadInput, "选择 upload 根目录");

        var officeLabel = new Label
        {
            Text = "officetrans 根目录",
            AutoSize = true,
            Location = new Point(24, 153)
        };
        var officeInput = new TextBox
        {
            Text = officeTransRoot,
            Location = new Point(24, 174),
            Size = new Size(548, 29),
            AccessibleName = "officetrans 根目录"
        };
        var officeBrowseButton = CreateBrowseButton(new Point(584, 173));
        officeBrowseButton.Click += (_, _) => SelectStorageDirectory(officeInput, "选择 officetrans 根目录");

        var saveButton = new Button
        {
            Text = "保存",
            Size = new Size(92, 34),
            Location = new Point(484, 240),
            BackColor = Color.FromArgb(35, 110, 197),
            ForeColor = Color.White,
            FlatStyle = FlatStyle.Flat
        };
        saveButton.FlatAppearance.BorderSize = 0;
        var cancelButton = new Button
        {
            Text = "取消",
            DialogResult = DialogResult.Cancel,
            Size = new Size(92, 34),
            Location = new Point(584, 240)
        };
        saveButton.Click += (_, _) =>
        {
            if (!TryNormalizeStorageRoot(uploadInput.Text, "upload", out var normalizedUpload, out var uploadError))
            {
                MessageBox.Show(uploadError, "upload 路径无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                uploadInput.Focus();
                return;
            }
            if (!TryNormalizeStorageRoot(officeInput.Text, "officetrans", out var normalizedOffice, out var officeError))
            {
                MessageBox.Show(officeError, "officetrans 路径无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                officeInput.Focus();
                return;
            }
            if (normalizedUpload.Equals(normalizedOffice, StringComparison.OrdinalIgnoreCase))
            {
                MessageBox.Show("upload 和 officetrans 不能设置为同一个目录。", "存储路径无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            uploadRoot = normalizedUpload;
            officeTransRoot = normalizedOffice;
            dialog.DialogResult = DialogResult.OK;
            dialog.Close();
        };
        dialog.AcceptButton = saveButton;
        dialog.CancelButton = cancelButton;
        dialog.Controls.AddRange([title, hint, uploadLabel, uploadInput, uploadBrowseButton,
            officeLabel, officeInput, officeBrowseButton, saveButton, cancelButton]);

        if (dialog.ShowDialog(this) != DialogResult.OK) return;
        storageStatus.Text = "已配置存储路径";
        storageStatus.ForeColor = Color.FromArgb(25, 126, 76);
        storageSettingsButton.Text = "修改存储路径";
        LayoutHeaderActions();
    }

    private static Button CreateBrowseButton(Point location) => new()
    {
        Text = "选择...",
        Size = new Size(92, 30),
        Location = location,
        FlatStyle = FlatStyle.System,
        Cursor = Cursors.Hand
    };

    private static void SelectStorageDirectory(TextBox input, string description)
    {
        using var browser = new FolderBrowserDialog
        {
            Description = description,
            UseDescriptionForTitle = true,
            ShowNewFolderButton = false,
            SelectedPath = Directory.Exists(input.Text.Trim()) ? input.Text.Trim() : string.Empty
        };
        if (browser.ShowDialog() == DialogResult.OK)
            input.Text = browser.SelectedPath;
    }

    private static bool TryNormalizeStorageRoot(
        string value,
        string expectedFolderName,
        out string normalizedPath,
        out string errorMessage)
    {
        normalizedPath = string.Empty;
        errorMessage = string.Empty;
        try
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                errorMessage = $"请输入 {expectedFolderName} 根目录。";
                return false;
            }
            normalizedPath = Path.GetFullPath(value.Trim()).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            if (!Directory.Exists(normalizedPath))
            {
                errorMessage = $"目录不存在：{normalizedPath}";
                return false;
            }
            if (!Path.GetFileName(normalizedPath).Equals(expectedFolderName, StringComparison.OrdinalIgnoreCase))
            {
                errorMessage = $"请选择名称为 {expectedFolderName} 的根目录。";
                return false;
            }
            return true;
        }
        catch (Exception ex) when (ex is ArgumentException or NotSupportedException or PathTooLongException)
        {
            errorMessage = "路径格式无效。";
            return false;
        }
    }

    private void BuildConnectionPanel()
    {
        connectionPanel.Size = new Size(460, 425);
        connectionPanel.BackColor = Color.White;
        connectionPanel.Padding = new Padding(42, 32, 42, 32);
        connectionPanel.BorderStyle = BorderStyle.FixedSingle;

        var heading = new Label
        {
            Text = "连接 OA 数据库",
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 14, FontStyle.Bold),
            ForeColor = Color.FromArgb(32, 48, 64),
            Location = new Point(42, 30)
        };
        var hint = new Label
        {
            Text = "请输入 SQL Server 数据库连接信息",
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 9),
            ForeColor = Color.FromArgb(109, 119, 130),
            Location = new Point(42, 62)
        };

        AddField("服务器", serverInput, "例如：服务器地址,1433", 96);
        AddField("数据库", databaseInput, "OA 数据库名称", 156);
        AddField("账户", usernameInput, "数据库账户", 216);
        AddField("密码", passwordInput, "数据库密码", 276);
        passwordInput.UseSystemPasswordChar = true;

        connectButton.Text = "验证并连接";
        connectButton.Size = new Size(376, 40);
        connectButton.Location = new Point(42, 350);
        connectButton.FlatStyle = FlatStyle.Flat;
        connectButton.FlatAppearance.BorderSize = 0;
        connectButton.BackColor = Color.FromArgb(35, 110, 197);
        connectButton.ForeColor = Color.White;
        connectButton.Font = new Font("Microsoft YaHei UI", 10, FontStyle.Bold);
        connectButton.Cursor = Cursors.Hand;
        connectButton.Click += ConnectButton_Click;

        connectionPanel.Controls.AddRange([heading, hint, serverInput, databaseInput, usernameInput, passwordInput, connectButton]);
        Controls.Add(connectionPanel);
        CenterConnectionPanel();
    }

    private void AddField(string label, TextBox input, string placeholder, int top)
    {
        var fieldLabel = new Label
        {
            Text = label,
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 9),
            ForeColor = Color.FromArgb(66, 78, 90),
            Location = new Point(42, top)
        };
        input.Name = label;
        input.PlaceholderText = placeholder;
        input.Font = new Font("Microsoft YaHei UI", 10);
        input.Size = new Size(376, 29);
        input.Location = new Point(42, top + 20);
        input.AccessibleName = label;
        connectionPanel.Controls.Add(fieldLabel);
    }

    private async void ConnectButton_Click(object? sender, EventArgs e)
    {
        if (string.IsNullOrWhiteSpace(serverInput.Text) || string.IsNullOrWhiteSpace(databaseInput.Text) ||
            string.IsNullOrWhiteSpace(usernameInput.Text) || string.IsNullOrWhiteSpace(passwordInput.Text))
        {
            MessageBox.Show("请填写服务器、数据库、账户和密码。", "连接信息不完整", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        connectButton.Enabled = false;
        connectButton.Text = "正在验证...";

        try
        {
            connection?.Dispose();
            var builder = new SqlConnectionStringBuilder
            {
                DataSource = serverInput.Text.Trim(),
                InitialCatalog = databaseInput.Text.Trim(),
                UserID = usernameInput.Text.Trim(),
                Password = passwordInput.Text,
                Encrypt = true,
                TrustServerCertificate = true,
                ConnectTimeout = 10
            };

            connection = new SqlConnection(builder.ConnectionString);
            await connection.OpenAsync();
            connectionPanel.Visible = false;
            connectionStatus.Text = "已连接数据库";
            connectionStatus.ForeColor = Color.FromArgb(25, 126, 76);
            LayoutHeaderActions();
            ShowFolderQueryPanel();
        }
        catch (SqlException)
        {
            connection?.Dispose();
            connection = null;
            MessageBox.Show("无法连接数据库。请检查服务器地址、数据库名称、账户密码及网络连通性。", "连接失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            if (connectionPanel.Visible)
            {
                connectButton.Enabled = true;
                connectButton.Text = "验证并连接";
            }
        }
    }

    private void CenterConnectionPanel()
    {
        connectionPanel.Location = new Point(
            Math.Max(20, (ClientSize.Width - connectionPanel.Width) / 2),
            Math.Max(100, 100 + (ClientSize.Height - 100 - connectionPanel.Height) / 2));
    }

    private void ShowFolderQueryPanel()
    {
        folderPanel.Dock = DockStyle.Fill;
        folderPanel.Padding = new Padding(28, 30, 28, 28);
        folderPanel.BackColor = Color.FromArgb(245, 247, 250);

        var heading = new Label
        {
            Text = "查询待删除文件夹中的文件",
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 14, FontStyle.Bold),
            ForeColor = Color.FromArgb(32, 48, 64),
            Location = new Point(28, 28)
        };
        var hint = new Label
        {
            Text = "输入文件夹名称后，查询该文件夹及其子文件夹中的所有文件。",
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 9),
            ForeColor = Color.FromArgb(109, 119, 130),
            Location = new Point(28, 58)
        };
        var folderLabel = new Label
        {
            Text = "待删除文件夹名称",
            AutoSize = true,
            Font = new Font("Microsoft YaHei UI", 9),
            ForeColor = Color.FromArgb(66, 78, 90),
            Location = new Point(28, 97)
        };

        folderNameInput.PlaceholderText = "例如：文控中心/PCB图纸";
        folderNameInput.Font = new Font("Microsoft YaHei UI", 10);
        folderNameInput.Location = new Point(28, 118);
        folderNameInput.Size = new Size(380, 29);
        folderNameInput.AccessibleName = "待删除文件夹名称";
        folderNameInput.KeyDown += (_, eventArgs) =>
        {
            if (eventArgs.KeyCode == Keys.Enter)
            {
                SearchButton_Click(searchButton, EventArgs.Empty);
                eventArgs.SuppressKeyPress = true;
            }
        };

        searchButton.Text = "查询文件";
        searchButton.Size = new Size(104, 29);
        searchButton.Location = new Point(420, 118);
        searchButton.FlatStyle = FlatStyle.Flat;
        searchButton.FlatAppearance.BorderSize = 0;
        searchButton.BackColor = Color.FromArgb(35, 110, 197);
        searchButton.ForeColor = Color.White;
        searchButton.Font = new Font("Microsoft YaHei UI", 9, FontStyle.Bold);
        searchButton.Cursor = Cursors.Hand;
        searchButton.Click += SearchButton_Click;

        queryStatus.AutoSize = true;
        queryStatus.Font = new Font("Microsoft YaHei UI", 9);
        queryStatus.ForeColor = Color.FromArgb(109, 119, 130);
        queryStatus.Location = new Point(28, 169);
        queryStatus.Text = "请输入文件夹名称进行查询。";

        resultGrid.Location = new Point(28, 199);
        resultGrid.Anchor = AnchorStyles.Top | AnchorStyles.Bottom | AnchorStyles.Left | AnchorStyles.Right;
        resultGrid.Size = new Size(folderPanel.ClientSize.Width - 56, folderPanel.ClientSize.Height - 227);
        resultGrid.BackgroundColor = Color.White;
        resultGrid.BorderStyle = BorderStyle.FixedSingle;
        resultGrid.AllowUserToAddRows = false;
        resultGrid.AllowUserToDeleteRows = false;
        resultGrid.AllowUserToOrderColumns = false;
        resultGrid.AllowUserToResizeRows = false;
        resultGrid.AutoSizeColumnsMode = DataGridViewAutoSizeColumnsMode.Fill;
        resultGrid.ColumnHeadersDefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9, FontStyle.Bold);
        resultGrid.DefaultCellStyle.Font = new Font("Microsoft YaHei UI", 9);
        resultGrid.ReadOnly = true;
        resultGrid.RowHeadersVisible = false;
        resultGrid.SelectionMode = DataGridViewSelectionMode.FullRowSelect;
        resultGrid.MultiSelect = true;

        deleteSelectedButton.Text = "删除选中项";
        deleteSelectedButton.Size = new Size(120, 32);
        deleteSelectedButton.Location = new Point(28, 164);
        ConfigureDeleteButton(deleteSelectedButton);
        deleteSelectedButton.Click += async (_, _) => await DeleteResourcesAsync(false);

        deleteAllButton.Text = "删除全部查询结果";
        deleteAllButton.Size = new Size(150, 32);
        deleteAllButton.Location = new Point(158, 164);
        ConfigureDeleteButton(deleteAllButton);
        deleteAllButton.Click += async (_, _) => await DeleteResourcesAsync(true);

        queryStatus.Location = new Point(325, 173);
        resultGrid.Location = new Point(28, 209);
        resultGrid.Size = new Size(folderPanel.ClientSize.Width - 56, folderPanel.ClientSize.Height - 237);

        folderPanel.Controls.AddRange([heading, hint, folderLabel, folderNameInput, searchButton,
            deleteSelectedButton, deleteAllButton, queryStatus, resultGrid]);
        Controls.Add(folderPanel);
        folderPanel.BringToFront();
    }

    private async void SearchButton_Click(object? sender, EventArgs e)
    {
        if (connection is null || connection.State != ConnectionState.Open)
        {
            MessageBox.Show("数据库连接已断开，请重新启动程序并连接数据库。", "无法查询", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        if (string.IsNullOrWhiteSpace(folderNameInput.Text))
        {
            MessageBox.Show("请输入待删除文件夹名称。", "查询条件不完整", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            folderNameInput.Focus();
            return;
        }

        searchButton.Enabled = false;
        searchButton.Text = "正在查询...";
        queryStatus.Text = "正在查询文件，请稍候...";

        try
        {
            using var command = connection.CreateCommand();
            var pathParts = folderNameInput.Text.Trim()
                .Split('/', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);
            if (pathParts.Length == 0 || pathParts.Any(string.IsNullOrWhiteSpace))
                throw new InvalidOperationException("文件夹路径不能为空。");

            var rootJoins = new StringBuilder();
            var rootWhere = new StringBuilder($"target.FR_NAME = @path{pathParts.Length - 1} AND target.IS_FOLDER = 1");
            for (var index = 1; index < pathParts.Length; index++)
            {
                var alias = $"parent{index}";
                var childAlias = index == 1 ? "target" : $"parent{index - 1}";
                rootJoins.AppendLine($"INNER JOIN DOC_RESOURCES AS {alias} ON {childAlias}.PARENT_FR_ID = {alias}.ID");
                rootWhere.Append($" AND {alias}.FR_NAME = @path{pathParts.Length - 1 - index}");
            }

            command.CommandText = $"""
                WITH FolderRoots AS (
                    SELECT
                        target.ID, target.FR_NAME, target.PARENT_FR_ID, target.LOGICAL_PATH, target.IS_FOLDER, target.SOURCE_ID, target.FR_SIZE,
                        CAST(
                            N'0:' + ISNULL(target.FR_NAME, N'') + N':' +
                            RIGHT(REPLICATE(N'0', 30) + CONVERT(nvarchar(30), target.ID), 30) + N'/'
                            AS nvarchar(max)
                        ) AS SORT_PATH
                    FROM DOC_RESOURCES AS target
                    {rootJoins}
                    WHERE {rootWhere}
                ),
                ResourceTree AS (
                    SELECT ID, FR_NAME, PARENT_FR_ID, LOGICAL_PATH, IS_FOLDER, SOURCE_ID, FR_SIZE, SORT_PATH
                    FROM FolderRoots
                    UNION ALL
                    SELECT
                        child.ID, child.FR_NAME, child.PARENT_FR_ID, child.LOGICAL_PATH, child.IS_FOLDER, child.SOURCE_ID, child.FR_SIZE,
                        CAST(
                            parent.SORT_PATH +
                            CASE WHEN child.IS_FOLDER = 1 THEN N'0:' ELSE N'1:' END +
                            ISNULL(child.FR_NAME, N'') + N':' +
                            RIGHT(REPLICATE(N'0', 30) + CONVERT(nvarchar(30), child.ID), 30) + N'/'
                            AS nvarchar(max)
                        )
                    FROM DOC_RESOURCES AS child
                    INNER JOIN ResourceTree AS parent ON child.PARENT_FR_ID = parent.ID
                )
                SELECT
                    ResourceTree.ID AS [文件ID],
                    ResourceTree.FR_NAME AS [文件名称],
                    fileInfo.ID AS [物理文件ID],
                    COALESCE(fileInfo.FILE_SIZE, ResourceTree.FR_SIZE, 0) AS [统计源文件大小]
                FROM ResourceTree
                LEFT JOIN CTP_FILE AS fileInfo ON fileInfo.ID = ResourceTree.SOURCE_ID
                WHERE ResourceTree.IS_FOLDER = 0
                ORDER BY ResourceTree.SORT_PATH
                OPTION (MAXRECURSION 32767);
                """;
            for (var index = 0; index < pathParts.Length; index++)
                command.Parameters.AddWithValue($"@path{index}", pathParts[index]);

            using var reader = await command.ExecuteReaderAsync();
            var result = new DataTable();
            result.Load(reader);
            resultGrid.DataSource = result;
            resultGrid.Columns["文件ID"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            resultGrid.Columns["物理文件ID"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            resultGrid.Columns["文件名称"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            resultGrid.Columns["文件名称"].FillWeight = 100;
            resultGrid.Columns["统计源文件大小"].Visible = false;
            queryStatus.Text = $"查询完成，共找到 {result.Rows.Count} 个文件；物理文件仅供核对，不会被删除。";
        }
        catch (InvalidOperationException ex)
        {
            resultGrid.DataSource = null;
            queryStatus.Text = "查询条件无效。";
            MessageBox.Show(ex.Message, "查询条件无效", MessageBoxButtons.OK, MessageBoxIcon.Warning);
        }
        catch (SqlException ex)
        {
            resultGrid.DataSource = null;
            queryStatus.Text = "查询失败。";
            MessageBox.Show(
                $"查询文件记录时数据库返回错误。\n\n错误编号：{ex.Number}\n错误信息：{ex.Message}\n\n请将以上内容截图或复制后提供。",
                "查询失败", MessageBoxButtons.OK, MessageBoxIcon.Error);
        }
        finally
        {
            searchButton.Enabled = true;
            searchButton.Text = "查询文件";
        }
    }

    private static void ConfigureDeleteButton(Button button)
    {
        button.FlatStyle = FlatStyle.Flat;
        button.FlatAppearance.BorderSize = 0;
        button.BackColor = Color.FromArgb(190, 61, 61);
        button.ForeColor = Color.White;
        button.Font = new Font("Microsoft YaHei UI", 9, FontStyle.Bold);
        button.Cursor = Cursors.Hand;
    }

    private async Task DeleteResourcesAsync(bool deleteAll)
    {
        if (connection is null || connection.State != ConnectionState.Open)
        {
            MessageBox.Show("数据库连接已断开，请重新启动程序并连接数据库。", "无法删除", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            return;
        }

        var rows = (deleteAll ? resultGrid.Rows.Cast<DataGridViewRow>() : resultGrid.SelectedRows.Cast<DataGridViewRow>())
            .Where(row => !row.IsNewRow).ToList();
        var items = rows.Select(row => new
            {
                ResourceId = Convert.ToString(row.Cells["文件ID"].Value),
                PhysicalId = Convert.ToString(row.Cells["物理文件ID"].Value),
                FileName = Convert.ToString(row.Cells["文件名称"].Value) ?? "未命名文件",
                FileSize = Convert.ToInt64(row.Cells["统计源文件大小"].Value ?? 0L)
            })
            .Where(item => long.TryParse(item.ResourceId, out _) && !string.IsNullOrWhiteSpace(item.PhysicalId))
            .Select(item => (ResourceId: long.Parse(item.ResourceId!), PhysicalId: item.PhysicalId!, item.FileName, item.FileSize))
            .Distinct().ToList();
        var resourceIds = items.Select(item => item.ResourceId).Distinct().ToList();
        if (items.Count == 0)
        {
            MessageBox.Show(deleteAll ? "当前没有可删除的查询结果。" : "请先选中要删除的文件。", "没有选择文件", MessageBoxButtons.OK, MessageBoxIcon.Information);
            return;
        }

        var scope = deleteAll ? "全部查询结果" : "选中的文件";
        var confirm = MessageBox.Show($"确定删除{scope}（共 {items.Count} 个）吗？\n\n将删除 upload 中的物理文件、officetrans 中同名物理 ID 文件夹，以及 OA 显示记录。此操作不可恢复。", "确认删除", MessageBoxButtons.YesNo, MessageBoxIcon.Warning, MessageBoxDefaultButton.Button2);
        if (confirm != DialogResult.Yes) return;

        deleteSelectedButton.Enabled = false; deleteAllButton.Enabled = false;
        queryStatus.Text = "正在删除服务器源文件和 OA 记录，请稍候...";
        try
        {
            if (!Directory.Exists(uploadRoot) || !Directory.Exists(officeTransRoot))
                throw new IOException("未找到 upload 或 officetrans 目录，请确认 OA 路径。 ");
            var physicalIds = items.Select(item => item.PhysicalId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var uploadCount = 0; var officeCount = 0; long officeTransSize = 0;
            foreach (var path in Directory.EnumerateFiles(uploadRoot, "*", SearchOption.AllDirectories)
                .Where(path => physicalIds.Contains(Path.GetFileName(path))).ToList())
            {
                EnsureUnderRoot(path, uploadRoot); File.Delete(path); uploadCount++;
            }
            foreach (var dateDir in Directory.EnumerateDirectories(officeTransRoot))
            {
                foreach (var target in Directory.EnumerateDirectories(dateDir)
                    .Where(path => physicalIds.Contains(Path.GetFileName(path))).ToList())
                {
                    EnsureUnderRoot(target, officeTransRoot);
                    officeTransSize += GetDirectorySize(target);
                    Directory.Delete(target, true); officeCount++;
                }
            }
            await using var transaction = await connection.BeginTransactionAsync();
            var affected = 0;
            const int batchSize = 500;
            foreach (var batch in resourceIds.Chunk(batchSize))
            {
                await using var command = connection.CreateCommand();
                command.Transaction = (SqlTransaction)transaction;
                var parameters = new List<string>();
                for (var i = 0; i < batch.Length; i++)
                {
                    var name = $"@id{i}";
                    parameters.Add(name);
                    command.Parameters.Add(name, System.Data.SqlDbType.BigInt).Value = batch[i];
                }
                command.CommandText = $"DELETE FROM DOC_RESOURCES WHERE IS_FOLDER = 0 AND ID IN ({string.Join(",", parameters)});";
                affected += await command.ExecuteNonQueryAsync();
            }
            await transaction.CommitAsync();
            foreach (var row in rows.OrderByDescending(row => row.Index)) if (row.DataBoundItem is DataRowView view) view.Row.Delete();
            queryStatus.Text = $"删除完成：OA 记录 {affected} 条，upload 文件 {uploadCount} 个，officetrans 文件夹 {officeCount} 个。";
            if (!string.IsNullOrWhiteSpace(enterpriseWechatWebhook))
            {
                queryStatus.Text += " 正在推送企业微信消息...";
                var pushResult = await SendDeleteNotificationAsync(
                    items.Select(item => (item.ResourceId, item.FileName, item.PhysicalId, item.FileSize)).ToList(),
                    items.Sum(item => item.FileSize),
                    affected, uploadCount, officeCount);
                queryStatus.Text = pushResult.Success
                    ? $"删除完成并已推送消息：OA 记录 {affected} 条，upload 文件 {uploadCount} 个，officetrans 文件夹 {officeCount} 个。"
                    : $"删除完成，但消息推送失败：OA 记录 {affected} 条，upload 文件 {uploadCount} 个，officetrans 文件夹 {officeCount} 个。";
                if (!pushResult.Success)
                    MessageBox.Show($"文件已删除，但企业微信消息推送失败。\n\n{pushResult.ErrorMessage}", "消息推送失败", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
        }
        catch (SqlException ex) { queryStatus.Text = "数据库删除失败，事务已回滚；请核对服务器文件。"; MessageBox.Show($"数据库删除失败，事务已回滚；服务器文件删除无法自动恢复，请立即核对。\n\n错误编号：{ex.Number}\n错误信息：{ex.Message}", "删除失败", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        catch (UnauthorizedAccessException) { queryStatus.Text = "没有删除服务器文件的权限。"; MessageBox.Show("没有删除服务器文件的权限，请使用具备目录写入权限的账户运行。", "权限不足", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        catch (IOException ex) { queryStatus.Text = "服务器文件删除失败。"; MessageBox.Show($"服务器文件删除失败，OA 记录未删除。\n\n{ex.Message}", "删除失败", MessageBoxButtons.OK, MessageBoxIcon.Error); }
        finally { deleteSelectedButton.Enabled = true; deleteAllButton.Enabled = true; }
    }

    private static void EnsureUnderRoot(string path, string root)
    {
        var fullPath = Path.GetFullPath(path);
        var fullRoot = Path.GetFullPath(root).TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar;
        if (!fullPath.StartsWith(fullRoot, StringComparison.OrdinalIgnoreCase)) throw new IOException("检测到超出 OA 存储根目录的路径，操作已中止。 ");
    }

    private static long GetDirectorySize(string directory)
    {
        long total = 0;
        foreach (var file in Directory.EnumerateFiles(directory, "*", SearchOption.AllDirectories))
        {
            try { total += new FileInfo(file).Length; }
            catch (FileNotFoundException) { }
            catch (DirectoryNotFoundException) { }
        }
        return total;
    }

    private async Task<PushResult> SendDeleteNotificationAsync(
        IReadOnlyList<(long ResourceId, string FileName, string PhysicalId, long FileSize)> files,
        long totalFileSize,
        int databaseCount,
        int uploadCount,
        int officeTransCount)
    {
        if (string.IsNullOrWhiteSpace(enterpriseWechatWebhook)) return new(false, "未配置企业微信 Webhook 地址。");
        var webhookUri = new Uri(enterpriseWechatWebhook);
        var key = GetQueryParameter(webhookUri, "key");
        if (string.IsNullOrWhiteSpace(key)) return new(false, "Webhook 地址中缺少 key 参数。");
        var timestamp = DateTime.Now;
        var temporaryDirectory = Path.Combine(Path.GetTempPath(), "SeeyonFilePurge", Guid.NewGuid().ToString("N"));
        var uploadFileName = $"Seeyon-OA-Delete-Details-{timestamp:yyyyMMdd-HHmmss}.csv";
        var temporaryPath = Path.Combine(temporaryDirectory, uploadFileName);

        try
        {
            Directory.CreateDirectory(temporaryDirectory);
            var csv = new StringBuilder();
            csv.AppendLine("项目,内容");
            csv.AppendLine($"查询文件夹,{EscapeCsv(folderNameInput.Text.Trim())}");
            csv.AppendLine($"删除文件数,{files.Count}");
            csv.AppendLine($"文件总大小,{EscapeCsv(FormatFileSize(totalFileSize))}");
            csv.AppendLine("统计说明,文件总大小按 CTP_FILE.FILE_SIZE 汇总；officetrans 被删除空间未计入");
            csv.AppendLine($"OA记录数,{databaseCount}");
            csv.AppendLine($"OA服务器,{EscapeCsv(Environment.MachineName)}");
            csv.AppendLine($"操作时间,{timestamp:yyyy-MM-dd HH:mm:ss}");
            csv.AppendLine();
            csv.AppendLine("文件ID,文件名称,物理文件ID,文件大小");
            foreach (var file in files)
                csv.AppendLine($"{file.ResourceId},{EscapeCsv(file.FileName)},{EscapeCsv(file.PhysicalId)},{EscapeCsv(FormatFileSize(file.FileSize))}");
            await File.WriteAllTextAsync(temporaryPath, csv.ToString(), new UTF8Encoding(true));

            var uploadUri = new UriBuilder(webhookUri)
            {
                Path = "/cgi-bin/webhook/upload_media",
                Query = $"key={Uri.EscapeDataString(key)}&type=file"
            }.Uri;
            var fileBytes = await File.ReadAllBytesAsync(temporaryPath);
            using var uploadContent = CreateWechatUploadContent(fileBytes, uploadFileName);
            using var uploadResponse = await WebhookClient.PostAsync(uploadUri, uploadContent);
            if (!uploadResponse.IsSuccessStatusCode)
                return new(false, $"上传明细文档时接口返回 HTTP 状态码 {(int)uploadResponse.StatusCode}。");
            var uploadResult = await uploadResponse.Content.ReadAsStringAsync();
            using var uploadDocument = JsonDocument.Parse(uploadResult);
            var uploadError = GetWechatError(uploadDocument.RootElement, "上传明细文档");
            if (uploadError is not null) return new(false, uploadError);
            if (!uploadDocument.RootElement.TryGetProperty("media_id", out var mediaIdElement))
                return new(false, "上传明细文档成功，但企业微信响应中没有 media_id。");
            var mediaId = mediaIdElement.GetString();
            if (string.IsNullOrWhiteSpace(mediaId)) return new(false, "企业微信返回的 media_id 为空。");

            var payload = new { msgtype = "file", file = new { media_id = mediaId } };
            using var sendResponse = await WebhookClient.PostAsJsonAsync(enterpriseWechatWebhook, payload);
            if (!sendResponse.IsSuccessStatusCode)
                return new(false, $"发送明细文档时接口返回 HTTP 状态码 {(int)sendResponse.StatusCode}。");
            var sendResult = await sendResponse.Content.ReadAsStringAsync();
            using var sendDocument = JsonDocument.Parse(sendResult);
            var sendError = GetWechatError(sendDocument.RootElement, "发送明细文档");
            return sendError is null ? new(true, string.Empty) : new(false, sendError);
        }
        catch (HttpRequestException) { return new(false, "请求企业微信接口失败，请检查服务器网络连接。"); }
        catch (TaskCanceledException) { return new(false, "请求企业微信接口超时。"); }
        catch (JsonException) { return new(false, "企业微信接口返回了无法识别的数据。"); }
        catch (IOException) { return new(false, "生成或读取临时明细文档失败。"); }
        finally
        {
            try { if (Directory.Exists(temporaryDirectory)) Directory.Delete(temporaryDirectory, true); }
            catch (IOException) { }
            catch (UnauthorizedAccessException) { }
        }
    }

    private static string? GetWechatError(JsonElement root, string operation)
    {
        if (!root.TryGetProperty("errcode", out var errorCode) || !errorCode.TryGetInt32(out var code))
            return $"{operation}时，企业微信响应中缺少错误码。";
        if (code == 0) return null;
        var message = root.TryGetProperty("errmsg", out var errorMessage)
            ? errorMessage.GetString() ?? "未提供错误信息"
            : "未提供错误信息";
        return $"{operation}失败。企业微信错误码：{code}，错误信息：{message}";
    }

    private static string? GetQueryParameter(Uri uri, string name)
    {
        foreach (var part in uri.Query.TrimStart('?').Split('&', StringSplitOptions.RemoveEmptyEntries))
        {
            var pair = part.Split('=', 2);
            if (pair.Length == 2 && pair[0].Equals(name, StringComparison.OrdinalIgnoreCase))
                return Uri.UnescapeDataString(pair[1]);
        }
        return null;
    }

    private static ByteArrayContent CreateWechatUploadContent(byte[] fileBytes, string fileName)
    {
        var boundary = "--------------------------" + Guid.NewGuid().ToString("N");
        var header = $"--{boundary}\r\n"
            + $"Content-Disposition: form-data; name=\"media\"; filename=\"{fileName}\"; filelength={fileBytes.Length}\r\n"
            + "Content-Type: application/octet-stream\r\n\r\n";
        var footer = $"\r\n--{boundary}--\r\n";
        var headerBytes = Encoding.ASCII.GetBytes(header);
        var footerBytes = Encoding.ASCII.GetBytes(footer);
        var body = new byte[headerBytes.Length + fileBytes.Length + footerBytes.Length];
        Buffer.BlockCopy(headerBytes, 0, body, 0, headerBytes.Length);
        Buffer.BlockCopy(fileBytes, 0, body, headerBytes.Length, fileBytes.Length);
        Buffer.BlockCopy(footerBytes, 0, body, headerBytes.Length + fileBytes.Length, footerBytes.Length);

        var content = new ByteArrayContent(body);
        content.Headers.TryAddWithoutValidation("Content-Type", $"multipart/form-data; boundary={boundary}");
        return content;
    }

    private static string EscapeCsv(string value) => $"\"{value.Replace("\"", "\"\"")}\"";

    private static string FormatFileSize(long bytes)
    {
        if (bytes < 0) bytes = 0;
        string[] units = ["字节", "KB", "MB", "GB", "TB"];
        var value = (double)bytes;
        var unitIndex = 0;
        while (value >= 1024 && unitIndex < units.Length - 1)
        {
            value /= 1024;
            unitIndex++;
        }
        return unitIndex == 0
            ? $"{bytes:N0} {units[unitIndex]}"
            : $"{value:N2} {units[unitIndex]}（{bytes:N0} 字节）";
    }
}

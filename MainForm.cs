using System.Data;
using Microsoft.Data.SqlClient;

namespace SeeyonFilePurge;

public sealed class MainForm : Form
{
    private const string UploadRoot = @"D:\Seeyon\A8\base\upload";
    private const string OfficeTransRoot = @"D:\Seeyon\A8\base\officetrans";
    private readonly Panel connectionPanel = new();
    private readonly Label connectionStatus = new();
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
        var header = new Panel
        {
            Dock = DockStyle.Top,
            Height = 72,
            BackColor = Color.White,
            Padding = new Padding(28, 0, 28, 0)
        };

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
        header.Controls.Add(title);
        header.Controls.Add(connectionStatus);
        header.Resize += (_, _) =>
            connectionStatus.Location = new Point(header.ClientSize.Width - connectionStatus.Width - 28, 27);

        Controls.Add(header);
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

        folderNameInput.PlaceholderText = "例如：产品承认书";
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
            command.CommandText = """
                WITH FolderRoots AS (
                    SELECT
                        ID, FR_NAME, PARENT_FR_ID, LOGICAL_PATH, IS_FOLDER, SOURCE_ID,
                        CAST(
                            N'0:' + ISNULL(FR_NAME, N'') + N':' +
                            RIGHT(REPLICATE(N'0', 30) + CONVERT(nvarchar(30), ID), 30) + N'/'
                            AS nvarchar(max)
                        ) AS SORT_PATH
                    FROM DOC_RESOURCES
                    WHERE FR_NAME = @folderName AND IS_FOLDER = 1
                ),
                ResourceTree AS (
                    SELECT ID, FR_NAME, PARENT_FR_ID, LOGICAL_PATH, IS_FOLDER, SOURCE_ID, SORT_PATH
                    FROM FolderRoots
                    UNION ALL
                    SELECT
                        child.ID, child.FR_NAME, child.PARENT_FR_ID, child.LOGICAL_PATH, child.IS_FOLDER, child.SOURCE_ID,
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
                    fileInfo.ID AS [物理文件ID]
                FROM ResourceTree
                LEFT JOIN CTP_FILE AS fileInfo ON fileInfo.ID = ResourceTree.SOURCE_ID
                WHERE ResourceTree.IS_FOLDER = 0
                ORDER BY ResourceTree.SORT_PATH
                OPTION (MAXRECURSION 32767);
                """;
            command.Parameters.AddWithValue("@folderName", folderNameInput.Text.Trim());

            using var reader = await command.ExecuteReaderAsync();
            var result = new DataTable();
            result.Load(reader);
            resultGrid.DataSource = result;
            resultGrid.Columns["文件ID"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            resultGrid.Columns["物理文件ID"].AutoSizeMode = DataGridViewAutoSizeColumnMode.AllCells;
            resultGrid.Columns["文件名称"].AutoSizeMode = DataGridViewAutoSizeColumnMode.Fill;
            resultGrid.Columns["文件名称"].FillWeight = 100;
            queryStatus.Text = $"查询完成，共找到 {result.Rows.Count} 个文件；物理文件仅供核对，不会被删除。";
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
                PhysicalId = Convert.ToString(row.Cells["物理文件ID"].Value)
            })
            .Where(item => long.TryParse(item.ResourceId, out _) && !string.IsNullOrWhiteSpace(item.PhysicalId))
            .Select(item => (ResourceId: long.Parse(item.ResourceId!), PhysicalId: item.PhysicalId!))
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
            if (!Directory.Exists(UploadRoot) || !Directory.Exists(OfficeTransRoot))
                throw new IOException("未找到 upload 或 officetrans 目录，请确认 OA 路径。 ");
            var physicalIds = items.Select(item => item.PhysicalId).ToHashSet(StringComparer.OrdinalIgnoreCase);
            var uploadCount = 0; var officeCount = 0;
            foreach (var path in Directory.EnumerateFiles(UploadRoot, "*", SearchOption.AllDirectories)
                .Where(path => physicalIds.Contains(Path.GetFileName(path))).ToList())
            {
                EnsureUnderRoot(path, UploadRoot); File.Delete(path); uploadCount++;
            }
            foreach (var dateDir in Directory.EnumerateDirectories(OfficeTransRoot))
            {
                foreach (var target in Directory.EnumerateDirectories(dateDir)
                    .Where(path => physicalIds.Contains(Path.GetFileName(path))).ToList())
                {
                    EnsureUnderRoot(target, OfficeTransRoot); Directory.Delete(target, true); officeCount++;
                }
            }
            await using var transaction = await connection.BeginTransactionAsync();
            await using var command = connection.CreateCommand(); command.Transaction = (SqlTransaction)transaction;
            var parameters = new List<string>();
            for (var i = 0; i < resourceIds.Count; i++) { var name = $"@id{i}"; parameters.Add(name); command.Parameters.Add(name, System.Data.SqlDbType.BigInt).Value = resourceIds[i]; }
            command.CommandText = $"DELETE FROM DOC_RESOURCES WHERE IS_FOLDER = 0 AND ID IN ({string.Join(",", parameters)});";
            var affected = await command.ExecuteNonQueryAsync(); await transaction.CommitAsync();
            foreach (var row in rows.OrderByDescending(row => row.Index)) if (row.DataBoundItem is DataRowView view) view.Row.Delete();
            queryStatus.Text = $"删除完成：OA 记录 {affected} 条，upload 文件 {uploadCount} 个，officetrans 文件夹 {officeCount} 个。";
        }
        catch (SqlException) { queryStatus.Text = "数据库删除失败，事务已回滚；请核对服务器文件。"; MessageBox.Show("数据库删除失败，事务已回滚；服务器文件删除无法自动恢复，请立即核对。", "删除失败", MessageBoxButtons.OK, MessageBoxIcon.Error); }
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
}

# 致远 OA 文件清理工具

用于连接致远 OA SQL Server 数据库并查询文件夹内容的 Windows Server 桌面工具项目。

## 构建

在开发机执行：

```powershell
dotnet build -c Release
```

可直接运行的 64 位 Windows Server 单文件程序位于发布附件中。

连接成功后，在“待删除文件夹名称”输入框中输入名称并点击“查询文件”，程序会递归查询 `DOC_RESOURCES` 下的所有子文件夹文件，并显示文件 ID、文件名称和物理文件 ID。

删除按钮会根据 `CTP_FILE.ID` 删除 `D:\Seeyon\A8\base\upload` 中同名物理文件，以及 `D:\Seeyon\A8\base\officetrans` 中同名物理 ID 文件夹；随后删除 `DOC_RESOURCES` 显示记录。不会删除 `CTP_FILE` 记录。

连接界面的账户和密码仅保留在当前进程内存中，不写入磁盘。

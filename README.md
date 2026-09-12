# 致远 OA 文件清理工具

用于连接致远 OA SQL Server 数据库并查询文件夹内容的 Windows Server 桌面工具项目。

## 构建

在开发机执行：

```powershell
dotnet build -c Release
```
![Uploading image.png…]()

可直接运行的 64 位 Windows Server 单文件程序位于发布附件中。

连接成功后，在“待删除文件夹名称”输入框中输入名称并点击“查询文件”，程序会递归查询 `DOC_RESOURCES` 下的所有子文件夹文件，并显示文件 ID、文件名称和物理文件 ID。

删除按钮会根据 `CTP_FILE.ID` 删除 `D:\Seeyon\A8\base\upload` 中同名物理文件，以及 `D:\Seeyon\A8\base\officetrans` 中同名物理 ID 文件夹；随后删除 `DOC_RESOURCES` 显示记录。不会删除 `CTP_FILE` 记录。

连接界面的账户和密码仅保留在当前进程内存中，不写入磁盘。

程序顶部支持配置企业微信群机器人 Webhook 地址。配置成功后显示“已配置消息推送”，Webhook 地址仅保留在当前进程内存中，不写入磁盘。

程序顶部支持自定义 `upload` 和 `officetrans` 根目录，默认路径为 `D:\Seeyon\A8\base\upload` 和 `D:\Seeyon\A8\base\officetrans`。配置仅在当前运行期间保存，适用于安装在其他磁盘或目录的 OA 环境。

文件删除及数据库事务提交成功后，程序会生成 UTF-8 CSV 清理明细文档并通过企业微信群机器人推送。文档包含删除汇总以及全部文件的文件 ID、文件名称和物理文件 ID。临时文档发送后立即删除；推送失败不会影响已经完成的删除操作。

推送文档中的源文件总大小来自 `CTP_FILE.FILE_SIZE`；`officetrans` 大小在删除前递归扫描物理文件夹计算，两者分别列出，不将 `officetrans` 空间重复计入源文件总大小。

# RSR 自动构建与发布

参考 Questionable 的 new-main 工作流，按 RSR 的 Windows/.NET 10 构建和汉化检查调整。

- PR 到 main：构建 Release、运行汉化检查和插件索引脚本测试，上传插件包与 SHA-256。
- 推送 main：读取 Directory.Build.targets 中的四段版本号；版本尚未发布时构建并创建 v版本号 Release。
- 手动运行 Release：同样只允许 main。已有 Release 不覆盖、不移动标签；会重试插件索引同步。
- 发布文件：latest.zip、RotationSolver.json 及各自的 .sha256。
- 版本初始值保持 7.5.6.16。下次发布前递增 Version，例如 7.5.6.17。无需另外传入 AssemblyVersion。
- Questionable 的任务/采集路径数据发布不适用于 RSR，因此没有复制。

## GitHub 配置

1. 启用 Actions，允许发布 job 使用 GITHUB_TOKEN 的 contents:write 权限。
2. Secret PLUGIN_REPO_TOKEN：对目标插件仓库具有 Contents 读写权限的 token。
3. Variable PLUGIN_REPOSITORY：可选，默认 QianChangUwU/DalamudPlugins。
4. Variable RELEASE_USERNAMES：可选 JSON 用户名数组，例如 ["QianChangUwU"]；留空表示允许所有 main 推送者。

未配置 PLUGIN_REPO_TOKEN 时仍发布 Release，但会警告并跳过索引同步。
索引按 InternalName=RotationSolver 更新或追加，保留其他插件、下载统计和已有独立测试频道。
同步失败会使 job 失败；解决配置或推送冲突后重新运行即可，已有 Release 不会重建。

构建下载 Dalamud 稳定版，固定 Roslyn 5.9.0 以兼容源码生成器。
PR 仅有读取权限，不使用发布 token，不自动提交格式化修改。
本地可设置 DALAMUD_HOME 和 RSR_COMPILER_PROPS 后执行 .github/build.ps1；
脚本会验证两个源清单与 ZIP 内的中文元数据、版本及插件标识。

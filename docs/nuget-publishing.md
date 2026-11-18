# NuGet Trusted Publishing

正式發布使用 `.github/workflows/publish-nuget.yml`。工作流程先驗證版本、建置與測試、依 `EzLib.nuspec` 打包，再透過 `NuGet/login@v1` 的 OIDC 取得短效金鑰發布至 nuget.org；不讀取本機 `apikey.txt`，也不需要 GitHub API Key secret。

## 一次性設定

GitHub 儲存庫 `Ezsoft4u/EzLib.dotnet8.Tools` 的 Actions variable `NUGET_USER` 設為持有 NuGet 信任原則的個人帳號 `MarkChu`。

nuget.org 的 Trusted Publishing 原則設定：

| 欄位 | 值 |
|---|---|
| Policy Name | `EzLib-GitHub-Actions` |
| Package Owner | `MarkChu` |
| CI/CD Provider | GitHub Actions |
| Repository Owner | `Ezsoft4u` |
| Repository | `EzLib.dotnet8.Tools` |
| Workflow File | `publish-nuget.yml` |
| Environment | 留空 |
| Scope | Push only new package versions |
| Glob Patterns and Packages | `EzLib.dotnet8.Tools` |

## 發布新版

1. 同步更新 `EzLib.csproj` 與 `EzLib.nuspec` 的版本及發行說明。
2. 將程式碼提交並推送至 GitHub。
3. 在 `main` 分支手動執行 Actions 的 **Publish NuGet**，或推送相符的版本 tag，例如 `v1.0.22`。
4. 確認工作流程成功，以及 nuget.org 新版本可用。建好的套件可從該次執行的 `nuget-package` artifact 下載。

手動發布僅接受 `main` 分支；tag 必須與套件版本一致。同一版本重複發布會失敗，避免將既存版本誤報為新發布成功。

本機 `Publish-NuGet.ps1` 保留作為手動備用工具，仍需本機有效 API Key。

參考：[NuGet 官方 Trusted Publishing 文件](https://learn.microsoft.com/en-us/nuget/nuget-org/trusted-publishing)。

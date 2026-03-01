# ResChecker

ResChecker 是一個輕量的 .NET 命令列工具與 MCP 伺服器，用於驗證指定的文字是否存在於某個資料夾中的 `.resx` 或 `.resources` 檔案內。此專案主要協助開發者確認本地化資源是否包含所需字串。

## 功能

- 遞迴掃描資料夾中的所有 `.resx` 和 `.resources` 檔案。
- 支援文化 (Culture) 專屬查詢，例如 `zh-TW`、`en-US` 等。
- 接受多個待查文字，可使用可配置的分隔符分隔。
- 回傳詳細結果，指出哪些字串存在或缺失。
- 對於缺少的字串會提供翻譯提示，方便翻譯或本地化團隊使用。
- 作為 Model Context Protocol (MCP) 的工具透過 STDIO 運行。

## 快速開始

### 先決條件

- [.NET SDK 8.0 以上](https://dotnet.microsoft.com/download)
- 支援 Windows、macOS 或 Linux（以 Windows 測試）。

### 建置

```bash
# 還原套件並建置
cd ResChecker
dotnet build
```

### 執行

預設情況下，應用程式已設定為 MCP 伺服器。你可以透過支援 MCP 的主機使用標準輸入/輸出來呼叫此工具（例如使用 VSCode 擴充或其他 MCP 用戶端）。
提供的主要方法為 `CheckResourceTextsFromFolder`，參數包含：

1. `textToCheck` – 要查詢的文字，可為以逗號、Tab、換行分隔的多個文字。
2. `resourceFolderPath` – 資源檔案資料夾的完整路徑。
3. `separators` *(可選)* – 用於拆分 `textToCheck` 的字元，預設為 `\t,\r,\n,`。
4. `culture` *(可選)* – 查詢的文化代碼，預設為 `zh-TW`。

```json
"ResChecker": {
			"type": "stdio",
			"command": "dotnet",
			"args": [
				"run",
				"--project",
				"path/to/ResChecker/ResChecker.csproj"
			]
		},
```

## 授權

本專案採用 MIT 授權。

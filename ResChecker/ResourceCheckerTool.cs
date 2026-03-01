using ModelContextProtocol.Server;
using System.Collections;
using System.ComponentModel;
using System.Resources;
using System.Text;
using System.Xml.Linq;

namespace ResChecker;

[McpServerToolType]
public static class ResourceCheckerTool
{


    [McpServerTool, Description("檢查指定的文字是否存在於外部 Resource 資料夾中的 .resx 或 .resources 檔案，並返回不存在的文字列表")]
    public static string CheckResourceTextsFromFolder(
        [Description("要檢查的文字，可以是單個或多個文字，用分隔符分開")] string textToCheck,
        [Description("Resource 資料夾的完整路徑")] string resourceFolderPath,
        [Description("分隔符字串，例如: '\\t,\\r,\\n,,' 表示使用 Tab、換行、逗號作為分隔符。預設為 '\\t,\\r,\\n,,'")] string? separators = null,
        [Description("要檢查的文化代碼，例如: 'zh-TW', 'en-US', 'zh-CN'。預設為 'zh-TW'")] string? culture = null)
    {
        try
        {
            // Debug logging
            Console.Error.WriteLine($"[DEBUG CheckResourceTextsFromFolder] Received: {textToCheck}");
            Console.Error.WriteLine($"[DEBUG] Bytes: {BitConverter.ToString(Encoding.UTF8.GetBytes(textToCheck))}");
            
            // 驗證資料夾是否存在
            if (!Directory.Exists(resourceFolderPath))
            {
                return $"❌ 錯誤: 資料夾不存在: {resourceFolderPath}";
            }

            // 設定預設分隔符
            char[] separatorChars = ParseSeparators(separators);

            // 分割文字
            List<string> textsToCheck = textToCheck
                .Split(separatorChars, StringSplitOptions.RemoveEmptyEntries)
                .Select(t => t.Trim())
                .ToList();

            Console.Error.WriteLine($"[DEBUG] Parsed {textsToCheck.Count} texts");
            foreach (var t in textsToCheck)
            {
                Console.Error.WriteLine($"[DEBUG] - '{t}'");
            }

            // 設定文化
            string cultureToCheck = string.IsNullOrEmpty(culture) ? "zh-TW" : culture;

            // 尋找所有 .resx 和 .resources 檔案
            var resxFiles = Directory.GetFiles(resourceFolderPath, "*.resx", SearchOption.AllDirectories);
            var resourcesFiles = Directory.GetFiles(resourceFolderPath, "*.resources", SearchOption.AllDirectories);

            if (resxFiles.Length == 0 && resourcesFiles.Length == 0)
            {
                return $"❌ 錯誤: 在 {resourceFolderPath} 中找不到任何 .resx 或 .resources 檔案";
            }

            List<string> outputList = new List<string>();
            List<string> existsTexts = new List<string>();

            // 檢查每個文字
            foreach (var text in textsToCheck)
            {
                bool found = false;

                // 檢查 .resx 檔案
                foreach (var resxFile in resxFiles)
                {
                    try
                    {
                        var fileName = Path.GetFileNameWithoutExtension(resxFile);
                        
                        // 檢查是否為指定文化的資源檔
                        if (fileName.EndsWith($".{cultureToCheck}", StringComparison.OrdinalIgnoreCase) || 
                            !fileName.Contains('.') && cultureToCheck == "zh-TW")
                        {
                            var xdoc = XDocument.Load(resxFile);
                            var dataElements = xdoc.Descendants("data");

                            foreach (var dataElement in dataElements)
                            {
                                var valueElement = dataElement.Element("value");
                                if (valueElement?.Value == text)
                                {
                                    var key = dataElement.Attribute("name")?.Value;
                                    existsTexts.Add(text);
                                    
                                    // 取得資源短名稱 (去掉文化代碼部分)
                                    var resourceShortName = fileName;
                                    if (resourceShortName.Contains('.'))
                                    {
                                        var parts = resourceShortName.Split('.');
                                        resourceShortName = parts[0]; // 取第一部分作為短名稱
                                    }
                                    
                                    outputList.Add($"{text} \t存在 \t File:{Path.GetFileName(resxFile)}\t\tRes.{resourceShortName}.{key}");
                                    found = true;
                                    break;
                                }
                            }

                            if (found) break;
                        }
                    }
                    catch
                    {
                        // 如果某個檔案讀取失敗，繼續檢查下一個
                        continue;
                    }
                }

                // 如果在 .resx 中沒找到，檢查 .resources 檔案
                if (!found)
                {
                    foreach (var resourceFile in resourcesFiles)
                    {
                        try
                        {
                            using var resourceReader = new ResourceReader(resourceFile);
                            foreach (DictionaryEntry entry in resourceReader)
                            {
                                if (entry.Value?.ToString() == text)
                                {
                                    existsTexts.Add(text);
                                    
                                    // 取得資源短名稱
                                    var fileName = Path.GetFileNameWithoutExtension(resourceFile);
                                    var resourceShortName = fileName;
                                    if (resourceShortName.Contains('.'))
                                    {
                                        var parts = resourceShortName.Split('.');
                                        resourceShortName = parts[0]; // 取第一部分作為短名稱
                                    }
                                    
                                    outputList.Add($"{text} \t存在 \t File:{Path.GetFileName(resourceFile)}\t\tRes.{resourceShortName}.{entry.Key}");
                                    found = true;
                                    break;
                                }
                            }

                            if (found) break;
                        }
                        catch
                        {
                            // 如果某個檔案讀取失敗，繼續檢查下一個
                            continue;
                        }
                    }
                }
            }

            // 找出不存在的文字
            var notExistsTexts = textsToCheck.Except(existsTexts).ToList();

            foreach (var item in notExistsTexts)
            {
                outputList.Add($"{item} \t不存在");
            }

            return FormatResult(textsToCheck.Count, existsTexts.Count, notExistsTexts, outputList, resourceFolderPath);
        }
        catch (Exception ex)
        {
            return $"❌ 錯誤: {ex.Message}\n\nStack Trace:\n{ex.StackTrace}";
        }
    }

    // 輔助方法：解析分隔符
    private static char[] ParseSeparators(string? separators)
    {
        if (string.IsNullOrEmpty(separators))
        {
            return new char[] { '\t', '\r', '\n', ',' };
        }

        var parsedSeparators = new List<char>();
        for (int i = 0; i < separators.Length; i++)
        {
            if (separators[i] == '\\' && i + 1 < separators.Length)
            {
                switch (separators[i + 1])
                {
                    case 't':
                        parsedSeparators.Add('\t');
                        i++;
                        break;
                    case 'r':
                        parsedSeparators.Add('\r');
                        i++;
                        break;
                    case 'n':
                        parsedSeparators.Add('\n');
                        i++;
                        break;
                    default:
                        parsedSeparators.Add(separators[i + 1]);
                        i++;
                        break;
                }
            }
            else if (separators[i] != '\\')
            {
                parsedSeparators.Add(separators[i]);
            }
        }
        return parsedSeparators.ToArray();
    }

    // 輔助方法：格式化結果
    private static string FormatResult(int totalCount, int existsCount, List<string> notExistsTexts, List<string> outputList, string? folderPath = null)
    {
        var sb = new StringBuilder();

        // 基本統計
        sb.AppendLine("## Resource 檢查結果");
        sb.AppendLine();
        if (folderPath != null)
        {
            sb.AppendLine($"資料夾: {folderPath}");
            sb.AppendLine();
        }
        sb.AppendLine($"總共檢查: {totalCount} 個文字");
        sb.AppendLine($"存在: {existsCount} 個");
        sb.AppendLine($"不存在: {notExistsTexts.Count} 個");
        sb.AppendLine();

        // 詳細結果
        sb.AppendLine("### 詳細檢查結果:");
        sb.AppendLine("```");
        foreach (var detail in outputList)
        {
            sb.AppendLine(detail);
        }
        sb.AppendLine("```");
        sb.AppendLine();

        // 如果有不存在的文字，加上翻譯 prompt
        if (notExistsTexts.Any())
        {
            sb.AppendLine("=============================分隔線================================");
            sb.AppendLine();
            sb.AppendLine("### 不存在的文字 (需要翻譯):");
            sb.AppendLine();
            foreach (var text in notExistsTexts)
            {
                sb.AppendLine(text);
            }
            sb.AppendLine();
            sb.AppendLine("以上這些不存在的幫我翻譯成 英文、簡中 用表格顯示 欄位為 key、comment、英文、簡中、繁中， key為英文去掉空白、特殊符號用pascalcase不超過20字元太長可用音節加母音縮減EX:PascalCase => PaCaCaSe ，comment同英文");
            sb.AppendLine();
            sb.AppendLine("=============================分隔線================================");
        }
        else
        {
            sb.AppendLine("✅ 所有文字都已存在於 Resource 中！");
        }

        return sb.ToString();
    }
}

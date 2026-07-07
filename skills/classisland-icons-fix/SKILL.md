---
name: classisland-icons-fix
description: >
  修复 ClassIsland/Avalonia 项目中的 Fluent System Icons 错误。使用场景：
  (1) 设置页面中的图标显示为空白/错误，(2) 将随机字符码替换为语义正确的代码，
  (3) 修复插件中使用 lucide/fluent 图标表达式时的图标加载问题，
  (4) 审计 XAML 和 C# 文件中的图标使用情况。
  包括通过 FluentSystemIcons-Resizable.json 进行字形查找、语义映射，
  以及 XAML（FluentIconSource/FluentIcon）和 C#（字符串字面量）的替换。
---

# Fluent 图标修复

## 概述

ClassIsland 使用 Fluent System Icons（字体文件 `FluentSystemIcons-Resizable.ttf`）。
每个图标都由 Unicode 码点（十六进制）引用。错误的码点会导致图标显示为空白或误导。
本技能提供了查找和修复这些问题的工作流。

## 工作流

### 1. 定位图标字体注册表

JSON 映射位置：
```
./references/FluentSystemIcons-Resizable.json
```

条目将 `ic_fluent_<name>` 映射到十进制码点。示例：
```json
"ic_fluent_copy_20_regular": 58763
```

### 2. 查询当前图标

使用 PowerShell 反向查询码点：
```powershell
$data = Get-Content '.\references\FluentSystemIcons-Resizable.json' -Raw | ConvertFrom-Json
$hex = 0xE58B  # 要检查的码点
$name = ($data.PSObject.Properties | Where-Object { $_.Value -eq $hex } | Select-Object -First 1).Name
"$('0x{0:X4}' -f $hex) -> $name"
```

批量查询：
```powershell
$codes = @(0xE774, 0xE8BD, 0xE839)
foreach ($code in $codes) {
    $hex = '0x{0:X4}' -f $code
    $name = ($data.PSObject.Properties | Where-Object { $_.Value -eq $code } | Select-Object -First 1).Name
    "$hex -> $name"
}
```

### 3. 按概念查找正确的图标

搜索 JSON 查找语义适当的图标：
```powershell
$patterns = @('*shield*20_filled','*power*20_filled','*server*20_filled')
foreach ($p in $patterns) {
    $found = $data.PSObject.Properties | Where-Object { $_.Name -like "ic_fluent_$p" } | Select-Object -First 2
    foreach ($f in $found) { "0x{0:X4} -> $($f.Name)" -f $f.Value }
}
```

常见语义映射：

| 概念 | 推荐图标 |
|---------|------------------|
| 启用/切换 | `power_20_filled` (`EDE8`) |
| 页面标题 | 因上下文而异 |
| 端口/网络 | `channel_20_regular` (`E3D2`) |
| 传输/协议 | `protocol_handler_20_regular` (`EE1F`) |
| 链接/地址 | `link_20_regular` (`EAB0`) |
| 信息/帮助 | `info_20_regular` (`E9E4`) |
| 警告 | `warning_20_filled` (`F430`) |
| 服务器 | `server_20_regular` (`EF1D`) 或 `server_20_filled` (`EF1C`) |
| 隐私/盾牌 | `shield_20_filled` (`EF4E`) |
| 复制 | `copy_20_regular` (`E58B`) |
| 添加 | `add_20_filled` (`E00C`) |
| 大脑/AI | `brain_20_filled` (`E29B`) |
| 火箭/测试 | `rocket_20_filled` (`EEA0`) |
| 聊天/通知 | `chat_20_regular` (`E3E4`) |

### 4. 应用修复

**XAML — FluentIconSource（SettingsExpander）：**
```xml
<!-- 修复前 -->
<fa:SettingsExpander IconSource="{ci:FluentIconSource &#xE774;}" .../>
<!-- 修复后 -->
<fa:SettingsExpander IconSource="{ci:FluentIconSource &#xEDE8;}" .../>
```

**XAML — FluentIcon（内联）：**
```xml
<!-- 修复前 -->
<ci:FluentIcon Glyph="&#xE7BA;" .../>
<!-- 修复后 -->
<ci:FluentIcon Glyph="&#xF430;" .../>
```

**XAML — IconText（页面标题）：**
```xml
<!-- 修复前 -->
<ci:IconText Glyph="&#xE774;" Text="..." .../>
<!-- 修复后 -->
<ci:IconText Glyph="&#xEF1D;" Text="..." .../>
```

**C# — 字符串字面量（属性）：**
```csharp
// 修复前
"\uE7C3"
// 修复后
"\uEDE9"
```

### 5. 避免在插件中使用 lucide

插件应该直接使用 Fluent 图标（纯十六进制如 `\uE3E4`），
而不是 `lucide(\uE5D3)` 表达式。当插件的通知提供程序构造时，
lucide 处理程序可能未注册，导致图标显示为空白。

**注意：`[NotificationProviderInfo]` 和 `[NotificationChannelInfo]` 属性不支持 lucide 十六进制码点。**
即使使用 lucide 的原始 hex（如 `\uE5D3` 对应 `bot-message-square`），也会显示为错误图标。
必须改用 Fluent 等效图标（如 `bot_20_filled` 对应 `\uE271`）。

如果必须使用 lucide，请在 `Plugin.Initialize()` 中注册处理程序：
```csharp
if (!IconExpressionHelper.IconExpressionHandlers.ContainsKey("lucide"))
{
    IconExpressionHelper.RegisterHandler("lucide", args => new LucideIconSource(args[0]));
}
```

### 6. 验证

修复后，运行 `dotnet build` 确保没有编译错误。
使用 grep 查找任何剩余的错误码点以确认完整性。

## Fluent 图标常见错误模式

这些图标名称经常作为错误的默认值出现——发现即替换：

| 错误字形 | 实际图标 | 替换为 |
|-------------|-------------|-------------|
| `E774` | draw_shape | 因上下文而异 |
| `E7BA` | dual_screen_update | `F430`（警告） |
| `E7C3` | dust | 因上下文而异 |
| `E8BD` | form | 因上下文而异 |
| `E839` | filter_sync | 因上下文而异 |
| `E8BC` | form_filled | 因上下文而异 |
| `E8A7` | font_space_tracking_out | `E9E4`（信息） |
| `E8A5` | font_space_tracking_in | 因上下文而异 |
| `ECC8` | people_money | `E00C`（添加） |
| `E9D9` | image_stack | 因上下文而异 |
| `E768` | double_swipe_up | 因上下文而异 |
| `E72E` | document_sass | 因上下文而异 |

---

# Lucide 图标修复

## 概述

ClassIsland 还使用 Lucide Icons（字体文件 `lucide.ttf`）作为补充图标库。
Lucide 提供了现代化的开源图标集，每个图标通过 Unicode 码点引用。
与 Fluent 类似，错误的码点会导致图标显示不正确。

## 工作流

### 1. 定位 lucide 图标注册表

JSON 映射位置：
```
./references/lucide.json
```

条目将图标名称映射到十进制码点。示例：
```json
"alarm": 59392
```

### 2. 查询当前 lucide 图标

使用 PowerShell 反向查询码点：
```powershell
$data = Get-Content '.\references\lucide.json' -Raw | ConvertFrom-Json
$hex = 0xE092  # 要检查的码点
$name = ($data.PSObject.Properties | Where-Object { $_.Value -eq $hex } | Select-Object -First 1).Name
"$('0x{0:X4}' -f $hex) -> $name"
```

批量查询：
```powershell
$codes = @(0xE0FF, 0xE224, 0xE54F)
foreach ($code in $codes) {
    $hex = '0x{0:X4}' -f $code
    $name = ($data.PSObject.Properties | Where-Object { $_.Value -eq $code } | Select-Object -First 1).Name
    "$hex -> $name"
}
```

### 3. 按概念查找 lucide 图标

搜索 JSON 查找相关图标：
```powershell
$patterns = @('*alert*','*check*','*clock*','*cloud*','*settings*')
foreach ($p in $patterns) {
    $found = $data.PSObject.Properties | Where-Object { $_.Name -like $p } | Select-Object -First 3
    foreach ($f in $found) { "0x{0:X4} -> $($f.Name)" -f $f.Value }
}
```

常见 Lucide 图标映射：

| 概念 | 推荐图标 | 码点 |
|---------|------------------|------|
| 警告 | `alert-circle` | `E092` |
| 检查/成功 | `check-circle` | `E0A5` |
| 信息 | `info` | `E0F1` |
| 帮助 | `help-circle` | `E0FF` |
| 关闭 | `x-circle` | `E224` |
| 设置 | `settings` | `E1E7` |
| 刷新 | `rotate-cw` | `E1F7` |
| 搜索 | `search` | `E250` |
| 下载 | `download` | `E54F` |
| 上传 | `upload` | `E568` |
| 删除 | `trash-2` | `E57D` |
| 编辑 | `edit-2` | `E5B3` |
| 菜单 | `menu` | `E629` |

### 4. 应用 Lucide 修复

**XAML — LucideIconSource：**
```xml
<!-- 修复前 -->
<ci:LucideIcon Glyph="&#xE768;" .../>
<!-- 修复后 -->
<ci:LucideIcon Glyph="&#xE092;" .../>
```

**表达式形式：**
```csharp
// 修复前
"lucide(\ue768)"
// 修复后
"lucide(\ue092)"
```

**C# — LucideIconSource 直接使用：**
```csharp
// 修复前
new LucideIconSource("\uE768")
// 修复后
new LucideIconSource("\uE092")
```

### 5. Lucide 与 Fluent 的互操作

在需要跨两个图标库的场景中：
- 优先使用 Fluent 作为主要图标库（更完整的设计系统）
- Lucide 用作补充，特别是在需要特定现代风格的地方
- 确保通过 `IconExpressionHelper` 正确注册两个处理程序

```csharp
// 在 App.axaml.cs 中（已默认配置）
IconExpressionHelper.RegisterHandler("fluent", args => new FluentIconSource(args[0]));
IconExpressionHelper.RegisterHandler("lucide", args => new LucideIconSource(args[0]));
```

### 6. 验证 Lucide 图标

修复后验证：
```powershell
# 检查 XAML 中是否还有错误的 lucide 码点
Select-String -Path '*.axaml' -Pattern 'LucideIcon|lucide\(' -Recursive | Select-String 'E768|E7BA|E8BD'
```

## Lucide 常见错误模式

这些码点经常被误用——发现即替换：

| 错误码点 | 实际图标 | 推荐替换 |
|-------------|-------------|-------------|
| `E768` | 未定义/错误 | 根据用途选择 |
| `E7BA` | 未定义/错误 | 根据用途选择 |
| `E8BD` | 未定义/错误 | 根据用途选择 |

**最佳实践：** 使用已验证的 lucide.json 中存在的码点。
使用上面的 PowerShell 脚本验证所有使用的码点都映射到实际的图标名称。

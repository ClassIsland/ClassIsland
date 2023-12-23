# AttachedSettings

## AttachedSettingsControl

### 定义
```cs
public partial class MyAttachedSettingsControl : UserControl, IAttachedSettingsControlBase
```

### 属性

| 名称 | 类型 | 描述 | 示例 | 
| --- | --- | --- | --- |
| `AttachedSettingsControlHelper` | `IAttachedSettingsHelper` | 访问自身设置 | ```new AttachedSettingsControlHelper<T>(new Guid("8FBC3A26-6D20-44DD-B895-B9411E3DDC51"), new T())``` |
| `Settings` | `T` | 对应设置 | `((AttachedSettingsControlHelper<AfterSchoolNotificationAttachedSettings>)AttachedSettingsControlHelper).AttachedSettings ?? new AfterSchoolNotificationAttachedSettings();`


``` cs
public partial class AfterSchoolNotificationAttachedSettingsControl : UserControl, IAttachedSettingsControlBase
{
    public AfterSchoolNotificationAttachedSettingsControl()
    {
        InitializeComponent();
    }

    public IAttachedSettingsHelper AttachedSettingsControlHelper { get; set; } = 
        new AttachedSettingsControlHelper<AfterSchoolNotificationAttachedSettings>(
        new Guid("8FBC3A26-6D20-44DD-B895-B9411E3DDC51"), new AfterSchoolNotificationAttachedSettings());

    public AfterSchoolNotificationAttachedSettings Settings =>
        ((AttachedSettingsControlHelper<AfterSchoolNotificationAttachedSettings>)AttachedSettingsControlHelper)
        .AttachedSettings ?? new AfterSchoolNotificationAttachedSettings();
}
```

## AttachedSettingsHostService

### GetAttachedSettingsByPriority 方法

```cs
public static T? GetAttachedSettingsByPriority<T>(
    Guid id, 
    Subject? subject = null,
    TimeLayoutItem? timeLayoutItem = null, 
    ClassPlan? classPlan = null, 
    TimeLayout? timeLayout = null) 
where T : IAttachedSettings
```

按照以下顺序获取附加设置：
``` mermaid
graph TD {
    Subject --> TimeLayoutItem --> ClassPlan --> TimeLayout --> null
}
```

当其中有一项的对应附加设置不为`null`，且`IsAttachSettingsEnabled`为`true`时，返回对应项目。如果什么也没有找到，返回`null`。

#### 参数

| 名称 | 类型 | 必选项？ | 默认值 | 描述 |
| -- | -- | -- | -- | -- |
| `T`（类型参数） | `T : IAttachedSettings` | **是** | - | 附加设置类型 |
| `id` | `Guid` | **是** | - | 附加设置ID |
| `subject` | `Subject?` | 否 | `null` | 可附加对象 |
| `timeLayoutItem` | `TimeLayoutItem?` | 否 | `null` | 可附加对象 |
| `classPlan` | `ClassPlan?` | 否 | `null` | 可附加对象 |
| `timeLayout` | `TimeLayout?` | 否 | `null` | 可附加对象 |

#### 返回值

类型：`T?`
| 条件 | 值 |
| -- | -- |
| 找到对应项目 | 对应附加设置 |
| 未找到回应项目 | `null` |

#### 示例
``` cs
var settings = AttachedSettingsHostService.GetAttachedSettingsByPriority
<ClassNotificationAttachedSettings>(
    ProviderGuid,
    mvm.CurrentSubject,
    mvm.CurrentTimeLayoutItem,
    mvm.CurrentClassPlan,
    mvm.CurrentClassPlan?.TimeLayout
);
```
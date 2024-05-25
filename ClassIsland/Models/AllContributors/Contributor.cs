using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json.Serialization;

using CommunityToolkit.Mvvm.ComponentModel;

namespace ClassIsland.Models.AllContributors;

public class Contributor : ObservableRecipient
{
    public static readonly Dictionary<string, string> ContributionKeys = new()
    {

        {"audio", "音频"},
        {"a11y", "无障碍"},
        {"bug", "Bug 反馈"},
        {"blog", "博文"},
        {"business", "业务发展"},
        {"code", "代码"},
        {"content", "内容"},
        {"data", "数据"},
        {"doc", "文档"},
        {"design", "设计"},
        {"example", "范例"},
        {"eventOrganizing", "活动组织者"},
        {"financial", "资金支持"},
        {"fundingFinding", "资金/拨款发现者"},
        {"ideas", "创意 & 策划"},
        {"infra", "基础设施"},
        {"maintenance", "维护"},
        {"mentoring", "辅导"},
        {"platform", "打包"},
        {"plugin", "插件/实用程序库"},
        {"projectManagement", "项目管理"},
        {"promotion", "推广"},
        {"question", "回答问题"},
        {"research", "研究"},
        {"review", "检查 Pull Request"},
        {"security", "安全"},
        {"tool", "工具"},
        {"translation", "翻译"},
        {"test", "测试"},
        {"tutorial", "教程"},
        {"talk", "交流"},
        {"userTesting", "用户测试"},
        {"video", "视频"}
    };

    private string _name = "";
    private string _avatarUrl = "";
    private List<string> _contributions = new();

    public string Name
    {
        get => _name;
        set
        {
            if (value == _name) return;
            _name = value;
            OnPropertyChanged();
        }
    }

    [JsonPropertyName("avatar_url")]
    public string AvatarUrl
    {
        get => _avatarUrl;
        set
        {
            if (value == _avatarUrl) return;
            _avatarUrl = value;
            OnPropertyChanged();
            OnPropertyChanged(nameof(AvatarUri));
        }
    }

    public Uri? AvatarUri
    {
        get
        {
            Uri.TryCreate(AvatarUrl, UriKind.RelativeOrAbsolute, out var uri);
            return uri;
        }
    }

    public List<string> Contributions
    {
        get => _contributions;
        set
        {
            if (Equals(value, _contributions)) return;
            _contributions = value;
            OnPropertyChanged();
        }
    }

    public string ContributionText => string.Join("，",
        Contributions.Where(i => ContributionKeys.ContainsKey(i)).Select(i => ContributionKeys[i]).ToList());
}
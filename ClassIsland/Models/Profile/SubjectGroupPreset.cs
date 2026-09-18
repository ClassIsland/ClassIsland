using System;
using System.Collections.Generic;
using System.Linq;
using ClassIsland.Shared.Models.Profile;

namespace ClassIsland.Models.Profile;

internal sealed class SubjectGroupPreset
{
    private static readonly IReadOnlyDictionary<string, SubjectGroupPreset> Presets =
        new Dictionary<string, SubjectGroupPreset>
        {
            ["primary"] = new(
            [
                new("主科", ["语文", "数学", "英语"]),
                new("副科",
                [
                    "科学", "道德与法治", "道法", "品德与社会", "品德与生活", "体育", "体育与健康", "音乐", "美术",
                    "信息技术", "信息科技", "计算机", "劳动", "劳动技术", "综合实践"
                ])
            ]),
            ["middle"] = new(
            [
                new("主科", ["语文", "数学", "英语", "物理", "化学"]),
                new("副科", ["体育", "体育与健康", "音乐", "美术", "信息技术", "信息科技", "计算机", "劳动", "劳动技术", "综合实践"]),
                new("小四门", ["政治", "思想政治", "道德与法治", "道法", "历史", "地理", "生物"])
            ]),
            ["high"] = new(
            [
                new("必修", ["语文", "数学", "英语"]),
                new("副科", ["体育", "体育与健康", "音乐", "美术", "信息技术", "信息科技", "计算机", "劳动", "劳动技术", "综合实践"]),
                new("理科", ["物理", "化学", "生物"]),
                new("文科", ["政治", "思想政治", "思政", "历史", "地理"])
            ])
        };

    private readonly IReadOnlyList<SubjectGroupPresetSection> _sections;

    private SubjectGroupPreset(IReadOnlyList<SubjectGroupPresetSection> sections)
    {
        _sections = sections;
    }

    public static bool TryGetPreset(string id, out SubjectGroupPreset? preset)
    {
        return Presets.TryGetValue(id, out preset);
    }

    public void Apply(ClassIsland.Shared.Models.Profile.Profile profile)
    {
        foreach (var subject in profile.Subjects.Values)
        {
            subject.GroupId = Guid.Empty;
        }

        profile.SubjectGroups.Clear();
        var groupIds = new Dictionary<SubjectGroupPresetSection, Guid>();
        foreach (var section in _sections)
        {
            var id = Guid.NewGuid();
            profile.SubjectGroups.Add(id, new SubjectGroup { Name = section.Name });
            groupIds.Add(section, id);
        }

        foreach (var subject in profile.Subjects.Values)
        {
            var name = subject.Name.Trim();
            var section = _sections.FirstOrDefault(x => x.SubjectNames.Contains(name));
            if (section is not null)
            {
                subject.GroupId = groupIds[section];
            }
        }
    }

    private sealed class SubjectGroupPresetSection
    {
        public SubjectGroupPresetSection(string name, IEnumerable<string> subjectNames)
        {
            Name = name;
            SubjectNames = new HashSet<string>(subjectNames, StringComparer.OrdinalIgnoreCase);
        }

        public string Name { get; }

        public HashSet<string> SubjectNames { get; }
    }
}

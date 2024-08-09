using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using ClassIsland.Core.Abstractions.Services;
using ClassIsland.Core.Enums;
using ClassIsland.Core.Extensions;
using ClassIsland.Core.Models.ProfileAnalyzing;
using ClassIsland.Shared;
using ClassIsland.Shared.Interfaces;
using CommunityToolkit.Mvvm.ComponentModel;
using WebSocketSharp;

namespace ClassIsland.Services;

using AttachableObjectNodeDictionary = ObservableDictionary<AttachableObjectAddress, AttachableObjectNode>;

public class ProfileAnalyzeService(IProfileService profileService) : ObservableRecipient, IProfileAnalyzeService
{
    public IProfileService ProfileService { get; } = profileService;

    public AttachableObjectNodeDictionary Nodes { get; } = new();

    public void Analyze()
    {
        Nodes.Clear();
        var profile = ProfileService.Profile;
        foreach (var i in profile.ClassPlans)
        {
            var keyClassPlan = new AttachableObjectAddress(i.Key);
            var nodeClassPlan = Nodes.GetOrCreateDefault(keyClassPlan,
                new AttachableObjectNode()
            {
                Object = i.Value,
                Target = AttachedSettingsTargets.ClassPlan,
                Address = keyClassPlan
            });


            var keyTimeLayout = new AttachableObjectAddress(i.Value.TimeLayoutId);
            var nodeTimeLayout = Nodes.GetOrCreateDefault(keyTimeLayout
                , new AttachableObjectNode()
                {
                    Object = i.Value.TimeLayout,
                    Target = AttachedSettingsTargets.TimeLayout,
                    Address = keyTimeLayout
                });
            nodeClassPlan.PreviousNodes.TryAdd(keyTimeLayout, nodeTimeLayout);
            nodeTimeLayout.NextNodes.TryAdd(keyClassPlan, nodeClassPlan);

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (i.Value.TimeLayout == null)
            {
                continue;
            }

            foreach (var p in i.Value.TimeLayout.Layouts)
            {
                var keyTimePoint = new AttachableObjectAddress(i.Value.TimeLayoutId,
                    i.Value.TimeLayout.Layouts.IndexOf(p));
                var nodeTimePoint = Nodes.GetOrCreateDefault(keyTimePoint, new AttachableObjectNode()
                {
                    Object = p,
                    Target = AttachedSettingsTargets.TimePoint,
                    Address = keyTimePoint
                });

                nodeTimeLayout.NextNodes.TryAdd(keyTimePoint, nodeTimePoint);
                nodeTimePoint.PreviousNodes.TryAdd(keyTimeLayout, nodeTimeLayout);

                nodeClassPlan.NextNodes.TryAdd(keyTimePoint, nodeTimePoint);
                nodeTimePoint.PreviousNodes.TryAdd(keyClassPlan, nodeClassPlan);
            }

            foreach (var j in i.Value.Classes)
            {
                var keyClassInfo = new AttachableObjectAddress(i.Key, j.Index);
                var nodeClassInfo = new AttachableObjectNode()
                {
                    Object = j,
                    Target = AttachedSettingsTargets.Lesson,
                    Address = keyClassInfo
                };

                var keyTimePoint = new AttachableObjectAddress(i.Value.TimeLayoutId,
                    j.CurrentTimeLayout.Layouts.IndexOf(j.CurrentTimeLayoutItem));
                var nodeTimePoint = Nodes.GetOrCreateDefault(keyTimePoint, new AttachableObjectNode()
                {
                    Object = j.CurrentTimeLayoutItem,
                    Target = AttachedSettingsTargets.TimePoint,
                    Address = keyTimePoint
                });

                var nodeLesson = Nodes.GetOrCreateDefault(keyClassInfo, nodeClassInfo);
                nodeLesson.PreviousNodes.TryAdd(keyClassPlan, nodeClassPlan);
                nodeClassPlan.NextNodes.TryAdd(keyClassInfo, nodeClassInfo);
                nodeClassPlan.RelatedLessons.TryAdd(keyClassInfo, nodeClassInfo);

                nodeLesson.PreviousNodes.TryAdd(keyTimePoint, nodeTimePoint);
                nodeTimePoint.NextNodes.TryAdd(keyClassInfo, nodeClassInfo);
                nodeTimePoint.RelatedLessons.TryAdd(keyClassInfo, nodeClassInfo);

                nodeClassPlan.NextNodes.TryAdd(keyTimePoint, nodeTimePoint);
                nodeTimePoint.PreviousNodes.TryAdd(keyClassPlan, nodeClassPlan);

                nodeTimeLayout.RelatedLessons.TryAdd(keyClassInfo, nodeClassInfo);

                if (!profile.Subjects.TryGetValue(j.SubjectId, out var subject))
                {
                    continue;
                }
                var keySubject = new AttachableObjectAddress(j.SubjectId);
                var nodeSubject = Nodes.GetOrCreateDefault(keySubject, new AttachableObjectNode()
                {
                    Object = subject,
                    Target = AttachedSettingsTargets.Subject,
                    Address = keySubject
                });

                nodeLesson.PreviousNodes.TryAdd(keySubject, nodeSubject);
                nodeSubject.NextNodes.TryAdd(keyClassInfo, nodeClassInfo);

                nodeTimePoint.NextNodes.TryAdd(keySubject, nodeSubject);
                nodeSubject.PreviousNodes.TryAdd(keyTimePoint, nodeTimePoint);

                nodeSubject.RelatedLessons.TryAdd(keyClassInfo, nodeClassInfo);
            }
        }
    }

    public string DumpMermaidGraph()
    {
        var mermaid = new StringBuilder();
        mermaid.AppendLine("graph LR");
        foreach (var node in Nodes.OrderBy(x => x.Value.Target))
        {
            mermaid.AppendLine(
                $"    n_{node.Key.Guid}_{node.Key.Index}[\"{node.Value.Target}_{node.Key.Guid}_{node.Key.Index}\"]");
            
            foreach (var i in node.Value.NextNodes)
            {
                mermaid.AppendLine(
                    $"    n_{node.Key.Guid}_{node.Key.Index} --> n_{i.Key.Guid}_{i.Key.Index}");
            }
        }

        return mermaid.ToString();
    }

    public void Walk(AttachableObjectNode node, ICollection<AttachableObjectNode> foundNodes, bool isUp, bool isInit=false)
    {
        if (!foundNodes.Contains(node))
        {
            foundNodes.Add(node);
        }
        if (node.Target == AttachedSettingsTargets.Subject && !isInit)
        {
            return;
        }

        foreach (var i in isUp ? node.PreviousNodes : node.NextNodes)
        {
            Walk(i.Value, foundNodes, isUp);
        }
    }

    public List<AttachableObjectNode> FindNextObjects(AttachableObjectAddress address, string id, bool requiresEnabled=true)
    {
        var results = new List<AttachableObjectNode>();
        Walk(Nodes[address], results, false, true);

        return [.. results.Where(x =>
            {
                if (x.Object.AttachedObjects.TryGetValue(id, out var obj) && obj is IAttachedSettings settings)
                {
                    return settings.IsAttachSettingsEnabled || !requiresEnabled;
                }
                return false;
            })
            .OrderByDescending(x => x.Target)
        ];
    }

    public List<AttachableObjectNode> FindPreviousObjects(AttachableObjectAddress address, string id, bool requiresEnabled = true)
    {
        var results = new List<AttachableObjectNode>();
        Walk(Nodes[address], results, true, true);

        return [.. results.Where(x =>
            {
                if (x.Object.AttachedObjects.TryGetValue(id, out var obj) && obj is IAttachedSettings settings)
                {
                    return settings.IsAttachSettingsEnabled || !requiresEnabled;
                }
                return false;
            })
            .OrderBy(x => x.Target)
        ];
    }
}
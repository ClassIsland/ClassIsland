using Avalonia.Automation.Peers;
using Avalonia.Controls;
using ClassIsland.Core.Assists;
using FluentAvalonia.UI.Controls;
using Xunit;

namespace ClassIsland.Lessons.Tests;

public sealed class FaItemsRepeaterAutomationTests
{
    [Fact]
    public void UpstreamPeer_EmptyRepeater_ReproducesStartupCrash()
    {
        var peer = new FAItemsRepeaterAutomationPeer(new FAItemsRepeater());

        Assert.Throws<NullReferenceException>(() => peer.GetChildren());
    }

    [Fact]
    public void Enabled_EmptyRepeater_ReturnsEmptyChildrenAndPreservesGroup()
    {
        var repeater = new FAItemsRepeater();
        FaItemsRepeaterAutomationAssist.SetIsEnabled(repeater, true);

        var peer = ControlAutomationPeer.CreatePeerForElement(repeater);

        Assert.Empty(peer.GetChildren());
        Assert.Equal(AutomationControlType.Group, peer.GetAutomationControlType());
        Assert.Same(peer, ControlAutomationPeer.CreatePeerForElement(repeater));
    }

    [Fact]
    public void Enabled_ExistingUpstreamPeer_IsReplaced()
    {
        var repeater = new FAItemsRepeater();
        var original = ControlAutomationPeer.CreatePeerForElement(repeater);

        FaItemsRepeaterAutomationAssist.SetIsEnabled(repeater, true);
        var repaired = ControlAutomationPeer.CreatePeerForElement(repeater);

        Assert.NotSame(original, repaired);
        Assert.IsAssignableFrom<FAItemsRepeaterAutomationPeer>(repaired);
        Assert.Empty(repaired.GetChildren());
    }

    [Fact]
    public void Enabled_PopulatedRepeater_PreservesRealizedChildren()
    {
        var button = new Button();
        var repeater = new FAItemsRepeater { ItemsSource = new[] { button } };
        FaItemsRepeaterAutomationAssist.SetIsEnabled(repeater, true);
        repeater.GetOrCreateElement(0);

        var peer = ControlAutomationPeer.CreatePeerForElement(repeater);

        Assert.Same(ControlAutomationPeer.CreatePeerForElement(button), Assert.Single(peer.GetChildren()));
    }

    [Fact]
    public void Disabled_RestoresTheOriginalPeerFactory()
    {
        var repeater = new FAItemsRepeater();
        FaItemsRepeaterAutomationAssist.SetIsEnabled(repeater, true);
        FaItemsRepeaterAutomationAssist.SetIsEnabled(repeater, false);

        Assert.IsType<FAItemsRepeaterAutomationPeer>(ControlAutomationPeer.CreatePeerForElement(repeater));
    }
}

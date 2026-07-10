using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.VisualTree;

namespace ClassIsland.Core.Controls;

/// <summary>
/// A <see cref="TabControl"/> that can be detached and attached again without
/// leaving its selected control in a presenter from the previous template.
/// </summary>
public class ReusableTabControl : TabControl
{
    private ContentPresenter? _selectedContentHost;
    private ContentPresenter? _selectedContentHost2;

    protected override Type StyleKeyOverride => typeof(TabControl);

    protected override bool RegisterContentPresenter(ContentPresenter presenter)
    {
        var isSelectedContentHost = presenter.Name == "PART_SelectedContentHost";
        var isSelectedContentHost2 = presenter.Name == "PART_SelectedContentHost2";

        // The old presenter has already lost its TemplatedParent at this point,
        // so use the presenters registered by the previous template as ownership proof.
        if (isSelectedContentHost &&
            SelectedContent is Control content &&
            content.GetVisualParent() is ContentPresenter oldPresenter &&
            !ReferenceEquals(oldPresenter, presenter) &&
            (ReferenceEquals(oldPresenter, _selectedContentHost) ||
             ReferenceEquals(oldPresenter, _selectedContentHost2)) &&
            ReferenceEquals(oldPresenter.Content, content))
        {
            oldPresenter.Content = null;
        }

        var result = base.RegisterContentPresenter(presenter);

        if (result && isSelectedContentHost)
        {
            _selectedContentHost = presenter;
        }
        else if (result && isSelectedContentHost2)
        {
            _selectedContentHost2 = presenter;
        }

        return result;
    }
}

using System.Text;
using ClassIsland.Shared;

namespace ClassIsland.Core.Models.ProfileAnalyzing;

/// <summary>
/// <see cref="AttachableSettingsObject"/>的位置。
/// </summary>
public class AttachableObjectAddress(Guid guid, int index = -1)
{
    public AttachableObjectAddress() : this(System.Guid.Empty, -1)
    {
    }

    public Guid Guid { get; } = guid;

    public int Index { get; } = index;


    /// <inheritdoc />
    public override int GetHashCode()
    {
        return new StringBuilder().Append(Guid).Append(Index).ToString().GetHashCode();
    }

    /// <inheritdoc />
    public override bool Equals(object? obj)
    {
        if (obj is AttachableObjectAddress address)
        {
            return address.Guid == Guid && address.Index == Index;
        }

        return false;
    }
}
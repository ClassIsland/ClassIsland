using System.Text;
using ClassIsland.Shared;

namespace ClassIsland.Core.Models.ProfileAnalyzing;

/// <summary>
/// <see cref="AttachableSettingsObject"/>的位置。
/// </summary>
public class AttachableObjectAddress(string guid, int index = -1)
{
    public AttachableObjectAddress() : this("", -1)
    {
        
    }

    public string Guid { get; set; } = guid;

    public int Index { get; set; } = index;


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
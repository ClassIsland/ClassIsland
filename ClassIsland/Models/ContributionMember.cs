using System.Windows.Media;
using System.Windows.Media.Imaging;

namespace ClassIsland.Models;

public class ContributionMember
{
    public string Name
    {
        get;
        set;
    } = "";

    public string Description
    {
        get;
        set;
    } = "";

    public string Website
    {
        get;
        set;
    } = "";

    public ImageSource AvatarImage
    {
        get;
        set;
    } = new BitmapImage();
}
using System.Windows;
using System.Windows.Media;

namespace Sqlbi.Bravo.UI.Controls
{
    public interface ITreeMapInfo
    {
        long Size { get; }

        Color RectangleColor { get; }

        Visibility OverlayVisibility { get; }
    }
}

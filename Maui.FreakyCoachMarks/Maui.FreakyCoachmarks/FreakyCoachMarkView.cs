using SkiaSharp;
using SkiaSharp.Views.Maui.Controls;
using Rect = SkiaSharp.SKRect;
using RoundRect = SkiaSharp.SKRoundRect;

namespace Maui.FreakyCoachMarks;

public class CoachMarkView : SKCanvasView
{
    public List<CoachMark> Marks { get; set; }


}

public class CoachMark
{
    public Point Position { get; set; }
    public CoachType CoachMarkType { get; set; }
    public string Text { get; set; }
    public TextPosition TextPosition { get; set; }
    public Rect Rect { get; set; }
}

public enum CoachType
{
    Circular,
    Rectangle
}

public enum TextPosition
{
    Above,
    Below,
    Left,
    Right
}
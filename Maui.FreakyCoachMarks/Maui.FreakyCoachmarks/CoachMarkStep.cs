namespace Maui.FreakyCoachMarks;

public class CoachMarkStep : ContentView
{
    public static readonly BindableProperty HighlightXProperty =
            BindableProperty.Create(nameof(HighlightX), typeof(double), typeof(CoachMarkStep), 0.0);

    public static readonly BindableProperty HighlightYProperty =
        BindableProperty.Create(nameof(HighlightY), typeof(double), typeof(CoachMarkStep), 0.0);

    public static readonly BindableProperty HighlightWidthProperty =
        BindableProperty.Create(nameof(HighlightWidth), typeof(double), typeof(CoachMarkStep), 0.0);

    public static readonly BindableProperty HighlightHeightProperty =
        BindableProperty.Create(nameof(HighlightHeight), typeof(double), typeof(CoachMarkStep), 0.0);

    public static readonly BindableProperty MessageProperty =
        BindableProperty.Create(nameof(Message), typeof(string), typeof(CoachMarkStep), string.Empty);

    public static readonly BindableProperty MessageXProperty =
        BindableProperty.Create(nameof(MessageX), typeof(double), typeof(CoachMarkStep), 0.0);

    public static readonly BindableProperty MessageYProperty =
        BindableProperty.Create(nameof(MessageY), typeof(double), typeof(CoachMarkStep), 0.0);

    public static readonly BindableProperty MessageWidthProperty =
        BindableProperty.Create(nameof(MessageWidth), typeof(double), typeof(CoachMarkStep), 0.0);

    public static readonly BindableProperty MessageHeightProperty =
        BindableProperty.Create(nameof(MessageHeight), typeof(double), typeof(CoachMarkStep), 0.0);

    public double HighlightX
    {
        get => (double)GetValue(HighlightXProperty);
        set => SetValue(HighlightXProperty, value);
    }

    public double HighlightY
    {
        get => (double)GetValue(HighlightYProperty);
        set => SetValue(HighlightYProperty, value);
    }

    public double HighlightWidth
    {
        get => (double)GetValue(HighlightWidthProperty);
        set => SetValue(HighlightWidthProperty, value);
    }

    public double HighlightHeight
    {
        get => (double)GetValue(HighlightHeightProperty);
        set => SetValue(HighlightHeightProperty, value);
    }

    public string Message
    {
        get => (string)GetValue(MessageProperty);
        set => SetValue(MessageProperty, value);
    }

    public double MessageX
    {
        get => (double)GetValue(MessageXProperty);
        set => SetValue(MessageXProperty, value);
    }

    public double MessageY
    {
        get => (double)GetValue(MessageYProperty);
        set => SetValue(MessageYProperty, value);
    }

    public double MessageWidth
    {
        get => (double)GetValue(MessageWidthProperty);
        set => SetValue(MessageWidthProperty, value);
    }

    public double MessageHeight
    {
        get => (double)GetValue(MessageHeightProperty);
        set => SetValue(MessageHeightProperty, value);
    }
}
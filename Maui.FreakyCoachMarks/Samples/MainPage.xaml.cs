using System.Globalization;

namespace Samples;

public partial class MainPage : ContentPage
{
    public MainPage()
    {
        InitializeComponent();
        // Ensure bindings are updated
        this.BindingContext = this;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        coachMarkControl.IsVisible = true;
    }
}

public class DoubleConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is double doubleValue)
        {
            return doubleValue;
        }
        return 0;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
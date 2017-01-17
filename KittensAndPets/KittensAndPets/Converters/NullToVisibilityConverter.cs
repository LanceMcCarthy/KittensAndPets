using System;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Data;

namespace KittensAndPets.Converters
{
    class NullToVisibilityConverter : IValueConverter
    {
        public bool IsInverted { get; set; }

        public object Convert(object value, Type targetType, object parameter, string language)
        {
            // This also covers the non-null empty string possibility
            if (IsInverted)
            {
                if (string.IsNullOrEmpty(value?.ToString()))
                    return Visibility.Visible;

                return Visibility.Collapsed;
            }
            else
            {
                if (string.IsNullOrEmpty(value?.ToString()))
                    return Visibility.Collapsed;

                return Visibility.Visible;
            }
            
        }

        public object ConvertBack(object value, Type targetType, object parameter, string language)
        {
            throw new NotImplementedException();
        }
    }
}

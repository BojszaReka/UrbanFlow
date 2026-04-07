using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.Text;
using System.Windows.Data;
using static Urbanflow.src.backend.models.util.Log;

namespace Urbanflow.src.backend.models.util
{
	public class LogLevelToBrushConverter : IValueConverter
	{
		public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
		{
			return value switch
			{
				LogLevel.Success => Brushes.Green,
				LogLevel.Warning => Brushes.Gold,
				LogLevel.Error => Brushes.Red,
				_ => Brushes.White
			};
		}

		public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
		{
			throw new NotImplementedException();
		}
	}
}

using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace iShareDownloader.Converts
{
    public class SpeedConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            double speed;
            if (value == null) speed = 0;
            else
            {
                if (!double.TryParse(value.ToString(), out speed))
                {
                    speed = 0;
                }
            }
            if (speed > 1024 * 1024)
            {
                speed = speed / (1024 * 1024);
                return string.Format("{0} MB/s", speed.ToString("0.00"));
            }
            else if (speed > 1024)
            {
                speed = speed / (1024);
                return string.Format("{0} KB/s", speed.ToString("0.00"));
            }
            else
            {
                return string.Format("{0} B/s", speed.ToString("0.00"));
            }
        }

        public object ConvertBack(object value, Type targetType, object parameter, System.Globalization.CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}

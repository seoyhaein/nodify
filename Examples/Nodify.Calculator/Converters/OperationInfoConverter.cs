﻿using System;
using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Markup;

namespace Nodify.Calculator
{
    public class OperationInfoConverter : MarkupExtension, IMultiValueConverter
    {
        public object Convert(object[] values, Type targetType, object parameter, CultureInfo culture)
        {
            if (values.Length == 2 && values[0] is OperationInfoViewModel info && values[1] is Point location)
            {
                return new CreateOperationInfoViewModel(info, location);
            }

            return values;
        }

        public object[] ConvertBack(object value, Type[] targetTypes, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
        // https://riptutorial.com/wpf/example/22049/using-markupextension-with-converters-to-skip-recource-declaration
        public override object ProvideValue(IServiceProvider serviceProvider)
            => this;
    }
}

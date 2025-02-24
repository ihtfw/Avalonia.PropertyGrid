﻿using Avalonia.Controls;
using Avalonia.PropertyGrid.Utils;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Text;

namespace Avalonia.PropertyGrid.Controls.Factories.Builtins
{
    internal class CommonCellEditFactory : AbstractCellEditFactory
    {
        public override int ImportPriority => int.MinValue;

        public override Control HandleNewProperty(object target, PropertyDescriptor propertyDescriptor)
        {
            var converter = TypeDescriptor.GetConverter(propertyDescriptor.PropertyType);

            TextBox control = new TextBox();

            control.VerticalContentAlignment = Avalonia.Layout.VerticalAlignment.Center;
            control.FontFamily = FontUtils.DefaultFontFamily;
            control.IsEnabled = converter != null && converter.CanConvertFrom(typeof(string)) && converter.CanConvertTo(typeof(string));

            // set first ...
            // HandlePropertyChanged(target, propertyDescriptor, control);

            if (control.IsEnabled)
            {
                control.PropertyChanged += (s, e) =>
                {
                    if (e.Property == TextBox.TextProperty)
                    {
                        string value = e.NewValue as string;

                        try
                        {
                            DataValidationErrors.ClearErrors(control);
                            var obj = converter.ConvertFrom(value);
                            SetAndRaise(control, propertyDescriptor, target, obj);
                        }
                        catch (Exception ee)
                        {
                            DataValidationErrors.SetErrors(control, new string[] { ee.Message });
                        }
                    }
                };
            }

            return control;
        }

        public override bool HandlePropertyChanged(object target, PropertyDescriptor propertyDescriptor, Control control)
        {
            if(control is TextBox textBox)
            {
                var value = propertyDescriptor.GetValue(target);

                if(value != null)
                {
                    var converter = TypeDescriptor.GetConverter(propertyDescriptor.PropertyType);

                    if(converter != null && converter.CanConvertFrom(typeof(string)) && converter.CanConvertTo(typeof(string)))
                    {
                        try
                        {
                            DataValidationErrors.ClearErrors(control);
                            textBox.Text = converter.ConvertTo(value, typeof(string)) as string;
                        }
                        catch (Exception ee)
                        {
                            DataValidationErrors.SetErrors(control, new string[] { ee.Message });
                        }
                    }
                    else
                    {
                        textBox.Text = value.ToString();
                    }

                    return true;
                }
            }

            return false;
        }
    }
}

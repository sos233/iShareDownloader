using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Reflection;

namespace UnityTrainDownloader.Utils
{
    /// <summary>
    /// PasswordBox中Password绑定帮助类
    /// </summary>
    public static class PasswordBoxBindingHelper
    {
        public static readonly DependencyProperty BindedPasswordProperty =
            DependencyProperty.RegisterAttached("BindedPassword", typeof(string),
            typeof(PasswordBoxBindingHelper),
            new UIPropertyMetadata(string.Empty, OnBindedPasswordChanged));

        public static readonly DependencyProperty IsPasswordBindingEnabledProperty =
            DependencyProperty.RegisterAttached("IsPasswordBindingEnabled", typeof(bool),
            typeof(PasswordBoxBindingHelper),
            new UIPropertyMetadata(false, OnIsPasswordBindingEnabledChanged));

        //BindedPassword Get/Set
        public static string GetBindedPassword(DependencyObject obj)
        {
            return (string)obj.GetValue(BindedPasswordProperty);
        }
        public static void SetBindedPassword(DependencyObject obj, string value)
        {
            obj.SetValue(BindedPasswordProperty, value);
        }

        //IsPasswordBindingEnabled Get/Set
        public static bool GetIsPasswordBindingEnabled(DependencyObject obj)
        {
            return (bool)obj.GetValue(IsPasswordBindingEnabledProperty);
        }
        public static void SetIsPasswordBindingEnabled(DependencyObject obj, bool value)
        {
            obj.SetValue(IsPasswordBindingEnabledProperty, value);
        }

        //when the buffer changed, upate the passwordBox's password
        private static void OnBindedPasswordChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var passwordBox = obj as PasswordBox;
            if (passwordBox != null)
                passwordBox.Password = e.NewValue == null ? string.Empty : e.NewValue.ToString();
        }

        private static void OnIsPasswordBindingEnabledChanged(DependencyObject obj, DependencyPropertyChangedEventArgs e)
        {
            var passwordBox = obj as PasswordBox;
            if (passwordBox != null)
            {
                passwordBox.PasswordChanged -= PasswordBoxPasswordChanged;
                if ((bool)e.NewValue)
                    passwordBox.PasswordChanged += PasswordBoxPasswordChanged;
            }
        }

        //when the passwordBox's password changed, update the selection index and the buffer
        private static void PasswordBoxPasswordChanged(object sender, RoutedEventArgs e)
        {
            var passwordBox = sender as PasswordBox;
            if (passwordBox != null)
            {
                SetPasswordBoxSelection(passwordBox, passwordBox.Password.Length + 1, passwordBox.Password.Length + 1);
                if (!String.Equals(GetBindedPassword(passwordBox), passwordBox.Password))
                    SetBindedPassword(passwordBox, passwordBox.Password);
            }
        }

        //update the selection index
        private static void SetPasswordBoxSelection(PasswordBox passwordBox, int start, int length)
        {
            var select = passwordBox.GetType().GetMethod("Select", BindingFlags.Instance | BindingFlags.NonPublic);
            select.Invoke(passwordBox, new object[] { start, length });
        }
    }
}

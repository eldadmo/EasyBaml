using System;
using System.Collections;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace EasyBamlAddin.Tools.Behaviors
{
    public static class ListBoxMultipleSelectionBehavior
    {
        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.RegisterAttached("SelectedItems",
            typeof(IList), typeof(ListBoxMultipleSelectionBehavior));

        public static readonly DependencyProperty AttachProperty =
            DependencyProperty.RegisterAttached("Attach",
            typeof(bool), typeof(ListBoxMultipleSelectionBehavior), new PropertyMetadata(false, Attach));

        public static readonly DependencyProperty DoubleClickCommandProperty =
            DependencyProperty.RegisterAttached("DoubleClickCommand",
            typeof(ICommand), typeof(ListBoxMultipleSelectionBehavior), new PropertyMetadata(null, OnDoubleClickCommandChanged));

        public static void SetAttach(DependencyObject dp, bool value)
        {
            dp.SetValue(AttachProperty, value);
        }

        public static bool GetAttach(DependencyObject dp)
        {
            return (bool)dp.GetValue(AttachProperty);
        }

        public static void SetDoubleClickCommand(DependencyObject dp, ICommand value)
        {
            dp.SetValue(DoubleClickCommandProperty, value);
        }

        public static ICommand GetDoubleClickCommand(DependencyObject dp)
        {
            return (ICommand)dp.GetValue(DoubleClickCommandProperty);
        }

        public static IList GetSelectedItems(DependencyObject dp)
        {
            return (IList)dp.GetValue(SelectedItemsProperty);
        }

        public static void SetSelectedItems(DependencyObject dp, IList value)
        {
            dp.SetValue(SelectedItemsProperty, value);
        }

        private static void Attach(DependencyObject sender,
            DependencyPropertyChangedEventArgs e)
        {
            var listBox = sender as ListBox;

            if (listBox == null)
                return;

            if ((bool)e.OldValue)
            {
                listBox.SelectionChanged -= SelectionChanged;
            }

            if ((bool)e.NewValue)
            {
                listBox.SelectionChanged += SelectionChanged;
            }
        }

        private static void SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            var listBox = sender as ListBox;
            SetSelectedItems(listBox, listBox.SelectedItems);
        }

        private static void OnDoubleClickCommandChanged(DependencyObject sender, DependencyPropertyChangedEventArgs e)
        {
            var listBox = sender as ListBox;

            listBox.MouseDoubleClick -= listBox_MouseDoubleClick;

            listBox.MouseDoubleClick += listBox_MouseDoubleClick;
        }

        static void listBox_MouseDoubleClick(object sender, MouseButtonEventArgs e)
        {
            var command = GetDoubleClickCommand((DependencyObject) sender);
            if (command != null)
            {
                command.Execute(null);
            }
        }
    } 
}

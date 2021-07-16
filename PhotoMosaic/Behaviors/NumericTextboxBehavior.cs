using Microsoft.Xaml.Behaviors;
using PhotoMosaic.Common.Extensions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PhotoMosaic.Behaviors
{
    public class NumericTextboxBehavior : Behavior<TextBox>
    {
        public static readonly DependencyProperty MaxProperty =
            DependencyProperty.Register(
            "Max", typeof(int),
            typeof(NumericTextboxBehavior),
            new PropertyMetadata(int.MaxValue)
            );

        public int Max
        {
            get { return (int)GetValue(MaxProperty); }
            set { SetValue(MaxProperty, value); }
        }

        public static readonly DependencyProperty MinProperty =
            DependencyProperty.Register(
            "Min", typeof(int),
            typeof(NumericTextboxBehavior),
            new PropertyMetadata(int.MinValue)
            );

        public int Min
        {
            get { return (int)GetValue(MinProperty); }
            set { SetValue(MinProperty, value); }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register(
            "Value", typeof(int),
            typeof(NumericTextboxBehavior),
            new PropertyMetadata(ValuePropertyChanged)
            );

        public int Value
        {
            get { return (int)GetValue(ValueProperty); }
            set { SetValue(ValueProperty, value); }
        }

        private bool _valueChangedInternally = false;

        public bool ValueChangedInternally => _valueChangedInternally;

        protected override void OnAttached()
        {
            base.OnAttached();
            AssociatedObject.PreviewTextInput += AssociatedObject_PreviewTextInput;
            AssociatedObject.PreviewKeyDown += AssociatedObject_PreviewKeyDown;
            DataObject.AddPastingHandler(AssociatedObject, Past_Handler);

            AssociatedObject.Loaded += AssociatedObject_Loaded;

            UpdateValue(Value.ToString());
        }

        protected override void OnDetaching()
        {
            AssociatedObject.PreviewTextInput -= AssociatedObject_PreviewTextInput;
            AssociatedObject.PreviewKeyDown -= AssociatedObject_PreviewKeyDown;
            DataObject.RemovePastingHandler(AssociatedObject, Past_Handler);

            AssociatedObject.Loaded -= AssociatedObject_Loaded;
            base.OnDetaching();
        }

        private void Past_Handler(object sender, DataObjectPastingEventArgs e)
        {
            if (e.DataObject.GetDataPresent(typeof(String)))
            {
                String pastedText = (String)e.DataObject.GetData(typeof(String));
                var newText = AssociatedObject.InsertText(pastedText);
                if (!UpdateValue(newText))
                {
                    e.CancelCommand();
                }
            }
            else
            {
                e.CancelCommand();
            }
        }

        private void AssociatedObject_PreviewTextInput(object sender, System.Windows.Input.TextCompositionEventArgs e)
        {
            e.Handled = !UpdateValue(AssociatedObject.InsertText(e.Text));
        }

        private void AssociatedObject_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            switch (e.Key)
            {
                case System.Windows.Input.Key.Back:
                    e.Handled = !UpdateValue(AssociatedObject.BackSpaceText());
                    break;
                case System.Windows.Input.Key.Delete:
                    e.Handled = !UpdateValue(AssociatedObject.DeleteText());
                    break;
            }

        }

        private bool UpdateValue(string newValue)
        {
            try
            {
                var newVal = (int)Convert.ChangeType(newValue, typeof(int));

                if (newVal < Min)
                {
                    _valueChangedInternally = true;
                    Value = Min;
                    _valueChangedInternally = false;
                    AssociatedObject.Text = Min.ToString();
                    return false;
                }
                else if (newVal > Max)
                {
                    _valueChangedInternally = true;
                    Value = Max;
                    _valueChangedInternally = false;
                    AssociatedObject.Text = Max.ToString();
                    return false;
                }
                else
                {
                    _valueChangedInternally = true;
                    Value = newVal;
                    _valueChangedInternally = false;
                    return true;
                }
            }
            catch (Exception e)
            {
                return false;
            }
        }

        private static void ValuePropertyChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behav = (NumericTextboxBehavior)d;

            if (behav?.AssociatedObject?.IsLoaded != true || behav.ValueChangedInternally)
                return;

            behav.AssociatedObject.Text = behav.Value.ToString();
        }
        private void AssociatedObject_Loaded(object sender, RoutedEventArgs e)
        {
            AssociatedObject.Text = Value.ToString();
        }
    }
}

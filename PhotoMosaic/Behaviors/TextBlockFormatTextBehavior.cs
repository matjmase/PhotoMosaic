using Microsoft.Xaml.Behaviors;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace PhotoMosaic.Behaviors
{
    public class TextBlockFormatTextBehavior : Behavior<TextBlock>
    {
        public static readonly DependencyProperty BeforeProperty =
    DependencyProperty.Register(
    "Before", typeof(string),
    typeof(TextBlockFormatTextBehavior), new PropertyMetadata(string.Empty, ValueUpdated)
    );
        public string Before
        {
            get { return (string)GetValue(BeforeProperty); }
            set { SetValue(BeforeProperty, value); }
        }

        public static readonly DependencyProperty CenterProperty =
    DependencyProperty.Register(
    "Center", typeof(string),
    typeof(TextBlockFormatTextBehavior), new PropertyMetadata(string.Empty, ValueUpdated)
    );
        public string Center
        {
            get { return (string)GetValue(CenterProperty); }
            set { SetValue(CenterProperty, value); }
        }

        public static readonly DependencyProperty AfterProperty =
    DependencyProperty.Register(
    "After", typeof(string),
    typeof(TextBlockFormatTextBehavior), new PropertyMetadata(string.Empty, ValueUpdated)
    );


        public string After
        {
            get { return (string)GetValue(AfterProperty); }
            set { SetValue(AfterProperty, value); }
        }

        private bool _isAttached;

        public bool IsAttached => _isAttached;

        protected override void OnAttached()
        {
            base.OnAttached();
            _isAttached = true;
            ValueUpdated(this, new DependencyPropertyChangedEventArgs());
        }

        protected override void OnDetaching()
        {
            _isAttached = false;
            base.OnDetaching();
        }


        private static void ValueUpdated(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var behav = (TextBlockFormatTextBehavior)d;

            if (!behav.IsAttached)
                return;

            behav.AssociatedObject.Text = behav.Before + behav.Center + behav.After;
        }
    }

}

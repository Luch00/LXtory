using System;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace ScreenShotterWPF
{
    /// <summary>
    /// Interaction logic for UpDown.xaml
    /// </summary>
    public partial class UpDown : UserControl
    {
        private readonly Regex _numMatch;

        public UpDown()
        {
            InitializeComponent();

            _numMatch = new Regex(@"^-?\d+$");
            Maximum = int.MaxValue;
            Minimum = 0;
            valueText.Text = "0";
        }

        private void ResetText(TextBox tb)
        {
            tb.Text = 0 < Minimum ? Minimum.ToString() : "0";
            tb.SelectAll();
        }

        private void valueText_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            var tb = (TextBox)sender;
            var text = tb.Text.Insert(tb.CaretIndex, e.Text);

            e.Handled = !_numMatch.IsMatch(text);
        }

        private void valueText_TextChanged(object sender, TextChangedEventArgs e)
        {
            var tb = (TextBox)sender;
            //if (!_numMatch.IsMatch(tb.Text)) ResetText(tb);
            //TextValue = tb.Text;
            Value = Convert.ToInt32(tb.Text);
            //if (Value < Minimum) Value = Minimum;
            //if (Value > Maximum) Value = Maximum;
            RaiseEvent(new RoutedEventArgs(ValueChangedEvent));
        }

        private void Increase_Click(object sender, RoutedEventArgs e)
        {
            if (Value < Maximum)
            {
                Value++;
                RaiseEvent(new RoutedEventArgs(IncreaseClickedEvent));
            }
        }

        private void Decrease_Click(object sender, RoutedEventArgs e)
        {
            if (Value > Minimum)
            {
                Value--;
                RaiseEvent(new RoutedEventArgs(DecreaseClickedEvent));
            }
        }

        //public string TextValue
        //{
        //    get
        //    {
        //        //return "";
        //        return GetValue(ValueProperty).ToString();
        //    }
        //    set
        //    {
        //        SetActualValue(Convert.ToInt32(value));
        //        valueText.Text = value;
        //    }
        //}

        private void SetActualValue(int value)
        {
            Value = value;
            if (value < Minimum) Value = Minimum;
            if (value > Maximum) Value = Maximum;
            RaiseEvent(new RoutedEventArgs(ValueChangedEvent));
        }
        //private int realValue;
        public int RealValue
        {
            get
            {
                //return realValue;
                return (int)GetValue(RealValueProperty);
            }
            set
            {
                if (value < Minimum) RealValue = Minimum;
                if (value > Maximum) RealValue = Maximum;
                SetValue(RealValueProperty, value);
            }
        }

        public int Value
        {
            get
            {
                return (int)GetValue(ValueProperty);
            }
            set
            {
                //valueText.Text = value.ToString();
                RealValue = value;
                SetValue(ValueProperty, value);
            }
        }

        public static readonly DependencyProperty ValueProperty =
            DependencyProperty.Register("Value", typeof(int), typeof(UpDown),
              new PropertyMetadata(0, new PropertyChangedCallback(OnSomeValuePropertyChanged)));

        public static readonly DependencyProperty RealValueProperty =
            DependencyProperty.Register("RealValue", typeof(int), typeof(UpDown),
                new PropertyMetadata(0, new PropertyChangedCallback(OnRealValueChanged)));

        private static void OnRealValueChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            //
        }

        private static void OnSomeValuePropertyChanged(DependencyObject target, DependencyPropertyChangedEventArgs e)
        {
            UpDown upDownBox = target as UpDown;
            upDownBox.valueText.Text = e.NewValue.ToString();
        }

        public int Maximum
        {
            get { return (int)GetValue(MaximumProperty); }
            set { SetValue(MaximumProperty, value); }
        }

        public static readonly DependencyProperty MaximumProperty =
            DependencyProperty.Register("Maximum", typeof(int), typeof(UpDown), new UIPropertyMetadata(100));

        public int Minimum
        {
            get { return (int)GetValue(MinimumProperty); }
            set { SetValue(MinimumProperty, value); }
        }

        public static readonly DependencyProperty MinimumProperty =
            DependencyProperty.Register("Minimum", typeof(int), typeof(UpDown), new UIPropertyMetadata(0));

        private static readonly RoutedEvent ValueChangedEvent = EventManager.RegisterRoutedEvent("ValueChanged", RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(UpDown));

        public event RoutedEventHandler ValueChanged
        {
            add { AddHandler(ValueChangedEvent, value); }
            remove { RemoveHandler(ValueChangedEvent, value); }
        }

        private static readonly RoutedEvent IncreaseClickedEvent =
            EventManager.RegisterRoutedEvent("IncreaseClicked", RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(UpDown));

        public event RoutedEventHandler IncreaseClicked
        {
            add { AddHandler(IncreaseClickedEvent, value); }
            remove { RemoveHandler(IncreaseClickedEvent, value); }
        }

        private static readonly RoutedEvent DecreaseClickedEvent =
            EventManager.RegisterRoutedEvent("DecreaseClicked", RoutingStrategy.Bubble,
            typeof(RoutedEventHandler), typeof(UpDown));

        private void valueText_PreviewKeyDown(object sender, KeyEventArgs e)
        {
            if (e.IsDown && e.Key == Key.Up && Value < Maximum)
            {
                Value++;
                RaiseEvent(new RoutedEventArgs(IncreaseClickedEvent));
            }
            else if (e.IsDown && e.Key == Key.Down && Value > Minimum)
            {
                Value--;
                RaiseEvent(new RoutedEventArgs(DecreaseClickedEvent));
            }
        }

        private void valueText_PreviewMouseWheel(object sender, MouseWheelEventArgs e)
        {
            if (e.Delta > 0 && Value < Maximum)
            {
                Value++;
                RaiseEvent(new RoutedEventArgs(IncreaseClickedEvent));
            }
            else if (e.Delta < 0 && Value > Minimum)
            {
                Value--;
                RaiseEvent(new RoutedEventArgs(DecreaseClickedEvent));
            }
        }
    }
}

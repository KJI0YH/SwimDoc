using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;

namespace UI.Views.Controls.LoadingSpinner;

public partial class LoadingSpinner : UserControl
{
    public static readonly DependencyProperty IsActiveProperty = DependencyProperty.Register(
        nameof(IsActive),
        typeof(bool),
        typeof(LoadingSpinner),
        new PropertyMetadata(false, OnIsActiveChanged));

    public static readonly DependencyProperty AngleProperty = DependencyProperty.Register(
        nameof(Angle),
        typeof(double),
        typeof(LoadingSpinner),
        new PropertyMetadata(0.0));

    private DispatcherTimer? _timer;

    public LoadingSpinner()
    {
        InitializeComponent();
        Unloaded += (_, _) => SetActive(false);
    }

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public double Angle
    {
        get => (double)GetValue(AngleProperty);
        private set => SetValue(AngleProperty, value);
    }

    private static void OnIsActiveChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is LoadingSpinner spinner)
            spinner.SetActive((bool)e.NewValue);
    }

    private void SetActive(bool active)
    {
        if (active)
        {
            _timer ??= new DispatcherTimer(DispatcherPriority.Render, Dispatcher)
            {
                Interval = TimeSpan.FromMilliseconds(16)
            };
            _timer.Tick -= OnTick;
            _timer.Tick += OnTick;
            if (!_timer.IsEnabled)
                _timer.Start();
            return;
        }

        _timer?.Stop();
    }

    private void OnTick(object? sender, EventArgs e) =>
        Angle = (Angle + 10) % 360;
}

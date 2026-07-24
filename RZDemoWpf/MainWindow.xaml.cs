using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace RZDemoWpf
{
    public partial class MainWindow : Window
    {
        private readonly MainViewModel viewModel = new MainViewModel();

        public MainWindow()
        {
            InitializeComponent();
            DataContext = viewModel;
        }

        protected override void OnClosed(System.EventArgs e)
        {
            viewModel.Dispose();
            base.OnClosed(e);
        }

        private void JogNegativeButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CaptureJogButton(sender);
            viewModel.BeginJogHold(false);
            e.Handled = true;
        }

        private void JogPositiveButton_PreviewMouseLeftButtonDown(object sender, MouseButtonEventArgs e)
        {
            CaptureJogButton(sender);
            viewModel.BeginJogHold(true);
            e.Handled = true;
        }

        private void JogButton_PreviewMouseLeftButtonUp(object sender, MouseButtonEventArgs e)
        {
            ReleaseJogButton(sender);
            viewModel.EndJogHold();
            e.Handled = true;
        }

        private void JogButton_MouseLeave(object sender, MouseEventArgs e)
        {
            if (e.LeftButton == MouseButtonState.Pressed)
            {
                ReleaseJogButton(sender);
                viewModel.EndJogHold();
            }
        }

        private void JogButton_LostMouseCapture(object sender, MouseEventArgs e)
        {
            viewModel.EndJogHold();
        }

        private static void CaptureJogButton(object sender)
        {
            if (sender is Button button)
            {
                button.CaptureMouse();
            }
        }

        private static void ReleaseJogButton(object sender)
        {
            if (sender is Button button && button.IsMouseCaptured)
            {
                button.ReleaseMouseCapture();
            }
        }
    }
}

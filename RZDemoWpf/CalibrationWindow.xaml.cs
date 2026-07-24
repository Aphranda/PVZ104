using System.Windows;

namespace RZDemoWpf
{
    public partial class CalibrationWindow : Window
    {
        public CalibrationWindow(CalibrationViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}

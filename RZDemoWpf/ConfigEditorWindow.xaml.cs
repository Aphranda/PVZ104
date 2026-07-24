using System.Windows;

namespace RZDemoWpf
{
    public partial class ConfigEditorWindow : Window
    {
        public ConfigEditorWindow(ConfigEditorViewModel viewModel)
        {
            InitializeComponent();
            DataContext = viewModel;
        }
    }
}

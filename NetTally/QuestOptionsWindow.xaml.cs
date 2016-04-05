using System.Windows;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for QuestOptionsWindow.xaml
    /// </summary>
    public partial class QuestOptionsWindow : Window
    {
        public QuestOptionsWindow()
        {
            InitializeComponent();
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

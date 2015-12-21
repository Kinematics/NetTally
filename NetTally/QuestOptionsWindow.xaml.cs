using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for QuestOptionsWindow.xaml
    /// </summary>
    public partial class QuestOptionsWindow : Window
    {
        IQuest Quest { get; }
        public List<int> ValidPostsPerPage { get; } = new List<int> { 0, 5, 10, 15, 20, 25, 30, 35, 40, 45, 50 };

        public QuestOptionsWindow(IQuest quest)
        {
            Quest = quest;

            InitializeComponent();

            DataContext = Quest;
        }

        private void closeButton_Click(object sender, RoutedEventArgs e)
        {
            Close();
        }
    }
}

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
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace NetTally
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        Tally tally = new Tally();

        public MainWindow()
        {
            InitializeComponent();
            this.textBox.DataContext = tally;
            //this.DataContext = tally;
        }

        private void button_Click(object sender, RoutedEventArgs e)
        {
            tally.Run("puella-magi-adfligo-systema.2538", 36743, 0);
        }
    }
}

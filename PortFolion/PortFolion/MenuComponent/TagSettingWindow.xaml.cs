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

namespace PortFolion.Views
{
    /// <summary>
    /// TagSettingWindow.xaml の相互作用ロジック
    /// </summary>
    public partial class TagSettingWindow : MahApps.Metro.Controls.MetroWindow
    {
        public TagSettingWindow()
        {
            InitializeComponent();
            this.DataContext = new ViewModels.TagSettingViewModel();
        }
    }
}

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

namespace ShopifyUpdater
{
  /// <summary>
  /// Interaction logic for ConfigurationWindow.xaml
  /// </summary>
  public partial class ConfigurationWindow : Window
  {
    public ConfigurationWindow()
    {
      InitializeComponent();

      Max_Inventory.Text = Properties.Settings.Default.max_inventory;
      Adjustment_Type.SelectedIndex = Properties.Settings.Default.adjust_type == "+" ? 0 : 1;
      Adjust_Amount.Text = Properties.Settings.Default.adjust_amount;
      Ignore_Type.SelectedIndex = Properties.Settings.Default.filter_type == "Begins with" ? 0 : 1;
      Ignore_Text.Text = Properties.Settings.Default.filter_string;
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
      try
      {
        Properties.Settings.Default.max_inventory = Int32.Parse(Max_Inventory.Text).ToString();
      }
      catch (FormatException)
      {
        Properties.Settings.Default.max_inventory = "0";
      }

      Properties.Settings.Default.adjust_type = Adjustment_Type.Text;
      try
      {
        Properties.Settings.Default.adjust_amount = Int32.Parse(Adjust_Amount.Text).ToString();
      }
      catch (FormatException)
      {
        Properties.Settings.Default.adjust_amount = "0";
      }

      Properties.Settings.Default.filter_type = Ignore_Type.Text;
      Properties.Settings.Default.filter_string = Ignore_Text.Text;

      this.Close();
    }
  }
}

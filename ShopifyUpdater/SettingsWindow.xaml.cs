using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;

namespace ShopifyUpdater
{
  /// <summary>
  /// Interaction logic for SettingsWindow.xaml
  /// </summary>
  public partial class SettingsWindow : Window
  {
    public SettingsWindow()
    {
      InitializeComponent();

      if (Properties.Settings.Default.shopify_store != "")
      {
        Shopify_Store_Url.Text = Properties.Settings.Default.shopify_store;
        Shopify_Store_Url.Foreground = Brushes.Black;
      }

      if (Properties.Settings.Default.shopify_key != "")
      {
        Shopify_Api_Key.Text = Properties.Settings.Default.shopify_key;
        Shopify_Api_Key.Foreground = Brushes.Black;
      }

      if (Properties.Settings.Default.shopify_password != "")
      {
        Shopify_Api_Password.Text = Properties.Settings.Default.shopify_password;
        Shopify_Api_Password.Foreground = Brushes.Black;
      }
    }

    private void Cancel_Click(object sender, RoutedEventArgs e)
    {
      this.Close();
    }

    private void Save_Click(object sender, RoutedEventArgs e)
    {
      if (Shopify_Store_Url.Text != "Enter .myshopify.com url (example-store.myshopify.com)")
      {
        Properties.Settings.Default.shopify_store = Shopify_Store_Url.Text;
      }
      else
      {
        Properties.Settings.Default.shopify_store = "";
      }

      if (Shopify_Api_Key.Text != "Enter api key")
      {
        Properties.Settings.Default.shopify_key = Shopify_Api_Key.Text;
      }
      else
      {
        Properties.Settings.Default.shopify_key = "";
      }

      if (Shopify_Api_Password.Text != "Enter api password")
      {
        Properties.Settings.Default.shopify_password = Shopify_Api_Password.Text;
      }
      else
      {
        Properties.Settings.Default.shopify_password = "";
      }

      Properties.Settings.Default.Save();
      this.Close();
    }

    private void Shopify_Manual_Click(object sender, MouseButtonEventArgs e)
    {
      string target = "https://help.shopify.com/en/manual/apps/private-apps#enable-private-app-development-from-the-Shopify-admin";
      try
      {
        System.Diagnostics.Process.Start(target);
      }
      catch (System.ComponentModel.Win32Exception noBrowser)
      {
        if (noBrowser.ErrorCode == -2147467259)
          MessageBox.Show(noBrowser.Message);
      }
      catch (System.Exception other)
      {
        MessageBox.Show(other.Message);
      }
    }

    private void TextBox_Focus(object sender, RoutedEventArgs e)
    {
      string text = (sender as TextBox).Text;
      if (text == "Enter .myshopify.com url (example-store.myshopify.com)" ||
          text == "Enter api key" || text == "Enter api password")
      {
        (sender as TextBox).Text = "";
        (sender as TextBox).Foreground = Brushes.Black;
      }
    }

    private void TextBox_LoseFocus(object sender, RoutedEventArgs e)
    {
      string text = (sender as TextBox).Text;
      if (text == "")
      {
        if ((sender as TextBox).Name == "Shopify_Store_Url")
        {
          (sender as TextBox).Text = "Enter .myshopify.com url (example-store.myshopify.com)";
        }
        if ((sender as TextBox).Name == "Shopify_Api_Key")
        {
          (sender as TextBox).Text = "Enter api key";
        }
        if ((sender as TextBox).Name == "Shopify_Api_Password")
        {
          (sender as TextBox).Text = "Enter api password";
        }

          (sender as TextBox).Foreground = (SolidColorBrush)(new BrushConverter().ConvertFrom("#F08B8B8B"));
      }
    }
  }
}

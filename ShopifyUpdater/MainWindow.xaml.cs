using CsvHelper;
using Microsoft.Win32;
using ShopifySharp;
using ShopifySharp.Filters;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

namespace ShopifyUpdater
{

  /// <summary>
  /// Interaction logic for MainWindow.xaml
  /// </summary>
  public partial class MainWindow : Window
  {
    private string shopify_store;
    private string shopify_password;
    private string csv_location;
    private long location_id;

    public MainWindow()
    {
      InitializeComponent();
      Check_Credentials();
    }

    // Close the app
    private void Exit_Click(object sender, RoutedEventArgs e)
    {
      Application.Current.Shutdown();
    }

    // Show shopify credentials setup window
    private void Credentials_Setup_Click(object sender = null, RoutedEventArgs e = null)
    {
      SettingsWindow settings_window = new SettingsWindow();
      settings_window.Owner = this;
      settings_window.Closed += Check_Credentials;
      settings_window.ShowDialog();
    }

    // Check for shopify credentials
    private void Check_Credentials(object sender = null, EventArgs e = null)
    {
      if (Properties.Settings.Default.shopify_store == "" || Properties.Settings.Default.shopify_key == ""
          || Properties.Settings.Default.shopify_password == "")
      {
        Inform_Setup.Visibility = Visibility.Visible;
        Setup_Button.Visibility = Visibility.Visible;
        Window_Filler.Visibility = Visibility.Visible;
      }
      else
      {
        Inform_Setup.Visibility = Visibility.Hidden;
        Setup_Button.Visibility = Visibility.Hidden;
        Window_Filler.Visibility = Visibility.Hidden;

        shopify_store = Properties.Settings.Default.shopify_store;
        shopify_password = Properties.Settings.Default.shopify_password;

        GetLocations();
      }
    }

    // Get store locations
    private async void GetLocations()
    {
      LocationService service = new LocationService(shopify_store, shopify_password);
      try
      {
        var locations = await service.ListAsync();
        int current = 0;
        LocationBox.Items.Clear();
        foreach (Location location in locations)
        {
          ComboBoxItem item = new ComboBoxItem();
          item.Content = location.Name;
          item.Tag = location.Id;
          if (current == 0)
          {
            item.IsSelected = true;
            location_id = (long)location.Id;
          }
          LocationBox.Items.Insert(0, item);
          current++;
        }
      }
      catch (ShopifyException se)
      {
        if (se.Message == "(404 Not Found) Not Found")
        {
          MessageBox.Show("'" + shopify_store + "' is not an existing store.", "Shopify Inventory Updater Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
        else
        {
          MessageBox.Show("Invalid password", "Shopify Inventory Updater Error", MessageBoxButton.OK, MessageBoxImage.Warning);

        }
        Credentials_Setup_Click();
      }
      catch (System.Net.Http.HttpRequestException)
      {
        MessageBox.Show("Shopify Store Url must contain '.myshopify.com'", "Shopify Inventory Updater Error", MessageBoxButton.OK, MessageBoxImage.Warning);
        Credentials_Setup_Click();
      }



    }

    // Update chosen location
    private void Location_Change(object sender, RoutedEventArgs e)
    {
      location_id = (long)LocationBox.SelectedValue;
    }

    // Open browser to Shopify's help page on private apps
    private void Shopify_Manual_Click(object sender, RoutedEventArgs e)
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

    // Locate inventory CSV file
    private void Csv_Select(object sender, RoutedEventArgs e)
    {
      OpenFileDialog openFileDialog = new OpenFileDialog();
      openFileDialog.Filter = "CSV file (*.csv)|*.csv|All files (*.*)|*.*";
      if (openFileDialog.ShowDialog() == true)
      {
        csv_location = openFileDialog.FileName;

        using (var reader = new StreamReader(csv_location))
        using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
        {
          //csv must have header row
          csv.Configuration.HasHeaderRecord = true;
          csv.Configuration.Delimiter = ",";
          csv.Read();
          csv.ReadHeader();

          if (csv.Context.HeaderRecord.Length > 1)
          {
            QtyColumn.Items.Clear();
            SyncColumn.Items.Clear();
            NameColumn.Items.Clear();

            int current = 0;

            foreach (string head in csv.Context.HeaderRecord)
            {
              ComboBoxItem QtyItem = new ComboBoxItem();
              QtyItem.Content = head;
              QtyColumn.Items.Insert(0, QtyItem);

              ComboBoxItem SyncItem = new ComboBoxItem();
              SyncItem.Content = head;
              SyncColumn.Items.Insert(0, SyncItem);

              ComboBoxItem NameItem = new ComboBoxItem();
              NameItem.Content = head;
              NameColumn.Items.Insert(0, NameItem);

              if (current == 0)
              {
                QtyItem.IsSelected = true;
                SyncItem.IsSelected = true;
                NameItem.IsSelected = true;
              }

              current++;
            }
          }
          else
          {
            MessageBox.Show("CSV file needs to have at least 2 columns", "Shopify Inventory Updater Error", MessageBoxButton.OK, MessageBoxImage.Warning);
          }
        }
      }
    }

    // Start inventory update process
    private async void Start_Update(object sender, RoutedEventArgs e)
    {
      ShopifyService.SetGlobalExecutionPolicy(new SmartRetryExecutionPolicy());

      // Empty out status box
      Update_Status.Items.Clear();

      // Disable start update button
      Start_Update_Button.IsEnabled = false;

      // read from csv
      Read_Csv();

      // get products from shopify
      await Get_Shopify_Products();

      // start the update
      await Update_Shopify_Inventory();

      // Re-enable start update button
      Start_Update_Button.IsEnabled = true;

      // Alert the user that the update is finished
      MessageBox.Show("Done Updating", "Shopify Inventory Updater", MessageBoxButton.OK, MessageBoxImage.Information);

    }

    // Read the inventory csv file and save into database
    private void Read_Csv()
    {
      Update_Status.Items.Insert(0, DateTime.Now.ToString("T") + " Reading csv file");

      using (var reader = new StreamReader(csv_location))
      using (var csv = new CsvReader(reader, CultureInfo.InvariantCulture))
      {
        // csv must have header row
        csv.Configuration.HasHeaderRecord = true;
        csv.Configuration.Delimiter = ",";
        csv.Read();
        csv.ReadHeader();

        // empty out the database
        SqliteDataAccess.EmptyTable();

        while (csv.Read())
        {
          // create product
          ProductModel p = new ProductModel();

          p.Name = csv.GetField(NameColumn.Text);
          p.SyncCol = csv.GetField(SyncColumn.Text);
          try
          {
            // convert qty column into integer
            int qty = Int32.Parse(csv.GetField(QtyColumn.Text));

            // get additional inventory adjustments from settings
            int settings_max_inventory = Int32.Parse(Properties.Settings.Default.max_inventory);
            int settings_adjust_amount = Int32.Parse(Properties.Settings.Default.adjust_amount);

            qty = Properties.Settings.Default.adjust_type == "+" ? qty + settings_adjust_amount : qty - settings_adjust_amount;
            if (settings_max_inventory > 0)
            {
              qty = qty > settings_max_inventory ? settings_max_inventory : qty;
            }

            p.CsvQty = qty < 0 ? 0 : qty;
          }
          catch (FormatException)
          {
            p.CsvQty = 0;
          }

          // save product into database
          SqliteDataAccess.SaveProduct(p);
        }
      }

      Update_Status.Items.Insert(0, DateTime.Now.ToString("T") + " Done reading csv file");
    }

    // Get products from shopify and save into database
    private async Task Get_Shopify_Products()
    {
      Update_Status.Items.Insert(0, DateTime.Now.ToString("T") + " Getting products from Shopify");

      List<Product> products = new List<Product>();
      ProductService service = new ProductService(shopify_store, shopify_password);

      // get the max 250 allowed products each time
      var page = await service.ListAsync(new ProductListFilter
      {
        Limit = 250,
      });

      bool nextPage = true;
      // Loop through the product variants and save/update database products
      while (nextPage)
      {
        products.AddRange(page.Items);

        foreach (Product item in page.Items)
        {
          foreach (ProductVariant variant in item.Variants)
          {
            SqliteDataAccess.updateOrSaveProduct(variant);
          }
        }

        nextPage = page.HasNextPage;
        page = await service.ListAsync(page.GetNextPageFilter());
      }

      Update_Status.Items.Insert(0, DateTime.Now.ToString("T") + " Done getting products from Shopify");
    }

    // Start updating inventory on Shopify store
    private async Task Update_Shopify_Inventory()
    {
      Update_Status.Items.Insert(0, DateTime.Now.ToString("T") + " Starting to update inventory on Shopify");

      var service = new InventoryLevelService(shopify_store, shopify_password);

      // get products from database
      List<ProductModel> products = SqliteDataAccess.GetAllProducts();

      // Loop through database products
      foreach (ProductModel p in products)
      {
        bool ignoreP = false;

        // Check if should ignore current product by comparing strings not to update
        if (ignoreP == false && Properties.Settings.Default.filter_string != "")
        {
          string[] words = Properties.Settings.Default.filter_string.Split(',');

          foreach (string word in words)
          {
            if (ignoreP == false)
            {
              ignoreP = Properties.Settings.Default.filter_type == "Begins with" ? p.Name.StartsWith(word) : p.Name.EndsWith(word);
              Update_Status.Items.Insert(0, DateTime.Now.ToString("T") + " Ignoring: " + p.Name);
            }
          }
        }

        // If csv inventory is same as shopify's inventory ignore the product
        if (p.CsvQty == p.StoreQty)
        {
          ignoreP = true;
          Update_Status.Items.Insert(0, DateTime.Now.ToString("T") + " Ignoring: " + p.Name);
        }

        // If product does not need to be ignored, update
        if (ignoreP == false)
        {
          try
          {
            Update_Status.Items.Insert(0, DateTime.Now.ToString("T") + " Updating: " + p.Name + " : " + p.CsvQty);
            await service.SetAsync(new InventoryLevel()
            {
              InventoryItemId = p.ProductId,
              LocationId = location_id,
              Available = p.CsvQty
            });
          }
          // in case if Shopify Api Rate limit is reached, wait and redo
          catch (ShopifyRateLimitException)
          {
            await Task.Delay(10000);
            Update_Status.Items.Insert(0, DateTime.Now.ToString("T") + " Updating: " + p.Name + " : " + p.CsvQty);
            await service.SetAsync(new InventoryLevel()
            {
              InventoryItemId = p.ProductId,
              LocationId = location_id,
              Available = p.CsvQty
            });
          }
        }
      }

      Update_Status.Items.Insert(0, DateTime.Now.ToString("T") + " Done updating inventory on Shopify");
    }

    // Show additional configuration window
    private void Configuration_Click(object sender, RoutedEventArgs e)
    {
      ConfigurationWindow configureation_window = new ConfigurationWindow();
      configureation_window.Owner = this;
      configureation_window.ShowDialog();
    }

    // Open browser to view about the app
    private void About_Click(object sender, RoutedEventArgs e)
    {
      string target = "https://github.com/BryanYeh/InventoryUpdater";
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

    // Save update log (let user choose where)
    private void Save_Log_Click(object sender, RoutedEventArgs e)
    {
      SaveFileDialog saveFileDialog1 = new SaveFileDialog();

      saveFileDialog1.Filter = "Text file (*.txt)|*.txt";
      saveFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);


      if (saveFileDialog1.ShowDialog() == true)
      {
        FileStream stream = new FileStream(saveFileDialog1.FileName, FileMode.OpenOrCreate);

        using (StreamWriter writer = new StreamWriter(stream))
        {
          foreach (string item in Update_Status.Items)
          {
            writer.WriteLine(item);
          }
        }

        stream.Dispose();
      }
    }

    // Save missing products in Shopify
    private void Save_Shopify_Log_Click(object sender, RoutedEventArgs e)
    {
      SaveFileDialog saveFileDialog1 = new SaveFileDialog();

      saveFileDialog1.Filter = "Text file (*.txt)|*.txt";
      saveFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);


      if (saveFileDialog1.ShowDialog() == true)
      {
        FileStream stream = new FileStream(saveFileDialog1.FileName, FileMode.OpenOrCreate);

        using (StreamWriter writer = new StreamWriter(stream))
        {
          // get products from database
          List<ProductModel> products = SqliteDataAccess.GetMissingShopifyProducts();

          writer.WriteLine("Missing products in Shopify that was in csv file");

          // Loop through database products
          foreach (ProductModel p in products)
          {
            writer.WriteLine(p.Name);
          }
        }

        stream.Dispose();
      }
    }

    // Save missing products in Csv
    private void Save_Csv_Log_Click(object sender, RoutedEventArgs e)
    {
      SaveFileDialog saveFileDialog1 = new SaveFileDialog();

      saveFileDialog1.Filter = "Text file (*.txt)|*.txt";
      saveFileDialog1.InitialDirectory = Environment.GetFolderPath(Environment.SpecialFolder.MyDocuments);


      if (saveFileDialog1.ShowDialog() == true)
      {
        FileStream stream = new FileStream(saveFileDialog1.FileName, FileMode.OpenOrCreate);



        using (StreamWriter writer = new StreamWriter(stream))
        {
          // get products from database
          List<ProductModel> products = SqliteDataAccess.GetMissingCsvProducts();

          writer.WriteLine("Missing products in Csv that was in Shopify");

          // Loop through database products
          foreach (ProductModel p in products)
          {
            writer.WriteLine("Product ID: " + p.ProductId);
          }
        }

        stream.Dispose();
      }
    }

  }
}

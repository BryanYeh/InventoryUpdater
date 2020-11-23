using Dapper;
using ShopifySharp;
using System.Collections.Generic;
using System.Configuration;
using System.Data;
using System.Data.SQLite;
using System.Linq;

namespace ShopifyUpdater
{
  public class SqliteDataAccess
  {
    public static List<ProductModel> GetAllProducts()
    {
      using (IDbConnection ff = new SQLiteConnection(LoadConnectionString()))
      {
        var output = ff.Query<ProductModel>("select * from Product where SyncCol != '' AND CsvQty != -999 AND ProductId != 0");
        return output.ToList();
      }
    }

    public static List<ProductModel> GetMissingShopifyProducts()
    {
      using (IDbConnection ff = new SQLiteConnection(LoadConnectionString()))
      {
        var output = ff.Query<ProductModel>("select * from Product where ProductId == 0");
        return output.ToList();
      }
    }

    public static List<ProductModel> GetMissingCsvProducts()
    {
      using (IDbConnection ff = new SQLiteConnection(LoadConnectionString()))
      {
        var output = ff.Query<ProductModel>("select * from Product where AND CsvQty == -999");
        return output.ToList();
      }
    }

    public static List<ProductModel> GetProducts()
    {
      using (IDbConnection ff = new SQLiteConnection(LoadConnectionString()))
      {
        var output = ff.Query<ProductModel>("select * from Product where SyncCol != '' AND CsvQty != -999 AND ProductId != 0 AND CsvQty != StoreQty");
        return output.ToList();
      }
    }

    public static void updateOrSaveProduct(ProductVariant variant)
    {
      using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
      {
        if (variant.InventoryManagement == "shopify")
        {
          if (IsInDb(variant.SKU))
          {
            cnn.Query<ProductModel>("update Product set ProductId = @ProductId, StoreQty = @StoreQty where SyncCol = @SyncCol", new { ProductId = variant.InventoryItemId, StoreQty = variant.InventoryQuantity, SyncCol = variant.SKU });
          }
          else
          {
            cnn.Query<ProductModel>("insert into Product (Name, CsvQty, SyncCol, ProductId, StoreQty) values (@Name, @CsvQty, @SyncCol, @ProductId, @StoreQty)", new { Name = variant.Title, CsvQty = -999, SyncCol = variant.SKU, ProductId = variant.InventoryItemId, StoreQty = variant.InventoryQuantity });
          }
        }
      }
    }

    public static ProductModel GetProduct(string syncCol)
    {
      using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
      {
        var output = cnn.Query<ProductModel>("select * from Product where SyncCol = @SyncCol", new { SyncCol = syncCol });
        return output.FirstOrDefault();
      }
    }

    private static bool IsInDb(string syncCol)
    {
      using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
      {
        var rows = cnn.Query<ProductModel>("select * from Product where SyncCol = @SyncCol", new { SyncCol = syncCol });
        return rows.Count() > 0;
      }
    }

    public static void SaveProduct(ProductModel product)
    {
      using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
      {
        cnn.Execute("insert into Product (Name,SyncCol,CsvQty,ProductId,StoreQty) values (@Name,@SyncCol,@CsvQty,@ProductId,@StoreQty)", product);
      }
    }

    public static void EmptyTable()
    {
      using (IDbConnection cnn = new SQLiteConnection(LoadConnectionString()))
      {
        cnn.Execute("delete from Product");
        cnn.Execute("DELETE FROM SQLITE_SEQUENCE WHERE name='Product'");
        //cnn.Execute("CREATE TABLE 'Product' ('Id' INTEGER NOT NULL UNIQUE,'Name' TEXT,'SyncCol'	TEXT,'CsvQty' INTEGER,'ProductId' INTEGER,'StoreQty' INTEGER,PRIMARY KEY('Id' AUTOINCREMENT))");
      }
    }

    private static string LoadConnectionString(string id = "Db")
    {
      return ConfigurationManager.ConnectionStrings[id].ConnectionString;
    }
  }
}

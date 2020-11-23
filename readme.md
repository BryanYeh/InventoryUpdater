# Shopify Inventory Updater

## This **Private** app updates your inventory in Shopify store based on the SKU, with CSV file

### Using the app
1. Run``ShopifyUpdater.exe``
2. Setup you Shopify credentials (Settings -> Setup Credentials) - [How to get private app credentials](https://help.shopify.com/en/manual/apps/private-apps#enable-private-app-development-from-the-Shopify-admin)
3. Set optional settings (Settings -> Configuration)
  * **Max Inventory** leave at 0 to set no limit
  * **Adjust Inventory** - can add or subtract from csv inventory
  * **Ignore** - A do not update products by product names, seperate by ``,`` 
4. After updating credentials, choose your store location to update inventory
5. Open CSV file
6. Choose the columns to match
7. Start Update and wait until finished notification
  * Note: **It will seem like the program is frozen while it is reading the CSV file and getting products from the Shopify store**

### System Requirements
* [Microsoft .Net Framework 4.7.2](https://dotnet.microsoft.com/download/dotnet-framework/net472)

### Built with
* [ShopifySharp](https://github.com/nozzlegear/ShopifySharp)

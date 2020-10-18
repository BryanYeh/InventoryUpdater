namespace ShopifyUpdater
{
    public class ProductModel
    {
        public int Id { get; set; }
        public string Name { get; set; }
        public string SyncCol { get; set; }
        public int CsvQty { get; set; }
        public long ProductId { get; set; }
        public int StoreQty { get; set; }
    }
}

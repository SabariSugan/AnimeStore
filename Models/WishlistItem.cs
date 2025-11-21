namespace AnimeStore.Models
{
    public class WishlistItem
    {
        public int Id { get; set; }

        // User who added this item
        public string UserId { get; set; }

        // Product details
        public int ProductId { get; set; }
        public Product Product { get; set; }  
    }
}

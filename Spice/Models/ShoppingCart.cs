namespace Spice.Models
{
    using System.ComponentModel.DataAnnotations;
    using System.ComponentModel.DataAnnotations.Schema;

    public class ShoppingCart
    {
        public ShoppingCart()
        {
            this.Count = 1;
        }

        public int Id { get; set; }

        public string ApplicationUserId { get; set; }

        [NotMapped]
        [ForeignKey("ApplicationUserId")]
        public ApplicationUser ApplicationUser { get; set; }

        public int MenuItemId { get; set; }

        [NotMapped]
        [ForeignKey("MenuItemId")]
        public MenuItem MenuItem { get; set; }

        [Range(1, int.MaxValue, ErrorMessage = "Please enter a value greater than or equal to {1}")]
        public int Count { get; set; }
    }
}

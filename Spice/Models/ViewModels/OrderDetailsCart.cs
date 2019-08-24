namespace Spice.Models.ViewModels
{
    using System.Collections.Generic;

    public class OrderDetailsCart
    {
        public List<ShoppingCart> ListCart { get; set; }

        public OrderHeader OrderHeader { get; set; }
    }
}

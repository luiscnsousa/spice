namespace Spice.Models.ViewModels
{
    using System.Collections.Generic;

    public class OrderDetailsViewModel
    {
        public OrderHeader OrderHeader { get; set; }

        public List<OrderDetails> OrderDetails { get; set; }
    }
}

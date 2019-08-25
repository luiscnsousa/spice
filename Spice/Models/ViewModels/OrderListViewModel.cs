namespace Spice.Models.ViewModels
{
    using System.Collections.Generic;

    public class OrderListViewModel
    {
        public IList<OrderDetailsViewModel> Orders { get; set; }

        public PagingInfo PagingInfo { get; set; }
    }
}

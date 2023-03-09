using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ObserverWoodDeal
{
    internal class WoodDeal
    {
        
        public string sellerName { get; set; }
        public string sellerInn { get; set; }
        public string buyerName { get; set; }
        public string buyerInn { get; set; }
        public double woodVolumeBuyer { get; set; }
        public double woodVolumeSeller { get; set; }
        public string dealNumber { get; set; }
        public DateTime? dealDate { get; set; }

    }
}

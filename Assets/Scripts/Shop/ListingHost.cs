using UnityEngine;

namespace Collector.Shop
{
    /// <summary>
    /// İlan sayfasını (<see cref="ListingDetailUI"/>) barındıran pencere: bilgisayardaki ilan penceresi
    /// veya bit pazarındaki tezgah penceresi. Satın alma ve kapatma nasıl yapılacağını host belirler.
    /// </summary>
    public abstract class ListingHost : MonoBehaviour
    {
        public abstract void BuyListing(Listing listing);

        /// <summary>İlan sayfasındaki geri butonu.</summary>
        public abstract void CloseDetail();

        public void Negotiate(Listing listing, int offer)
        {
            Bargaining.Negotiate(listing, offer);
            OnListingChanged();
        }

        protected virtual void OnListingChanged() { }
    }
}

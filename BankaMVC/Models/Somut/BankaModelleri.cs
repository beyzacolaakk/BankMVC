namespace BankaMVC.Models.Somut
{
    namespace BankaYonetimPaneli.Models
    {

        // Enum tanımlamaları
        public enum IstekDurumu
        {
            Pending,
            Active,
            Rejected
        }
         
        public enum DestekDurumu
        {
            Open,
            Process,
            Resolved,  
            Closed
        }

        public enum KartTipi
        {
            Credit,
            Bank
        }
    }

}

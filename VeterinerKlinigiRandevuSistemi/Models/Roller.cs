namespace VeterinerKlinigiRandevuSistemi.Models
{
    public static class Roller
    {
        //Attribute'lar yalnızca derleme zamanında bilinen sabit değerler kabul ettiği için, controller'larda yetki kontrolü yaparken const tanımlanır.
        public const string Admin = "Admin";
        public const string Veteriner = "Veteriner";
        public const string Uye = "Uye";

        public static readonly string[] Tumu = { Admin, Veteriner, Uye };
    }
}

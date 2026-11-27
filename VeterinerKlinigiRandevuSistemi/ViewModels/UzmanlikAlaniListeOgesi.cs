namespace VeterinerKlinigiRandevuSistemi.ViewModels;

//Uzmanlık listesinde her satır: alanın adı + o alanda kaç veteriner var
public class UzmanlikAlaniListeOgesi
{
    public int Id { get; set; }
    public string Ad { get; set; } = string.Empty;
    public int VeterinerSayisi { get; set; }
}
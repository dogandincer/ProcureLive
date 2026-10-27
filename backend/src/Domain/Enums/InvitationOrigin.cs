namespace ProcureLive.Domain.Enums;

/// <summary>
/// Katılım kaydının kaynağı. Yalnızca gerçek davetler (Direct / InviteLink) ihale sahibini "davet eden firma"
/// yapar ve Standart üyeye o firmanın diğer açık ihalelerini açar. Vitrinden kendiliğinden katılım (SelfJoin)
/// bunu açmaz — aksi halde Plus vitrinindeki tek bir ihaleye katılmak, firmanın listelemeye kapalı ihalelerini
/// de görünür kılardı.
/// </summary>
public enum InvitationOrigin
{
    Direct = 0,
    InviteLink = 1,
    SelfJoin = 2
}

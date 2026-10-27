using System.Security.Cryptography;
using Microsoft.EntityFrameworkCore;
using ProcureLive.Application.Common.Exceptions;
using ProcureLive.Application.Common.Interfaces;
using ProcureLive.Contracts.Invitations;
using ProcureLive.Domain.Entities.Auctions;
using ProcureLive.Domain.Enums;

namespace ProcureLive.Application.Invitations;

internal static class InviteRules
{
    public const int DefaultValidDays = 7;
    public const int MaxValidDays = 30;

    /// <summary>256-bit, URL-güvenli rastgele token (tahmin edilemez; kaba kuvvet pratikte imkânsız).</summary>
    public static string NewToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(32))
            .Replace('+', '-').Replace('/', '_').TrimEnd('=');

    public static string PathFor(string token) => $"/invite?token={token}";

    public static bool IsAuctionClosed(AuctionStatus status) =>
        status is AuctionStatus.Ended or AuctionStatus.Cancelled or AuctionStatus.Awarded;

    public static InviteLinkDto ToDto(InvitationToken t, string companyName, DateTime now) =>
        new(t.Id, t.Token, PathFor(t.Token), t.CompanyId, companyName, t.Email, t.ExpiresAt, t.UsedAt, t.ExpiresAt <= now, t.CreatedAt);

    /// <summary>Token'ı yükler ve kullanılabilirliğini doğrular (yoksa/süresi dolmuşsa/ihale kapanmışsa kodlu hata).</summary>
    public static async Task<InvitationToken> LoadUsableAsync(IApplicationDbContext context, string token, DateTime now, CancellationToken ct)
    {
        InvitationToken? t = await context.InvitationTokens
            .Include(x => x.Auction)
            .FirstOrDefaultAsync(x => x.Token == token, ct);

        if (t is null)
            throw new ConflictException("Davet linki geçersiz.", ErrorCodes.InviteInvalid);
        if (t.ExpiresAt <= now)
            throw new ConflictException("Davet linkinin süresi dolmuş.", ErrorCodes.InviteExpired);
        if (IsAuctionClosed(t.Auction.Status))
            throw new ConflictException("Bu ihale artık katılıma kapalı.", ErrorCodes.AuctionClosed);

        return t;
    }

    /// <summary>
    /// Cari için davet kaydını garanti eder; reddedilmiş daveti yeniden "Davetli" yapar (bilinçli yeniden davet).
    /// Kendiliğinden katılım kaydı varsa artık gerçek bir davettir (Origin = InviteLink).
    /// </summary>
    public static async Task EnsureInvitedAsync(
        IApplicationDbContext context, Guid auctionId, Guid companyId, Guid invitedByUserId, DateTime now, CancellationToken ct)
    {
        AuctionInvitation? invitation = await context.AuctionInvitations
            .FirstOrDefaultAsync(i => i.AuctionId == auctionId && i.CompanyId == companyId, ct);

        if (invitation is null)
        {
            context.AuctionInvitations.Add(new AuctionInvitation
            {
                AuctionId = auctionId,
                CompanyId = companyId,
                InvitedByUserId = invitedByUserId,
                Status = InvitationStatus.Invited,
                Origin = InvitationOrigin.InviteLink,
                InvitedAt = now,
            });
            return;
        }

        if (invitation.Origin == InvitationOrigin.SelfJoin)
        {
            invitation.Origin = InvitationOrigin.InviteLink;
        }

        if (invitation.Status == InvitationStatus.Declined)
        {
            invitation.Status = InvitationStatus.Invited;
            invitation.RespondedAt = null;
        }
    }
}

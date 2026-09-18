using Zouq.Domain.Enums;

namespace Zouq.Domain.Designs;

/// <summary>
/// Design lifecycle rules. Public/For You content requires Delivered-eligible then explicit publish.
/// </summary>
public static class DesignLifecycleRules
{
    public static bool IsImmutable(DesignStatus status) =>
        status is DesignStatus.Ordered or DesignStatus.DeliveredEligible or DesignStatus.Reusable
            or DesignStatus.UsedInOrder;

    public static bool IsEditableDraft(DesignStatus status) =>
        status is DesignStatus.Draft or DesignStatus.Unpublished;

    /// <summary>Eligible for For You / reuse by others.</summary>
    public static bool IsPublicReusable(DesignStatus status, DesignVisibility visibility) =>
        status == DesignStatus.Reusable && visibility == DesignVisibility.Public;

    public static void EnsureEditable(DesignStatus status)
    {
        if (IsImmutable(status))
            throw new InvalidOperationException("Design used in an order is immutable.");
    }

    public static void EnsureCanPublishAsReusable(DesignStatus status)
    {
        if (status != DesignStatus.DeliveredEligible && status != DesignStatus.Reusable)
            throw new InvalidOperationException(
                "Design can only be published for reuse after its related order is Delivered.");
    }

    /// <summary>
    /// Order creation: own editable draft, OR (handled separately) public reusable source.
    /// Private designs owned by others are never orderable.
    /// </summary>
    public static void EnsureCanOrderAsOwnedDraft(Guid buyerId, Guid ownerId, DesignStatus status)
    {
        if (ownerId != buyerId)
            throw new UnauthorizedAccessException("You cannot order another user's design.");

        if (!IsEditableDraft(status))
            throw new InvalidOperationException("Only editable draft designs owned by you can be ordered.");
    }

    public static void EnsureCanDeriveAsReusable(DesignStatus status, DesignVisibility visibility, bool isDeleted)
    {
        if (isDeleted)
            throw new UnauthorizedAccessException("Design is not available.");

        if (!IsPublicReusable(status, visibility))
            throw new UnauthorizedAccessException(
                "Design is not published for reuse. Only Delivered + explicitly published designs can be used.");
    }
}

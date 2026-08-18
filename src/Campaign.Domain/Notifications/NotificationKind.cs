namespace Campaign.Domain.Notifications;

/// <summary>
/// Why an in-app or email notice was created. Email bodies never include hidden orders or private chat text.
/// </summary>
public enum NotificationKind
{
    /// <summary>The recipient was tagged in a chat message they can see.</summary>
    Mention = 0,

    /// <summary>A private chat message was sent to a channel the recipient can read.</summary>
    PrivateChat = 1,

    /// <summary>The campaign launched and the first phase opened.</summary>
    CampaignStarted = 2,

    /// <summary>The campaign reached its end instant.</summary>
    CampaignEnded = 3,

    /// <summary>A new phase opened after the previous window resolved.</summary>
    PhaseChanged = 4,

    /// <summary>The recipient still needs to submit orders, a battle result, or a retreat.</summary>
    ActionRequired = 5,

    /// <summary>The recipient was tagged in public site chat.</summary>
    SiteChatMention = 6,

    /// <summary>An administrator sent a site-chat announcement to everyone or to this recipient.</summary>
    SiteAdminMessage = 7,

    /// <summary>A campaign manager removed the recipient from a campaign.</summary>
    CampaignKicked = 8,
}

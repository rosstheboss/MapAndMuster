namespace MapAndMuster.Infrastructure.Email;

/// <summary>
/// A composed outbound email. Bodies must not include hidden orders, private chat, or secret tokens in logs.
/// </summary>
/// <param name="To">The recipient address.</param>
/// <param name="Subject">The subject line.</param>
/// <param name="Body">The plain-text body.</param>
public sealed record EmailMessage(string To, string Subject, string Body);

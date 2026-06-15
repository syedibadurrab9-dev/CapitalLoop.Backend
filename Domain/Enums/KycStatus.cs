using System;

namespace Domain.Enums;

/// <summary>
/// Reflects the operational onboarding lifecycle state boundaries of a user identity verification trail [CL-104].
/// Explicitly coordinates system privilege restrictions across both the Mobile Client App and Angular Admin Portal modules.
/// </summary>
public enum KycStatus
{
    /// <summary>
    /// Initial baseline state. User profile lacks uploaded document images or has had a prior submission explicitly rejected.
    /// Blocks Premium Upgrade workflows and locks down specialized investment advisory tools.
    /// </summary>
    Unverified = 0,

    /// <summary>
    /// Onshore Document OCR extraction completed. Image payloads and metadata vectors are sitting 
    /// securely inside processing queues awaiting back-office specialist interaction.
    /// </summary>
    Pending = 1,

    /// <summary>
    /// Document aggregate invariants verified successfully by an active compliance agent. 
    /// Unlocks advanced robo-advisory features, manual ledger tracking extensions, and external synchronization channels.
    /// </summary>
    Approved = 2
}
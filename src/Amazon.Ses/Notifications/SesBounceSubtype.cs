﻿namespace Amazon.Ses
{
    public enum SesBounceSubtype : byte
    {
        Undetermined       = 1,
        General            = 2,
        NoEmail            = 3,
        Suppressed         = 4,
        MailboxFull        = 5,
        MessageToolarge    = 6,
        ContentRejected    = 7,
        AttachmentRejected = 8
    }
}
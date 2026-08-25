-- Seed the statuses. These strings are the wire values in GiftExchange.Library.Models.HatStatus;
-- READY_TO_CLOSE is the "cooled off" state, which kept its original name on the wire.
INSERT INTO hat_status (status) VALUES
    ('IN_PROGRESS'),
    ('READY_FOR_ASSIGNMENT'),
    ('NAMES_ASSIGNED'),
    ('INVITATIONS_SENT'),
    ('READY_TO_CLOSE'),
    ('CLOSED')

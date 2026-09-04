-- CRUD on the three do-not-add lists and on participant_leave_token.
--
-- All four are read and written by the API function, which connects as this role, so one grant
-- covers every path: the leave pages issue and consume the tokens, and every service that adds
-- somebody to an exchange consults the lists first.
--
-- SELECT alone would not do for the lists. Leaving writes to them, and it writes as the same role
-- that reads them -- there is no separate administrative path here, because a refusal that only an
-- administrator could record would not be a refusal the person themselves had made.
GRANT SELECT, INSERT, UPDATE, DELETE ON do_not_add_to_exchange, do_not_add_by_organizer, do_not_add_anywhere, participant_leave_token TO giftexchange_user

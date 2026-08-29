# Receiving gift ideas by email.
#
# A subdomain of its own rather than mail.namesoutofahat.com, and the reason is the rule in ses.tf:
# store_emails matches that whole domain, because SES receipt rules match either one exact address
# or an entire domain and have no way to claim a prefix. Every per-participant gift ideas address
# would therefore be swept into the same archive-and-notify path as the DMARC reports, which is not
# what should happen to them.
#
# Receive-only in practice: nothing is sent from this domain today, and invitations keep coming from
# mail.namesoutofahat.com. The outbound authentication below — DKIM, custom MAIL FROM, SPF, DMARC —
# is published anyway, so that the domain is defended against forgery now and ready to send later
# without waiting on DNS propagation. See the note above each for which of the two it is doing.
#
# The receipt rule that acts on this mail is not here. It invokes a Lambda that lives in the
# names-out-of-a-hat-api repository, so it is declared there, against the same rule set. What this
# file owns is the identity and the DNS, which is where the rest of this domain's email DNS lives.
resource "aws_ses_domain_identity" "gift_ideas" {
  domain = "ideas.namesoutofahat.com"
}

resource "aws_route53_record" "gift_ideas_verification" {
  zone_id = data.aws_route53_zone.main.zone_id
  name    = "_amazonses.ideas.namesoutofahat.com"
  type    = "TXT"
  ttl     = 600
  records = [aws_ses_domain_identity.gift_ideas.verification_token]
}

resource "aws_ses_domain_identity_verification" "gift_ideas" {
  domain     = aws_ses_domain_identity.gift_ideas.id
  depends_on = [aws_route53_record.gift_ideas_verification]
}

# What actually points mail at SES. Scoped to this subdomain, so the apex keeps having no MX of its
# own and nothing about where mail for namesoutofahat.com goes is changed by this.
resource "aws_route53_record" "gift_ideas_mx" {
  zone_id = data.aws_route53_zone.main.zone_id
  name    = "ideas.namesoutofahat.com"
  type    = "MX"
  ttl     = 600
  records = ["10 inbound-smtp.us-east-1.amazonaws.com"]
}

# DKIM keys for this domain.
#
# Nothing signs with them today, and that is worth being plain about rather than discovering later:
# DKIM signs outbound mail, and nothing is ever sent from ideas.namesoutofahat.com. Every message
# the application sends — invitations, confirmations, the forwards carrying somebody's gift ideas
# — goes out from donotreply@mail.namesoutofahat.com and is signed by the keys on that domain.
#
# They are here so that the day something does send from here, the keys are already published and
# propagated rather than being the thing that has to be waited on. What actually protects this
# domain in the meantime is the SPF and DMARC pair further down.
resource "aws_ses_domain_dkim" "gift_ideas" {
  domain = aws_ses_domain_identity.gift_ideas.domain
}

resource "aws_route53_record" "gift_ideas_dkim" {
  count   = 3
  zone_id = data.aws_route53_zone.main.zone_id
  name    = "${element(aws_ses_domain_dkim.gift_ideas.dkim_tokens, count.index)}._domainkey.ideas.namesoutofahat.com"
  type    = "CNAME"
  ttl     = 600
  records = ["${element(aws_ses_domain_dkim.gift_ideas.dkim_tokens, count.index)}.dkim.amazonses.com"]
}

# A custom MAIL FROM domain, so that SPF can align for DMARC.
#
# Without this SES uses its own default envelope domain, us-east-1.amazonses.com. SPF then passes
# — that record authorizes SES perfectly well — but it passes for the wrong domain: DMARC compares
# the envelope domain against the header From: domain, and amazonses.com is not a relaxed match for
# ideas.namesoutofahat.com. The SES console reports that as the MAIL FROM not being aligned.
#
# DMARC needs only one aligned pass and the DKIM keys above supply one, so this is not what stands
# between this domain and a DMARC failure. It is here so both legs align rather than one, which is
# what the console is asking for and what a receiver scoring the message would rather see.
#
# UseDefaultValue rather than RejectMessage: if the MX below ever stops resolving, SES falls back to
# its own envelope domain and the mail still goes out unaligned, instead of not going out at all.
resource "aws_ses_domain_mail_from" "gift_ideas" {
  domain                 = aws_ses_domain_identity.gift_ideas.domain
  mail_from_domain       = "bounce.ideas.namesoutofahat.com"
  behavior_on_mx_failure = "UseDefaultValue"
}

# Where bounces and complaints for that envelope domain are delivered back to.
resource "aws_route53_record" "gift_ideas_mail_from_mx" {
  zone_id = data.aws_route53_zone.main.zone_id
  name    = aws_ses_domain_mail_from.gift_ideas.mail_from_domain
  type    = "MX"
  ttl     = 600
  records = ["10 feedback-smtp.us-east-1.amazonses.com"]
}

# And the SPF record that makes the alignment worth having: this is the domain a receiver now
# evaluates SPF against, so it is the one that has to authorize SES.
resource "aws_route53_record" "gift_ideas_mail_from_spf" {
  zone_id = data.aws_route53_zone.main.zone_id
  name    = aws_ses_domain_mail_from.gift_ideas.mail_from_domain
  type    = "TXT"
  ttl     = 600
  records = ["v=spf1 include:amazonses.com -all"]
}

# "No host is authorized to send mail as this domain."
#
# The useful statement for a domain that only ever receives. Without it there is no SPF record here
# at all, and a receiver evaluating a forged From: at ideas.namesoutofahat.com finds nothing to
# check against. -all rather than ~all because the claim is absolute: there is no legitimate sender
# to make an exception for.
#
# It stays -all even with the MAIL FROM above, and because of it: SES now sends with an envelope
# domain of bounce.ideas.namesoutofahat.com, so this record is no longer the one SPF is evaluated
# against. It only governs mail claiming an envelope domain of ideas.namesoutofahat.com itself,
# which nothing legitimate ever will.
resource "aws_route53_record" "gift_ideas_spf" {
  zone_id = data.aws_route53_zone.main.zone_id
  name    = "ideas.namesoutofahat.com"
  type    = "TXT"
  ttl     = 600
  records = ["v=spf1 -all"]
}

# And the policy that gives the line above teeth.
#
# Without this the subdomain inherits the organizational record on namesoutofahat.com, which says
# p=quarantine — forged mail lands in a spam folder rather than being refused. Nothing legitimate
# sends from here, so there is no reason to settle for quarantine, and reject costs nothing that
# could be missed.
#
# Reports go to the same address the other two DMARC records use, so everything arrives in one
# place rather than one nobody is watching.
resource "aws_route53_record" "gift_ideas_dmarc" {
  zone_id = data.aws_route53_zone.main.zone_id
  name    = "_dmarc.ideas.namesoutofahat.com"
  type    = "TXT"
  ttl     = 600
  records = ["v=DMARC1; p=reject; rua=mailto:dmarc@mail.namesoutofahat.com"]
}

# Authorizes mail.namesoutofahat.com to receive DMARC aggregate reports about this domain.
#
# Required by RFC 7489 section 7.1, and easy to miss because leaving it out breaks nothing
# visibly: the rua above names an address on a different domain, and a reporting receiver that
# finds no authorization here simply does not send the report. Strict receivers, Google and
# Microsoft among them, do check. The symptom of getting this wrong is reports that never arrive
# and no error anywhere saying why.
#
# The record's own contents carry no policy — its existence at this name is the whole statement.
resource "aws_route53_record" "gift_ideas_dmarc_report_authorization" {
  zone_id = data.aws_route53_zone.main.zone_id
  name    = "ideas.namesoutofahat.com._report._dmarc.mail.namesoutofahat.com"
  type    = "TXT"
  ttl     = 600
  records = ["v=DMARC1"]
}

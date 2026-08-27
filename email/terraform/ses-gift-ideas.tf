# Receiving gift ideas by email.
#
# A subdomain of its own rather than mail.namesoutofahat.com, and the reason is the rule in ses.tf:
# store_emails matches that whole domain, because SES receipt rules match either one exact address
# or an entire domain and have no way to claim a prefix. Every per-participant gift ideas address
# would therefore be swept into the same archive-and-notify path as the DMARC reports, which is not
# what should happen to them.
#
# Receive-only, so no DKIM, no MAIL FROM and no SPF: those authenticate outbound mail, and nothing
# is ever sent from this domain. Invitations keep coming from mail.namesoutofahat.com.
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
# domain in the meantime is the pair of records below.
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

# "No host is authorized to send mail as this domain."
#
# The useful statement for a domain that only ever receives. Without it there is no SPF record here
# at all, and a receiver evaluating a forged From: at ideas.namesoutofahat.com finds nothing to
# check against. -all rather than ~all because the claim is absolute: there is no legitimate sender
# to make an exception for.
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

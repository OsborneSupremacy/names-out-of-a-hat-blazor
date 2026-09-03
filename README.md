# Names Out Of A Hat

An application for running a "names out of a hat" gift exchange. It's live at [namesoutofahat.com](https://namesoutofahat.com/).

![Names Out A Hat Screenshot 1](docs/demo-01.gif)

## What is a "names out of a hat" gift exchange?

For families and groups who exchange gifts around Christmas or another holiday, buying for everyone gets expensive fast. The usual fix is that everybody buys for exactly one other person, decided by a drawing:

1. Everyone writes their name on a piece of paper.
2. All the names go into a hat.
3. The hat is shaken.
4. Names are drawn one at a time, and nobody reveals the name they drew.
5. If someone draws their own name — or, in some variations, their spouse's — every name goes back in and the whole thing starts over.

|                                                                      |                                                    |                                                     |
|----------------------------------------------------------------------|----------------------------------------------------|-----------------------------------------------------|
| ![Everyone writes their name on a piece of paper](docs/IMG_8224.jpg) | ![All names are put into a hat](docs/IMG_8222.jpg) | ![Names are drawn one at a time](docs/IMG_8223.jpg) |

The original motivation for building this was simply that my family meets around Christmas Day, but we'd like to know who we're buying for by Thanksgiving — and we're rarely in the same room that early. Everything below is what the application turned out to be good for once the drawing itself stopped being the hard part.

## What this does that a hat cannot

Anything a physical hat does well, it does for free. These are the things it can't do at all.

### Nobody has to be in the same room

The draw happens wherever everyone is, and each person learns who they drew by email.

### The rules are enforced instead of hoped for

Nobody can draw their own name, or the name of anyone marked ineligible for them. With paper, an illegal draw is caught only if the person who drew it says so.

### Ineligibility is per person, and arbitrary

Exclude a spouse. Exclude whoever they drew last year. Exclude a sibling who always guesses. A hat holds one rule — the one everyone remembers to apply — and it applies to everyone equally.

### You can ask what somebody wants without revealing that you drew them

A participant can ask the person whose name they drew what they'd like, and the question arrives without a name on it. They can also ask *around* them — a spouse, a parent, anyone else in the exchange — for people who don't want to tip off the recipient at all. Replies come back by email and are forwarded on with the asker's identity still withheld. The paper equivalent requires a trusted go-between who then knows the answer.

### Gift ideas arrive by replying to an email

No account, no app, no link to click. The address a participant replies to is unique to that conversation, which is how a plain reply gets routed to the right person while staying anonymous.

### You can see whether the invitation actually arrived

Every invitation's delivery status is visible per participant — delivered, bounced, marked as spam, or nothing heard yet. When an address is wrong you can correct just that one and resend to just that person, without disturbing the draw. Hand somebody the wrong slip of paper and it's simply gone.

### The details travel with the assignment

The price range and any additional instructions are in every invitation, so nobody has to remember the number somebody said out loud in November.

### Next year is one click

A finished exchange can be copied into a new one with the same people and the same rules, and with everybody's recipient from last time excluded automatically. A hat has no memory.

### It ends with a record

When the organizer closes the exchange, everyone receives the full list of who drew whom.

## The lifecycle of an exchange

An exchange moves through a fixed sequence of states, and what's permitted depends on where it is:

| Status                 | What it means                                                                            |
|------------------------|------------------------------------------------------------------------------------------|
| `IN_PROGRESS`          | Being set up. Participants and eligibility can be edited freely.                         |
| `READY_FOR_ASSIGNMENT` | Validated. Still editable, but edits can invalidate it again.                            |
| `NAMES_ASSIGNED`       | The draw has happened. The picks exist and are hidden from everyone.                     |
| `INVITATIONS_SENT`     | Everyone has been told who they drew.                                                    |
| `READY_TO_CLOSE`       | Enough time has passed that closing is unlikely to be an accident.                       |
| `CLOSED`               | The organizer confirmed the exchange happened. Picks are revealed; nothing more changes. |

## Decisions

Most of the interesting decisions in this codebase are about what *not* to do. They're documented at the call sites too; this is the short version.

### The draw is brute force, on purpose

Most exchanges are symmetric — in mine, three couples, spouse and last year's recipient excluded, so six people with three eligible recipients each. Nobody's position is special and draw order doesn't matter.

Asymmetric ones are where it gets interesting. If exactly one person is eligible to draw Juliet, then Juliet gets picked only if that person happens to pick her, and every other outcome deadlocks. An algorithm could fix that by choosing who draws first — but that means the "randomness" is steering, which is the one property the paper version has that's worth keeping.

So the draw shuffles, assigns, and retries with a fresh seed if it deadlocks, up to a limit. It doesn't try to be clever. Failure is cheap and the retry count is a knob. If exchanges ever get complex enough that brute force stops converging, the right move is a smarter algorithm *after* n failed attempts — not instead of them.

Validation is the other half of this: rather than solving hard shapes silently, the app tells the organizer their shape is hard while they can still change it.

### Cool-off before closing

Closing reveals every pick, permanently, to everybody. It's the only irreversible action in the application, so it isn't available immediately — an exchange has to sit in `INVITATIONS_SENT` for a while before it can be closed, which is enough to stop a mis-click from spoiling everyone's surprise. The transition is made by a scheduled job, not by a timestamp check on read, so the state is a real one rather than a computed one.

### Sign-in is a magic link, and redemption sits behind a button

There are no passwords. Sign-in is a single-use link, valid for fifteen minutes, and only the hash of the token is ever stored — a dump of the table doesn't let anyone redeem pending links.

Single-use links and corporate mail security don't get along. Gateways that scan delivered mail follow links to check them, and a scanner that follows a magic link spends it before the recipient ever sees the email. Redeeming from JavaScript on page load stopped the scanners that fetch without rendering; Proofpoint renders the page in a real browser and ran that fetch itself.

The fix, verified against a live Proofpoint gateway: the token lives in the URL *fragment* (which never reaches the server) and redemption happens when a human presses a button. Scanners render the page; they don't press buttons.

The tempting alternative — allowing a token to be used twice — was rejected. A use count trades away the single-use property against an unbounded number of scanner fetches, and every one of those fetches would be issued a real session token. If this ever stops working, the answer is a typed code that never appears in a link at all.

The request endpoint also always reports success, so it can't be used to find out which addresses have accounts.

### The Ask is two endpoints for one action, for the same reason

The "ask for gift ideas" button lives in an email, so following it is a GET — and the same scanners would fire it on delivery. A GET that sent the request would mail somebody on behalf of a participant who hadn't yet read their invitation, and burn their throttle window doing it.

So the GET only renders the list of people you could ask, which a scanner is welcome to fetch as often as it likes, and the POST behind the button on that page does the work.

Asking is throttled to once a week per pair. The person being asked can't distinguish repeated asks from nagging — they don't know how many people are asking, only how often they're being asked.

### Anonymity is described honestly rather than overstated

Being asked what *you* would like reveals nothing: everyone is drawn by exactly one person, so the recipient already knew somebody held their name. Being asked what *somebody else* would like reveals that the asker drew that person — and the reader knows it wasn't them and wasn't the subject, so in a small exchange the remaining field is short.

That can't be engineered away, so the page says so before anyone chooses. The asker is the only person who knows whether the people they have in mind will bother working it out.

### Delivery tracking records delivery, not opens

Participant emails are tagged and sent through an SES configuration set; send, delivery, delay, bounce, complaint, reject and rendering-failure events flow through SNS and SQS into a per-message record the organizer can see.

Open tracking was considered and rejected. SES opens are a tracking pixel, and the signal is wrong in exactly the direction that matters: Apple Mail Privacy Protection pre-fetches images at delivery, so Apple recipients read as "opened" always; clients that block images make real readers read as nothing; and the corporate gateways that pre-render mail — the same ones behind the magic-link problem above — would fetch the pixel, meaning the people least likely to have seen their invitation are the ones most reliably reported as having opened it.

A confidently wrong "opened" is worse than no signal at all, because it stops an organizer from chasing somebody who never saw their assignment. Delivery and bounce carry no such ambiguity. This is also why an absent status is labelled "No confirmation yet" and never "not delivered".

### Inbound mail is silent until it knows who's writing

A message arriving at a gift ideas address is checked in a deliberate order. Everything that decides whether we're willing to speak to this sender at all comes first, and every one of those failures ends in silence — at that point nothing has established who wrote in, and replying to an address we can't vouch for turns the mailbox into a way of sending mail to strangers. Once a live token and a matching From address have both been seen, there's a known participant to reply to, and from there every refusal says why.

Nothing a sender writes is ever turned into a link. URLs arrive as text, so the reader sees where they actually go rather than words wrapped around an anchor. Some clients will make them clickable anyway, which is fine — what matters is that this application isn't the thing that hid the address.

### Correcting an address is its own endpoint

Editing a participant resets the exchange to `IN_PROGRESS`, which is correct before the draw and ruinous after it. But the delivery column means organizers now *find out* about bad addresses after invitations have gone out, and the only remedy used to be removing and re-adding the participant — which tears down their assignment.

So fixing an address is a separate, narrower operation that leaves the draw intact and resends automatically. The resend isn't optional: an address corrected after invitations went out is only ever corrected because somebody didn't receive theirs, and a correction that left them still not knowing would fix nothing.

### User content is moderated, and fails closed

Free-text fields go through Amazon Comprehend's toxicity detection. If the check can't be performed, the content is rejected rather than accepted.

### The wire contract lives in the repo, not in an export

`GiftExchange.Library/Schemas/*.json` is the source of truth. Terraform uploads those as API Gateway models, and `SchemaDriftTests` holds them to the records the application actually serializes. A hand-maintained OpenAPI file used to live in `docs/` and spent most of its life describing an API that no longer existed. It's gone; see [src/README.md](src/README.md) for how to export the deployed description when you want one.

### Conventions

- **Request/response records, not tuples.** Anything beyond a trivial parameter list gets an `internal record` in `GiftExchange.Library/Messaging`. A tuple has no name, nowhere to hang documentation, and every added value is a breaking signature change.
- **No LINQ query syntax.** Method syntax only, in production code and tests. Where EF joins get hairy, reach through navigation properties and let EF emit the join.
- **No foreign keys, by choice.** DSQL supports them now, and this codebase still doesn't use them. Referential integrity is the application's job — it's the layer that knows what the relationships mean and when they're allowed to change — and enforcing it in the database costs an extra read on every write that would have to check a referenced row. That latency buys back a guarantee the application is already making. So some ids deliberately have no navigation property behind them.

## How it's built

- **Frontend** — React 19 + TypeScript on Vite, served from S3 behind CloudFront, with a CloudFront function handling SPA routing.
- **API** — .NET 10 on AWS Lambda behind API Gateway. A single router Lambda dispatches on `method + resource` to a keyed handler, so one deployment artifact serves every endpoint.
- **Database** — Aurora DSQL (Postgres) via EF Core, connecting as a non-admin role with IAM auth. Migrations run as admin from their own workflow; the application role can't.
- **Ephemeral state** — DynamoDB with TTL, for magic-link tokens and throttle windows.
- **Email** — SES for sending and receiving, SQS for fan-out and for delivery events, SNS in between.
- **Async work** — EventBridge Scheduler for the cool-off transition, SQS-triggered Lambdas for invitations, delivery events and inbound gift ideas.
- **Infrastructure** — Terraform, in two independent roots. See below.

### Repository layout

| Path                             | What's in it                                                                                                                        |
|----------------------------------|-------------------------------------------------------------------------------------------------------------------------------------|
| `src/GiftExchange.Library`       | All application code: handlers, services, providers, entities, schemas.                                                             |
| `src/GiftExchange.Library.Tests` | Unit tests, including the schema drift tests.                                                                                       |
| `src/app`                        | The React frontend.                                                                                                                 |
| `iac/terraform`                  | Application infrastructure — Lambdas, API Gateway, DSQL, CloudFront. Changes most deploys.                                          |
| `email/terraform`                | SES identities and email DNS for namesoutofahat.com. Changes almost never, and its failures are silent, which is why it's separate. |
| `db`                             | SQL migrations for tables and roles.                                                                                                |
| `scripts`                        | Lambda build script.                                                                                                                |

The two Terraform roots have separate state. `iac/` reads `email/` through a remote state data source, so when a new output is added, `email/` has to be applied first.

## Running it locally

The React app:

```bash
cd src/app
npm run dev
```

Tests:

```bash
dotnet test
```

```bash
cd src/app && npm test
```

More detail, including how to export the deployed OpenAPI description, is in [src/README.md](src/README.md).

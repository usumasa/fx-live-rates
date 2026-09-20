# fx-live-rates

## むしずもう (Beetle Battle) mini game

A single-button game for toddlers: tap anywhere (or press Space/Enter) to make
your beetle push the opponent out of the ring. No reading required — just tap
and watch the beetles fight, with confetti and cheers on every win.

Open [`beetle-battle/index.html`](beetle-battle/index.html) directly in a
browser (desktop or mobile) to play. No build step or server needed.

## きょうりゅうごはん (Dino Feast) mini game

Pick a dinosaur — Tyrannosaurus, Spinosaurus, Brachiosaurus, Stegosaurus, or
Ankylosaurus — then tap the food that pops up around the screen before the
20-second timer runs out. Each tapped food flies into the dinosaur's mouth
with a chomp; the round ends with a confetti celebration showing how many
pieces you ate, then a fresh round starts automatically.

Open [`dino-feast/index.html`](dino-feast/index.html) directly in a browser
to play. No build step or server needed.

## モンスターアリーナ (Monster Arena) mini game

A small Dragon Quest Monsters / Pokémon-style game: raise, catch, synthesize
(fuse) and battle 15 original fantasy monsters, then take a 3-monster party
online to battle a friend in real time — no account or Claude login required
on either side.

- **Catch & grow**: pick a starter, fight wild monsters to level up and catch
  new ones.
- **Synthesize**: fuse two single-family monsters (🔥ほのお／💧みず／🌿くさ／🐾けもの／😈あくま)
  into one of 10 hybrid species.
- **Battle**: Pokémon-style 1v1 with a 3-monster party — attack, use a limited-use
  skill move, or switch — with a simple type triangle (fire beats grass beats
  water beats fire).
- **Online battle**: create a room and share its 4-letter code with a friend,
  or join theirs, to battle live. Both sides only ever exchange move choices;
  each browser independently computes identical results from a shared random
  seed, so there's no server-side game logic to trust — just a place to pass
  messages back and forth.

Open [`monster-arena/index.html`](monster-arena/index.html) to play. Everything
except online battles works immediately with no setup. Online battles need a
**free Firebase Realtime Database** project (a few minutes, one time):

1. Go to the [Firebase console](https://console.firebase.google.com/) → **Add
   project** → give it any name → Google Analytics can be disabled → Create.
2. In the left menu, **Build → Realtime Database** → **Create Database** →
   keep the default location → start in **test mode** (rules are tightened in
   step 4).
3. Click the gear icon → **Project settings** → scroll to **Your apps** → click
   the `</>` (web) icon → give it a nickname → register (skip Firebase
   Hosting) → copy the `firebaseConfig` object shown.
4. Paste those values into `FIREBASE_CONFIG` near the top of the `<script>` in
   `monster-arena/index.html` (search for `YOUR_API_KEY`).
5. In the Realtime Database's **Rules** tab, replace the rules with the
   following so only the `rooms` (battle rooms) and `saves` (cloud save)
   paths used by the game are readable/writable, without requiring anyone
   to sign in:
   ```json
   {
     "rules": {
       "rooms": {
         ".read": true,
         ".write": true
       },
       "saves": {
         ".read": true,
         ".write": true
       }
     }
   }
   ```

That's it — anyone with the page open can then create or join a battle room
by code, with no sign-in on either side.

- **Cloud save**: from the home screen, **☁️ クラウドセーブ** shows a
  6-letter code for the current device. Tap **ほぞんする** to upload your
  box/party to that code, or enter someone else's (or your own, from
  another device) code and tap **ふっかつする** to restore it — this
  overwrites local data, so it asks for confirmation first.
- **Surviving private browsing on iPhone**: on iOS, tapping a link or a
  plain bookmark reopens it in whatever Safari mode (including Private
  Browsing) was active when it was saved, which uses Safari's ephemeral
  private-mode storage and loses local save data as soon as the tab
  closes. **Add the page to the Home Screen instead** (Share → Add to
  Home Screen) — the page declares `apple-mobile-web-app-capable`, so the
  resulting icon launches as its own standalone app with a persistent
  storage container, entirely independent of whatever Safari mode it was
  added from. Opening the game from that icon makes the normal auto-save
  behave exactly like it does on desktop — no manual steps needed.
- **Cross-device / no-Home-Screen fallback**: if you can't add a Home
  Screen icon (or want the same save on a second device), the first time
  you tap **ほぞんする** on the ☁️ クラウドセーブ screen (or restore from
  a code), the page's own URL gets a `?code=XXXXXX` appended and every
  save from then on is silently pushed to that code too. Bookmark the
  page at that point — reopening that URL (even in a fresh private tab)
  reads the code back out and restores your data automatically.
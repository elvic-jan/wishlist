namespace Wishlist.Application.Localization;

/// <summary>
/// Dutch for every text the server shows to people: error messages, emails,
/// lasting-toast messages and the invite link preview. The key is the English
/// text exactly as written in the code (see <see cref="Texts"/>); a test fails
/// when a user-facing text has no entry here, or an entry matches nothing.
///
/// Wording: informal ("je"). Glossary: wishlist = verlanglijst, wish = wens,
/// circle = kring, buddy = buddy, gift = cadeau, reserve = reserveren,
/// passkey = passkey, sign in = aanmelden, invite = uitnodigen.
/// </summary>
public static class DutchTexts
{
    public static readonly IReadOnlyDictionary<string, string> Entries = new Dictionary<string, string>
    {
        // ── Errors ──────────────────────────────────────────────────────────
        ["An unexpected error occurred."] = "Er ging iets onverwachts mis.",
        ["A passkey credential is required."] = "Er is een passkey nodig.",
        ["An account with this email already exists."] = "Er bestaat al een account met dit e-mailadres.",
        ["Circle name cannot be empty."] = "Geef de kring een naam.",
        ["Give the passkey a name."] = "Geef de passkey een naam.",
        ["Invalid credentials."] = "Het e-mailadres of wachtwoord klopt niet.",
        ["Invalid or expired refresh token."] = "Je sessie is verlopen. Meld je opnieuw aan.",
        ["Invalid registration data."] = "De gegevens voor je account kloppen niet.",
        ["Invitation not found."] = "Uitnodiging niet gevonden.",
        ["Item title cannot be empty."] = "Geef de wens een titel.",
        ["Message not found."] = "Bericht niet gevonden.",
        ["Name cannot be empty."] = "Vul een naam in.",
        ["No active claim on this item."] = "Dit cadeau is niet gereserveerd.",
        ["Only a reservation can be kept; this gift is already bought."] =
            "Alleen een reservering kan je behouden; dit cadeau is al gekocht.",
        ["Only the claimer can mark a gift as bought."] =
            "Alleen wie het cadeau reserveerde, kan het als gekocht aanduiden.",
        ["Passkey not found."] = "Passkey niet gevonden.",
        ["Registration is by invitation only."] = "Je kan alleen een account maken met een uitnodiging.",
        ["This invitation has expired."] = "Deze uitnodiging is verlopen.",
        ["This is your only way to sign in. Add another passkey first, then remove this one."] =
            "Dit is je enige manier om aan te melden. Voeg eerst een andere passkey toe en verwijder dan deze.",
        ["This item has already been claimed."] = "Dit cadeau is al gereserveerd.",
        ["This reservation has run out. Reserve the gift again if it's still free."] =
            "Deze reservering is afgelopen. Reserveer het cadeau opnieuw als het nog vrij is.",
        ["Title cannot be empty."] = "Vul een titel in.",
        ["Transfer ownership or remove all members before leaving as owner."] =
            "Als eigenaar kan je de kring pas verlaten als je ze overdraagt of alle leden verwijdert.",
        ["URL must be an absolute http or https URL."] = "Een link moet beginnen met http:// of https://.",
        ["Use your own wishlist to change your own wishes."] =
            "Pas je eigen wensen aan op je eigen verlanglijst.",
        ["User not found."] = "Gebruiker niet gevonden.",
        ["Wishlist item not found."] = "Wens niet gevonden.",
        ["You are not a buddy for this wishlist."] = "Je bent geen buddy voor deze verlanglijst.",
        ["You are not a member of this circle."] = "Je bent geen lid van deze kring.",
        ["You can only delete your own wishlist items."] = "Je kan alleen je eigen wensen verwijderen.",
        ["You can only edit your own wishlist items."] = "Je kan alleen je eigen wensen aanpassen.",
        ["You can only invite people from your circles."] = "Je kan alleen mensen uit je kringen uitnodigen.",
        ["You can only keep your own reservation."] = "Je kan alleen je eigen reservering behouden.",
        ["You can only pick someone you share a circle with."] =
            "Je kan alleen iemand kiezen met wie je een kring deelt.",
        ["You can only release your own claim."] = "Je kan alleen je eigen reservering vrijgeven.",
        ["You can't be your own buddy."] = "Je kan niet je eigen buddy zijn.",
        ["You cannot claim an item from your own wishlist."] =
            "Je kan geen cadeau van je eigen verlanglijst reserveren.",
        ["You do not share a circle with this user."] = "Je deelt geen kring met deze persoon.",
        ["You must be a member of this circle to generate an invitation."] =
            "Je moet lid zijn van deze kring om iemand uit te nodigen.",
        ["You've already imported your wishes from the old site."] =
            "Je hebt je wensen van de oude site al geïmporteerd.",
        ["Your details changed while the passkey was being created. Please try again."] =
            "Je gegevens zijn gewijzigd terwijl de passkey werd gemaakt. Probeer het opnieuw.",
        ["A wishlist item may have at most {0} URLs."] = "Een wens kan hoogstens {0} links hebben.",
        ["Pick between 1 and {0} people."] = "Kies tussen 1 en {0} mensen.",
        ["{0} can be at most {1} characters."] = "{0} mag hoogstens {1} tekens lang zijn.",
        ["This language is not available."] = "Deze taal is niet beschikbaar.",
        ["The passkey request expired. Please try again."] =
            "Het passkey-verzoek is verlopen. Probeer het opnieuw.",
        ["A valid email address is required."] = "Vul een geldig e-mailadres in.",
        ["Too many password-reset requests. Please try again later."] =
            "Te veel aanvragen om je wachtwoord te herstellen. Probeer het later opnieuw.",
        ["Invalid or expired token."] = "Deze link is ongeldig of verlopen.",
        ["Too many invitation attempts. Please try again later."] =
            "Te veel pogingen met een uitnodiging. Probeer het later opnieuw.",
        ["This confirmation link is invalid or has expired."] = "Deze bevestigingslink is ongeldig of verlopen.",

        // What a too-long value is called in "{0} can be at most {1} characters."
        ["Name"] = "De naam",
        ["Email address"] = "Het e-mailadres",
        ["Circle name"] = "De naam van de kring",
        ["Password"] = "Het wachtwoord",
        ["Title"] = "De titel",
        ["Description"] = "De beschrijving",
        ["A link"] = "Een link",

        // ── Invitations ─────────────────────────────────────────────────────
        ["Someone"] = "Iemand",
        ["a circle"] = "een kring",
        ["{0} invited you to {1} on Kadoozle"] = "{0} nodigt je uit voor {1} op Kadoozle",
        ["Join the circle to see each other's wishlists and coordinate gifts."] =
            "Word lid van de kring om elkaars verlanglijsten te zien en cadeaus af te stemmen.",

        // ── Emails ──────────────────────────────────────────────────────────
        ["Hi {0},"] = "Hallo {0},",
        ["Thanks,"] = "Groeten,",

        ["Confirm your Kadoozle email"] = "Bevestig je e-mailadres voor Kadoozle",
        ["Welcome to Kadoozle! Please confirm this is your email address, so you can " +
         "get back into your account if you ever lose your phone or forget how you signed in."] =
            "Welkom bij Kadoozle! Bevestig dat dit jouw e-mailadres is. Zo raak je weer in je account " +
            "als je ooit je telefoon kwijt bent of niet meer weet hoe je je aanmeldde.",
        ["Confirm my email"] = "Mijn e-mailadres bevestigen",
        ["The link works for 3 days. If you didn't create a Kadoozle account, you can ignore this email."] =
            "De link werkt 3 dagen. Heb je geen Kadoozle-account gemaakt? Dan mag je deze e-mail negeren.",

        ["A passkey was added to your Kadoozle account"] = "Er is een passkey toegevoegd aan je Kadoozle-account",
        ["A new passkey was just added to your Kadoozle account (at **{0}**). " +
         "It lets you sign in with Face ID, a fingerprint or your device PIN."] =
            "Er is net een nieuwe passkey toegevoegd aan je Kadoozle-account (om **{0}**). " +
            "Daarmee meld je je aan met Face ID, een vingerafdruk of de pincode van je toestel.",
        ["A new passkey (\"{0}\") was just added to your Kadoozle account (at **{1}**). " +
         "It lets you sign in with Face ID, a fingerprint or your device PIN."] =
            "Er is net een nieuwe passkey (\"{0}\") toegevoegd aan je Kadoozle-account (om **{1}**). " +
            "Daarmee meld je je aan met Face ID, een vingerafdruk of de pincode van je toestel.",
        ["If this was you, there's nothing to do."] = "Was jij dit? Dan hoef je niets te doen.",
        ["If it wasn't you, open your sign-in settings, remove the passkey and sign out other devices"] =
            "Was jij dit niet? Open dan je aanmeldinstellingen, verwijder de passkey en meld andere toestellen af",

        ["Get back into your Kadoozle account"] = "Opnieuw toegang tot je Kadoozle-account",
        ["We received a request to get back into your Kadoozle account. " +
         "If you did not make this request, you can safely ignore this email."] =
            "We kregen een vraag om opnieuw toegang te krijgen tot je Kadoozle-account. " +
            "Kwam die vraag niet van jou? Dan mag je deze e-mail gerust negeren.",
        ["Open the link below within the next 30 minutes, on the phone or computer you want to use. " +
         "You can then sign in with Face ID or a fingerprint on that device, or set a new password:"] =
            "Open de link hieronder binnen de 30 minuten, op de telefoon of computer die je wil gebruiken. " +
            "Daarna kan je je op dat toestel aanmelden met Face ID of een vingerafdruk, of een nieuw wachtwoord kiezen:",

        ["Your Kadoozle password was changed"] = "Je Kadoozle-wachtwoord is gewijzigd",
        ["Your Kadoozle account password was just changed (at **{0}**)."] =
            "Het wachtwoord van je Kadoozle-account is net gewijzigd (om **{0}**).",
        ["If you made this change, no action is needed."] = "Heb je dit zelf gedaan? Dan hoef je niets te doen.",
        ["If you did **not** make this change, request another password reset from the " +
         "sign-in page right away, and contact support if you cannot regain access."] =
            "Heb je dit **niet** zelf gedaan? Vraag dan meteen een nieuw wachtwoord aan op de " +
            "aanmeldpagina, en neem contact op als je niet meer in je account raakt.",

        ["{0} made you a buddy for their wishlist"] = "{0} maakte je buddy voor zijn of haar verlanglijst",
        ["{0} made you a **buddy** for their wishlist on Kadoozle. " +
         "You can now add, change and remove wishes on their list, for example when you know what they'd like."] =
            "{0} maakte je **buddy** voor zijn of haar verlanglijst op Kadoozle. " +
            "Je kan nu wensen toevoegen, aanpassen en verwijderen op die lijst, bijvoorbeeld als je weet wat {0} graag wil.",
        ["Open {0}'s wishlist"] = "Open de verlanglijst van {0}",

        ["Your reservation of \"{0}\" ends on {1}"] = "Je reservering van \"{0}\" loopt af op {1}",
        ["A while ago you reserved **{0}** on {1}'s wishlist. " +
         "The reservation is still open and ends on **{2}**. After that, others can reserve it again."] =
            "Een tijd geleden reserveerde je **{0}** op de verlanglijst van {1}. " +
            "De reservering staat nog open en loopt af op **{2}**. Daarna kunnen anderen het opnieuw reserveren.",
        ["Open the wishlist to keep your reservation, mark the gift as bought, or release it."] =
            "Open de verlanglijst om je reservering te behouden, het cadeau als gekocht aan te duiden of het vrij te geven.",
        ["Your reservation of \"{0}\" has ended"] = "Je reservering van \"{0}\" is afgelopen",
        ["Your reservation of **{0}** on {1}'s wishlist has ended, so others in the circle can reserve it now."] =
            "Je reservering van **{0}** op de verlanglijst van {1} is afgelopen. Anderen in de kring kunnen het nu reserveren.",
        ["If you still want to give it, open the wishlist and reserve it again while it's free."] =
            "Wil je het nog altijd geven? Open dan de verlanglijst en reserveer het opnieuw zolang het vrij is.",

        ["{0} invited you to join {1}"] = "{0} nodigt je uit voor {1}",
        ["{0} invited you to join the circle **{1}** on Kadoozle."] =
            "{0} nodigt je uit voor de kring **{1}** op Kadoozle.",
        ["Open Kadoozle to join or decline"] = "Open Kadoozle om lid te worden of te weigeren",

        // ── Lasting-toast messages ──────────────────────────────────────────
        ["Bring your wishes from the old site"] = "Breng je wensen mee van de oude site",
        ["Had a wishlist on verlanglijstje? Import your wishes into Kadoozle in a few taps, so you don't have to type them again."] =
            "Had je een lijst op verlanglijstje? Importeer je wensen in een paar tikken in Kadoozle, dan hoef je ze niet opnieuw te typen.",
        ["Import my wishes"] = "Mijn wensen importeren",
        ["GiftCircles is now Kadoozle"] = "GiftCircles heet nu Kadoozle",
        ["Same app, new name and address: kadoozle.app. Passkeys from the old address no longer work, so add a new one. " +
         "If you had the app on your home screen, remove it and add Kadoozle again."] =
            "Dezelfde app, met een nieuwe naam en een nieuw adres: kadoozle.app. Passkeys van het oude adres werken niet meer, voeg dus een nieuwe toe. " +
            "Stond de app op je startscherm? Verwijder ze dan en voeg Kadoozle opnieuw toe.",
        ["Add a passkey"] = "Een passkey toevoegen",
        ["New: wishlist buddies"] = "Nieuw: buddy's voor je verlanglijst",
        ["Let someone you trust keep your wishlist up to date. Pick your buddies on My wishlist: " +
         "they can add, change and remove wishes for you. Handy for kids, grandparents, or a partner who knows what you like."] =
            "Laat iemand die je vertrouwt je verlanglijst bijhouden. Kies je buddy's op Mijn verlanglijst: " +
            "zij kunnen wensen voor jou toevoegen, aanpassen en verwijderen. Handig voor kinderen, grootouders of een partner die weet wat je graag hebt.",
        ["Pick my buddies"] = "Mijn buddy's kiezen",
        ["What's new"] = "Wat is er nieuw",
        ["Invite people straight to WhatsApp or by email with \"Invite people\" on a circle. " +
         "And if you reserve a gift, we'll remind you before the reservation runs out."] =
            "Nodig mensen meteen uit via WhatsApp of e-mail met \"Mensen uitnodigen\" bij een kring. " +
            "En als je een cadeau reserveert, herinneren we je eraan voor de reservering afloopt.",
    };
}

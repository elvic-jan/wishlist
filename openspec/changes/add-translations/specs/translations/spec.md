## ADDED Requirements

### Requirement: The app is available in English and Dutch
Every text the app produces SHALL be available in English and Dutch: pages, dialogs, menus, toasts, validation and error messages (including those returned by the server), lasting-toast messages, the invite share text and emails. Text entered by people (wishes, descriptions, display names, circle names) SHALL be shown as entered and never translated. Dates and numbers SHALL be formatted for the active language. The page's `lang` attribute SHALL equal the active language. A text missing in the active language SHALL fall back to English, and automated checks SHALL fail when a text has no Dutch translation.

#### Scenario: Using the app in Dutch
- **WHEN** a person whose language is Dutch opens any page
- **THEN** all app texts on it are Dutch, dates read like "14 november 2026", and the page's `lang` is `nl`

#### Scenario: Server error in the person's language
- **WHEN** a person whose language is Dutch submits a wish with a title longer than allowed
- **THEN** the error shown is in Dutch

#### Scenario: User content is untouched
- **WHEN** a Dutch-language person views a wish titled "Board game" in a circle named "Family"
- **THEN** the title and circle name are shown exactly as entered

#### Scenario: Missing translation is caught before release
- **WHEN** a change adds an app text without a Dutch translation
- **THEN** the build or the tests fail

### Requirement: The language follows the browser until the person chooses
On a device without a stored choice and without a signed-in account that has a saved language, the app SHALL use the first language in the browser's preferred languages that it supports, and English when none is supported.

#### Scenario: Dutch browser, first visit
- **WHEN** someone with a browser set to Dutch (for example `nl-BE`) opens the sign-in page for the first time
- **THEN** the page is in Dutch

#### Scenario: Unsupported browser language
- **WHEN** someone with a browser set to French opens the app for the first time
- **THEN** the app is in English

### Requirement: People can switch language, and the choice is remembered
The app SHALL offer a language switch in the account menu and on the signed-out pages (sign in, register, forgot password, recover, join). Switching SHALL take effect immediately without reloading, SHALL be remembered on the device, and for a signed-in person SHALL be saved on their account. When a person signs in, an account with a saved language SHALL determine the app's language on that device; an account without one SHALL be given the language the device is currently showing. A new account SHALL be created with the language shown during registration.

#### Scenario: Switching while signed in
- **WHEN** a signed-in person chooses Nederlands in the account menu
- **THEN** the app turns Dutch at once and their account's language is Dutch

#### Scenario: Choice follows the account to another device
- **WHEN** a person whose account language is Dutch signs in on a device whose browser is English
- **THEN** the app is in Dutch after signing in

#### Scenario: Switching before signing in
- **WHEN** a visitor chooses Nederlands on the sign-in page and reloads
- **THEN** the page is still in Dutch

#### Scenario: Existing account without a language
- **WHEN** a person whose account has no saved language signs in on a device showing Dutch
- **THEN** the app stays Dutch and their account's language becomes Dutch

#### Scenario: Registering in Dutch
- **WHEN** someone registers while the app shows Dutch
- **THEN** their account's language is Dutch and their confirmation email is in Dutch

#### Scenario: Unsupported language is refused
- **WHEN** a request tries to save a language the app does not offer
- **THEN** the request is rejected and the account's language is unchanged

### Requirement: Emails use the recipient's language
An email SHALL be written (subject and body, including dates) in the recipient's saved language. When the recipient has none, it SHALL use the language of the person whose action caused the email; when there is no such person or they have none, English. For emails a person triggers for themself (confirm email, password reset or recovery, password changed, passkey added) without a saved language, the language of the request SHALL be used.

#### Scenario: Recipient has a language
- **WHEN** an English-language person makes a Dutch-language person their buddy
- **THEN** the buddy's email is in Dutch

#### Scenario: Recipient without a language, sender has one
- **WHEN** a Dutch-language person invites a circle-mate whose account has no saved language to another circle
- **THEN** the invitation email is in Dutch

#### Scenario: No sender
- **WHEN** the monthly run sends a reservation reminder to a person without a saved language
- **THEN** the email is in English

#### Scenario: Password reset from a Dutch page
- **WHEN** someone without a saved language asks for a reset link from the forgot-password page shown in Dutch
- **THEN** the email is in Dutch

### Requirement: Invite link previews use the inviter's language
The link preview (title and description) of an invite link SHALL be in the saved language of the person who created the invite, and English when they have none.

#### Scenario: Dutch inviter shares a link
- **WHEN** a Dutch-language person shares an invite link in a chat app
- **THEN** the preview's title and description are in Dutch

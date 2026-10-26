## ADDED Requirements

### Requirement: Monthly reservation job
A scheduled job SHALL run on the 1st of every month at 07:00 UTC, in the same Container Apps environment and from the same image as the app, to send reservation reminders, end expired reservations and email their reservers. It SHALL use the app's database and email settings, need no public endpoint, and be updated to the new image on every deploy.

#### Scenario: Runs monthly
- **WHEN** it is 07:00 UTC on the 1st of a month
- **THEN** the job starts, processes reservations, logs how many reminders and expiries it handled, and exits

#### Scenario: Same code as the app
- **WHEN** a new version is deployed
- **THEN** the job's image is updated to the same image as the app

#### Scenario: Runs without the app being awake
- **WHEN** the app is scaled to zero at the scheduled time
- **THEN** the job still runs, without starting the web app

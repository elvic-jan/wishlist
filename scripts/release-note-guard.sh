#!/usr/bin/env bash
# Shows a release note only with the release it was written for.
#
# The note lives in src/Wishlist.Web/index.html and stays there until someone
# changes it, so a release that doesn't change it (a Dependabot update, say)
# would announce the previous release's note again. Before the image is built,
# this compares the note with the one on the live site. When they're the same,
# it renames the tag to release-note-previous: the app ignores that and shows
# its generic text, and the next run still sees which note was last written.
#
# Usage: scripts/release-note-guard.sh <live site URL>
set -euo pipefail

live_url="${1%/}"
page="src/Wishlist.Web/index.html"

note_in() {
  # The content of <meta name="release-note"> or <meta name="release-note-previous">.
  sed -n -E 's/.*<meta name="release-note(-previous)?" content="([^"]*)".*/\2/p' | head -n 1
}

ours=$(note_in < "$page")
if [ -z "$ours" ]; then
  echo "No release note in $page; the app shows its generic text."
  exit 0
fi

if ! live_html=$(curl -fsS --max-time 20 "$live_url/index.html"); then
  echo "::warning::Couldn't read the live site; keeping the release note \"$ours\"."
  exit 0
fi
live=$(printf '%s' "$live_html" | note_in)

if [ "$ours" = "$live" ]; then
  sed -i -E 's/<meta name="release-note" content=/<meta name="release-note-previous" content=/' "$page"
  echo "::notice::Release note unchanged since the live version (\"$ours\"); this release shows the generic text."
else
  echo "::notice::Release note for this release: \"$ours\" (live: \"${live:-none}\")."
fi

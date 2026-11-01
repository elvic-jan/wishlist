#!/usr/bin/env node
// Works out the version of the commit being deployed, and the release history
// for the What's new page, from git tags and the files in release-notes/.
//
// - Each production release is tagged vX.Y.Z (by the deploy, after it succeeds).
// - A release's notes are the release-notes/*.md files that exist at its tag
//   but not at the tag before. So the files never move, and nothing is
//   committed back to main.
// - The next version bumps the last tag by the highest type among the new
//   files: major > feature (minor) > fix (patch). No files: patch.
//   No tag at all: 1.0.0.
//
// Usage: node scripts/release-version.mjs [--json <releases.json>] [--notes <notes.md>]
// Prints the version. With GITHUB_OUTPUT set, also writes version=… there.
// Needs the full history and tags (actions/checkout with fetch-depth: 0).

import { execFileSync } from 'node:child_process'
import { appendFileSync, mkdirSync, writeFileSync } from 'node:fs'
import { dirname } from 'node:path'

const DIR = 'release-notes'
const TYPES = ['fix', 'feature', 'major']

const git = (...args) => execFileSync('git', args, { encoding: 'utf8' }).trim()
const lines = text => text.split('\n').map(l => l.trim()).filter(Boolean)

const parse = v => v.slice(1).split('.').map(Number)
const compare = (a, b) => {
  const [x, y] = [parse(a), parse(b)]
  return x[0] - y[0] || x[1] - y[1] || x[2] - y[2]
}

/** The note files at a commit, read from git (not the working tree). */
function notesAt(ref) {
  if (!ref || !hasNotesDir(ref)) return new Map()
  const files = lines(git('ls-tree', '--name-only', `${ref}:${DIR}`))
    .filter(f => f.endsWith('.md') && f !== 'README.md')
  const notes = new Map()
  for (const file of files) {
    const text = git('show', `${ref}:${DIR}/${file}`).replace(/\r\n/g, '\n')
    const m = text.match(/^---\n\s*type:\s*(feature|fix|major)\s*\n---\n([\s\S]*)$/)
    if (!m) throw new Error(`${DIR}/${file} at ${ref}: malformed (see ${DIR}/README.md)`)
    notes.set(file, { type: m[1], lines: lines(m[2]) })
  }
  return notes
}

function hasNotesDir(ref) {
  try {
    execFileSync('git', ['rev-parse', '--verify', '--quiet', `${ref}:${DIR}`], { stdio: 'ignore' })
    return true
  } catch {
    return false
  }
}

/** Notes at `ref` that weren't there at `previous`, highest type first. */
function newNotes(ref, previous) {
  const before = notesAt(previous)
  const after = notesAt(ref)
  return [...after]
    .filter(([file]) => !before.has(file))
    .sort(([fa, a], [fb, b]) => TYPES.indexOf(b.type) - TYPES.indexOf(a.type) || fa.localeCompare(fb))
    .map(([, note]) => note)
}

function bump(version, notes) {
  const [major, minor, patch] = parse(version)
  const highest = Math.max(0, ...notes.map(n => TYPES.indexOf(n.type)))
  if (highest === 2) return `v${major + 1}.0.0`
  if (highest === 1) return `v${major}.${minor + 1}.0`
  return `v${major}.${minor}.${patch + 1}`
}

const tags = lines(git('tag', '--list', 'v*', '--merged', 'HEAD'))
  .filter(t => /^v\d+\.\d+\.\d+$/.test(t))
  .sort(compare)
const head = git('rev-parse', 'HEAD')
const today = new Date().toISOString().slice(0, 10)

const releases = []
let previous = null
for (const tag of tags) {
  releases.push({
    version: tag.slice(1),
    date: git('for-each-ref', '--format=%(creatordate:short)', `refs/tags/${tag}`),
    notes: newNotes(tag, previous).flatMap(n => n.lines),
  })
  previous = tag
}

// Deploying a commit that is already a release (a re-run): that's the version.
const tagged = tags.find(t => git('rev-list', '-n', '1', t) === head)
let version
if (tagged) {
  version = tagged.slice(1)
} else {
  const notes = newNotes('HEAD', previous)
  version = (previous ? bump(previous, notes) : 'v1.0.0').slice(1)
  releases.push({ version, date: today, notes: notes.flatMap(n => n.lines) })
}
releases.reverse()

const args = process.argv.slice(2)
const option = name => {
  const i = args.indexOf(name)
  return i >= 0 ? args[i + 1] : undefined
}

const json = option('--json')
if (json) {
  mkdirSync(dirname(json), { recursive: true })
  writeFileSync(json, JSON.stringify({ releases }, null, 2) + '\n')
}

const notesFile = option('--notes')
if (notesFile) {
  const current = releases.find(r => r.version === version)
  const body = current.notes.length ? current.notes.map(n => `- ${n}`).join('\n') : '- Small fixes and improvements'
  writeFileSync(notesFile, body + '\n')
}

if (process.env.GITHUB_OUTPUT) appendFileSync(process.env.GITHUB_OUTPUT, `version=${version}\n`)
console.log(version)

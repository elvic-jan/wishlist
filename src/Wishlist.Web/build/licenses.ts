import fs from 'node:fs'
import path from 'node:path'
import type { Plugin } from 'vite'

export interface LicensedPackage {
  name: string
  version: string
  license: string
  homepage?: string
  text: string
}

const LICENSE_FILE = /^(licen[cs]e|copying)(\.|-|$)/i

// For MIT packages that ship no licence file of their own.
const MIT_TEXT = `Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in all
copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN THE
SOFTWARE.`

/** The package folder and name for a module inside node_modules, or null. */
function packageOf(id: string): { root: string; name: string } | null {
  const file = id.replace(/\\/g, '/').split('?')[0]
  const at = file.lastIndexOf('/node_modules/')
  if (at < 0) return null
  const parts = file.slice(at + '/node_modules/'.length).split('/')
  const name = parts[0].startsWith('@') ? `${parts[0]}/${parts[1]}` : parts[0]
  return { root: `${file.slice(0, at)}/node_modules/${name}`, name }
}

function licenseName(field: unknown): string | null {
  if (typeof field === 'string') return field
  if (field && typeof field === 'object' && 'type' in field && typeof field.type === 'string') return field.type
  return null
}

/** A link for the page, only when it is an http(s) URL. */
function homepageOf(pkg: Record<string, unknown>): string | undefined {
  const url = repositoryOrHomepage(pkg)
  return url && /^https?:\/\//.test(url) ? url : undefined
}

function repositoryOrHomepage(pkg: Record<string, unknown>): string | undefined {
  if (typeof pkg.homepage === 'string') return pkg.homepage
  const repo = pkg.repository
  const url = typeof repo === 'string' ? repo : repo && typeof repo === 'object' && 'url' in repo ? repo.url : undefined
  if (typeof url !== 'string') return undefined
  // "owner/repo" is npm's shorthand for a GitHub repository.
  if (/^[\w.-]+\/[\w.-]+$/.test(url)) return `https://github.com/${url}`
  return url.replace(/^git\+/, '').replace(/^git:\/\//, 'https://').replace(/\.git$/, '')
}

function authorOf(pkg: Record<string, unknown>): string {
  const author = pkg.author
  if (typeof author === 'string') return author.replace(/\s*[<(].*$/, '')
  if (author && typeof author === 'object' && 'name' in author && typeof author.name === 'string') return author.name
  return `the ${String(pkg.name)} authors`
}

/** The licence text: the package's own file, or the standard MIT text when it has none. */
function licenseText(root: string, pkg: Record<string, unknown>, file: string | undefined, license: string | null): string {
  if (file) return fs.readFileSync(path.join(root, file), 'utf8').trim()
  if (license === 'MIT') return `MIT License\n\nCopyright (c) ${authorOf(pkg)}\n\n${MIT_TEXT}`
  return `${String(pkg.name)} is licensed under ${license}. The package ships no separate licence file.`
}

/**
 * Writes /licenses.json: every npm package in the browser bundle with its
 * licence text, for the Open-source licences page. Fails the build when a
 * bundled package has no licence. `alsoInclude` names packages that reach the
 * bundle another way (Tailwind's generated CSS).
 */
export function licenses(alsoInclude: string[] = []): Plugin {
  return {
    name: 'kadoozle-licenses',
    apply: 'build',
    generateBundle(_options, bundle) {
      const roots = new Map<string, string>()
      for (const name of alsoInclude) roots.set(path.resolve('node_modules', name).replace(/\\/g, '/'), name)
      for (const output of Object.values(bundle)) {
        if (output.type !== 'chunk') continue
        for (const id of Object.keys(output.modules)) {
          const found = packageOf(id)
          if (found) roots.set(found.root, found.name)
        }
      }

      const packages = new Map<string, LicensedPackage>()
      for (const [root, name] of roots) {
        const pkg = JSON.parse(fs.readFileSync(path.join(root, 'package.json'), 'utf8')) as Record<string, unknown>
        const version = String(pkg.version ?? '')
        const key = `${name}@${version}`
        if (packages.has(key)) continue

        const file = fs.readdirSync(root).find(f => LICENSE_FILE.test(f))
        const license = licenseName(pkg.license)
        if (!license && !file) this.error(`No licence found for bundled package ${key}`)

        packages.set(key, {
          name,
          version,
          license: license ?? 'See licence text',
          homepage: homepageOf(pkg),
          text: licenseText(root, pkg, file, license),
        })
      }

      const list = [...packages.values()].sort((a, b) => a.name.localeCompare(b.name))
      this.emitFile({
        type: 'asset',
        fileName: 'licenses.json',
        source: JSON.stringify({ packages: list }),
      })
    },
  }
}

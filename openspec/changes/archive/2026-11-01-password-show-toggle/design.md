## Context

The three auth pages render `<Input type="password">` (shadcn input). Edge adds its own reveal button through the `::-ms-reveal` pseudo-element; other browsers have none.

## Decisions

### 1. One `PasswordInput` component

`components/ui/password-input.tsx` wraps `Input` and keeps a `visible` state. It renders `type={visible ? 'text' : 'password'}` and an icon button (lucide `Eye` / `EyeOff`) placed inside the right edge of the field; the input gets right padding so text never runs under it. It takes the same props as `Input`, so pages only swap the component name. Each field has its own state, so on Recover each field toggles alone.

### 2. Button behaviour

- `type="button"`, so it never submits the form.
- `aria-label` "Show password" / "Hide password" and `aria-pressed`, so screen readers announce it.
- `tabIndex={-1}` is not used: keyboard users can reach it with Tab after the field.
- `onMouseDown` prevents default, so clicking it keeps focus (and the mobile keyboard) in the field.
- Hidden is the default on every page load; nothing is remembered.

### 3. Hide Edge's button with CSS

`input::-ms-reveal { display: none; }` in `index.css`. Other browsers ignore it.

Alternative: keep Edge's own button and only add ours elsewhere. Rejected: browser sniffing for a cosmetic difference; one consistent button is simpler.

## Risks / Trade-offs

- [A shown password is visible to someone looking over the shoulder] → It is hidden by default and only shown on a deliberate click.
- [Password managers] → The input keeps its `id`, `name`, `autocomplete` and stays the same element when the type changes, so autofill keeps working.

import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import client from '../api/client';

interface WishlistItem {
  id: string;
  title: string;
  description?: string;
  urls: string[];
  createdAt: string;
}

const MAX_URLS = 10;

interface FormState {
  title: string;
  description: string;
  urls: string[];
}

const emptyForm: FormState = { title: '', description: '', urls: [] };

function isValidHttpUrl(value: string): boolean {
  const trimmed = value.trim();
  if (!trimmed) return false;
  try {
    const parsed = new URL(trimmed);
    return parsed.protocol === 'http:' || parsed.protocol === 'https:';
  } catch {
    return false;
  }
}

function hostFor(rawUrl: string): string {
  try {
    const parsed = new URL(rawUrl);
    return parsed.host.replace(/^www\./, '');
  } catch {
    return rawUrl;
  }
}

export function MyWishlistPage() {
  const [items, setItems] = useState<WishlistItem[]>([]);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState<FormState>(emptyForm);
  const [adding, setAdding] = useState(false);
  const [error, setError] = useState('');

  const load = () => client.get('/wishlists/me').then(r => setItems(r.data));

  useEffect(() => { load(); }, []);

  const setUrl = (idx: number, value: string) =>
    setForm(f => ({ ...f, urls: f.urls.map((u, i) => (i === idx ? value : u)) }));

  const addUrlRow = () =>
    setForm(f => (f.urls.length >= MAX_URLS ? f : { ...f, urls: [...f.urls, ''] }));

  const removeUrlRow = (idx: number) =>
    setForm(f => ({ ...f, urls: f.urls.filter((_, i) => i !== idx) }));

  const cleanUrlsForSubmit = (): string[] | null => {
    const cleaned = form.urls.map(u => u.trim()).filter(u => u.length > 0);
    if (cleaned.length > MAX_URLS) {
      setError(`You can attach at most ${MAX_URLS} URLs.`);
      return null;
    }
    for (const u of cleaned) {
      if (!isValidHttpUrl(u)) {
        setError('Each URL must start with http:// or https://');
        return null;
      }
    }
    return cleaned;
  };

  const addItem = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    const urls = cleanUrlsForSubmit();
    if (urls === null) return;
    try {
      await client.post('/wishlists/me/items', {
        title: form.title,
        description: form.description || null,
        urls,
      });
      setForm(emptyForm);
      setAdding(false);
      load();
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error;
      setError(msg ?? 'Failed to add item.');
    }
  };

  const saveEdit = async (id: string) => {
    setError('');
    const urls = cleanUrlsForSubmit();
    if (urls === null) return;
    try {
      await client.patch(`/wishlists/me/items/${id}`, {
        title: form.title || null,
        description: form.description || null,
        urls,
      });
      setEditingId(null);
      setForm(emptyForm);
      load();
    } catch (err: unknown) {
      const msg = (err as { response?: { data?: { error?: string } } })?.response?.data?.error;
      setError(msg ?? 'Failed to save.');
    }
  };

  const deleteItem = async (id: string) => {
    if (!confirm('Delete this item?')) return;
    await client.delete(`/wishlists/me/items/${id}`);
    load();
  };

  const startEdit = (item: WishlistItem) => {
    setEditingId(item.id);
    setForm({
      title: item.title,
      description: item.description ?? '',
      urls: item.urls.length > 0 ? [...item.urls] : [],
    });
  };

  const renderUrlEditor = () => (
    <div style={{ marginBottom: 8 }}>
      <label style={{ fontSize: 13, color: '#555' }}>Links</label>
      {form.urls.map((u, i) => (
        <div key={i} style={{ display: 'flex', gap: 4, marginBottom: 4 }}>
          <input
            value={u}
            onChange={e => setUrl(i, e.target.value)}
            placeholder="https://..."
            style={{ flex: 1 }}
          />
          <button type="button" onClick={() => removeUrlRow(i)} aria-label="Remove URL">
            ✕
          </button>
        </div>
      ))}
      <button
        type="button"
        onClick={addUrlRow}
        disabled={form.urls.length >= MAX_URLS}
        style={{ fontSize: 13 }}
      >
        + Add link {form.urls.length > 0 && `(${form.urls.length}/${MAX_URLS})`}
      </button>
    </div>
  );

  return (
    <div style={{ maxWidth: 600, margin: '40px auto', padding: 24 }}>
      <Link to="/">← Back</Link>
      <h1>My wishlist</h1>
      <p style={{ color: '#666', fontSize: 14 }}>People in your groups will see this list, but you won't see who has reserved what.</p>
      {error && <p style={{ color: 'red' }}>{error}</p>}

      <ul style={{ listStyle: 'none', padding: 0 }}>
        {items.map(item => (
          <li key={item.id} style={{ borderBottom: '1px solid #ddd', padding: '12px 0' }}>
            {editingId === item.id ? (
              <div>
                <input value={form.title} onChange={e => setForm(f => ({ ...f, title: e.target.value }))} placeholder="Title" style={{ display: 'block', marginBottom: 8, width: '100%' }} />
                <input value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} placeholder="Description" style={{ display: 'block', marginBottom: 8, width: '100%' }} />
                {renderUrlEditor()}
                <button onClick={() => saveEdit(item.id)}>Save</button>
                <button onClick={() => { setEditingId(null); setForm(emptyForm); }} style={{ marginLeft: 8 }}>Cancel</button>
              </div>
            ) : (
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                <div>
                  <strong>{item.title}</strong>
                  {item.description && <p style={{ margin: '4px 0', color: '#555' }}>{item.description}</p>}
                  {item.urls.length > 0 && (
                    <div style={{ marginTop: 4, display: 'flex', flexWrap: 'wrap', gap: 8 }}>
                      {item.urls.map((u, i) => (
                        <a key={i} href={u} target="_blank" rel="noopener noreferrer" style={{ fontSize: 13 }}>
                          {hostFor(u)}
                        </a>
                      ))}
                    </div>
                  )}
                </div>
                <div>
                  <button onClick={() => startEdit(item)} style={{ marginRight: 8 }}>Edit</button>
                  <button onClick={() => deleteItem(item.id)} style={{ color: 'red' }}>Delete</button>
                </div>
              </div>
            )}
          </li>
        ))}
      </ul>

      {adding ? (
        <form onSubmit={addItem} style={{ marginTop: 16 }}>
          <input value={form.title} onChange={e => setForm(f => ({ ...f, title: e.target.value }))} placeholder="Title *" required style={{ display: 'block', marginBottom: 8, width: '100%' }} />
          <input value={form.description} onChange={e => setForm(f => ({ ...f, description: e.target.value }))} placeholder="Description" style={{ display: 'block', marginBottom: 8, width: '100%' }} />
          {renderUrlEditor()}
          <button type="submit">Add</button>
          <button type="button" onClick={() => { setAdding(false); setForm(emptyForm); }} style={{ marginLeft: 8 }}>Cancel</button>
        </form>
      ) : (
        <button onClick={() => { setForm(emptyForm); setAdding(true); }} style={{ marginTop: 16 }}>+ Add item</button>
      )}
    </div>
  );
}

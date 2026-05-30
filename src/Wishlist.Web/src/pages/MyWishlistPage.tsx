import { useEffect, useState } from 'react';
import { Link } from 'react-router-dom';
import client from '../api/client';

interface WishlistItem {
  id: string;
  title: string;
  description?: string;
  url?: string;
  price?: number;
  createdAt: string;
}

export function MyWishlistPage() {
  const [items, setItems] = useState<WishlistItem[]>([]);
  const [editingId, setEditingId] = useState<string | null>(null);
  const [form, setForm] = useState({ title: '', description: '', url: '', price: '' });
  const [adding, setAdding] = useState(false);
  const [error, setError] = useState('');

  const load = () => client.get('/wishlists/me').then(r => setItems(r.data));

  useEffect(() => { load(); }, []);

  const addItem = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    try {
      await client.post('/wishlists/me/items', {
        title: form.title,
        description: form.description || null,
        url: form.url || null,
        price: form.price ? Number(form.price) : null,
      });
      setForm({ title: '', description: '', url: '', price: '' });
      setAdding(false);
      load();
    } catch (err: any) {
      setError(err.response?.data?.error ?? 'Failed to add item.');
    }
  };

  const saveEdit = async (id: string) => {
    try {
      await client.patch(`/wishlists/me/items/${id}`, {
        title: form.title || null,
        description: form.description || null,
        url: form.url || null,
        price: form.price ? Number(form.price) : null,
      });
      setEditingId(null);
      load();
    } catch (err: any) {
      setError(err.response?.data?.error ?? 'Failed to save.');
    }
  };

  const deleteItem = async (id: string) => {
    if (!confirm('Delete this item?')) return;
    await client.delete(`/wishlists/me/items/${id}`);
    load();
  };

  const startEdit = (item: WishlistItem) => {
    setEditingId(item.id);
    setForm({ title: item.title, description: item.description ?? '', url: item.url ?? '', price: item.price?.toString() ?? '' });
  };

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
                <input value={form.url} onChange={e => setForm(f => ({ ...f, url: e.target.value }))} placeholder="Link" style={{ display: 'block', marginBottom: 8, width: '100%' }} />
                <input value={form.price} onChange={e => setForm(f => ({ ...f, price: e.target.value }))} placeholder="Price" type="number" style={{ display: 'block', marginBottom: 8, width: '100%' }} />
                <button onClick={() => saveEdit(item.id)}>Save</button>
                <button onClick={() => setEditingId(null)} style={{ marginLeft: 8 }}>Cancel</button>
              </div>
            ) : (
              <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
                <div>
                  <strong>{item.title}</strong>
                  {item.description && <p style={{ margin: '4px 0', color: '#555' }}>{item.description}</p>}
                  {item.url && <a href={item.url} target="_blank" rel="noreferrer" style={{ fontSize: 13 }}>View link</a>}
                  {item.price != null && <span style={{ marginLeft: 8, fontSize: 13 }}>€{item.price}</span>}
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
          <input value={form.url} onChange={e => setForm(f => ({ ...f, url: e.target.value }))} placeholder="Link" style={{ display: 'block', marginBottom: 8, width: '100%' }} />
          <input value={form.price} onChange={e => setForm(f => ({ ...f, price: e.target.value }))} placeholder="Price" type="number" step="0.01" style={{ display: 'block', marginBottom: 8, width: '100%' }} />
          <button type="submit">Add</button>
          <button type="button" onClick={() => setAdding(false)} style={{ marginLeft: 8 }}>Cancel</button>
        </form>
      ) : (
        <button onClick={() => setAdding(true)} style={{ marginTop: 16 }}>+ Add item</button>
      )}
    </div>
  );
}

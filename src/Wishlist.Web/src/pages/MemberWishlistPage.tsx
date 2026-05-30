import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import client from '../api/client';

interface GiftClaim {
  id: string;
  claimedByUserId: string;
  status: 'Reserved' | 'Bought';
  isClaimedByMe: boolean;
}

interface WishlistItem {
  id: string;
  ownerId: string;
  title: string;
  description?: string;
  url?: string;
  price?: number;
  claim: GiftClaim | null;
}

export function MemberWishlistPage() {
  const { userId } = useParams<{ userId: string }>();
  const [items, setItems] = useState<WishlistItem[]>([]);
  const [error, setError] = useState('');

  const load = () =>
    client.get(`/wishlists/${userId}`)
      .then(r => setItems(r.data))
      .catch(() => setError('Failed to load wishlist.'));

  useEffect(() => { load(); }, [userId]);

  const reserve = async (itemId: string) => {
    try {
      await client.post(`/wishlists/${userId}/items/${itemId}/claim`);
      load();
    } catch (err: any) {
      setError(err.response?.data?.error ?? 'Failed to reserve.');
    }
  };

  const markBought = async (itemId: string) => {
    try {
      await client.patch(`/wishlists/${userId}/items/${itemId}/claim`);
      load();
    } catch (err: any) {
      setError(err.response?.data?.error ?? 'Failed to update status.');
    }
  };

  const releaseClaim = async (itemId: string) => {
    try {
      await client.delete(`/wishlists/${userId}/items/${itemId}/claim`);
      load();
    } catch (err: any) {
      setError(err.response?.data?.error ?? 'Failed to release claim.');
    }
  };

  const statusBadge = (claim: GiftClaim | null) => {
    if (!claim) return <span style={{ color: '#888', fontSize: 13 }}>Available</span>;
    if (claim.status === 'Bought') return <span style={{ color: 'green', fontSize: 13 }}>Bought ✓</span>;
    if (claim.isClaimedByMe) return <span style={{ color: '#c77', fontSize: 13 }}>Reserved by you</span>;
    return <span style={{ color: '#c77', fontSize: 13 }}>Reserved by someone</span>;
  };

  return (
    <div style={{ maxWidth: 600, margin: '40px auto', padding: 24 }}>
      <Link to="/">← Back</Link>
      <h1>Wishlist</h1>
      {error && <p style={{ color: 'red' }}>{error}</p>}

      {items.length === 0 && <p>This person hasn't added any items yet.</p>}

      <ul style={{ listStyle: 'none', padding: 0 }}>
        {items.map(item => (
          <li key={item.id} style={{ borderBottom: '1px solid #ddd', padding: '16px 0' }}>
            <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'flex-start' }}>
              <div style={{ flex: 1 }}>
                <strong>{item.title}</strong>
                {item.description && <p style={{ margin: '4px 0', color: '#555' }}>{item.description}</p>}
                <div style={{ marginTop: 4 }}>
                  {item.url && <a href={item.url} target="_blank" rel="noreferrer" style={{ fontSize: 13, marginRight: 12 }}>View link</a>}
                  {item.price != null && <span style={{ fontSize: 13 }}>€{item.price}</span>}
                </div>
                <div style={{ marginTop: 8 }}>{statusBadge(item.claim)}</div>
              </div>

              <div style={{ display: 'flex', flexDirection: 'column', gap: 8, marginLeft: 16 }}>
                {!item.claim && (
                  <button onClick={() => reserve(item.id)} style={{ background: '#4a90d9', color: 'white', border: 'none', padding: '6px 12px', cursor: 'pointer' }}>
                    Reserve
                  </button>
                )}
                {item.claim?.isClaimedByMe && item.claim.status === 'Reserved' && (
                  <button onClick={() => markBought(item.id)} style={{ background: 'green', color: 'white', border: 'none', padding: '6px 12px', cursor: 'pointer' }}>
                    Mark as bought
                  </button>
                )}
                {item.claim?.isClaimedByMe && (
                  <button onClick={() => releaseClaim(item.id)} style={{ background: 'transparent', color: '#c00', border: '1px solid #c00', padding: '6px 12px', cursor: 'pointer' }}>
                    Release claim
                  </button>
                )}
                {item.claim && !item.claim.isClaimedByMe && (
                  <button disabled style={{ background: '#eee', color: '#999', border: 'none', padding: '6px 12px' }}>
                    Reserved
                  </button>
                )}
              </div>
            </div>
          </li>
        ))}
      </ul>
    </div>
  );
}

import { useEffect, useState } from 'react';
import { useParams, Link } from 'react-router-dom';
import client from '../api/client';
import { useAuth } from '../context/AuthContext';

interface Member {
  userId: string;
  displayName: string;
  role: string;
}

interface WishlistEntry {
  userId: string;
  displayName: string;
}

export function GroupDetailPage() {
  const { groupId } = useParams<{ groupId: string }>();
  const { user } = useAuth();
  const [members, setMembers] = useState<Member[]>([]);
  const [wishlists, setWishlists] = useState<WishlistEntry[]>([]);
  const [inviteUrl, setInviteUrl] = useState('');
  const [copied, setCopied] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    Promise.all([
      client.get(`/groups/${groupId}/members`),
      client.get(`/groups/${groupId}/wishlists`),
    ]).then(([membersRes, wishlistsRes]) => {
      setMembers(membersRes.data);
      setWishlists(wishlistsRes.data);
    }).catch(() => setError('Failed to load group details.'));
  }, [groupId]);

  const generateInvite = async () => {
    try {
      const { data } = await client.post(`/groups/${groupId}/invitations`);
      setInviteUrl(data.inviteUrl);
      await navigator.clipboard.writeText(data.inviteUrl);
      setCopied(true);
      setTimeout(() => setCopied(false), 3000);
    } catch {
      setError('Failed to generate invitation link.');
    }
  };

  const leaveGroup = async () => {
    if (!confirm('Leave this group?')) return;
    try {
      await client.delete(`/groups/${groupId}/members/me`);
      window.location.href = '/';
    } catch (err: any) {
      setError(err.response?.data?.error ?? 'Failed to leave group.');
    }
  };

  return (
    <div style={{ maxWidth: 600, margin: '40px auto', padding: 24 }}>
      <Link to="/">← Back</Link>
      <h1>Group</h1>
      {error && <p style={{ color: 'red' }}>{error}</p>}

      <h2>Members</h2>
      <ul style={{ listStyle: 'none', padding: 0 }}>
        {members.map(m => (
          <li key={m.userId} style={{ padding: '8px 0' }}>
            {m.displayName} {m.role === 'Owner' ? '(owner)' : ''}
            {m.userId !== user?.id && (
              <Link to={`/wishlist/${m.userId}`} style={{ marginLeft: 12 }}>View wishlist</Link>
            )}
          </li>
        ))}
      </ul>

      <h2>Other wishlists</h2>
      <ul style={{ listStyle: 'none', padding: 0 }}>
        {wishlists.map(w => (
          <li key={w.userId} style={{ padding: '8px 0' }}>
            <Link to={`/wishlist/${w.userId}`}>{w.displayName}'s wishlist</Link>
          </li>
        ))}
      </ul>

      <div style={{ marginTop: 24, display: 'flex', gap: 12, flexWrap: 'wrap' }}>
        <button onClick={generateInvite}>
          {copied ? 'Copied!' : 'Generate invite link'}
        </button>
        {inviteUrl && <span style={{ wordBreak: 'break-all', fontSize: 12 }}>{inviteUrl}</span>}
        <button onClick={leaveGroup} style={{ marginLeft: 'auto', color: 'red' }}>Leave group</button>
      </div>
    </div>
  );
}

import { useEffect, useState } from 'react';
import { Link, useNavigate } from 'react-router-dom';
import client from '../api/client';
import { useAuth } from '../context/AuthContext';

interface Group {
  id: string;
  name: string;
  createdAt: string;
}

export function HomePage() {
  const { user, logout } = useAuth();
  const navigate = useNavigate();
  const [groups, setGroups] = useState<Group[]>([]);
  const [newGroupName, setNewGroupName] = useState('');
  const [showCreate, setShowCreate] = useState(false);
  const [error, setError] = useState('');

  useEffect(() => {
    client.get('/groups/me')
      .then(r => setGroups(r.data))
      .catch((err) => setError(`Load failed: ${err.response?.status} ${err.response?.data?.error ?? err.message}`));
  }, []);

  const createGroup = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    try {
      const { data } = await client.post('/groups', { name: newGroupName });
      setGroups(prev => [...prev, data]);
      setNewGroupName('');
      setShowCreate(false);
    } catch (err: any) {
      setError(`${err.response?.status ?? 'Network error'}: ${err.response?.data?.error ?? err.message}`);
    }
  };

  const handleLogout = async () => {
    await logout();
    navigate('/login');
  };

  return (
    <div style={{ maxWidth: 600, margin: '40px auto', padding: 24 }}>
      <div style={{ display: 'flex', justifyContent: 'space-between', alignItems: 'center' }}>
        <h1>My groups</h1>
        <div>
          <Link to="/wishlist/me" style={{ marginRight: 16 }}>My wishlist</Link>
          <button onClick={handleLogout}>Sign out ({user?.displayName})</button>
        </div>
      </div>

      {groups.length === 0 && <p>You're not in any groups yet. Create one to get started.</p>}
      <ul style={{ listStyle: 'none', padding: 0 }}>
        {groups.map(g => (
          <li key={g.id} style={{ borderBottom: '1px solid #ddd', padding: '12px 0' }}>
            <Link to={`/groups/${g.id}`}>{g.name}</Link>
          </li>
        ))}
      </ul>

      {showCreate ? (
        <form onSubmit={createGroup}>
          {error && <p style={{ color: 'red' }}>{error}</p>}
          <input
            value={newGroupName}
            onChange={e => setNewGroupName(e.target.value)}
            placeholder="Group name"
            required
            style={{ marginRight: 8 }}
          />
          <button type="submit">Create</button>
          <button type="button" onClick={() => setShowCreate(false)} style={{ marginLeft: 8 }}>Cancel</button>
        </form>
      ) : (
        <button onClick={() => setShowCreate(true)}>+ New group</button>
      )}
    </div>
  );
}

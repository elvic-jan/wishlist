import { useEffect, useState } from 'react';
import { useParams, useNavigate } from 'react-router-dom';
import client from '../api/client';
import { useAuth } from '../context/AuthContext';

export function JoinGroupPage() {
  const { code } = useParams<{ code: string }>();
  const { user } = useAuth();
  const navigate = useNavigate();
  const [status, setStatus] = useState<'joining' | 'done' | 'error' | 'unauthenticated'>('joining');
  const [error, setError] = useState('');

  useEffect(() => {
    if (!user) {
      setStatus('unauthenticated');
      return;
    }

    client.post(`/invitations/${code}/join`)
      .then(() => {
        setStatus('done');
        setTimeout(() => navigate('/'), 2000);
      })
      .catch((err) => {
        setStatus('error');
        setError(err.response?.data?.error ?? 'Failed to join group. The link may have expired.');
      });
  }, [code, user, navigate]);

  if (status === 'unauthenticated') {
    return (
      <div style={{ maxWidth: 400, margin: '80px auto', padding: 24, textAlign: 'center' }}>
        <p>Please <a href={`/login?redirect=/join/${code}`}>sign in</a> to join the group.</p>
      </div>
    );
  }

  if (status === 'joining') return <div style={{ textAlign: 'center', marginTop: 80 }}>Joining group…</div>;
  if (status === 'done') return <div style={{ textAlign: 'center', marginTop: 80 }}>Joined! Redirecting…</div>;
  return (
    <div style={{ textAlign: 'center', marginTop: 80, color: 'red' }}>
      <p>{error}</p>
      <a href="/">Go home</a>
    </div>
  );
}

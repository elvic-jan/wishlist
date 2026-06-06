import { useState } from 'react';
import { Link } from 'react-router-dom';
import { requestPasswordReset } from '../api/auth';

export function ForgotPasswordPage() {
  const [email, setEmail] = useState('');
  const [submitted, setSubmitted] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    setError('');
    try {
      await requestPasswordReset(email);
      // Always show the same confirmation, matching the server's
      // enumeration-safe response — never tell the user whether the
      // email actually exists.
      setSubmitted(true);
    } catch (err: unknown) {
      // axios responses with a real status (e.g. 429) land here. Treat
      // any non-202 the same way — the user retries.
      const status = (err as { response?: { status?: number } })?.response?.status;
      if (status === 429) {
        setError('Too many requests. Please wait a few minutes and try again.');
      } else {
        setError('Something went wrong. Please try again in a moment.');
      }
    }
  };

  if (submitted) {
    return (
      <div style={{ maxWidth: 400, margin: '80px auto', padding: 24 }}>
        <h1>Check your inbox</h1>
        <p>
          If an account exists for <strong>{email}</strong>, we&rsquo;ve sent a
          link to reset your password. The link expires in 30 minutes.
        </p>
        <p>
          Didn&rsquo;t get an email? Check your spam folder, then{' '}
          <Link to="/forgot-password" onClick={() => setSubmitted(false)}>
            request another one
          </Link>
          .
        </p>
        <p>
          <Link to="/login">Back to sign in</Link>
        </p>
      </div>
    );
  }

  return (
    <div style={{ maxWidth: 400, margin: '80px auto', padding: 24 }}>
      <h1>Forgot your password?</h1>
      <p>Enter the email address on your account and we&rsquo;ll send you a reset link.</p>
      {error && <p style={{ color: 'red' }}>{error}</p>}
      <form onSubmit={handleSubmit}>
        <div>
          <label>Email</label>
          <input
            type="email"
            value={email}
            onChange={e => setEmail(e.target.value)}
            required
            style={{ display: 'block', width: '100%', marginBottom: 12 }}
          />
        </div>
        <button type="submit" style={{ width: '100%' }}>Send reset link</button>
      </form>
      <p>
        <Link to="/login">Back to sign in</Link>
      </p>
    </div>
  );
}

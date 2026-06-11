import { useState } from 'react';
import './LoginScreen.css';

interface Props {
  onLogin: (username: string) => Promise<void>;
}

export default function LoginScreen({ onLogin }: Props) {
  const [username, setUsername] = useState('');
  const [loading, setLoading] = useState(false);
  const [error, setError] = useState('');

  const handleSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!username.trim()) return;
    setLoading(true);
    setError('');
    try {
      await onLogin(username.trim());
    } catch {
      setError('Failed to connect. Make sure the server is running.');
    } finally {
      setLoading(false);
    }
  };

  return (
    <div className="login-screen">
      <form className="login-card" onSubmit={handleSubmit}>
        <div className="login-logo">ChatBox</div>
        <p className="login-subtitle">Enter your username to join</p>
        <input
          className="login-input"
          type="text"
          placeholder="Username"
          value={username}
          onChange={e => setUsername(e.target.value)}
          maxLength={50}
          autoFocus
        />
        {error && <p className="login-error">{error}</p>}
        <button className="login-btn" type="submit" disabled={loading || !username.trim()}>
          {loading ? 'Connecting...' : 'Join Chat'}
        </button>
      </form>
    </div>
  );
}

import { useState } from 'react';
import { ConnectionStatus, OnlineUser } from '../types';
import UserAvatar from './UserAvatar';
import './SidebarLeft.css';

interface Props {
  user: { id: string; username: string } | null;
  onlineUsers: OnlineUser[];
  connectionStatus: ConnectionStatus;
  onLogin: (username: string) => Promise<void>;
  onConnect: () => void;
  onLogout: () => void;
  showOnlineList?: boolean;
}

export default function SidebarLeft({
  user,
  onlineUsers,
  connectionStatus,
  onLogin,
  onConnect,
  onLogout,
  showOnlineList = true,
}: Props) {
  const [loginUsername, setLoginUsername] = useState('');
  const [loginLoading, setLoginLoading] = useState(false);
  const [loginError, setLoginError] = useState('');

  const handleLoginSubmit = async (e: React.FormEvent) => {
    e.preventDefault();
    if (!loginUsername.trim() || loginLoading) return;
    setLoginLoading(true);
    setLoginError('');
    try {
      await onLogin(loginUsername.trim());
    } catch (err) {
      console.error('Login error:', err);
      const msg = err instanceof Error ? err.message : String(err);
      setLoginError(msg || 'Failed to connect. Is the server running?');
    } finally {
      setLoginLoading(false);
    }
  };

  if (!user) {
    return (
      <div className="sidebar-left">
        <div className="sidebar-header">
          <div className="app-name">ChatBox</div>
        </div>
        <div className="login-section">
          <form className="login-form" onSubmit={handleLoginSubmit}>
            <div className="login-title">Welcome!</div>
            <div className="login-desc">Enter your username to join</div>
            <input
              className="login-input"
              type="text"
              placeholder="Username"
              value={loginUsername}
              onChange={e => setLoginUsername(e.target.value)}
              maxLength={50}
              autoFocus
            />
            {loginError && <div className="login-error-msg">{loginError}</div>}
            <button className="login-submit-btn" type="submit" disabled={loginLoading || !loginUsername.trim()}>
              {loginLoading ? 'Connecting...' : 'Join Chat'}
            </button>
          </form>
        </div>
      </div>
    );
  }

  const statusColor = connectionStatus === 'connected' ? '#3BA55D' : connectionStatus === 'connecting' ? '#FAA81A' : '#F23F42';
  const statusText = connectionStatus === 'connected' ? 'Online' : connectionStatus === 'connecting' ? 'Connecting...' : 'Disconnected';

  const others = onlineUsers.filter(u => u.userId !== user.id);
  const currentUser = onlineUsers.find(u => u.userId === user.id);

  return (
    <div className="sidebar-left">
      <div className="sidebar-header">
        <div className="app-name">ChatBox</div>
      </div>

      <div className="user-section">
        <UserAvatar username={user.username} size={40} />
        <div className="user-info">
          <div className="user-name">{user.username}</div>
          <div className="user-label">You</div>
        </div>
      </div>

      {showOnlineList && (
        <div className="online-section">
          <div className="online-section-header">
            Online — {onlineUsers.length}
          </div>
          <div className="online-list">
            {currentUser && (
              <div key={currentUser.userId} className="online-user-item">
                <UserAvatar username={currentUser.username} size={24} />
                <span className="online-user-name">{currentUser.username}</span>
                <span className="online-dot" />
              </div>
            )}
            {others.map(u => (
              <div key={u.userId} className="online-user-item">
                <UserAvatar username={u.username} size={24} />
                <span className="online-user-name">{u.username}</span>
                <span className="online-dot" />
              </div>
            ))}
          </div>
        </div>
      )}

      <div className="sidebar-footer">
        <div className="connection-status" onClick={connectionStatus === 'disconnected' ? onConnect : undefined}>
          <span className="status-dot" style={{ background: statusColor }} />
          <span className="status-text">{statusText}</span>
        </div>
        <button className="logout-btn" onClick={onLogout} title="Logout">
          <svg width="18" height="18" viewBox="0 0 24 24" fill="none" stroke="currentColor" strokeWidth="2" strokeLinecap="round" strokeLinejoin="round">
            <path d="M9 21H5a2 2 0 0 1-2-2V5a2 2 0 0 1 2-2h4" />
            <polyline points="16 17 21 12 16 7" />
            <line x1="21" y1="12" x2="9" y2="12" />
          </svg>
        </button>
      </div>
    </div>
  );
}

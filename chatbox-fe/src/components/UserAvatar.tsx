import './UserAvatar.css';

interface Props {
  username: string;
  size?: number;
}

function hashColor(username: string): string {
  const colors = [
    '#5865F2', '#ED4245', '#FAA61A', '#57F287', '#EB459E',
    '#00B0F4', '#FF73FA', '#C9CDFF', '#AF5CF7', '#F26522',
  ];
  let hash = 0;
  for (let i = 0; i < username.length; i++) {
    hash = username.charCodeAt(i) + ((hash << 5) - hash);
  }
  return colors[Math.abs(hash) % colors.length];
}

export default function UserAvatar({ username, size = 36 }: Props) {
  const letter = username.charAt(0).toUpperCase();
  const bg = hashColor(username);

  return (
    <div
      className="user-avatar"
      style={{
        width: size,
        height: size,
        minWidth: size,
        background: bg,
        fontSize: size * 0.45,
      }}
    >
      {letter}
    </div>
  );
}

import { Message, User } from '../types';

export const API_URL = '';
const BASE = '/api';

export async function loginUser(username: string): Promise<User> {
  const res = await fetch(`${BASE}/user/login`, {
    method: 'POST',
    headers: { 'Content-Type': 'application/json' },
    body: JSON.stringify({ username }),
  });
  if (!res.ok) throw new Error('Login failed');
  return res.json();
}

export async function getChatHistory(): Promise<Message[]> {
  const res = await fetch(`${BASE}/chat/history`);
  if (!res.ok) throw new Error('Failed to fetch history');
  return res.json();
}

export async function uploadFile(
  senderId: string,
  file: File,
  content?: string
): Promise<Message> {
  const formData = new FormData();
  formData.append('senderId', senderId);
  formData.append('file', file);
  if (content) formData.append('content', content);

  const res = await fetch(`${BASE}/chat/upload`, {
    method: 'POST',
    body: formData,
  });
  if (!res.ok) {
    const err = await res.json().catch(() => ({ message: 'Upload failed' }));
    throw new Error(err.message || 'Upload failed');
  }
  return res.json();
}

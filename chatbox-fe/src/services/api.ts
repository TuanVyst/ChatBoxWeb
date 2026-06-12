import { Message, User } from '../types';

export const API_URL = '';
const BASE = '/api';

export interface UploadProgress {
  percent: number;       // 0-100
  loaded: number;        // bytes đã gửi
  total: number;         // tổng bytes
  speed: number;         // bytes/giây
}

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
  content?: string,
  onProgress?: (progress: UploadProgress) => void
): Promise<Message> {
  const formData = new FormData();
  formData.append('senderId', senderId);
  formData.append('file', file);
  if (content) formData.append('content', content);

  return new Promise<Message>((resolve, reject) => {
    const xhr = new XMLHttpRequest();
    let startTime = Date.now();

    xhr.upload.addEventListener('progress', (e) => {
      if (e.lengthComputable && onProgress) {
        const elapsed = (Date.now() - startTime) / 1000; // giây
        const speed = elapsed > 0 ? e.loaded / elapsed : 0;
        onProgress({
          percent: Math.round((e.loaded / e.total) * 100),
          loaded: e.loaded,
          total: e.total,
          speed,
        });
      }
    });

    xhr.addEventListener('load', () => {
      if (xhr.status >= 200 && xhr.status < 300) {
        try {
          resolve(JSON.parse(xhr.responseText));
        } catch {
          reject(new Error('Invalid response'));
        }
      } else {
        try {
          const err = JSON.parse(xhr.responseText);
          reject(new Error(err.message || 'Upload failed'));
        } catch {
          reject(new Error('Upload failed'));
        }
      }
    });

    xhr.addEventListener('error', () => reject(new Error('Network error')));
    xhr.addEventListener('abort', () => reject(new Error('Upload cancelled')));

    xhr.open('POST', `${BASE}/chat/upload`);
    xhr.send(formData);
  });
}

export interface User {
  id: string;
  username: string;
  createdAt: string;
}

export interface Message {
  id: string;
  senderId: string;
  senderUsername: string;
  content: string | null;
  type: MessageType;
  fileUrl: string | null;
  timestamp: string;
}

export enum MessageType {
  Text = 0,
  Image = 1,
  File = 2,
}

export interface OnlineUser {
  userId: string;
  username: string;
}

export type ConnectionStatus = 'disconnected' | 'connecting' | 'connected';

export interface Channel {
  id: string;
  name: string;
}

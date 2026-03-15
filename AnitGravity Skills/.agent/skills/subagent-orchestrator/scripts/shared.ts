/**
 * Shared types and utilities for A.N.T. Subagent Orchestrator
 */

import { readFileSync, existsSync, mkdirSync, writeFileSync } from 'fs';
import { join } from 'path';

// ── Types ──────────────────────────────────────────────────

export interface Session {
  task_id: string;
  status: 'queued' | 'spawning' | 'running' | 'completed' | 'crashed' | 'killed';
  branch: string;
  worktree: string;
  started_at?: string;
  last_heartbeat?: string;
  retries_remaining: number;
  output_log: string;
  pid?: number;
  dependencies: string[];
  priority: number;
}

export interface SessionState {
  version: string;
  repo: string;
  sessions: Session[];
  completed: Session[];
  crashed: Session[];
}

// ── Constants ──────────────────────────────────────────────

export const ANT_DIR = '.ant';
export const SESSIONS_FILE = join(ANT_DIR, 'sessions.json');
export const LOGS_DIR = join(ANT_DIR, 'logs');
export const WORKTREES_DIR = join(ANT_DIR, 'worktrees');

// ── State Management ───────────────────────────────────────

export function loadState(): SessionState {
  if (!existsSync(SESSIONS_FILE)) {
    console.error('❌ No session state found. Run subagent-spawn first.');
    process.exit(1);
  }
  return JSON.parse(readFileSync(SESSIONS_FILE, 'utf8'));
}

export function saveState(state: SessionState): void {
  if (!existsSync(ANT_DIR)) mkdirSync(ANT_DIR, { recursive: true });
  writeFileSync(SESSIONS_FILE, JSON.stringify(state, null, 2));
}

export function findSession(state: SessionState, taskId: string): { session: Session; pool: 'sessions' | 'completed' | 'crashed' } | null {
  let session = state.sessions.find(s => s.task_id === taskId);
  if (session) return { session, pool: 'sessions' };

  session = state.completed.find(s => s.task_id === taskId);
  if (session) return { session, pool: 'completed' };

  session = state.crashed.find(s => s.task_id === taskId);
  if (session) return { session, pool: 'crashed' };

  return null;
}

// ── Process Helpers ────────────────────────────────────────

export function isProcessAlive(pid: number): boolean {
  try {
    process.kill(pid, 0); // Signal 0 = just check existence
    return true;
  } catch {
    return false;
  }
}

// ── Formatting Helpers ─────────────────────────────────────

export function elapsed(startIso?: string): string {
  if (!startIso) return '—';
  const ms = Date.now() - new Date(startIso).getTime();
  const sec = Math.floor(ms / 1000);
  if (sec < 60) return `${sec}s`;
  const min = Math.floor(sec / 60);
  if (min < 60) return `${min}m ${sec % 60}s`;
  const hr = Math.floor(min / 60);
  return `${hr}h ${min % 60}m`;
}

export function statusIcon(status: Session['status']): string {
  const icons: Record<string, string> = {
    queued: '⏳',
    spawning: '🔄',
    running: '🟢',
    completed: '✅',
    crashed: '💥',
    killed: '🔴',
  };
  return icons[status] || '❓';
}

export function priorityLabel(p: number): string {
  const labels: Record<number, string> = { 1: 'low', 2: 'normal', 3: 'high', 4: 'urgent' };
  return labels[p] || 'normal';
}

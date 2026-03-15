#!/usr/bin/env node
/**
 * Subagent Status — View active/queued/completed/crashed agents
 *
 * Usage:
 *   subagent-status              # Full dashboard
 *   subagent-status --json       # Machine-readable output
 *   subagent-status <task-id>    # Single task detail
 */

import { readFileSync, existsSync, statSync } from 'fs';
import {
  loadState,
  findSession,
  isProcessAlive,
  elapsed,
  statusIcon,
  priorityLabel,
  saveState,
  Session,
  SessionState,
  LOGS_DIR,
} from './shared';
import { join } from 'path';

// ── Reconcile stale PIDs ───────────────────────────────────

function reconcile(state: SessionState): number {
  let fixes = 0;

  for (const session of state.sessions) {
    if (session.status === 'running' && session.pid) {
      if (!isProcessAlive(session.pid)) {
        session.status = 'crashed';
        session.last_heartbeat = new Date().toISOString();
        state.sessions = state.sessions.filter(s => s.task_id !== session.task_id);
        state.crashed.push(session);
        fixes++;
      }
    }
  }

  if (fixes > 0) saveState(state);
  return fixes;
}

// ── Single-task detail view ────────────────────────────────

function showDetail(state: SessionState, taskId: string): void {
  const found = findSession(state, taskId);
  if (!found) {
    console.error(`❌ Task "${taskId}" not found in any pool.`);
    process.exit(1);
  }

  const { session, pool } = found;
  const logPath = session.output_log;
  const logExists = existsSync(logPath);
  const logSize = logExists ? statSync(logPath).size : 0;

  console.log(`\n┌─── Task: ${session.task_id} ${'─'.repeat(Math.max(1, 50 - session.task_id.length))}┐`);
  console.log(`│  Status     : ${statusIcon(session.status)} ${session.status}`);
  console.log(`│  Pool       : ${pool}`);
  console.log(`│  Branch     : ${session.branch}`);
  console.log(`│  Worktree   : ${session.worktree}`);
  console.log(`│  Priority   : ${priorityLabel(session.priority)}`);
  console.log(`│  PID        : ${session.pid ?? '—'}`);
  console.log(`│  Started    : ${session.started_at ?? '—'}`);
  console.log(`│  Elapsed    : ${elapsed(session.started_at)}`);
  console.log(`│  Heartbeat  : ${session.last_heartbeat ?? '—'}`);
  console.log(`│  Retries    : ${session.retries_remaining}`);
  console.log(`│  Depends on : ${session.dependencies.length ? session.dependencies.join(', ') : '(none)'}`);
  console.log(`│  Log        : ${logPath} (${(logSize / 1024).toFixed(1)} KB)`);
  console.log(`└${'─'.repeat(56)}┘`);

  if (logExists && logSize > 0) {
    const tail = readFileSync(logPath, 'utf8').split('\n').slice(-20).join('\n');
    console.log(`\n── Last 20 lines of log ──\n${tail}`);
  }
}

// ── Dashboard view ─────────────────────────────────────────

function formatRow(s: Session): string {
  const alive = s.pid ? (isProcessAlive(s.pid) ? '●' : '✖') : ' ';
  const el = elapsed(s.started_at);
  const deps = s.dependencies.length ? `[${s.dependencies.join(',')}]` : '';
  return `  ${statusIcon(s.status)} ${s.task_id.padEnd(28)} ${s.status.padEnd(10)} ${el.padStart(8)}  PID:${String(s.pid ?? '—').padEnd(7)} ${alive}  ${deps}`;
}

function showDashboard(state: SessionState): void {
  const now = new Date().toISOString().replace('T', ' ').slice(0, 19);
  const total = state.sessions.length + state.completed.length + state.crashed.length;

  console.log(`\n╔══════════════════════════════════════════════════════════╗`);
  console.log(`║           A.N.T. Subagent Orchestrator Dashboard        ║`);
  console.log(`╠══════════════════════════════════════════════════════════╣`);
  console.log(`║  Repo : ${state.repo.padEnd(48)}║`);
  console.log(`║  Time : ${now.padEnd(48)}║`);
  console.log(`║  Total: ${String(total).padEnd(48)}║`);
  console.log(`╚══════════════════════════════════════════════════════════╝`);

  // Running
  const running = state.sessions.filter(s => s.status === 'running');
  const queued = state.sessions.filter(s => s.status === 'queued' || s.status === 'spawning');

  console.log(`\n🟢 RUNNING (${running.length})`);
  if (running.length === 0) {
    console.log('  (none)');
  } else {
    for (const s of running) console.log(formatRow(s));
  }

  console.log(`\n⏳ QUEUED (${queued.length})`);
  if (queued.length === 0) {
    console.log('  (none)');
  } else {
    for (const s of queued) console.log(formatRow(s));
  }

  console.log(`\n✅ COMPLETED (${state.completed.length})`);
  if (state.completed.length === 0) {
    console.log('  (none)');
  } else {
    for (const s of state.completed.slice(-10)) console.log(formatRow(s));
    if (state.completed.length > 10) console.log(`  ... and ${state.completed.length - 10} more`);
  }

  console.log(`\n💥 CRASHED (${state.crashed.length})`);
  if (state.crashed.length === 0) {
    console.log('  (none)');
  } else {
    for (const s of state.crashed) console.log(formatRow(s));
  }

  console.log('');
}

// ── Main ───────────────────────────────────────────────────

function main(): void {
  const args = process.argv.slice(2);
  const state = loadState();

  // Reconcile ghost processes
  const fixed = reconcile(state);
  if (fixed > 0) console.log(`⚠️  Reconciled ${fixed} stale process(es) → crashed`);

  if (args.includes('--json')) {
    console.log(JSON.stringify(state, null, 2));
    return;
  }

  const taskId = args.find(a => !a.startsWith('--'));
  if (taskId) {
    showDetail(state, taskId);
  } else {
    showDashboard(state);
  }
}

main();

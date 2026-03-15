#!/usr/bin/env node
/**
 * Subagent Kill — Terminate stuck or unwanted agents
 *
 * Usage:
 *   subagent-kill <task-id>              # Kill one agent
 *   subagent-kill <task-id> --cleanup    # Kill + remove worktree + branch
 *   subagent-kill --all                  # Kill all running agents
 *   subagent-kill --all --cleanup        # Nuclear: kill all + clean everything
 */

import { existsSync } from 'fs';
import { execSync } from 'child_process';
import {
  loadState,
  saveState,
  isProcessAlive,
  statusIcon,
  Session,
  SessionState,
} from './shared';

// ── Kill a single session ──────────────────────────────────

function killSession(state: SessionState, taskId: string, cleanup: boolean): boolean {
  const idx = state.sessions.findIndex(s => s.task_id === taskId);

  if (idx === -1) {
    console.error(`❌ Task "${taskId}" not found in active sessions.`);
    // Check if it's already finished
    if (state.completed.find(s => s.task_id === taskId)) {
      console.log(`  ℹ️  Task is in completed pool. Use --cleanup to remove its worktree.`);
      if (cleanup) cleanupWorktree(state.completed.find(s => s.task_id === taskId)!);
    } else if (state.crashed.find(s => s.task_id === taskId)) {
      console.log(`  ℹ️  Task is in crashed pool. Use --cleanup to remove its worktree.`);
      if (cleanup) cleanupWorktree(state.crashed.find(s => s.task_id === taskId)!);
    }
    return false;
  }

  const session = state.sessions[idx];

  // Kill the process
  if (session.pid) {
    if (isProcessAlive(session.pid)) {
      try {
        // On Windows, use taskkill for process tree; on Unix, kill -TERM
        if (process.platform === 'win32') {
          execSync(`taskkill /PID ${session.pid} /T /F`, { stdio: 'pipe' });
        } else {
          process.kill(session.pid, 'SIGTERM');
          // Give it 3s to exit gracefully, then SIGKILL
          setTimeout(() => {
            try {
              if (isProcessAlive(session.pid!)) {
                process.kill(session.pid!, 'SIGKILL');
              }
            } catch { /* already dead */ }
          }, 3000);
        }
        console.log(`  🔪 Killed process ${session.pid}`);
      } catch (e) {
        console.warn(`  ⚠️  Could not kill PID ${session.pid}: ${e}`);
      }
    } else {
      console.log(`  ℹ️  Process ${session.pid} was already dead`);
    }
  }

  // Update state
  session.status = 'killed';
  session.last_heartbeat = new Date().toISOString();
  state.sessions.splice(idx, 1);
  state.crashed.push(session); // killed goes to crashed pool

  if (cleanup) {
    cleanupWorktree(session);
  }

  saveState(state);
  console.log(`  ${statusIcon('killed')} ${taskId} → killed`);
  return true;
}

// ── Cleanup worktree and optionally branch ─────────────────

function cleanupWorktree(session: Session): void {
  const worktreePath = session.worktree;
  const branchName = session.branch;

  // Remove worktree
  if (existsSync(worktreePath)) {
    try {
      execSync(`git worktree remove --force ${worktreePath}`, { stdio: 'pipe' });
      console.log(`  🧹 Removed worktree: ${worktreePath}`);
    } catch (e) {
      console.warn(`  ⚠️  Could not remove worktree: ${e}`);
    }
  }

  // Prune stale worktree references
  try {
    execSync('git worktree prune', { stdio: 'pipe' });
  } catch { /* ignore */ }

  // Delete branch (only if it's a task/* branch we created)
  if (branchName.startsWith('task/')) {
    try {
      execSync(`git branch -D ${branchName}`, { stdio: 'pipe' });
      console.log(`  🗑️  Deleted branch: ${branchName}`);
    } catch {
      console.log(`  ℹ️  Branch ${branchName} may have been already deleted or merged`);
    }
  }
}

// ── Main ───────────────────────────────────────────────────

function main(): void {
  const args = process.argv.slice(2);
  const cleanup = args.includes('--cleanup');
  const killAll = args.includes('--all');
  const taskId = args.find(a => !a.startsWith('--'));

  if (!killAll && !taskId) {
    console.error('Usage: subagent-kill <task-id> [--cleanup]');
    console.error('       subagent-kill --all [--cleanup]');
    process.exit(1);
  }

  const state = loadState();

  if (killAll) {
    const active = state.sessions.filter(s => s.status === 'running' || s.status === 'spawning');
    if (active.length === 0) {
      console.log('✅ No running agents to kill.');
      if (cleanup) {
        // Cleanup queued too
        const queued = [...state.sessions];
        for (const s of queued) {
          cleanupWorktree(s);
          s.status = 'killed';
          s.last_heartbeat = new Date().toISOString();
          state.crashed.push(s);
        }
        state.sessions = [];
        saveState(state);
        if (queued.length > 0) console.log(`🧹 Cleaned up ${queued.length} queued task(s).`);
      }
      return;
    }

    console.log(`🔪 Killing ${active.length} running agent(s)...\n`);
    const ids = active.map(s => s.task_id);
    let killed = 0;
    for (const id of ids) {
      if (killSession(state, id, cleanup)) killed++;
    }

    // Also kill queued if cleanup
    if (cleanup) {
      const queued = [...state.sessions];
      for (const s of queued) {
        cleanupWorktree(s);
        s.status = 'killed';
        s.last_heartbeat = new Date().toISOString();
        state.crashed.push(s);
      }
      state.sessions = [];
      saveState(state);
      if (queued.length > 0) console.log(`\n🧹 Cleaned up ${queued.length} remaining queued task(s).`);
    }

    console.log(`\n📊 Killed ${killed}/${ids.length} agent(s).`);
  } else {
    killSession(state, taskId!, cleanup);
  }
}

main();

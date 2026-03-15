#!/usr/bin/env node
/**
 * Subagent Restore — Resume a crashed task
 *
 * Usage:
 *   subagent-restore <task-id>           # Re-spawn with remaining retries
 *   subagent-restore <task-id> --force   # Ignore retry count, force respawn
 *   subagent-restore --all               # Restore all crashed tasks
 */

import { readFileSync, existsSync, writeFileSync } from 'fs';
import { execSync, spawn } from 'child_process';
import { join } from 'path';
import {
  loadState,
  saveState,
  findSession,
  isProcessAlive,
  Session,
  SessionState,
  WORKTREES_DIR,
} from './shared';

// ── Restore a single session ───────────────────────────────

function restoreSession(state: SessionState, taskId: string, force: boolean): boolean {
  // Find in crashed pool
  const idx = state.crashed.findIndex(s => s.task_id === taskId);
  if (idx === -1) {
    // Maybe it's still in active sessions but marked crashed
    const active = state.sessions.find(s => s.task_id === taskId && s.status === 'crashed');
    if (!active) {
      console.error(`❌ Task "${taskId}" not found in crashed pool.`);
      return false;
    }
    // Move it to proper pool first, then restore
    state.sessions = state.sessions.filter(s => s.task_id !== taskId);
    state.crashed.push(active);
    return restoreSession(state, taskId, force);
  }

  const session = state.crashed[idx];

  // Check retries
  if (!force && session.retries_remaining <= 0) {
    console.error(`❌ ${taskId}: No retries remaining. Use --force to override.`);
    return false;
  }

  // Decrement retry count (unless forced)
  if (!force) {
    session.retries_remaining--;
  }

  // Clean up old worktree if it exists but is stale
  const worktreePath = session.worktree;
  if (existsSync(worktreePath)) {
    try {
      execSync(`git worktree remove --force ${worktreePath}`, { stdio: 'pipe' });
      console.log(`  🧹 Removed stale worktree: ${worktreePath}`);
    } catch {
      console.warn(`  ⚠️  Could not remove worktree ${worktreePath}, it may still be in use`);
    }
  }

  // Re-create worktree
  const branchName = session.branch;
  try {
    execSync(`git worktree add ${worktreePath} ${branchName}`, { stdio: 'pipe' });
    console.log(`  📂 Restored worktree on branch ${branchName}`);
  } catch {
    // Branch might not exist — create fresh
    try {
      execSync(`git worktree add -b ${branchName} ${worktreePath}`, { stdio: 'pipe' });
      console.log(`  📂 Created fresh worktree on new branch ${branchName}`);
    } catch (e) {
      console.error(`  ❌ Could not create worktree: ${e}`);
      return false;
    }
  }

  // Read task body from the worktree (if .ant-task.md exists) or log
  let taskBody = `Resume task: ${taskId}. Check .ant-task.md for original instructions. Fix the issues that caused the previous crash.`;
  const taskFile = join(worktreePath, '.ant-task.md');
  if (existsSync(taskFile)) {
    taskBody = readFileSync(taskFile, 'utf8');
    taskBody = `[RESTORED — previous attempt crashed. Retries remaining: ${session.retries_remaining}]\n\n${taskBody}`;
  }

  // Append crash context from log tail
  if (existsSync(session.output_log)) {
    const logTail = readFileSync(session.output_log, 'utf8').split('\n').slice(-30).join('\n');
    taskBody += `\n\n## Previous Crash Log (last 30 lines)\n\`\`\`\n${logTail}\n\`\`\`\n\nFix the above issues and complete the task.`;
  }

  // Reset log with restore header
  writeFileSync(session.output_log, `# Task: ${session.task_id} (RESTORED)\n# Restored at: ${new Date().toISOString()}\n# Retries remaining: ${session.retries_remaining}\n\n`, { flag: 'w' });

  // Spawn agent
  const child = spawn('claude', ['--print', taskBody], {
    cwd: worktreePath,
    detached: true,
    stdio: ['ignore', 'pipe', 'pipe'],
  });

  session.pid = child.pid;
  session.status = 'running';
  session.started_at = new Date().toISOString();
  session.last_heartbeat = session.started_at;

  child.stdout?.on('data', (data) => {
    writeFileSync(session.output_log, data, { flag: 'a' });
  });
  child.stderr?.on('data', (data) => {
    writeFileSync(session.output_log, `[stderr] ${data}`, { flag: 'a' });
  });
  child.on('exit', (code) => {
    // Reload state (may have changed) and update
    const freshState = loadState();
    const s = freshState.sessions.find(x => x.task_id === taskId);
    if (s) {
      s.status = code === 0 ? 'completed' : 'crashed';
      s.last_heartbeat = new Date().toISOString();
      freshState.sessions = freshState.sessions.filter(x => x.task_id !== taskId);
      freshState[code === 0 ? 'completed' : 'crashed'].push(s);
      saveState(freshState);
    }
  });

  // Move from crashed → active
  state.crashed.splice(idx, 1);
  state.sessions.push(session);
  saveState(state);

  console.log(`✅ Restored ${taskId} (PID: ${child.pid}, retries left: ${session.retries_remaining})`);
  return true;
}

// ── Main ───────────────────────────────────────────────────

function main(): void {
  const args = process.argv.slice(2);
  const force = args.includes('--force');
  const restoreAll = args.includes('--all');
  const taskId = args.find(a => !a.startsWith('--'));

  if (!restoreAll && !taskId) {
    console.error('Usage: subagent-restore <task-id> [--force]');
    console.error('       subagent-restore --all [--force]');
    process.exit(1);
  }

  const state = loadState();

  if (restoreAll) {
    if (state.crashed.length === 0) {
      console.log('✅ No crashed tasks to restore.');
      return;
    }
    console.log(`🔄 Restoring ${state.crashed.length} crashed task(s)...\n`);
    // Copy IDs first since we mutate the array
    const crashedIds = state.crashed.map(s => s.task_id);
    let restored = 0;
    for (const id of crashedIds) {
      if (restoreSession(state, id, force)) restored++;
    }
    console.log(`\n📊 Restored ${restored}/${crashedIds.length} task(s). Run 'subagent-status' to monitor.`);
  } else {
    restoreSession(state, taskId!, force);
  }
}

main();

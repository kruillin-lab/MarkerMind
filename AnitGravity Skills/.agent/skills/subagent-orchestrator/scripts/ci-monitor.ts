#!/usr/bin/env node
/**
 * CI Monitor — Poll GitHub Actions for task branches and auto-react to failures
 *
 * Usage:
 *   subagent-ci                          # One-shot check of all task branches
 *   subagent-ci --watch                  # Continuous polling (every 60s)
 *   subagent-ci --watch --interval 30    # Custom poll interval in seconds
 *   subagent-ci <task-id>                # Check single task's CI
 */

import { execSync } from 'child_process';
import { readFileSync, writeFileSync, existsSync } from 'fs';
import { join } from 'path';
import {
  loadState,
  saveState,
  isProcessAlive,
  statusIcon,
  Session,
  SessionState,
  ANT_DIR,
  LOGS_DIR,
} from './shared';

interface CIRun {
  status: string;           // queued | in_progress | completed
  conclusion: string | null; // success | failure | cancelled | skipped | null
  name: string;
  html_url: string;
  head_branch: string;
  run_number: number;
  updated_at: string;
}

// ── GitHub CLI helpers ─────────────────────────────────────

function ghAvailable(): boolean {
  try {
    execSync('gh --version', { stdio: 'pipe' });
    return true;
  } catch {
    return false;
  }
}

function getRunsForBranch(branch: string): CIRun[] {
  try {
    const raw = execSync(
      `gh run list --branch "${branch}" --limit 3 --json status,conclusion,name,headBranch,htmlUrl,runNumber,updatedAt`,
      { stdio: 'pipe', encoding: 'utf8', timeout: 15000 }
    );
    return JSON.parse(raw).map((r: any) => ({
      status: r.status,
      conclusion: r.conclusion,
      name: r.name,
      html_url: r.htmlUrl,
      head_branch: r.headBranch,
      run_number: r.runNumber,
      updated_at: r.updatedAt,
    }));
  } catch {
    return [];
  }
}

function getFailedLogs(branch: string): string {
  try {
    // Get the latest failed run ID
    const raw = execSync(
      `gh run list --branch "${branch}" --status failure --limit 1 --json databaseId`,
      { stdio: 'pipe', encoding: 'utf8', timeout: 10000 }
    );
    const runs = JSON.parse(raw);
    if (runs.length === 0) return '';

    const runId = runs[0].databaseId;
    const logs = execSync(
      `gh run view ${runId} --log-failed`,
      { stdio: 'pipe', encoding: 'utf8', timeout: 30000 }
    );

    // Truncate to last 100 lines for agent consumption
    return logs.split('\n').slice(-100).join('\n');
  } catch {
    return '(Could not retrieve failure logs)';
  }
}

// ── CI status icon ─────────────────────────────────────────

function ciIcon(run: CIRun): string {
  if (run.status !== 'completed') return '🔄';
  switch (run.conclusion) {
    case 'success': return '✅';
    case 'failure': return '❌';
    case 'cancelled': return '🚫';
    case 'skipped': return '⏭️';
    default: return '❓';
  }
}

// ── Check all task branches ────────────────────────────────

interface CIResult {
  task_id: string;
  branch: string;
  runs: CIRun[];
  failed: boolean;
  passing: boolean;
  pending: boolean;
}

function checkAllBranches(state: SessionState, targetTaskId?: string): CIResult[] {
  const results: CIResult[] = [];

  // Check active + completed sessions
  const allSessions = [...state.sessions, ...state.completed, ...state.crashed];
  const targets = targetTaskId
    ? allSessions.filter(s => s.task_id === targetTaskId)
    : allSessions.filter(s => s.status === 'running' || s.status === 'completed');

  for (const session of targets) {
    const runs = getRunsForBranch(session.branch);
    const latestCompleted = runs.find(r => r.status === 'completed');
    const pending = runs.some(r => r.status === 'in_progress' || r.status === 'queued');

    results.push({
      task_id: session.task_id,
      branch: session.branch,
      runs,
      failed: latestCompleted?.conclusion === 'failure',
      passing: latestCompleted?.conclusion === 'success',
      pending,
    });
  }

  return results;
}

// ── React to CI failures ───────────────────────────────────

function reactToFailure(state: SessionState, result: CIResult): void {
  const session = state.sessions.find(s => s.task_id === result.task_id);
  if (!session) return; // Only react for active sessions

  // Don't inject if agent is already dead
  if (session.pid && !isProcessAlive(session.pid)) return;

  // Write failure context to the worktree so the agent can pick it up
  const failureLog = getFailedLogs(result.branch);
  const ciFixFile = join(session.worktree, '.ant-ci-failure.md');

  const content = `# CI Failure Detected
## Branch: ${result.branch}
## Time: ${new Date().toISOString()}
## Run: ${result.runs[0]?.html_url ?? 'unknown'}

### Failed Logs (last 100 lines)
\`\`\`
${failureLog}
\`\`\`

### Instructions
Fix the CI failures above. Run tests locally before pushing again.
`;

  writeFileSync(ciFixFile, content);
  console.log(`  📝 Wrote CI failure context to ${ciFixFile}`);

  // Also append to the agent's output log
  writeFileSync(session.output_log, `\n[CI-MONITOR] CI failure detected at ${new Date().toISOString()}\n`, { flag: 'a' });
}

// ── Display results ────────────────────────────────────────

function displayResults(results: CIResult[]): void {
  if (results.length === 0) {
    console.log('ℹ️  No task branches to check.');
    return;
  }

  console.log(`\n┌─── CI Status ─────────────────────────────────────────────┐`);

  for (const r of results) {
    const latest = r.runs[0];
    const statusStr = r.passing ? '✅ passing'
      : r.failed ? '❌ FAILED'
      : r.pending ? '🔄 running'
      : '—  no runs';

    console.log(`│  ${r.task_id.padEnd(28)} ${statusStr.padEnd(14)} ${r.branch}`);

    if (latest) {
      console.log(`│    ${ciIcon(latest)} ${latest.name} #${latest.run_number} (${latest.updated_at})`);
      if (latest.html_url) console.log(`│    🔗 ${latest.html_url}`);
    }
  }

  console.log(`└───────────────────────────────────────────────────────────┘\n`);

  const failures = results.filter(r => r.failed);
  if (failures.length > 0) {
    console.log(`⚠️  ${failures.length} branch(es) have CI failures.`);
  }
}

// ── Watch mode ─────────────────────────────────────────────

async function watchMode(intervalSec: number, targetTaskId?: string): Promise<void> {
  console.log(`👁️  Watching CI status every ${intervalSec}s (Ctrl+C to stop)\n`);

  const checkOnce = () => {
    const state = loadState();
    const results = checkAllBranches(state, targetTaskId);
    displayResults(results);

    // Auto-react to failures
    for (const result of results) {
      if (result.failed) {
        reactToFailure(state, result);
      }
    }
  };

  // First check immediately
  checkOnce();

  // Then poll
  setInterval(checkOnce, intervalSec * 1000);
}

// ── Main ───────────────────────────────────────────────────

async function main(): Promise<void> {
  if (!ghAvailable()) {
    console.error('❌ GitHub CLI (gh) is required. Install from https://cli.github.com');
    process.exit(1);
  }

  const args = process.argv.slice(2);
  const watch = args.includes('--watch');
  const intervalIdx = args.indexOf('--interval');
  const interval = intervalIdx !== -1 ? parseInt(args[intervalIdx + 1], 10) : 60;
  const taskId = args.find(a => !a.startsWith('--') && isNaN(Number(a)));

  if (watch) {
    await watchMode(interval, taskId);
  } else {
    const state = loadState();
    const results = checkAllBranches(state, taskId);
    displayResults(results);

    // React to failures
    for (const result of results) {
      if (result.failed) {
        reactToFailure(state, result);
      }
    }
  }
}

main().catch(console.error);

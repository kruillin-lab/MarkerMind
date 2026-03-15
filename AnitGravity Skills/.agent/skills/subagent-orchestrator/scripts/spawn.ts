#!/usr/bin/env node
/**
 * Subagent Spawn - Parallel task spawner for A.N.T. Orchestrator
 * 
 * Usage: subagent-spawn task1.md task2.md [task3.md...]
 */

import { readFileSync, existsSync, mkdirSync, writeFileSync } from 'fs';
import { parse } from 'yaml';
import { join, resolve, basename } from 'path';
import { execSync, spawn } from 'child_process';
import { randomUUID } from 'crypto';

interface TaskConfig {
  task_id: string;
  description: string;
  branch_from?: string;
  priority?: 'low' | 'normal' | 'high' | 'urgent';
  retries?: number;
  ci_reaction?: boolean;
  timeout?: string;
  dependencies?: string[];
}

interface Session {
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

interface SessionState {
  version: string;
  repo: string;
  sessions: Session[];
  completed: Session[];
  crashed: Session[];
}

const ANT_DIR = '.ant';
const SESSIONS_FILE = join(ANT_DIR, 'sessions.json');
const LOGS_DIR = join(ANT_DIR, 'logs');
const WORKTREES_DIR = join(ANT_DIR, 'worktrees');

function initState(repoPath: string): SessionState {
  if (!existsSync(ANT_DIR)) mkdirSync(ANT_DIR, { recursive: true });
  if (!existsSync(LOGS_DIR)) mkdirSync(LOGS_DIR, { recursive: true });
  if (!existsSync(WORKTREES_DIR)) mkdirSync(WORKTREES_DIR, { recursive: true });
  
  if (existsSync(SESSIONS_FILE)) {
    return JSON.parse(readFileSync(SESSIONS_FILE, 'utf8'));
  }
  
  const state: SessionState = {
    version: '1.0',
    repo: repoPath,
    sessions: [],
    completed: [],
    crashed: []
  };
  
  writeFileSync(SESSIONS_FILE, JSON.stringify(state, null, 2));
  return state;
}

function parseTaskFile(filePath: string): { config: TaskConfig; body: string } {
  const content = readFileSync(filePath, 'utf8');
  const match = content.match(/^---\n([\s\S]*?)\n---\n([\s\S]*)$/);
  
  if (!match) {
    throw new Error(`Invalid task file format: ${filePath}`);
  }
  
  const config = parse(match[1]) as TaskConfig;
  const body = match[2].trim();
  
  if (!config.task_id) {
    config.task_id = basename(filePath, '.md');
  }
  
  return { config, body };
}

function priorityValue(p: string): number {
  const map: Record<string, number> = { low: 1, normal: 2, high: 3, urgent: 4 };
  return map[p] || 2;
}

function spawnSubagent(session: Session, taskBody: string): void {
  const logStream = writeFileSync(session.output_log, `# Task: ${session.task_id}\n# Started: ${new Date().toISOString()}\n\n`, { flag: 'w' });
  
  // Create worktree
  const worktreePath = session.worktree;
  const branchName = `task/${session.task_id}`;
  
  try {
    execSync(`git worktree add -b ${branchName} ${worktreePath}`, { stdio: 'pipe' });
  } catch (e) {
    // Branch might exist, try without -b
    execSync(`git worktree add ${worktreePath} ${branchName}`, { stdio: 'pipe' });
  }
  
  // Write task body to worktree
  writeFileSync(join(worktreePath, '.ant-task.md'), taskBody);
  
  // Spawn Claude Code in worktree with task
  const child = spawn('claude', ['--print', taskBody], {
    cwd: worktreePath,
    detached: true,
    stdio: ['ignore', 'pipe', 'pipe']
  });
  
  session.pid = child.pid;
  session.status = 'running';
  session.started_at = new Date().toISOString();
  session.last_heartbeat = session.started_at;
  
  // Pipe output to log
  child.stdout?.on('data', (data) => {
    writeFileSync(session.output_log, data, { flag: 'a' });
  });
  
  child.stderr?.on('data', (data) => {
    writeFileSync(session.output_log, `[stderr] ${data}`, { flag: 'a' });
  });
  
  child.on('exit', (code) => {
    updateSessionStatus(session.task_id, code === 0 ? 'completed' : 'crashed');
  });
  
  console.log(`✓ Spawned ${session.task_id} (PID: ${child.pid})`);
}

function updateSessionStatus(taskId: string, status: Session['status']): void {
  const state = JSON.parse(readFileSync(SESSIONS_FILE, 'utf8'));
  const session = state.sessions.find((s: Session) => s.task_id === taskId);
  
  if (!session) return;
  
  session.status = status;
  session.last_heartbeat = new Date().toISOString();
  
  if (status === 'completed' || status === 'crashed' || status === 'killed') {
    state.sessions = state.sessions.filter((s: Session) => s.task_id !== taskId);
    state[status === 'completed' ? 'completed' : 'crashed'].push(session);
  }
  
  writeFileSync(SESSIONS_FILE, JSON.stringify(state, null, 2));
}

async function main(): Promise<void> {
  const taskFiles = process.argv.slice(2);
  
  if (taskFiles.length === 0) {
    console.error('Usage: subagent-spawn <task1.md> [task2.md] [...]');
    process.exit(1);
  }
  
  const repoPath = process.cwd();
  const state = initState(repoPath);
  
  // Parse all tasks first
  const tasks: { config: TaskConfig; body: string; file: string }[] = [];
  
  for (const file of taskFiles) {
    if (!existsSync(file)) {
      console.error(`❌ Task file not found: ${file}`);
      continue;
    }
    
    try {
      const { config, body } = parseTaskFile(file);
      tasks.push({ config, body, file });
    } catch (e) {
      console.error(`❌ Failed to parse ${file}: ${e}`);
    }
  }
  
  // Sort by priority (higher = first)
  tasks.sort((a, b) => priorityValue(b.config.priority || 'normal') - priorityValue(a.config.priority || 'normal'));
  
  // Check dependencies
  const activeTaskIds = new Set(state.sessions.map((s: Session) => s.task_id));
  const queuedTaskIds = new Set(tasks.map(t => t.config.task_id));
  
  for (const task of tasks) {
    if (task.config.dependencies) {
      for (const dep of task.config.dependencies) {
        if (!activeTaskIds.has(dep) && !queuedTaskIds.has(dep)) {
          console.error(`⚠️  ${task.config.task_id}: Dependency ${dep} not found`);
        }
      }
    }
  }
  
  // Create sessions
  const newSessions: Session[] = tasks.map(task => ({
    task_id: task.config.task_id,
    status: 'queued',
    branch: `task/${task.config.task_id}`,
    worktree: join(WORKTREES_DIR, task.config.task_id),
    retries_remaining: task.config.retries || 2,
    output_log: join(LOGS_DIR, `${task.config.task_id}.log`),
    dependencies: task.config.dependencies || [],
    priority: priorityValue(task.config.priority || 'normal')
  }));
  
  // Add to state
  state.sessions.push(...newSessions);
  writeFileSync(SESSIONS_FILE, JSON.stringify(state, null, 2));
  
  // Spawn tasks that have no dependencies
  for (const task of tasks) {
    const session = newSessions.find(s => s.task_id === task.config.task_id)!;
    
    if (session.dependencies.length === 0) {
      session.status = 'spawning';
      writeFileSync(SESSIONS_FILE, JSON.stringify(state, null, 2));
      
      try {
        spawnSubagent(session, task.body);
      } catch (e) {
        console.error(`❌ Failed to spawn ${task.config.task_id}: ${e}`);
        session.status = 'crashed';
        updateSessionStatus(task.config.task_id, 'crashed');
      }
    } else {
      console.log(`⏳ ${task.config.task_id} queued (waiting for: ${session.dependencies.join(', ')})`);
    }
  }
  
  console.log(`\n📊 Run 'subagent-status' to monitor progress`);
}

main().catch(console.error);

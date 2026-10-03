import { readFileSync } from 'node:fs';

const severities = ['info', 'low', 'moderate', 'high', 'critical'];

export function loadAllowlist(text) {
  const entries = new Map();
  const lines = text.split(/\r?\n/);
  for (let index = 0; index < lines.length; index += 1) {
    const line = lines[index].trim();
    if (line.length === 0 || line.startsWith('#')) {
      continue;
    }

    const match = /^(GHSA-[a-z0-9-]+)\s+(\d{4}-\d{2}-\d{2})\s+(\S.*)$/i.exec(line);
    if (!match) {
      throw new Error(`Invalid npm audit allowlist line ${index + 1}.`);
    }

    entries.set(match[1].toUpperCase(), { until: match[2], reason: match[3].trim() });
  }

  return entries;
}

export function classifyAudit(report, allowlist, level, today) {
  if (!report || typeof report !== 'object' || report.error || !report.vulnerabilities) {
    return { status: 'unusable' };
  }

  const threshold = severities.indexOf(level);
  if (threshold < 0) {
    throw new Error(`Unknown npm audit level "${level}".`);
  }

  const blocking = [];
  const accepted = [];
  const seenBlocking = new Set();
  const seenAccepted = new Set();

  for (const vulnerability of Object.values(report.vulnerabilities)) {
    for (const via of vulnerability.via ?? []) {
      if (typeof via !== 'object' || via === null || typeof via.url !== 'string') {
        continue;
      }

      if (severities.indexOf(via.severity) < threshold) {
        continue;
      }

      const id = ghsaId(via.url);
      const entry = id ? allowlist.get(id) : undefined;
      if (entry && entry.until >= today) {
        if (!seenAccepted.has(id)) {
          seenAccepted.add(id);
          accepted.push({ id, until: entry.until, reason: entry.reason });
        }
        continue;
      }

      const key = id ?? via.url;
      if (seenBlocking.has(key)) {
        continue;
      }

      seenBlocking.add(key);
      blocking.push({
        id: id ?? via.url,
        title: via.title ?? vulnerability.name,
        severity: via.severity,
        url: via.url,
      });
    }
  }

  return { status: blocking.length > 0 ? 'blocking' : 'clear', blocking, accepted };
}

function ghsaId(url) {
  const match = /\/(GHSA-[a-z0-9-]+)$/i.exec(url);
  return match ? match[1].toUpperCase() : null;
}

function utcToday() {
  return process.env.AUDIT_TODAY ?? new Date().toISOString().slice(0, 10);
}

function isDirectRun() {
  const entry = process.argv[1];
  return typeof entry === 'string' && entry.endsWith('npm-audit-filter.mjs');
}

if (isDirectRun()) {
  const reportPath = process.argv[2];
  const allowlistPath = process.argv[3];
  const level = process.argv[4] ?? 'high';
  if (!reportPath || !allowlistPath) {
    console.error('Usage: node npm-audit-filter.mjs <audit.json> <allowlist> [level]');
    process.exit(2);
  }

  let report;
  try {
    report = JSON.parse(readFileSync(reportPath, 'utf8'));
  } catch {
    process.exit(2);
  }

  let allowlist;
  try {
    allowlist = loadAllowlist(readFileSync(allowlistPath, 'utf8'));
  } catch (error) {
    console.error(error instanceof Error ? error.message : 'Invalid npm audit allowlist.');
    process.exit(1);
  }

  const result = classifyAudit(report, allowlist, level, utcToday());
  if (result.status === 'unusable') {
    process.exit(2);
  }

  for (const advisory of result.accepted) {
    console.log(`Accepted npm advisory ${advisory.id} until ${advisory.until} (${advisory.reason}).`);
  }

  if (result.status === 'blocking') {
    for (const advisory of result.blocking) {
      console.error(`${advisory.severity} ${advisory.id}: ${advisory.title}`);
      console.error(advisory.url);
    }
    process.exit(1);
  }
}

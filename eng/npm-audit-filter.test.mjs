import assert from 'node:assert/strict';
import { describe, it } from 'node:test';

import { classifyAudit, loadAllowlist } from './npm-audit-filter.mjs';

const allowlist = loadAllowlist(`
# comment
GHSA-vfj7-8cjw-p6xm 2026-11-03 braces <=3.0.3 has no patched release
`);

function report(extra = {}) {
  return {
    vulnerabilities: {
      braces: {
        via: [
          {
            severity: 'high',
            title: 'braces stack exhaustion',
            url: 'https://github.com/advisories/GHSA-vfj7-8cjw-p6xm',
          },
        ],
      },
      micromatch: { via: ['braces'] },
      ...extra,
    },
  };
}

describe('npm audit allowlist', () => {
  it('accepts the unpatched braces advisory before its review date', () => {
    const result = classifyAudit(report(), allowlist, 'high', '2026-10-03');
    assert.equal(result.status, 'clear');
    assert.deepEqual(
      result.accepted.map((item) => item.id),
      ['GHSA-VFJ7-8CJW-P6XM'],
    );
  });

  it('rejects the braces advisory after its review date', () => {
    const result = classifyAudit(report(), allowlist, 'high', '2026-11-04');
    assert.equal(result.status, 'blocking');
    assert.equal(result.blocking[0].id, 'GHSA-VFJ7-8CJW-P6XM');
  });

  it('still fails when another high advisory is present', () => {
    const result = classifyAudit(
      report({
        example: {
          via: [
            {
              severity: 'critical',
              title: 'example',
              url: 'https://github.com/advisories/GHSA-xxxx-yyyy-zzzz',
            },
          ],
        },
      }),
      allowlist,
      'high',
      '2026-10-03',
    );
    assert.equal(result.status, 'blocking');
    assert.deepEqual(
      result.blocking.map((item) => item.id),
      ['GHSA-XXXX-YYYY-ZZZZ'],
    );
  });

  it('ignores advisories below the audit level and unusable reports', () => {
    const moderate = classifyAudit(
      {
        vulnerabilities: {
          braces: {
            via: [{ severity: 'moderate', title: 'low', url: 'https://github.com/advisories/GHSA-aaaa-bbbb-cccc' }],
          },
        },
      },
      allowlist,
      'high',
      '2026-10-03',
    );
    assert.equal(moderate.status, 'clear');
    assert.equal(classifyAudit({ error: { code: 'EAI_AGAIN' } }, allowlist, 'high', '2026-10-03').status, 'unusable');
  });
});

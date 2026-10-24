import { DailyEntry } from '../../../core/services/daily-entry.service';
import {
  buildDailyEntryTreeRows,
  matchesDailyEntrySearch,
  normalizeDailyEntrySearchQuery
} from './daily-entry-tree.util';

function entry(overrides: Partial<DailyEntry> = {}): DailyEntry {
  return {
    id: overrides.id ?? 'e1',
    siteId: 's1',
    entryType: 'aavak',
    entryDate: '2026-01-01',
    mainLedgerId: overrides.mainLedgerId ?? 'm1',
    mainLedgerName: overrides.mainLedgerName ?? 'Construction',
    subLedgerId: overrides.subLedgerId ?? 's1',
    subLedgerName: overrides.subLedgerName ?? 'Cement',
    amount: overrides.amount ?? 1500000,
    cashBank: 'Cash',
    description: overrides.description ?? ''
  };
}

describe('normalizeDailyEntrySearchQuery', () => {
  it('strips Rs and commas', () => {
    expect(normalizeDailyEntrySearchQuery('  Rs. 1,500  ')).toBe('1500');
  });
});

describe('matchesDailyEntrySearch', () => {
  it('matches main and sub ledger names', () => {
    const e = entry();
    expect(matchesDailyEntrySearch(e, 'const')).toBeTrue();
    expect(matchesDailyEntrySearch(e, 'cem')).toBeTrue();
    expect(matchesDailyEntrySearch(e, 'xyz')).toBeFalse();
  });

  it('matches stored and ÷1000 display amounts', () => {
    const e = entry({ amount: 1500000 });
    expect(matchesDailyEntrySearch(e, '1500000')).toBeTrue();
    expect(matchesDailyEntrySearch(e, '1500')).toBeTrue();
  });
});

describe('buildDailyEntryTreeRows', () => {
  const entries = [
    entry({ id: 'a', mainLedgerId: 'm1', mainLedgerName: 'Construction', subLedgerName: 'Cement', amount: 1000 }),
    entry({ id: 'b', mainLedgerId: 'm1', mainLedgerName: 'Construction', subLedgerName: 'Steel', amount: 2000 }),
    entry({ id: 'c', mainLedgerId: 'm2', mainLedgerName: 'Bank', subLedgerName: 'HDFC', amount: 5000 })
  ];

  it('shows collapsed main rows with totals when nothing expanded', () => {
    const rows = buildDailyEntryTreeRows(entries, '', new Set());
    expect(rows.length).toBe(2);
    expect(rows[0]).toEqual(jasmine.objectContaining({
      kind: 'main',
      mainLedgerId: 'm1',
      totalAmount: 3000,
      expanded: false
    }));
    expect(rows[1]).toEqual(jasmine.objectContaining({
      kind: 'main',
      mainLedgerId: 'm2',
      totalAmount: 5000,
      expanded: false
    }));
  });

  it('includes entry children when main is expanded', () => {
    const rows = buildDailyEntryTreeRows(entries, '', new Set(['m1']));
    expect(rows.map((r) => r.kind)).toEqual(['main', 'entry', 'entry', 'main']);
    expect(rows[1].kind === 'entry' && rows[1].entry.id).toBe('a');
    expect(rows[2].kind === 'entry' && rows[2].entry.id).toBe('b');
  });

  it('filters by search and auto-expands matching parents', () => {
    const rows = buildDailyEntryTreeRows(entries, 'HDFC', new Set());
    expect(rows.length).toBe(2);
    expect(rows[0]).toEqual(jasmine.objectContaining({
      kind: 'main',
      mainLedgerId: 'm2',
      expanded: true
    }));
    expect(rows[1].kind === 'entry' && rows[1].entry.subLedgerName).toBe('HDFC');
  });
});

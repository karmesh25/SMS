import { DailyEntry } from '../../../core/services/daily-entry.service';
import { formatDisplayAmount } from '../../../shared/pipes/indian-currency.pipe';

export type DailyEntryTreeRow =
  | {
      kind: 'main';
      mainLedgerId: string;
      mainLedgerName: string;
      totalAmount: number;
      expanded: boolean;
    }
  | { kind: 'entry'; entry: DailyEntry; parentId: string };

export function normalizeDailyEntrySearchQuery(query: string): string {
  return query
    .trim()
    .toLowerCase()
    .replace(/rs\.?/gi, '')
    .replace(/,/g, '')
    .trim();
}

export function matchesDailyEntrySearch(entry: DailyEntry, query: string): boolean {
  const q = normalizeDailyEntrySearchQuery(query);
  if (!q) return true;

  const main = (entry.mainLedgerName ?? '').toLowerCase();
  const sub = (entry.subLedgerName ?? '').toLowerCase();
  if (main.includes(q) || sub.includes(q)) return true;

  const amountRaw = String(entry.amount);
  const amountDisplay = formatDisplayAmount(entry.amount);
  return amountRaw.includes(q) || amountDisplay.includes(q);
}

/** Groups filtered entries by main ledger; expands all parents while searching. */
export function buildDailyEntryTreeRows(
  entries: DailyEntry[],
  query: string,
  expandedIds: ReadonlySet<string>
): DailyEntryTreeRow[] {
  const filtered = entries.filter((e) => matchesDailyEntrySearch(e, query));
  const groups = new Map<string, { name: string; items: DailyEntry[] }>();

  for (const entry of filtered) {
    let group = groups.get(entry.mainLedgerId);
    if (!group) {
      group = { name: entry.mainLedgerName, items: [] };
      groups.set(entry.mainLedgerId, group);
    }
    group.items.push(entry);
  }

  const searching = normalizeDailyEntrySearchQuery(query).length > 0;
  const rows: DailyEntryTreeRow[] = [];

  for (const [mainLedgerId, group] of groups) {
    const expanded = searching || expandedIds.has(mainLedgerId);
    const totalAmount = group.items.reduce((sum, e) => sum + Number(e.amount || 0), 0);
    rows.push({
      kind: 'main',
      mainLedgerId,
      mainLedgerName: group.name,
      totalAmount,
      expanded
    });

    if (expanded) {
      for (const entry of group.items) {
        rows.push({ kind: 'entry', entry, parentId: mainLedgerId });
      }
    }
  }

  return rows;
}

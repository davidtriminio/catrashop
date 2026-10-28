/** A numbered page or a gap ("…") in a compact pager. */
export type PageItem = number | '…';

/**
 * Builds a compact pager: always the first and last page, the current page with one neighbour on each side, and
 * gaps in between, e.g. 1 … 4 5 6 … 27. With few pages it simply lists them all.
 */
export function buildPages(current: number, totalPages: number): PageItem[] {
  if (totalPages <= 1) return totalPages === 1 ? [1] : [];
  const wanted = new Set<number>([1, totalPages, current - 1, current, current + 1]);
  const pages = [...wanted].filter((p) => p >= 1 && p <= totalPages).sort((a, b) => a - b);

  const items: PageItem[] = [];
  let previous = 0;
  for (const page of pages) {
    if (page - previous === 2) items.push(previous + 1); // a single missing page is cheaper to show than "…"
    else if (page - previous > 2) items.push('…');
    items.push(page);
    previous = page;
  }
  return items;
}

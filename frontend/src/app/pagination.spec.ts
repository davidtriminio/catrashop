import { buildPages } from '@ch/util/pagination';

describe('buildPages', () => {
  it('returns nothing without pages and a single page for one', () => {
    expect(buildPages(1, 0)).toEqual([]);
    expect(buildPages(1, 1)).toEqual([1]);
  });

  it('lists every page when there are few', () => {
    expect(buildPages(2, 4)).toEqual([1, 2, 3, 4]);
  });

  it('collapses distant pages into gaps around the current page', () => {
    expect(buildPages(1, 27)).toEqual([1, 2, '…', 27]);
    expect(buildPages(14, 27)).toEqual([1, '…', 13, 14, 15, '…', 27]);
    expect(buildPages(27, 27)).toEqual([1, '…', 26, 27]);
  });

  it('shows a single missing page instead of a gap', () => {
    expect(buildPages(4, 8)).toEqual([1, 2, 3, 4, 5, '…', 8]);
  });
});

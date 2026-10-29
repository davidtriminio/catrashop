import { HONDURAS_DEPARTMENTS } from '@ch/util/departments';

describe('Honduras departments', () => {
  it('lists the 18 departments with unique ISO 3166-2 codes and names', () => {
    expect(HONDURAS_DEPARTMENTS).toHaveLength(18);
    expect(new Set(HONDURAS_DEPARTMENTS.map((d) => d.code)).size).toBe(18);
    expect(new Set(HONDURAS_DEPARTMENTS.map((d) => d.name)).size).toBe(18);
    for (const d of HONDURAS_DEPARTMENTS) expect(d.code).toMatch(/^HN-[A-Z]{2}$/);
  });
});

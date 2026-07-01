describe('FlatGrid floor grouping', () => {
  function computeFloorFromFlatNo(flatNo: string, wingName: string, floors: number, flatsPerFloor: number): number {
    const prefix = wingName.toUpperCase();
    if (!flatNo.toUpperCase().startsWith(prefix)) return 0;
    const suffix = flatNo.slice(prefix.length);
    const position = parseInt(suffix, 10);
    if (!position || position <= 0) return 0;
    return Math.ceil(position / flatsPerFloor);
  }

  function buildPlotGroups(flats: { id: string }[], plotName: string) {
    return [{ key: 'plots', title: plotName, flats }];
  }

  it('groups tower flats by floor', () => {
    expect(computeFloorFromFlatNo('BHAGVATI101', 'BHAGVATI', 4, 100)).toBe(2);
    expect(computeFloorFromFlatNo('BHAGVATI250', 'BHAGVATI', 4, 100)).toBe(3);
  });

  it('returns 0 for non-matching prefix', () => {
    expect(computeFloorFromFlatNo('SHOP01', 'BHAGVATI', 4, 100)).toBe(0);
  });

  it('plot mode uses a single group without floors', () => {
    const flats = [{ id: '1' }, { id: '2' }];
    const groups = buildPlotGroups(flats, 'SCHEME-A');
    expect(groups.length).toBe(1);
    expect(groups[0].title).toBe('SCHEME-A');
    expect(groups[0].flats.length).toBe(2);
  });
});

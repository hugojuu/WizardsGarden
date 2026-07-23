namespace WizardGarden.Core
{
    /// <summary>
    /// 작업대에 투입된 재료 한 묶음. 매칭 엔진은 여러 묶음의 조성 합·id별 개수·총 가치를 집계한다.
    /// unitComposition·unitValue는 1개 기준 값이며 count만큼 곱해 합산된다.
    /// </summary>
    public readonly struct BrewInputItem
    {
        public readonly string ItemId;
        public readonly ElementComposition UnitComposition;
        public readonly int UnitValue;
        public readonly int Count;

        public BrewInputItem(string itemId, ElementComposition unitComposition, int unitValue, int count)
        {
            ItemId = itemId;
            UnitComposition = unitComposition;
            UnitValue = unitValue;
            Count = count;
        }
    }
}

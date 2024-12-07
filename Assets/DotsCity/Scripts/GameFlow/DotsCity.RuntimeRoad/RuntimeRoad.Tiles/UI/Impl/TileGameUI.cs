using System.Collections.Generic;
using UnityEngine;

namespace Spirit604.DotsCity.RuntimeRoad
{
    public class TileGameUI : TileGameUIBase
    {
        [SerializeField] private TileGridView tileGridView;
        [SerializeField] private ModePanel modePanel;
        [SerializeField] private PagePanel pagePanel;

        protected virtual void Awake()
        {
            pagePanel.OnPackageClicked += OnPageSelectedInternal;
            tileGridView.OnTileSelected += OnTileSelectedInternal;
            tileGridView.OnVariantSelected += OnVariantSelectedInternal;
            tileGridView.OnVariantPopupOpened += OnPopupOpenedInternal;
            modePanel.OnModeClicked += OnModeSelectedInternal;
        }

        public override void InitPages(int pageCount)
        {
            pagePanel.Init(pageCount);
        }

        public override void Populate(IList<PrefabData> tilePrefabs)
        {
            tileGridView.Populate(tilePrefabs);
        }

        public override void SetVariant(PrefabType prefabType, int variant)
        {
            tileGridView.SetVariant(prefabType, variant);
        }
    }
}

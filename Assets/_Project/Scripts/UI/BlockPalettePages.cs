using UnityEngine;
using UnityEngine.UI;

namespace Cubely.Blocks.Showcase
{
    /// <summary>Pages through the cards created by the palette, without scrolling or respawning them.</summary>
    public class BlockPalettePages : MonoBehaviour
    {
        [SerializeField, Min(1)] private int cardsPerPage = 3;
        [SerializeField] private Button previousButton;
        [SerializeField] private Button nextButton;
        [SerializeField] private Text pageLabel;

        private RectTransform content;
        private BlockPaletteCard[] cards;
        private int page;

        public void Initialize(BlockPaletteCard[] cardInstances, RectTransform cardContent)
        {
            cards = cardInstances;
            content = cardContent;
            page = 0;
            RefreshPage();
        }

        private void OnEnable() => RefreshPage();

        public void PreviousPage()
        {
            page--;
            RefreshPage();
        }

        public void NextPage()
        {
            page++;
            RefreshPage();
        }

        private void RefreshPage()
        {
            // Controller initialization is explicit; component Awake/OnEnable ordering does not matter.
            if (cards == null) return;
            int size = Mathf.Max(1, cardsPerPage);
            int pageCount = Mathf.Max(1, (cards.Length + size - 1) / size);
            page = Mathf.Clamp(page, 0, pageCount - 1);
            int first = page * size;
            for (int i = 0; i < cards.Length; i++)
                cards[i].gameObject.SetActive(i >= first && i < first + size);

            previousButton.interactable = page > 0;
            nextButton.interactable = page < pageCount - 1;
            pageLabel.text = $"{page + 1} / {pageCount}";
            LayoutRebuilder.MarkLayoutForRebuild(content);
        }
    }
}

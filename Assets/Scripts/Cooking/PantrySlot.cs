using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace CookingSystem
{
    [DisallowMultipleComponent]
    public class PantrySlot : MonoBehaviour
    {
        [Header("Restock Configuration")]
        [Tooltip("The ingredient GameObject/Prefab to instantiate when restocking.")]
        [SerializeField] private GameObject ingredientPrefab;

        [Tooltip("Time in seconds to restock a new ingredient after one is consumed.")]
        [SerializeField] private float restockDuration = 3f;

        [Header("Visual Feedback")]
        [Tooltip("Optional UI fill image showing cooldown progress (0 to 1).")]
        [SerializeField] private Image cooldownOverlay;

        [Tooltip("Optional text showing restock countdown.")]
        [SerializeField] private TextMeshProUGUI cooldownText;

        private DraggableIngredient currentIngredient;
        private Coroutine restockCoroutine;

        public bool IsRestocking => restockCoroutine != null;
        public DraggableIngredient CurrentIngredient => currentIngredient;

        private void Start()
        {
            // If an ingredient is already a child of this slot, bind to it
            if (currentIngredient == null)
            {
                var existing = GetComponentInChildren<DraggableIngredient>();
                if (existing != null)
                {
                    BindIngredient(existing);
                }
                else if (ingredientPrefab != null)
                {
                    SpawnIngredient();
                }
            }

            SetCooldownVisuals(false, 0f, string.Empty);
        }

        public void SetIngredientPrefab(GameObject prefab)
        {
            ingredientPrefab = prefab;
        }

        public void BindIngredient(DraggableIngredient ingredient)
        {
            if (currentIngredient != null)
            {
                currentIngredient.onAccepted -= HandleIngredientConsumed;
            }

            currentIngredient = ingredient;
            if (currentIngredient != null)
            {
                currentIngredient.onAccepted += HandleIngredientConsumed;
            }
        }

        private void HandleIngredientConsumed(DraggableIngredient ingredient)
        {
            if (ingredient == currentIngredient)
            {
                currentIngredient.onAccepted -= HandleIngredientConsumed;
                currentIngredient = null;
            }

            StartRestock();
        }

        public void StartRestock()
        {
            if (restockCoroutine != null)
            {
                StopCoroutine(restockCoroutine);
            }

            restockCoroutine = StartCoroutine(RestockRoutine());
        }

        private IEnumerator RestockRoutine()
        {
            float elapsed = 0f;
            SetCooldownVisuals(true, 1f, $"{restockDuration:F1}s");

            while (elapsed < restockDuration)
            {
                elapsed += Time.deltaTime;
                float remaining = Mathf.Max(0f, restockDuration - elapsed);
                float progress = 1f - Mathf.Clamp01(elapsed / restockDuration);

                SetCooldownVisuals(true, progress, $"{remaining:F1}s");
                yield return null;
            }

            SpawnIngredient();
            SetCooldownVisuals(false, 0f, string.Empty);
            restockCoroutine = null;
        }

        private void SpawnIngredient()
        {
            if (ingredientPrefab == null) return;

            GameObject newObj = Instantiate(ingredientPrefab, transform);
            newObj.name = ingredientPrefab.name;

            var rect = newObj.GetComponent<RectTransform>();
            if (rect != null)
            {
                rect.anchoredPosition = new Vector2(0, 10);
                rect.localScale = Vector3.one;
            }

            var draggable = newObj.GetComponent<DraggableIngredient>();
            if (draggable != null)
            {
                BindIngredient(draggable);
            }
        }

        private void SetCooldownVisuals(bool active, float fill, string text)
        {
            if (cooldownOverlay != null)
            {
                cooldownOverlay.gameObject.SetActive(active);
                cooldownOverlay.fillAmount = fill;
            }

            if (cooldownText != null)
            {
                cooldownText.gameObject.SetActive(active);
                cooldownText.text = text;
            }
        }

        private void OnDestroy()
        {
            if (currentIngredient != null)
            {
                currentIngredient.onAccepted -= HandleIngredientConsumed;
            }
        }
    }
}

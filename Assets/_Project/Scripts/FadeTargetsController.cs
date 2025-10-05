using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[DisallowMultipleComponent]
public class SimpleFadeToggle : MonoBehaviour
{
    public enum ActionType { Appear, Disappear }

    [System.Serializable]
    public class Item
    {
        public GameObject Target;
        public ActionType Action = ActionType.Appear;

        [System.NonSerialized] public CanvasGroup RuntimeCanvasGroup;
        [System.NonSerialized] public Coroutine RunningCoroutine;
    }

    [Header("Items")]
    [SerializeField] private List<Item> _items = new List<Item>();

    [Header("Fade settings")]
    [Tooltip("Общая длительность фейда для появления и исчезновения (в секундах)")]
    [SerializeField] private float FadeDuration = 0.2f;

    private readonly AnimationCurve _curve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Button")]
    [Tooltip("Перетащите кнопку вручную, если хотите, чтобы она вызывала ApplyAll")]
    [SerializeField] private Button ManualButton; // если null — автоподвязки нет

    private void Awake()
    {
        if (ManualButton != null)
        {
            ManualButton.onClick.AddListener(OnPressed);
        }

        for (int i = 0; i < _items.Count; i++)
        {
            EnsureCanvasGroup(_items[i]);
        }
    }

    private void EnsureCanvasGroup(Item item)
    {
        if (item == null || item.Target == null) return;
        CanvasGroup cg = item.Target.GetComponent<CanvasGroup>();
        if (cg == null)
        {
            cg = item.Target.AddComponent<CanvasGroup>();
            cg.interactable = true;
            cg.blocksRaycasts = true;
        }
        item.RuntimeCanvasGroup = cg;
    }

    public void OnPressed()
    {
        ApplyAll();
    }

    public void ApplyAll()
    {
        for (int i = 0; i < _items.Count; i++)
        {
            StartItem(i);
        }
    }

    private void StartItem(int index)
    {
        if (index < 0 || index >= _items.Count) return;
        Item item = _items[index];
        if (item == null || item.Target == null) return;

        EnsureCanvasGroup(item);

        if (item.RunningCoroutine != null)
        {
            StopCoroutine(item.RunningCoroutine);
            item.RunningCoroutine = null;
        }

        item.RunningCoroutine = StartCoroutine(HandleFade(item));
    }

    private IEnumerator HandleFade(Item item)
    {
        CanvasGroup cg = item.RuntimeCanvasGroup ?? item.Target.GetComponent<CanvasGroup>();
        float dur = Mathf.Max(0.0001f, FadeDuration);

        if (item.Action == ActionType.Appear)
        {
            if (!item.Target.activeSelf) item.Target.SetActive(true);
            if (cg == null)
            {
                yield break;
            }

            cg.alpha = 0f;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float n = Mathf.Clamp01(t / dur);
                cg.alpha = Mathf.Lerp(0f, 1f, _curve.Evaluate(n));
                yield return null;
            }
            cg.alpha = 1f;
        }
        else // Disappear
        {
            if (cg == null)
            {
                item.Target.SetActive(false);
                yield break;
            }

            float start = cg.alpha;
            float t = 0f;
            while (t < dur)
            {
                t += Time.unscaledDeltaTime;
                float n = Mathf.Clamp01(t / dur);
                cg.alpha = Mathf.Lerp(start, 0f, _curve.Evaluate(n));
                yield return null;
            }
            cg.alpha = 0f;
            item.Target.SetActive(false);
        }

        item.RunningCoroutine = null;
    }

    // Программный API
    public void SetActionForIndex(int index, ActionType action)
    {
        if (index < 0 || index >= _items.Count) return;
        _items[index].Action = action;
    }

    public void SetTargetAt(int index, GameObject go)
    {
        if (index < 0) return;
        while (_items.Count <= index) _items.Add(new Item());
        _items[index].Target = go;
        EnsureCanvasGroup(_items[index]);
    }
}
using System.Collections;
using UnityEngine;
using TMPro;
using System.Text;

public class SpriteNumberText : MonoBehaviour
{
    [Header("TMP Reference")]
    [SerializeField] private TextMeshProUGUI textUI;

    [Header("Animation")]
    [SerializeField] private double animationDuration = 1.5;
    [SerializeField] private AnimationCurve animationCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private Coroutine animCoroutine;
    private double currentValue;

    // ─────────────────────────────────────────────
    // PUBLIC FUNCTIONS
    // ─────────────────────────────────────────────

    public void SetNumber(double value)
    {
        StopAnim();
        currentValue = value;
        textUI.text = ConvertToSpriteText(value);
    }

    public void AnimateFromZero(double target)
    {
        StopAnim();
        currentValue = 0;
        animCoroutine = StartCoroutine(AnimateRoutine(target));
    }

    // ─────────────────────────────────────────────
    // ANIMATION
    // ─────────────────────────────────────────────

    private IEnumerator AnimateRoutine(double target)
    {
        double time = 0;

        int decimals = GetDecimalPlaces(target);
        double step = System.Math.Pow(10, -decimals); // 0.1, 0.01, etc.

        while (time < animationDuration)
        {
            time += Time.deltaTime;

            double t = time / animationDuration;
            if (t > 1) t = 1;

            float curveT = animationCurve.Evaluate((float)t);

            double raw = LerpDouble(0, target, curveT);

            // 🔥 SNAP to step (this is the key fix)
            currentValue = System.Math.Floor(raw / step) * step;

            textUI.text = ConvertToSpriteText(currentValue);

            yield return null;
        }

        currentValue = target;
        textUI.text = ConvertToSpriteText(target);
        animCoroutine = null;
    }

    // ─────────────────────────────────────────────
    // DOUBLE LERP
    // ─────────────────────────────────────────────

    private double LerpDouble(double a, double b, double t)
    {
        return a + (b - a) * t;
    }

    // ─────────────────────────────────────────────
    // DETECT DECIMAL PLACES FROM TARGET
    // ─────────────────────────────────────────────

    private int GetDecimalPlaces(double value)
    {
        string s = value.ToString();
        int index = s.IndexOf('.');

        if (index < 0) return 0;

        int decimals = s.Length - index - 1;

        // Clamp max 3 decimals
        return Mathf.Min(decimals, 3);
    }

    // ─────────────────────────────────────────────
    // NUMBER → SPRITE TEXT
    // ─────────────────────────────────────────────

    private string ConvertToSpriteText(double value)
    {
        string number = FormatNumber(value);

        StringBuilder sb = new StringBuilder();

        foreach (char c in number)
        {
            if (c >= '0' && c <= '9')
            {
                sb.Append($"<sprite index={c - '0'}>");
            }
            else if (c == '.')
            {
                sb.Append("<sprite index=10>");
            }
        }

        return sb.ToString();
    }

    // ─────────────────────────────────────────────
    // FORMAT (NO TRAILING ZEROS, MAX 3 DECIMALS)
    // ─────────────────────────────────────────────

    private string FormatNumber(double value)
    {
        double rounded = System.Math.Round(value, 3);
        return rounded.ToString("0.###");
    }

    // ─────────────────────────────────────────────
    // CLEANUP
    // ─────────────────────────────────────────────

    private void StopAnim()
    {
        if (animCoroutine != null)
        {
            StopCoroutine(animCoroutine);
            animCoroutine = null;
        }
    }
}
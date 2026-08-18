using System;
using DG.Tweening;
using UnityEngine;
using UnityEngine.UI;

// Owns the whole win celebration: the counting text plus, from tier 2 up, the big-win stage
// (glow + label + darkening blocker + character slide-in + coin fountain).
//
// The tiers are staged off the counting text's NORMALIZED progress rather than off timers, so a
// tier's look stays the same no matter what lerpDuration is dialled in. Tier 1 is text-only and
// never blocks the spin flow; tiers 2/3 hold the flow until they finish or the player skips.
//
// Everything is cached and pre-hidden in Awake, and every tween is stored so it can be killed —
// a skip or a fresh spin must always be able to slam the whole thing back to its resting state.
public class WinCelebrationController : MonoBehaviour
{
  // Floor on lerpDuration: a misconfigured 0 would skip the per-frame setter and with it the
  // stage triggers. Applied wherever the duration is read so callers all agree on the value.
  private const float MinLerpDuration = 0.01f;

  [Serializable]
  private class TierSettings
  {
    [Tooltip("How long the amount takes to count from 0 to the win.")]
    public float lerpDuration = 0.8f;
    [Tooltip("How long the celebration stays on screen after the count finishes.")]
    public float holdDuration = 2f;

    [Tooltip("Tier 1 leaves this off — text lerp only, no blocker.")]
    public bool showBigWin = false;
    [Range(0f, 1f)]
    [Tooltip("Count progress at which the big-win stage appears.")]
    public float bigWinAt = 0.4f;

    [Tooltip("Only tier 3 swaps BIG WIN out for MEGA WIN.")]
    public bool showMegaWin = false;
    [Range(0f, 1f)]
    [Tooltip("Count progress at which BIG WIN cross-scales into MEGA WIN.")]
    public float megaWinAt = 0.55f;

    [Tooltip("Shapes the count — the stage thresholds above still read raw (linear) progress.")]
    public AnimationCurve lerpCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Tooltip("Clip key played as the tier starts: spin/win/lose/spinStop/megaWin/phone. Leave empty for silence.")]
    public string audioKey = "";
    [Tooltip("Extra clip layered in at the MEGA WIN swap, on top of audioKey. Leave empty for none; never plays if the tier is skipped before megaWinAt.")]
    public string megaWinAudioKey = "";
  }

  [Header("Root")]
  [SerializeField] private CanvasGroup root;
  [SerializeField] private SpriteNumberText winText;
  [SerializeField] private float rootFadeInDuration = 0.15f;
  [SerializeField] private float resetFadeDuration = 0.3f;

  [Header("Big Win Stage")]
  [SerializeField] private RectTransform glowBG;
  [SerializeField] private RectTransform bigWinLabel;
  [SerializeField] private RectTransform megaWinLabel;
  [SerializeField] private float glowRotateDuration = 8f;
  [SerializeField] private float labelScaleUpDuration = 0.35f;
  [SerializeField] private float labelScaleDownDuration = 0.25f;
  [SerializeField] private Ease labelScaleUpEase = Ease.OutBack;
  [SerializeField] private Ease labelScaleDownEase = Ease.InBack;

  [Header("Skip Blocker")]
  // The darkening Image and the skip Button live on the same GameObject, so one reference covers
  // both: the Image is pulled off the Button in Awake. Give that Image a fully opaque colour —
  // blockerAlpha below is what drives the dim, and dimming the colour too would multiply out.
  [SerializeField] private Button skipButton;
  [SerializeField][Range(0f, 1f)] private float blockerAlpha = 0.4f;
  [SerializeField] private float blockerFadeDuration = 0.25f;

  [Header("Character")]
  // The character rides between two marker RectTransforms in LOCAL space, so all three need the
  // same parent for their localPositions to be comparable. Both markers are read once in Awake.
  [SerializeField] private RectTransform character;       // parent of the character/extra image children
  [SerializeField] private RectTransform characterPointA; // off-screen resting spot, also the reset target
  [SerializeField] private RectTransform characterPointB; // on-screen target
  [SerializeField] private float characterMoveInDuration = 0.5f;
  [SerializeField] private float characterMoveOutDuration = 0.35f;
  [SerializeField] private Ease characterMoveInEase = Ease.OutBack;
  [SerializeField] private Ease characterMoveOutEase = Ease.InBack;

  [Header("Coin Fountain")]
  [SerializeField] private CoinFountainPool coinFountain;
  [SerializeField] private float coinFadeOutDuration = 0.3f;

  [Header("Audio")]
  [SerializeField] private AudioController audioController;

  [Header("Debug")]
  [Tooltip("Play mode only: 1/2/3 play that tier with the amount below, 4 skips. Leave OFF for builds.")]
  [SerializeField] private bool debugKeys = false;
  [SerializeField] private double debugTestAmount = 123.456;

  [Header("Tiers")]
  [SerializeField] private TierSettings tier1;
  [SerializeField] private TierSettings tier2;
  [SerializeField] private TierSettings tier3;

  private Tween _rootFadeTween;
  private Tween _countTween;
  private Tween _holdTween;
  private Tween _blockerTween;
  private Tween _characterTween;
  private Tween _glowSpin;
  private Tween _glowScaleTween;
  private Tween _bigWinTween;
  private Tween _megaWinTween;
  private Sequence _outroSeq;

  private Image _blockerImage;       // the darkening Image, shares a GameObject with skipButton
  private Vector3 _characterPointALocal; // captured before anything moves
  private Vector3 _characterPointBLocal;
  private double _targetAmount;
  private Action _onFinished;
  private bool _isPlaying;
  private bool _bigWinShown;
  private bool _megaWinShown;

  internal bool IsPlaying => _isPlaying;

  private void Awake()
  {
    if (characterPointA) _characterPointALocal = characterPointA.localPosition;
    if (characterPointB) _characterPointBLocal = characterPointB.localPosition;

    if (skipButton)
    {
      _blockerImage = skipButton.GetComponent<Image>();
      skipButton.onClick.RemoveAllListeners();
      skipButton.onClick.AddListener(Skip);
    }

    HardReset();
  }

  private void OnDestroy()
  {
    KillAllTweens();
  }

  // ─────────────────────────────────────────────
  // PUBLIC API
  // ─────────────────────────────────────────────

  // tier: 1 = text only (non-blocking), 2 = big win, 3 = big win then mega win.
  // onFinished fires once, when the celebration starts fading out — callers use it to unblock the
  // spin flow, so it is also fired by HardReset to make a stuck spin impossible.
  internal void Play(int tier, double amount, Action onFinished = null)
  {
    TierSettings cfg = GetTier(tier);
    if (cfg == null)
    {
      Debug.LogWarning("WinCelebrationController: unknown tier " + tier);
      onFinished?.Invoke();
      return;
    }

    HardReset(); // never layer two celebrations

    _targetAmount = amount;
    _onFinished = onFinished;
    _isPlaying = true;
    _bigWinShown = false;
    _megaWinShown = false;

    if (winText) winText.SetNumber(0);

    PlayAudio(cfg.audioKey);

    if (root)
    {
      root.alpha = 0f;
      // Has to be true while playing: a parent CanvasGroup with blocksRaycasts off would swallow
      // the blocker button's clicks. Every other child is Raycast Target OFF, so nothing else
      // catches input, and the blocker Image's own raycastTarget gates when the skip is clickable.
      root.blocksRaycasts = true;
      _rootFadeTween = root.DOFade(1f, rootFadeInDuration).SetLink(gameObject);
    }

    // Drive a normalized 0->1 value rather than the amount itself: the stage thresholds need raw
    // progress, and the display curve is applied on top of it.
    float p = 0f;
    _countTween = DOTween.To(() => p, v => { p = v; OnCountProgress(v, cfg); }, 1f, Mathf.Max(MinLerpDuration, cfg.lerpDuration))
      .SetEase(Ease.Linear)
      .SetLink(gameObject)
      .OnComplete(() => OnCountComplete(cfg));
  }

  // How long this tier's count takes. The UI's own total-win counter reads this so both numbers
  // land on the final amount together. Returns -1 for an unknown tier so callers can fall back.
  internal float GetLerpDuration(int tier)
  {
    TierSettings cfg = GetTier(tier);
    return cfg == null ? -1f : Mathf.Max(MinLerpDuration, cfg.lerpDuration);
  }

  // Called on every spin. Gracefully skips a running celebration, otherwise just slams state clean.
  internal void SkipAndReset()
  {
    if (_isPlaying) Skip();
    else HardReset();
  }

  // ─────────────────────────────────────────────
  // COUNT + STAGING
  // ─────────────────────────────────────────────

  private void OnCountProgress(float p, TierSettings cfg)
  {
    if (winText) winText.SetNumber(_targetAmount * cfg.lerpCurve.Evaluate(p));

    if (cfg.showBigWin && !_bigWinShown && p >= cfg.bigWinAt) EnterBigWinStage();
    if (cfg.showMegaWin && !_megaWinShown && p >= cfg.megaWinAt) SwapToMegaWin(cfg);
  }

  private void OnCountComplete(TierSettings cfg)
  {
    _countTween = null;
    if (winText) winText.SetNumber(_targetAmount);
    _holdTween = DOVirtual.DelayedCall(cfg.holdDuration, BeginOutro).SetLink(gameObject);
  }

  // Tiers 2 and 3 both pass through here — the blocker, character and coin fountain all belong to
  // "a big win happened", not to a specific tier.
  private void EnterBigWinStage()
  {
    _bigWinShown = true;

    if (glowBG)
    {
      glowBG.gameObject.SetActive(true);
      glowBG.localScale = Vector3.zero;
      _glowScaleTween = glowBG.DOScale(Vector3.one, labelScaleUpDuration)
        .SetEase(labelScaleUpEase).SetLink(gameObject);
      glowBG.localRotation = Quaternion.identity;
      _glowSpin = glowBG.DOLocalRotate(new Vector3(0f, 0f, -360f), glowRotateDuration, RotateMode.FastBeyond360)
        .SetLoops(-1, LoopType.Restart).SetEase(Ease.Linear).SetLink(gameObject);
    }

    if (bigWinLabel)
    {
      bigWinLabel.gameObject.SetActive(true);
      bigWinLabel.localScale = Vector3.zero;
      _bigWinTween = bigWinLabel.DOScale(Vector3.one, labelScaleUpDuration)
        .SetEase(labelScaleUpEase).SetLink(gameObject);
    }

    if (_blockerImage)
    {
      _blockerImage.gameObject.SetActive(true);
      SetBlockerAlpha(0f); // always fade in fresh
      _blockerImage.raycastTarget = true;
      _blockerTween = _blockerImage.DOFade(blockerAlpha, blockerFadeDuration).SetLink(gameObject);
    }

    if (character && characterPointB)
      _characterTween = character.DOLocalMove(_characterPointBLocal, characterMoveInDuration)
        .SetEase(characterMoveInEase).SetLink(gameObject);

    if (coinFountain) coinFountain.StartFountain();
  }

  private void SwapToMegaWin(TierSettings cfg)
  {
    _megaWinShown = true;

    // Layered, not replacing: audioKey's clip is still playing and should carry on underneath.
    PlayAudioOneShot(cfg.megaWinAudioKey);

    if (bigWinLabel)
    {
      _bigWinTween?.Kill();
      _bigWinTween = bigWinLabel.DOScale(Vector3.zero, labelScaleDownDuration)
        .SetEase(labelScaleDownEase).SetLink(gameObject)
        .OnComplete(() => { if (bigWinLabel) bigWinLabel.gameObject.SetActive(false); });
    }

    if (megaWinLabel)
    {
      megaWinLabel.gameObject.SetActive(true);
      megaWinLabel.localScale = Vector3.zero;
      _megaWinTween = megaWinLabel.DOScale(Vector3.one, labelScaleUpDuration)
        .SetEase(labelScaleUpEase).SetLink(gameObject);
    }
  }

  // ─────────────────────────────────────────────
  // SKIP / OUTRO / RESET
  // ─────────────────────────────────────────────

  // Blocker button, and the spin interrupt via SkipAndReset. The amount is snapped to the real win
  // BEFORE the outro so the exact value is what the player watches scale away.
  private void Skip()
  {
    if (!_isPlaying) return;

    _countTween?.Kill();
    _countTween = null;
    _holdTween?.Kill();
    _holdTween = null;

    if (winText) winText.SetNumber(_targetAmount);

    BeginOutro();
  }

  private void BeginOutro()
  {
    _holdTween = null;
    if (_outroSeq != null) return; // already fading out

    // Hand control back to the spin flow now — the fade-out is purely cosmetic from here.
    Action finished = _onFinished;
    _onFinished = null;
    finished?.Invoke();

    if (_blockerImage) _blockerImage.raycastTarget = false; // skip must not fire twice

    if (coinFountain)
    {
      coinFountain.StopFountain();
      coinFountain.FadeOutAllActive(coinFadeOutDuration);
    }

    _blockerTween?.Kill();
    _characterTween?.Kill();
    _glowScaleTween?.Kill();
    _bigWinTween?.Kill();
    _megaWinTween?.Kill();
    _rootFadeTween?.Kill();

    _outroSeq = DOTween.Sequence().SetLink(gameObject);

    if (_blockerImage && _blockerImage.gameObject.activeSelf)
      _outroSeq.Insert(0f, _blockerImage.DOFade(0f, resetFadeDuration));

    if (glowBG && glowBG.gameObject.activeSelf)
      _outroSeq.Insert(0f, glowBG.DOScale(Vector3.zero, labelScaleDownDuration).SetEase(labelScaleDownEase));

    if (bigWinLabel && bigWinLabel.gameObject.activeSelf)
      _outroSeq.Insert(0f, bigWinLabel.DOScale(Vector3.zero, labelScaleDownDuration).SetEase(labelScaleDownEase));

    if (megaWinLabel && megaWinLabel.gameObject.activeSelf)
      _outroSeq.Insert(0f, megaWinLabel.DOScale(Vector3.zero, labelScaleDownDuration).SetEase(labelScaleDownEase));

    if (character && characterPointA)
      _outroSeq.Insert(0f, character.DOLocalMove(_characterPointALocal, characterMoveOutDuration).SetEase(characterMoveOutEase));

    if (root)
      _outroSeq.Insert(0f, root.DOFade(0f, resetFadeDuration));

    _outroSeq.OnComplete(HardReset);
  }

  // Slams everything back to the resting state captured in Awake.
  private void HardReset()
  {
    KillAllTweens();

    if (glowBG)
    {
      glowBG.localScale = Vector3.zero;
      glowBG.localRotation = Quaternion.identity;
      glowBG.gameObject.SetActive(false);
    }
    if (bigWinLabel)
    {
      bigWinLabel.localScale = Vector3.zero;
      bigWinLabel.gameObject.SetActive(false);
    }
    if (megaWinLabel)
    {
      megaWinLabel.localScale = Vector3.zero;
      megaWinLabel.gameObject.SetActive(false);
    }
    if (_blockerImage)
    {
      SetBlockerAlpha(0f);
      _blockerImage.raycastTarget = false;
      _blockerImage.gameObject.SetActive(false);
    }
    if (character && characterPointA) character.localPosition = _characterPointALocal;
    if (coinFountain) coinFountain.ClearAll();
    if (winText) winText.SetNumber(0);
    // Deliberately not SetActive(false) — the CanvasGroup usually lives on this same GameObject,
    // and deactivating it would disable this component. alpha 0 hides it just as well, and every
    // child that actually costs anything is deactivated individually above.
    if (root)
    {
      root.alpha = 0f;
      root.blocksRaycasts = false;
    }

    _isPlaying = false;
    _bigWinShown = false;
    _megaWinShown = false;

    // Never leave a caller waiting on a callback that will now never arrive.
    Action finished = _onFinished;
    _onFinished = null;
    finished?.Invoke();
  }

  // Replaces whatever the win/lose source is playing.
  private void PlayAudio(string key)
  {
    if (audioController && !string.IsNullOrEmpty(key)) audioController.PlayWLAudio(key);
  }

  // Stacks on top of it instead.
  private void PlayAudioOneShot(string key)
  {
    if (audioController && !string.IsNullOrEmpty(key)) audioController.PlayWLAudioOneShot(key);
  }

  // The blocker dims via its Image colour alpha, so DOFade's endpoints have to be set by hand
  // rather than through a CanvasGroup.
  private void SetBlockerAlpha(float a)
  {
    if (_blockerImage == null) return;
    Color c = _blockerImage.color;
    c.a = a;
    _blockerImage.color = c;
  }

  private void KillAllTweens()
  {
    _rootFadeTween?.Kill(); _rootFadeTween = null;
    _countTween?.Kill(); _countTween = null;
    _holdTween?.Kill(); _holdTween = null;
    _blockerTween?.Kill(); _blockerTween = null;
    _characterTween?.Kill(); _characterTween = null;
    _glowSpin?.Kill(); _glowSpin = null;
    _glowScaleTween?.Kill(); _glowScaleTween = null;
    _bigWinTween?.Kill(); _bigWinTween = null;
    _megaWinTween?.Kill(); _megaWinTween = null;
    _outroSeq?.Kill(); _outroSeq = null;
  }

  private TierSettings GetTier(int tier)
  {
    switch (tier)
    {
      case 1: return tier1;
      case 2: return tier2;
      case 3: return tier3;
      default: return null;
    }
  }

  // ─────────────────────────────────────────────
  // DEBUG KEYS — 1/2/3 play a tier, 4 skips. Turn debugKeys off before shipping.
  // ─────────────────────────────────────────────

  private void Update()
  {
    if (!debugKeys) return;

    if (Input.GetKeyDown(KeyCode.Alpha1)) Play(1, debugTestAmount);
    else if (Input.GetKeyDown(KeyCode.Alpha2)) Play(2, debugTestAmount);
    else if (Input.GetKeyDown(KeyCode.Alpha3)) Play(3, debugTestAmount);
    else if (Input.GetKeyDown(KeyCode.Alpha4)) SkipAndReset();
  }
}

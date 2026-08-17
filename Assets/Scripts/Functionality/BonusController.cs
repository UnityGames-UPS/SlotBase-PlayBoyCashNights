using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using Spine.Unity;

public class BonusController : MonoBehaviour
{
  [SerializeField]
  private RectTransform Wheel_Transform;
  [SerializeField]
  private BoxCollider2D[] point_colliders;
  [SerializeField]
  private TMP_Text[] Bonus_Text;
  [SerializeField]
  private GameObject Bonus_Object;
  [SerializeField]
  private SlotBehaviour slotManager;
  [SerializeField]
  private AudioController _audioManager;
  [SerializeField]
  private SocketIOManager m_SocketManager;
  [SerializeField]
  private UIManager uIManager;
  [SerializeField]
  private GameObject SlotReel;
  [SerializeField]
  private GameObject BonusObjectMask;
  [SerializeField]
  private Transform startpos;
  [SerializeField]
  private Transform endpos;
  [SerializeField]
  private Vector2 MaskExpandedSize = new Vector2(2340f, 1403.09f);
  private Vector2 maskDefaultSize;
  private Vector2 maskDefaultPos;

  [Header("Charecter")]
  [SerializeField] private SkeletonGraphic BlueLadyAnim;
  [SerializeField] private SkeletonGraphic YellowLadyAnim;
  [SerializeField] private Transform YellowLadyStartPos;
  [SerializeField] private Transform YellowLadyEndPos;
  [SerializeField] private Transform BlueLadyStartPos;
  [SerializeField] private Transform BlueLadyEndPos;
  [SerializeField] private float CharacterMoveDuration = 2f;
  [SerializeField] private Ease CharacterMoveEaseIn = Ease.OutQuad;
  [SerializeField] private Ease CharacterMoveEaseOut = Ease.InQuad;
  [SerializeField] private float YellowLadyHoldDuration = 3f;
  internal bool isCollision = false;

  private Tween idleWheelTween;
  private Tween wheelRoutine;

  private float elasticIntensity = 5f;

  private int stopIndex = 0;

  private RectTransform slotRect;
  private RectTransform maskRect;




  private void Start()
  {
    slotRect = SlotReel.GetComponent<RectTransform>();
    maskRect = BonusObjectMask.GetComponent<RectTransform>();
    maskDefaultSize = maskRect.sizeDelta;
    maskDefaultPos = maskRect.anchoredPosition;

    SetToStartState();
    StartIdleWheelSpin();
  }

  private void SetToStartState()
  {
    slotRect.position = startpos.position;
    maskRect.anchoredPosition = maskDefaultPos;
    maskRect.sizeDelta = maskDefaultSize;
  }

  internal void StartBonus(int stop)
  {
    AnimateToEnd();
    ResetColliders();
    if (_audioManager) _audioManager.SwitchBGSound(true);
    PopulateWheel(m_SocketManager.FeaturesData.wheelBonus);
    stopIndex = stop;
    if (Bonus_Object) Bonus_Object.SetActive(true);

    StartCoroutine(doCharecterAnim());
    DOVirtual.DelayedCall(7f, () =>
    {
      uIManager.PlayWheelLoop(true);
      Spinbutton();
    });
  }

  IEnumerator doCharecterAnim()
  {
    MoveBlueLadyIn();
    BlueLadyAnim.AnimationState.SetAnimation(0, "animation", true);
    yield return new WaitForSeconds(1f);
    YellowLadyAnim.AnimationState.SetAnimation(0, "animation", true);
    MoveYellowLadyCycle();
  }

  private void Spinbutton()
  {
    StopIdleWheelSpin();
    isCollision = false;
    RotateWheel();
    DOVirtual.DelayedCall(1.5f, () =>
    {
      TurnCollider(stopIndex);
    });
  }

  internal void PopulateWheel(WheelBonus bonusdata)
  {
    for (int i = 0; i < bonusdata.multipliers.Count; i++)
    {
      if (Bonus_Text[i]) Bonus_Text[i].text = (bonusdata.multipliers[i]).ToString();
      Bonus_Text[i].color = (i % 2 != 0) ? Color.black : Color.white;
    }
  }

  private void RotateWheel()
  {
    // FIX 1: Removed hard reset to 359 (was causing jerk)
    // FIX 2: LocalAxisAdd instead of FastBeyond360
    // FIX 3: LoopType.Incremental instead of default Restart
    if (Wheel_Transform)
    {
      wheelRoutine = Wheel_Transform
        .DORotate(new Vector3(0, 0, -360f), 1f, RotateMode.LocalAxisAdd)
        .SetEase(Ease.Linear)
        .SetLoops(-1, LoopType.Incremental);
    }
    _audioManager.PlayBonusAudio("cycleSpin");
  }

  private void ResetColliders()
  {
    foreach (BoxCollider2D col in point_colliders)
    {
      col.enabled = false;
    }
  }

  private void TurnCollider(int point)
  {
    if (point_colliders[point]) point_colliders[point].enabled = true;
  }

  internal void StopWheel()
  {
    _audioManager.StopBonusAaudio();
    uIManager.PlayWheelLoop(false);

    if (wheelRoutine != null)
    {
      // FIX 4: Kill instead of Pause
      wheelRoutine.Kill();
      wheelRoutine = null;

      Wheel_Transform.DORotate(
        Wheel_Transform.eulerAngles + Vector3.forward * Random.Range(-elasticIntensity, elasticIntensity),
        1f
      ).SetEase(Ease.OutElastic)
      .OnComplete(() =>
      {
        // FIX 5: Reset angle after elastic settles — invisible to user, prevents float accumulation
        // Wheel_Transform.localEulerAngles = Vector3.zero;
      });
    }

    if (Bonus_Text[stopIndex].text.Equals("NO \nBONUS"))
    {
      PlayWinLooseSound(false);
    }
    else
    {
      PlayWinLooseSound(true);
    }

    DOVirtual.DelayedCall(1.5f, () =>
    {
      MoveBlueLadyOut();
      if (_audioManager) _audioManager.SwitchBGSound(false);
      Debug.Log("Swiching to Realllllllll");
      DOVirtual.DelayedCall(3f, () =>
      {
        ResetColliders();
        m_SocketManager.ResultData.payload.winAmount = m_SocketManager.ResultData.payload.bonusResult.bonuseWinAmount;
        slotManager.CheckWinPopups();
      });
    });

    DOVirtual.DelayedCall(3f, () =>
    {
      uIManager.StopWheelborderAnim();
      AnimateToStart();
      DOVirtual.DelayedCall(1f, () =>
      {
        StartIdleWheelSpin();
      });
    });
  }

  internal void PlayWinLooseSound(bool isWin)
  {
    if (isWin)
    {
      _audioManager.PlayBonusAudio("win");
    }
    else
    {
      _audioManager.PlayBonusAudio("lose");
    }
  }

  public void AnimateToEnd(float duration = 0.5f)
  {
    slotRect.DOAnchorPos(
      ((RectTransform)endpos).anchoredPosition,
      duration
    );
    maskRect.DOSizeDelta(
      MaskExpandedSize,
      duration
    );
  }

  public void AnimateToStart(float duration = 0.5f)
  {
    slotRect.DOAnchorPos(
      ((RectTransform)startpos).anchoredPosition,
      duration
    );
    maskRect.DOSizeDelta(
      maskDefaultSize,
      duration
    );
  }

  private void StartIdleWheelSpin()
  {
    idleWheelTween?.Kill();
    idleWheelTween = null;

    if (Wheel_Transform)
    {
      // FIX 5: Reset angle cleanly before idle starts
      Wheel_Transform.localEulerAngles = Vector3.zero;

      idleWheelTween = Wheel_Transform
        .DORotate(new Vector3(0, 0, -360f), 8f, RotateMode.LocalAxisAdd)
        .SetEase(Ease.Linear)
        .SetLoops(-1, LoopType.Incremental);
    }
  }

  private void StopIdleWheelSpin()
  {
    idleWheelTween?.Kill();
    idleWheelTween = null;
  }

  private void MoveCharacter(GameObject character, Transform start, Transform end, bool snapToStart, Ease ease)
  {
    character.SetActive(true);
    if (snapToStart) character.transform.localPosition = start.localPosition;
    character.transform.DOLocalMove(end.localPosition, CharacterMoveDuration).SetEase(ease);
  }

  public void MoveBlueLadyIn()
  {
    MoveCharacter(BlueLadyAnim.gameObject, BlueLadyStartPos, BlueLadyEndPos, snapToStart: true, ease: CharacterMoveEaseIn);
  }

  public void MoveBlueLadyOut()
  {
    MoveCharacter(BlueLadyAnim.gameObject, BlueLadyEndPos, BlueLadyStartPos, snapToStart: false, ease: CharacterMoveEaseOut);
  }

  public void MoveYellowLadyCycle()
  {
    YellowLadyAnim.gameObject.SetActive(true);
    YellowLadyAnim.transform.localPosition = YellowLadyStartPos.localPosition;

    DOTween.Sequence()
      .Append(YellowLadyAnim.transform.DOLocalMove(YellowLadyEndPos.localPosition, CharacterMoveDuration).SetEase(CharacterMoveEaseIn))
      .AppendInterval(YellowLadyHoldDuration)
      .Append(YellowLadyAnim.transform.DOLocalMove(YellowLadyStartPos.localPosition, CharacterMoveDuration).SetEase(CharacterMoveEaseOut));
  }
}

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;

public class BonusController : MonoBehaviour
{
  [SerializeField]
  private Button Spin_Button;
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
  private GameObject PopupPanel;
  [SerializeField]
  private Transform Win_Transform;
  [SerializeField]
  private Transform Loose_Transform;
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

  [Header("Charecter")]
  [SerializeField] private GameObject BlueLady;
  [SerializeField] private ImageAnimation YellowLady;
  [SerializeField] private Transform YellowLadyStartPos;
  [SerializeField] private Transform YellowLadyEndPos;
  [SerializeField] private Transform BlueLadyStartPos;
  [SerializeField] private Transform BlueLadyEndPos;
  internal bool isCollision = false;

  private Tween idleWheelTween;
  private Tween wheelRoutine;

  private float elasticIntensity = 5f;

  private int stopIndex = 0;

  private RectTransform slotRect;
  private RectTransform maskRect;



  private void Start()
  {
    if (Spin_Button) Spin_Button.onClick.RemoveAllListeners();
    if (Spin_Button) Spin_Button.onClick.AddListener(Spinbutton);

    slotRect = SlotReel.GetComponent<RectTransform>();
    maskRect = BonusObjectMask.GetComponent<RectTransform>();

    SetToStartState();
    StartIdleWheelSpin();
  }
  private void SetToStartState()
  {
    // Move Slot Reel
    slotRect.position = startpos.position;

    // Apply Bonus Mask size from screenshot start values
    maskRect.sizeDelta = new Vector2(2340f, 428.44f);
  }
  internal void StartBonus(int stop)
  {
    AnimateToEnd();
    ResetColliders();
    if (PopupPanel) PopupPanel.SetActive(false);
    if (Win_Transform) Win_Transform.gameObject.SetActive(false);
    if (Loose_Transform) Loose_Transform.gameObject.SetActive(false);
    if (_audioManager) _audioManager.SwitchBGSound(true);
    PopulateWheel(m_SocketManager.FeaturesData.wheelBonus);
    stopIndex = stop;
    if (Bonus_Object) Bonus_Object.SetActive(true);
    if (Spin_Button) Spin_Button.interactable = true;

    StartCoroutine(doCharecterAnim());
    // if (slotManager.IsAutoSpin || slotManager.IsFreeSpin)
    // {
    //   Spin_Button.gameObject.SetActive(false);
    DOVirtual.DelayedCall(7f, () =>
    {
      uIManager.PlayWheelLoop(true);
      Spinbutton();
    });
    // }
    // else
    // {
    //   Spin_Button.gameObject.SetActive(true);
    // }
  }
  IEnumerator doCharecterAnim()
  {
    MoveBlueLadyIn();
    yield return new WaitForSeconds(1f);
    YellowLady.StopAnimation();
    YellowLady.StartAnimation();

  }

  private void Spinbutton()
  {
    StopIdleWheelSpin();

    isCollision = false;
    if (Spin_Button) Spin_Button.interactable = false;
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
      // if (Bonus_Text[i]) Bonus_Text[i].text = (bonusdata.multipliers[i] * m_SocketManager.InitialData.bets[slotManager.BetCounter]).ToString();
      // Debug.Log("Bonus Data: " + bonusdata[i]);
      // Debug.Log("Bet Data: " + m_SocketManager.InitialData.bets[slotManager.BetCounter]);
      // Debug.Log("Multiplied Form: " + (double.Parse(bonusdata[i]) * m_SocketManager.InitialData.bets[slotManager.BetCounter]).ToString());
      Bonus_Text[i].color = (i % 2 != 0) ? Color.black : Color.white;

    }
  }

  private void RotateWheel()
  {
    if (Wheel_Transform) Wheel_Transform.localEulerAngles = new Vector3(0, 0, 359);
    if (Wheel_Transform) wheelRoutine = Wheel_Transform.DORotate(new Vector3(0, 0, 0), 1, RotateMode.FastBeyond360).SetEase(Ease.Linear).SetLoops(-1);
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
      wheelRoutine.Pause(); // Pause the rotation

      // Apply an elastic effect to the paused rotation
      Wheel_Transform.DORotate(Wheel_Transform.eulerAngles + Vector3.forward * Random.Range(-elasticIntensity, elasticIntensity), 1f)
          .SetEase(Ease.OutElastic);
    }
    if (Bonus_Text[stopIndex].text.Equals("NO \nBONUS"))
    {
      if (Loose_Transform) Loose_Transform.gameObject.SetActive(true);
      if (Loose_Transform) Loose_Transform.localScale = Vector3.zero;
      // if (PopupPanel) PopupPanel.SetActive(true);
      // uIManager.PlayWheelLoop(true);
      if (Loose_Transform) Loose_Transform.DOScale(Vector3.one, 1f);
      PlayWinLooseSound(false);
    }
    else
    {
      if (Win_Transform) Win_Transform.gameObject.SetActive(true);
      Win_Transform.GetChild(0).GetComponent<TMP_Text>().text += m_SocketManager.ResultData.payload.bonusResult.bonuseWinAmount.ToString("F3");
      if (Win_Transform) Win_Transform.localScale = Vector3.zero;
      // if (PopupPanel) PopupPanel.SetActive(true);

      if (Win_Transform) Win_Transform.DOScale(Vector3.one, 1f);
      PlayWinLooseSound(true);
    }
    DOVirtual.DelayedCall(1.5f, () =>
    {
      ResetColliders();
      MoveBlueLadyOut();
      if (_audioManager) _audioManager.SwitchBGSound(false);
      // if (Bonus_Object) Bonus_Object.SetActive(false);
      Debug.Log("Swiching to Realllllllll");
      DOVirtual.DelayedCall(3f, () =>
          {
            m_SocketManager.ResultData.payload.winAmount = m_SocketManager.ResultData.payload.bonusResult.bonuseWinAmount;
            slotManager.CheckWinPopups();
          });
    });
    DOVirtual.DelayedCall(3f, () =>
             {
               uIManager.StopWheelborderAnim();
               if (PopupPanel) PopupPanel.SetActive(false);
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

    maskRect.DOAnchorPos(
        new Vector2(-47f, 102.46f),
        duration
    );

    maskRect.DOSizeDelta(
        new Vector2(2340f, 1403.09f),
        duration
    );
  }

  public void AnimateToStart(float duration = 0.5f)
  {
    slotRect.DOAnchorPos(
        ((RectTransform)startpos).anchoredPosition,
        duration
    );

    maskRect.DOAnchorPos(
        new Vector2(-47f, 589.7898f),
        duration
    );

    maskRect.DOSizeDelta(
        new Vector2(2340f, 428.44f),
        duration
    );
  }

  private void StartIdleWheelSpin()
  {
    idleWheelTween?.Kill();

    if (Wheel_Transform)
    {
      idleWheelTween = Wheel_Transform
          .DORotate(new Vector3(0, 0, -360f), 8f, RotateMode.LocalAxisAdd)
          .SetEase(Ease.Linear)
          .SetLoops(-1, LoopType.Restart);
    }
  }

  private void StopIdleWheelSpin()
  {
    idleWheelTween?.Kill();
    idleWheelTween = null;
  }





  private IEnumerator MoveCharacter(GameObject character, Transform start, Transform end, float duration)
  {
    character.SetActive(true);
    character.transform.position = start.position;

    float time = 0f;

    while (time < duration)
    {
      time += Time.deltaTime;
      float t = time / duration;

      character.transform.position = Vector3.Lerp(start.position, end.position, t);
      yield return null;
    }

    character.transform.position = end.position;
  }

  public void MoveBlueLadyIn(float duration = 0.5f)
  {
    StartCoroutine(MoveCharacter(BlueLady, BlueLadyStartPos, BlueLadyEndPos, duration));
  }

  public void MoveBlueLadyOut(float duration = 0.5f)
  {
    StartCoroutine(MoveCharacter(BlueLady, BlueLadyEndPos, BlueLadyStartPos, duration));
  }

  // public void MoveYellowLadyIn(float duration = 0.5f)
  // {
  //   StartCoroutine(MoveCharacter(YellowLady, YellowLadyStartPos, YellowLadyEndPos, duration));
  // }

  // public void MoveYellowLadyOut(float duration = 0.5f)
  // {
  //   StartCoroutine(MoveCharacter(YellowLady, YellowLadyEndPos, YellowLadyStartPos, duration));
  // }
}

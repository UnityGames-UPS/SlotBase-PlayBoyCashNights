using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using System.Linq;
using TMPro;
using System;
using System.Reflection;

public class SlotBehaviour : MonoBehaviour
{
  [Header("Sprites")]
  [SerializeField]
  private Sprite[] myImages;  //images taken initially

  [Header("Slot Images")]
  [SerializeField]
  private List<SlotImage> images;     //class to store total images
  [SerializeField]
  private List<SlotImage> Tempimages;     //class to store the result matrix

  [Header("Slots Elements")]
  [SerializeField]
  private LayoutElement[] Slot_Elements;

  [Header("Slots Transforms")]
  [SerializeField]
  private Transform[] Slot_Transform;

  [Header("Line Button Objects")]

  private Dictionary<int, string> y_string = new Dictionary<int, string>();

  [Header("Buttons")]
  [SerializeField]
  private Button SlotStart_Button;
  [SerializeField]
  private Button AutoSpin_Button;
  [SerializeField] private Button AutoSpinStop_Button;
  [SerializeField]
  private Button MaxBet_Button;
  [SerializeField]
  private Button TBetPlus_Button;
  [SerializeField]
  private Button TBetMinus_Button;
  [SerializeField] private Button Turbo_Button;
  [SerializeField] private Button StopSpin_Button;

  [Header("Animated Sprites")]
  [SerializeField] private Sprite[] symbolZero;
  [SerializeField] private Sprite[] symbolOne;
  [SerializeField] private Sprite[] symbolTwo;
  [SerializeField] private Sprite[] symbolThree;
  [SerializeField] private Sprite[] symbolFour;
  [SerializeField] private Sprite[] symbolFive;
  [SerializeField] private Sprite[] symbolSix;
  [SerializeField] private Sprite[] symbolSeven;
  [SerializeField] private Sprite[] symbolEight;

  [Header("Miscellaneous UI")]
  [SerializeField]
  private TMP_Text Balance_text;
  [SerializeField]
  private TMP_Text TotalBet_text;
  [SerializeField]
  private TMP_Text LineBet_text;
  [SerializeField]
  private TMP_Text TotalWin_text;
  [SerializeField] private GameObject SmallWinObj;
  [SerializeField] private ImageAnimation CoinSplash;
  [SerializeField] private SpriteNumberText SmallwinText;
  [Header("Audio Management")]
  [SerializeField]
  private AudioController audioController;

  [SerializeField]
  private UIManager uiManager;

  [Header("BonusGame Popup")]
  [SerializeField]
  private BonusController _bonusManager;

  [Header("Free Spins Board")]
  [SerializeField]
  private GameObject FSBoard_Object;
  [SerializeField]
  private TMP_Text FSnum_text;

  [Header("Reel Tweening")]
  [SerializeField] private float reelSpeed = 6100f;   // local units/second
  [SerializeField] private float SpinTopY;
  [SerializeField] private float SpinBottomY;
  [SerializeField] private float RestY;

  private float DurationFor(float fromY, float toY)
    => Mathf.Abs(toY - fromY) / Mathf.Max(reelSpeed, 0.0001f);

  [SerializeField]
  private GameObject Image_Prefab;    //icons prefab
  [SerializeField] Sprite[] TurboToggleSprites;
  [SerializeField]
  private PayoutCalculation PayCalculator;

  private List<Tween> alltweens = new List<Tween>();  //fixed-size, indexed by reel column — never Add()/Clear() after init

  private Tweener WinTween = null;

  [SerializeField]
  private List<ImageAnimation> TempList;  //stores the sprites whose animation is running at present 

  [SerializeField]
  private SocketIOManager SocketManager;

  private Coroutine AutoSpinRoutine = null;
  private Coroutine FreeSpinRoutine = null;
  private Coroutine tweenroutine;
  private Tween BalanceTween;
  internal bool IsAutoSpin = false;
  internal bool IsFreeSpin = false;
  private bool IsSpinning = false;
  private bool CheckSpinAudio = false;
  internal bool CheckPopups = false;
  internal int BetCounter = 0;
  private double currentBalance = 0;
  private double currentTotalBet = 0;
  protected int Lines = 5;
  private int numberOfSlots = 3;          //number of columns
  private bool StopSpinToggle;
  private float SpinDelay = 0.2f;
  private bool IsTurboOn;
  internal bool WasAutoSpinOn;
  internal bool socketConnected = false;
  private int[,] initialMatrix = new int[,]
  {
    { 8, 11, 11, 12, 8 },
    { 11, 8, 8, 8, 12 },
    { 12, 12, 12, 11, 11 }
  };

  private void Start()
  {
    IsAutoSpin = false;

    for (int i = 0; i < numberOfSlots; i++)
    {
      alltweens.Add(null);
    }

    if (SlotStart_Button) SlotStart_Button.onClick.RemoveAllListeners();
    if (SlotStart_Button) SlotStart_Button.onClick.AddListener(delegate { StartSlots(); });

    if (TBetPlus_Button) TBetPlus_Button.onClick.RemoveAllListeners();
    if (TBetPlus_Button) TBetPlus_Button.onClick.AddListener(delegate { ChangeBet(true); });

    if (TBetMinus_Button) TBetMinus_Button.onClick.RemoveAllListeners();
    if (TBetMinus_Button) TBetMinus_Button.onClick.AddListener(delegate { ChangeBet(false); });

    if (MaxBet_Button) MaxBet_Button.onClick.RemoveAllListeners();
    if (MaxBet_Button) MaxBet_Button.onClick.AddListener(MaxBet);

    if (StopSpin_Button) StopSpin_Button.onClick.RemoveAllListeners();
    if (StopSpin_Button) StopSpin_Button.onClick.AddListener(() => { audioController.PlayButtonAudio(); StopSpinToggle = true; StopSpin_Button.gameObject.SetActive(false); });

    if (AutoSpin_Button) AutoSpin_Button.onClick.RemoveAllListeners();
    if (AutoSpin_Button) AutoSpin_Button.onClick.AddListener(AutoSpin);

    if (Turbo_Button) Turbo_Button.onClick.RemoveAllListeners();
    if (Turbo_Button) Turbo_Button.onClick.AddListener(TurboToggle);

    if (AutoSpinStop_Button) AutoSpinStop_Button.onClick.RemoveAllListeners();
    if (AutoSpinStop_Button) AutoSpinStop_Button.onClick.AddListener(StopAutoSpin);

    if (FSBoard_Object) FSBoard_Object.SetActive(false);
  }

  void TurboToggle()
  {
    audioController.PlayButtonAudio();
    if (IsTurboOn)
    {
      IsTurboOn = false;
      Turbo_Button.GetComponent<ImageAnimation>().StopAnimation();
      Turbo_Button.image.sprite = TurboToggleSprites[0];
      Turbo_Button.image.color = new Color(0.86f, 0.86f, 0.86f, 1);
    }
    else
    {
      IsTurboOn = true;
      Turbo_Button.GetComponent<ImageAnimation>().StartAnimation();
      Turbo_Button.image.color = new Color(1, 1, 1, 1);
    }
  }

  #region Autospin
  private void AutoSpin()
  {
    if (!IsAutoSpin)
    {
      IsAutoSpin = true;
      if (AutoSpinStop_Button) AutoSpinStop_Button.gameObject.SetActive(true);
      if (AutoSpin_Button) AutoSpin_Button.gameObject.SetActive(false);

      if (AutoSpinRoutine != null)
      {
        StopCoroutine(AutoSpinRoutine);
        AutoSpinRoutine = null;
      }
      AutoSpinRoutine = StartCoroutine(AutoSpinCoroutine());

    }
  }

  private void StopAutoSpin()
  {
    audioController.PlayButtonAudio();
    if (IsAutoSpin)
    {
      IsAutoSpin = false;
      if (AutoSpinStop_Button) AutoSpinStop_Button.gameObject.SetActive(false);
      if (AutoSpin_Button) AutoSpin_Button.gameObject.SetActive(true);
      StartCoroutine(StopAutoSpinCoroutine());
    }
  }

  private IEnumerator AutoSpinCoroutine()
  {
    while (IsAutoSpin)
    {
      yield return new WaitUntil(() => !CheckPopups);
      StartSlots(IsAutoSpin);
      yield return tweenroutine;
      yield return new WaitForSeconds(SpinDelay);
    }
    WasAutoSpinOn = false;
  }

  private IEnumerator StopAutoSpinCoroutine()
  {
    yield return new WaitUntil(() => !IsSpinning);
    ToggleButtonGrp(true);
    if (AutoSpinRoutine != null || tweenroutine != null)
    {
      StopCoroutine(AutoSpinRoutine);
      StopCoroutine(tweenroutine);
      tweenroutine = null;
      AutoSpinRoutine = null;
      StopCoroutine(StopAutoSpinCoroutine());
    }
  }
  #endregion

  #region FreeSpin
  internal void FreeSpin(int spins)
  {
    if (!IsFreeSpin)
    {
      if (FSnum_text) FSnum_text.text = spins.ToString();
      if (FSBoard_Object) FSBoard_Object.SetActive(true);
      IsFreeSpin = true;
      ToggleButtonGrp(false);

      if (FreeSpinRoutine != null)
      {
        StopCoroutine(FreeSpinRoutine);
        FreeSpinRoutine = null;
      }
      FreeSpinRoutine = StartCoroutine(FreeSpinCoroutine(spins));
    }
  }

  private IEnumerator FreeSpinCoroutine(int spinchances)
  {
    yield return new WaitForSecondsRealtime(1.5f);
    int i = 0;
    while (i < spinchances)
    {
      uiManager.FreeSpins--;
      if (FSnum_text) FSnum_text.text = uiManager.FreeSpins.ToString();
      StartSlots();
      yield return tweenroutine;
      yield return new WaitForSeconds(SpinDelay);
      i++;
    }
    if (FSBoard_Object) FSBoard_Object.SetActive(false);
    if (WasAutoSpinOn)
    {
      AutoSpin();
    }
    else
    {
      ToggleButtonGrp(true);
    }
    IsFreeSpin = false;
  }
  #endregion

  private void CompareBalance()
  {
    if (currentBalance < currentTotalBet)
    {
      uiManager.LowBalPopup();
    }
  }

  internal void UpdateBalanceDisplay(double newBalance)
  {
    currentBalance = newBalance;
    if (Balance_text) Balance_text.text = newBalance.ToString("F3");
    CompareBalance();
  }

  //Generate Static Lines from button hovers
  internal void GenerateStaticLine(TMP_Text LineID_Text)
  {
    DestroyStaticLine();
    int LineID = 1;
    try
    {
      LineID = int.Parse(LineID_Text.text);
    }
    catch (Exception e)
    {
      Debug.Log("Exception while parsing " + e.Message);
    }
    PayCalculator.GeneratePayoutLinesBackend(LineID, true);
  }

  //Destroy Static Lines from button hovers
  internal void DestroyStaticLine()
  {
    PayCalculator.ResetStaticLine();
  }

  private void MaxBet()
  {
    if (audioController) audioController.PlayButtonAudio();
    BetCounter = SocketManager.InitialData.bets.Count - 1;
    if (LineBet_text) LineBet_text.text = SocketManager.InitialData.bets[BetCounter].ToString();
    if (TotalBet_text) TotalBet_text.text = (SocketManager.InitialData.bets[BetCounter] * Lines).ToString();
    currentTotalBet = SocketManager.InitialData.bets[BetCounter] * Lines;

  }

  private void ChangeBet(bool IncDec)
  {
    if (audioController) audioController.PlayButtonAudio();
    if (IncDec)
    {
      BetCounter++;
      if (BetCounter >= SocketManager.InitialData.bets.Count)
      {
        BetCounter = 0; // Loop back to the first bet
      }
    }
    else
    {
      BetCounter--;
      if (BetCounter < 0)
      {
        BetCounter = SocketManager.InitialData.bets.Count - 1; // Loop to the last bet
      }
    }
    if (LineBet_text) LineBet_text.text = SocketManager.InitialData.bets[BetCounter].ToString();
    if (TotalBet_text) TotalBet_text.text = (SocketManager.InitialData.bets[BetCounter] * Lines).ToString();
    currentTotalBet = SocketManager.InitialData.bets[BetCounter] * Lines;
    uiManager.InitialiseUIData(SocketManager.UIData.paylines);
    _bonusManager.PopulateWheel(SocketManager.FeaturesData.wheelBonus);
  }

  #region InitialFunctions
  // internal void shuffleInitialMatrix()
  // {
  //   for (int i = 0; i < Tempimages.Count; i++)
  //   {
  //     for (int j = 0; j < 3; j++)
  //     {
  //       int randomIndex = UnityEngine.Random.Range(0, 14);
  //       Tempimages[i].slotImages[j].sprite = myImages[randomIndex];
  //     }
  //   }
  // }


  internal void InitializeMatrix()
  {
    for (int row = 0; row < initialMatrix.GetLength(0); row++)
    {
      for (int col = 0; col < initialMatrix.GetLength(1); col++)
      {
        int val = initialMatrix[row, col];

        Tempimages[col].slotImages[row].sprite = myImages[val];

        ImageAnimation animScript = Tempimages[col].slotImages[row].GetComponent<ImageAnimation>();
        if (animScript != null)
        {
          PopulateAnimationSprites(animScript, val);

          animScript.StartAnimation();
          TempList.Add(animScript);
        }
      }
    }
  }


  internal void SetInitialUI()
  {
    socketConnected = true;
    BetCounter = 0;
    if (LineBet_text) LineBet_text.text = SocketManager.InitialData.bets[BetCounter].ToString();
    if (TotalBet_text) TotalBet_text.text = (SocketManager.InitialData.bets[BetCounter] * Lines).ToString();
    if (TotalWin_text) TotalWin_text.text = "0.000";
    if (Balance_text) Balance_text.text = SocketManager.PlayerData.balance.ToString("F3");
    currentBalance = SocketManager.PlayerData.balance;
    currentTotalBet = SocketManager.InitialData.bets[BetCounter] * Lines;
    _bonusManager.PopulateWheel(SocketManager.FeaturesData.wheelBonus);
    CompareBalance();
    uiManager.InitialiseUIData(SocketManager.UIData.paylines);
  }
  #endregion

  private void OnApplicationFocus(bool focus)
  {
    uiManager.SetMuteAll(!focus);
  }

  private void PopulateAnimationSprites(ImageAnimation animScript, int val)
  {
    animScript.textureArray.Clear();
    animScript.textureArray.TrimExcess();

    Sprite[] selectedSprites = null;

    switch (val)
    {
      case 0:
        selectedSprites = symbolZero;
        animScript.AnimationSpeed = 20;
        animScript.ScaleSize = 1f;
        break;

      case 1:
        selectedSprites = symbolOne;
        animScript.AnimationSpeed = 11;
        animScript.ScaleSize = 1f;
        break;

      case 2:
        selectedSprites = symbolTwo;
        animScript.AnimationSpeed = 34;
        animScript.ScaleSize = 1.1f;
        break;

      case 3:
        selectedSprites = symbolThree;
        animScript.AnimationSpeed = 34;
        animScript.ScaleSize = 1.6f;
        break;

      case 4:
        selectedSprites = symbolFour;
        animScript.AnimationSpeed = 34;
        animScript.ScaleSize = 1.3f;
        break;
      case 5:
        selectedSprites = symbolFive;
        animScript.AnimationSpeed = 12;
        animScript.ScaleSize = 1.4f;
        break;

      case 6:
        selectedSprites = symbolSix;
        animScript.AnimationSpeed = 40;
        animScript.ScaleSize = 1.6f;
        break;

      case 7:
        selectedSprites = symbolSeven;
        animScript.AnimationSpeed = 40;
        animScript.ScaleSize = 2.5f;
        break;

      case 8:
        selectedSprites = symbolEight;
        animScript.AnimationSpeed = 40;
        animScript.ScaleSize = 1.9f;
        break;
    }

    if (selectedSprites == null) return;

    for (int i = 0; i < selectedSprites.Length; i++)
    {
      animScript.textureArray.Add(selectedSprites[i]);
    }

    animScript.AnimationSpeed = GetDynamicSpeed(selectedSprites.Length);
  }
  private float GetDynamicSpeed(int frameCount)
  {
    return Mathf.Max(5f, frameCount - 5f);
  }
  #region SlotSpin
  //starts the spin process
  private void StartSlots(bool autoSpin = false)
  {
    Debug.Log("StartButtonClicked");
    if (audioController) audioController.PlaySpinButtonAudio();
    if (TotalWin_text) TotalWin_text.text = "0.000";

    if (!autoSpin)
    {
      if (AutoSpinRoutine != null)
      {
        StopCoroutine(AutoSpinRoutine);
        StopCoroutine(tweenroutine);
        tweenroutine = null;
        AutoSpinRoutine = null;
      }
    }
    WinningsAnim(false);
    if (SlotStart_Button) SlotStart_Button.interactable = false;
    if (TempList.Count > 0)
    {
      StopGameAnimation();
    }
    PayCalculator.ResetLines();
    tweenroutine = StartCoroutine(TweenRoutine());
  }

  //manage the Routine for spinning of the slots
  private IEnumerator TweenRoutine()
  {
    SmallWinObj.SetActive(false);
    uiManager.PlayRellsLoop(false);
    if (currentBalance < currentTotalBet && !IsFreeSpin)
    {
      CompareBalance();
      StopAutoSpin();
      yield return new WaitForSeconds(1);
      ToggleButtonGrp(true);
      yield break;
    }
    if (audioController) audioController.PlayWLAudio("spin");
    CheckSpinAudio = true;

    IsSpinning = true;

    ToggleButtonGrp(false);
    if (!IsTurboOn && !IsFreeSpin && !IsAutoSpin)
    {
      StopSpin_Button.gameObject.SetActive(true);
    }
    KillAllTweens();
    for (int i = 0; i < numberOfSlots; i++)
    {
      InitializeTweening(Slot_Transform[i], i);
      yield return new WaitForSeconds(0.1f);
    }

    if (!IsFreeSpin)
    {
      BalanceDeduction();
    }

    SocketManager.AccumulateResult(BetCounter);
    yield return new WaitUntil(() => SocketManager.isResultdone);

    for (int i = 0; i < 3; i++)
    {
      for (int j = 0; j < 3; j++)
      {
        //    print("image loc: " + j + " " + i);
        int resultNum = int.Parse(SocketManager.ResultData.matrix[i][j]);
        //  print("resultNum: " + resultNum);
        PopulateAnimationSprites(Tempimages[j].slotImages[i].GetComponent<ImageAnimation>(), resultNum);
        Tempimages[j].slotImages[i].sprite = myImages[resultNum];
      }
    }
    CheckForFeaturesAnimation();


    if (IsTurboOn || IsFreeSpin)
    {
      StopSpinToggle = true;
    }
    else
    {
      for (int i = 0; i < 9; i++)
      {
        yield return new WaitForSecondsRealtime(0.1f);
        if (StopSpinToggle)
        {
          yield return new WaitForSecondsRealtime(0.1f);
          break;
        }
      }
    }

    for (int i = 0; i < numberOfSlots; i++)
    {
      StopTweening(Slot_Transform[i], i);

      float wait = IsTurboOn || StopSpinToggle ? 0.2f : 0.6f;
      yield return new WaitForSecondsRealtime(wait);
    }
    yield return alltweens[^1].WaitForCompletion();
    for (int i = 0; i < numberOfSlots; i++)
    {
      Slot_Transform[i].localPosition = new Vector2(Slot_Transform[i].localPosition.x, RestY);
    }
    KillAllTweens();
    uiManager.PlayRellsLoop(true);
    StopSpinToggle = false;
    StopSpin_Button.gameObject.SetActive(false);
    audioController.StopWLAaudio();

    if (SocketManager.ResultData.payload.winAmount > 0)
    {
      SpinDelay = 1.2f;
    }
    else
    {
      SpinDelay = 0.2f;
    }

    if (SocketManager.ResultData.payload.winAmount > 0)
    {
      List<int> winLine = new();
      foreach (var item in SocketManager.ResultData.payload.wins)
      {
        winLine.Add(item.line);
      }
      CheckPayoutLineBackend(winLine);
      // ShowSmallWin(SocketManager.ResultData.payload.winAmount);

    }

    CheckPopups = true;

    //if (TotalWin_text) TotalWin_text.text = SocketManager.ResultData.payload.winAmount.ToString("F3");


    // if (SocketManager.ResultData.jackpot.isTriggered)
    // {
    //   uiManager.PopulateWin(4, SocketManager.ResultData.jackpot.amount);
    //   yield return new WaitUntil(() => !CheckPopups);
    //   CheckPopups = true;
    // }

    if (SocketManager.ResultData.payload.bonusResult.isBonusTriggered)
    {
      yield return new WaitForSecondsRealtime(1f);
      CheckBonusGame();
    }
    else
    {
      CheckWinPopups();
    }
    // CheckPopups = false;
    yield return new WaitUntil(() => !CheckPopups);

    if (SocketManager.ResultData.payload.winAmount > 0)
    {
      yield return new WaitForSeconds(2f);
      SmallWinObj.SetActive(false);
    }
    BalanceTween?.Kill();
    if (Balance_text) Balance_text.text = SocketManager.ResultData.player.balance.ToString("F3");

    currentBalance = SocketManager.PlayerData.balance;
    if (!IsAutoSpin && !IsFreeSpin)
    {
      ToggleButtonGrp(true);
      IsSpinning = false;
    }
    else
    {
      // yield return new WaitForSeconds(2f);
      IsSpinning = false;
    }
    // if (SocketManager.ResultData.freeSpin.isFreeSpin)
    // {
    //   if (IsFreeSpin)
    //   {
    //     IsFreeSpin = false;
    //     if (FreeSpinRoutine != null)
    //     {
    //       StopCoroutine(FreeSpinRoutine);
    //       FreeSpinRoutine = null;
    //     }
    //   }
    //   uiManager.FreeSpinProcess((int)SocketManager.ResultData.freeSpin.count);
    //   if (IsAutoSpin)
    //   {
    //     WasAutoSpinOn = true;
    //     StopAutoSpin();
    //     yield return new WaitForSeconds(0.1f);
    //   }
    // }
  }

  private void ShowSmallWin(double winAmount)
  {
    SmallWinObj.SetActive(true);
    CoinSplash.StartAnimation();
    SmallwinText.AnimateFromZero(winAmount);
    AnimateNormalText(winAmount);
  }
  private void CheckForFeaturesAnimation()
  {
    bool playJackpot = false;
    bool playScatter = false;
    bool playBonus = SocketManager.ResultData.payload.bonusResult.isBonusTriggered;
    bool playFreespin = false;
    // if (SocketManager.ResultData.jackpot.amount > 0)
    // {
    //   playJackpot = true;
    // }

    PlayFeatureAnimation(playJackpot, playScatter, playBonus, playFreespin);
  }
  private void PlayFeatureAnimation(bool jackpot = false, bool scatter = false, bool bonus = false, bool freeSpin = false)
  {
    for (int i = 0; i < SocketManager.ResultData.matrix.Count; i++)
    {
      for (int j = 0; j < SocketManager.ResultData.matrix[i].Count; j++)
      {

        if (int.TryParse(SocketManager.ResultData.matrix[i][j], out int parsedNumber))
        {
          if (jackpot && parsedNumber == 12)
          {
            StartGameAnimation(Tempimages[j].slotImages[i].gameObject);
          }
          if (scatter && parsedNumber == 11)
          {
            StartGameAnimation(Tempimages[j].slotImages[i].gameObject);
          }
          if (bonus && parsedNumber == 8)
          {
            StartGameAnimation(Tempimages[j].slotImages[i].gameObject);
            audioController.PlayWLAudio("phone");
          }
          if (freeSpin && parsedNumber == 9)
          {
            StartGameAnimation(Tempimages[j].slotImages[i].gameObject);
          }
        }

      }
    }
  }
  private void BalanceDeduction()
  {
    double bet = 0;
    double balance = 0;
    try
    {
      bet = double.Parse(TotalBet_text.text);
    }
    catch (Exception e)
    {
      Debug.Log("Error while conversion " + e.Message);
    }

    try
    {
      balance = double.Parse(Balance_text.text);
    }
    catch (Exception e)
    {
      Debug.Log("Error while conversion " + e.Message);
    }
    double initAmount = balance;

    balance = balance - bet;

    BalanceTween = DOTween.To(() => initAmount, (val) => initAmount = val, balance, 0.8f).OnUpdate(() =>
    {
      if (Balance_text) Balance_text.text = initAmount.ToString("F3");
    });
  }

  internal void CheckWinPopups()
  {
    if (SocketManager.ResultData.payload.winAmount >= currentTotalBet * 5 && SocketManager.ResultData.payload.winAmount < currentTotalBet * 10)
    {
      uiManager.PopulateWin(1, SocketManager.ResultData.payload.winAmount);
    }
    else if (SocketManager.ResultData.payload.winAmount >= currentTotalBet * 10 && SocketManager.ResultData.payload.winAmount < currentTotalBet * 15)
    {
      uiManager.PopulateWin(2, SocketManager.ResultData.payload.winAmount);
    }
    else if (SocketManager.ResultData.payload.winAmount >= currentTotalBet * 15)
    {
      uiManager.PopulateWin(3, SocketManager.ResultData.payload.winAmount);
    }
    else
    {
      if (SocketManager.ResultData.payload.winAmount > 0) ShowSmallWin(SocketManager.ResultData.payload.winAmount);
      CheckPopups = false;
    }
  }

  internal void CheckBonusGame()
  {
    _bonusManager.StartBonus(
     SocketManager.ResultData.payload.bonusResult.wheelIndex ?? 0
 );
  }

  //generate the payout lines generated 
  private void CheckPayoutLineBackend(List<int> LineId, double jackpot = 0)
  {
    if (LineId.Count > 0)
    {
      if (jackpot <= 0)
      {
        if (audioController) audioController.PlayWLAudio("win");
      }

      for (int i = 0; i < LineId.Count; i++)
      {
        PayCalculator.GeneratePayoutLinesBackend(LineId[i]);
      }

      if (jackpot > 0)
      {
        if (audioController) audioController.PlayWLAudio("megaWin");
        for (int i = 0; i < Tempimages.Count; i++)
        {
          for (int k = 0; k < Tempimages[i].slotImages.Count; k++)
          {
            StartGameAnimation(Tempimages[i].slotImages[k].gameObject);
          }
        }
      }
      else
      {
        List<KeyValuePair<int, int>> coords = new();
        for (int j = 0; j < LineId.Count; j++)
        {
          for (int k = 0; k < 3; k++)
          {
            int rowIndex = SocketManager.InitialData.lines[LineId[j]][k];
            int columnIndex = k;
            coords.Add(new KeyValuePair<int, int>(rowIndex, columnIndex));
          }
        }

        foreach (var coord in coords)
        {
          int rowIndex = coord.Key;
          int columnIndex = coord.Value;
          StartGameAnimation(Tempimages[columnIndex].slotImages[rowIndex].gameObject);
        }
      }
      WinningsAnim(true);
    }
    else
    {

      //if (audioController) audioController.PlayWLAudio("lose");
      if (audioController) audioController.StopWLAaudio();
    }
    CheckSpinAudio = false;
  }

  private void WinningsAnim(bool IsStart)
  {
    // if (IsStart)
    // {
    //   WinTween = TotalWin_text.gameObject.GetComponent<RectTransform>().DOScale(new Vector2(1.5f, 1.5f), 1f).SetLoops(-1, LoopType.Yoyo).SetDelay(0);
    // }
    // else
    // {
    //   WinTween.Kill();
    //   TotalWin_text.gameObject.GetComponent<RectTransform>().localScale = Vector3.one;
    // }
  }

  #endregion

  internal void CallCloseSocket()
  {
    StartCoroutine(SocketManager.CloseSocket());
  }


  void ToggleButtonGrp(bool toggle)
  {
    if (SlotStart_Button) SlotStart_Button.interactable = toggle;
    if (MaxBet_Button) MaxBet_Button.interactable = toggle;
    if (AutoSpin_Button) AutoSpin_Button.interactable = toggle;
    if (TBetMinus_Button) TBetMinus_Button.interactable = toggle;
    if (TBetPlus_Button) TBetPlus_Button.interactable = toggle;
    // if(Turbo_Button) Turbo_Button.interactable = toggle;
  }

  //start the icons animation
  private void StartGameAnimation(GameObject animObjects)
  {
    ImageAnimation temp = animObjects.GetComponent<ImageAnimation>();
    temp.StartAnimation();

    // temp.ScaleUp();
    // RectTransform rect = animObjects.GetComponent<RectTransform>();
    // rect.localScale = new Vector3(1.2f, 1.2f, 1f);
    TempList.Add(temp);
  }

  //stop the icons animation
  private void StopGameAnimation()
  {
    for (int i = 0; i < TempList.Count; i++)
    {
      TempList[i].StopAnimation();

      RectTransform rect = TempList[i].GetComponent<RectTransform>();

      rect.localScale = Vector3.one;
    }
    TempList.Clear();
    TempList.TrimExcess();
  }


  #region TweeningCode
  private Tween InitializeTweening(Transform slotTransform, int index)
  {
    alltweens[index]?.Kill();
    Sequence seq = DOTween.Sequence();
    float startY = slotTransform.localPosition.y;
    seq.Append(slotTransform.DOLocalMoveY(SpinBottomY, DurationFor(startY, SpinBottomY)).SetEase(Ease.Linear));
    seq.AppendCallback(() =>
    {
      slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, SpinTopY);
      Tweener tweener = slotTransform.DOLocalMoveY(SpinBottomY, DurationFor(SpinTopY, SpinBottomY))
        .SetLoops(-1, LoopType.Restart)
        .SetEase(Ease.Linear);
      alltweens[index] = tweener;
    });
    alltweens[index] = seq;
    return seq;
  }

  private void StopTweening(Transform slotTransform, int index)
  {
    alltweens[index]?.Kill();
    slotTransform.localPosition = new Vector2(slotTransform.localPosition.x, SpinTopY);
    alltweens[index] = slotTransform.DOLocalMoveY(RestY, DurationFor(SpinTopY, RestY)).SetEase(Ease.OutBack, 0.9f);
  }

  private void KillAllTweens()
  {
    for (int i = 0; i < alltweens.Count; i++)
    {
      alltweens[i]?.Kill();
      alltweens[i] = null;
    }
  }
  #endregion

  private float animDuration = 1.5f;
  private AnimationCurve animCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

  private Coroutine textAnimCoroutine;

  internal void AnimateNormalText(double targetValue)
  {
    if (textAnimCoroutine != null)
      StopCoroutine(textAnimCoroutine);

    textAnimCoroutine = StartCoroutine(NormalTextRoutine(targetValue));
  }
  private IEnumerator NormalTextRoutine(double target)
  {
    double current = 0;
    float time = 0;

    while (time < animDuration)
    {
      time += Time.deltaTime;

      float t = Mathf.Clamp01(time / animDuration);
      float curved = animCurve.Evaluate(t);

      current = target * curved;

      if (TotalWin_text)
        TotalWin_text.text = current.ToString("0.###"); // no trailing zeros

      yield return null;
    }

    // final value
    if (TotalWin_text)
      TotalWin_text.text = target.ToString("0.###");

    textAnimCoroutine = null;
  }

}

[Serializable]
public class SlotImage
{
  public List<Image> slotImages = new List<Image>(10);
}


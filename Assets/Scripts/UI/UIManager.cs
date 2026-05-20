using UnityEngine;
using DG.Tweening;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic;
using Unity.VisualScripting;

public class UIManager : MonoBehaviour
{
  [SerializeField] private JSFunctCalls jsFunctCalls;
  [Header("Menu UI")]
  [SerializeField]
  private Button Menu_Button;
  [SerializeField]
  private GameObject Menu_Object;
  [SerializeField]
  private RectTransform Menu_RT;

  [Header("Settings UI")]
  [SerializeField]
  private Button Settings_Button;
  [SerializeField]
  private GameObject Settings_Object;
  [SerializeField]
  private RectTransform Settings_RT;
  [SerializeField]
  private Button Terms_Button;
  [SerializeField]
  private Button Privacy_Button;

  [SerializeField]
  private Button Exit_Button;
  [SerializeField]
  private GameObject Exit_Object;
  [SerializeField]
  private RectTransform Exit_RT;

  [SerializeField]
  private Button Paytable_Button;
  [SerializeField]
  private GameObject Paytable_Object;
  [SerializeField]
  private RectTransform Paytable_RT;

  [Header("Popus UI")]
  [SerializeField]
  private GameObject MainPopup_Object;

  [Header("About Popup")]
  [SerializeField]
  private GameObject AboutPopup_Object;
  [SerializeField]
  private Button AboutExit_Button;
  [SerializeField]
  private Image AboutLogo_Image;
  [SerializeField]
  private Button Support_Button;

  [Header("Paytable Popup")]
  [SerializeField]
  private GameObject PaytablePopup_Object;
  [SerializeField]
  private Button PaytableExit_Button;
  [SerializeField]
  private TMP_Text[] SymbolsText;
  [SerializeField]
  private Button LeftNavBtn;
  [SerializeField]
  private Button RightNavBtn;
  [SerializeField]
  private List<GameObject> Pages;
  private int currentPageIndex = 0;

  [Header("Settings Popup")]
  [SerializeField]
  private GameObject SettingsPopup_Object;
  [SerializeField]
  private Button SettingsExit_Button;
  [SerializeField]
  private Button Sound_Button;
  [SerializeField]
  private Button Music_Button;

  [SerializeField]
  private GameObject MusicOn_Object;
  [SerializeField]
  private GameObject MusicOff_Object;
  [SerializeField]
  private GameObject SoundOn_Object;
  [SerializeField]
  private GameObject SoundOff_Object;

  [Header("Win Popup")]
  [SerializeField]
  private List<Sprite> BigWin_Sprite;
  [SerializeField]
  private List<Sprite> HugeWin_Sprite;
  [SerializeField]
  private List<Sprite> MegaWin_Sprite;
  [SerializeField]
  private Sprite Jackpot_Sprite;
  [SerializeField] private ImageAnimation Win_Image;
  [SerializeField] private ImageAnimation LuckyGirl;
  [SerializeField]
  private GameObject WinPopup_Object;
  [SerializeField]
  private TMP_Text Win_Text;
  [SerializeField] private Button SkipWinAnimation;

  [Header("FreeSpins Popup")]
  [SerializeField]
  private GameObject FreeSpinPopup_Object;
  [SerializeField]
  private TMP_Text Free_Text;

  [Header("Splash Screen")]
  [SerializeField]
  private GameObject Loading_Object;
  [SerializeField]
  private Image Loading_Image;
  [SerializeField]
  private TMP_Text Loading_Text;
  [SerializeField]
  private TMP_Text LoadPercent_Text;
  [SerializeField]
  private Button QuitSplash_button;

  [Header("Disconnection Popup")]
  [SerializeField]
  private Button CloseDisconnect_Button;
  [SerializeField]
  private GameObject DisconnectPopup_Object;

  [Header("AnotherDevice Popup")]
  [SerializeField]
  private Button CloseAD_Button;
  [SerializeField]
  private GameObject ADPopup_Object;

  [Header("Reconnection Popup")]
  [SerializeField]
  private TMP_Text reconnect_Text;
  [SerializeField]
  private GameObject ReconnectPopup_Object;

  [Header("LowBalance Popup")]
  [SerializeField]
  private Button LBExit_Button;
  [SerializeField]
  private GameObject LBPopup_Object;

  [Header("Quit Popup")]
  [SerializeField]
  private GameObject QuitPopup_Object;
  [SerializeField]
  private Button YesQuit_Button;
  [SerializeField]
  private Button NoQuit_Button;
  [SerializeField]
  private Button CrossQuit_Button;

  [Header("Animation Popup")]
  [SerializeField] private ImageAnimation SlotBorder;
  [SerializeField]
  private List<Sprite> SlotBorderOne;
  [SerializeField]
  private List<Sprite> SlotBorderTwo;
  [SerializeField] private ImageAnimation WheelBorder;
  [SerializeField]
  private List<Sprite> WheelBorderOne;
  [SerializeField]
  private List<Sprite> WheelBorderTwo;
  [SerializeField]
  private Sprite WheelBorderSprite;

  [Header("Classes")]
  [SerializeField]
  private AudioController audioController;
  [SerializeField]
  private Button m_AwakeGameButton;

  [SerializeField]
  private Button GameExit_Button;

  [SerializeField]
  private SlotBehaviour slotManager;

  [SerializeField]
  private SocketIOManager socketManager;

  private bool isMusic = true;
  private bool isSound = true;
  private Tween WinPopupTextTween;
  private Tween ClosePopupTween;
  internal bool isExit = false;
  internal int FreeSpins;


  private void Awake()
  {
    // Make sure jsFunctCalls is assigned before using it
    if (jsFunctCalls != null)
    {
      jsFunctCalls.RegisterVisibilityListener(gameObject.name);
    }
    else
    {
      Debug.LogWarning("jsFunctCalls reference is null in Awake()");
    }
  }
  private void Start()
  {

    if (Menu_Button) Menu_Button.onClick.RemoveAllListeners();
    if (Menu_Button) Menu_Button.onClick.AddListener(OpenMenu);

    if (Exit_Button) Exit_Button.onClick.RemoveAllListeners();
    if (Exit_Button) Exit_Button.onClick.AddListener(CloseMenu);

    //if (About_Button) About_Button.onClick.RemoveAllListeners();
    //if (About_Button) About_Button.onClick.AddListener(delegate { OpenPopup(AboutPopup_Object); });

    if (AboutExit_Button) AboutExit_Button.onClick.RemoveAllListeners();
    if (AboutExit_Button) AboutExit_Button.onClick.AddListener(delegate { ClosePopup(AboutPopup_Object); });

    if (Paytable_Button) Paytable_Button.onClick.RemoveAllListeners();
    if (Paytable_Button) Paytable_Button.onClick.AddListener(delegate { OpenPopup(PaytablePopup_Object); });

    if (PaytableExit_Button) PaytableExit_Button.onClick.RemoveAllListeners();
    if (PaytableExit_Button) PaytableExit_Button.onClick.AddListener(delegate { ClosePopup(PaytablePopup_Object); });

    if (Settings_Button) Settings_Button.onClick.RemoveAllListeners();
    if (Settings_Button) Settings_Button.onClick.AddListener(delegate { OpenPopup(SettingsPopup_Object); });

    if (SettingsExit_Button) SettingsExit_Button.onClick.RemoveAllListeners();
    if (SettingsExit_Button) SettingsExit_Button.onClick.AddListener(delegate { ClosePopup(SettingsPopup_Object); });

    if (MusicOn_Object) MusicOn_Object.SetActive(true);
    if (MusicOff_Object) MusicOff_Object.SetActive(false);

    if (SoundOn_Object) SoundOn_Object.SetActive(true);
    if (SoundOff_Object) SoundOff_Object.SetActive(false);

    if (GameExit_Button) GameExit_Button.onClick.RemoveAllListeners();
    if (GameExit_Button) GameExit_Button.onClick.AddListener(delegate
    {
      OpenPopup(QuitPopup_Object);
    });

    if (NoQuit_Button) NoQuit_Button.onClick.RemoveAllListeners();
    if (NoQuit_Button) NoQuit_Button.onClick.AddListener(delegate
    {
      if (!isExit)
      {
        ClosePopup(QuitPopup_Object);
      }
    });

    if (CrossQuit_Button) CrossQuit_Button.onClick.RemoveAllListeners();
    if (CrossQuit_Button) CrossQuit_Button.onClick.AddListener(delegate
    {
      if (!isExit)
      {
        ClosePopup(QuitPopup_Object);
      }
    });

    if (LBExit_Button) LBExit_Button.onClick.RemoveAllListeners();
    if (LBExit_Button) LBExit_Button.onClick.AddListener(delegate { ClosePopup(LBPopup_Object); });

    if (YesQuit_Button) YesQuit_Button.onClick.RemoveAllListeners();
    if (YesQuit_Button) YesQuit_Button.onClick.AddListener(delegate
    {
      CallOnExitFunction();
      Debug.Log("quit event: pressed YES Button ");

    });

    if (CloseDisconnect_Button) CloseDisconnect_Button.onClick.RemoveAllListeners();
    if (CloseDisconnect_Button) CloseDisconnect_Button.onClick.AddListener(CallOnExitFunction); //BackendChanges

    if (CloseAD_Button) CloseAD_Button.onClick.RemoveAllListeners();
    if (CloseAD_Button) CloseAD_Button.onClick.AddListener(CallOnExitFunction);

    if (QuitSplash_button) QuitSplash_button.onClick.RemoveAllListeners();
    if (QuitSplash_button) QuitSplash_button.onClick.AddListener(delegate { OpenPopup(QuitPopup_Object); });

    if (audioController) audioController.ToggleMute(false);

    isMusic = true;
    isSound = true;

    if (Sound_Button) Sound_Button.onClick.RemoveAllListeners();
    if (Sound_Button) Sound_Button.onClick.AddListener(ToggleSound);

    if (Music_Button) Music_Button.onClick.RemoveAllListeners();
    if (Music_Button) Music_Button.onClick.AddListener(ToggleMusic);

    if (SkipWinAnimation) SkipWinAnimation.onClick.RemoveAllListeners();
    if (SkipWinAnimation) SkipWinAnimation.onClick.AddListener(SkipWin);

    if (LeftNavBtn) LeftNavBtn.onClick.RemoveAllListeners();
    if (LeftNavBtn) LeftNavBtn.onClick.AddListener(PreviousPage);

    if (RightNavBtn) RightNavBtn.onClick.RemoveAllListeners();
    if (RightNavBtn) RightNavBtn.onClick.AddListener(NextPage);
  }

  internal void LowBalPopup()
  {
    OpenPopup(LBPopup_Object);
  }

  internal void DisconnectionPopup()
  {
    if (!isExit)
    {
      OpenPopup(DisconnectPopup_Object);
    }
  }

  internal void ReconnectionPopup()
  {
    OpenPopup(ReconnectPopup_Object);
  }

  internal void CheckAndClosePopups()
  {
    if (ReconnectPopup_Object.activeInHierarchy)
    {
      ClosePopup(ReconnectPopup_Object);
    }
    if (DisconnectPopup_Object.activeInHierarchy)
    {
      ClosePopup(DisconnectPopup_Object);
    }
  }


  internal void PopulateWin(int value, double amount)
  {
    List<Sprite> selectedSprites = null;

    switch (value)
    {
      case 1: { selectedSprites = BigWin_Sprite; Win_Image.AnimationSpeed = 100; } break;
      case 2: { selectedSprites = BigWin_Sprite; Win_Image.AnimationSpeed = 100; } break;
      case 3: { selectedSprites = HugeWin_Sprite; Win_Image.AnimationSpeed = 100; } break;
    }

    // Guard: don't proceed if no valid sprites
    if (selectedSprites == null || selectedSprites.Count == 0)
    {
      Debug.LogWarning("PopulateWin: no sprites for value " + value);
      slotManager.CheckPopups = false; // unblock the spin flow
      return;
    }

    // Copy sprites instead of direct reference assignment
    Win_Image.textureArray.Clear();
    Win_Image.textureArray.TrimExcess();
    for (int i = 0; i < selectedSprites.Count; i++)
    {
      Win_Image.textureArray.Add(selectedSprites[i]);
    }

    Win_Image.StartAnimation();
    LuckyGirl.StartAnimation();
    StartPopupAnim(amount);
  }
  private void StartFreeSpins(int spins)
  {
    // if (MainPopup_Object) MainPopup_Object.SetActive(false);
    if (FreeSpinPopup_Object) FreeSpinPopup_Object.SetActive(false);

    slotManager.FreeSpin(spins);
  }

  internal void FreeSpinProcess(int spins)
  {
    int ExtraSpins = spins - FreeSpins;
    FreeSpins = spins;
    // Debug.Log("ExtraSpins: " + ExtraSpins);
    // Debug.Log("Total Spins: " + spins);
    if (FreeSpinPopup_Object) FreeSpinPopup_Object.SetActive(true);
    if (Free_Text) Free_Text.text = ExtraSpins.ToString() + " Free spins awarded.";
    // if (MainPopup_Object) MainPopup_Object.SetActive(true);
    DOVirtual.DelayedCall(1.5f, () =>
    {
      StartFreeSpins(spins);
    });
  }

  void SkipWin()
  {
    Debug.Log("Skip win called");
    if (ClosePopupTween != null)
    {
      ClosePopupTween.Kill();
      ClosePopupTween = null;
    }
    if (WinPopupTextTween != null)
    {
      WinPopupTextTween.Kill();
      WinPopupTextTween = null;
    }
    ClosePopup(WinPopup_Object);
    slotManager.CheckPopups = false;
  }

  private void StartPopupAnim(double amount)
  {
    double initAmount = 0;
    if (WinPopup_Object) WinPopup_Object.SetActive(true);
    SpriteNumberText Text = Win_Text.gameObject.GetComponent<SpriteNumberText>();
    Text.AnimateFromZero(amount);
    slotManager.AnimateNormalText(amount);
    audioController.PlayWLAudio("phone");
    // // if (MainPopup_Object) MainPopup_Object.SetActive(true);
    // WinPopupTextTween = DOTween.To(() => initAmount, (val) => initAmount = val, amount, 1f).OnUpdate(() =>
    // {
    //   if (Win_Text) Win_Text.text = initAmount.ToString("F3");
    // });

    ClosePopupTween = DOVirtual.DelayedCall(4f, () =>
    {
      // ClosePopup(WinPopup_Object);
      if (WinPopup_Object) WinPopup_Object.SetActive(false);
      slotManager.CheckPopups = false;
    });
  }

  internal void ADfunction()
  {
    OpenPopup(ADPopup_Object);
  }

  internal void InitialiseUIData(Paylines symbolsText)
  {
    PopulateSymbolsPayout(symbolsText);
  }
  internal void PlayRellsLoop(bool loopanim = false)
  {
    SlotBorder.StopAnimation();

    SlotBorder.textureArray.Clear();

    List<Sprite> source = loopanim ? SlotBorderOne : SlotBorderTwo;

    if (source == null || source.Count == 0)
    {
      Debug.LogError("Source list is empty!");
      return;
    }

    // 🔁 Copy manually using for loop
    for (int i = 0; i < source.Count; i++)
    {
      SlotBorder.textureArray.Add(source[i]);
    }

    SlotBorder.AnimationSpeed = SlotBorder.textureArray.Count - 5;
    SlotBorder.StartAnimation();
  }
  internal void PlayWheelLoop(bool loopanim = false)
  {
    WheelBorder.StopAnimation();
    WheelBorder.textureArray.Clear();
    List<Sprite> source = loopanim ? WheelBorderOne : WheelBorderTwo;

    if (source == null || source.Count == 0)
    {
      Debug.LogError("Source list is empty!");
      return;
    }

    // 🔁 Copy manually using for loop
    for (int i = 0; i < source.Count; i++)
    {
      WheelBorder.textureArray.Add(source[i]);
    }
    WheelBorder.AnimationSpeed = WheelBorder.textureArray.Count;
    WheelBorder.StartAnimation();
  }
  internal void StopWheelborderAnim()
  {

    WheelBorder.StopAnimation();
    WheelBorder.gameObject.GetComponent<Image>().sprite = WheelBorderSprite;
  }
  private void PopulateSymbolsPayout(Paylines paylines)
  {
    double betPerLine = 1;
    //double betPerLine = socketManager.InitialData.bets[slotManager.BetCounter];
    SymbolsText[0].text = (paylines.symbols[6].multiplier[0] * betPerLine).ToString();
    SymbolsText[1].text = (paylines.symbols[7].multiplier[0] * betPerLine).ToString();
    SymbolsText[2].text = (paylines.symbols[1].multiplier[0] * betPerLine).ToString();
    SymbolsText[3].text = (paylines.symbols[4].multiplier[0] * betPerLine).ToString();
    SymbolsText[4].text = (paylines.symbols[3].multiplier[0] * betPerLine).ToString();
    SymbolsText[5].text = (paylines.symbols[5].multiplier[0] * betPerLine).ToString();
    SymbolsText[6].text = (paylines.symbols[2].multiplier[0] * betPerLine).ToString();
    SymbolsText[7].text = socketManager.FeaturesData.specialWins.anyBar.pay.ToString();
    SymbolsText[8].text = (paylines.symbols[5].multiplier[1] * betPerLine).ToString();
    SymbolsText[9].text = (paylines.symbols[5].multiplier[2] * betPerLine).ToString();


  }
  private void NextPage()
  {
    currentPageIndex++;

    // Loop to first page
    if (currentPageIndex >= Pages.Count)
      currentPageIndex = 0;

    ShowPage(currentPageIndex);
  }

  private void PreviousPage()
  {
    currentPageIndex--;

    // Loop to last page
    if (currentPageIndex < 0)
      currentPageIndex = Pages.Count - 1;

    ShowPage(currentPageIndex);
  }

  private void ShowPage(int index)
  {
    for (int i = 0; i < Pages.Count; i++)
    {
      Pages[i].SetActive(i == index);
    }
  }
  internal string GetSymbolDescription(string name)
  {
    if (socketManager.UIData.paylines.symbols == null) return null;
    foreach (var symbol in socketManager.UIData.paylines.symbols)
    {
      if (symbol.name == name)
      {
        return symbol.description;
      }
    }
    return null;
  }

  private void CallOnExitFunction()
  {
    if (!isExit)
    {
      isExit = true;
      audioController.PlayButtonAudio();
      slotManager.CallCloseSocket();
    }
  }

  private void OpenMenu()
  {
    audioController.PlayButtonAudio();
    if (Menu_Object) Menu_Object.SetActive(false);
    if (Exit_Object) Exit_Object.SetActive(true);
    //if (About_Object) About_Object.SetActive(true);
    if (Paytable_Object) Paytable_Object.SetActive(true);
    if (Settings_Object) Settings_Object.SetActive(true);

    //DOTween.To(() => About_RT.anchoredPosition, (val) => About_RT.anchoredPosition = val, new Vector2(About_RT.anchoredPosition.x, About_RT.anchoredPosition.y + 150), 0.1f).OnUpdate(() =>
    //{
    //    LayoutRebuilder.ForceRebuildLayoutImmediate(About_RT);
    //});

    DOTween.To(() => Paytable_RT.anchoredPosition, (val) => Paytable_RT.anchoredPosition = val, new Vector2(Paytable_RT.anchoredPosition.x, Paytable_RT.anchoredPosition.y + 125), 0.1f).OnUpdate(() =>
    {
      LayoutRebuilder.ForceRebuildLayoutImmediate(Paytable_RT);
    });

    DOTween.To(() => Settings_RT.anchoredPosition, (val) => Settings_RT.anchoredPosition = val, new Vector2(Settings_RT.anchoredPosition.x, Settings_RT.anchoredPosition.y + 250), 0.1f).OnUpdate(() =>
    {
      LayoutRebuilder.ForceRebuildLayoutImmediate(Settings_RT);
    });
  }

  private void CloseMenu()
  {

    if (audioController) audioController.PlayButtonAudio();
    //DOTween.To(() => About_RT.anchoredPosition, (val) => About_RT.anchoredPosition = val, new Vector2(About_RT.anchoredPosition.x, About_RT.anchoredPosition.y - 150), 0.1f).OnUpdate(() =>
    //{
    //    LayoutRebuilder.ForceRebuildLayoutImmediate(About_RT);
    //});

    DOTween.To(() => Paytable_RT.anchoredPosition, (val) => Paytable_RT.anchoredPosition = val, new Vector2(Paytable_RT.anchoredPosition.x, Paytable_RT.anchoredPosition.y - 125), 0.1f).OnUpdate(() =>
    {
      LayoutRebuilder.ForceRebuildLayoutImmediate(Paytable_RT);
    });

    DOTween.To(() => Settings_RT.anchoredPosition, (val) => Settings_RT.anchoredPosition = val, new Vector2(Settings_RT.anchoredPosition.x, Settings_RT.anchoredPosition.y - 250), 0.1f).OnUpdate(() =>
    {
      LayoutRebuilder.ForceRebuildLayoutImmediate(Settings_RT);
    });

    DOVirtual.DelayedCall(0.1f, () =>
     {
       if (Menu_Object) Menu_Object.SetActive(true);
       if (Exit_Object) Exit_Object.SetActive(false);
       //if (About_Object) About_Object.SetActive(false);
       if (Paytable_Object) Paytable_Object.SetActive(false);
       if (Settings_Object) Settings_Object.SetActive(false);
     });
  }

  private void OpenPopup(GameObject Popup)
  {
    if (audioController) audioController.PlayButtonAudio();
    if (Popup) Popup.SetActive(true);
    if (MainPopup_Object) MainPopup_Object.SetActive(true);
  }

  internal void ClosePopup(GameObject Popup)
  {
    if (audioController) audioController.PlayButtonAudio();
    if (Popup) Popup.SetActive(false);
    if (!DisconnectPopup_Object.activeSelf)
    {
      if (MainPopup_Object) MainPopup_Object.SetActive(false);
    }
  }

  private void ToggleMusic()
  {
    isMusic = !isMusic;
    if (isMusic)
    {
      if (MusicOn_Object) MusicOn_Object.SetActive(true);
      if (MusicOff_Object) MusicOff_Object.SetActive(false);
      audioController.ToggleMute(false, "bg");
    }
    else
    {
      if (MusicOn_Object) MusicOn_Object.SetActive(false);
      if (MusicOff_Object) MusicOff_Object.SetActive(true);
      audioController.ToggleMute(true, "bg");
    }
  }

  private void UrlButtons(string url)
  {
    Application.OpenURL(url);
  }

  private void ToggleSound()
  {
    isSound = !isSound;
    if (isSound)
    {
      if (SoundOn_Object) SoundOn_Object.SetActive(true);
      if (SoundOff_Object) SoundOff_Object.SetActive(false);
      if (audioController) audioController.ToggleMute(false, "button");
      if (audioController) audioController.ToggleMute(false, "wl");
    }
    else
    {
      if (SoundOn_Object) SoundOn_Object.SetActive(false);
      if (SoundOff_Object) SoundOff_Object.SetActive(true);
      if (audioController) audioController.ToggleMute(true, "button");
      if (audioController) audioController.ToggleMute(true, "wl");
    }
  }

  public void OnFocusChanged(string value)
  {

    bool focused = value == "1";
    Debug.Log("Focus Changed and audio called" + focused);
    if (focused)
    {
      if (!audioController.audioPlayer_button.mute)
      {
        if (audioController) audioController.ToggleMute(false, "button");
        if (audioController) audioController.ToggleMute(false, "wl");
      }
      if (!audioController.bg_adudio.mute)
      {
        if (audioController) audioController.ToggleMute(false, "bg");
      }

    }
    else
    {
      if (audioController) audioController.ToggleMute(true);
      // if (audioController) audioController.ToggleMute(true, "wl");
    }
    //socketManager?.HandleFusChanocge(focused);
  }
}

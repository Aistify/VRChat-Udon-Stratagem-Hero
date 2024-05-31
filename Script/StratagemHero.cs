using System;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;
using VRC.SDK3.Data;
using VRC.SDKBase;
using VRC.Udon.Common.Enums;
using VRC.Udon.Common.Interfaces;
using Random = UnityEngine.Random;

// ReSharper disable ArrangeObjectCreationWhenTypeEvident

// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable once CheckNamespace
namespace A1ST.StratagemHero
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class StratagemHero : UdonSharpBehaviour
    {
        [SerializeField]
        private string jsonString;

        public TextMeshProUGUI currentPlayerText;

        [SerializeField]
        public GameObject[] screens;

        #region Start Screen UI
        [SerializeField]
        private TextMeshProUGUI startScreenInstructionsText;
        #endregion

        #region Get Ready Screen UI
        [SerializeField]
        private TextMeshProUGUI getReadyScreenRoundText;
        #endregion

        #region Game Screen UI
        [SerializeField]
        private TextMeshProUGUI gameScreenRoundText;

        [SerializeField]
        private TextMeshProUGUI gameScreenScoreText;

        [SerializeField]
        private RawImage[] gameScreenStratagemIcons;

        [SerializeField]
        private TextMeshProUGUI gameScreenStratagemNameText;

        [SerializeField]
        private RawImage[] gameScreenStratagemSequenceIcons;

        [SerializeField]
        private GameObject gameScreenTimerBar;

        [SerializeField]
        private Image[] gameScreenImages;

        private readonly Color _orange = new Color(255f / 255f, 128f / 255f, 70f / 255f);
        private readonly Color _yellow = new Color(237f / 255f, 242f / 255f, 196f / 255f);
        #endregion

        #region Round End Screen UI
        [SerializeField]
        private TextMeshProUGUI[] roundEndScreenTextArr;
        #endregion

        #region Game Over Screen UI
        [SerializeField]
        private TextMeshProUGUI gameOverScreenScoreText;
        #endregion

        #region Audio Sources
        [SerializeField]
        private AudioSource gameMusic;

        [SerializeField]
        private AudioSource sfxStart;

        [SerializeField]
        private AudioSource[] sfxClick;
        private int _sfxClickIndex;

        [SerializeField]
        private AudioSource[] sfxFail;
        private int _sfxFailIndex;

        [SerializeField]
        private AudioSource[] sfxComplete;

        [SerializeField]
        private AudioSource sfxScoreShow;
        private int _sfxCompleteIndex;

        [SerializeField]
        private AudioSource sfxWin;

        [SerializeField]
        private AudioSource sfxLose;
        #endregion

        // ReSharper disable InconsistentNaming

        private DataList _stratagemsDataList;
        private float _stratagemImageMod;
        public bool _debug;

        [UdonSynced]
        public bool _gameStart;

        [UdonSynced]
        private bool _gamePaused;

        [UdonSynced]
        private int round;

        [UdonSynced]
        private double roundStartTime;

        [UdonSynced]
        private float roundTimeLeft;

        [UdonSynced]
        private int[] roundStratagems;

        [UdonSynced]
        private int[] stratagemDisplayIndex;

        [UdonSynced]
        private string currentStratagemName;

        [UdonSynced]
        private int[] sequenceDisplayIndex;

        [UdonSynced]
        private int currentRoundIndex;
        private string[] _playerSequence;

        private bool _perfect;

        [UdonSynced]
        private int score;

        [UdonSynced]
        private int[] scoresArr;

        public string controlKeys = "WASD";

        [UdonSynced]
        public int maxStratagemsCount = 16;

        [UdonSynced]
        public float roundMinTime = 10.0f;

        // ReSharper restore InconsistentNaming

        #region Init Data
        private void Start()
        {
            ReloadInstructions();
            LoadJsonString();
            RequestSerialization();
            OnDeserialization();
        }

        public void ReloadInstructions()
        {
            var vrControls = controlKeys == "DDR" ? "the DDR machine" : "the thumbstick";
            startScreenInstructionsText.text = Networking.LocalPlayer.IsUserInVR()
                ? $"Press the green button and use {vrControls} or buttons to play!"
                : $"Press the green button and use {controlKeys} to play!";
        }

        private void LoadJsonString()
        {
            if (VRCJson.TryDeserializeFromJson(jsonString, out var result))
            {
                InitializeGameData(result);
            }
            else
                Debug.Log($"Failed to Deserialize json {jsonString} - {result.ToString()}");
        }

        private void InitializeGameData(DataToken result)
        {
            scoresArr = new int[4];
            roundStratagems = new int[6];
            stratagemDisplayIndex = new int[gameScreenStratagemIcons.Length];
            sequenceDisplayIndex = new int[gameScreenStratagemSequenceIcons.Length];

            _stratagemsDataList = result.DataDictionary["stratagems"].DataList;
            _stratagemImageMod = 1.00f / (float)result.DataDictionary["rows"].Double;
        }
        #endregion

        // Handle Game Over Game State and Display Color
        public override void PostLateUpdate()
        {
            if (!_gameStart || _gamePaused) // Pause game to prevent game over from time up
                return;

            var elapsedTime =
                (DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds - roundStartTime;
            var timeLeftPercent = Mathf.Clamp01(
                (float)(roundTimeLeft - elapsedTime) / roundTimeLeft
            );
            gameScreenTimerBar.transform.localScale = new Vector3(timeLeftPercent, 1, 1);

            if (timeLeftPercent < 0.20f)
            {
                gameScreenRoundText.color = _orange;
                gameScreenScoreText.color = _orange;
                foreach (var image in gameScreenImages)
                    image.color = _orange;
            }
            if (timeLeftPercent > 0.20f)
            {
                gameScreenRoundText.color = _yellow;
                gameScreenScoreText.color = _yellow;
                foreach (var image in gameScreenImages)
                    image.color = _yellow;
            }

            if (
                timeLeftPercent != 0
                || Networking.LocalPlayer != Networking.GetOwner(gameObject)
                || _debug
            )
                return;

            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(GameOver));
        }

        #region Game State Management
        public void ReInitializeGame()
        {
            Debug.Log("Reinitializing Game!");

            round = 0;
            score = 0;

            for (var i = 0; i < screens.Length; i++)
                screens[i].gameObject.SetActive(false);
            screens[0].gameObject.SetActive(true);

            _gameStart = false;
            _gamePaused = false;

            RequestSerialization();
            OnDeserialization();
        }

        public void GetReady()
        {
            if (!_gameStart)
                return;

            Debug.Log("Get Ready!");

            _gamePaused = true;
            round++;

            RequestSerialization();
            OnDeserialization();

            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(GetReadyAudioVisual));
            SendCustomEventDelayedSeconds(nameof(StartNewRound), 3f, EventTiming.LateUpdate);
        }

        public void GetReadyAudioVisual()
        {
            for (var i = 1; i < roundEndScreenTextArr.Length; i++) // Skips the first one but sets the rest to inactive
                roundEndScreenTextArr[i].transform.parent.gameObject.SetActive(false);

            for (var i = 0; i < screens.Length; i++)
                screens[i].gameObject.SetActive(false);
            screens[1].gameObject.SetActive(true);

            gameMusic.Stop();
        }

        public void StartNewRound()
        {
            if (!_gameStart)
                return;

            Debug.Log("Starting New Round!");

            _perfect = true;
            currentRoundIndex = 0;
            roundStartTime = (DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds;
            roundTimeLeft = roundMinTime;
            _gamePaused = false;

            GenerateRoundStratagems();
            RequestSerialization();
            OnDeserialization();

            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(StartNewRoundAudioVisual));
            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ReloadGameScreenUI));
            ClearPlayerInput();
        }

        public void StartNewRoundAudioVisual()
        {
            gameMusic.Play();

            for (var i = 0; i < screens.Length; i++)
                screens[i].gameObject.SetActive(false);
            screens[2].gameObject.SetActive(true);
        }

        private void GenerateRoundStratagems()
        {
            var stratagems = new int[
                maxStratagemsCount == 0 ? round + 5 : Math.Min(round + 5, maxStratagemsCount)
            ];
            for (var i = 0; i < stratagems.Length; i++)
            {
                stratagems[i] = Random.Range(13, _stratagemsDataList.Count);
            }
            roundStratagems = stratagems;
        }

        private void NextSequence()
        {
            Debug.Log("Next Sequence!");

            _gamePaused = true;
            score += GetStratagemSequence(currentRoundIndex).Count * 5;
            currentRoundIndex++;

            RequestSerialization();
            OnDeserialization();

            if (currentRoundIndex + 1 > roundStratagems.Length)
                RoundComplete();
            else
            {
                roundTimeLeft += 1f;
                ReloadGameScreenUI();
                ClearPlayerInput();
            }
        }

        private void RoundComplete()
        {
            Debug.Log("Round Complete!");

            _gamePaused = true;

            var elapsedTime =
                (DateTime.Now - new DateTime(1970, 1, 1)).TotalSeconds - roundStartTime;
            var timeLeftPercent = (roundTimeLeft - elapsedTime) / roundTimeLeft;

            scoresArr = new int[4];
            scoresArr[0] = 50 + round * 25;
            scoresArr[1] = (int)(100 * timeLeftPercent);
            scoresArr[2] = _perfect ? 100 : 0;
            score += scoresArr[0] + scoresArr[1] + scoresArr[2];
            scoresArr[3] = score;

            RequestSerialization();
            OnDeserialization();

            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(RoundCompleteAudioVisual));
            SendCustomEventDelayedSeconds(nameof(GetReady), 4f, EventTiming.LateUpdate);
        }

        public void RoundCompleteAudioVisual()
        {
            gameMusic.Stop();
            sfxWin.PlayOneShot(sfxWin.clip, 1f);

            for (var i = 0; i < screens.Length; i++)
                screens[i].gameObject.SetActive(false);
            screens[3].gameObject.SetActive(true);

            SendCustomEventDelayedSeconds(nameof(TimeBonus), 0.395f, EventTiming.LateUpdate);
            SendCustomEventDelayedSeconds(nameof(PerfectBonus), 0.975f, EventTiming.LateUpdate);
            SendCustomEventDelayedSeconds(nameof(TotalScore), 1.575f, EventTiming.LateUpdate);
        }

        #region ShowBonuses
        public void TimeBonus()
        {
            sfxScoreShow.PlayOneShot(sfxScoreShow.clip);
            roundEndScreenTextArr[1].transform.parent.gameObject.SetActive(true);
        }

        public void PerfectBonus() =>
            roundEndScreenTextArr[2].transform.parent.gameObject.SetActive(true);

        public void TotalScore() =>
            roundEndScreenTextArr[3].transform.parent.gameObject.SetActive(true);
        #endregion

        public void GameOver()
        {
            Debug.Log("Game Over!");

            _gameStart = false;
            _gamePaused = true;

            gameMusic.Stop();
            sfxLose.PlayOneShot(sfxLose.clip, 1);
            for (var i = 0; i < screens.Length; i++)
                screens[i].gameObject.SetActive(false);
            screens[4].gameObject.SetActive(true);

            RequestSerialization();
            OnDeserialization();

            SendCustomEventDelayedSeconds(nameof(ReInitializeGame), 5f, EventTiming.LateUpdate);
        }

        public void ForceReset()
        {
            Debug.Log("Force Reset!");

            gameMusic.Stop();

            SendCustomNetworkEvent(NetworkEventTarget.All, nameof(ReInitializeGame));
        }
        #endregion

        #region Display Control
        public void ReloadGameScreenUI()
        {
            ReInitializeStratagemDisplayIndex();
            ReInitializeSequenceDisplayIndex();
        }

        private void ReInitializeStratagemDisplayIndex()
        {
            for (var i = 0; i < gameScreenStratagemIcons.Length; i++)
            {
                var index = currentRoundIndex + i;
                var stratagemID =
                    index >= roundStratagems.Length ? 0 : roundStratagems[currentRoundIndex + i];

                stratagemDisplayIndex[i] = stratagemID;
            }
            RequestSerialization();
            OnDeserialization();
        }

        private void ReInitializeSequenceDisplayIndex()
        {
            var currentStratagemID = roundStratagems[currentRoundIndex];
            var stratagem = _stratagemsDataList[currentStratagemID];

            currentStratagemName = stratagem
                .DataDictionary["name"]
                .String.ToUpper()
                .Replace("_", " ");

            sequenceDisplayIndex = new int[gameScreenStratagemSequenceIcons.Length];
            var i = 0;

            for (; i < stratagem.DataDictionary["keys"].DataList.Count; i++)
            {
                var stratagemID = 0;
                if (stratagem.DataDictionary["keys"].DataList[i].String == "UP")
                    stratagemID = 1;
                if (stratagem.DataDictionary["keys"].DataList[i].String == "DOWN")
                    stratagemID = 2;
                if (stratagem.DataDictionary["keys"].DataList[i].String == "LEFT")
                    stratagemID = 3;
                if (stratagem.DataDictionary["keys"].DataList[i].String == "RIGHT")
                    stratagemID = 4;

                sequenceDisplayIndex[i] = stratagemID;
            }

            for (; i < gameScreenStratagemSequenceIcons.Length; i++)
                sequenceDisplayIndex[i] = 0;

            RequestSerialization();
            OnDeserialization();
        }
        #endregion

        #region Input Router
        public void Up() => HandleInput("UP");

        public void Down() => HandleInput("DOWN");

        public void Left() => HandleInput("LEFT");

        public void Right() => HandleInput("RIGHT");

        public void DebugMode() => _debug = !_debug;
        #endregion

        private void HandleInput(string key)
        {
            if (_gamePaused)
                return;

            if (!_gameStart)
            {
                sfxStart.PlayOneShot(sfxStart.clip, 1f);
                _gameStart = true;
                GetReady(); // Start Game
                return;
            }

            for (var i = 0; i < _playerSequence.Length; i++)
            {
                if (_playerSequence[i] != "Empty")
                    continue;
                _playerSequence[i] = key;

                var stratagemSequence = GetStratagemSequence(currentRoundIndex);

                // If player made a mistake at sequence index
                if (_playerSequence[i] != stratagemSequence[i].String)
                {
                    _perfect = false;
                    SendCustomNetworkEvent(NetworkEventTarget.All, nameof(FailAudioVisual));

                    for (var j = 0; j < i - 1; j++)
                        UpdateSequenceIconAtIndex(stratagemSequence, j, true); // Make this be an overlay instead so players can continue inputting while it displayed red

                    ClearPlayerInput();
                    ReInitializeSequenceDisplayIndex();
                    return;
                }

                // Else correct input
                SendCustomNetworkEvent(NetworkEventTarget.All, nameof(SuccessAudioVisual));
                UpdateSequenceIconAtIndex(stratagemSequence, i);

                // Player completes sequence
                if (i + 2 > _playerSequence.Length)
                {
                    SendCustomNetworkEvent(NetworkEventTarget.All, nameof(CompleteAudioVisual));
                    NextSequence();
                }
                break;
            }
        }

        public void FailAudioVisual()
        {
            sfxFail[_sfxFailIndex].PlayOneShot(sfxFail[_sfxFailIndex].clip);
            _sfxFailIndex = _sfxFailIndex < sfxFail.Length - 1 ? _sfxFailIndex + 1 : 0;
        }

        public void SuccessAudioVisual()
        {
            sfxClick[_sfxClickIndex].PlayOneShot(sfxClick[_sfxClickIndex].clip);
            _sfxClickIndex = _sfxClickIndex < sfxClick.Length - 1 ? _sfxClickIndex + 1 : 0;
        }

        public void CompleteAudioVisual()
        {
            sfxComplete[_sfxCompleteIndex].PlayOneShot(sfxComplete[_sfxCompleteIndex].clip);
            _sfxCompleteIndex =
                _sfxCompleteIndex < sfxComplete.Length - 1 ? _sfxCompleteIndex + 1 : 0;
        }

        #region Utilities
        private void ClearPlayerInput()
        {
            var keyCount = GetStratagemSequence(currentRoundIndex).Count;
            _playerSequence = new string[keyCount];
            for (var i = 0; i < _playerSequence.Length; i++)
                _playerSequence[i] = "Empty";

            _gamePaused = false;
        }

        private DataList GetStratagemSequence(int stratagemID)
        {
            if (stratagemID >= roundStratagems.Length)
                return _stratagemsDataList[0].DataDictionary["keys"].DataList;

            return _stratagemsDataList[roundStratagems[stratagemID]]
                .DataDictionary["keys"]
                .DataList;
        }

        private float GetXCoordinates(int stratagemID)
        {
            return _stratagemImageMod * (stratagemID % (1.00f / _stratagemImageMod));
        }

        private float GetYCoordinates(int stratagemID)
        {
            return 1
                - _stratagemImageMod * Mathf.FloorToInt(stratagemID / (1.00f / _stratagemImageMod))
                - _stratagemImageMod;
        }

        private void UpdateText()
        {
            getReadyScreenRoundText.text = round.ToString();

            gameScreenScoreText.text = score.ToString();
            gameScreenRoundText.text = round.ToString();

            gameScreenStratagemNameText.text = currentStratagemName;

            for (var i = 0; i < roundEndScreenTextArr.Length; i++)
            {
                roundEndScreenTextArr[i].text = scoresArr[i].ToString();
            }

            gameOverScreenScoreText.text = score.ToString();
        }

        private void UpdateStratagemDisplay()
        {
            for (var i = 0; i < gameScreenStratagemIcons.Length; i++)
            {
                var x = GetXCoordinates(stratagemDisplayIndex[i]);
                var y = GetYCoordinates(stratagemDisplayIndex[i]);
                gameScreenStratagemIcons[i].color = Color.white;
                gameScreenStratagemIcons[i].uvRect = new Rect(
                    x,
                    y,
                    _stratagemImageMod,
                    _stratagemImageMod
                );
            }
        }

        private void UpdateSequenceDisplay()
        {
            for (var i = 0; i < gameScreenStratagemSequenceIcons.Length; i++)
            {
                var x = GetXCoordinates(sequenceDisplayIndex[i]);
                var y = GetYCoordinates(sequenceDisplayIndex[i]);
                gameScreenStratagemSequenceIcons[i].color = Color.white;
                gameScreenStratagemSequenceIcons[i].uvRect = new Rect(
                    x,
                    y,
                    _stratagemImageMod,
                    _stratagemImageMod
                );
            }
        }

        private void UpdateSequenceIconAtIndex(
            DataList stratagemSequence,
            int sequenceIndex,
            bool incorrect = false
        )
        {
            var incorrectInt = incorrect ? 4 : 0;
            var stratagemID = 0;
            if (stratagemSequence[sequenceIndex].String == "UP")
                stratagemID = 5 + incorrectInt;
            if (stratagemSequence[sequenceIndex].String == "DOWN")
                stratagemID = 6 + incorrectInt;
            if (stratagemSequence[sequenceIndex].String == "LEFT")
                stratagemID = 7 + incorrectInt;
            if (stratagemSequence[sequenceIndex].String == "RIGHT")
                stratagemID = 8 + incorrectInt;

            sequenceDisplayIndex[sequenceIndex] = stratagemID;

            RequestSerialization();
            OnDeserialization();
        }
        #endregion

        public override void OnDeserialization()
        {
            UpdateText();
            UpdateStratagemDisplay();
            UpdateSequenceDisplay();
        }
    }
}

using JetBrains.Annotations;
using UdonSharp;
using UnityEngine;
using VRC.SDKBase;
using VRC.Udon.Common;

// ReSharper disable ForCanBeConvertedToForeach
// ReSharper disable once CheckNamespace
namespace A1ST.StratagemHero
{
    [UdonBehaviourSyncMode(BehaviourSyncMode.Manual)]
    public class InputHandler : UdonSharpBehaviour
    {
        [SerializeField]
        public StratagemHero stratagemHero;

        [SerializeField]
        private GameObject ddrMachine;

        [SerializeField]
        private MeshRenderer[] contactRenderers;

        [UdonSynced]
        public int currentPlayerId = 999;

        private VRCPlayerApi _localPlayer;

        public string keymap = "QWERTY";

        public float _thresholdHigh = 0.5f;
        public float _thresholdLow = 0.10f;

        private bool _horizontalSent;
        private bool _verticalSent;

        #region Input Handling
        // Desktop Controls
        public void Update()
        {
            if (!IsLocalPlayerOwner() || _localPlayer.IsUserInVR())
                return;

            switch (keymap)
            {
                case "QWERTY":
                {
                    if (Input.GetKeyDown(KeyCode.W))
                        stratagemHero.Up();
                    else if (Input.GetKeyDown(KeyCode.S))
                        stratagemHero.Down();
                    else if (Input.GetKeyDown(KeyCode.A))
                        stratagemHero.Left();
                    if (Input.GetKeyDown(KeyCode.D))
                        stratagemHero.Right();
                    break;
                }
                case "AZERTY":
                {
                    if (Input.GetKeyDown(KeyCode.Z))
                        stratagemHero.Up();
                    else if (Input.GetKeyDown(KeyCode.S))
                        stratagemHero.Down();
                    else if (Input.GetKeyDown(KeyCode.Q))
                        stratagemHero.Left();
                    if (Input.GetKeyDown(KeyCode.D))
                        stratagemHero.Right();
                    break;
                }
                case "JCUKEN":
                {
                    if (Input.GetKeyDown(KeyCode.U))
                        stratagemHero.Up();
                    else if (Input.GetKeyDown(KeyCode.N))
                        stratagemHero.Left();
                    else if (Input.GetKeyDown(KeyCode.H))
                        stratagemHero.Down();
                    if (Input.GetKeyDown(KeyCode.K))
                        stratagemHero.Right();
                    break;
                }
                case "BEPO":
                {
                    if (Input.GetKeyDown(KeyCode.Z))
                        stratagemHero.Up();
                    else if (Input.GetKeyDown(KeyCode.Q))
                        stratagemHero.Left();
                    else if (Input.GetKeyDown(KeyCode.S))
                        stratagemHero.Down();
                    if (Input.GetKeyDown(KeyCode.D))
                        stratagemHero.Right();
                    break;
                }
                case "Arrow Keys":
                {
                    if (Input.GetKeyDown(KeyCode.UpArrow))
                        stratagemHero.Up();
                    else if (Input.GetKeyDown(KeyCode.LeftArrow))
                        stratagemHero.Left();
                    else if (Input.GetKeyDown(KeyCode.DownArrow))
                        stratagemHero.Down();
                    if (Input.GetKeyDown(KeyCode.RightArrow))
                        stratagemHero.Right();
                    break;
                }
                case "DDR":
                {
                    break;
                }
                default:
                    keymap = "QWERTY";
                    break;
            }
        }

        // VR Thumbstick Controls
        public override void InputMoveHorizontal(float axisPosition, UdonInputEventArgs args)
        {
            if (!IsLocalPlayerOwner() || !_localPlayer.IsUserInVR() || keymap == "DDR")
                return;

            if (axisPosition >= -_thresholdLow && axisPosition <= _thresholdLow)
                _horizontalSent = false;

            if (_horizontalSent)
                return;
            if (axisPosition < -_thresholdHigh)
            {
                stratagemHero.Left();
                _horizontalSent = true;
            }
            else if (axisPosition > _thresholdHigh)
            {
                stratagemHero.Right();
                _horizontalSent = true;
            }
        }

        public override void InputMoveVertical(float axisPosition, UdonInputEventArgs args)
        {
            if (!IsLocalPlayerOwner() || !_localPlayer.IsUserInVR() || keymap == "DDR")
                return;

            if (axisPosition >= -_thresholdLow && axisPosition <= _thresholdLow)
                _verticalSent = false;

            if (_verticalSent)
                return;
            if (axisPosition < -_thresholdHigh)
            {
                stratagemHero.Down();
                _verticalSent = true;
            }
            else if (axisPosition > _thresholdHigh)
            {
                stratagemHero.Up();
                _verticalSent = true;
            }
        }
        #endregion

        private void Start()
        {
            _localPlayer = Networking.LocalPlayer;
            RequestSerialization();
        }

        #region Function Handling
        public void SetOptions(string option, string param)
        {
            switch (option)
            {
                case "MaxStratagemCount":
                    stratagemHero.maxStratagemsCount = int.Parse(param);
                    break;
                case "MinRoundTime":
                    stratagemHero.roundMinTime = float.Parse(param);
                    break;
                case "_thresholdHigh":
                    _thresholdHigh = float.Parse(param);
                    break;
                case "_thresholdLow":
                    _thresholdLow = float.Parse(param);
                    break;
            }
        }

        public void SetKeyMap(string param)
        {
            keymap = param;
            // ReSharper disable StringLiteralTypo
            switch (param)
            {
                case "QWERTY":
                    stratagemHero.controlKeys = "WASD";
                    break;
                case "AZERTY":
                    stratagemHero.controlKeys = "ZSQD";
                    break;
                case "JCUKEN":
                    stratagemHero.controlKeys = "UNHK";
                    break;
                case "BEPO":
                    stratagemHero.controlKeys = "ZQSD";
                    break;
                case "DDR":
                    stratagemHero.controlKeys = "DDR";
                    break;
                case "Arrow Keys":
                    stratagemHero.controlKeys = "Arrow Keys";
                    break;
                default:
                    keymap = "QWERTY";
                    stratagemHero.controlKeys = "WASD";
                    break;
            }
            // ReSharper restore StringLiteralTypo
            ToggleDDR(keymap == "DDR");
            stratagemHero.ReloadInstructions();
        }

        [UsedImplicitly]
        public void HideContacts()
        {
            for (var i = 0; i < contactRenderers.Length; i++)
            {
                contactRenderers[i].enabled = !contactRenderers[i].enabled;
            }
        }

        [UsedImplicitly]
        public void UnsetOwner()
        {
            if (currentPlayerId != _localPlayer.playerId)
                return;
            currentPlayerId = 999;
            _localPlayer.SetRunSpeed();
            _localPlayer.SetWalkSpeed();
            _localPlayer.SetStrafeSpeed();

            if (stratagemHero._gameStart)
                stratagemHero.GameOver();

            RequestSerialization();
            OnDeserialization();
        }

        [UsedImplicitly]
        public void SetOwner()
        {
            if (currentPlayerId != 999)
                return;
            Networking.SetOwner(_localPlayer, gameObject);
            Networking.SetOwner(_localPlayer, stratagemHero.gameObject);
            for (var i = 0; i < stratagemHero.screens.Length; i++)
                Networking.SetOwner(_localPlayer, stratagemHero.screens[i]);

            currentPlayerId = _localPlayer.playerId;
            _localPlayer.SetRunSpeed(0);
            _localPlayer.SetWalkSpeed(0);
            _localPlayer.SetStrafeSpeed(0);
            RequestSerialization();
            OnDeserialization();
        }

        [UsedImplicitly]
        public void Up()
        {
            if (!IsLocalPlayerOwner())
                return;

            stratagemHero.Up();
        }

        [UsedImplicitly]
        public void Down()
        {
            if (!IsLocalPlayerOwner())
                return;

            stratagemHero.Down();
        }

        [UsedImplicitly]
        public void Left()
        {
            if (!IsLocalPlayerOwner())
                return;

            stratagemHero.Left();
        }

        [UsedImplicitly]
        public void Right()
        {
            if (!IsLocalPlayerOwner())
                return;

            stratagemHero.Right();
        }

        [UsedImplicitly]
        public void DebugMode() => stratagemHero.DebugMode();

        [UsedImplicitly]
        public void ForceReset() => stratagemHero.ForceReset();
        #endregion

        // ReSharper disable once InconsistentNaming
        private void ToggleDDR(bool toggle)
        {
            if (!IsLocalPlayerOwner())
                return;

            if (toggle)
            {
                keymap = "DDR";
                _localPlayer.SetRunSpeed();
                _localPlayer.SetWalkSpeed();
                _localPlayer.SetStrafeSpeed();
            }
            else
            {
                _localPlayer.SetRunSpeed(0);
                _localPlayer.SetWalkSpeed(0);
                _localPlayer.SetStrafeSpeed(0);
            }
            ddrMachine.SetActive(toggle);
        }

        private bool IsLocalPlayerOwner()
        {
            return _localPlayer != null && _localPlayer.playerId == currentPlayerId;
        }

        private void UpdateCurrentPlayerDisplay()
        {
            var currentPlayer = VRCPlayerApi.GetPlayerById(currentPlayerId);
            stratagemHero.currentPlayerText.text =
                currentPlayerId == 999
                    ? "Current Player: "
                    : $"Current Player: {currentPlayer.displayName}";
        }

        public override void OnDeserialization()
        {
            UpdateCurrentPlayerDisplay();
        }
    }
}

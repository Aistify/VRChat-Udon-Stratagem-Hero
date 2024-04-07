using A1ST.StratagemHero;
using JetBrains.Annotations;
using TMPro;
using UdonSharp;
using UnityEngine;
using UnityEngine.UI;

public class UInputField : UdonSharpBehaviour
{
    [SerializeField]
    private InputField inputField;

    [SerializeField]
    private Text _textInput;

    [SerializeField]
    private TextMeshProUGUI textDisplay;

    [SerializeField]
    private InputHandler inputHandler;

    [SerializeField]
    private string option;

    private void Start()
    {
        _textInput = inputField.textComponent;
    }

    [UsedImplicitly]
    public void HandleInput()
    {
        textDisplay.text = _textInput.text;
        inputHandler.SetOptions(option, _textInput.text);
    }
}

using A1ST.StratagemHero;
using UdonSharp;
using UnityEngine;

public class UButton : UdonSharpBehaviour
{
    [SerializeField]
    private InputHandler inputHandler;

    [SerializeField]
    private string function;

    [SerializeField]
    private string param;

    public override void Interact() => HandleInteract();

    private void OnTriggerEnter(Collider other) => HandleInteract();

    // ReSharper disable once MemberCanBePrivate.Global
    public void HandleInteract()
    {
        Debug.Log(function);
        Debug.Log(nameof(inputHandler.HideContacts));

        if (function != "SetVar")
            inputHandler.SendCustomEvent(function);
        else
        {
            Debug.Log($"Setting Keymap to {param}");
            inputHandler.SetKeyMap(param);
        }
    }
}

using System.Collections;
using UnityEngine;
using System.Collections.Generic;
using UnityEngine.UI;

[RequireComponent(typeof(SpriteRenderer))]
public class HintIndicator : Singleton<HintIndicator>
{
    private SpriteRenderer spriteRenderer;
    private Transform hintLocation;
    private Coroutine autoHintCoroutine;
    [SerializeField] private Button hintButton;
    [SerializeField] private float delayBeforeAutoHint;
    protected override void Init()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        spriteRenderer.enabled = false;
        hintButton.interactable = false;
    }

    public void IndicateHint(Transform hintLocation)
    {
        CancelHint();
        transform.position = hintLocation.position;
        spriteRenderer.enabled = true;
    }

    public void CancelHint()
    {
        spriteRenderer.enabled = false;
        hintButton.interactable = false;
        if (autoHintCoroutine != null)
            StopCoroutine(autoHintCoroutine);
        autoHintCoroutine = null;
    }

    public void EnableHintButton()
    {
        hintButton.interactable = true;
    }

    public void StartAutoHint(Transform hintLocation)
    {
        this.hintLocation = hintLocation;
        autoHintCoroutine = StartCoroutine(WaitAndIndicateHint());
    }
    public IEnumerator WaitAndIndicateHint()
    {
        yield return new WaitForSeconds(delayBeforeAutoHint);
        //IndicateHint(hintLocation);
        EnableHintButton();
    }
}
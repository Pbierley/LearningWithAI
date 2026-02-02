using System.Collections;
using UnityEngine;
using Mediapipe.Unity.Sample.MediaPipeVideo;
using UnityEngine.UI;

public class TutorialPanelSwitcher : MonoBehaviour
{
    public CanvasGroup introPanel;   // assign IntroPanel's CanvasGroup
    public CanvasGroup cameraPanel;  // assign CameraPanel's CanvasGroup

    public float fadeDuration = 0.8f;

    private void Start()
    {
        // Start: intro visible, camera hidden
        SetCanvasGroupState(introPanel, 1f, true, true);
        SetCanvasGroupState(cameraPanel, 0f, false, false);
    }

    // Assign this to your NextButton.OnClick()
    public void OnNextButtonPressed()
    {
        // start fade coroutine (non-blocking)
        StartCoroutine(FadeIntroToCamera());
    }

    private IEnumerator FadeIntroToCamera()
    {
        float t = 0f;

        // Ensure both active so CanvasGroup.alpha works during transition
        introPanel.gameObject.SetActive(true);
        cameraPanel.gameObject.SetActive(true);

        // Initially: intro alpha = 1, camera alpha = 0
        introPanel.alpha = 1f;
        cameraPanel.alpha = 0f;

        introPanel.interactable = false;
        introPanel.blocksRaycasts = false;
        cameraPanel.interactable = false;
        cameraPanel.blocksRaycasts = false;

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float a = t / fadeDuration;
            introPanel.alpha = Mathf.Lerp(1f, 0f, a);
            cameraPanel.alpha = Mathf.Lerp(0f, 1f, a);
            yield return null;
        }

        // finalize states
        SetCanvasGroupState(introPanel, 0f, false, false);
        SetCanvasGroupState(cameraPanel, 1f, true, true);

        // Try to open MediaPipe video solution if present in the scene
        // The MediaPipe sample places the runner on a GameObject named "Solution".
        var solObj = GameObject.Find("Solution");
        if (solObj == null)
        {
            Debug.LogWarning("TutorialPanelSwitcher: Could not find GameObject named 'Solution' to start MediaPipe video.");
            yield break;
        }

        // If the Solution object is inactive, activate it so its Awake/Start run.
        var wasActive = solObj.activeInHierarchy;
        if (!wasActive)
        {
            Debug.Log("TutorialPanelSwitcher: Activating 'Solution' GameObject so MediaPipe can initialize.");
            solObj.SetActive(true);
            // Wait one frame so Unity runs Awake/Start on the activated objects
            yield return null;
        }

        var solutionComp = solObj.GetComponent<MediaPipeVideoSolution>();
        if (solutionComp == null)
        {
            Debug.LogWarning("TutorialPanelSwitcher: 'Solution' GameObject found but does not have MediaPipeVideoSolution component.");
            yield break;
        }

        Debug.Log("TutorialPanelSwitcher: Starting MediaPipe video solution.");
        // Start or resume the MediaPipe solution runner
        solutionComp.Play();
    }

    private void SetCanvasGroupState(CanvasGroup cg, float alpha, bool active, bool interactable)
    {
        cg.alpha = alpha;
        cg.gameObject.SetActive(active);
        cg.interactable = interactable;
        cg.blocksRaycasts = interactable;
    }
}

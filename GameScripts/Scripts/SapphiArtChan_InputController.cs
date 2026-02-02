using UnityEngine;

public class SapphiArtChan_InputController : MonoBehaviour
{
    private Animator animator;
    private string currentAnim = "idle";
    private bool actionPlaying = false;

    [Header("Magic Settings")]
    public GameObject aoePrefab;      // drag your AoE prefab here
    public Transform spawnPoint;      // where it spawns (e.g. empty GameObject at feet)

    void Start()
    {
        animator = GetComponent<Animator>();
        SetIdle();
    }

    void Update()
    {
        // Only accept input if not in the middle of an action
        if (!actionPlaying)
        {
            if (Input.GetKeyDown(KeyCode.Alpha1))
                PlayAction("hit01", "param_idletohit01");

            else if (Input.GetKeyDown(KeyCode.Alpha2))
                PlayAction("hit02", "param_idletohit02");

            else if (Input.GetKeyDown(KeyCode.Alpha3))
                PlayAction("hit03", "param_idletohit03");

            else if (Input.GetKeyDown(KeyCode.E))
                PlayAction("winpose", "param_idletowinpose");
            
            else if (Input.GetKeyDown(KeyCode.Q))
                Instantiate(aoePrefab, spawnPoint.position, spawnPoint.rotation);
        }

        // Check if action animation finished → go back to Idle
        if (actionPlaying)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(0);

            // wait until current animation is done
            if (state.normalizedTime >= 1f && !state.loop)
            {
                actionPlaying = false;
                SetIdle();
            }
        }
    }

    private void SetIdle()
    {
        ResetAllFlags();
        animator.SetBool("param_toidle", true);
        currentAnim = "idle";
    }

    private void PlayAction(string animName, string triggerParam)
    {
        ResetAllFlags();
        animator.SetBool(triggerParam, true);
        currentAnim = animName;
        actionPlaying = true;
    }

    private void ResetAllFlags()
    {
        animator.SetBool("param_toidle", false);
        animator.SetBool("param_idletowalk", false);
        animator.SetBool("param_idletorunning", false);
        animator.SetBool("param_idletojump", false);
        animator.SetBool("param_idletowinpose", false);
        animator.SetBool("param_idletoko_big", false);
        animator.SetBool("param_idletodamage", false);
        animator.SetBool("param_idletohit01", false);
        animator.SetBool("param_idletohit02", false);
        animator.SetBool("param_idletohit03", false);
    }
}
